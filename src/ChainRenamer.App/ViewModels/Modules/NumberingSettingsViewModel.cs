using ChainRenamer.App.Resources;
using ChainRenamer.Engine;
using ChainRenamer.Engine.Modules;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChainRenamer.App.ViewModels.Modules;

public partial class NumberingSettingsViewModel : ModuleSettingsViewModel
{
    public NumberingSettingsViewModel(NumberingModule? module = null) =>
        (Start, Step, Digits, Style, Where, Position, Anchor, Separator, ResetPerFolder, ResetOn) = module ?? new NumberingModule();

    [ObservableProperty] public partial int Start { get; set; }
    [ObservableProperty] public partial int Step { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDigits))]
    public partial NumberStyle Style { get; set; }

    [ObservableProperty] public partial int Digits { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPosition), nameof(IsAnchor), nameof(HasSeparator))]
    public partial InsertAt Where { get; set; }

    [ObservableProperty] public partial int Position { get; set; }
    [ObservableProperty] public partial string Anchor { get; set; }
    [ObservableProperty] public partial string Separator { get; set; }
    [ObservableProperty] public partial bool ResetPerFolder { get; set; }
    [ObservableProperty] public partial string ResetOn { get; set; }

    public bool IsDigits => Style == NumberStyle.Digits;
    public bool IsPosition => Where == InsertAt.Position;
    public bool IsAnchor => Where is InsertAt.Before or InsertAt.After;
    public bool HasSeparator => Where is InsertAt.Start or InsertAt.End;

    public IReadOnlyList<Choice> Styles { get; } =
    [
        new(NumberStyle.Digits, Strings.NumberStyle_Digits),
        new(NumberStyle.LowerLetters, Strings.NumberStyle_LowerLetters),
        new(NumberStyle.UpperLetters, Strings.NumberStyle_UpperLetters),
        new(NumberStyle.UpperRoman, Strings.NumberStyle_UpperRoman),
        new(NumberStyle.LowerRoman, Strings.NumberStyle_LowerRoman),
    ];

    public IReadOnlyList<Choice> Places { get; } = InsertSettingsViewModel.Places();

    public override string Title => Strings.Module_Numbering;
    public override string Description => Strings.Numbering_Description;

    public override IModule ToModule() => new NumberingModule(Start, Step, Digits, Style, Where, Position, Anchor, Separator, ResetPerFolder, ResetOn);
}
