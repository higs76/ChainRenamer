using ChainRenamer.Engine.Modules;

namespace ChainRenamer.Engine.Tests;

public class VariablesTests
{
    private static readonly Item Episode = new(FileId.New(), "speedy 03.mkv")
    {
        Folder = @"\\nas\video\Speedy\Season 1",
        Size = 1_572_864,     // 1.5 MB
        Modified = new DateTime(2024, 5, 17, 20, 30, 0),
    };

    private static string Expand(string text, int number = 1) => Variables.Expand(text, Episode, number);

    [Theory]
    [InlineData("{name}", "speedy 03")]
    [InlineData("{ext}", "mkv")]
    [InlineData("{folder}", "Season 1")]
    [InlineData("{folder:2}", "Speedy")]
    [InlineData("{folder:3}", "video")]
    [InlineData("{size}", "1.5 MB")]
    [InlineData("{size:KB}", "1536")]
    [InlineData("{date}", "2024-05-17")]
    [InlineData("{date:yyyyMMdd}", "20240517")]
    [InlineData("{n}", "7")]
    [InlineData("{n:3}", "007")]
    public void Expands_the_built_in_variables(string text, string expected) =>
        Assert.Equal(expected, Expand(text, 7));

    [Theory]
    [InlineData("{dossier}", "Season 1")]
    [InlineData("{nom}", "speedy 03")]
    [InlineData("{taille:MB}", "2")]
    [InlineData("{FOLDER}", "Season 1")]
    public void Accepts_french_names_and_any_case(string text, string expected) =>
        Assert.Equal(expected, Expand(text));

    [Fact]
    public void Today_is_the_date_of_the_day()
    {
        Assert.Equal(DateTime.Today.ToString("yyyy-MM-dd"), Expand("{today}"));
    }

    [Fact]
    public void Leaves_unknown_variables_and_plain_braces()
    {
        Assert.Equal("{unknown} {} {a b}", Expand("{unknown} {} {a b}"));
    }

    [Fact]
    public void A_captured_variable_wins()
    {
        var item = Episode with { Variables = Episode.Variables.Add("Season", "01").Add("folder", "mine") };

        Assert.Equal("S01 mine", Variables.Expand("S{season} {folder}", item, 1));
    }

    [Fact]
    public void Template_rebuilds_the_name_and_keeps_the_extension()
    {
        var items = new TemplateModule("{folder:2} - {folder} - {n:2}").Apply([Episode, Episode with { FileId = FileId.New() }]);

        Assert.Equal(["Speedy - Season 1 - 01.mkv", "Speedy - Season 1 - 02.mkv"], items.Names());
        Assert.Equal(["speedy 03.mkv"], new TemplateModule().Apply([Episode]).Names());
    }

    [Fact]
    public void Insert_and_replace_expand_variables()
    {
        Assert.Equal(["[Season 1] speedy 03.mkv"], new InsertModule("[{folder}] ", InsertAt.Start).Apply([Episode]).Names());
        Assert.Equal(["Season 1 03.mkv"], new ReplaceModule("speedy", "{folder}").Apply([Episode]).Names());
        Assert.Equal(["speedy E03 (2024).mkv"], new RegexReplaceModule(@"(\d+)$", "E$1 ({date:yyyy})").Apply([Episode]).Names());
    }

    [Fact]
    public void A_wrong_date_format_throws_so_that_the_step_reports_it()
    {
        Assert.ThrowsAny<FormatException>(() => Expand("{date:%}"));
    }
}
