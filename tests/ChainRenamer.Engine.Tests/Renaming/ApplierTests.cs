using ChainRenamer.Engine.Renaming;

namespace ChainRenamer.Engine.Tests.Renaming;

/// <summary>Real renaming, in a temporary folder.</summary>
public sealed class ApplierTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("ChainRenamer-").FullName;
    private string JournalPath => Path.Combine(root, "journal", "run.jsonl");

    public void Dispose() => Directory.Delete(root, recursive: true);

    /// <summary>Creates files whose content is their original name, to follow them through the renaming.</summary>
    private List<FileItem> CreateFiles(params string[] names) =>
        [.. names.Select(name =>
        {
            var path = Path.Combine(root, name);
            File.WriteAllText(path, name);
            return FileItem.FromPath(path);
        })];

    private static RenamePlan Plan(List<FileItem> files, params string[] newNames) =>
        RenamePlan.Create(files, [.. files.Zip(newNames, (f, n) => new Item(f.Id, n))], File.Exists);

    /// <summary>File name → its content (the original name), sorted by name.</summary>
    private string[] Disk() =>
        [.. Directory.GetFiles(root).Order(StringComparer.Ordinal).Select(p => $"{Path.GetFileName(p)}={File.ReadAllText(p)}")];

    [Fact]
    public async Task Renames_the_files()
    {
        var files = CreateFiles("a.txt", "b.txt");

        var result = await Applier.ApplyAsync(Plan(files, "x.txt", "y.txt"), JournalPath);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Renamed);
        Assert.Equal(["x.txt=a.txt", "y.txt=b.txt"], Disk());
    }

    [Fact]
    public async Task Swaps_two_names()
    {
        var files = CreateFiles("a.txt", "b.txt");

        await Applier.ApplyAsync(Plan(files, "b.txt", "a.txt"), JournalPath);

        Assert.Equal(["a.txt=b.txt", "b.txt=a.txt"], Disk());
    }

    [Fact]
    public async Task Handles_a_cycle()
    {
        var files = CreateFiles("1.txt", "2.txt", "3.txt");

        await Applier.ApplyAsync(Plan(files, "2.txt", "3.txt", "1.txt"), JournalPath);

        Assert.Equal(["1.txt=3.txt", "2.txt=1.txt", "3.txt=2.txt"], Disk());
    }

    [Fact]
    public async Task Handles_a_renumbering_shift()
    {
        var files = CreateFiles("ep 1.mkv", "ep 2.mkv", "ep 3.mkv");

        await Applier.ApplyAsync(Plan(files, "ep 2.mkv", "ep 3.mkv", "ep 4.mkv"), JournalPath);

        Assert.Equal(["ep 2.mkv=ep 1.mkv", "ep 3.mkv=ep 2.mkv", "ep 4.mkv=ep 3.mkv"], Disk());
    }

    [Fact]
    public async Task Changes_the_case_only()
    {
        var files = CreateFiles("photo.jpg");

        await Applier.ApplyAsync(Plan(files, "Photo.JPG"), JournalPath);

        Assert.Equal(["Photo.JPG=photo.jpg"], Disk());
    }

    [Fact]
    public async Task Leaves_no_temporary_file()
    {
        var files = CreateFiles("a.txt", "b.txt");

        await Applier.ApplyAsync(Plan(files, "b.txt", "a.txt"), JournalPath);

        Assert.DoesNotContain(Directory.GetFiles(root), p => p.EndsWith(".tmp"));
    }

    [Fact]
    public async Task Writes_every_move_to_the_journal()
    {
        var files = CreateFiles("a.txt");

        await Applier.ApplyAsync(Plan(files, "x.txt"), JournalPath);

        var journal = Journal.Read(JournalPath);
        Assert.Equal([new JournalEntry(files[0].FullPath, Path.Combine(root, "x.txt"))], journal);
    }

    [Fact]
    public async Task Only_the_files_whose_name_is_taken_go_through_a_temporary_name()
    {
        // b is free to go to c at once; a must wait for b to leave.
        var files = CreateFiles("a.txt", "b.txt", "d.txt");

        var result = await Applier.ApplyAsync(Plan(files, "b.txt", "c.txt", "e.txt"), JournalPath);

        Assert.Equal(3, result.Renamed);
        Assert.Equal(["b.txt=a.txt", "c.txt=b.txt", "e.txt=d.txt"], Disk());
        Assert.Equal(4, Journal.Read(JournalPath).Count);    // a → temporary → b, b → c, d → e
    }

    [Fact]
    public async Task A_case_only_change_goes_through_a_temporary_name()
    {
        var files = CreateFiles("photo.jpg");

        await Applier.ApplyAsync(Plan(files, "Photo.jpg"), JournalPath);

        Assert.Equal(2, Journal.Read(JournalPath).Count);
    }

    [Fact]
    public async Task Undo_puts_every_name_back()
    {
        var files = CreateFiles("1.txt", "2.txt", "3.txt");
        await Applier.ApplyAsync(Plan(files, "2.txt", "3.txt", "1.txt"), JournalPath);

        Assert.True(await Applier.UndoAsync(JournalPath));

        Assert.Equal(["1.txt=1.txt", "2.txt=2.txt", "3.txt=3.txt"], Disk());
    }

    [Fact]
    public async Task Undoing_twice_does_no_harm()
    {
        var files = CreateFiles("a.txt", "b.txt");
        await Applier.ApplyAsync(Plan(files, "b.txt", "a.txt"), JournalPath);

        await Applier.UndoAsync(JournalPath);
        await Applier.UndoAsync(JournalPath);

        Assert.Equal(["a.txt=a.txt", "b.txt=b.txt"], Disk());
    }

    [Fact]
    public async Task An_error_in_the_middle_undoes_everything()
    {
        var files = CreateFiles("a.txt", "b.txt", "c.txt");
        var plan = Plan(files, "x.txt", "y.txt", "z.txt");

        // A file created after the check takes one of the target names: its move fails in phase 2.
        File.WriteAllText(Path.Combine(root, "y.txt"), "intruder");
        var result = await Applier.ApplyAsync(plan, JournalPath);

        Assert.False(result.Succeeded);
        Assert.IsAssignableFrom<IOException>(result.Error);
        Assert.True(result.RolledBack);
        Assert.Equal(["a.txt=a.txt", "b.txt=b.txt", "c.txt=c.txt", "y.txt=intruder"], Disk());
    }

    [Fact]
    public async Task A_locked_file_stops_and_undoes_the_renaming()
    {
        var files = CreateFiles("a.txt", "b.txt");

        Task<ApplyResult> apply;
        using (File.Open(files[1].FullPath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            apply = Applier.ApplyAsync(Plan(files, "x.txt", "y.txt"), JournalPath);
            await apply;
        }
        var result = await apply;

        Assert.False(result.Succeeded);
        Assert.True(result.RolledBack);
        Assert.Equal(["a.txt=a.txt", "b.txt=b.txt"], Disk());
    }

    [Fact]
    public async Task A_plan_with_conflicts_is_refused()
    {
        var files = CreateFiles("a.txt", "b.txt");

        await Assert.ThrowsAsync<InvalidOperationException>(() => Applier.ApplyAsync(Plan(files, "x.txt", "x.txt"), JournalPath));
        Assert.Equal(["a.txt=a.txt", "b.txt=b.txt"], Disk());
    }

    [Fact]
    public async Task After_a_rollback_nothing_remains_to_undo()
    {
        var files = CreateFiles("a.txt", "b.txt");
        var plan = Plan(files, "x.txt", "y.txt");
        File.WriteAllText(Path.Combine(root, "y.txt"), "intruder");
        await Applier.ApplyAsync(plan, JournalPath);

        Assert.Empty(Journal.ToUndo(Journal.Read(JournalPath)));
    }

    [Fact]
    public void Journal_knows_what_remains_to_undo()
    {
        JournalEntry m1 = new("a", "t1"), m2 = new("t1", "b"), m3 = new("c", "t2");

        var remaining = Journal.ToUndo([m1, m2, m3, m3 with { Undo = true }]);

        Assert.Equal([m1, m2], remaining);
    }

    [Fact]
    public void A_journal_line_cut_by_a_crash_is_ignored()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(JournalPath)!);
        File.WriteAllText(JournalPath, """{"From":"a","To":"b"}""" + "\n" + """{"From":"c","To""");

        Assert.Equal([new JournalEntry("a", "b")], Journal.Read(JournalPath));
    }
}
