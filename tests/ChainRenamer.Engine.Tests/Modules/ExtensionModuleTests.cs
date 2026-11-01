using ChainRenamer.Engine.Modules;

namespace ChainRenamer.Engine.Tests.Modules;

public class ExtensionModuleTests
{
    private static string Change(ExtensionModule module, string name) => module.Apply(TestItems.Of(name))[0].Name;

    [Theory]
    [InlineData(ExtensionAction.Lower, "Holiday PHOTO.jpg")]
    [InlineData(ExtensionAction.Upper, "Holiday PHOTO.JPG")]
    [InlineData(ExtensionAction.Title, "Holiday PHOTO.Jpg")]
    [InlineData(ExtensionAction.Remove, "Holiday PHOTO")]
    public void Changes_only_the_extension(ExtensionAction action, string expected) =>
        Assert.Equal(expected, Change(new(action), "Holiday PHOTO.jPG"));

    [Theory]
    [InlineData("jpeg")]
    [InlineData(".jpeg")]
    [InlineData(" jpeg ")]
    public void Sets_an_extension_with_or_without_its_dot(string text)
    {
        Assert.Equal("photo.jpeg", Change(new(ExtensionAction.Set, text), "photo.jpg"));
        Assert.Equal("README.jpeg", Change(new(ExtensionAction.Set, text), "README"));
    }

    [Fact]
    public void Adds_after_the_extension()
    {
        Assert.Equal("card.holiday.gif", Change(new(ExtensionAction.Add, "gif"), "card.holiday"));
        Assert.Equal("card.gif", Change(new(ExtensionAction.Add, ".gif"), "card"));
    }

    [Fact]
    public void An_empty_text_changes_nothing()
    {
        Assert.Equal("photo.jpg", Change(new(ExtensionAction.Set, ""), "photo.jpg"));
        Assert.Equal("photo.jpg", Change(new(ExtensionAction.Add, " "), "photo.jpg"));
    }

    [Fact]
    public void Only_the_last_extension_counts() =>
        Assert.Equal("archive.tar.GZ", Change(new(ExtensionAction.Upper), "archive.tar.gz"));
}
