namespace ChainRenamer.Engine;

/// <summary>
/// The ordered chain of steps. Each step's output is cached; any change only
/// invalidates the cache from the first affected position onwards.
/// Step 0 is the source: it cannot be moved or removed, and nothing goes before it.
/// </summary>
public sealed class Pipeline
{
    private readonly List<Step> steps = [];
    private readonly List<StepResult?> results = [];
    private IReadOnlyList<Item> initial = [];

    public Pipeline(IModule source) => InsertAt(0, new Step(StepId.New(), source));

    /// <summary>A saved chain placed after a source, steps as saved (names, disabled steps).</summary>
    public Pipeline(IModule source, Chain chain) : this(source)
    {
        foreach (var step in chain.Steps) InsertAt(steps.Count, step);
    }

    public IReadOnlyList<Step> Steps => steps;
    public IReadOnlyList<FileItem> Files { get; private set; } = [];

    /// <summary>Output of the last step: the final names, in final order.</summary>
    public IReadOnlyList<Item> Output => Compute(steps.Count - 1).Items;

    public void SetFiles(IEnumerable<FileItem> files)
    {
        Files = [.. files];
        initial = [.. Files.Select(f => new Item(f.Id, f.Name) { Folder = f.Directory, Size = f.Size, Modified = f.Modified })];
        InvalidateFrom(0);
    }

    public Step Add(IModule module, string? name = null) => Insert(steps.Count, module, name);

    public Step Insert(int index, IModule module, string? name = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, steps.Count);
        return InsertAt(index, new Step(StepId.New(), module, name));
    }

    /// <summary>Replaces a step by a modified copy, e.g. <c>pipeline.Update(step with { IsEnabled = false })</c>.</summary>
    public void Update(Step step)
    {
        int i = IndexOf(step.Id);
        bool onlyRenamed = steps[i] with { Name = step.Name } == step;
        steps[i] = step;
        if (!onlyRenamed) InvalidateFrom(i);
    }

    public void Remove(StepId id)
    {
        int i = IndexOfMovable(id);
        steps.RemoveAt(i);
        results.RemoveAt(i);
        InvalidateFrom(i);
    }

    public void Move(StepId id, int newIndex)
    {
        int i = IndexOfMovable(id);
        ArgumentOutOfRangeException.ThrowIfLessThan(newIndex, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(newIndex, steps.Count);
        if (i == newIndex) return;

        var step = steps[i];
        steps.RemoveAt(i);
        steps.Insert(newIndex, step);
        InvalidateFrom(Math.Min(i, newIndex));
    }

    public IReadOnlyList<Item> GetInput(StepId id) => IndexOf(id) is var i and > 0 ? Compute(i - 1).Items : initial;

    public IReadOnlyList<Item> GetOutput(StepId id) => Compute(IndexOf(id)).Items;

    public StepResult GetResult(StepId id) => Compute(IndexOf(id));

    private Step InsertAt(int index, Step step)
    {
        steps.Insert(index, step);
        results.Insert(index, null);
        InvalidateFrom(index);
        return step;
    }

    private StepResult Compute(int index)
    {
        if (results[index] is { } cached) return cached;

        var input = index == 0 ? initial : Compute(index - 1).Items;
        var step = steps[index];
        return results[index] = step.IsEnabled ? Apply(step, input) : new(input);
    }

    // A failing module (e.g. a regex still being typed) must not break the whole preview:
    // the step keeps its input and reports the error.
    private static StepResult Apply(Step step, IReadOnlyList<Item> input)
    {
        try { return new(Run(step, input)); }
        catch (Exception ex) { return new(input, ex); }
    }

    // Skipped files are hidden from a name module, then put back unchanged, at their rank.
    private static IReadOnlyList<Item> Run(Step step, IReadOnlyList<Item> input)
    {
        if (step.Skipped.IsEmpty || step.Module is not INameModule) return step.Module.Apply(input);

        var renamed = step.Module.Apply([.. input.Where(i => !step.Skipped.Contains(i.FileId))]).ToDictionary(i => i.FileId);
        return [.. input.Select(i => renamed.GetValueOrDefault(i.FileId, i))];
    }

    private void InvalidateFrom(int index)
    {
        for (int i = index; i < results.Count; i++) results[i] = null;
    }

    private int IndexOf(StepId id) =>
        steps.FindIndex(s => s.Id == id) is var i and >= 0 ? i : throw new KeyNotFoundException($"Unknown step {id}.");

    private int IndexOfMovable(StepId id) =>
        IndexOf(id) is var i and > 0 ? i : throw new InvalidOperationException("The source step cannot be moved or removed.");
}
