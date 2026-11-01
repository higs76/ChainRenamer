using ChainRenamer.Engine.Modules;

namespace ChainRenamer.Engine.Tests.Modules;

public class DatesModuleTests
{
    private static string Change(DatesModule module, string name) => module.Apply(TestItems.Of(name))[0].Name;

    [Theory]
    [InlineData("Holiday 17.05.2024.jpg", "Holiday 2024-05-17.jpg")]
    [InlineData("Holiday 17-05-2024.jpg", "Holiday 2024-05-17.jpg")]
    [InlineData("Holiday 17_5_24.jpg", "Holiday 2024-05-17.jpg")]
    [InlineData("Scan 01 04 1985.pdf", "Scan 1985-04-01.pdf")]
    [InlineData("Report 17052024.pdf", "Report 2024-05-17.pdf")]
    [InlineData("From 01.04.2024 to 15.04.2024.txt", "From 2024-04-01 to 2024-04-15.txt")]
    public void Rewrites_day_month_year_dates(string name, string expected) =>
        Assert.Equal(expected, Change(new(), name));

    [Fact]
    public void Reads_the_other_orders()
    {
        Assert.Equal("Invoice 2024-05-17.pdf", Change(new(DateOrder.MonthDayYear), "Invoice 05.17.2024.pdf"));
        Assert.Equal("IMG 2024-05-17 1832.jpg", Change(new(DateOrder.YearMonthDay), "IMG 20240517 1832.jpg"));
        Assert.Equal("Log 2024-05-17.txt", Change(new(DateOrder.YearMonthDay), "Log 2024.5.17.txt"));
    }

    [Fact]
    public void Writes_any_format()
    {
        Assert.Equal("Holiday 20240517.jpg", Change(new(Format: "yyyyMMdd"), "Holiday 17.05.2024.jpg"));
        Assert.Equal("Holiday 2024.jpg", Change(new(Format: "yyyy"), "Holiday 17.05.2024.jpg"));
    }

    [Theory]
    [InlineData("Day 31.02.2024.txt")]      // no February 31st
    [InlineData("Version 1.2.3.txt")]       // a one-digit year
    [InlineData("Speedy 1x03.mkv")]
    [InlineData("Code 123456789.txt")]      // nine digits: not a date
    [InlineData("Mixed 17.05-2024.txt")]    // two different separators
    public void Leaves_what_is_not_a_date(string name) =>
        Assert.Equal(name, Change(new(), name));

    [Fact]
    public void Never_touches_the_extension()
    {
        Assert.Equal("a 2024-05-17.20240517", Change(new(), "a 17.05.2024.20240517"));
    }

    [Fact]
    public void A_wrong_format_throws_so_that_the_step_reports_it()
    {
        Assert.ThrowsAny<FormatException>(() => Change(new(Format: "%"), "17.05.2024.txt"));
    }
}
