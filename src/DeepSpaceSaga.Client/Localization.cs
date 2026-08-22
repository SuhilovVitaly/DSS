using System.Text.Json;

namespace DeepSpaceSaga.Client;

/// <summary>
/// Resolves UI text by key from the JSON dictionary under Data\Locale\ that matches the
/// language saved in Settings.json (gameSettings.language, written by SkiaWindow's
/// SaveLanguage). Loaded once and cached, same "applies after restart" model as the
/// other gameSettings-backed values in SkiaWindow.cs — no live-switch support yet.
/// </summary>
public static class Localization
{
    private static readonly Lazy<Dictionary<string, string>> _strings = new(Load);

    public static string Get(string key) =>
        _strings.Value.TryGetValue(key, out var value) ? value : key;

    private static Dictionary<string, string> Load()
    {
        var language = ReadLanguageSetting();
        var strings = LoadLocaleFile(language);
        if (strings is { Count: > 0 })
            return strings;

        return LoadLocaleFile("English") ?? new Dictionary<string, string>();
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
