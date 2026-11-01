namespace ChainRenamer.Engine.Modules;

/// <summary>What <see cref="RemoveModule"/> removes.</summary>
public enum RemoveWhat
{
    /// <summary>The first <see cref="RemoveModule.Count"/> characters.</summary>
    First,

    /// <summary>The last <see cref="RemoveModule.Count"/> characters.</summary>
    Last,

    /// <summary>From position <see cref="RemoveModule.From"/> to <see cref="RemoveModule.To"/>.</summary>
    Range,

    /// <summary>Everything before <see cref="RemoveModule.Text"/>.</summary>
    Before,

    /// <summary>Everything after <see cref="RemoveModule.Text"/>.</summary>
    After,

    /// <summary>Each part from <see cref="RemoveModule.Text"/> to <see cref="RemoveModule.EndText"/>: "[1080p]", "(VOSTFR)".</summary>
    Between,
}

/// <summary>
/// Removes part of the name, without its extension, by position or around a text.
/// Positions count characters from 1; a negative position counts from the end (-1 is the last character).
/// </summary>
/// <param name="IncludeText">Before, After and Between: the texts themselves are removed too.</param>
/// <param name="LastOccurrence">Before and After: the text is searched from the end of the name.</param>
[Module("Remove")]
public sealed record RemoveModule(
    RemoveWhat What = RemoveWhat.First,
    int Count = 1,
    int From = 1,
    int To = 1,
    string Text = "",
    string EndText = "",
    bool IncludeText = true,
    bool LastOccurrence = false,
    bool MatchCase = false) : INameModule
{
    private StringComparison Comparison => MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    public IReadOnlyList<Item> Apply(IReadOnlyList<Item> input) =>
        [.. input.Select(i => i.With(NamePart.Stem, Remove(i.Stem)))];

    private string Remove(string name) => What switch
    {
        RemoveWhat.First => name[Math.Clamp(Count, 0, name.Length)..],
        RemoveWhat.Last => name[..(name.Length - Math.Clamp(Count, 0, name.Length))],
        RemoveWhat.Range => RemoveRange(name),
        RemoveWhat.Before or RemoveWhat.After => RemoveAround(name),
        _ => RemoveBetween(name),
    };

    private string RemoveRange(string name)
    {
        // 1-based and inclusive; -1 is the last character.
        int Index(int position) => position < 0 ? name.Length + position : position - 1;
        int from = Math.Max(Index(From), 0), to = Math.Min(Index(To), name.Length - 1);
        if (from > to) (from, to) = (Math.Max(Index(To), 0), Math.Min(Index(From), name.Length - 1));
        return from > to ? name : name.Remove(from, to - from + 1);
    }

    private string RemoveAround(string name)
    {
        if (Text.Length == 0) return name;
        int at = LastOccurrence ? name.LastIndexOf(Text, Comparison) : name.IndexOf(Text, Comparison);
        if (at < 0) return name;

        return What == RemoveWhat.Before
            ? name[(IncludeText ? at + Text.Length : at)..]
            : name[..(IncludeText ? at : at + Text.Length)];
    }

    private string RemoveBetween(string name)
    {
        if (Text.Length == 0 || EndText.Length == 0) return name;
        for (int start = name.IndexOf(Text, Comparison); start >= 0; start = name.IndexOf(Text, start, Comparison))
        {
            int end = name.IndexOf(EndText, start + Text.Length, Comparison);
            if (end < 0) break;

            // Inside only: the texts stay, and the search goes on after the end text.
            int from = IncludeText ? start : start + Text.Length, to = IncludeText ? end + EndText.Length : end;
            name = name.Remove(from, to - from);
            start = IncludeText ? from : from + EndText.Length;
        }
        return name;
    }
}
