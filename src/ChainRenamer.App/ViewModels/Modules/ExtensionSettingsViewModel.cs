using ChainRenamer.App.Resources;
using ChainRenamer.Engine;
using ChainRenamer.Engine.Modules;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChainRenamer.App.ViewModels.Modules;

public partial class ExtensionSettingsViewModel : ModuleSettingsViewModel
{
    public ExtensionSettingsViewModel(ExtensionModule? module = null) => (Action, Text) = module ?? new ExtensionModule();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasText))]
    public partial ExtensionAction Action { get; set; }

    [ObservableProperty] public partial string Text { get; set; }

    public bool HasText => Action is ExtensionAction.Set or ExtensionAction.Add;

    public IReadOnlyList<Choice> Choices { get; } =
    [
        new(ExtensionAction.Lower, Strings.ExtensionAction_Lower),
        new(ExtensionAction.Upper, Strings.ExtensionAction_Upper),
        new(ExtensionAction.Title, Strings.ExtensionAction_Title),
        new(ExtensionAction.Set, Strings.ExtensionAction_Set),
        new(ExtensionAction.Add, Strings.ExtensionAction_Add),
        new(ExtensionAction.Remove, Strings.ExtensionAction_Remove),
    ];

    public override string Title => Strings.Module_Extension;
    public override string Description => Strings.Extension_Description;

    public override IModule ToModule() => new ExtensionModule(Action, Text);
}
