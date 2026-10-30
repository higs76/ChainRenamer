namespace ChainRenamer.Engine;

/// <summary>Stable identifier of a source file, independent of its name and rank.</summary>
public readonly record struct FileId(Guid Value)
{
    public static FileId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

/// <summary>Stable identifier of a step, independent of its position in the chain.</summary>
public readonly record struct StepId(Guid Value)
{
    public static StepId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}
