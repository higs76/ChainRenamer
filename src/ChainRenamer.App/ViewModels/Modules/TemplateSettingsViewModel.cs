using ChainRenamer.App.Resources;
using ChainRenamer.Engine;
using ChainRenamer.Engine.Modules;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChainRenamer.App.ViewModels.Modules;

public partial class TemplateSettingsViewModel : ModuleSettingsViewModel
{
    public TemplateSettingsViewModel(TemplateModule? module = null) => Template = module?.Template ?? "";

    [ObservableProperty] public partial string Template { get; set; }

    /// <summary>
    /// The variables offered by the "Insert a variable" menu. Each label starts with the variable in the language
    /// of the interface ("{dossier} : le dossier du fichier"): that is what the menu inserts.
    /// </summary>
    public IReadOnlyList<Choice> Variables { get; } =
        [.. new[] { Strings.Variable_Name, Strings.Variable_Folder, Strings.Variable_Number, Strings.Variable_Date, Strings.Variable_Size, Strings.Variable_Ext, Strings.Variable_Today }
            .Select(label => new Choice(label[..(label.IndexOf('}') + 1)], label))];

    [RelayCommand]
    private void AddVariable(string variable) => Template += variable;

    public override string Title => Strings.Module_Template;
    public override string Description => Strings.Template_Description;

    public override IModule ToModule() => new TemplateModule(Template);
}
