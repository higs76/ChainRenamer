using System.Collections.Immutable;

namespace ChainRenamer.Engine.Modules;

/// <summary>
/// Sorts the files by their current name, in natural order. Equal names keep their order.
/// The user can then move files by hand: <see cref="ManualOrder"/> overrides the natural order.
/// </summary>
public sealed record SortModule(bool Descending = false) : IModule
{
    /// <summary>
    /// The order set by hand, or empty. Files missing from it (e.g. loaded afterwards) follow, in natural order.
    /// Session only: not saved with the chain.
    /// </summary>
    public ImmutableArray<FileId> ManualOrder { get; init; } = [];

    public IReadOnlyList<Item> Apply(IReadOnlyList<Item> input)
    {
        var sorted = Descending
            ? input.OrderByDescending(i => i.Name, NaturalOrder.Comparer)
            : input.OrderBy(i => i.Name, NaturalOrder.Comparer);
        if (ManualOrder.IsEmpty) return [.. sorted];

        var rank = ManualOrder.Select((id, index) => (id, index)).ToDictionary(p => p.id, p => p.index);
        return [.. sorted.OrderBy(i => rank.GetValueOrDefault(i.FileId, int.MaxValue))];
    }

    /// <summary>The manual order obtained by moving one file of <paramref name="order"/> by <paramref name="offset"/> places.</summary>
    public static ImmutableArray<FileId> Move(IReadOnlyList<Item> order, FileId id, int offset)
    {
        var ids = order.Select(i => i.FileId).ToList();
        int from = ids.IndexOf(id);
        if (from < 0) return [.. ids];

        int to = Math.Clamp(from + offset, 0, ids.Count - 1);
        ids.RemoveAt(from);
        ids.Insert(to, id);
        return [.. ids];
    }
}
