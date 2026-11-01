namespace ChainRenamer.Engine.Modules;

/// <summary>What <see cref="ExtensionModule"/> does to the extension.</summary>
public enum ExtensionAction
{
    /// <summary>.JPG → .jpg</summary>
    Lower,

    /// <summary>.jpg → .JPG</summary>
    Upper,

    /// <summary>.jpg → .Jpg</summary>
    Title,

    /// <summary>Replaced by <see cref="ExtensionModule.Text"/>, or added if there is none.</summary>
    Set,

    /// <summary><see cref="ExtensionModule.Text"/> added after the extension: .holiday → .holiday.gif</summary>
    Add,

    /// <summary>No extension any more.</summary>
    Remove,
}

/// <summary>
/// Changes the extension: the only module that touches it. The extension is what follows the last dot.
/// <see cref="Text"/> may be written with or without its dot.
/// </summary>
[Module("Extension")]
public sealed record ExtensionModule(ExtensionAction Action = ExtensionAction.Lower, string Text = "") : INameModule
{
    private string Dotted => Text.Trim() is { Length: > 0 } text ? "." + text.TrimStart('.') : "";

    public IReadOnlyList<Item> Apply(IReadOnlyList<Item> input) => [.. input.Select(i => i.With(NamePart.Extension, Change(i.Extension)))];

    private string Change(string extension) => Action switch
    {
        ExtensionAction.Lower => extension.ToLowerInvariant(),
        ExtensionAction.Upper => extension.ToUpperInvariant(),
        ExtensionAction.Title => extension.Length > 1 ? "." + char.ToUpperInvariant(extension[1]) + extension[2..].ToLowerInvariant() : extension,
        ExtensionAction.Set => Dotted.Length > 0 ? Dotted : extension,
        ExtensionAction.Add => extension + Dotted,
        _ => "",
    };
}
