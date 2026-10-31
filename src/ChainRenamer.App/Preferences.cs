using System.Text.Json;
using System.Text.Json.Serialization;
using ChainRenamer.Engine;

namespace ChainRenamer.App;

/// <summary>The user's settings, kept in %APPDATA%\ChainRenamer\settings.json.</summary>
public sealed record Preferences(string? LibraryFolder = null, bool ShowOriginal = true)
{
    /// <summary>Where the settings are kept; changed only by test harnesses, to leave the user's settings alone.</summary>
    public static string FilePath { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChainRenamer", "settings.json");

    [JsonIgnore]
    public string Library => LibraryFolder ?? ChainLibrary.DefaultFolder;

    /// <summary>The saved settings, or the defaults if there are none or they cannot be read.</summary>
    public static Preferences Load()
    {
        try
        {
            return File.Exists(FilePath) ? JsonSerializer.Deserialize<Preferences>(File.ReadAllText(FilePath)) ?? new() : new();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Losing a preference is not worth interrupting the user.
        }
    }
}
