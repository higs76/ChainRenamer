using ChainRenamer.App.Resources;
using ChainRenamer.Engine;
using ChainRenamer.Engine.Modules;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChainRenamer.App.ViewModels.Modules;

public partial class CleanSettingsViewModel : ModuleSettingsViewModel
{
    public CleanSettingsViewModel(CleanModule? module = null) =>
        (TrimSpaces, CollapseSpaces, RemoveAccents, RemoveDigits, RemoveSymbols, RemoveEmojis, LeadingDots) = module ?? new CleanModule();

    [ObservableProperty] public partial bool TrimSpaces { get; set; }
    [ObservableProperty] public partial bool CollapseSpaces { get; set; }
    [ObservableProperty] public partial bool RemoveAccents { get; set; }
    [ObservableProperty] public partial bool RemoveDigits { get; set; }
    [ObservableProperty] public partial bool RemoveSymbols { get; set; }
    [ObservableProperty] public partial bool RemoveEmojis { get; set; }
    [ObservableProperty] public partial bool LeadingDots { get; set; }

    public override string Title => Strings.Module_Clean;
    public override string Description => Strings.Clean_Description;

    public override IModule ToModule() => new CleanModule(TrimSpaces, CollapseSpaces, RemoveAccents, RemoveDigits, RemoveSymbols, RemoveEmojis, LeadingDots);
}
