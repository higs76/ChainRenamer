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
/// 1) every file takes a unique temporary name derived from its FileId;
/// 2) every file takes its final name.
/// This handles swaps (A↔B), cycles, renumbering and case-only changes on case-insensitive file systems
/// (NTFS, SMB). Each move is written to the journal as soon as it is done.
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
            var done = new List<JournalEntry>();
            int total = plan.Operations.Count * 2;
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

                    foreach (var op in plan.Operations) Move(op.SourcePath, TemporaryPath(op));
                    foreach (var op in plan.Operations) Move(TemporaryPath(op), op.TargetPath);
                    return new ApplyResult(plan.Operations.Count);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or OperationCanceledException)
                {
                    int renamed = done.Count - plan.Operations.Count;
                    return new ApplyResult(Math.Max(renamed, 0), ex, RolledBack: Undo(done, journal));
                }
            }
        }, CancellationToken.None);
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
