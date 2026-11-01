using ChainRenamer.App.Resources;
using ChainRenamer.Engine;
using ChainRenamer.Engine.Modules;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChainRenamer.App.ViewModels.Modules;

public partial class RemoveSettingsViewModel : ModuleSettingsViewModel
{
    public RemoveSettingsViewModel(RemoveModule? module = null) =>
        (What, Count, From, To, Text, EndText, IncludeText, LastOccurrence, MatchCase) = module ?? new RemoveModule();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCount), nameof(IsRange), nameof(IsText), nameof(IsAround), nameof(IsBetween))]
    public partial RemoveWhat What { get; set; }

    [ObservableProperty] public partial int Count { get; set; }
    [ObservableProperty] public partial int From { get; set; }
    [ObservableProperty] public partial int To { get; set; }
    [ObservableProperty] public partial string Text { get; set; }
    [ObservableProperty] public partial string EndText { get; set; }
    [ObservableProperty] public partial bool IncludeText { get; set; }
    [ObservableProperty] public partial bool LastOccurrence { get; set; }
    [ObservableProperty] public partial bool MatchCase { get; set; }

    // Which fields the panel shows.
    public bool IsCount => What is RemoveWhat.First or RemoveWhat.Last;
    public bool IsRange => What == RemoveWhat.Range;
    public bool IsText => What is RemoveWhat.Before or RemoveWhat.After or RemoveWhat.Between;
    public bool IsAround => What is RemoveWhat.Before or RemoveWhat.After;
    public bool IsBetween => What == RemoveWhat.Between;

    public IReadOnlyList<Choice> Choices { get; } =
    [
        new(RemoveWhat.First, Strings.RemoveWhat_First),
        new(RemoveWhat.Last, Strings.RemoveWhat_Last),
        new(RemoveWhat.Range, Strings.RemoveWhat_Range),
        new(RemoveWhat.Before, Strings.RemoveWhat_Before),
        new(RemoveWhat.After, Strings.RemoveWhat_After),
        new(RemoveWhat.Between, Strings.RemoveWhat_Between),
    ];

    public override string Title => Strings.Module_Remove;
    public override string Description => Strings.Remove_Description;

    public override IModule ToModule() => new RemoveModule(What, Count, From, To, Text, EndText, IncludeText, LastOccurrence, MatchCase);
}
