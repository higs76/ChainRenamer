namespace ChainRenamer.Engine;

/// <summary>
/// The state of one file as it flows through the chain.
/// Its rank is its position in the list; a file is included as long as it is in the list.
/// </summary>
public sealed record Item(FileId FileId, string Name)
{
    /// <summary>The folder of the file, which never changes: numbering can start again in each folder.</summary>
    public string Folder { get; init; } = "";

    /// <summary>The size of the file in bytes, which never changes.</summary>
    public long Size { get; init; }

    /// <summary>The last write time of the file, which never changes.</summary>
    public DateTime Modified { get; init; }

    // Not Path.GetExtension: a module may have put a '\' in the name, which must stay plain text.
    private int ExtensionStart => Name.LastIndexOf('.') is var i and >= 0 && i < Name.Length - 1 ? i : Name.Length;

    /// <summary>The name without its extension.</summary>
    public string Stem => Name[..ExtensionStart];

    /// <summary>The extension, dot included, or an empty string.</summary>
    public string Extension => Name[ExtensionStart..];

    public string Get(NamePart part) => part switch
    {
        NamePart.Stem => Stem,
        NamePart.Extension => Extension,
        _ => Name,
    };

    public Item With(NamePart part, string text) => this with
    {
        Name = part switch
        {
            NamePart.Stem => text + Extension,
            NamePart.Extension => Stem + text,
            _ => text,
        }
    };
}

/// <summary>The part of the name a name module works on.</summary>
public enum NamePart
{
    Stem,
    Extension,
    Whole,
}
