using System.Text.RegularExpressions;

namespace ChainRenamer.Engine.Modules;

/// <summary>What a filter looks at.</summary>
public enum FilterOn
{
    Name,
    Extension,
    Size,
    Modified,
    Length,
}

/// <summary>How a filter on the name compares it with the text.</summary>
public enum TextTest
{
    Contains,
    StartsWith,
    EndsWith,
    Wildcard,
    Regex,
}

public enum SizeUnit
{
    Bytes,
    KB,
    MB,
    GB,
}

/// <summary>
/// Keeps the files that match one criterion, or, with <see cref="Exclude"/>, removes them.
/// Several criteria: several Filter steps, each one working on what the previous ones kept.
/// An empty filter (no text, no bound) keeps every file.
/// </summary>
/// <param name="Text">
/// Name: the text, a pattern (<c>*.mkv;*.mp4</c>) or a regular expression, tested on the whole current name.
/// Extension: a list such as <c>mkv, mp4</c>, with or without dots, case ignored.
/// </param>
/// <param name="Min">Size (in <paramref name="Unit"/>) or length (in characters, extension included) at least this.</param>
/// <param name="Max">At most this. Swapped with <paramref name="Min"/> if smaller.</param>
/// <param name="From">Modified on this day or later.</param>
/// <param name="To">Modified on this day or earlier.</param>
[Module("Filter")]
public sealed record FilterModule(
    FilterOn On = FilterOn.Name,
    TextTest Test = TextTest.Contains,
    string Text = "",
    bool MatchCase = false,
    double? Min = null,
    double? Max = null,
    SizeUnit Unit = SizeUnit.MB,
    DateOnly? From = null,
    DateOnly? To = null,
    bool Exclude = false) : IModule
{
    // Guards against a pathological pattern freezing the preview.
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1);

    public IReadOnlyList<Item> Apply(IReadOnlyList<Item> input) =>
        Matcher() is { } matches ? [.. input.Where(i => matches(i) != Exclude)] : input;

    /// <summary>The test of one file, or null when the filter has nothing to test.</summary>
    private Func<Item, bool>? Matcher() => On switch
    {
        FilterOn.Name when Text.Length > 0 => NameMatcher(),
        FilterOn.Extension when Extensions() is { Count: > 0 } extensions => i => extensions.Contains(i.Extension.TrimStart('.')),
        FilterOn.Size when Min is not null || Max is not null => i => InRange(i.Size / Math.Pow(1024, (int)Unit)),
        FilterOn.Length when Min is not null || Max is not null => i => InRange(i.Name.Length),
        FilterOn.Modified when From is not null || To is not null => i => DateOnly.FromDateTime(i.Modified) is var day
            && (From is not { } from || day >= from) && (To is not { } to || day <= to),
        _ => null,
    };

    private Func<Item, bool> NameMatcher()
    {
        var comparison = MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        switch (Test)
        {
            case TextTest.StartsWith: return i => i.Name.StartsWith(Text, comparison);
            case TextTest.EndsWith: return i => i.Name.EndsWith(Text, comparison);
            case TextTest.Contains: return i => i.Name.Contains(Text, comparison);
        }

        var options = MatchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
        var regex = new Regex(Test == TextTest.Regex ? Text : WildcardPattern(Text), options, Timeout);
        return i => regex.IsMatch(i.Name);
    }

    // "*.mkv;*.mp4" → ^(.*\.mkv|.*\.mp4)$ : * any text, ? one character, ; between patterns.
    private static string WildcardPattern(string text) => "^(" + string.Join("|", text
        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(p => Regex.Escape(p).Replace(@"\*", ".*").Replace(@"\?", "."))) + ")$";

    private HashSet<string> Extensions() =>
        new(Text.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries).Select(e => e.TrimStart('.')), StringComparer.OrdinalIgnoreCase);

    private bool InRange(double value)
    {
        var (low, high) = Min is { } min && Max is { } max && min > max ? (Max, Min) : (Min, Max);
        return (low is not { } l || value >= l) && (high is not { } h || value <= h);
    }
}
