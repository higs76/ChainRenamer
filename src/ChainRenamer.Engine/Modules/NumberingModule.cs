using System.Globalization;

namespace ChainRenamer.Engine.Modules;

public enum NumberStyle
{
    /// <summary>1, 2, 3, padded with zeros: 01, 02…</summary>
    Digits,

    /// <summary>a, b… z, aa, ab…</summary>
    LowerLetters,

    /// <summary>A, B… Z, AA, AB…</summary>
    UpperLetters,

    /// <summary>I, II, III, IV…</summary>
    UpperRoman,

    /// <summary>i, ii, iii, iv…</summary>
    LowerRoman,
}

/// <summary>
/// Numbers the files in their order (put a Sort step before), and inserts the number in the name, without its extension.
/// </summary>
/// <param name="Digits">The minimum number of digits: 3 gives 001. Letters and Roman numerals are not padded.</param>
/// <param name="Separator">Between the number and the name, when the number goes at the start or at the end.</param>
/// <param name="ResetPerFolder">Numbering starts again in each folder (with the source's subfolders).</param>
/// <param name="ResetOn">A variable (<c>season</c> or <c>{season}</c>) or a template (<c>{date:yyyy}</c>): one counter per value.</param>
[Module("Numbering")]
public sealed record NumberingModule(
    int Start = 1,
    int Step = 1,
    int Digits = 2,
    NumberStyle Style = NumberStyle.Digits,
    InsertAt Where = InsertAt.Start,
    int Position = 0,
    string Anchor = "",
    string Separator = " - ",
    bool ResetPerFolder = false,
    string ResetOn = "") : INameModule
{
    public IReadOnlyList<Item> Apply(IReadOnlyList<Item> input)
    {
        var place = new InsertModule(Where: Where, Position: Position, Anchor: Anchor);
        var next = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);     // the next number of each counter
        // "season" or "{season}": one counter per value of the variable; a template such as "{date:yyyy}" works too.
        var resetOn = ResetOn.Trim() is { Length: > 0 } r && !Variables.In(r) ? "{" + r + "}" : ResetOn.Trim();
        return [.. input.Select((item, k) =>
        {
            // A single counter when numbering does not start again.
            var counter = (ResetPerFolder ? item.Folder : "") + "|" + Variables.Expand(resetOn, item, k + 1);
            int n = next.GetValueOrDefault(counter, Start);
            next[counter] = n + Step;

            var text = Where switch
            {
                InsertAt.Start => Format(n) + Separator,
                InsertAt.End => Separator + Format(n),
                _ => Format(n),
            };
            return item.With(NamePart.Stem, place.Insert(item.Stem, text));
        })];
    }

    private string Format(int n) => Style switch
    {
        NumberStyle.Digits => n.ToString("D" + Math.Clamp(Digits, 1, 10), CultureInfo.InvariantCulture),
        _ when n < 1 => n.ToString(CultureInfo.InvariantCulture),      // no letter nor Roman numeral for 0
        NumberStyle.LowerLetters => Letters(n),
        NumberStyle.UpperLetters => Letters(n).ToUpperInvariant(),
        NumberStyle.UpperRoman => Roman(n),
        _ => Roman(n).ToLowerInvariant(),
    };

    // As the columns of a spreadsheet: z is followed by aa.
    private static string Letters(int n) => n <= 26 ? ((char)('a' + n - 1)).ToString() : Letters((n - 1) / 26) + Letters((n - 1) % 26 + 1);

    private static readonly (int Value, string Symbol)[] Numerals =
        [(1000, "M"), (900, "CM"), (500, "D"), (400, "CD"), (100, "C"), (90, "XC"), (50, "L"), (40, "XL"), (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I")];

    private static string Roman(int n)
    {
        if (n >= 4000) return n.ToString(CultureInfo.InvariantCulture);
        var roman = "";
        foreach (var (value, symbol) in Numerals)
            for (; n >= value; n -= value) roman += symbol;
        return roman;
    }
}
