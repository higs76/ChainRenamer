using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace ChainRenamer.Engine.Modules;

/// <summary>
/// Sorts the files by their current name, in natural order. Equal names keep their order.
/// The user can then move files by hand: <see cref="ManualOrder"/> overrides the natural order.
/// </summary>
/// <summary>What files are sorted by.</summary>
public enum SortBy
{
    Name,
    Modified,

    /// <summary>The shooting date (metadata); the modification date for a file without one.</summary>
    Taken,
    Size,
}

[Module("Sort")]
public sealed record SortModule(bool Descending = false, SortBy By = SortBy.Name) : IModule
{
    /// <summary>
    /// The order set by hand, or empty. Files missing from it (e.g. loaded afterwards) follow, in natural order.
    /// Session only: not saved with the chain.
    /// </summary>
    [JsonIgnore]
    public ImmutableArray<FileId> ManualOrder { get; init; } = [];

    public IReadOnlyList<Item> Apply(IReadOnlyList<Item> input)
    {
        // Equal values keep the natural order of their names.
        var byName = input.OrderBy(i => i.Name, NaturalOrder.Comparer);
        var sorted = By switch
        {
            SortBy.Modified => Descending ? byName.OrderByDescending(i => i.Modified) : byName.OrderBy(i => i.Modified),
            SortBy.Taken => Descending ? byName.OrderByDescending(Taken) : byName.OrderBy(Taken),
            SortBy.Size => Descending ? byName.OrderByDescending(i => i.Size) : byName.OrderBy(i => i.Size),
            _ => Descending ? input.OrderByDescending(i => i.Name, NaturalOrder.Comparer) : byName,
        };
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

    private static DateTime Taken(Item item) =>
        item.Metadata.TryGetValue("taken", out var taken) && DateTime.TryParse(taken, System.Globalization.CultureInfo.InvariantCulture, out var date) ? date : item.Modified;
}
