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

    /// <summary>Combat UI has locale-owned generic messages, never caller-supplied literal fallbacks.</summary>
    internal static string Combat(string key, Func<string, string>? localize = null)
    {
        var get = localize ?? Get;
        string? Find(string suffix)
        {
            string full = "Combat." + suffix;
            try { string value = get(full); return string.IsNullOrWhiteSpace(value) || value == full ? null : value; }
            catch (KeyNotFoundException) { return null; }
        }
        if (Find(key) is { } translated) return translated;
        // Profile IDs are configurable public data. Preserve unknown custom identifiers, not UI prose.
        if (key.StartsWith("Profile.", StringComparison.Ordinal)) return key["Profile.".Length..];
        string generic = key.StartsWith("Reason.", StringComparison.Ordinal) ? "CommandRejected" :
            key.StartsWith("Forecast.", StringComparison.Ordinal) ? "Forecast.Unknown" : "Unknown";
        return Find(generic) ?? "—";
    }

    internal static string CombatModuleName(string moduleTypeId, string capturedName)
    {
        string key = "Combat.Module." + moduleTypeId, translated = Get(key);
        return translated == key ? capturedName : translated;
    }

    internal static string CombatPanelCaption(string identity)
    {
        foreach (var (name, key) in new[] { ("Torpedo Launcher", "TorpedoLauncher"), ("Countermeasure Launcher", "CountermissileLauncher"),
            ("Navigation", "Navigation"), ("Maneuver", "Maneuver"), ("Engine", "Engine"), ("Space Control", "SpaceControl"),
            ("Player Ship", "PlayerShip"), ("Selected Object", "SelectedObject") })
        {
            if (!identity.StartsWith(name, StringComparison.Ordinal)) continue;
            string suffix = identity[name.Length..];
            if (suffix.Length == 0 || suffix.StartsWith(' ') && suffix.Trim().All(char.IsDigit)) return Combat("Panel." + key) + suffix;
        }
        return identity;
    }

    internal static string CombatFieldLabel(string label) => label switch
    {
        "Name" => Combat("Field.Name"),
        "Speed" => Combat("Field.Speed"),
        "Direction" => Combat("Field.Direction"),
        "Captain" => Combat("Field.Captain"),
        "Distance" => Combat("Field.Distance"),
        "Target" => Combat("Target"),
        "Travelled" => Combat("Field.Travelled"),
        "ETA" => Combat("ETA"),
        "Hit chance" => Combat("Field.LegacyHitChance"),
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
