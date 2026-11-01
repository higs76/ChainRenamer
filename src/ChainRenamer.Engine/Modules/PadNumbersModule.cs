using System.Text.RegularExpressions;

namespace ChainRenamer.Engine.Modules;

/// <summary>Which numbers of the name a module works on.</summary>
public enum WhichNumbers
{
    All,
    First,
    Last,
}

/// <summary>
/// Pads the numbers of the name with zeros, so that they sort and line up: <c>Episode 5</c> → <c>Episode 05</c>,
/// <c>S1E5</c> → <c>S01E05</c>. A number already longer is kept. The extension is never touched.
/// </summary>
/// <param name="Digits">The number of digits, or 0 for as many as the longest number among all the files.</param>
/// <param name="Which">All the numbers, or only the first or the last one (to leave <c>x265</c> or <c>1080p</c> alone).</param>
/// <param name="TrimZeros">Removes the zeros beyond <paramref name="Digits"/>: <c>007</c> → <c>07</c>.</param>
[Module("PadNumbers")]
public sealed partial record PadNumbersModule(int Digits = 2, WhichNumbers Which = WhichNumbers.All, bool TrimZeros = false) : INameModule
{
    // 0-9 only: \d would also match the digits of other scripts.
    [GeneratedRegex("[0-9]+")]
    private static partial Regex Number();

    public IReadOnlyList<Item> Apply(IReadOnlyList<Item> input)
    {
        int digits = Digits > 0 ? Digits : input.SelectMany(i => Selected(i.Stem)).Select(m => Trim(m.Value).Length).DefaultIfEmpty(1).Max();
        return [.. input.Select(i => i.With(NamePart.Stem, Pad(i.Stem, digits)))];
    }

    private string Pad(string name, int digits)
    {
        var selected = Selected(name).Select(m => m.Index).ToHashSet();
        return Number().Replace(name, m => selected.Contains(m.Index) ? Trim(m.Value).PadLeft(digits, '0') : m.Value);
    }

    private IEnumerable<Match> Selected(string name)
    {
        var numbers = Number().Matches(name);
        return numbers.Count == 0 ? [] : Which switch
        {
            WhichNumbers.First => [numbers[0]],
            WhichNumbers.Last => [numbers[^1]],
            _ => numbers,
        };
    }

    private string Trim(string number) => TrimZeros && number.TrimStart('0') is var trimmed ? (trimmed.Length > 0 ? trimmed : "0") : number;
}
