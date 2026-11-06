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
    [ThreadStatic] private static Dictionary<string, string>? _testStrings;

    public static string Get(string key) =>
        (_testStrings ?? _strings.Value).TryGetValue(key, out var value) ? value : key;

    internal static string WorldManeuverError(string reason)
    {
        string key = "World.Panel.Reason." + reason;
        string text = Get(key);
        return text == key ? Get("World.CommandRejected") : text;
    }

    internal static string WorldPanelCaption(string identity) => identity switch
    {
        "Navigation" => Get("World.Panel.Navigation"),
        "Maneuver" => Get("World.Panel.Maneuver"),
        "Engine" => Get("World.Panel.Engine"),
        "Space Control" => Get("World.Panel.SpaceControl"),
        "Player Ship" => Get("World.Panel.PlayerShip"),
        "Selected Object" => Get("World.Panel.SelectedObject"),
        _ => identity
    };

    internal static string WorldFieldLabel(string label) => label switch
    {
        "Name" => Get("World.Field.Name"),
        "Speed" => Get("World.Field.Speed"),
        "Direction" => Get("World.Field.Direction"),
        "Captain" => Get("World.Field.Captain"),
        "Distance" => Get("World.Field.Distance"),
        _ => label
    };

    /// <summary>Thread-local test seam using the actual locale files; runtime language still applies after restart.</summary>
    internal static IDisposable UseLocaleForTests(string language)
    {
        var previous = _testStrings;
        _testStrings = LoadLocaleFile(language) ?? throw new InvalidOperationException("Missing test locale: " + language);
        return new LocaleScope(previous, Environment.CurrentManagedThreadId);
    }
    private sealed class LocaleScope(Dictionary<string, string>? previous, int threadId) : IDisposable
    {
        public void Dispose()
        {
            if (Environment.CurrentManagedThreadId != threadId) throw new InvalidOperationException("Locale test scope must remain on its UI thread.");
            _testStrings = previous;
        }
    }

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
