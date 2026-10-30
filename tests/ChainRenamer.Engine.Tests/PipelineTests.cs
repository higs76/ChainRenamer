namespace ChainRenamer.Engine.Tests;

public class PipelineTests
{
    /// <summary>Appends a suffix to every name and counts its calls.</summary>
    private sealed class SuffixModule(string suffix) : IModule
    {
        public int Calls { get; private set; }

        public IReadOnlyList<Item> Apply(IReadOnlyList<Item> input)
        {
            Calls++;
            return [.. input.Select(i => i with { Name = i.Name + suffix })];
        }
    }

    private readonly SuffixModule source = new("");
    private readonly SuffixModule a = new("-a");
    private readonly SuffixModule b = new("-b");
    private readonly SuffixModule c = new("-c");
    private readonly Pipeline pipeline;
    private readonly Step stepA, stepB, stepC;

    public PipelineTests()
    {
        pipeline = new Pipeline(source);
        pipeline.SetFiles([FileItem.FromPath(@"C:\photos\x"), FileItem.FromPath(@"C:\photos\y")]);
        stepA = pipeline.Add(a);
        stepB = pipeline.Add(b);
        stepC = pipeline.Add(c);
    }

    private string[] Names(IReadOnlyList<Item> items) => [.. items.Select(i => i.Name)];
    private int[] Calls() => [source.Calls, a.Calls, b.Calls, c.Calls];

    [Fact]
    public void Steps_apply_in_order()
    {
        Assert.Equal(["x-a-b-c", "y-a-b-c"], Names(pipeline.Output));
    }

    [Fact]
    public void Input_of_a_step_is_output_of_the_previous_one()
    {
        Assert.Equal(["x-a", "y-a"], Names(pipeline.GetInput(stepB.Id)));
        Assert.Equal(["x-a-b", "y-a-b"], Names(pipeline.GetOutput(stepB.Id)));
    }

    [Fact]
    public void Input_of_the_source_is_the_original_file_names()
    {
        Assert.Equal(["x", "y"], Names(pipeline.GetInput(pipeline.Steps[0].Id)));
    }

    [Fact]
    public void Disabled_step_copies_its_input()
    {
        pipeline.Update(stepB with { IsEnabled = false });

        Assert.Same(pipeline.GetInput(stepB.Id), pipeline.GetOutput(stepB.Id));
        Assert.Equal(["x-a-c", "y-a-c"], Names(pipeline.Output));
        Assert.Equal(0, b.Calls);
    }

    [Fact]
    public void Outputs_are_cached()
    {
        _ = pipeline.Output;
        _ = pipeline.Output;
        _ = pipeline.GetOutput(stepA.Id);

        Assert.Equal([1, 1, 1, 1], Calls());
    }

    [Fact]
    public void Only_computes_up_to_the_requested_step()
    {
        _ = pipeline.GetOutput(stepA.Id);

        Assert.Equal([1, 1, 0, 0], Calls());
    }

    [Fact]
    public void Updating_a_step_recomputes_from_that_step()
    {
        _ = pipeline.Output;
        var b2 = new SuffixModule("-B");
        pipeline.Update(stepB with { Module = b2 });

        Assert.Equal(["x-a-B-c", "y-a-B-c"], Names(pipeline.Output));
        Assert.Equal([1, 1, 1, 2], Calls());
        Assert.Equal(1, b2.Calls);
    }

    [Fact]
    public void Renaming_a_step_recomputes_nothing()
    {
        _ = pipeline.Output;
        pipeline.Update(stepB with { Name = "Suffix B" });
        _ = pipeline.Output;

        Assert.Equal("Suffix B", pipeline.Steps[2].Name);
        Assert.Equal([1, 1, 1, 1], Calls());
    }

    [Fact]
    public void Moving_a_step_down_recomputes_from_its_old_position()
    {
        _ = pipeline.Output;
        pipeline.Move(stepA.Id, 3);

        Assert.Equal(["x-b-c-a", "y-b-c-a"], Names(pipeline.Output));
        Assert.Equal([1, 2, 2, 2], Calls());
    }

    [Fact]
    public void Moving_a_step_up_recomputes_from_its_new_position()
    {
        _ = pipeline.Output;
        pipeline.Move(stepC.Id, 2);

        Assert.Equal(["x-a-c-b", "y-a-c-b"], Names(pipeline.Output));
        Assert.Equal([1, 1, 2, 2], Calls());
    }

    [Fact]
    public void Steps_keep_their_id_when_moved()
    {
        pipeline.Move(stepA.Id, 3);

        Assert.Equal([stepB.Id, stepC.Id, stepA.Id], pipeline.Steps.Skip(1).Select(s => s.Id));
    }

    [Fact]
    public void Removing_a_step_recomputes_the_following_ones()
    {
        _ = pipeline.Output;
        pipeline.Remove(stepB.Id);

        Assert.Equal(["x-a-c", "y-a-c"], Names(pipeline.Output));
        Assert.Equal([1, 1, 1, 2], Calls());
    }

    [Fact]
    public void Inserting_a_step_recomputes_from_its_position()
    {
        _ = pipeline.Output;
        pipeline.Insert(2, new SuffixModule("-i"));

        Assert.Equal(["x-a-i-b-c", "y-a-i-b-c"], Names(pipeline.Output));
        Assert.Equal([1, 1, 2, 2], Calls());
    }

    [Fact]
    public void Changing_the_files_recomputes_everything()
    {
        _ = pipeline.Output;
        pipeline.SetFiles([FileItem.FromPath(@"\\server\share\z")]);

        Assert.Equal(["z-a-b-c"], Names(pipeline.Output));
        Assert.Equal([2, 2, 2, 2], Calls());
    }

    [Fact]
    public void The_source_cannot_be_moved_or_removed()
    {
        var sourceId = pipeline.Steps[0].Id;

        Assert.Throws<InvalidOperationException>(() => pipeline.Move(sourceId, 2));
        Assert.Throws<InvalidOperationException>(() => pipeline.Remove(sourceId));
    }

    [Fact]
    public void Nothing_can_go_before_the_source()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => pipeline.Insert(0, new SuffixModule("-z")));
        Assert.Throws<ArgumentOutOfRangeException>(() => pipeline.Move(stepA.Id, 0));
    }

    private sealed class FailingModule : IModule
    {
        public IReadOnlyList<Item> Apply(IReadOnlyList<Item> input) => throw new ArgumentException("bad pattern");
    }

    [Fact]
    public void A_failing_step_reports_its_error_and_copies_its_input()
    {
        pipeline.Update(stepB with { Module = new FailingModule() });

        var result = pipeline.GetResult(stepB.Id);

        Assert.True(result.Failed);
        Assert.IsType<ArgumentException>(result.Error);
        Assert.Same(pipeline.GetInput(stepB.Id), result.Items);
        Assert.Equal(["x-a-c", "y-a-c"], Names(pipeline.Output));
    }

    [Fact]
    public void Fixing_a_failing_step_clears_its_error()
    {
        pipeline.Update(stepB with { Module = new FailingModule() });
        _ = pipeline.Output;
        pipeline.Update(stepB);

        Assert.False(pipeline.GetResult(stepB.Id).Failed);
        Assert.Equal(["x-a-b-c", "y-a-b-c"], Names(pipeline.Output));
    }

    [Fact]
    public void A_disabled_failing_step_is_not_in_error()
    {
        pipeline.Update(stepB with { Module = new FailingModule(), IsEnabled = false });

        Assert.False(pipeline.GetResult(stepB.Id).Failed);
    }

    [Fact]
    public void Unknown_step_is_rejected()
    {
        Assert.Throws<KeyNotFoundException>(() => pipeline.GetOutput(StepId.New()));
    }
}
