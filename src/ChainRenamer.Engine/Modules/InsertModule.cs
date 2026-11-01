namespace ChainRenamer.Engine.Modules;

/// <summary>Where <see cref="InsertModule"/> puts its text.</summary>
public enum InsertAt
{
    Start,

    /// <summary>At the end of the name, before the extension.</summary>
    End,

    /// <summary>After <see cref="InsertModule.Position"/> characters; a negative position counts from the end.</summary>
    Position,

    /// <summary>Before the first <see cref="InsertModule.Anchor"/>.</summary>
    Before,

    /// <summary>After the first <see cref="InsertModule.Anchor"/>.</summary>
    After,
}

/// <summary>Inserts a text in the name, without its extension. Variables ({folder}…) will come later.</summary>
[Module("Insert")]
public sealed record InsertModule(
    string Text = "",
    InsertAt Where = InsertAt.End,
    int Position = 0,
    string Anchor = "",
    bool MatchCase = false) : INameModule
{
    public IReadOnlyList<Item> Apply(IReadOnlyList<Item> input) =>
        Text.Length == 0 ? input : [.. input.Select(i => i.With(NamePart.Stem, Insert(i.Stem, Text)))];

    /// <summary>Inserts <paramref name="text"/> where this module says; also used by <see cref="NumberingModule"/>.</summary>
    internal string Insert(string name, string text)
    {
        var comparison = MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        int at = Where switch
        {
            InsertAt.Start => 0,
            InsertAt.End => name.Length,
            InsertAt.Position => Math.Clamp(Position < 0 ? name.Length + Position : Position, 0, name.Length),
            _ when Anchor.Length == 0 => -1,
            InsertAt.Before => name.IndexOf(Anchor, comparison),
            _ => name.IndexOf(Anchor, comparison) is var found and >= 0 ? found + Anchor.Length : -1,
        };
        return at < 0 ? name : name.Insert(at, text);
    }
}
