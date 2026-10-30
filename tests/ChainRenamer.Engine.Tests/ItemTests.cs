namespace ChainRenamer.Engine.Tests;

public class ItemTests
{
    [Theory]
    [InlineData("photo.jpg", "photo", ".jpg")]
    [InlineData("archive.tar.gz", "archive.tar", ".gz")]
    [InlineData("README", "README", "")]
    [InlineData(".gitignore", "", ".gitignore")]
    [InlineData("ends with dot.", "ends with dot.", "")]
    [InlineData(@"a\b.txt", @"a\b", ".txt")]
    [InlineData("", "", "")]
    public void Splits_stem_and_extension(string name, string stem, string extension)
    {
        var item = new Item(FileId.New(), name);

        Assert.Equal(stem, item.Stem);
        Assert.Equal(extension, item.Extension);
    }

    [Theory]
    [InlineData(NamePart.Stem, "new", "new.jpg")]
    [InlineData(NamePart.Extension, ".png", "photo.png")]
    [InlineData(NamePart.Whole, "new.txt", "new.txt")]
    public void Replaces_one_part_of_the_name(NamePart part, string text, string expected)
    {
        var item = new Item(FileId.New(), "photo.jpg");

        var changed = item.With(part, text);

        Assert.Equal(expected, changed.Name);
        Assert.Equal(item.FileId, changed.FileId);
    }
}
