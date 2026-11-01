using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ChainRenamer.Engine.Modules;

/// <summary>
/// Tidies the name, without its extension, by check boxes. Often the last step of a chain:
/// what the other steps leave behind (double spaces, a space before the extension) goes away.
/// </summary>
/// <param name="TrimSpaces">No space at the start or at the end of the name.</param>
/// <param name="CollapseSpaces">Several spaces in a row become one.</param>
/// <param name="RemoveAccents">é → e, ç → c, œ → oe, ß → ss.</param>
/// <param name="RemoveSymbols">© ® ™ € $ + = and the like (emojis too).</param>
/// <param name="LeadingDots">Dots at the start of the name (hidden files on Linux and macOS).</param>
[Module("Clean")]
public sealed partial record CleanModule(
    bool TrimSpaces = true,
    bool CollapseSpaces = true,
    bool RemoveAccents = false,
    bool RemoveDigits = false,
    bool RemoveSymbols = false,
    bool RemoveEmojis = false,
    bool LeadingDots = false) : INameModule
{
    [GeneratedRegex(@"[  ]{2,}")]
    private static partial Regex Spaces();

    // Letters that are not a letter plus an accent, so that Unicode cannot take them apart.
    private static readonly Dictionary<char, string> Ligatures = new()
    {
        ['œ'] = "oe", ['Œ'] = "OE", ['æ'] = "ae", ['Æ'] = "AE", ['ß'] = "ss",
        ['ø'] = "o", ['Ø'] = "O", ['đ'] = "d", ['Đ'] = "D", ['ł'] = "l", ['Ł'] = "L",
    };

    public IReadOnlyList<Item> Apply(IReadOnlyList<Item> input) => [.. input.Select(Clean)];

    private Item Clean(Item item)
    {
        // On the whole name: ".config" has no stem, only an extension.
        if (LeadingDots) item = item.With(NamePart.Whole, item.Name.TrimStart('.'));
        return item.With(NamePart.Stem, Clean(item.Stem));
    }

    private string Clean(string name)
    {
        if (RemoveEmojis || RemoveSymbols) name = Keep(name, r => !(RemoveSymbols && IsSymbol(r)) && !(RemoveEmojis && IsEmoji(r)));
        if (RemoveDigits) name = Keep(name, r => !Rune.IsDigit(r));
        if (RemoveAccents) name = WithoutAccents(name);
        if (CollapseSpaces) name = Spaces().Replace(name, " ");
        if (TrimSpaces) name = name.Trim();
        return name;
    }

    private static string Keep(string text, Func<Rune, bool> keep) =>
        string.Concat(text.EnumerateRunes().Where(keep).Select(r => r.ToString()));

    private static bool IsSymbol(Rune r) => Rune.GetUnicodeCategory(r)
        is UnicodeCategory.MathSymbol or UnicodeCategory.CurrencySymbol or UnicodeCategory.ModifierSymbol or UnicodeCategory.OtherSymbol
        || IsEmoji(r);

    // The emoji blocks, and the invisible characters that glue emojis together (joiner, variation selector, keycap, tags).
    private static bool IsEmoji(Rune r) => r.Value is (>= 0x1F000 and <= 0x1FAFF) or (>= 0x2600 and <= 0x27BF) or (>= 0x2B00 and <= 0x2BFF)
        or 0x200D or 0xFE0F or 0x20E3 or (>= 0xE0020 and <= 0xE007F);

    private static string WithoutAccents(string text)
    {
        // "é" decomposed is "e" followed by an accent: the accents are dropped, then the rest recomposed.
        var letters = text.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .Select(c => Ligatures.GetValueOrDefault(c, c.ToString()));
        return string.Concat(letters).Normalize(NormalizationForm.FormC);
    }
}
