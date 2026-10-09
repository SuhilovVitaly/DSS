using System.Text.Json;

namespace DeepSpaceSaga.Client;

/// <summary>
/// Resolves UI text by key from the JSON dictionary under Data\Locale\ that matches the
/// language saved in Settings.json (gameSettings.language, written by SkiaWindow's
/// SaveLanguage). Settings can atomically publish a new dictionary and revision.
/// </summary>
public static class Localization
{
    private sealed record LocaleState(string Language, Dictionary<string, string> Strings, long Revision);
    private static readonly object Sync = new();
    private static LocaleState _state = Load(ReadLanguageSetting(), 0);
    internal static long Revision => Volatile.Read(ref _state).Revision;
    internal static string CurrentLanguage => Volatile.Read(ref _state).Language;

    public static string Get(string key) =>
        Volatile.Read(ref _state).Strings.TryGetValue(key, out var value) ? value : key;

    internal static void SetLanguage(string language)
    {
        language = language is "English" or "Russian" ? language : "English";
        lock (Sync)
        {
            if (_state.Language == language) return;
            Volatile.Write(ref _state, Load(language, _state.Revision + 1));
        }
    }

    private static LocaleState Load(string language, long revision)
    {
        var strings = LoadLocaleFile("English") ?? new Dictionary<string, string>();
        if (language != "English" && LoadLocaleFile(language) is { } translated)
            foreach (var (key, value) in translated) strings[key] = value;
        return new(language, strings, revision);
    }

    private static string ReadLanguageSetting()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Settings.json");
            if (!File.Exists(path))
                return "English";

            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.TryGetProperty("gameSettings", out var gs) &&
                gs.TryGetProperty("language", out var language))
            {
                var value = language.GetString();
                return value is "English" or "Russian" ? value : "English";
            }

            return "English";
        }
        catch
        {
            return "English";
        }
    }

    internal static Dictionary<string, string>? LoadLocaleFile(string language)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Data", "Locale", $"{language}.json");
            if (!File.Exists(path))
                return null;

            return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path));
        }
        catch
        {
            return null;
        }
    }
}
