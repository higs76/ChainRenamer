using System.Text.RegularExpressions;

namespace ChainRenamer.Engine.Modules;

/// <summary>
/// Replaces what a regular expression matches, in the name without its extension.
/// The replacement can use groups: <c>$1</c>, <c>${name}</c>.
/// </summary>
[Module("RegexReplace")]
public sealed record RegexReplaceModule(string Pattern, string ReplaceWith, bool MatchCase = false) : INameModule
{
    // Guards against a pathological pattern freezing the preview.
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1);

    /// <exception cref="ArgumentException">Invalid regular expression.</exception>
    public IReadOnlyList<Item> Apply(IReadOnlyList<Item> input)
    {
        if (Pattern.Length == 0) return input;

        var regex = new Regex(Pattern, MatchCase ? RegexOptions.None : RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, Timeout);
        return [.. input.Select((i, k) => i.With(NamePart.Stem, regex.Replace(i.Stem, Variables.Expand(ReplaceWith, i, k + 1))))];
    }
}
