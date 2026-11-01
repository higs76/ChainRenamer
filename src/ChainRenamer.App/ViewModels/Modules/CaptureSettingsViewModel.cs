using ChainRenamer.App.Resources;
using ChainRenamer.Engine;
using ChainRenamer.Engine.Modules;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChainRenamer.App.ViewModels.Modules;

public partial class CaptureSettingsViewModel : ModuleSettingsViewModel
{
    public CaptureSettingsViewModel(CaptureModule? module = null) => (Pattern, MatchCase) = module ?? new CaptureModule();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Captured))]
    public partial string Pattern { get; set; }

    [ObservableProperty] public partial bool MatchCase { get; set; }

    /// <summary>The variables the expression creates, as the following steps use them: "{season}, {episode}".</summary>
    public string Captured => VariableNames() is { Count: > 0 } names
        ? string.Format(Strings.Capture_Variables, string.Join(", ", names.Select(n => "{" + n + "}")))
        : Strings.Capture_NoVariable;

    public IReadOnlyList<string> VariableNames() => ((CaptureModule)ToModule()).VariableNames();

    public override string Title => Strings.Module_Capture;
    public override string Description => Strings.Capture_Description;

    public override IModule ToModule() => new CaptureModule(Pattern, MatchCase);
}
