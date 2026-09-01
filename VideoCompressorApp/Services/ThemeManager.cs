using System.Collections.ObjectModel;
using System.Windows;

namespace VideoCompressor.Services;

public static class ThemeManager
{
    public static readonly string[] ThemeModes = { "Light", "Dark" };
    public static readonly string[] AccentColors = { "Blue", "Green", "Purple", "Orange" };

    public static void Apply(string themeMode, string accentColor)
    {
        var dictionaries = Application.Current.Resources.MergedDictionaries;

        ReplaceDictionary(dictionaries, "/Themes/Light.xaml", "/Themes/Dark.xaml",
            themeMode == "Dark" ? "/Themes/Dark.xaml" : "/Themes/Light.xaml");

        ReplaceDictionary(dictionaries,
            "/Themes/AccentBlue.xaml", "/Themes/AccentGreen.xaml", "/Themes/AccentPurple.xaml", "/Themes/AccentOrange.xaml",
            accentColor switch
            {
                "Green" => "/Themes/AccentGreen.xaml",
                "Purple" => "/Themes/AccentPurple.xaml",
                "Orange" => "/Themes/AccentOrange.xaml",
                _ => "/Themes/AccentBlue.xaml",
            });
    }

    private static void ReplaceDictionary(Collection<ResourceDictionary> dictionaries, params string[] candidatesThenNewPath)
    {
        var candidates = candidatesThenNewPath[..^1];
        var newPath = candidatesThenNewPath[^1];

        for (int i = dictionaries.Count - 1; i >= 0; i--)
        {
            var source = dictionaries[i].Source?.OriginalString ?? "";
            if (Array.Exists(candidates, c => source.EndsWith(c, StringComparison.OrdinalIgnoreCase)))
                dictionaries.RemoveAt(i);
        }

        dictionaries.Add(new ResourceDictionary { Source = new Uri(newPath, UriKind.Relative) });
    }
}
