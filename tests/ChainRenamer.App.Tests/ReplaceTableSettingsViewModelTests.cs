using ChainRenamer.App.ViewModels.Modules;
using ChainRenamer.Engine.Modules;

namespace ChainRenamer.App.Tests;

public class ReplaceTableSettingsViewModelTests
{
    [Fact]
    public void Pasted_lines_replace_the_empty_line()
    {
        var vm = new ReplaceTableSettingsViewModel();

        vm.Import("VOSTFR\tVOST\nS01 → Saison 1");

        Assert.Equal([new("VOSTFR", "VOST"), new("S01", "Saison 1")], ((ReplaceTableModule)vm.ToModule()).Pairs.ToArray());
    }

    [Fact]
    public void Every_edit_changes_the_module()
    {
        var vm = new ReplaceTableSettingsViewModel(new([new("a", "b"), new("c", "d")]));
        int changes = 0;
        vm.Changed += (_, _) => changes++;

        vm.Pairs[0].ReplaceWith = "x";
        vm.Pairs[1].Enabled = false;
        vm.Pairs[0].RemoveCommand.Execute(null);
        vm.AddLineCommand.Execute(null);

        Assert.Equal(4, changes);
        Assert.Equal([new("c", "d", false), new("", "")], ((ReplaceTableModule)vm.ToModule()).Pairs.ToArray());
    }

    [Fact]
    public void Export_is_read_back_by_import()
    {
        var vm = new ReplaceTableSettingsViewModel(new([new("ß", "ss"), new(" - ", " ")]));
        var copy = new ReplaceTableSettingsViewModel();

        copy.Import(vm.Export());

        Assert.Equal(vm.ToModule(), copy.ToModule());
    }
}
