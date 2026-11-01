using System.Text;
using System.Text.Json;
using ChainRenamer.Engine;
using ChainRenamer.Engine.Modules;
using ChainRenamer.Engine.Renaming;

// Replays a saved chain on a folder: a preview by default, the renaming with --apply.
// Exit codes: 0 done, 1 conflicts or renaming error, 2 wrong arguments, chain or folder.
Console.OutputEncoding = Encoding.UTF8;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };   // Ctrl+C: the renaming is undone

try
{
    return Arguments.Parse(args) switch
    {
        { Help: true } => Usage(0),
        { Undo: { } journal } => await UndoAsync(journal),
        { Chain: { } chain, Folder: { } folder } options => await RunAsync(options, chain, folder),
        _ => Usage(2),
    };
}
catch (ArgumentException ex)
{
    return Fail(ex.Message);
}

async Task<int> RunAsync(Arguments options, string chainArgument, string folder)
{
    Chain chain;
    try
    {
        chain = await LoadChainAsync(chainArgument, options.Library);
    }
    catch (ChainFormatException ex)
    {
        return Fail($"{chainArgument}: {Describe(ex)}");
    }

    var source = new SourceModule(Path.GetFullPath(folder), options.Subfolders);
    IReadOnlyList<FileItem> files;
    try
    {
        files = await FileScanner.ScanAsync(source, cancellation.Token);
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
        return Fail($"{folder}: {ex.Message}");
    }

    var pipeline = new Pipeline(source, chain);
    pipeline.SetFiles(files);

    var failed = pipeline.Steps.Skip(1).Select((step, i) => (step, number: i + 2, result: pipeline.GetResult(step.Id)))
        .Where(s => s.result.Failed).ToList();
    foreach (var (step, number, result) in failed)
        Console.Error.WriteLine($"Step {number} ({step.Name ?? ModuleAttribute.NameOf(step.Module)}) failed: {result.Error!.Message}");

    var plan = RenamePlan.Create(pipeline.Files, pipeline.Output, options.Apply ? p => File.Exists(p) || Directory.Exists(p) : null);
    var conflicts = plan.Conflicts.ToDictionary(c => c.FileId, c => c.Kind);
    foreach (var op in plan.Operations)
    {
        var line = $"{Path.GetRelativePath(source.Folder, op.SourcePath)}  →  {Path.GetFileName(op.TargetPath)}";
        if (conflicts.TryGetValue(op.FileId, out var kind)) Console.WriteLine($"! {line}   [{kind}]");
        else Console.WriteLine($"  {line}");
    }
    Console.WriteLine($"{Count(files.Count, "file")}, {plan.Operations.Count} to rename, {Count(plan.Conflicts.Count, "conflict")}.");

    if (plan.Conflicts.Count > 0 || failed.Count > 0) return Fail("Nothing was renamed: fix the chain first.", 1);
    if (!options.Apply)
    {
        if (plan.Operations.Count > 0) Console.WriteLine("Preview only: add --apply to rename the files.");
        return 0;
    }
    if (plan.Operations.Count == 0) return 0;

    var journal = Journal.NewPath();
    var applied = await Applier.ApplyAsync(plan, journal, cancellationToken: cancellation.Token);
    if (applied.Succeeded)
    {
        Console.WriteLine($"Renamed {Count(applied.Renamed, "file")}. To undo: --undo \"{journal}\"");
        return 0;
    }
    return Fail(applied.RolledBack
        ? $"Renaming stopped, every file was put back: {applied.Error!.Message}"
        : $"Renaming stopped, some files could not be put back: {applied.Error!.Message}. Fix the cause, then: --undo \"{journal}\"", 1);
}

async Task<int> UndoAsync(string journal)
{
    if (!File.Exists(journal)) return Fail($"Journal not found: {journal}");
    return await Applier.UndoAsync(journal)
        ? Done("Every file was put back.")
        : Fail($"Some files could not be put back. Fix the cause, then run --undo again: it only undoes what remains.", 1);
}

// A path to a .chain file, or the name of a chain of the library.
static async Task<Chain> LoadChainAsync(string chain, string? libraryFolder)
{
    if (File.Exists(chain)) return await Chain.LoadAsync(chain);

    var library = new ChainLibrary(libraryFolder ?? AppLibraryFolder());
    return library.Contains(chain)
        ? await library.LoadAsync(chain)
        : throw new ArgumentException($"Chain not found: {chain} is neither a file nor a chain of the library {library.Folder}.");
}

// The library chosen in the settings of the App, which the CLI shares.
static string AppLibraryFolder()
{
    var settings = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChainRenamer", "settings.json");
    try
    {
        using var json = JsonDocument.Parse(File.ReadAllText(settings));
        if (json.RootElement.TryGetProperty("LibraryFolder", out var folder) && folder.GetString() is { Length: > 0 } path) return path;
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
    {
        // No settings: the default library.
    }
    return ChainLibrary.DefaultFolder;
}

static string Describe(ChainFormatException ex) => ex.Error switch
{
    ChainError.UnsupportedVersion => $"chain version {ex.Detail} is not supported by this version of ChainRenamer.",
    ChainError.UnknownModule => $"unknown module {ex.Detail}.",
    ChainError.InvalidSettings => $"invalid settings for the module {ex.Detail}.",
    _ => $"not a valid chain file ({ex.Detail})",
};

static string Count(int n, string noun) => $"{n} {noun}{(n == 1 ? "" : "s")}";

static int Done(string message)
{
    Console.WriteLine(message);
    return 0;
}

static int Fail(string message, int code = 2)
{
    Console.Error.WriteLine(message);
    return code;
}

static int Usage(int code)
{
    (code == 0 ? Console.Out : Console.Error).WriteLine("""
        Replays a chain saved by ChainRenamer on a folder.

        Usage:
          ChainRenamer.Cli --chain <chain> [--subfolders] [--apply] [--library <folder>] <folder>
          ChainRenamer.Cli --undo <journal>

          --chain <chain>     a .chain file, or the name of a chain of the library
          --subfolders        include the files of the subfolders
          --apply             rename the files; without it, only show what would be renamed
          --library <folder>  the chain library (default: the one chosen in the App)
          --undo <journal>    put back the files of a renaming, from the journal it wrote
        """);
    return code;
}

/// <summary>The command line, parsed.</summary>
sealed record Arguments(string? Chain, string? Folder, bool Subfolders, bool Apply, string? Library, string? Undo, bool Help)
{
    /// <exception cref="ArgumentException">An unknown option, a missing value or a second folder.</exception>
    public static Arguments Parse(string[] args)
    {
        var parsed = new Arguments(null, null, false, false, null, null, false);
        for (int i = 0; i < args.Length; i++)
        {
            string Value() => ++i < args.Length ? args[i] : throw new ArgumentException($"{args[i - 1]} needs a value.");
            parsed = args[i] switch
            {
                "--chain" or "-c" => parsed with { Chain = Value() },
                "--subfolders" or "-s" => parsed with { Subfolders = true },
                "--apply" => parsed with { Apply = true },
                "--library" => parsed with { Library = Value() },
                "--undo" => parsed with { Undo = Value() },
                "--help" or "-h" or "-?" or "/?" => parsed with { Help = true },
                ['-', ..] => throw new ArgumentException($"Unknown option: {args[i]}"),
                _ when parsed.Folder is null => parsed with { Folder = args[i] },
                _ => throw new ArgumentException($"Only one folder can be given: {args[i]}"),
            };
        }
        return parsed;
    }
}
