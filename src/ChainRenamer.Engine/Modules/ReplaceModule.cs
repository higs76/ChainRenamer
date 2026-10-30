using System.Text.RegularExpressions;

namespace ChainRenamer.Engine.Modules;

/// <summary>Replaces a text or a regular expression. Works on the name without its extension by default.</summary>
/// <remarks>In regex mode, the replacement can use groups: <c>$1</c>, <c>${name}</c>.</remarks>
public sealed record ReplaceModule(
    string Find,
    string ReplaceWith,
    bool UseRegex = false,
    bool MatchCase = false,
    NamePart Part = NamePart.Stem) : IModule
{
    // Guards against a pathological pattern freezing the preview.
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    /// <exception cref="ArgumentException">Invalid regular expression.</exception>
    public IReadOnlyList<Item> Apply(IReadOnlyList<Item> input) =>
        Find.Length == 0 ? input : [.. input.Select(i => i.With(Part, Replace(i.Get(Part))))];

    private string Replace(string text) => UseRegex
        ? Regex.Replace(text, Find, ReplaceWith, MatchCase ? RegexOptions.None : RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeout)
        : text.Replace(Find, ReplaceWith, MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
}
