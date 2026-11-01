using System.Globalization;
using System.Text.RegularExpressions;

namespace ChainRenamer.Engine.Modules;

/// <summary>The order of day, month and year in the dates of the names.</summary>
public enum DateOrder
{
    DayMonthYear,
    MonthDayYear,
    YearMonthDay,
}

/// <summary>
/// Finds the dates of the name and writes them in another format: <c>17.05.2024</c> → <c>2024-05-17</c>, which sorts well.
/// A date is three numbers with the same separator (<c>.</c>, <c>-</c>, <c>_</c> or a space) or eight digits in a row
/// (<c>20240517</c>); the year has 2 or 4 digits. What is not a real date (<c>31.02.2024</c>) is left as it is.
/// </summary>
/// <param name="Format">A .NET date format; month and day names follow the language of Windows.</param>
[Module("Dates")]
public sealed partial record DatesModule(DateOrder Order = DateOrder.DayMonthYear, string Format = "yyyy-MM-dd") : INameModule
{
    [GeneratedRegex(@"(?<![0-9])(?:(?<a>[0-9]{1,4})(?<sep>[.\-_ ])(?<b>[0-9]{1,2})\k<sep>(?<c>[0-9]{1,4})|(?<compact>[0-9]{8}))(?![0-9])")]
    private static partial Regex Date();

    public IReadOnlyList<Item> Apply(IReadOnlyList<Item> input) =>
        Format.Length == 0 ? input : [.. input.Select(i => i.With(NamePart.Stem, Date().Replace(i.Stem, m => Read(m) is { } date ? date.ToString(Format, CultureInfo.CurrentCulture) : m.Value)))];

    private DateTime? Read(Match m)
    {
        // The three numbers in the order of the name.
        var (a, b, c) = m.Groups["compact"].Value is { Length: 8 } d
            ? Order == DateOrder.YearMonthDay ? (d[..4], d[4..6], d[6..]) : (d[..2], d[2..4], d[4..])
            : (m.Groups["a"].Value, m.Groups["b"].Value, m.Groups["c"].Value);
        var (day, month, year) = Order switch
        {
            DateOrder.DayMonthYear => (a, b, c),
            DateOrder.MonthDayYear => (b, a, c),
            _ => (c, b, a),
        };
        if (day.Length > 2 || year.Length is not (2 or 4)) return null;

        int y = int.Parse(year), mo = int.Parse(month), da = int.Parse(day);
        if (year.Length == 2) y = CultureInfo.InvariantCulture.Calendar.ToFourDigitYear(y);     // 24 → 2024, 85 → 1985
        return mo is >= 1 and <= 12 && y >= 1 && da >= 1 && da <= DateTime.DaysInMonth(y, mo) ? new DateTime(y, mo, da) : null;
    }
}
