using ChainRenamer.Engine;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChainRenamer.App.ViewModels;

/// <summary>What the user may do on the rows of the selected step.</summary>
/// <param name="Check">Check or uncheck a file: include it (source step), or apply the step to it (name step).</param>
/// <param name="Move">Move a file by an offset (sort step).</param>
public sealed record RowActions(Action<FileId, bool>? Check = null, Action<FileId, int>? Move = null);

/// <summary>One line of the preview grid: a file seen at the selected step.</summary>
public sealed partial class PreviewRow : ObservableObject
{
    private readonly RowActions actions;
    private bool updating;

    /// <param name="after">The name after the step, or null if the step left the file out.</param>
    /// <param name="skipped">The step left this file as it is.</param>
    /// <param name="conflict">Why this name could not be applied, or null.</param>
    public PreviewRow(FileId fileId, string original, string before, string? after, bool skipped, string? conflict, RowActions actions)
    {
        FileId = fileId;
        Original = original;
        this.actions = actions;
        Set(before, after, skipped, conflict);
    }

    public FileId FileId { get; }
    public string Original { get; }
    public bool CanCheck => actions.Check is not null;
    public bool CanMove => actions.Move is not null && IsIncluded;

    [ObservableProperty] public partial string Before { get; private set; } = "";
    [ObservableProperty] public partial string After { get; private set; } = "";
    [ObservableProperty] public partial bool IsChanged { get; private set; }
    [ObservableProperty] public partial bool IsSkipped { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanMove))]
    public partial bool IsIncluded { get; private set; }

    /// <summary>The grid's check box: included and not skipped.</summary>
    [ObservableProperty] public partial bool IsChecked { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasConflict))]
    public partial string? Conflict { get; private set; }

    public bool HasConflict => Conflict is not null;

    /// <summary>Updates the row in place, so that the grid keeps its scroll position.</summary>
    public void CopyFrom(PreviewRow other) => Set(other.Before, other.IsIncluded ? other.After : null, other.IsSkipped, other.Conflict);

    public void MoveBy(int offset) => actions.Move?.Invoke(FileId, offset);

    private void Set(string before, string? after, bool skipped, string? conflict)
    {
        updating = true;
        Before = before;
        After = after ?? "";
        IsIncluded = after is not null;
        IsSkipped = skipped;
        IsChecked = IsIncluded && !skipped;
        IsChanged = IsIncluded && after != before;
        Conflict = conflict;
        updating = false;
    }

    // Called when the user clicks the check box, not when the row is refreshed.
    partial void OnIsCheckedChanged(bool value)
    {
        if (!updating) actions.Check?.Invoke(FileId, value);
    }
}
