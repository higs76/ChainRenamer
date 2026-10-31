namespace ChainRenamer.Engine.Modules;

/// <summary>Which occurrences of the searched text are replaced.</summary>
public enum Occurrence
{
    All,
    First,
    Last,
}

/// <summary>
/// Replaces a text by another, in the name without its extension (the extension has its own module).
/// The searched text is taken literally: regular expressions are <see cref="RegexReplaceModule"/>'s job.
/// </summary>
public sealed record ReplaceModule(
    string Find,
    string ReplaceWith,
    bool MatchCase = false,
    Occurrence Occurrence = Occurrence.All) : INameModule
{
    private StringComparison Comparison => MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    public IReadOnlyList<Item> Apply(IReadOnlyList<Item> input) =>
        Find.Length == 0 ? input : [.. input.Select(i => i.With(NamePart.Stem, Replace(i.Stem)))];

    private string Replace(string text)
    {
        if (Occurrence == Occurrence.All) return text.Replace(Find, ReplaceWith, Comparison);

        int at = Occurrence == Occurrence.First ? text.IndexOf(Find, Comparison) : text.LastIndexOf(Find, Comparison);
        return at < 0 ? text : string.Concat(text.AsSpan(0, at), ReplaceWith, text.AsSpan(at + Find.Length));
    }
}
