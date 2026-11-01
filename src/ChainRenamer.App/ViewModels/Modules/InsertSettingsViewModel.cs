using ChainRenamer.App.Resources;
using ChainRenamer.Engine;
using ChainRenamer.Engine.Modules;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChainRenamer.App.ViewModels.Modules;

public partial class InsertSettingsViewModel : ModuleSettingsViewModel
{
    public InsertSettingsViewModel(InsertModule? module = null) =>
        (Text, Where, Position, Anchor, MatchCase) = module ?? new InsertModule();

    [ObservableProperty] public partial string Text { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPosition), nameof(IsAnchor))]
    public partial InsertAt Where { get; set; }

    [ObservableProperty] public partial int Position { get; set; }
    [ObservableProperty] public partial string Anchor { get; set; }
    [ObservableProperty] public partial bool MatchCase { get; set; }

    public bool IsPosition => Where == InsertAt.Position;
    public bool IsAnchor => Where is InsertAt.Before or InsertAt.After;

    /// <summary>The places where a text or a number can go; Numbering uses them too.</summary>
    public static IReadOnlyList<Choice> Places() =>
    [
        new(InsertAt.Start, Strings.InsertAt_Start),
        new(InsertAt.End, Strings.InsertAt_End),
        new(InsertAt.Position, Strings.InsertAt_Position),
        new(InsertAt.Before, Strings.InsertAt_Before),
        new(InsertAt.After, Strings.InsertAt_After),
    ];

    public IReadOnlyList<Choice> Choices { get; } = Places();

    public override string Title => Strings.Module_Insert;
    public override string Description => Strings.Insert_Description;

    public override IModule ToModule() => new InsertModule(Text, Where, Position, Anchor, MatchCase);
}
