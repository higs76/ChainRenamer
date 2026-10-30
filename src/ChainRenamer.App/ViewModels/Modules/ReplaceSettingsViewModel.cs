using ChainRenamer.App.Resources;
using ChainRenamer.Engine;
using ChainRenamer.Engine.Modules;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChainRenamer.App.ViewModels.Modules;

public sealed record PartChoice(NamePart Value, string Label);

public partial class ReplaceSettingsViewModel : ModuleSettingsViewModel
{
    [ObservableProperty] public partial string Find { get; set; } = "";
    [ObservableProperty] public partial string ReplaceWith { get; set; } = "";
    [ObservableProperty] public partial bool UseRegex { get; set; }
    [ObservableProperty] public partial bool MatchCase { get; set; }
    [ObservableProperty] public partial NamePart Part { get; set; } = NamePart.Stem;

    public IReadOnlyList<PartChoice> Parts { get; } =
    [
        new(NamePart.Stem, Strings.Part_Stem),
        new(NamePart.Extension, Strings.Part_Extension),
        new(NamePart.Whole, Strings.Part_Whole),
    ];

    public override string Title => Strings.Module_Replace;

    public override IModule ToModule() => new ReplaceModule(Find, ReplaceWith, UseRegex, MatchCase, Part);
}
