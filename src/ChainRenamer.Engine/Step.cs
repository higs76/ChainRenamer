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
}
