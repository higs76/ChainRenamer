using System.Globalization;
using ChainRenamer.App.Resources;
using ChainRenamer.Engine;
using ChainRenamer.Engine.Modules;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChainRenamer.App.ViewModels.Modules;

public partial class CaseSettingsViewModel : ModuleSettingsViewModel
{
    public CaseSettingsViewModel(CaseModule? module = null)
    {
        // A new step leaves the small words of the interface language in lowercase.
        module ??= new CaseModule(SmallWords: CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "fr" ? SmallWords.French : SmallWords.English);
        (Case, SmallWords, Exceptions, RomanNumerals, KeepAcronyms, SpaceBeforeCapitals) = module;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTitle))]
    public partial TextCase Case { get; set; }

    [ObservableProperty] public partial SmallWords SmallWords { get; set; }
    [ObservableProperty] public partial string Exceptions { get; set; }
    [ObservableProperty] public partial bool RomanNumerals { get; set; }
    [ObservableProperty] public partial bool KeepAcronyms { get; set; }
    [ObservableProperty] public partial bool SpaceBeforeCapitals { get; set; }

    /// <summary>Small words only matter in a title.</summary>
    public bool IsTitle => Case == TextCase.Title;

    public IReadOnlyList<Choice> Cases { get; } =
    [
        new(TextCase.Lower, Strings.Case_Lower),
        new(TextCase.Upper, Strings.Case_Upper),
        new(TextCase.Title, Strings.Case_Title),
        new(TextCase.Sentence, Strings.Case_Sentence),
    ];

    public IReadOnlyList<Choice> Languages { get; } =
    [
        new(SmallWords.None, Strings.SmallWords_None),
        new(SmallWords.English, Strings.SmallWords_English),
        new(SmallWords.French, Strings.SmallWords_French),
    ];

    public override string Title => Strings.Module_Case;
    public override string Description => Strings.Case_Description;

    public override IModule ToModule() => new CaseModule(Case, SmallWords, Exceptions, RomanNumerals, KeepAcronyms, SpaceBeforeCapitals);
}
