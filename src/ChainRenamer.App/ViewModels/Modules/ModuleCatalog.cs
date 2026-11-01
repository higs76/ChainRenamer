using ChainRenamer.App.Resources;
using ChainRenamer.Engine;
using ChainRenamer.Engine.Modules;

namespace ChainRenamer.App.ViewModels.Modules;

/// <summary>A module the user can add, and how to build its settings panel, empty or from a loaded module.</summary>
public sealed record ModuleType(string Title, Type Module, Func<IModule?, ModuleSettingsViewModel> Create)
{
    public static ModuleType Of<T>(string title, Func<T?, ModuleSettingsViewModel> create) where T : class, IModule =>
        new(title, typeof(T), module => create(module as T));
}

public static class ModuleCatalog
{
    /// <summary>
    /// The modules offered by "+ Add a module". Adding a module to the app:
    /// its settings view model and view, its texts in Strings.resx, and one line here.
    /// </summary>
    public static IReadOnlyList<ModuleType> Addable { get; } =
    [
        // In a logical order: list modules first, then name modules from the simplest.
        ModuleType.Of<SortModule>(Strings.Module_Sort, m => new SortSettingsViewModel(m)),
        ModuleType.Of<FilterModule>(Strings.Module_Filter, m => new FilterSettingsViewModel(m)),
        ModuleType.Of<ReplaceModule>(Strings.Module_Replace, m => new ReplaceSettingsViewModel(m)),
        ModuleType.Of<RegexReplaceModule>(Strings.Module_RegexReplace, m => new RegexReplaceSettingsViewModel(m)),
        ModuleType.Of<ReplaceTableModule>(Strings.Module_ReplaceTable, m => new ReplaceTableSettingsViewModel(m)),
        ModuleType.Of<TemplateModule>(Strings.Module_Template, m => new TemplateSettingsViewModel(m)),
        ModuleType.Of<CaseModule>(Strings.Module_Case, m => new CaseSettingsViewModel(m)),
        ModuleType.Of<RemoveModule>(Strings.Module_Remove, m => new RemoveSettingsViewModel(m)),
        ModuleType.Of<InsertModule>(Strings.Module_Insert, m => new InsertSettingsViewModel(m)),
        ModuleType.Of<NumberingModule>(Strings.Module_Numbering, m => new NumberingSettingsViewModel(m)),
        ModuleType.Of<PadNumbersModule>(Strings.Module_PadNumbers, m => new PadNumbersSettingsViewModel(m)),
        ModuleType.Of<CleanModule>(Strings.Module_Clean, m => new CleanSettingsViewModel(m)),
        ModuleType.Of<ExtensionModule>(Strings.Module_Extension, m => new ExtensionSettingsViewModel(m)),
    ];

    /// <summary>The settings panel showing a module read from a chain, or null if the app has none for it.</summary>
    public static ModuleSettingsViewModel? SettingsOf(IModule module) =>
        Addable.FirstOrDefault(t => t.Module == module.GetType())?.Create(module);
}
