using Microsoft.Win32;

namespace ChainRenamer.App.Tests;

/// <summary>Writes under a scratch key of the current user, never in the real Explorer menu, and removes it.</summary>
public sealed class ExplorerMenuTests : IDisposable
{
    private const string Parent = @"Software\ChainRenamerTests";
    private readonly string root = Parent + @"\" + Guid.NewGuid().ToString("N");
    private readonly string previous = ExplorerMenu.ClassesRoot;

    public ExplorerMenuTests() => ExplorerMenu.ClassesRoot = root;

    public void Dispose()
    {
        ExplorerMenu.ClassesRoot = previous;
        // The whole scratch key: only this class uses it, and its tests run one after the other.
        if (OperatingSystem.IsWindows()) Registry.CurrentUser.DeleteSubKeyTree(Parent, throwOnMissingSubKey: false);
    }

    [Fact]
    public void Installs_the_entry_on_folders_and_on_their_background()
    {
        if (!OperatingSystem.IsWindows()) return;

        ExplorerMenu.Install("Rename with ChainRenamer", @"C:\Tools\ChainRenamer.exe");

        Assert.True(ExplorerMenu.IsInstalled);
        Assert.Equal("\"C:\\Tools\\ChainRenamer.exe\" \"%1\"", ExplorerMenu.Command());
        using var background = Registry.CurrentUser.OpenSubKey($@"{root}\Directory\Background\shell\ChainRenamer\command");
        Assert.Equal("\"C:\\Tools\\ChainRenamer.exe\" \"%V\"", background?.GetValue(""));
        using var entry = Registry.CurrentUser.OpenSubKey($@"{root}\Directory\shell\ChainRenamer");
        Assert.Equal("Rename with ChainRenamer", entry?.GetValue(""));
    }

    [Fact]
    public void Uninstalls_both_entries()
    {
        if (!OperatingSystem.IsWindows()) return;

        ExplorerMenu.Install("x", @"C:\a.exe");
        ExplorerMenu.Uninstall();
        ExplorerMenu.Uninstall();       // twice: no harm

        Assert.False(ExplorerMenu.IsInstalled);
        Assert.Null(Registry.CurrentUser.OpenSubKey($@"{root}\Directory\Background\shell\ChainRenamer"));
    }
}
