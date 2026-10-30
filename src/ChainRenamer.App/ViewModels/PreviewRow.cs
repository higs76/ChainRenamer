using ChainRenamer.Engine;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChainRenamer.App.ViewModels;

/// <summary>One line of the preview grid: a file seen at the selected step.</summary>
public sealed partial class PreviewRow : ObservableObject
{
    private readonly Action<FileId, bool>? onToggle;
    private bool updating;

    /// <param name="after">The name after the step, or null if the step left the file out.</param>
    /// <param name="onToggle">Set when the user may check or uncheck the file (source step).</param>
    public PreviewRow(FileId fileId, string original, string before, string? after, Action<FileId, bool>? onToggle = null)
    {
        FileId = fileId;
        Original = original;
        this.onToggle = onToggle;
        Set(before, after);
    }

    public FileId FileId { get; }
    public string Original { get; }
    public bool CanToggle => onToggle is not null;

    [ObservableProperty] public partial string Before { get; private set; } = "";
    [ObservableProperty] public partial string After { get; private set; } = "";
    [ObservableProperty] public partial bool IsIncluded { get; set; }
    [ObservableProperty] public partial bool IsChanged { get; private set; }

    /// <summary>Updates the row in place, so that the grid keeps its scroll position.</summary>
    public void CopyFrom(PreviewRow other) => Set(other.Before, other.IsIncluded ? other.After : null);

    private void Set(string before, string? after)
    {
        updating = true;
        Before = before;
        After = after ?? "";
        IsIncluded = after is not null;
        IsChanged = after is not null && after != before;
        updating = false;
    }

    // Called when the user clicks the grid's check box, not when the row is refreshed.
    partial void OnIsIncludedChanged(bool value)
    {
        if (!updating) onToggle?.Invoke(FileId, value);
    }
}
