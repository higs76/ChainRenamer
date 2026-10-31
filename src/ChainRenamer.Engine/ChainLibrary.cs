using System.IO.Compression;
using System.Text.Json;
using ChainRenamer.Engine.Renaming;

namespace ChainRenamer.Engine;

/// <summary>
/// The saved chains: a folder with one .chain file per chain, named after it, and favorites.json.
/// Everything lives in the folder, so that a backup, a copy or a move of the folder keeps it all.
/// </summary>
public sealed class ChainLibrary(string folder)
{
    public const string Extension = ".chain";
    private const string FavoritesFile = "favorites.json";

    public string Folder { get; } = folder;

    public static string DefaultFolder { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChainRenamer", "chains");

    /// <summary>The chain names, in natural order.</summary>
    public IReadOnlyList<string> Names() => Directory.Exists(Folder)
        ? [.. Directory.EnumerateFiles(Folder, "*" + Extension).Select(f => Path.GetFileNameWithoutExtension(f)).Order(NaturalOrder.Comparer)]
        : [];

    /// <summary>A chain name must be usable as a file name.</summary>
    public static bool IsValidName(string name) => NameRules.Check(name) is null && NameRules.Check(name + Extension) is null;

    public string PathOf(string name) => Path.Combine(Folder, name + Extension);

    public bool Contains(string name) => File.Exists(PathOf(name));

    public Task<Chain> LoadAsync(string name, CancellationToken cancellation = default) => Chain.LoadAsync(PathOf(name), cancellation);

    /// <exception cref="ArgumentException">The name cannot be used as a file name.</exception>
    public Task SaveAsync(string name, Chain chain, CancellationToken cancellation = default) =>
        IsValidName(name) ? chain.SaveAsync(PathOf(name), cancellation) : throw new ArgumentException($"Invalid chain name: {name}", nameof(name));

    public void Delete(string name)
    {
        File.Delete(PathOf(name));
        SetFavorite(name, false);
    }

    // ───────────── Favorites ─────────────

    /// <summary>The favorite chains that still exist, in natural order.</summary>
    public IReadOnlyList<string> Favorites()
    {
        var path = Path.Combine(Folder, FavoritesFile);
        if (!File.Exists(path)) return [];
        try
        {
            var names = JsonSerializer.Deserialize<string[]>(File.ReadAllText(path)) ?? [];
            return [.. names.Where(Contains).Order(NaturalOrder.Comparer)];
        }
        catch (JsonException)
        {
            return [];      // a damaged list of favorites is not worth an error
        }
    }

    public bool IsFavorite(string name) => Favorites().Contains(name, StringComparer.OrdinalIgnoreCase);

    public void SetFavorite(string name, bool favorite)
    {
        var names = Favorites().Where(n => !n.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();
        if (favorite) names.Add(name);
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Path.Combine(Folder, FavoritesFile), JsonSerializer.Serialize(names));
    }

    // ───────────── Import and export ─────────────

    /// <summary>A name not used yet: <paramref name="name"/>, else "name (2)", "name (3)"…</summary>
    public string FreeName(string name)
    {
        var free = name;
        for (int n = 2; Contains(free); n++) free = $"{name} ({n})";
        return free;
    }

    /// <summary>Copies a .chain file into the library, under a free name. Returns that name.</summary>
    /// <exception cref="ChainFormatException">The file is not a chain this version can read.</exception>
    public async Task<string> ImportAsync(string file, CancellationToken cancellation = default)
    {
        var chain = await Chain.LoadAsync(file, cancellation);
        var name = FreeName(Path.GetFileNameWithoutExtension(file));
        await SaveAsync(name, chain, cancellation);
        return name;
    }

    /// <summary>Zips every chain and the favorites, for a reinstallation or another computer.</summary>
    public void ExportAll(string zip)
    {
        File.Delete(zip);
        using var archive = ZipFile.Open(zip, ZipArchiveMode.Create);
        foreach (var file in Names().Select(PathOf).Append(Path.Combine(Folder, FavoritesFile)).Where(File.Exists))
            archive.CreateEntryFromFile(file, Path.GetFileName(file));
    }

    /// <summary>
    /// Imports the chains of a zip made by <see cref="ExportAll"/>, under free names when a name is taken,
    /// and adds its favorites. Invalid chains are skipped. Returns the imported names.
    /// </summary>
    public async Task<IReadOnlyList<string>> ImportAllAsync(string zip, CancellationToken cancellation = default)
    {
        using var archive = ZipFile.OpenRead(zip);
        var imported = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);   // name in the zip → name here
        foreach (var entry in archive.Entries.Where(e => e.Name.EndsWith(Extension, StringComparison.OrdinalIgnoreCase)))
        {
            using var reader = new StreamReader(entry.Open());
            Chain chain;
            try { chain = Chain.FromJson(await reader.ReadToEndAsync(cancellation)); }
            catch (ChainFormatException) { continue; }

            var name = FreeName(Path.GetFileNameWithoutExtension(entry.Name));
            await SaveAsync(name, chain, cancellation);
            imported[Path.GetFileNameWithoutExtension(entry.Name)] = name;
        }

        if (archive.GetEntry(FavoritesFile) is { } favorites)
        {
            try
            {
                using var stream = favorites.Open();
                foreach (var name in JsonSerializer.Deserialize<string[]>(stream) ?? [])
                    if (imported.TryGetValue(name, out var here)) SetFavorite(here, true);
            }
            catch (JsonException) { }
        }
        return [.. imported.Values];
    }

    /// <summary>
    /// Copies the chains and favorites of this library into <paramref name="target"/> (when the user moves it),
    /// without overwriting what is there already.
    /// </summary>
    public void CopyTo(string target)
    {
        Directory.CreateDirectory(target);
        foreach (var file in Names().Select(PathOf).Append(Path.Combine(Folder, FavoritesFile)).Where(File.Exists))
        {
            var to = Path.Combine(target, Path.GetFileName(file));
            if (!File.Exists(to)) File.Copy(file, to);
        }
    }
}
