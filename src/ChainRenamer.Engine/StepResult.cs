namespace ChainRenamer.Engine;

/// <summary>What a step produced. On error, <see cref="Items"/> is the step's unchanged input.</summary>
public sealed record StepResult(IReadOnlyList<Item> Items, Exception? Error = null)
{
    public bool Failed => Error is not null;
}
