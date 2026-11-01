using Avalonia.Controls;
using Avalonia.Input;
using ChainRenamer.App.ViewModels.Help;

namespace ChainRenamer.App.Views;

public partial class HelpWindow : Window
{
    public HelpWindow() => InitializeComponent();

    // A new page starts at its top.
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is HelpViewModel vm)
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(HelpViewModel.Page)) PageScroll.Offset = default;
            };
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape) Close();
    }
}
