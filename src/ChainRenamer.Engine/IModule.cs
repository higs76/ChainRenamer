namespace ChainRenamer.Engine;

/// <summary>
/// A renaming rule. Modules are pure: they never touch the disk and return a new list.
/// Implement them as immutable records, so that a settings change is a new instance.
/// </summary>
public interface IModule
{
    IReadOnlyList<Item> Apply(IReadOnlyList<Item> input);
}
