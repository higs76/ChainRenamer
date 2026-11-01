using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ChainRenamer.App.Resources;
using ChainRenamer.App.ViewModels;
using ChainRenamer.App.ViewModels.Help;
using ChainRenamer.Engine;

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

        // Selection with a rectangle, as in the Explorer (Ctrl: added to the selection).
        MarqueeSelect.Attach(PreviewGrid);

        ChainsMenu.SubmenuOpened += OnChainsMenuOpened;
    }

    private MainViewModel? ViewModel => DataContext as MainViewModel;

    // A DataGrid column is not in the visual tree and has no DataContext: its visibility is set from here.
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (ViewModel is not { } vm) return;

        var original = PreviewGrid.Columns[1];
        original.IsVisible = vm.ShowOriginal;
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.ShowOriginal)) original.IsVisible = vm.ShowOriginal;

            // Save as: the name box takes the focus, its text selected.
            if (args.PropertyName == nameof(MainViewModel.IsNaming) && vm.IsNaming)
                Dispatcher.UIThread.Post(() => { NameBox.Focus(); NameBox.SelectAll(); });
        };
    }

    // ───────────── Chains menu ─────────────

    private readonly List<Control> favoriteItems = [];

    // The menu is rebuilt each time it opens, so that it always shows the library as it is.
    private void OnChainsMenuOpened(object? sender, RoutedEventArgs e)
    {
        if (e.Source != ChainsMenu || ViewModel is not { } vm) return;

        foreach (var item in favoriteItems) ChainsMenu.Items.Remove(item);
        favoriteItems.Clear();
        favoriteItems.AddRange(FavoriteItems(vm.Favorites(), vm));
        if (favoriteItems.Count > 0) favoriteItems.Add(new Separator());
        for (int i = 0; i < favoriteItems.Count; i++) ChainsMenu.Items.Insert(i, favoriteItems[i]);

        var all = vm.SavedChains();
        AllChainsMenu.ItemsSource = all.Count == 0
            ? [new MenuItem { Header = Strings.Menu_NoChain, IsEnabled = false }]
            : all.Select(c => ChainItem((c.IsFavorite ? "★  " : "") + c.Name, c.Name, vm)).ToList();
    }

    /// <summary>A folder of favorites is a submenu, as in a browser.</summary>
    private static IEnumerable<MenuItem> FavoriteItems(IEnumerable<Favorite> favorites, MainViewModel vm) => favorites.Select(f =>
    {
        if (!f.IsFolder) return ChainItem("★  " + f.Name, f.Name, vm);
        var items = FavoriteItems(f.Items!, vm).ToList();
        return new MenuItem
        {
            Header = new TextBlock { Text = f.Name },
            ItemsSource = items.Count > 0 ? items : [new MenuItem { Header = Strings.Menu_EmptyFolder, IsEnabled = false }],
        };
    });

    private async void OnOrganizeFavorites(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } vm) return;
        await new FavoritesWindow { DataContext = vm.OrganizeFavorites() }.ShowDialog(this);
        vm.FavoritesChanged();
    }

    // The header is a TextBlock: a plain string would turn "_" into an access key ("Series_2" → "Series2").
    private static MenuItem ChainItem(string header, string name, MainViewModel vm)
    {
        var item = new MenuItem { Header = new TextBlock { Text = header } };
        item.Click += (_, _) => vm.OpenChain(name);
        return item;
    }

    // ───────────── Files and folders (pickers are view business: the view model only gets paths) ─────────────

    private static readonly FilePickerFileType ChainFiles = new(Strings.Picker_Chain) { Patterns = ["*" + ChainLibrary.Extension] };
    private static readonly FilePickerFileType BackupFiles = new(Strings.Picker_Backup) { Patterns = ["*.zip"] };

    private async void OnImport(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Strings.Picker_Import,
            AllowMultiple = true,
            FileTypeFilter = [new(Strings.Picker_Chains) { Patterns = ["*" + ChainLibrary.Extension, "*.zip"] }, ChainFiles, BackupFiles],
        });
        if (ViewModel is { } vm) await vm.ImportAsync(files.Select(f => f.TryGetLocalPath()).OfType<string>());
    }

    private async void OnExport(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } vm) return;
        if (await SavePath(Strings.Picker_Export, vm.ChainName ?? Strings.Chain_Untitled, ChainFiles) is { } path) await vm.ExportAsync(path);
    }

    private async void OnExportAll(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } vm) return;
        if (await SavePath(Strings.Picker_ExportAll, $"{Strings.Backup_Name} {DateTime.Now:yyyy-MM-dd}", BackupFiles) is { } path) await vm.ExportAllAsync(path);
    }

    private async Task<string?> SavePath(string title, string name, FilePickerFileType type)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = name,
            DefaultExtension = type.Patterns![0].TrimStart('*', '.'),
            FileTypeChoices = [type],
            ShowOverwritePrompt = true,
        });
        return file?.TryGetLocalPath();
    }

    private async void OnOpenLibrary(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } vm) return;
        Directory.CreateDirectory(vm.LibraryFolder);
        await Launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(vm.LibraryFolder));
    }

    private async void OnMoveLibrary(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } vm) return;
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = Strings.Picker_Library });
        if (folders.Count > 0 && folders[0].TryGetLocalPath() is { } path) await vm.MoveLibraryAsync(path);
    }

    private void OnQuit(object? sender, RoutedEventArgs e) => Close();

    // ───────────── Help: a window beside this one, opened at the page of the selected step ─────────────

    private HelpWindow? help;

    private void OnHelp(object? sender, RoutedEventArgs e) => ShowHelp();

    private void ShowHelp()
    {
        var key = HelpCatalog.KeyOf(ViewModel?.SelectedStep?.Settings.ToModule().GetType());
        if (help is { DataContext: HelpViewModel shown })
        {
            shown.Show(key);
            help.Activate();
            return;
        }
        help = new HelpWindow { DataContext = new HelpViewModel(key) };
        help.Closed += (_, _) => help = null;
        help.Show();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.F1)
        {
            e.Handled = true;
            ShowHelp();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        help?.Close();
        base.OnClosed(e);
    }

    // Closing with unsaved changes asks first, in the top bar.
    private bool closeConfirmed;

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!closeConfirmed && ViewModel is { IsModified: true } vm)
        {
            e.Cancel = true;
            vm.CloseAfterGuard(() =>
            {
                closeConfirmed = true;
                Close();
            });
        }
        base.OnClosing(e);
    }

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
