using System.Collections.Immutable;
using ChainRenamer.App.Resources;
using ChainRenamer.Engine;
using ChainRenamer.Engine.Modules;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChainRenamer.App.ViewModels.Modules;

public partial class SortSettingsViewModel : ModuleSettingsViewModel
{
    [ObservableProperty] public partial bool Descending { get; set; }

    /// <summary>Set by the ↑ ↓ buttons of the preview grid.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsManual))]
    public partial ImmutableArray<FileId> ManualOrder { get; set; } = [];

    public bool IsManual => !ManualOrder.IsEmpty;

    public override string Title => Strings.Module_Sort;
    public override string Description => Strings.Sort_Description;

    public override IModule ToModule() => new SortModule(Descending) { ManualOrder = ManualOrder };

    /// <summary>Moves one file of the step's current output.</summary>
    public void Move(IReadOnlyList<Item> currentOrder, FileId id, int offset) =>
        ManualOrder = SortModule.Move(currentOrder, id, offset);

    [RelayCommand]
    private void ResetOrder() => ManualOrder = [];

    // Changing the direction starts again from the natural order.
    partial void OnDescendingChanged(bool value) => ManualOrder = [];
}
