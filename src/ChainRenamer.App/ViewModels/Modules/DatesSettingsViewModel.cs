using System.Globalization;
using ChainRenamer.App.Resources;
using ChainRenamer.Engine;
using ChainRenamer.Engine.Modules;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChainRenamer.App.ViewModels.Modules;

public partial class DatesSettingsViewModel : ModuleSettingsViewModel
{
    public DatesSettingsViewModel(DatesModule? module = null) => (Order, Format) = module ?? new DatesModule();

    [ObservableProperty] public partial DateOrder Order { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Example))]
    public partial string Format { get; set; }

    /// <summary>What the format gives for May 17th, 2024, or a warning while it is not valid.</summary>
    public string Example
    {
        get
        {
            try { return string.Format(Strings.Dates_Example, new DateTime(2024, 5, 17).ToString(Format, CultureInfo.CurrentCulture)); }
            catch (FormatException) { return Strings.Dates_InvalidFormat; }
        }
    }

    public IReadOnlyList<Choice> Orders { get; } =
    [
        new(DateOrder.DayMonthYear, Strings.DateOrder_DayMonthYear),
        new(DateOrder.MonthDayYear, Strings.DateOrder_MonthDayYear),
        new(DateOrder.YearMonthDay, Strings.DateOrder_YearMonthDay),
    ];

    public override string Title => Strings.Module_Dates;
    public override string Description => Strings.Dates_Description;

    public override IModule ToModule() => new DatesModule(Order, Format);
}
