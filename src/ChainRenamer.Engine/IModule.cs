namespace ChainRenamer.Engine;

/// <summary>
/// A renaming rule. Modules are pure: they never touch the disk and return a new list.
/// Implement them as immutable records, so that a settings change is a new instance.
/// </summary>
/// <remarks>
/// Two families: list modules (source, filter, sort) change which files are included and their rank;
/// name modules (<see cref="INameModule"/>) change the names.
/// </remarks>
public interface IModule
{
    IReadOnlyList<Item> Apply(IReadOnlyList<Item> input);
}

/// <summary>
/// A module that only changes names: it returns one item per input item, in the same order.
/// A step running a name module can skip some files (<see cref="Step.Skipped"/>).
/// </summary>
public interface INameModule : IModule;
