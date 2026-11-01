using System.Collections.Immutable;
using ChainRenamer.App.Resources;
using ChainRenamer.App.ViewModels.Modules;
using ChainRenamer.Engine;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChainRenamer.App.ViewModels;

/// <summary>One block of the chain column. Its state is the source of truth for the pipeline step.</summary>
public partial class StepViewModel : ViewModelBase
{
    public StepViewModel(StepId id, ModuleSettingsViewModel settings)
    {
        Id = id;
        Settings = settings;
        IsNameStep = settings.ToModule() is INameModule;
        settings.Changed += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Raised when the step must be pushed to the pipeline again.</summary>
    public event EventHandler? Changed;

    public StepId Id { get; }
    public ModuleSettingsViewModel Settings { get; }
    public string Title => Settings.Title;
    public string Description => Settings.Description;
    public bool IsSource => Settings is SourceSettingsViewModel;

    /// <summary>A step that only changes names: the user can make it skip some files.</summary>
    public bool IsNameStep { get; }

    /// <summary>Files this step leaves as they are (unchecked in its grid).</summary>
    [ObservableProperty] public partial ImmutableHashSet<FileId> Skipped { get; set; } = [];

    [ObservableProperty] public partial string? Name { get; set; }

    // The part of the name a name step works on (Segment): after a text, before a text.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSegment), nameof(SegmentHeader))]
    public partial string SegmentAfter { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSegment), nameof(SegmentHeader))]
    public partial string SegmentBefore { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SegmentHeader))]
    public partial bool SegmentLast { get; set; }

    public bool HasSegment => SegmentAfter.Length > 0 || SegmentBefore.Length > 0;

    /// <summary>"Part of the name: after the last “ - ”", readable while the block is closed.</summary>
    public string SegmentHeader => string.Format(Strings.Segment_Header, !HasSegment ? Strings.Segment_Whole : string.Join(", ",
        new[]
        {
            SegmentAfter.Length == 0 ? null : string.Format(SegmentLast ? Strings.Segment_SummaryLastAfter : Strings.Segment_SummaryAfter, SegmentAfter),
            SegmentBefore.Length == 0 ? null : string.Format(Strings.Segment_SummaryBefore, SegmentBefore),
        }.OfType<string>()));

    public Segment? Segment
    {
        get => HasSegment ? new(SegmentAfter, SegmentBefore, SegmentLast) : null;
        set => (SegmentAfter, SegmentBefore, SegmentLast) = value ?? new Segment();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Status))]
    public partial bool IsEnabled { get; set; } = true;

    // Display state, refreshed by MainViewModel after each recomputation.
    [ObservableProperty] public partial int Number { get; set; }
    [ObservableProperty] public partial string? Error { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Status))]
    public partial int ChangeCount { get; set; }

    public string Status => IsEnabled ? string.Format(Strings.Step_Changes, ChangeCount) : Strings.Step_Disabled;

    public Step ToStep() => new(Id, Settings.ToModule(), string.IsNullOrWhiteSpace(Name) ? null : Name.Trim(), IsEnabled) { Skipped = Skipped, Segment = IsNameStep ? Segment : null };

    partial void OnNameChanged(string? value) => Changed?.Invoke(this, EventArgs.Empty);
    partial void OnIsEnabledChanged(bool value) => Changed?.Invoke(this, EventArgs.Empty);
    partial void OnSegmentAfterChanged(string value) => Changed?.Invoke(this, EventArgs.Empty);
    partial void OnSegmentBeforeChanged(string value) => Changed?.Invoke(this, EventArgs.Empty);
    partial void OnSegmentLastChanged(bool value) => Changed?.Invoke(this, EventArgs.Empty);
    partial void OnSkippedChanged(ImmutableHashSet<FileId> value) => Changed?.Invoke(this, EventArgs.Empty);
}
