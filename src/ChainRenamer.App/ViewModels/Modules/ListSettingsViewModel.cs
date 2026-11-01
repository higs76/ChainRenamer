using ChainRenamer.App.Resources;
using ChainRenamer.Engine;
using ChainRenamer.Engine.Modules;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChainRenamer.App.ViewModels.Modules;

public partial class ListSettingsViewModel : ModuleSettingsViewModel
{
    public ListSettingsViewModel(ListModule? module = null) =>
        (Lines, Mode, Where, Position, Anchor, MatchCase) = module ?? new ListModule();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Count))]
    public partial string Lines { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInsert), nameof(IsPosition), nameof(IsAnchor), nameof(Hint))]
    public partial ListMode Mode { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPosition), nameof(IsAnchor))]
    public partial InsertAt Where { get; set; }

    [ObservableProperty] public partial int Position { get; set; }
    [ObservableProperty] public partial string Anchor { get; set; }
    [ObservableProperty] public partial bool MatchCase { get; set; }

    public bool IsInsert => Mode == ListMode.Insert;
    public bool IsPosition => IsInsert && Where == InsertAt.Position;
    public bool IsAnchor => IsInsert && Where is InsertAt.Before or InsertAt.After;

    /// <summary>"12 lines": to compare with the number of files at a glance.</summary>
    public string Count => string.Format(Strings.List_Count, ((ListModule)ToModule()).Items().Count);

    public string Hint => Mode == ListMode.Pairs ? Strings.List_PairsHint : Strings.List_Hint;

    public IReadOnlyList<Choice> Modes { get; } =
    [
        new(ListMode.ReplaceName, Strings.ListMode_ReplaceName),
        new(ListMode.Insert, Strings.ListMode_Insert),
        new(ListMode.Pairs, Strings.ListMode_Pairs),
    ];

    public IReadOnlyList<Choice> Places { get; } = InsertSettingsViewModel.Places();

    public override string Title => Strings.Module_List;
    public override string Description => Strings.List_Description;

    public override IModule ToModule() => new ListModule(Lines, Mode, Where, Position, Anchor, MatchCase);
}
