namespace ChainRenamer.Engine.Modules;

/// <summary>Sorts the files by their current name, in natural order. Equal names keep their order.</summary>
public sealed record SortModule(bool Descending = false) : IModule
{
    public IReadOnlyList<Item> Apply(IReadOnlyList<Item> input) =>
        [.. Descending
            ? input.OrderByDescending(i => i.Name, NaturalOrder.Comparer)
            : input.OrderBy(i => i.Name, NaturalOrder.Comparer)];
}
