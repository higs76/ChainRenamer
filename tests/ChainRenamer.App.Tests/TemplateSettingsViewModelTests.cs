using System.Globalization;
using ChainRenamer.App.ViewModels.Modules;
using ChainRenamer.Engine;

namespace ChainRenamer.App.Tests;

public class TemplateSettingsViewModelTests
{
    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    public void Every_variable_of_the_menu_is_known_to_the_engine(string language)
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo(language);
        try
        {
            var item = new Item(FileId.New(), "a.mkv") { Folder = @"C:\Videos", Size = 10, Modified = DateTime.Today };
            foreach (var variable in new TemplateSettingsViewModel().Variables.Select(v => (string)v.Value))
                Assert.NotEqual(variable, Variables.Expand(variable, item, 1));
        }
        finally { CultureInfo.CurrentUICulture = previous; }
    }

    [Fact]
    public void The_menu_adds_the_variable_at_the_end()
    {
        var vm = new TemplateSettingsViewModel(new("{folder} - "));

        vm.AddVariableCommand.Execute("{n:2}");

        Assert.Equal("{folder} - {n:2}", vm.Template);
    }
}
