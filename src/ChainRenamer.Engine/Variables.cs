using System.Globalization;
using System.Text.RegularExpressions;

namespace ChainRenamer.Engine;

/// <summary>
/// The variables that the modules accepting text (Insert, Replace, Template…) expand: <c>{name}</c> or <c>{name:format}</c>.
/// English names, with French aliases, so that a chain works whatever the language of the interface.
/// A variable captured by a Capture step (<see cref="Item.Variables"/>) wins over a built-in one of the same name.
/// An unknown variable is left as typed: plain braces in a name stay.
/// </summary>
public static partial class Variables
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    // French aliases of the built-in variables.
    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["nom"] = "name",
        ["dossier"] = "folder",
        ["taille"] = "size",
        ["aujourdhui"] = "today",
    };

    [GeneratedRegex(@"\{(?<name>[\p{L}\p{N}_]+)(?::(?<format>[^{}]*))?\}")]
    private static partial Regex Variable();

    /// <summary>Whether a text holds something that looks like a variable: the modules skip the work otherwise.</summary>
    public static bool In(string text) => text.Contains('{');

    /// <param name="number">The rank of the file among those the step works on, from 1: <c>{n}</c>.</param>
    public static string Expand(string text, Item item, int number) => !In(text) ? text : Variable().Replace(text, match =>
    {
        var name = match.Groups["name"].Value;
        var format = match.Groups["format"].Success ? match.Groups["format"].Value : null;
        if (item.Variables.TryGetValue(name, out var captured)) return captured;

        return (Aliases.GetValueOrDefault(name) ?? name).ToLowerInvariant() switch
        {
            "name" => item.Stem,
            "ext" => item.Extension.TrimStart('.'),
            "folder" => Folder(item.Folder, format),
            "size" => Size(item.Size, format),
            "date" => item.Modified.ToString(DateFormat(format), Culture),
            "today" => DateTime.Today.ToString(DateFormat(format), Culture),
            "n" => int.TryParse(format, out int digits) ? number.ToString(new string('0', Math.Clamp(digits, 1, 12)), Culture) : number.ToString(Culture),
            _ => match.Value,
        };
    });

    // {folder} is the parent folder, {folder:2} its parent, and so on, up to the share of a UNC path; never the drive (C:).
    private static string Folder(string path, string? format)
    {
        int level = int.TryParse(format, out int l) && l > 1 ? l : 1;
        var folders = path.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries).Where(f => !f.EndsWith(':')).ToArray();
        return level <= folders.Length ? folders[^level] : "";
    }

    // {size} in the most readable unit (1.5 MB), or {size:KB}, {size:MB}, {size:GB} as a rounded number, {size:B} in bytes.
    private static string Size(long bytes, string? format)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        int unit = format is null
            ? (int)Math.Min(units.Length - 1, bytes <= 0 ? 0 : Math.Floor(Math.Log(bytes, 1024)))
            : Array.FindIndex(units, u => u.Equals(format, StringComparison.OrdinalIgnoreCase));
        if (unit < 0) unit = 0;
        double value = bytes / Math.Pow(1024, unit);
        return format is null ? $"{value.ToString(unit == 0 ? "0" : "0.#", Culture)} {units[unit]}" : Math.Round(value).ToString(Culture);
    }

    private static string DateFormat(string? format) => string.IsNullOrEmpty(format) ? "yyyy-MM-dd" : format;
}
