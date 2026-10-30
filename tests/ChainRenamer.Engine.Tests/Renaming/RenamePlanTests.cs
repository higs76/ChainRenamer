using ChainRenamer.Engine.Renaming;

namespace ChainRenamer.Engine.Tests.Renaming;

public class RenamePlanTests
{
    private const string Folder = @"C:\videos";

    private static FileItem File(string name) => FileItem.FromPath(Path.Combine(Folder, name));

    private static Item Rename(FileItem file, string newName) => new(file.Id, newName);

    private static ConflictKind? ConflictOf(RenamePlan plan, FileItem file) =>
        plan.Conflicts.FirstOrDefault(c => c.FileId == file.Id)?.Kind;

    [Fact]
    public void Lists_only_the_files_whose_name_changes()
    {
        FileItem a = File("a.txt"), b = File("b.txt");

        var plan = RenamePlan.Create([a, b], [Rename(a, "A1.txt"), Rename(b, "b.txt")]);

        var op = Assert.Single(plan.Operations);
        Assert.Equal(new RenameOperation(a.Id, @"C:\videos\a.txt", @"C:\videos\A1.txt"), op);
        Assert.True(plan.CanApply);
    }

    [Fact]
    public void A_case_only_change_is_a_rename()
    {
        var a = File("photo.jpg");

        var plan = RenamePlan.Create([a], [Rename(a, "Photo.jpg")]);

        Assert.Single(plan.Operations);
        Assert.Empty(plan.Conflicts);
    }

    [Fact]
    public void Files_left_out_keep_their_name()
    {
        FileItem a = File("a.txt"), b = File("b.txt");

        var plan = RenamePlan.Create([a, b], [Rename(a, "c.txt")]);

        Assert.Equal([a.Id], plan.Operations.Select(o => o.FileId));
    }

    [Fact]
    public void Swapping_two_names_is_not_a_conflict()
    {
        FileItem a = File("a.txt"), b = File("b.txt");

        var plan = RenamePlan.Create([a, b], [Rename(a, "b.txt"), Rename(b, "a.txt")], exists: _ => true);

        Assert.True(plan.CanApply);
    }

    [Fact]
    public void Two_files_with_the_same_new_name_are_in_conflict()
    {
        FileItem a = File("a.txt"), b = File("b.txt"), c = File("c.txt");

        var plan = RenamePlan.Create([a, b, c], [Rename(a, "x.txt"), Rename(b, "X.TXT"), Rename(c, "c.txt")]);

        Assert.Equal(ConflictKind.Duplicate, ConflictOf(plan, a));
        Assert.Equal(ConflictKind.Duplicate, ConflictOf(plan, b));
        Assert.Null(ConflictOf(plan, c));
        Assert.False(plan.CanApply);
    }

    [Fact]
    public void Taking_the_name_of_a_file_that_stays_is_a_conflict()
    {
        FileItem a = File("a.txt"), b = File("b.txt");

        // b is left out (unchecked, filtered): it keeps its name.
        var plan = RenamePlan.Create([a, b], [Rename(a, "b.txt")]);

        Assert.Equal(ConflictKind.Duplicate, ConflictOf(plan, a));
    }

    [Fact]
    public void Same_name_in_two_folders_is_not_a_conflict()
    {
        var a = FileItem.FromPath(@"C:\videos\S01\a.txt");
        var b = FileItem.FromPath(@"C:\videos\S02\b.txt");

        var plan = RenamePlan.Create([a, b], [Rename(a, "x.txt"), Rename(b, "x.txt")]);

        Assert.True(plan.CanApply);
    }

    [Fact]
    public void A_name_taken_on_disk_by_an_unlisted_file_is_a_conflict()
    {
        var a = File("a.txt");

        var plan = RenamePlan.Create([a], [Rename(a, "hidden.txt")], exists: path => path.EndsWith("hidden.txt"));

        Assert.Equal(ConflictKind.AlreadyExists, ConflictOf(plan, a));
    }

    [Fact]
    public void Without_disk_check_existing_files_are_not_looked_for()
    {
        var a = File("a.txt");

        Assert.True(RenamePlan.Create([a], [Rename(a, "hidden.txt")]).CanApply);
    }

    [Theory]
    [InlineData("", ConflictKind.EmptyName)]
    [InlineData("a?.txt", ConflictKind.InvalidCharacter)]
    [InlineData(@"sub\a.txt", ConflictKind.InvalidCharacter)]
    [InlineData("CON.txt", ConflictKind.ReservedName)]
    [InlineData("a.", ConflictKind.TrailingDotOrSpace)]
    public void Reports_invalid_names(string newName, ConflictKind expected)
    {
        var a = File("a.txt");

        Assert.Equal(expected, ConflictOf(RenamePlan.Create([a], [Rename(a, newName)]), a));
    }

    [Fact]
    public void Reports_paths_too_long()
    {
        var a = File("a.txt");

        var plan = RenamePlan.Create([a], [Rename(a, new string('x', 250) + ".txt")]);

        Assert.Equal(ConflictKind.PathTooLong, ConflictOf(plan, a));
    }

    [Fact]
    public void Nothing_to_rename_cannot_be_applied()
    {
        var a = File("a.txt");

        Assert.False(RenamePlan.Create([a], [Rename(a, "a.txt")]).CanApply);
    }
}
