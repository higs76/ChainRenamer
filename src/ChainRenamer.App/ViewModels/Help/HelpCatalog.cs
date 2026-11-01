using ChainRenamer.App.Resources;
using ChainRenamer.Engine;
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
        new("Variables", H.Group_Start, H.Variables_Title, H.Variables_Intro + "\n\n" + H.Variables_Metadata)
        {
            OptionsTitle = null,
            Options =
            [
                new("{name}", H.Variable_Name_Text),
                new("{ext}", H.Variable_Ext_Text),
                new("{folder}", H.Variable_Folder_Text),
                new("{n}", H.Variable_Number_Text),
                new("{date}", H.Variable_Date_Text),
                new("{today}", H.Variable_Today_Text),
                new("{size}", H.Variable_Size_Text),
                new(H.Variable_Taken_Label, H.Variable_Taken_Text),
                new(H.Variable_Photo_Label, H.Variable_Photo_Text),
                new(H.Variable_Music_Label, H.Variable_Music_Text),
                new(H.Variable_Captured_Label, H.Variable_Captured_Text),
            ],
            Examples =
            [
                new(H.Template_Example_Folders, new TemplateModule("{folder:2} - {folder} - {n:2}"), @"Speedy\Season 1\speedy.1x01.mkv", @"Speedy\Season 1\speedy.1x02.mkv"),
            ],
        },
        new("Segment", H.Group_Start, H.Segment_Title, H.Segment_Intro)
        {
            OptionsTitle = null,
            Options = [new(Strings.Segment_After, H.Segment_After_Text), new(Strings.Segment_Before, H.Segment_Before_Text)],
            Examples =
            [
                new(H.Segment_Example, new Step(StepId.New(), new CaseModule(SmallWords: SmallWords.English)) { Segment = new(" - ", LastAfter: true) }.AsModule(),
                    "SPEEDY - 01x01 - the first lap.mkv", "SPEEDY - 01x02 - rain on the track.mkv"),
            ],
        },
        new("Markers", H.Group_Start, H.Markers_Title, H.Markers_Intro)
        {
            Examples =
            [
                new(H.Markers_Example, Then(
                        new RegexReplaceModule(@"(\d+x\d+)", "$1@@"),
                        new ListModule(" - The First Lap\n - Rain on the Track", ListMode.Insert, InsertAt.Before, Anchor: "@@"),
                        new ReplaceModule("@@", "")),
                    "Speedy 1x01 [1080p].mkv", "Speedy 1x02 VOSTFR.mkv"),
            ],
        },
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
            Options = [new(Strings.Sort_By, H.SortBy_Text), new(Strings.Sort_Descending, H.Sort_Descending_Text)],
            Examples =
            [
                new(H.Sort_Example_Natural, new SortModule(), "Episode 10.mkv", "Episode 2.mkv", "Episode 1.mkv"),
                new(H.Sort_Example_Descending, new SortModule(Descending: true), "Photo 1.jpg", "Photo 2.jpg", "Photo 10.jpg"),
            ],
        },
        new("Filter", H.Group_Modules, Strings.Module_Filter, H.Filter_Intro)
        {
            Module = typeof(FilterModule),
            Options =
            [
                new(Strings.FilterMode_Keep, H.FilterMode_Keep_Text),
                new(Strings.FilterMode_Remove, H.FilterMode_Remove_Text),
                new(Strings.FilterOn_Name, H.FilterOn_Name_Text),
                new(Strings.TextTest_Wildcard, H.TextTest_Wildcard_Text),
                new(Strings.TextTest_Regex, H.TextTest_Regex_Text),
                new(Strings.FilterOn_Extension, H.FilterOn_Extension_Text),
                new(Strings.FilterOn_Size, H.FilterOn_Size_Text),
                new(Strings.FilterOn_Modified, H.FilterOn_Modified_Text),
                new(Strings.FilterOn_Length, H.FilterOn_Length_Text),
                new(Strings.Replace_MatchCase, H.Filter_MatchCase_Text),
            ],
            Examples =
            [
                new(H.Filter_Example_Contains, new FilterModule(Text: "VOSTFR"), "Speedy 01 VOSTFR.mkv", "Speedy 01 VF.mkv", "Speedy 02 vostfr.mkv"),
                new(H.Filter_Example_Wildcard, new FilterModule(Test: TextTest.Wildcard, Text: "*.mkv;*.mp4"), "Speedy 01.mkv", "Speedy 01.srt", "Trailer.mp4", "cover.jpg"),
                new(H.Filter_Example_Remove, new FilterModule(FilterOn.Extension, Text: "nfo, txt", Exclude: true), "Speedy 01.mkv", "Speedy.nfo", "readme.txt"),
                new(H.Filter_Example_Regex, new FilterModule(Test: TextTest.Regex, Text: @"\bS\d+E\d+\b"), "Speedy S01E01.mkv", "Speedy - Making of.mkv"),
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
        new("ReplaceTable", H.Group_Modules, Strings.Module_ReplaceTable, H.ReplaceTable_Intro)
        {
            Module = typeof(ReplaceTableModule),
            Options =
            [
                new($"{Strings.ReplaceTable_Find} → {Strings.ReplaceTable_With}", H.ReplaceTable_Lines_Text),
                new(Strings.ReplaceTable_Paste, H.ReplaceTable_Paste_Text),
                new($"{Strings.ReplaceTable_Load} / {Strings.ReplaceTable_Save}", H.ReplaceTable_Files_Text),
                new(Strings.Replace_MatchCase, H.ReplaceTable_MatchCase_Text),
            ],
            Examples =
            [
                new(H.ReplaceTable_Example_Tags, new ReplaceTableModule([new("VOSTFR", "VOST"), new(" [1080p]", "")]), "Speedy 01 VOSTFR [1080p].mkv"),
                new(H.ReplaceTable_Example_Chain, new ReplaceTableModule([new("S01", "Saison 1"), new("Saison", "Season")]), "Speedy S01 E03.mkv"),
                new(H.ReplaceTable_Example_Letters, new ReplaceTableModule([new("ß", "ss"), new("æ", "ae"), new("ø", "o"), new("Ø", "O")], MatchCase: true), "Straße.txt", "Kærlighed på Ø.mp3"),
            ],
        },
        new("Template", H.Group_Modules, Strings.Module_Template, H.Template_Intro)
        {
            Module = typeof(TemplateModule),
            Options = [new(Strings.Template_Label, H.Template_Text_Text), new(Strings.Template_AddVariable, H.Variables_Title)],
            Examples =
            [
                new(H.Template_Example_Folders, new TemplateModule("{folder:2} - {folder} - {n:2}"), @"Speedy\Season 1\speedy.1x01.mkv", @"Speedy\Season 1\speedy.1x02.mkv"),
                new(H.Template_Example_Date, new TemplateModule("{n:3} - {name}"), "Holiday.jpg", "Beach.jpg"),
            ],
        },
        new("Capture", H.Group_Modules, Strings.Module_Capture, H.Capture_Intro)
        {
            Module = typeof(CaptureModule),
            Options = [new(Strings.Capture_Pattern, H.Capture_Pattern_Text), new(Strings.Replace_MatchCase, H.Capture_MatchCase_Text)],
            Examples =
            [
                new(H.Capture_Example_Template, Then(new CaptureModule(@"(?<season>\d+)x(?<episode>\d+)"), new TemplateModule("S{season}E{episode}")),
                    "speedy.1x03.mkv", "speedy.2x11.mkv"),
            ],
        },
        new("List", H.Group_Modules, Strings.Module_List, H.List_Intro)
        {
            Module = typeof(ListModule),
            Options =
            [
                new(Strings.ListMode_ReplaceName, H.ListMode_ReplaceName_Text),
                new(Strings.ListMode_Insert, H.ListMode_Insert_Text),
                new(Strings.ListMode_Pairs, H.ListMode_Pairs_Text),
                new(Strings.Replace_MatchCase, H.List_MatchCase_Text),
            ],
            Examples =
            [
                new(H.List_Example_Titles, new ListModule("The First Lap\nRain on the Track\nA New Rival"), "Speedy 01.mkv", "Speedy 02.mkv", "Speedy 03.mkv"),
                new(H.List_Example_Insert, new ListModule(" - The First Lap\n - Rain on the Track", ListMode.Insert), "Speedy 01.mkv", "Speedy 02.mkv"),
                new(H.List_Example_Pairs, new ListModule("Speedy 02\tFinale\nSpeedy 01\tPilot", ListMode.Pairs), "Speedy 01.mkv", "Speedy 02.mkv"),
            ],
        },
        new("Case", H.Group_Modules, Strings.Module_Case, H.Case_Intro)
        {
            Module = typeof(CaseModule),
            Options =
            [
                new(Strings.Case_Mode, H.Case_Mode_Text),
                new(Strings.Case_SmallWords, H.Case_SmallWords_Text),
                new(Strings.Case_Exceptions, H.Case_Exceptions_Text),
                new(Strings.Case_Roman, H.Case_Roman_Text),
                new(Strings.Case_Acronyms, H.Case_Acronyms_Text),
                new(Strings.Case_SpaceBeforeCapitals, H.Case_SpaceBeforeCapitals_Text),
            ],
            Examples =
            [
                new(H.Case_Example_Title, new CaseModule(SmallWords: SmallWords.English), "the lord of the rings.mkv"),
                new(H.Case_Example_French, new CaseModule(SmallWords: SmallWords.French), "le retour de l'épisode.mkv"),
                new(H.Case_Example_Roman, new CaseModule(SmallWords: SmallWords.English), "rocky iv on DVD.mkv"),
                new(H.Case_Example_Upper, new CaseModule(Exceptions: "iPhone, TV"), "MY IPHONE ON TV.mp4"),
                new(H.Case_Example_Glued, new CaseModule(SpaceBeforeCapitals: true), "MyHolidayInParis.jpg"),
                new(H.Case_Example_Sentence, new CaseModule(TextCase.Sentence), "THE MATRIX RELOADED.mkv"),
            ],
        },
        new("Remove", H.Group_Modules, Strings.Module_Remove, H.Remove_Intro)
        {
            Module = typeof(RemoveModule),
            Options =
            [
                Choice(Strings.Remove_What, Strings.RemoveWhat_First, H.RemoveWhat_First_Text),
                Choice(Strings.Remove_What, Strings.RemoveWhat_Last, H.RemoveWhat_Last_Text),
                Choice(Strings.Remove_What, Strings.RemoveWhat_Range, H.RemoveWhat_Range_Text),
                Choice(Strings.Remove_What, Strings.RemoveWhat_Before, H.RemoveWhat_Before_Text),
                Choice(Strings.Remove_What, Strings.RemoveWhat_After, H.RemoveWhat_After_Text),
                Choice(Strings.Remove_What, Strings.RemoveWhat_Between, H.RemoveWhat_Between_Text),
                new(Strings.Remove_IncludeText, H.Remove_IncludeText_Text),
                new(Strings.Remove_Last, H.Remove_Last_Text),
                new(Strings.Replace_MatchCase, H.Remove_MatchCase_Text),
            ],
            Examples =
            [
                new(H.Remove_Example_First, new RemoveModule(RemoveWhat.First, Count: 4), "001 Speedy 01.mkv"),
                new(H.Remove_Example_Range, new RemoveModule(RemoveWhat.Range, From: -3, To: -1), "Holiday 2024 (1).jpg"),
                new(H.Remove_Example_Before, new RemoveModule(RemoveWhat.Before, Text: " - "), "Queen - Bohemian Rhapsody.mp3"),
                new(H.Remove_Example_After, new RemoveModule(RemoveWhat.After, Text: " ["), "Speedy 01 [1080p] [x265].mkv"),
                new(H.Remove_Example_Last, new RemoveModule(RemoveWhat.After, Text: " - ", LastOccurrence: true), "Speedy - 01 - My first race.mkv"),
                new(H.Remove_Example_Between, new RemoveModule(RemoveWhat.Between, Text: "[", EndText: "]"), "Speedy [VOSTFR] 01 [1080p].mkv"),
            ],
        },
        new("Insert", H.Group_Modules, Strings.Module_Insert, H.Insert_Intro)
        {
            Module = typeof(InsertModule),
            Options =
            [
                new(Strings.Insert_Text, H.Insert_Text_Text),
                new(Strings.InsertAt_Start, H.InsertAt_Start_Text),
                new(Strings.InsertAt_End, H.InsertAt_End_Text),
                new(Strings.InsertAt_Position, H.InsertAt_Position_Text),
                new(Strings.InsertAt_Before, H.InsertAt_Before_Text),
                new(Strings.InsertAt_After, H.InsertAt_After_Text),
                new(H.Variables_Title, H.Insert_Variables_Text),
            ],
            Examples =
            [
                new(H.Insert_Example_Start, new InsertModule("[HD] ", InsertAt.Start), "Speedy 01.mkv"),
                new(H.Insert_Example_End, new InsertModule(" (2019)", InsertAt.End), "Parasite.mkv"),
                new(H.Insert_Example_Position, new InsertModule(" ", InsertAt.Position, 4), "2024Holiday.jpg"),
                new(H.Insert_Example_Before, new InsertModule("S01", InsertAt.Before, Anchor: "E", MatchCase: true), "Speedy E03.mkv"),
                new(H.Insert_Example_After, new InsertModule("(Live) ", InsertAt.After, Anchor: " - "), "Queen - Bohemian Rhapsody.mp3"),
                new(H.Insert_Example_Variable, new InsertModule("[{folder}] ", InsertAt.Start), @"Holiday 2024\IMG_0042.jpg"),
            ],
        },
        new("Numbering", H.Group_Modules, Strings.Module_Numbering, H.Numbering_Intro)
        {
            Module = typeof(NumberingModule),
            Options =
            [
                new(Strings.Numbering_Style, H.Numbering_Style_Text),
                new(Strings.Numbering_Start, H.Numbering_Start_Text),
                new(Strings.Numbering_Step, H.Numbering_Step_Text),
                new(Strings.Numbering_Digits, H.Numbering_Digits_Text),
                new(Strings.Numbering_Where, H.Numbering_Where_Text),
                new(Strings.Numbering_Separator, H.Numbering_Separator_Text),
                new(Strings.Numbering_Reset, H.Numbering_Reset_Text),
                new(Strings.Numbering_ResetOn, H.Numbering_ResetOn_Text),
            ],
            Examples =
            [
                new(H.Numbering_Example_Default, new NumberingModule(), "Paris.jpg", "London.jpg", "Rome.jpg"),
                new(H.Numbering_Example_End, new NumberingModule(Digits: 3, Where: InsertAt.End, Separator: " "), "Holiday.jpg", "Holiday.jpg"),
                new(H.Numbering_Example_After, new NumberingModule(Where: InsertAt.After, Anchor: "S01E"), "Speedy S01E.mkv", "Speedy S01E.mkv"),
                new(H.Numbering_Example_Reset, new NumberingModule(ResetPerFolder: true), @"Season 1\Start.mkv", @"Season 1\Race.mkv", @"Season 2\Return.mkv"),
                new(H.Numbering_Example_ResetOn, Then(new CaptureModule(@"S(?<season>\d+)"), new NumberingModule(Where: InsertAt.End, ResetOn: "season")),
                    "Speedy S1 Start.mkv", "Speedy S1 Race.mkv", "Speedy S2 Return.mkv"),
                new(H.Numbering_Example_Roman, new NumberingModule(Style: NumberStyle.UpperRoman, Where: InsertAt.End, Separator: " "), "Rocky.mkv", "Rocky.mkv", "Rocky.mkv"),
            ],
        },
        new("PadNumbers", H.Group_Modules, Strings.Module_PadNumbers, H.PadNumbers_Intro)
        {
            Module = typeof(PadNumbersModule),
            Options =
            [
                Choice(Strings.PadNumbers_Which, Strings.WhichNumbers_All, H.WhichNumbers_All_Text),
                Choice(Strings.PadNumbers_Which, Strings.WhichNumbers_First, H.WhichNumbers_First_Text),
                Choice(Strings.PadNumbers_Which, Strings.WhichNumbers_Last, H.WhichNumbers_Last_Text),
                new(Strings.PadNumbers_Digits, H.PadNumbers_Digits_Text),
                new(Strings.PadNumbers_Automatic, H.PadNumbers_Automatic_Text),
                new(Strings.PadNumbers_TrimZeros, H.PadNumbers_TrimZeros_Text),
            ],
            Examples =
            [
                new(H.PadNumbers_Example_All, new PadNumbersModule(), "Speedy S1E5.mkv", "Track 7.mp3"),
                new(H.PadNumbers_Example_Automatic, new PadNumbersModule(Digits: 0), "Photo 1.jpg", "Photo 20.jpg", "Photo 120.jpg"),
                new(H.PadNumbers_Example_Last, new PadNumbersModule(Which: WhichNumbers.Last), "Speedy 2024 1080p - 7.mkv"),
                new(H.PadNumbers_Example_Trim, new PadNumbersModule(TrimZeros: true), "Episode 007.mkv", "Episode 0012.mkv"),
            ],
        },
        new("Dates", H.Group_Modules, Strings.Module_Dates, H.Dates_Intro)
        {
            Module = typeof(DatesModule),
            Options = [new(Strings.Dates_Order, H.Dates_Order_Text), new(Strings.Dates_Format, H.Dates_Format_Text)],
            Examples =
            [
                new(H.Dates_Example_Iso, new DatesModule(), "Holiday 17.05.2024.jpg", "Birthday 3-1-85.jpg"),
                new(H.Dates_Example_Camera, new DatesModule(DateOrder.YearMonthDay), "IMG_20240517_183200.jpg"),
                new(H.Dates_Example_Year, new DatesModule(Format: "yyyy"), "Report 31.12.2023.pdf"),
            ],
        },
        new("Clean", H.Group_Modules, Strings.Module_Clean, H.Clean_Intro)
        {
            Module = typeof(CleanModule),
            Options =
            [
                new(Strings.Clean_Trim, H.Clean_Trim_Text),
                new(Strings.Clean_Collapse, H.Clean_Collapse_Text),
                new(Strings.Clean_LeadingDots, H.Clean_LeadingDots_Text),
                new(Strings.Clean_Accents, H.Clean_Accents_Text),
                new(Strings.Clean_Digits, H.Clean_Digits_Text),
                new(Strings.Clean_Symbols, H.Clean_Symbols_Text),
                new(Strings.Clean_Emojis, H.Clean_Emojis_Text),
            ],
            Examples =
            [
                new(H.Clean_Example_Default, new CleanModule(), " Speedy  01   final .mkv"),
                new(H.Clean_Example_Accents, new CleanModule(RemoveAccents: true), "Été à Noël, cœur.mkv"),
                new(H.Clean_Example_Emojis, new CleanModule(RemoveEmojis: true), "Holiday 🏖️ 2024 ☀️.jpg"),
                new(H.Clean_Example_Symbols, new CleanModule(RemoveSymbols: true), "Movie™ (2019) ©.mkv"),
            ],
        },
        new("Extension", H.Group_Modules, Strings.Module_Extension, H.Extension_Intro)
        {
            Module = typeof(ExtensionModule),
            Options =
            [
                new(Strings.ExtensionAction_Lower, H.ExtensionAction_Lower_Text),
                new(Strings.ExtensionAction_Upper, H.ExtensionAction_Upper_Text),
                new(Strings.ExtensionAction_Title, H.ExtensionAction_Title_Text),
                new(Strings.ExtensionAction_Set, H.ExtensionAction_Set_Text),
                new(Strings.ExtensionAction_Add, H.ExtensionAction_Add_Text),
                new(Strings.ExtensionAction_Remove, H.ExtensionAction_Remove_Text),
            ],
            Examples =
            [
                new(H.Extension_Example_Lower, new ExtensionModule(ExtensionAction.Lower), "IMG_0042.JPG", "Report.Pdf"),
                new(H.Extension_Example_Set, new ExtensionModule(ExtensionAction.Set, "jpg"), "photo.jpeg", "scan.JPEG"),
                new(H.Extension_Example_Add, new ExtensionModule(ExtensionAction.Add, "gif"), "card.holiday"),
                new(H.Extension_Example_Remove, new ExtensionModule(ExtensionAction.Remove), "notes.txt.bak"),
            ],
        },
    ];

    /// <summary>The page explaining a module, or the home page.</summary>
    public static string KeyOf(Type? module) => Pages().FirstOrDefault(p => p.Module is not null && p.Module == module)?.Key ?? Home;

    /// <summary>Several modules one after the other, for an example that needs a chain (a capture changes no name).</summary>
    private sealed record Chained(IModule[] Modules) : IModule
    {
        public IReadOnlyList<Item> Apply(IReadOnlyList<Item> input) => Modules.Aggregate(input, (items, module) => module.Apply(items));
    }

    private static IModule Then(params IModule[] modules) => new Chained(modules);

    /// <summary>A step seen as a module, for an example that needs the step's options (part of the name).</summary>
    private sealed record StepAsModule(Step Step) : IModule
    {
        public IReadOnlyList<Item> Apply(IReadOnlyList<Item> input) => Step.Apply(input);
    }

    private static IModule AsModule(this Step step) => new StepAsModule(step);

    // "Occurrences: First"
    private static HelpOption Choice(string choice, string text) => Choice(Strings.Replace_Occurrence, choice, text);

    private static HelpOption Choice(string setting, string choice, string text) => new(string.Format(H.Choice, setting, choice), text);
}
