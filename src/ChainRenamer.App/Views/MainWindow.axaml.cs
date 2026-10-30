using Avalonia.Controls;
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
    }

    private void MoveTo(object dragged, object target) => (DataContext as MainViewModel)?.MoveTo(dragged, target);
}
