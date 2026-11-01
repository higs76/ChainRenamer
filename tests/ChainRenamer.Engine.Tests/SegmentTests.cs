using ChainRenamer.Engine.Modules;

namespace ChainRenamer.Engine.Tests;

public class SegmentTests
{
    private static string[] Run(IModule module, Segment segment, params string[] names)
    {
        var pipeline = new Pipeline(new SourceModule(@"C:\videos"));
        pipeline.SetFiles(names.Select(n => FileItem.FromPath(@"C:\videos\" + n)));
        var step = pipeline.Add(module);
        pipeline.Update(step with { Segment = segment });
        return pipeline.Output.Names();
    }

    [Fact]
    public void A_step_works_after_the_last_separator_only()
    {
        Assert.Equal(["SPEEDY - 01x01 - The First Lap.mkv"],
            Run(new CaseModule(SmallWords: SmallWords.English), new(" - ", LastAfter: true), "SPEEDY - 01x01 - the first lap.mkv"));
    }

    [Fact]
    public void A_step_works_before_a_text_only()
    {
        // The year and the tags after " (" are spared.
        Assert.Equal(["Le Fabuleux Destin (2001) [1080p].mkv"],
            Run(new ReplaceModule(".", " "), new(Before: " ("), "Le.Fabuleux.Destin (2001) [1080p].mkv"));
    }

    [Fact]
    public void A_step_works_between_two_texts()
    {
        Assert.Equal(["Film (2001) [1080P].mkv"], Run(new CaseModule(TextCase.Upper), new("[", "]"), "Film (2001) [1080p].mkv"));
    }

    [Fact]
    public void A_name_without_the_texts_is_left_alone()
    {
        Assert.Equal(["Trailer.mkv", "a - B.mkv"], Run(new CaseModule(TextCase.Upper), new(" - "), "Trailer.mkv", "a - b.mkv"));
    }

    [Fact]
    public void The_extension_still_belongs_to_the_module()
    {
        Assert.Equal(["a - b.MKV"], Run(new ExtensionModule(ExtensionAction.Upper), new(" - "), "a - b.mkv"));
    }

    [Fact]
    public void A_list_module_ignores_the_segment()
    {
        Assert.Equal(["a.mkv", "b.mkv"], Run(new SortModule(), new("x"), "b.mkv", "a.mkv"));
    }

    [Fact]
    public void Splits_the_name_in_three()
    {
        Assert.Equal(("a - ", "b", " - c"), new Segment(" - ", " - ").Split("a - b - c"));
        Assert.Equal(("a - b - ", "c", ""), new Segment(" - ", LastAfter: true).Split("a - b - c"));
        Assert.Null(new Segment(Before: "x").Split("abc"));
        Assert.True(new Segment().IsWhole);
    }

    [Fact]
    public void The_segment_is_saved_with_the_chain()
    {
        var chain = new Chain([new Step(StepId.New(), new CaseModule()) { Segment = new(" - ", "(", LastAfter: true) }, new Step(StepId.New(), new CaseModule())]);

        var loaded = Chain.FromJson(chain.ToJson());

        Assert.Equal(new Segment(" - ", "(", LastAfter: true), loaded.Steps[0].Segment);
        Assert.Null(loaded.Steps[1].Segment);
        Assert.Single(chain.ToJson().Split('\n'), line => line.Contains("\"segment\""));     // only the step that has one
    }
}
