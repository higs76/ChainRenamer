using System.Text.RegularExpressions;

namespace ChainRenamer.Engine.Modules;

/// <summary>
/// Captures parts of the name into variables, without changing it: the named groups of a regular expression,
/// <c>(?&lt;season&gt;\d+)x(?&lt;episode&gt;\d+)</c>, become <c>{season}</c> and <c>{episode}</c> for the following steps.
/// A name the expression does not match gets no variable, so that <c>{season}</c> shows as typed in its new name.
/// </summary>
[Module("Capture")]
public sealed record CaptureModule(string Pattern = "", bool MatchCase = false) : INameModule
{
    // Guards against a pathological pattern freezing the preview.
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1);

    public IReadOnlyList<Item> Apply(IReadOnlyList<Item> input)
    {
        if (Pattern.Length == 0) return input;

        var regex = new Regex(Pattern, MatchCase ? RegexOptions.None : RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, Timeout);
        var names = Names(regex);
        return [.. input.Select(item => regex.Match(item.Stem) is { Success: true } match
            ? item with { Variables = item.Variables.SetItems(names.Where(n => match.Groups[n].Success).Select(n => KeyValuePair.Create(n, match.Groups[n].Value))) }
            : item)];
    }

    /// <summary>The variables the expression creates: its named groups. Empty if the expression is not valid yet.</summary>
    public IReadOnlyList<string> VariableNames()
    {
        try { return Pattern.Length == 0 ? [] : Names(new Regex(Pattern)); }
        catch (ArgumentException) { return []; }
    }

    // Numbered groups ("0", "1"…) are not variables.
    private static string[] Names(Regex regex) => [.. regex.GetGroupNames().Where(n => !int.TryParse(n, out _))];
}
