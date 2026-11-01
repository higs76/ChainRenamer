using System.Text.Json.Serialization;

namespace ChainRenamer.Engine;

/// <summary>
/// A favorite, as in a browser: a chain of the library, or a folder of favorites (<see cref="Items"/> is not null).
/// The user keeps them in the order they choose.
/// </summary>
public sealed record Favorite(string Name, IReadOnlyList<Favorite>? Items = null)
{
    [JsonIgnore]
    public bool IsFolder => Items is not null;

    public static Favorite Folder(string name, params IEnumerable<Favorite> items) => new(name, [.. items]);

    /// <summary>The chain names of a tree of favorites, folders included.</summary>
    public static IEnumerable<string> Chains(IEnumerable<Favorite> favorites) =>
        favorites.SelectMany(f => f.IsFolder ? Chains(f.Items!) : [f.Name]);

    /// <summary>
    /// The same tree with each chain renamed by <paramref name="rename"/>, or removed when it returns null.
    /// Folders stay, even empty.
    /// </summary>
    public static IReadOnlyList<Favorite> Map(IEnumerable<Favorite> favorites, Func<string, string?> rename) =>
    [
        .. favorites.Select(f => f.IsFolder ? f with { Items = Map(f.Items!, rename) } : rename(f.Name) is { } name ? f with { Name = name } : null)
                    .OfType<Favorite>()
    ];

    /// <summary>
    /// Adds <paramref name="added"/> after <paramref name="favorites"/>: a folder joins the folder of the same name,
    /// a chain already at that level is not repeated.
    /// </summary>
    public static IReadOnlyList<Favorite> Merge(IEnumerable<Favorite> favorites, IEnumerable<Favorite> added)
    {
        var merged = favorites.ToList();
        foreach (var favorite in added)
        {
            int same = merged.FindIndex(f => f.IsFolder == favorite.IsFolder && f.Name.Equals(favorite.Name, StringComparison.OrdinalIgnoreCase));
            if (same < 0) merged.Add(favorite);
            else if (favorite.IsFolder) merged[same] = merged[same] with { Items = Merge(merged[same].Items!, favorite.Items!) };
        }
        return merged;
    }
}
