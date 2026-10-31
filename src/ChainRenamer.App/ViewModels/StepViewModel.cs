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
    public bool IsSource => Settings is SourceSettingsViewModel;

    /// <summary>A step that only changes names: the user can make it skip some files.</summary>
    public bool IsNameStep { get; }

    /// <summary>Files this step leaves as they are (unchecked in its grid).</summary>
    [ObservableProperty] public partial ImmutableHashSet<FileId> Skipped { get; set; } = [];

    [ObservableProperty] public partial string? Name { get; set; }

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

    public Step ToStep() => new(Id, Settings.ToModule(), string.IsNullOrWhiteSpace(Name) ? null : Name.Trim(), IsEnabled) { Skipped = Skipped };

    partial void OnNameChanged(string? value) => Changed?.Invoke(this, EventArgs.Empty);
    partial void OnIsEnabledChanged(bool value) => Changed?.Invoke(this, EventArgs.Empty);
    partial void OnSkippedChanged(ImmutableHashSet<FileId> value) => Changed?.Invoke(this, EventArgs.Empty);
}
