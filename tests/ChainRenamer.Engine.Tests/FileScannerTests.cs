using ChainRenamer.Engine.Modules;

namespace ChainRenamer.Engine.Tests;

public sealed class FileScannerTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("ChainRenamer-").FullName;

    public FileScannerTests()
    {
        foreach (var name in new[] { "ep 10.mkv", "ep 2.mkv", @"S01\ep 1.mkv" })
        {
            var path = Path.Combine(root, name);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "");
        }
        var hidden = Path.Combine(root, "Thumbs.db");
        File.WriteAllText(hidden, "");
        File.SetAttributes(hidden, FileAttributes.Hidden | FileAttributes.System);
    }

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public async Task Lists_the_folder_in_natural_order_without_hidden_files()
    {
        var files = await FileScanner.ScanAsync(new SourceModule(root));

        Assert.Equal(["ep 2.mkv", "ep 10.mkv"], files.Select(f => f.Name));
        Assert.All(files, f => Assert.Equal(root, f.Directory));
    }

    [Fact]
    public async Task Reads_the_size_and_date_of_each_file()
    {
        var path = Path.Combine(root, "ep 2.mkv");
        File.WriteAllText(path, "12345");
        File.SetLastWriteTime(path, new DateTime(2024, 5, 17, 20, 30, 0));

        var file = (await FileScanner.ScanAsync(new SourceModule(root)))[0];

        Assert.Equal(5, file.Size);
        Assert.Equal(new DateTime(2024, 5, 17, 20, 30, 0), file.Modified);
    }

    [Fact]
    public async Task Can_include_subfolders()
    {
        var files = await FileScanner.ScanAsync(new SourceModule(root, IncludeSubfolders: true));

        Assert.Equal(["ep 2.mkv", "ep 10.mkv", "ep 1.mkv"], files.Select(f => f.Name));
    }

    [Fact]
    public async Task Gives_each_file_its_own_id()
    {
        var files = await FileScanner.ScanAsync(new SourceModule(root));

        Assert.Equal(files.Count, files.Select(f => f.Id).Distinct().Count());
    }

    [Fact]
    public async Task Missing_folder_is_reported()
    {
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => FileScanner.ScanAsync(new SourceModule(Path.Combine(root, "missing"))));
    }
}
