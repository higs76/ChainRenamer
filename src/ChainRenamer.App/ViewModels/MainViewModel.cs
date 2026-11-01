using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using ChainRenamer.App.Resources;
using ChainRenamer.App.ViewModels.Modules;
using ChainRenamer.Engine;
using ChainRenamer.Engine.Renaming;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChainRenamer.App.ViewModels;

/// <summary>An entry of a menu built from data: its text and what it does.</summary>
public sealed record MenuEntry(string Title, IRelayCommand Command);

public partial class MainViewModel : ViewModelBase
{
    private readonly Pipeline pipeline;
    private readonly SourceSettingsViewModel source = new();
    private Dictionary<FileId, string> originals = [];
    private RenamePlan finalPlan = new([], []);
    private IReadOnlyDictionary<FileId, ConflictKind> diskConflicts = new Dictionary<FileId, ConflictKind>();

    public MainViewModel()
    {
        preferences = Preferences.Load();
        library = new ChainLibrary(preferences.Library);
        ShowOriginal = preferences.ShowOriginal;
        pipeline = new Pipeline(source.ToModule());
        Steps.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(CanReorder));
            OnPropertyChanged(nameof(ShowChainHint));
        };
        source.FilesLoaded += files =>
        {
            originals = files.ToDictionary(f => f.Id, f => f.Name);
            pipeline.SetFiles(files);
            Remember(source.Folder.Trim());
            Refresh();
        };
        Attach(new StepViewModel(pipeline.Steps[0].Id, source));
        AddEntries = [.. ModuleCatalog.Addable.Select(t => new MenuEntry(t.Title, new RelayCommand(() => AddStep(t.Create(null)))))];
        SelectedStep = Steps[0];
        Refresh();
    }

    private static readonly string Version = typeof(MainViewModel).Assembly.GetName().Version?.ToString(3) ?? "";

    /// <summary>The window title: the chain, a dot if it has unsaved changes, and the version.</summary>
    public string Title => $"{ChainName ?? Strings.Chain_Untitled}{(IsModified ? " •" : "")} — ChainRenamer {Version}";

    public ObservableCollection<StepViewModel> Steps { get; } = [];
    public RowCollection<PreviewRow> Rows { get; } = [];
    public IReadOnlyList<MenuEntry> AddEntries { get; }

    /// <summary>At least two steps besides the fixed source: their order can be changed.</summary>
    public bool CanReorder => Steps.Count > 2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCheck), nameof(CheckTip))]
    [NotifyCanExecuteChangedFor(nameof(CheckSelectionCommand), nameof(UncheckSelectionCommand), nameof(InvertChecksCommand))]
    public partial StepViewModel? SelectedStep { get; set; }
    [ObservableProperty] public partial string Summary { get; set; } = "";
    [ObservableProperty] public partial string? Banner { get; set; }
    [ObservableProperty] public partial bool BannerIsError { get; set; }

    // Column headers name the selected step, so that "before" and "after" are clear.
    [ObservableProperty] public partial string BeforeHeader { get; set; } = "";
    [ObservableProperty] public partial string AfterHeader { get; set; } = "";

    /// <summary>The Original column can be hidden on small screens.</summary>
    [ObservableProperty] public partial bool ShowOriginal { get; set; } = true;

    partial void OnSelectedStepChanged(StepViewModel? value) => RefreshPreview();

    partial void OnShowOriginalChanged(bool value)
    {
        if (preferences.ShowOriginal == value) return;
        preferences = preferences with { ShowOriginal = value };   // "with" copies a record, changing some properties
        preferences.Save();
    }

    private void AddStep(ModuleSettingsViewModel settings)
    {
        SelectedStep = AddStep(settings, null, true);
        Refresh();
    }

    private StepViewModel AddStep(ModuleSettingsViewModel settings, string? name, bool enabled, Segment? segment = null)
    {
        var step = new StepViewModel(pipeline.Add(settings.ToModule()).Id, settings) { Name = name, IsEnabled = enabled, Segment = segment };
        pipeline.Update(step.ToStep());
        return Attach(step);
    }

    private StepViewModel Attach(StepViewModel step)
    {
        step.Changed += (_, _) =>
        {
            pipeline.Update(step.ToStep());
            RefreshSoon();
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

    // ───────────── Checking files ─────────────

    /// <summary>
    /// The files unchecked in the grid of the selected step: left out by the source, or skipped by a name step.
    /// Null when the step has no check boxes (list steps other than the source).
    /// </summary>
    private ImmutableHashSet<FileId>? Unchecked
    {
        get => SelectedStep switch
        {
            { IsSource: true } => source.Excluded,
            { IsNameStep: true } step => step.Skipped,
            _ => null,
        };
        set
        {
            if (value is null || SelectedStep is not { } step) return;
            if (step.IsSource) source.Excluded = value;
            else step.Skipped = value;
        }
    }

    public bool CanCheck => Unchecked is not null;

    /// <summary>Both kinds of check box look alike: the tooltip tells what this one does.</summary>
    public string? CheckTip => SelectedStep switch
    {
        { IsSource: true } => Strings.Check_Included,
        { IsNameStep: true } => Strings.Check_Applied,
        _ => null,
    };

    /// <summary>The rows selected in the grid (Ctrl+click, Shift+click, Ctrl+A), set by the view.</summary>
    public IReadOnlyList<PreviewRow> SelectedRows { get; set; } = [];

    // The last file checked or unchecked by a click: the start of a Shift+click range.
    private FileId? anchor;

    private bool refreshing;

    /// <summary>The header check box: checked, unchecked, or indeterminate when only some files are.</summary>
    [ObservableProperty] public partial bool? AllChecked { get; set; }

    partial void OnAllCheckedChanged(bool? value)
    {
        if (!refreshing && value is bool check) Check(Rows, check);
    }

    private void Check(IEnumerable<PreviewRow> rows, bool check)
    {
        var ids = rows.Select(r => r.FileId);
        if (Unchecked is { } current) Unchecked = check ? current.Except(ids) : current.Union(ids);
    }

    private void CheckOne(FileId id, bool check)
    {
        anchor = id;
        if (Unchecked is { } current) Unchecked = check ? current.Remove(id) : current.Add(id);
    }

    /// <summary>Shift+click on a check box: the whole range from the last clicked file takes that file's state.</summary>
    public void CheckRange(PreviewRow to)
    {
        int from = Rows.ToList().FindIndex(r => r.FileId == anchor);
        if (from < 0)
        {
            CheckOne(to.FileId, !to.IsChecked);
            return;
        }

        int end = Rows.IndexOf(to);
        Check(Rows.Skip(Math.Min(from, end)).Take(Math.Abs(end - from) + 1), Rows[from].IsChecked);
    }

    /// <summary>Space on selected rows: checks them all, unless they all are already.</summary>
    public void ToggleSelection()
    {
        if (CanCheck && SelectedRows.Count > 0) Check(SelectedRows, SelectedRows.Any(r => !r.IsChecked));
    }

    [RelayCommand(CanExecute = nameof(CanCheck))]
    private void CheckSelection() => Check(SelectedRows, true);

    [RelayCommand(CanExecute = nameof(CanCheck))]
    private void UncheckSelection() => Check(SelectedRows, false);

    [RelayCommand(CanExecute = nameof(CanCheck))]
    private void InvertChecks()
    {
        if (Unchecked is { } current) Unchecked = [.. Rows.Select(r => r.FileId).Where(id => !current.Contains(id))];
    }

    // ───────────── Applying the renaming ─────────────

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand), nameof(UndoCommand))]
    public partial bool IsBusy { get; set; }

    // A question in the top bar (rename the files? discard the changes?), and what to do if the user agrees.
    [ObservableProperty] public partial bool IsConfirming { get; set; }
    [ObservableProperty] public partial string ConfirmText { get; set; } = "";
    [ObservableProperty] public partial string ConfirmYes { get; set; } = "";
    private Func<Task>? confirmed;
    [ObservableProperty] public partial string? Status { get; set; }
    [ObservableProperty] public partial bool StatusIsError { get; set; }

    /// <summary>The journal of the last renaming, while it can still be undone.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UndoCommand))]
    public partial string? LastJournal { get; set; }

    private bool CanApply => finalPlan.CanApply && !IsBusy;
    private bool CanUndo => LastJournal is not null && !IsBusy;

    private void Confirm(string text, string yes, Func<Task> action)
    {
        (ConfirmText, ConfirmYes, confirmed) = (text, yes, action);
        IsConfirming = true;
    }

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        IsConfirming = false;
        var action = confirmed;
        confirmed = null;
        if (action is not null) await action();
    }

    [RelayCommand]
    private void CancelConfirm() => (IsConfirming, confirmed) = (false, null);

    [RelayCommand(CanExecute = nameof(CanApply))]
    private void Apply() =>
        Confirm(string.Format(Strings.Apply_Confirm, finalPlan.Operations.Count, source.Folder), Strings.Apply_Yes, RenameAsync);

    private async Task RenameAsync()
    {
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
            var journal = Journal.NewPath();
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

    // ───────────── Preview ─────────────

    private bool refreshPending;

    /// <summary>
    /// Refreshes once the pending input is handled: the keys typed while a large folder is computed
    /// are all taken into account by a single refresh, instead of one refresh per key.
    /// </summary>
    private void RefreshSoon()
    {
        if (refreshPending) return;
        refreshPending = true;
        Dispatcher.UIThread.Post(() => { if (refreshPending) Refresh(); }, DispatcherPriority.Background);
    }

    /// <summary>Updates every block of the chain column, the final plan, then the preview.</summary>
    private void Refresh()
    {
        refreshPending = false;
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

        IsModified = CurrentChain().ToJson() != savedJson;
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
            SortSettingsViewModel sort => new RowActions(Move: (id, offset) => sort.Move(pipeline.GetOutput(step.Id), id, offset)),
            _ => new RowActions(Check: CanCheck ? CheckOne : null),
        };
        var skipped = step.IsNameStep ? step.Skipped : [];

        // The conflicts there would be if the chain stopped at this step (plus, on the last step, those found on disk).
        var conflicts = RenamePlan.Create(pipeline.Files, result.Items).Conflicts.ToDictionary(c => c.FileId, c => c.Kind);
        if (step == Steps[^1])
            foreach (var (id, kind) in diskConflicts) conflicts.TryAdd(id, kind);

        PreviewRow Row(FileId id, string? newName) => new(id, originals.GetValueOrDefault(id, ""), before[id], newName,
            skipped.Contains(id), conflicts.TryGetValue(id, out var kind) ? Describe(kind) : null, actions);

        // A capture does not change the names: its "after" column shows what it captured, {season} = 1 · {episode} = 03.
        var captured = step.Settings is CaptureSettingsViewModel capture ? capture.VariableNames() : [];
        string After(Item i) => captured.Count == 0 || !captured.Any(i.Variables.ContainsKey) ? i.Name
            : string.Join("  ·  ", captured.Where(i.Variables.ContainsKey).Select(n => $"{{{n}}} = {i.Variables[n]}"));

        // The source keeps the folder order, so that unchecking a file does not move it.
        // Other steps show their output order, then the files they left out.
        List<PreviewRow> rows = step.IsSource
            ? [.. input.Select(i => Row(i.FileId, after.GetValueOrDefault(i.FileId)))]
            : [.. result.Items.Select(i => Row(i.FileId, After(i))),
               .. input.Where(i => !after.ContainsKey(i.FileId)).Select(i => Row(i.FileId, null))];

        UpdateRows(step, rows);

        string stepLabel = string.IsNullOrWhiteSpace(step.Name) ? step.Title : step.Name.Trim();
        BeforeHeader = string.Format(Strings.Column_Before, step.Number, stepLabel);
        AfterHeader = string.Format(Strings.Column_After, step.Number, stepLabel);

        int excluded = input.Count - result.Items.Count;
        int skippedCount = Rows.Count(r => r.IsSkipped);
        Summary = string.Format(Strings.Summary, result.Items.Count, step.ChangeCount - excluded, excluded, conflicts.Count)
            + (skippedCount > 0 ? string.Format(Strings.Summary_Skipped, skippedCount) : "");

        refreshing = true;
        int checkedCount = Rows.Count(r => r.IsChecked);
        AllChecked = checkedCount == Rows.Count && Rows.Count > 0 ? true : checkedCount == 0 ? false : null;
        refreshing = false;
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

    // Beyond this, rebuilding the grid at once is cheaper than moving rows one by one.
    private const int MaxMoves = 50;

    /// <summary>
    /// For the same step and the same files, rows are reordered and updated in place,
    /// so that the grid keeps its scroll position (typing a regex, dragging a file…).
    /// </summary>
    private void UpdateRows(StepViewModel step, List<PreviewRow> rows)
    {
        var current = Rows.ToDictionary(r => r.FileId);
        if (step != rowsStep || rows.Count != Rows.Count || !rows.All(r => current.ContainsKey(r.FileId)) || !Reorder(rows))
        {
            rowsStep = step;
            Rows.ReplaceAll(rows);
            return;
        }

        for (int k = 0; k < rows.Count; k++) Rows[k].CopyFrom(rows[k]);
    }

    /// <summary>Puts the rows in the order of <paramref name="rows"/>; false if that takes too many moves.</summary>
    private bool Reorder(List<PreviewRow> rows)
    {
        int moves = 0;
        for (int k = 0; k < rows.Count; k++)
        {
            if (Rows[k].FileId == rows[k].FileId) continue;     // most often: already in place
            if (++moves > MaxMoves) return false;

            // The rows before k are in place: the one wanted here is further down.
            int from = k + 1;
            while (Rows[from].FileId != rows[k].FileId) from++;
            var row = Rows[from];
            // Not Rows.Move: the DataGrid does not redraw on a "Move" notification.
            Rows.RemoveAt(from);
            Rows.Insert(k, row);
        }
        return true;
    }
}
