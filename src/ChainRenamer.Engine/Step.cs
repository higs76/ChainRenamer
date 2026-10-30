namespace ChainRenamer.Engine;

/// <summary>A module placed in the chain, with its optional display name and enabled state.</summary>
public sealed record Step(StepId Id, IModule Module, string? Name = null, bool IsEnabled = true);
