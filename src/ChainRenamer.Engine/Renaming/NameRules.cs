namespace ChainRenamer.Engine.Renaming;

/// <summary>Why a file cannot be renamed as planned. The interfaces translate it into text.</summary>
public enum ConflictKind
{
    EmptyName,
    InvalidCharacter,
    ReservedName,
    TrailingDotOrSpace,
    PathTooLong,
    /// <summary>Two files of the same folder would get the same name.</summary>
    Duplicate,
    /// <summary>A file or folder that is not part of the renaming already has this name.</summary>
    AlreadyExists,
}

/// <summary>Windows naming rules, which also apply to SMB shares.</summary>
public static class NameRules
{
    /// <summary>Beyond this length, Explorer and many programs can no longer open the file.</summary>
    public const int MaxPathLength = 259;

    private static readonly char[] InvalidCharacters = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];

    private static readonly HashSet<string> ReservedNames = new(
        ["CON", "PRN", "AUX", "NUL",
         .. Enumerable.Range(1, 9).Select(n => $"COM{n}"),
         .. Enumerable.Range(1, 9).Select(n => $"LPT{n}")],
        StringComparer.OrdinalIgnoreCase);

    /// <summary>The first rule the name breaks, or null if it is valid.</summary>
    public static ConflictKind? Check(string name)
    {
        if (name.Length == 0) return ConflictKind.EmptyName;
        if (name.IndexOfAny(InvalidCharacters) >= 0 || name.Any(char.IsControl)) return ConflictKind.InvalidCharacter;
        if (name[^1] is '.' or ' ') return ConflictKind.TrailingDotOrSpace;

        // "CON", "con.txt" and "CON .txt" are all reserved: what counts is the part before the first dot.
        var device = name.Split('.')[0].TrimEnd();
        return ReservedNames.Contains(device) ? ConflictKind.ReservedName : null;
    }
}
