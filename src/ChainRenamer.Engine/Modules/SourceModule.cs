using System.Collections.Immutable;

namespace ChainRenamer.Engine.Modules;

/// <summary>
/// The fixed first step: where the files come from, and which of them the user unchecked.
/// Listing the folder is done by <see cref="FileScanner"/>, since modules never touch the disk.
/// </summary>
public sealed record SourceModule(string Folder, bool IncludeSubfolders = false) : IModule
{
    /// <summary>Files unchecked by the user. Session only: not saved with the chain.</summary>
    public ImmutableHashSet<FileId> Excluded { get; init; } = [];

    public IReadOnlyList<Item> Apply(IReadOnlyList<Item> input) =>
        Excluded.IsEmpty ? input : [.. input.Where(i => !Excluded.Contains(i.FileId))];
}
