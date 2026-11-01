namespace ChainRenamer.Engine.Renaming;

/// <param name="Renamed">Files that received their new name.</param>
/// <param name="Error">What stopped the renaming, or null on success.</param>
/// <param name="RolledBack">After an error: every move already done was undone.</param>
public sealed record ApplyResult(int Renamed, Exception? Error = null, bool RolledBack = false)
{
    public bool Succeeded => Error is null;
}

/// <summary>
/// The only place where files are really renamed, in two phases:
/// 1) the files whose new name is still taken by a file of the renaming take a unique temporary name,
///    derived from their FileId;
/// 2) the other files take their new name directly, then the waiting ones take theirs.
/// This handles swaps (A↔B), cycles, renumbering and case-only changes on case-insensitive file systems
/// (NTFS, SMB), with a single move for most files. Each move is written to the journal as soon as it is done.
/// </summary>
public static class Applier
{
    public static string TemporaryPath(RenameOperation operation) =>
        Path.Combine(Path.GetDirectoryName(operation.SourcePath)!, $"~chainrenamer-{operation.FileId}.tmp");

    /// <exception cref="InvalidOperationException">The plan has conflicts.</exception>
    public static Task<ApplyResult> ApplyAsync(RenamePlan plan, string journalPath,
        IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        if (plan.Conflicts.Count > 0) throw new InvalidOperationException("A plan with conflicts cannot be applied.");

        return Task.Run(() =>
        {
            var viaTemporary = NeedingTemporary(plan.Operations);
            var done = new List<JournalEntry>();
            int total = plan.Operations.Count + viaTemporary.Count, renamed = 0;
            using (var journal = Journal.Open(journalPath))
            {
                try
                {
                    void Move(string from, string to)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        File.Move(from, to);   // never overwrites: an unexpected file stops everything
                        var entry = new JournalEntry(from, to);
                        journal.Write(entry);
                        done.Add(entry);
                        progress?.Report((double)done.Count / total);
                    }

                    // 1) Out of the way: the files whose new name is still taken by a file of the renaming.
                    foreach (var op in viaTemporary) Move(op.SourcePath, TemporaryPath(op));
                    // 2) The others go straight to a free name; then the waiting ones, to names freed meanwhile.
                    foreach (var op in plan.Operations.Except(viaTemporary)) { Move(op.SourcePath, op.TargetPath); renamed++; }
                    foreach (var op in viaTemporary) { Move(TemporaryPath(op), op.TargetPath); renamed++; }
                    return new ApplyResult(renamed);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or OperationCanceledException)
                {
                    return new ApplyResult(renamed, ex, RolledBack: Undo(done, journal));
                }
            }
        }, CancellationToken.None);
    }

    /// <summary>
    /// The operations whose target is the current path of a file of the renaming (itself included, for a case-only
    /// change on a case-insensitive file system): swaps, cycles, shifts. Any other target is free, as the plan checked.
    /// </summary>
    public static IReadOnlyList<RenameOperation> NeedingTemporary(IReadOnlyList<RenameOperation> operations)
    {
        var sources = operations.Select(o => o.SourcePath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return [.. operations.Where(o => sources.Contains(o.TargetPath))];
    }

    /// <summary>Puts back every file of a journal. Can be run again safely: only what remains is undone.</summary>
    /// <returns>True if every move was undone.</returns>
    public static Task<bool> UndoAsync(string journalPath) => Task.Run(() =>
    {
        var toUndo = Journal.ToUndo(Journal.Read(journalPath));
        using var journal = Journal.Open(journalPath);
        return Undo(toUndo, journal);
    });

    // Newest first; each move undone is written to the journal.
    private static bool Undo(IReadOnlyList<JournalEntry> moves, Journal journal)
    {
        bool complete = true;
        foreach (var move in moves.Reverse())
        {
            try
            {
                File.Move(move.To, move.From);
                journal.Write(move with { Undo = true });
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                complete = false;
            }
        }
        return complete;
    }
}
