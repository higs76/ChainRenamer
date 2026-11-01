using System.Globalization;
using System.Reflection;
using ChainRenamer.App.ViewModels.Help;
using ChainRenamer.App.ViewModels.Modules;
using ChainRenamer.Engine;
using ChainRenamer.Engine.Modules;

namespace ChainRenamer.App.Tests;

public class HelpTests
{
    public static TheoryData<string> Languages => ["en", "fr"];

    /// <summary>Builds the help in <paramref name="language"/>, as the App does under that Windows language.</summary>
    private static IReadOnlyList<HelpPage> PagesIn(string language)
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo(language);
        try { return HelpCatalog.Pages(); }
        finally { CultureInfo.CurrentUICulture = previous; }
    }

    [Fact]
    public void Every_module_has_its_page()
    {
        foreach (var type in ModuleCatalog.Addable.Select(t => t.Module))
            Assert.Equal(type.GetCustomAttribute<ModuleAttribute>()!.Name, HelpCatalog.KeyOf(type));    // the key of the website anchor
        Assert.Equal("Source", HelpCatalog.KeyOf(typeof(SourceModule)));
        Assert.Equal(HelpCatalog.Home, HelpCatalog.KeyOf(null));
    }

    [Fact]
    public void Page_keys_are_unique() =>
        Assert.Equal(PagesIn("en").Count, PagesIn("en").Select(p => p.Key).Distinct().Count());

    [Fact]
    public void Every_example_changes_something()
    {
        foreach (var example in PagesIn("en").SelectMany(p => p.Examples))
            Assert.True(example.Rows.Any(r => r.IsChanged), example.Caption);    // for a sort: a file at another place
    }

    [Fact]
    public void Examples_show_what_the_module_does()
    {
        var page = PagesIn("en").Single(p => p.Key == "Sort");

        Assert.Equal(["Episode 1.mkv", "Episode 2.mkv", "Episode 10.mkv"], page.Examples[0].Rows.Select(r => r.After));
        Assert.Equal("speedy 01x03 my first race.mkv", PagesIn("en").Single(p => p.Key == "Replace").Examples[0].Rows[0].After);
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void Every_page_has_its_texts(string language)
    {
        foreach (var page in PagesIn(language))
        {
            Assert.All(page.Texts(), t => Assert.False(string.IsNullOrWhiteSpace(t), page.Key));
            Assert.NotEmpty(page.Paragraphs);
        }
    }

    [Fact]
    public void The_french_help_is_translated()
    {
        var english = PagesIn("en");
        var french = PagesIn("fr");

        Assert.All(english.Zip(french), p => Assert.NotEqual(p.First.Intro, p.Second.Intro));
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void Markup_is_closed(string language)
    {
        // An odd number of ` or ** would show the markup instead of formatting the text.
        foreach (var text in PagesIn(language).SelectMany(p => p.Texts()))
        {
            Assert.True(text.Count(c => c == '`') % 2 == 0, text);
            Assert.True((text.Split("**").Length - 1) % 2 == 0, text);
            Assert.DoesNotContain(@".\n", text);     // a line break typed as text instead of a real one
        }
    }

    [Fact]
    public void Search_ignores_case_and_accents()
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo("fr");
        try
        {
            var help = new HelpViewModel();
            help.Search = "EXPRESSION REGULIERE";

            Assert.Contains(help.Items, i => i.Page.Key == "RegexReplace");
            Assert.DoesNotContain(help.Items, i => i.Page.Key == "Shortcuts");

            help.Search = "zzzz";
            Assert.True(help.IsEmpty);
            Assert.Null(help.Page);

            help.Show("Sort");      // opening a page clears a search that hides it
            Assert.Equal("", help.Search);
            Assert.Equal("Sort", help.Page?.Key);
        }
        finally { CultureInfo.CurrentUICulture = previous; }
    }

    [Fact]
    public void Each_group_is_named_once()
    {
        var headers = new HelpViewModel().Items.Select(i => i.GroupHeader).OfType<string>().ToList();

        Assert.Equal(2, headers.Count);
        Assert.Equal(headers.Count, headers.Distinct().Count());
    }
}
