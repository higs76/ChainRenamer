using System.Globalization;
using System.Text.RegularExpressions;

namespace ChainRenamer.Engine.Modules;

public enum TextCase
{
    /// <summary>everything in lowercase</summary>
    Lower,

    /// <summary>EVERYTHING IN UPPERCASE</summary>
    Upper,

    /// <summary>Each Word Capitalized, but the Small Words</summary>
    Title,

    /// <summary>Only the first word capitalized</summary>
    Sentence,
}

/// <summary>The language whose small words stay in lowercase in a title ("of", "the"; "de", "la").</summary>
public enum SmallWords
{
    None,
    English,
    French,
}

/// <summary>
/// Changes the case of the name, without its extension (the extension has its own module).
/// A word is a run of letters, digits and apostrophes; everything else (spaces, dots, dashes…) separates words.
/// </summary>
/// <param name="Exceptions">Words whose case is imposed, separated by commas: "TV, iPhone".</param>
/// <param name="RomanNumerals">Roman numerals up to XXXIX in uppercase: "Part iii" → "Part III".</param>
/// <param name="KeepAcronyms">
/// In Title and Sentence, a word written in uppercase (DVD, USA) stays so, unless the whole name is in uppercase.
/// </param>
/// <param name="SpaceBeforeCapitals">Separates glued words first: "MyHoliday" → "My Holiday".</param>
[Module("Case")]
public sealed partial record CaseModule(
    TextCase Case = TextCase.Title,
    SmallWords SmallWords = SmallWords.None,
    string Exceptions = "",
    bool RomanNumerals = true,
    bool KeepAcronyms = true,
    bool SpaceBeforeCapitals = false) : INameModule
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    private static readonly HashSet<string> English = new(StringComparer.OrdinalIgnoreCase)
    {
        // Not "up", "off", "so"…: they are often adverbs, capitalized in a title ("Give Up").
        "a", "an", "the", "and", "but", "or", "nor", "for", "as", "at", "by", "in", "of", "on", "per", "to", "via", "vs",
    };

    private static readonly HashSet<string> French = new(StringComparer.OrdinalIgnoreCase)
    {
        "le", "la", "les", "un", "une", "des", "du", "de", "et", "ou", "ni", "à", "au", "aux",
        "en", "sur", "sous", "par", "pour", "avec", "sans", "dans", "chez",
    };

    [GeneratedRegex(@"[\p{L}\p{N}\p{M}'’]+")]
    private static partial Regex Word();

    [GeneratedRegex(@"(?<=\p{Ll})(?=\p{Lu})")]
    private static partial Regex GluedWords();

    // Up to 39: a larger limit would take ordinary words for numbers ("mix", "dix", "civil").
    [GeneratedRegex(@"^(?=.)x{0,3}(ix|iv|v?i{0,3})$", RegexOptions.IgnoreCase)]
    private static partial Regex Roman();

    public IReadOnlyList<Item> Apply(IReadOnlyList<Item> input)
    {
        var exceptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in Exceptions.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)) exceptions.TryAdd(e, e);
        return [.. input.Select(i => i.With(NamePart.Stem, Change(i.Stem, exceptions)))];
    }

    private string Change(string name, Dictionary<string, string> exceptions)
    {
        if (SpaceBeforeCapitals) name = GluedWords().Replace(name, " ");

        bool allUpper = !name.Any(char.IsLower);
        var words = Word().Matches(name);
        int index = 0;

        return Word().Replace(name, match =>
        {
            var word = match.Value;
            bool first = index == 0, last = index == words.Count - 1;
            index++;

            if (exceptions.TryGetValue(word, out var imposed)) return imposed;
            if (Case == TextCase.Lower) return word.ToLower(Culture);
            if (Case == TextCase.Upper) return word.ToUpper(Culture);
            if (RomanNumerals && Roman().IsMatch(word)) return word.ToUpper(Culture);
            if (KeepAcronyms && !allUpper && word.Length > 1 && word.All(c => !char.IsLetter(c) || char.IsUpper(c)) && word.Any(char.IsLetter)) return word;

            var lower = word.ToLower(Culture);
            if (Case == TextCase.Sentence) return first ? Capitalize(lower, first) : lower;
            return !first && !last && IsSmall(lower) ? lower : Capitalize(lower, first);
        });
    }

    private bool IsSmall(string word) => SmallWords switch
    {
        SmallWords.English => English.Contains(word),
        SmallWords.French => French.Contains(word),
        _ => false,
    };

    /// <summary>French elision: "l'épisode" → "l'Épisode"; at the start of the title, "L'Épisode".</summary>
    private static string? Elided(string word) => word.Length > 2 && word[1] is '\'' or '’' ? word[..2] : null;

    private string Capitalize(string word, bool first) =>
        SmallWords == SmallWords.French && Elided(word) is { } article
            ? (first ? Upper(article) : article) + Upper(word[2..])
            : Upper(word);

    private static string Upper(string word) => word.Length == 0 ? word : char.ToUpper(word[0], Culture) + word[1..];
}
