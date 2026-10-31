using ChainRenamer.App.Resources;
using ChainRenamer.Engine;
using ChainRenamer.Engine.Modules;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChainRenamer.App.ViewModels.Modules;

public partial class RegexReplaceSettingsViewModel : ModuleSettingsViewModel
{
    [ObservableProperty] public partial string Pattern { get; set; } = "";
    [ObservableProperty] public partial string ReplaceWith { get; set; } = "";
    [ObservableProperty] public partial bool MatchCase { get; set; }

    public override string Title => Strings.Module_RegexReplace;
    public override string Description => Strings.Regex_Description;

    public override IModule ToModule() => new RegexReplaceModule(Pattern, ReplaceWith, MatchCase);
}
