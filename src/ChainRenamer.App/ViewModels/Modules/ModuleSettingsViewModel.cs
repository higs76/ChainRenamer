using System.ComponentModel;
using ChainRenamer.Engine;

namespace ChainRenamer.App.ViewModels.Modules;

/// <summary>
/// The settings panel of one module. Its view is found by name:
/// <c>ViewModels.Modules.XxxSettingsViewModel</c> → <c>Views.Modules.XxxSettingsView</c>.
/// </summary>
public abstract class ModuleSettingsViewModel : ViewModelBase
{
    /// <summary>Raised on any property change, so that the step rebuilds its module.</summary>
    public event EventHandler? Changed;

    public abstract string Title { get; }

    /// <summary>What the module does, in one sentence, shown next to the step name.</summary>
    public abstract string Description { get; }

    public abstract IModule ToModule();

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
