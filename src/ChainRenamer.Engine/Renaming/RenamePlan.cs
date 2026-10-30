namespace ChainRenamer.Engine.Renaming;

/// <summary>One file to rename.</summary>
public sealed record RenameOperation(FileId FileId, string SourcePath, string TargetPath);

public sealed record Conflict(FileId FileId, ConflictKind Kind);

/// <summary>
/// What applying the chain would do: the files to rename, and the conflicts that prevent it.
/// Built from the output of any step, so that each step of the preview can show its conflicts.
/// </summary>
public sealed record RenamePlan(IReadOnlyList<RenameOperation> Operations, IReadOnlyList<Conflict> Conflicts)
{
    private static readonly StringComparer PathComparer = StringComparer.OrdinalIgnoreCase;

    public bool CanApply => Conflicts.Count == 0 && Operations.Count > 0;

    /// <param name="files">Every file listed by the source; those absent from <paramref name="output"/> keep their name.</param>
    /// <param name="output">The names wanted, e.g. the output of the last step.</param>
    /// <param name="exists">Checks the disk for a name taken by something outside the renaming. Null: no disk check.</param>
    public static RenamePlan Create(IReadOnlyList<FileItem> files, IReadOnlyList<Item> output, Func<string, bool>? exists = null)
    {
        var newNames = output.ToDictionary(i => i.FileId, i => i.Name);
        var operations = new List<RenameOperation>();
        var conflicts = new List<Conflict>();

        // Where every file will be once renamed; files left out or unchanged stay where they are.
        var finalPaths = files.ToDictionary(
            f => f.Id,
            f => newNames.TryGetValue(f.Id, out var name) ? Path.Combine(f.Directory, name) : f.FullPath);

        foreach (var file in files)
        {
            var target = finalPaths[file.Id];
            if (target == file.FullPath) continue;   // ordinal: a case-only change is a real rename

            operations.Add(new(file.Id, file.FullPath, target));
            // The name itself, not Path.GetFileName(target): a '\' in it must be reported, not read as a folder.
            if (CheckTarget(newNames[file.Id], target) is { } kind) conflicts.Add(new(file.Id, kind));
        }

        // Two files ending up with the same path: the renamed ones are in conflict.
        var renamed = operations.Select(o => o.FileId).ToHashSet();
        conflicts.AddRange(finalPaths
            .GroupBy(p => p.Value, PathComparer)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g)
            .Where(p => renamed.Contains(p.Key))
            .Select(p => new Conflict(p.Key, ConflictKind.Duplicate)));

        if (exists is not null)
        {
            // A path that belongs to a listed file is freed or kept by the plan; any other one must be free.
            var listedPaths = files.Select(f => f.FullPath).ToHashSet(PathComparer);
            conflicts.AddRange(operations
                .Where(o => !listedPaths.Contains(o.TargetPath) && exists(o.TargetPath))
                .Select(o => new Conflict(o.FileId, ConflictKind.AlreadyExists)));
        }

        return new(operations, [.. conflicts.DistinctBy(c => c.FileId)]);
    }

    private static ConflictKind? CheckTarget(string name, string path) =>
        NameRules.Check(name) ?? (path.Length > NameRules.MaxPathLength ? ConflictKind.PathTooLong : null);
}
