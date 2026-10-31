using ChainRenamer.App.Resources;
using ChainRenamer.Engine;
using ChainRenamer.Engine.Modules;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChainRenamer.App.ViewModels.Modules;

public sealed record OccurrenceChoice(Occurrence Value, string Label);

public partial class ReplaceSettingsViewModel : ModuleSettingsViewModel
{
    [ObservableProperty] public partial string Find { get; set; } = "";
    [ObservableProperty] public partial string ReplaceWith { get; set; } = "";
    [ObservableProperty] public partial bool MatchCase { get; set; }
    [ObservableProperty] public partial Occurrence Occurrence { get; set; } = Occurrence.All;

    public IReadOnlyList<OccurrenceChoice> Occurrences { get; } =
    [
        new(Occurrence.All, Strings.Occurrence_All),
        new(Occurrence.First, Strings.Occurrence_First),
        new(Occurrence.Last, Strings.Occurrence_Last),
    ];

    public override string Title => Strings.Module_Replace;
    public override string Description => Strings.Replace_Description;

    public override IModule ToModule() => new ReplaceModule(Find, ReplaceWith, MatchCase, Occurrence);
}
