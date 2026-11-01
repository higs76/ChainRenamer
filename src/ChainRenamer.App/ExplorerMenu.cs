using System.Runtime.Versioning;
using Microsoft.Win32;

namespace ChainRenamer.App;

/// <summary>
/// The "Rename with ChainRenamer" entry of the Explorer menu, on a folder and on the background of an open folder.
/// Written for the current user only (HKCU\Software\Classes): no administrator rights needed.
/// On Windows 11 it shows under "Show more options".
/// </summary>
[SupportedOSPlatform("windows")]
public static class ExplorerMenu
{
    private const string Key = "ChainRenamer";

    // The folder clicked is %1; the background of an open folder gives that folder as %V.
    private static readonly (string Path, string Argument)[] Places =
    [
        (@"Directory\shell\" + Key, "%1"),
        (@"Directory\Background\shell\" + Key, "%V"),
    ];

    /// <summary>Where the entries are written; tests use a scratch key instead of the user's.</summary>
    public static string ClassesRoot { get; set; } = @"Software\Classes";

    public static bool IsInstalled => Command() is not null;

    /// <param name="label">The text of the entry, in the language of the interface.</param>
    /// <param name="exe">The program to start: the one running now, by default.</param>
    public static void Install(string label, string? exe = null)
    {
        exe ??= Environment.ProcessPath ?? throw new InvalidOperationException("The path of the program is unknown.");
        foreach (var (path, argument) in Places)
        {
            using var entry = Registry.CurrentUser.CreateSubKey($@"{ClassesRoot}\{path}");
            entry.SetValue("", label);
            entry.SetValue("Icon", $"\"{exe}\"");
            using var command = entry.CreateSubKey("command");
            command.SetValue("", $"\"{exe}\" \"{argument}\"");
        }
    }

    public static void Uninstall()
    {
        foreach (var (path, _) in Places) Registry.CurrentUser.DeleteSubKeyTree($@"{ClassesRoot}\{path}", throwOnMissingSubKey: false);
    }

    /// <summary>The command written in the registry, or null: to check that it still starts this program.</summary>
    public static string? Command()
    {
        using var key = Registry.CurrentUser.OpenSubKey($@"{ClassesRoot}\{Places[0].Path}\command");
        return key?.GetValue("") as string;
    }
}
