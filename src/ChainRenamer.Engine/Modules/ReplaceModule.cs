namespace ChainRenamer.Engine.Modules;

/// <summary>Which occurrences of the searched text are replaced.</summary>
public enum Occurrence
{
    All,
    First,
    Last,

    /// <summary>Only if the name starts with the searched text.</summary>
    Start,

    /// <summary>Only if the name ends with the searched text.</summary>
    End,
}

/// <summary>
/// Replaces a text by another, in the name without its extension (the extension has its own module).
/// The searched text is taken literally: regular expressions are <see cref="RegexReplaceModule"/>'s job.
/// </summary>
[Module("Replace")]
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

        int at = Occurrence switch
        {
            Occurrence.First => text.IndexOf(Find, Comparison),
            Occurrence.Last => text.LastIndexOf(Find, Comparison),
            Occurrence.Start => text.StartsWith(Find, Comparison) ? 0 : -1,
            _ => text.EndsWith(Find, Comparison) ? text.Length - Find.Length : -1,
        };
        return at < 0 ? text : string.Concat(text.AsSpan(0, at), ReplaceWith, text.AsSpan(at + Find.Length));
    }
}
