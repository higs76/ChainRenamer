using System.Collections.Immutable;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChainRenamer.Engine;

/// <summary>Why a .chain file could not be read. The CLI and the App turn it into a message.</summary>
public enum ChainError
{
    InvalidFile,
    UnsupportedVersion,
    UnknownModule,
    InvalidSettings,
}

public sealed class ChainFormatException(ChainError error, string? detail = null, Exception? inner = null)
    : Exception($"{error}: {detail}", inner)
{
    public ChainError Error { get; } = error;

    /// <summary>The module type or version at fault, if any.</summary>
    public string? Detail { get; } = detail;
}

/// <summary>
/// A saved chain (.chain file): the steps that follow the source, a recipe that can be replayed on any folder.
/// Not saved: the source itself, and what depends on the loaded files (skipped files, manual order).
/// </summary>
public sealed record Chain(ImmutableArray<Step> Steps)
{
    public const int Version = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        // A missing setting without a default value, or a null one that cannot be null, is an error.
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        WriteIndented = true,
        // Readable files: apostrophes, accents and "+" as typed, not as '. Safe, since this is not HTML.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>The module types that can be saved, by their <see cref="ModuleAttribute"/> name.</summary>
    private static readonly Dictionary<string, Type> Types = typeof(IModule).Assembly.GetTypes()
        .Select(t => (type: t, name: t.GetCustomAttribute<ModuleAttribute>()?.Name))
        .Where(m => m.name is not null)
        .ToDictionary(m => m.name!, m => m.type, StringComparer.OrdinalIgnoreCase);

    // The file layout. A setting missing from the file takes the default value of the module's parameter.
    private sealed record Header(int Version);
    private sealed record ChainFile(int Version, List<StepFile> Steps);
    private sealed record StepFile(string Type, string? Name = null, bool Enabled = true, JsonElement? Settings = null, Segment? Segment = null);

    public string ToJson() => JsonSerializer.Serialize(new ChainFile(Version, [.. Steps.Select(ToFile)]), Options);

    /// <exception cref="ChainFormatException">The text is not a chain this version can read.</exception>
    public static Chain FromJson(string json)
    {
        try
        {
            // The version first: a newer file may have another layout.
            var version = JsonSerializer.Deserialize<Header>(json, Options)?.Version ?? throw new JsonException("Empty chain.");
            if (version != Version) throw new ChainFormatException(ChainError.UnsupportedVersion, version.ToString());

            var file = JsonSerializer.Deserialize<ChainFile>(json, Options)!;
            return new([.. file.Steps.Select(FromFile)]);
        }
        catch (JsonException ex)
        {
            throw new ChainFormatException(ChainError.InvalidFile, ex.Message, ex);
        }
    }

    /// <summary>Saves through a temporary file, so that a failure never leaves a half-written chain.</summary>
    public async Task SaveAsync(string path, CancellationToken cancellation = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temp = path + ".tmp";
        await File.WriteAllTextAsync(temp, ToJson(), cancellation);
        File.Move(temp, path, overwrite: true);
    }

    /// <inheritdoc cref="FromJson"/>
    public static async Task<Chain> LoadAsync(string path, CancellationToken cancellation = default) =>
        FromJson(await File.ReadAllTextAsync(path, cancellation));

    private static StepFile ToFile(Step step)
    {
        var type = step.Module.GetType();
        var name = type.GetCustomAttribute<ModuleAttribute>()?.Name
            ?? throw new InvalidOperationException($"{type.Name} has no [Module] attribute, so it cannot be saved.");
        return new(name, step.Name, step.IsEnabled, JsonSerializer.SerializeToElement(step.Module, type, Options),
            step.Segment is { IsWhole: false } segment ? segment : null);
    }

    private static Step FromFile(StepFile file)
    {
        if (!Types.TryGetValue(file.Type, out var type)) throw new ChainFormatException(ChainError.UnknownModule, file.Type);
        try
        {
            var settings = file.Settings ?? JsonSerializer.SerializeToElement(new { });
            var module = (IModule)settings.Deserialize(type, Options)!;
            return new(StepId.New(), module, file.Name, file.Enabled) { Segment = file.Segment };
        }
        catch (JsonException ex)
        {
            throw new ChainFormatException(ChainError.InvalidSettings, file.Type, ex);
        }
    }
}
