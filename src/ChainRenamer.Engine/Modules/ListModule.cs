namespace ChainRenamer.Engine.Modules;

/// <summary>What a list does with its lines.</summary>
public enum ListMode
{
    /// <summary>Line N becomes the name of file N; the extension is kept.</summary>
    ReplaceName,

    /// <summary>Line N is inserted in the name of file N.</summary>
    Insert,

    /// <summary>Each line is "old name → new name": the file is found by its name, not by its rank.</summary>
    Pairs,
}

/// <summary>
/// Renames from a list of lines pasted or loaded from a text file, such as episode titles copied from a website.
/// Line N goes to file N, hence a Sort step just before. An empty line, or a file without line, keeps its name.
/// The lines may hold variables (<c>{n:2} - </c>).
/// </summary>
/// <param name="Lines">The lines, separated by line breaks; empty lines at the end do not count.</param>
/// <param name="Where">Insert mode: where the line goes, as for Insert.</param>
/// <param name="MatchCase">Insert mode: for the anchor text. Pairs mode: for the old names.</param>
[Module("List")]
public sealed record ListModule(
    string Lines = "",
    ListMode Mode = ListMode.ReplaceName,
    InsertAt Where = InsertAt.End,
    int Position = 0,
    string Anchor = "",
    bool MatchCase = false) : INameModule
{
    /// <summary>The lines of the list.</summary>
    public IReadOnlyList<string> Items() => Lines.Length == 0 ? [] : Lines.ReplaceLineEndings("\n").TrimEnd('\n').Split('\n');

    public IReadOnlyList<Item> Apply(IReadOnlyList<Item> input)
    {
        var lines = Items();
        if (lines.Count == 0) return input;
        if (Mode == ListMode.Pairs) return Rename(input, ReplaceTableModule.Parse(Lines));

        var place = new InsertModule(Where: Where, Position: Position, Anchor: Anchor, MatchCase: MatchCase);
        return [.. input.Select((item, k) => k >= lines.Count || lines[k].Length == 0 ? item
            : Variables.Expand(lines[k], item, k + 1) is var line && Mode == ListMode.Insert
                ? item.With(NamePart.Stem, place.Insert(item.Stem, line))
                : item.With(NamePart.Stem, line))];
    }

    // Pairs: an old name with its extension gives the whole new name; without, the extension is kept.
    private IReadOnlyList<Item> Rename(IReadOnlyList<Item> input, IReadOnlyList<ReplacePair> pairs)
    {
        var comparer = MatchCase ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        var byName = new Dictionary<string, string>(comparer);
        foreach (var pair in pairs) byName.TryAdd(pair.Find.Trim(), pair.ReplaceWith.Trim());

        return [.. input.Select(item =>
            byName.TryGetValue(item.Name, out var name) ? item.With(NamePart.Whole, name)
            : byName.TryGetValue(item.Stem, out var stem) ? item.With(NamePart.Stem, stem)
            : item)];
    }

    /// <summary>Pairs mode: the old names of the list that match no file, to warn about them.</summary>
    public IReadOnlyList<string> Unmatched(IReadOnlyList<Item> input)
    {
        if (Mode != ListMode.Pairs) return [];
        var comparer = MatchCase ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        var names = input.SelectMany(i => new[] { i.Name, i.Stem }).ToHashSet(comparer);
        return [.. ReplaceTableModule.Parse(Lines).Select(p => p.Find.Trim()).Where(n => n.Length > 0 && !names.Contains(n))];
    }
}
