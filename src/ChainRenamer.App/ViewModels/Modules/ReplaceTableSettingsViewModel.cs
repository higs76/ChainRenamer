using System.Collections.ObjectModel;
using System.ComponentModel;
using ChainRenamer.App.Resources;
using ChainRenamer.Engine;
using ChainRenamer.Engine.Modules;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChainRenamer.App.ViewModels.Modules;

/// <summary>One line of the table, as edited in the panel.</summary>
public sealed partial class ReplacePairViewModel(ReplacePair pair, Action<ReplacePairViewModel> remove) : ObservableObject
{
    [ObservableProperty] public partial string Find { get; set; } = pair.Find;
    [ObservableProperty] public partial string ReplaceWith { get; set; } = pair.ReplaceWith;
    [ObservableProperty] public partial bool Enabled { get; set; } = pair.Enabled;

    public ReplacePair ToPair() => new(Find, ReplaceWith, Enabled);

    [RelayCommand]
    private void Remove() => remove(this);
}

public partial class ReplaceTableSettingsViewModel : ModuleSettingsViewModel
{
    public ReplaceTableSettingsViewModel(ReplaceTableModule? module = null)
    {
        Pairs.CollectionChanged += (_, e) =>
        {
            // Any line added, removed or edited changes the module.
            foreach (var pair in e.NewItems?.OfType<ReplacePairViewModel>() ?? []) pair.PropertyChanged += OnPairChanged;
            OnPropertyChanged(nameof(Pairs));
        };
        MatchCase = module?.MatchCase ?? false;
        Add(module?.Pairs ?? [new("", "")]);
    }

    public ObservableCollection<ReplacePairViewModel> Pairs { get; } = [];

    [ObservableProperty] public partial bool MatchCase { get; set; }

    public override string Title => Strings.Module_ReplaceTable;
    public override string Description => Strings.ReplaceTable_Description;

    public override IModule ToModule() => new ReplaceTableModule([.. Pairs.Select(p => p.ToPair())], MatchCase);

    [RelayCommand]
    private void AddLine() => Add([new("", "")]);

    /// <summary>Adds the lines of a pasted or loaded text, after the last line that is not empty.</summary>
    public void Import(string text)
    {
        foreach (var empty in Pairs.Where(p => p.Find.Length == 0 && p.ReplaceWith.Length == 0).ToList()) Pairs.Remove(empty);
        Add(ReplaceTableModule.Parse(text));
    }

    /// <summary>The table as text, to save it to a file and load it in another chain.</summary>
    public string Export() => ReplaceTableModule.Format(Pairs.Select(p => p.ToPair()));

    private void Add(IEnumerable<ReplacePair> pairs)
    {
        foreach (var pair in pairs) Pairs.Add(new(pair, p => Pairs.Remove(p)));
    }

    private void OnPairChanged(object? sender, PropertyChangedEventArgs e) => OnPropertyChanged(nameof(Pairs));
}
