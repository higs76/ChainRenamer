using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ChainRenamer.App.ViewModels;

namespace ChainRenamer.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Dragging is pure view interaction; the view model only receives the resulting move.
        DragReorder.Attach(StepList, MoveTo);
        DragReorder.Attach(PreviewGrid, MoveTo);

        // Fast checking: Shift+click on a check box, Space on the selected rows.
        PreviewGrid.SelectionChanged += (_, _) =>
        {
            if (ViewModel is { } vm) vm.SelectedRows = [.. PreviewGrid.SelectedItems.OfType<PreviewRow>()];
        };
        PreviewGrid.AddHandler(PointerPressedEvent, OnGridPressed, RoutingStrategies.Tunnel);
        PreviewGrid.AddHandler(KeyDownEvent, OnGridKeyDown, RoutingStrategies.Tunnel);
    }

    private MainViewModel? ViewModel => DataContext as MainViewModel;

    private void MoveTo(object dragged, object target) => ViewModel?.MoveTo(dragged, target);

    private void OnGridPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Shift)) return;
        if ((e.Source as Control)?.FindAncestorOfType<CheckBox>(includeSelf: true) is not { DataContext: PreviewRow { CanCheck: true } row }) return;

        e.Handled = true;   // the check box must not toggle on its own
        ViewModel?.CheckRange(row);
    }

    private void OnGridKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Space || ViewModel is not { CanCheck: true } vm) return;

        e.Handled = true;
        vm.ToggleSelection();
    }
}
