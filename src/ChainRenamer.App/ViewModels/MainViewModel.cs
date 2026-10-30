using System.Collections.ObjectModel;
using ChainRenamer.App.Resources;
using ChainRenamer.App.ViewModels.Modules;
using ChainRenamer.Engine;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChainRenamer.App.ViewModels;

public sealed record AddModuleEntry(string Title, IRelayCommand Command);

public partial class MainViewModel : ViewModelBase
{
    private readonly Pipeline pipeline;
    private readonly SourceSettingsViewModel source = new();
    private Dictionary<FileId, string> originals = [];

    public MainViewModel()
    {
        pipeline = new Pipeline(source.ToModule());
        source.FilesLoaded += files =>
        {
            originals = files.ToDictionary(f => f.Id, f => f.Name);
            pipeline.SetFiles(files);
            Refresh();
        };
        Attach(new StepViewModel(pipeline.Steps[0].Id, source));
        AddEntries = [.. ModuleCatalog.Addable.Select(t => new AddModuleEntry(t.Title, new RelayCommand(() => AddStep(t.Create()))))];
        SelectedStep = Steps[0];
        Refresh();
    }

    public string Title { get; } = $"ChainRenamer {typeof(MainViewModel).Assembly.GetName().Version?.ToString(3)}";

    public ObservableCollection<StepViewModel> Steps { get; } = [];
    public ObservableCollection<PreviewRow> Rows { get; } = [];
    public IReadOnlyList<AddModuleEntry> AddEntries { get; }

    [ObservableProperty] public partial StepViewModel? SelectedStep { get; set; }
    [ObservableProperty] public partial string Summary { get; set; } = "";
    [ObservableProperty] public partial string? Banner { get; set; }
    [ObservableProperty] public partial bool BannerIsError { get; set; }

    partial void OnSelectedStepChanged(StepViewModel? value) => RefreshPreview();

    private void AddStep(ModuleSettingsViewModel settings)
    {
        var step = pipeline.Add(settings.ToModule());
        SelectedStep = Attach(new StepViewModel(step.Id, settings));
        Refresh();
    }

    private StepViewModel Attach(StepViewModel step)
    {
        step.Changed += (_, _) =>
        {
            pipeline.Update(step.ToStep());
            Refresh();
        };
        Steps.Add(step);
        return step;
    }

    [RelayCommand]
    private void RemoveStep(StepViewModel step)
    {
        int index = Steps.IndexOf(step);
        pipeline.Remove(step.Id);
        Steps.Remove(step);
        SelectedStep = Steps[Math.Min(index, Steps.Count - 1)];
        Refresh();
    }

    [RelayCommand] private void MoveUp(StepViewModel step) => Move(step, -1);
    [RelayCommand] private void MoveDown(StepViewModel step) => Move(step, +1);

    private void Move(StepViewModel step, int delta)
    {
        int from = Steps.IndexOf(step), to = from + delta;
        if (to < 1 || to >= Steps.Count) return;

        pipeline.Move(step.Id, to);
        Steps.Move(from, to);
        SelectedStep = step;
        Refresh();
    }

    /// <summary>Updates every block of the chain column, then the preview.</summary>
    private void Refresh()
    {
        for (int i = 0; i < Steps.Count; i++)
        {
            var step = Steps[i];
            var result = pipeline.GetResult(step.Id);
            step.Number = i + 1;
            step.Error = result.Error?.Message;
            step.ChangeCount = CountChanges(pipeline.GetInput(step.Id), result.Items);
        }
        RefreshPreview();
    }

    /// <summary>A file counts as changed if it was renamed, left out or moved.</summary>
    private static int CountChanges(IReadOnlyList<Item> input, IReadOnlyList<Item> output)
    {
        var before = input.ToDictionary(i => i.FileId, i => i.Name);
        var outputIds = output.Select(i => i.FileId).ToHashSet();
        var kept = input.Where(i => outputIds.Contains(i.FileId)).ToList();

        int renamed = output.Count(i => before.GetValueOrDefault(i.FileId) != i.Name);
        int moved = output.Where((item, rank) => kept[rank].FileId != item.FileId && before[item.FileId] == item.Name).Count();
        return renamed + moved + input.Count - output.Count;
    }

    private void RefreshPreview()
    {
        if (SelectedStep is not { } step) return;

        var input = pipeline.GetInput(step.Id);
        var result = pipeline.GetResult(step.Id);
        var before = input.ToDictionary(i => i.FileId, i => i.Name);
        var after = result.Items.ToDictionary(i => i.FileId, i => i.Name);
        Action<FileId, bool>? toggle = step.IsSource ? source.SetIncluded : null;

        PreviewRow Row(FileId id, string? newName) => new(id, originals.GetValueOrDefault(id, ""), before[id], newName, toggle);

        // The source keeps the folder order, so that unchecking a file does not move it.
        // Other steps show their output order, then the files they left out.
        List<PreviewRow> rows = step.IsSource
            ? [.. input.Select(i => Row(i.FileId, after.GetValueOrDefault(i.FileId)))]
            : [.. result.Items.Select(i => Row(i.FileId, i.Name)),
               .. input.Where(i => !after.ContainsKey(i.FileId)).Select(i => Row(i.FileId, null))];

        if (Rows.Select(r => r.FileId).SequenceEqual(rows.Select(r => r.FileId)))
        {
            for (int k = 0; k < rows.Count; k++) Rows[k].CopyFrom(rows[k]);
        }
        else
        {
            Rows.Clear();
            foreach (var row in rows) Rows.Add(row);
        }

        int excluded = input.Count - result.Items.Count;
        Summary = string.Format(Strings.Summary, result.Items.Count, step.ChangeCount - excluded, excluded);
        (Banner, BannerIsError) = (step.IsEnabled, step.Error) switch
        {
            (false, _) => (Strings.Banner_Disabled, false),
            (_, { } error) => (string.Format(Strings.Banner_Error, error), true),
            _ => (null, false),
        };
    }
}
