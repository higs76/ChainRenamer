using ChainRenamer.App.Resources;
using ChainRenamer.Engine;
using ChainRenamer.Engine.Modules;

namespace ChainRenamer.App.ViewModels.Modules;

/// <summary>A module the user can add, its icon in the collapsed chain, and how to build its settings panel.</summary>
public sealed record ModuleType(string Title, string Icon, Type Module, Func<IModule?, ModuleSettingsViewModel> Create)
{
    public static ModuleType Of<T>(string title, string icon, Func<T?, ModuleSettingsViewModel> create) where T : class, IModule =>
        new(title, icon, typeof(T), module => create(module as T));
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
        ModuleType.Of<SortModule>(Strings.Module_Sort, "⇅", m => new SortSettingsViewModel(m)),
        ModuleType.Of<FilterModule>(Strings.Module_Filter, "▽", m => new FilterSettingsViewModel(m)),
        ModuleType.Of<ReplaceModule>(Strings.Module_Replace, "⇄", m => new ReplaceSettingsViewModel(m)),
        ModuleType.Of<RegexReplaceModule>(Strings.Module_RegexReplace, ".*", m => new RegexReplaceSettingsViewModel(m)),
        ModuleType.Of<ReplaceTableModule>(Strings.Module_ReplaceTable, "☰", m => new ReplaceTableSettingsViewModel(m)),
        ModuleType.Of<TemplateModule>(Strings.Module_Template, "{ }", m => new TemplateSettingsViewModel(m)),
        ModuleType.Of<CaptureModule>(Strings.Module_Capture, "( )", m => new CaptureSettingsViewModel(m)),
        ModuleType.Of<ListModule>(Strings.Module_List, "≣", m => new ListSettingsViewModel(m)),
        ModuleType.Of<CaseModule>(Strings.Module_Case, "Aa", m => new CaseSettingsViewModel(m)),
        ModuleType.Of<RemoveModule>(Strings.Module_Remove, "⌫", m => new RemoveSettingsViewModel(m)),
        ModuleType.Of<InsertModule>(Strings.Module_Insert, "+", m => new InsertSettingsViewModel(m)),
        ModuleType.Of<NumberingModule>(Strings.Module_Numbering, "#", m => new NumberingSettingsViewModel(m)),
        ModuleType.Of<PadNumbersModule>(Strings.Module_PadNumbers, "07", m => new PadNumbersSettingsViewModel(m)),
        ModuleType.Of<DatesModule>(Strings.Module_Dates, "31", m => new DatesSettingsViewModel(m)),
        ModuleType.Of<CleanModule>(Strings.Module_Clean, "✧", m => new CleanSettingsViewModel(m)),
        ModuleType.Of<ExtensionModule>(Strings.Module_Extension, ".x", m => new ExtensionSettingsViewModel(m)),
    ];

    /// <summary>The icon of a module in the collapsed chain column.</summary>
    public static string IconOf(IModule module) =>
        module is SourceModule ? "▤" : Addable.FirstOrDefault(t => t.Module == module.GetType())?.Icon ?? "•";

    /// <summary>The settings panel showing a module read from a chain, or null if the app has none for it.</summary>
    public static ModuleSettingsViewModel? SettingsOf(IModule module) =>
        Addable.FirstOrDefault(t => t.Module == module.GetType())?.Create(module);
}
