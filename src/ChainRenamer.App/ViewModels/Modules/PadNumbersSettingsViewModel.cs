using ChainRenamer.App.Resources;
using ChainRenamer.Engine;
using ChainRenamer.Engine.Modules;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChainRenamer.App.ViewModels.Modules;

public partial class PadNumbersSettingsViewModel : ModuleSettingsViewModel
{
    public PadNumbersSettingsViewModel(PadNumbersModule? module = null)
    {
        (var digits, Which, TrimZeros) = module ?? new PadNumbersModule();
        // The module's 0 ("as many as the longest number") is a check box here.
        (Automatic, Digits) = digits > 0 ? (false, digits) : (true, 2);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFixed))]
    public partial bool Automatic { get; set; }

    [ObservableProperty] public partial int Digits { get; set; }
    [ObservableProperty] public partial WhichNumbers Which { get; set; }
    [ObservableProperty] public partial bool TrimZeros { get; set; }

    public bool IsFixed => !Automatic;

    public IReadOnlyList<Choice> Choices { get; } =
    [
        new(WhichNumbers.All, Strings.WhichNumbers_All),
        new(WhichNumbers.First, Strings.WhichNumbers_First),
        new(WhichNumbers.Last, Strings.WhichNumbers_Last),
    ];

    public override string Title => Strings.Module_PadNumbers;
    public override string Description => Strings.PadNumbers_Description;

    public override IModule ToModule() => new PadNumbersModule(Automatic ? 0 : Digits, Which, TrimZeros);
}
