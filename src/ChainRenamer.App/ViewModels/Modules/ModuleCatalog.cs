using ChainRenamer.App.Resources;

namespace ChainRenamer.App.ViewModels.Modules;

public sealed record ModuleType(string Title, Func<ModuleSettingsViewModel> Create);

public static class ModuleCatalog
{
    /// <summary>
    /// The modules offered by "+ Add a module". Adding a module to the app:
    /// its settings view model and view, its texts in Strings.resx, and one line here.
    /// </summary>
    public static IReadOnlyList<ModuleType> Addable { get; } =
    [
        new(Strings.Module_Replace, () => new ReplaceSettingsViewModel()),
        new(Strings.Module_RegexReplace, () => new RegexReplaceSettingsViewModel()),
        new(Strings.Module_Sort, () => new SortSettingsViewModel()),
    ];
}
