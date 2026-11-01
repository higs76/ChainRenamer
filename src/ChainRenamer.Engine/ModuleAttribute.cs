using System.Reflection;

namespace ChainRenamer.Engine;

/// <summary>
/// The stable name of a module type, written in .chain files and used as the key of its help page.
/// It must never change once published: renaming the class is fine, renaming this breaks saved chains.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ModuleAttribute(string name) : Attribute
{
    public string Name { get; } = name;

    /// <summary>The stable name of a module, or null if its type has none.</summary>
    public static string? NameOf(IModule module) =>
        module.GetType().GetCustomAttribute<ModuleAttribute>()?.Name;
}
