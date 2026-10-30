namespace ChainRenamer.Engine;

/// <summary>
/// The ordered chain of steps. Each step's output is cached; any change only
/// invalidates the cache from the first affected position onwards.
/// Step 0 is the source: it cannot be moved or removed, and nothing goes before it.
/// </summary>
public sealed class Pipeline
{
    private readonly List<Step> steps = [];
    private readonly List<IReadOnlyList<Item>?> outputs = [];
    private IReadOnlyList<Item> initial = [];

    public Pipeline(IModule source) => InsertAt(0, new Step(StepId.New(), source));

    public IReadOnlyList<Step> Steps => steps;
    public IReadOnlyList<FileItem> Files { get; private set; } = [];

    /// <summary>Output of the last step: the final names, in final order.</summary>
    public IReadOnlyList<Item> Output => Compute(steps.Count - 1);

    public void SetFiles(IEnumerable<FileItem> files)
    {
        Files = [.. files];
        initial = [.. Files.Select(f => new Item(f.Id, f.Name))];
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
        outputs.RemoveAt(i);
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

    public IReadOnlyList<Item> GetInput(StepId id) => IndexOf(id) is var i and > 0 ? Compute(i - 1) : initial;

    public IReadOnlyList<Item> GetOutput(StepId id) => Compute(IndexOf(id));

    private Step InsertAt(int index, Step step)
    {
        steps.Insert(index, step);
        outputs.Insert(index, null);
        InvalidateFrom(index);
        return step;
    }

    private IReadOnlyList<Item> Compute(int index)
    {
        if (outputs[index] is { } cached) return cached;

        var input = index == 0 ? initial : Compute(index - 1);
        var step = steps[index];
        return outputs[index] = step.IsEnabled ? step.Module.Apply(input) : input;
    }

    private void InvalidateFrom(int index)
    {
        for (int i = index; i < outputs.Count; i++) outputs[i] = null;
    }

    private int IndexOf(StepId id) =>
        steps.FindIndex(s => s.Id == id) is var i and >= 0 ? i : throw new KeyNotFoundException($"Unknown step {id}.");

    private int IndexOfMovable(StepId id) =>
        IndexOf(id) is var i and > 0 ? i : throw new InvalidOperationException("The source step cannot be moved or removed.");
}
