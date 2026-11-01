using System.Collections.Immutable;

namespace ChainRenamer.Engine.Modules;

/// <summary>One line of a replacement table. A disabled line is kept in the table, but not applied.</summary>
public sealed record ReplacePair(string Find, string ReplaceWith, bool Enabled = true);

/// <summary>
/// A list of texts to replace, applied in order on the name without its extension, each on the result of the previous
/// one: transliteration (<c>ß</c> → <c>ss</c>), a vocabulary of one's own (<c>VOSTFR</c> → <c>VOST</c>)…
/// </summary>
[Module("ReplaceTable")]
public sealed record ReplaceTableModule(ImmutableArray<ReplacePair> Pairs, bool MatchCase = false) : INameModule
{
    public IReadOnlyList<Item> Apply(IReadOnlyList<Item> input)
    {
        var pairs = Pairs.Where(p => p.Enabled && p.Find.Length > 0).ToList();
        var comparison = MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return pairs.Count == 0 ? input
            : [.. input.Select(i => i.With(NamePart.Stem, pairs.Aggregate(i.Stem, (name, p) => name.Replace(p.Find, p.ReplaceWith, comparison))))];
    }

    // The separators understood between a text and its replacement, the first found on a line wins.
    private static readonly string[] Separators = ["\t", " → ", "→", " -> ", "->", " => ", "=>", "="];

    /// <summary>
    /// Reads a table pasted or loaded from a text file: one pair per line, the text and its replacement separated by a tab
    /// (as copied from a spreadsheet), an arrow (<c>→</c>, <c>-&gt;</c>, <c>=&gt;</c>) or <c>=</c>. Spaces around the text
    /// are kept, those around an arrow are not. A line without separator replaces its text by nothing; empty lines are skipped.
    /// </summary>
    public static ImmutableArray<ReplacePair> Parse(string text) =>
        [.. text.ReplaceLineEndings("\n").Split('\n')
            .Where(line => line.Length > 0)
            .Select(line => Separators.Select(s => (s, at: line.IndexOf(s, StringComparison.Ordinal))).FirstOrDefault(p => p.at > 0) is { s: { } separator, at: var at }
                ? new ReplacePair(line[..at], line[(at + separator.Length)..])
                : new ReplacePair(line, ""))];

    /// <summary>The table as text, one pair per line separated by a tab: what <see cref="Parse"/> reads back.</summary>
    public static string Format(IEnumerable<ReplacePair> pairs) => string.Join(Environment.NewLine, pairs.Select(p => $"{p.Find}\t{p.ReplaceWith}"));

    // ImmutableArray compares by reference: two tables with the same lines are the same module.
    public bool Equals(ReplaceTableModule? other) => other is not null && MatchCase == other.MatchCase && Pairs.SequenceEqual(other.Pairs);

    public override int GetHashCode() => Pairs.Aggregate(MatchCase.GetHashCode(), HashCode.Combine);
}
