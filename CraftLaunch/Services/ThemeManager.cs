using System.Windows;

namespace CraftLaunch.Services;

public static class ThemeManager
{
    public static void Apply(string theme)
    {
        var source = new Uri($"pack://application:,,,/Themes/Theme.{theme}.xaml");
        var dictionaries = Application.Current.Resources.MergedDictionaries;
        var next = new ResourceDictionary { Source = source };

        var index = dictionaries.IndexOf(d => d.Source?.OriginalString.Contains("Theme.") == true);
        if (index >= 0)
            dictionaries[index] = next;
        else
            dictionaries.Insert(0, next);
    }

    private static int IndexOf(this IList<ResourceDictionary> dictionaries, Func<ResourceDictionary, bool> predicate)
    {
        for (var i = 0; i < dictionaries.Count; i++)
        {
            if (predicate(dictionaries[i]))
                return i;
        }
        return -1;
    }
}
