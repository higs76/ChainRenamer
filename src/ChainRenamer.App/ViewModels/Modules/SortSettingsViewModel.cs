using ChainRenamer.App.Resources;
using ChainRenamer.Engine;
using ChainRenamer.Engine.Modules;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChainRenamer.App.ViewModels.Modules;

public partial class SortSettingsViewModel : ModuleSettingsViewModel
{
    [ObservableProperty] public partial bool Descending { get; set; }

    public override string Title => Strings.Module_Sort;

    public override IModule ToModule() => new SortModule(Descending);
}
