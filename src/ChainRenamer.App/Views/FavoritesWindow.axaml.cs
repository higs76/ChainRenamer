using Avalonia.Controls;
using Avalonia.Interactivity;
using ChainRenamer.App.Resources;
using ChainRenamer.App.ViewModels;

namespace ChainRenamer.App.Views;

public partial class FavoritesWindow : Window
{
    public FavoritesWindow() => InitializeComponent();

    private FavoritesViewModel? ViewModel => DataContext as FavoritesViewModel;

    // The two lists depend on the tree at the moment of the click: their menus are built then.
    private void OnAddChain(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } vm) return;
        var chains = vm.AddableChains();
        ShowMenu((Control)sender!, chains.Count == 0
            ? [new MenuItem { Header = Strings.Favorites_NothingToAdd, IsEnabled = false }]
            : chains.Select(name => Item(name, () => vm.AddChain(name))));
    }

    private void OnMoveTo(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } vm) return;
        ShowMenu((Control)sender!, vm.Destinations().Select(d => Item(d.Label, () => vm.MoveTo(d.Folder))));
    }

    // The header is a TextBlock: a plain string would turn "_" into an access key.
    private static MenuItem Item(string header, Action click)
    {
        var item = new MenuItem { Header = new TextBlock { Text = header } };
        item.Click += (_, _) => click();
        return item;
    }

    private static void ShowMenu(Control target, IEnumerable<MenuItem> items)
    {
        var menu = new MenuFlyout { Placement = PlacementMode.BottomEdgeAlignedLeft };
        foreach (var item in items) menu.Items.Add(item);
        menu.ShowAt(target);
    }

    private void OnClose(object? sender, RoutedEventArgs e) => Close();
}
