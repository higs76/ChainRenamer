using System.Globalization;
using ChainRenamer.App.Resources;

namespace ChainRenamer.App.ViewModels;

/// <summary>A language of the interface; an empty code is the language of Windows.</summary>
public sealed record LanguageChoice(string Code, string Label);

// The settings window works on the main view model: the preferences live here.
public partial class MainViewModel
{
    /// <summary>The language chosen when the application started: a new choice needs a restart.</summary>
    private static readonly string? StartLanguage = Preferences.Load().Language;

    // Each language is named in its own language, so that anyone finds theirs.
    public IReadOnlyList<LanguageChoice> Languages { get; } =
    [
        new("", string.Format(Strings.Settings_WindowsLanguage, CultureInfo.InstalledUICulture.NativeName)),
        new("en", "English"),
        new("fr", "Français"),
    ];

    public string Language
    {
        get => preferences.Language ?? "";
        set
        {
            var language = value.Length == 0 ? null : value;
            if (language == preferences.Language) return;
            preferences = preferences with { Language = language };
            preferences.Save();
            OnPropertyChanged();
            OnPropertyChanged(nameof(NeedsRestart));
        }
    }

    public bool NeedsRestart => preferences.Language != StartLanguage;
}
