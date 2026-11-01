using ChainRenamer.App.Resources;
using ChainRenamer.Engine;
using ChainRenamer.Engine.Modules;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChainRenamer.App.ViewModels.Modules;

public partial class FilterSettingsViewModel : ModuleSettingsViewModel
{
    public FilterSettingsViewModel(FilterModule? module = null)
    {
        (On, Test, Text, MatchCase, var min, var max, Unit, var from, var to, Exclude) = module ?? new FilterModule();
        // The controls use their own types: decimal for NumericUpDown, DateTime for CalendarDatePicker.
        (Min, Max) = ((decimal?)min, (decimal?)max);
        (FromDate, ToDate) = (from?.ToDateTime(TimeOnly.MinValue), to?.ToDateTime(TimeOnly.MinValue));
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsName), nameof(IsExtension), nameof(IsBounds), nameof(IsSize), nameof(IsLength), nameof(IsDate), nameof(IsRange))]
    public partial FilterOn On { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWildcard))]
    public partial TextTest Test { get; set; }

    [ObservableProperty] public partial string Text { get; set; }
    [ObservableProperty] public partial bool MatchCase { get; set; }
    [ObservableProperty] public partial decimal? Min { get; set; }
    [ObservableProperty] public partial decimal? Max { get; set; }
    [ObservableProperty] public partial SizeUnit Unit { get; set; }
    [ObservableProperty] public partial DateTime? FromDate { get; set; }
    [ObservableProperty] public partial DateTime? ToDate { get; set; }
    [ObservableProperty] public partial bool Exclude { get; set; }

    // Which fields the panel shows.
    public bool IsName => On == FilterOn.Name;
    public bool IsWildcard => IsName && Test == TextTest.Wildcard;
    public bool IsExtension => On == FilterOn.Extension;
    public bool IsBounds => On is FilterOn.Size or FilterOn.Length;
    public bool IsSize => On == FilterOn.Size;
    public bool IsLength => On == FilterOn.Length;
    public bool IsDate => On == FilterOn.Modified;
    public bool IsRange => IsBounds || IsDate;

    public IReadOnlyList<Choice> Modes { get; } =
    [
        new(false, Strings.FilterMode_Keep),
        new(true, Strings.FilterMode_Remove),
    ];

    public IReadOnlyList<Choice> Criteria { get; } =
    [
        new(FilterOn.Name, Strings.FilterOn_Name),
        new(FilterOn.Extension, Strings.FilterOn_Extension),
        new(FilterOn.Size, Strings.FilterOn_Size),
        new(FilterOn.Modified, Strings.FilterOn_Modified),
        new(FilterOn.Length, Strings.FilterOn_Length),
    ];

    public IReadOnlyList<Choice> Tests { get; } =
    [
        new(TextTest.Contains, Strings.TextTest_Contains),
        new(TextTest.StartsWith, Strings.TextTest_StartsWith),
        new(TextTest.EndsWith, Strings.TextTest_EndsWith),
        new(TextTest.Wildcard, Strings.TextTest_Wildcard),
        new(TextTest.Regex, Strings.TextTest_Regex),
    ];

    public IReadOnlyList<Choice> Units { get; } =
    [
        new(SizeUnit.Bytes, Strings.SizeUnit_Bytes),
        new(SizeUnit.KB, Strings.SizeUnit_KB),
        new(SizeUnit.MB, Strings.SizeUnit_MB),
        new(SizeUnit.GB, Strings.SizeUnit_GB),
    ];

    public override string Title => Strings.Module_Filter;
    public override string Description => Strings.Filter_Description;

    public override IModule ToModule() => new FilterModule(On, Test, Text, MatchCase, (double?)Min, (double?)Max, Unit,
        FromDate is { } from ? DateOnly.FromDateTime(from) : null, ToDate is { } to ? DateOnly.FromDateTime(to) : null, Exclude);
}
