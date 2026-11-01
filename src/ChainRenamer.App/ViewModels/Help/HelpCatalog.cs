using ChainRenamer.App.Resources;
using ChainRenamer.Engine.Modules;
using H = ChainRenamer.App.Resources.HelpTexts;

namespace ChainRenamer.App.ViewModels.Help;

/// <summary>
/// The pages of the help, in the order of the list. Adding a module: its page here, its texts in HelpTexts.resx.
/// The labels of the settings come from Strings.resx, so that the help uses the words of the screen.
/// </summary>
public static class HelpCatalog
{
    public const string Home = "Chain";

    /// <summary>Built on each call, in the current language of the interface.</summary>
    public static IReadOnlyList<HelpPage> Pages() =>
    [
        new("Chain", H.Group_Start, H.Chain_Title, H.Chain_Intro),
        new("Selection", H.Group_Start, H.Selection_Title, H.Selection_Intro),
        new("Apply", H.Group_Start, H.Apply_Title, H.Apply_Intro),
        new("Library", H.Group_Start, H.Library_Title, H.Library_Intro),
        new("Shortcuts", H.Group_Start, H.Shortcuts_Title, H.Shortcuts_Intro)
        {
            OptionsTitle = null,
            Options =
            [
                new(H.Key_F1, H.Key_F1_Text),
                new(H.Key_New, H.Key_New_Text),
                new(H.Key_Save, H.Key_Save_Text),
                new(H.Key_SaveAs, H.Key_SaveAs_Text),
                new(H.Key_Favorite, H.Key_Favorite_Text),
                new(H.Key_Space, H.Key_Space_Text),
                new(H.Key_Range, H.Key_Range_Text),
                new(H.Key_Rectangle, H.Key_Rectangle_Text),
            ],
        },

        new("Source", H.Group_Modules, Strings.Module_Source, H.Source_Intro)
        {
            Module = typeof(SourceModule),
            Options = [new(Strings.Source_Folder, H.Source_Folder_Text), new(Strings.Source_Subfolders, H.Source_Subfolders_Text)],
        },
        new("Sort", H.Group_Modules, Strings.Module_Sort, H.Sort_Intro)
        {
            Module = typeof(SortModule),
            Options = [new(Strings.Sort_Descending, H.Sort_Descending_Text)],
            Examples =
            [
                new(H.Sort_Example_Natural, new SortModule(), "Episode 10.mkv", "Episode 2.mkv", "Episode 1.mkv"),
                new(H.Sort_Example_Descending, new SortModule(Descending: true), "Photo 1.jpg", "Photo 2.jpg", "Photo 10.jpg"),
            ],
        },
        new("Replace", H.Group_Modules, Strings.Module_Replace, H.Replace_Intro)
        {
            Module = typeof(ReplaceModule),
            Options =
            [
                new(Strings.Replace_Find, H.Replace_Find_Text),
                new(H.Replace_With_Label, H.Replace_With_Text),
                new(Strings.Replace_MatchCase, H.Replace_MatchCase_Text),
                Choice(Strings.Occurrence_All, H.Occurrence_All_Text),
                Choice(Strings.Occurrence_First, H.Occurrence_First_Text),
                Choice(Strings.Occurrence_Last, H.Occurrence_Last_Text),
                Choice(Strings.Occurrence_Start, H.Occurrence_Start_Text),
                Choice(Strings.Occurrence_End, H.Occurrence_End_Text),
            ],
            Examples =
            [
                new(H.Replace_Example_Dots, new ReplaceModule(".", " "), "speedy.01x03.my.first.race.mkv"),
                new(H.Replace_Example_Last, new ReplaceModule(" - ", ". ", Occurrence: Occurrence.Last), "Queen - Live Aid - 01 - Intro.mp3"),
                new(H.Replace_Example_Start, new ReplaceModule("[HD] ", "", Occurrence: Occurrence.Start), "[HD] Speedy 01.mkv", "Speedy [HD] 02.mkv"),
                new(H.Replace_Example_Case, new ReplaceModule("EP", "Episode ", MatchCase: true), "Speedy EP01.mkv", "Step by step.mkv"),
            ],
        },
        new("RegexReplace", H.Group_Modules, Strings.Module_RegexReplace, H.Regex_Intro)
        {
            Module = typeof(RegexReplaceModule),
            Options =
            [
                new(Strings.Regex_Pattern, H.Regex_Pattern_Text),
                new(Strings.Regex_With, H.Regex_With_Text),
                new(Strings.Replace_MatchCase, H.Regex_MatchCase_Text),
            ],
            Examples =
            [
                new(H.Regex_Example_Season, new RegexReplaceModule(@"(\d+)x(\d+)", "S$1E$2"), "Speedy 01x03.mkv", "Speedy 1x12.mkv"),
                new(H.Regex_Example_Swap, new RegexReplaceModule("^(.+) - (.+)$", "$2 - $1"), "Queen - Bohemian Rhapsody.mp3"),
                new(H.Regex_Example_Brackets, new RegexReplaceModule(@"\s*\[.*?\]", ""), "Speedy 01 [1080p] [x265].mkv"),
                new(H.Regex_Example_Spaces, new RegexReplaceModule(@"\s+", " "), "Speedy   01  final.mkv"),
            ],
        },
    ];

    /// <summary>The page explaining a module, or the home page.</summary>
    public static string KeyOf(Type? module) => Pages().FirstOrDefault(p => p.Module is not null && p.Module == module)?.Key ?? Home;

    // "Occurrences: First"
    private static HelpOption Choice(string choice, string text) => new(string.Format(H.Choice, Strings.Replace_Occurrence, choice), text);
}
