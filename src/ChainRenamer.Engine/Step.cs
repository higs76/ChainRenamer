using System.Collections.Immutable;

namespace ChainRenamer.Engine;

/// <summary>A module placed in the chain, with its optional display name and enabled state.</summary>
public sealed record Step(StepId Id, IModule Module, string? Name = null, bool IsEnabled = true)
{
    /// <summary>
    /// Files this step leaves as they are, e.g. the "collateral damage" of a rule meant for the others.
    /// Only for name modules: they only see the other files, which keep their rank.
    /// Session only: not saved with the chain.
    /// </summary>
    public ImmutableHashSet<FileId> Skipped { get; init; } = [];

    /// <summary>The part of the name a name step works on, or null for the whole name. Saved with the chain.</summary>
    public Segment? Segment { get; init; }

    /// <summary>
    /// Runs the module of the step. A name module does not see the skipped files, which are put back unchanged at
    /// their rank; with a segment, it only sees that part of each name, then the name is put back together.
    /// </summary>
    public IReadOnlyList<Item> Apply(IReadOnlyList<Item> input)
    {
        var segment = Segment is { IsWhole: false } s ? s : null;
        if ((Skipped.IsEmpty && segment is null) || Module is not INameModule) return Module.Apply(input);

        var around = new Dictionary<FileId, (string Prefix, string Suffix)>();
        var visible = new List<Item>();
        foreach (var item in input.Where(i => !Skipped.Contains(i.FileId)))
        {
            if (segment is null) visible.Add(item);
            else if (segment.Split(item.Stem) is { } cut)
            {
                around[item.FileId] = (cut.Prefix, cut.Suffix);
                visible.Add(item with { Name = cut.Part + item.Extension });
            }
        }

        var renamed = Module.Apply(visible).ToDictionary(i => i.FileId);
        return [.. input.Select(i => !renamed.TryGetValue(i.FileId, out var r) ? i
            : around.TryGetValue(i.FileId, out var a) ? r with { Name = a.Prefix + r.Stem + a.Suffix + r.Extension }
            : r)];
    }
}
