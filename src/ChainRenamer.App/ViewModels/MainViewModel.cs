using System.Collections.ObjectModel;
using ChainRenamer.App.Resources;
using ChainRenamer.App.ViewModels.Modules;
using ChainRenamer.Engine;
using ChainRenamer.Engine.Renaming;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChainRenamer.App.ViewModels;

public sealed record AddModuleEntry(string Title, IRelayCommand Command);

public partial class MainViewModel : ViewModelBase
{
    private readonly Pipeline pipeline;
    private readonly SourceSettingsViewModel source = new();
    private Dictionary<FileId, string> originals = [];
    private RenamePlan finalPlan = new([], []);
    private IReadOnlyDictionary<FileId, ConflictKind> diskConflicts = new Dictionary<FileId, ConflictKind>();

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

    /// <summary>
    /// Drag and drop with a handle: the dragged step or file takes the place of the one under the pointer.
    /// Called continuously while dragging, so the preview follows the gesture.
    /// </summary>
    public void MoveTo(object dragged, object target)
    {
        switch (dragged, target)
        {
            case (StepViewModel step, StepViewModel over):
                Move(step, Steps.IndexOf(over));
                break;
            case (PreviewRow row, PreviewRow over) when row.CanMove:
                row.MoveBy(Rows.IndexOf(over) - Rows.IndexOf(row));
                break;
        }
    }

    private void Move(StepViewModel step, int to)
    {
        int from = Steps.IndexOf(step);
        if (from < 1 || to < 1 || to >= Steps.Count || to == from) return;

        pipeline.Move(step.Id, to);
        Steps.Move(from, to);
        SelectedStep = step;
        Refresh();
    }

    // ───────────── Applying the renaming ─────────────

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand), nameof(UndoCommand))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty] public partial bool IsConfirming { get; set; }
    [ObservableProperty] public partial string ConfirmText { get; set; } = "";
    [ObservableProperty] public partial string? Status { get; set; }
    [ObservableProperty] public partial bool StatusIsError { get; set; }

    /// <summary>The journal of the last renaming, while it can still be undone.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UndoCommand))]
    public partial string? LastJournal { get; set; }

    private bool CanApply => finalPlan.CanApply && !IsBusy;
    private bool CanUndo => LastJournal is not null && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanApply))]
    private void Apply()
    {
        ConfirmText = string.Format(Strings.Apply_Confirm, finalPlan.Operations.Count, source.Folder);
        IsConfirming = true;
    }

    [RelayCommand]
    private void CancelApply() => IsConfirming = false;

    [RelayCommand]
    private async Task ConfirmApplyAsync()
    {
        IsConfirming = false;
        IsBusy = true;
        try
        {
            // Last check against the disk: a file may have appeared since the folder was loaded.
            SetStatus(Strings.Apply_Checking);
            var plan = await Task.Run(() => RenamePlan.Create(pipeline.Files, pipeline.Output, p => File.Exists(p) || Directory.Exists(p)));
            if (plan.Conflicts.Count > 0)
            {
                diskConflicts = plan.Conflicts.ToDictionary(c => c.FileId, c => c.Kind);
                SelectedStep = Steps[^1];
                RefreshPreview();
                SetStatus(string.Format(Strings.Apply_DiskConflicts, plan.Conflicts.Count), error: true);
                return;
            }

            // Progress<T> runs its callback on the thread that created it: here, the UI thread.
            var progress = new Progress<double>(p => SetStatus(string.Format(Strings.Apply_Running, p)));
            var journal = NewJournalPath();
            var result = await Applier.ApplyAsync(plan, journal, progress);

            if (result.Succeeded)
            {
                LastJournal = journal;
                SetStatus(string.Format(Strings.Apply_Done, result.Renamed));
            }
            else
            {
                SetStatus(result.RolledBack
                    ? string.Format(Strings.Apply_Failed, result.Error!.Message)
                    : string.Format(Strings.Apply_FailedPartial, result.Error!.Message, journal), error: true);
            }
            await source.LoadCommand.ExecuteAsync(null);   // show the folder as it now is
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private async Task UndoAsync()
    {
        IsBusy = true;
        try
        {
            bool complete = await Applier.UndoAsync(LastJournal!);
            SetStatus(complete ? Strings.Undo_Done : string.Format(Strings.Undo_Partial, LastJournal), error: !complete);
            if (complete) LastJournal = null;
            await source.LoadCommand.ExecuteAsync(null);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetStatus(string text, bool error = false) => (Status, StatusIsError) = (text, error);

    private static string NewJournalPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ChainRenamer", "journals", $"{DateTime.Now:yyyyMMdd-HHmmss}.jsonl");

    // ───────────── Preview ─────────────

    /// <summary>Updates every block of the chain column, the final plan, then the preview.</summary>
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

        diskConflicts = new Dictionary<FileId, ConflictKind>();   // stale as soon as the chain changes
        finalPlan = RenamePlan.Create(pipeline.Files, pipeline.Output);
        ApplyCommand.NotifyCanExecuteChanged();
        if (!IsBusy && finalPlan.Conflicts.Count > 0)
            SetStatus(string.Format(Strings.Apply_Blocked, finalPlan.Conflicts.Count), error: true);
        else if (StatusIsError && !IsBusy)
            Status = null;

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
        var actions = step.Settings switch
        {
            SourceSettingsViewModel => new RowActions(Toggle: source.SetIncluded),
            SortSettingsViewModel sort => new RowActions(Move: (id, offset) => sort.Move(pipeline.GetOutput(step.Id), id, offset)),
            _ => new RowActions(),
        };

        // The conflicts there would be if the chain stopped at this step (plus, on the last step, those found on disk).
        var conflicts = RenamePlan.Create(pipeline.Files, result.Items).Conflicts.ToDictionary(c => c.FileId, c => c.Kind);
        if (step == Steps[^1])
            foreach (var (id, kind) in diskConflicts) conflicts.TryAdd(id, kind);

        PreviewRow Row(FileId id, string? newName) => new(id, originals.GetValueOrDefault(id, ""), before[id], newName,
            conflicts.TryGetValue(id, out var kind) ? Describe(kind) : null, actions);

        // The source keeps the folder order, so that unchecking a file does not move it.
        // Other steps show their output order, then the files they left out.
        List<PreviewRow> rows = step.IsSource
            ? [.. input.Select(i => Row(i.FileId, after.GetValueOrDefault(i.FileId)))]
            : [.. result.Items.Select(i => Row(i.FileId, i.Name)),
               .. input.Where(i => !after.ContainsKey(i.FileId)).Select(i => Row(i.FileId, null))];

        UpdateRows(step, rows);

        int excluded = input.Count - result.Items.Count;
        Summary = string.Format(Strings.Summary, result.Items.Count, step.ChangeCount - excluded, excluded, conflicts.Count);
        (Banner, BannerIsError) = (step.IsEnabled, step.Error) switch
        {
            (false, _) => (Strings.Banner_Disabled, false),
            (_, { } error) => (string.Format(Strings.Banner_Error, error), true),
            _ => (null, false),
        };
    }

    private static string Describe(ConflictKind kind) => kind switch
    {
        ConflictKind.EmptyName => Strings.Conflict_EmptyName,
        ConflictKind.InvalidCharacter => Strings.Conflict_InvalidCharacter,
        ConflictKind.ReservedName => Strings.Conflict_ReservedName,
        ConflictKind.TrailingDotOrSpace => Strings.Conflict_TrailingDotOrSpace,
        ConflictKind.PathTooLong => Strings.Conflict_PathTooLong,
        ConflictKind.Duplicate => Strings.Conflict_Duplicate,
        ConflictKind.AlreadyExists => Strings.Conflict_AlreadyExists,
        _ => kind.ToString(),
    };

    private StepViewModel? rowsStep;

    /// <summary>
    /// For the same step and the same files, rows are reordered and updated in place,
    /// so that the grid keeps its scroll position (typing a regex, dragging a file…).
    /// </summary>
    private void UpdateRows(StepViewModel step, List<PreviewRow> rows)
    {
        var current = Rows.ToDictionary(r => r.FileId);
        if (step != rowsStep || rows.Count != Rows.Count || !rows.All(r => current.ContainsKey(r.FileId)))
        {
            rowsStep = step;
            Rows.Clear();
            foreach (var row in rows) Rows.Add(row);
            return;
        }

        for (int k = 0; k < rows.Count; k++)
        {
            var row = current[rows[k].FileId];
            int from = Rows.IndexOf(row);
            if (from != k)
            {
                // Not Rows.Move: the DataGrid does not redraw on a "Move" notification.
                Rows.RemoveAt(from);
                Rows.Insert(k, row);
            }
            row.CopyFrom(rows[k]);
        }
    }
}
