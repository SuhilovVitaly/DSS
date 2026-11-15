using System.Text.Json;
using System.Text.Json.Nodes;

namespace DeepSpaceSaga.Client.UI.Screens.Tutorial;

/// <summary>
/// <c>Settings.json</c> → <c>gameSettings.tutorialHints</c> (bool). A missing, unreadable or non-bool value means "on";
/// writes merge into the existing file (every other key is kept) and replace it atomically (temp file + move), following
/// <c>TacticalBattleSkinPreference</c>.
/// </summary>
internal static class TutorialHintsPreference
{
    private const string SettingsGroup = "gameSettings";
    internal const string Property = "tutorialHints";

    internal static bool Read(string settingsPath, Action<string>? warning = null)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(settingsPath));
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new FormatException("settings root must be an object");
            if (!document.RootElement.TryGetProperty(SettingsGroup, out var settings))
                return true;
            if (settings.ValueKind != JsonValueKind.Object)
                throw new FormatException($"{SettingsGroup} must be an object");
            if (!settings.TryGetProperty(Property, out var property))
                return true;
            return property.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => throw new FormatException($"{SettingsGroup}.{Property} must be true or false"),
            };
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return true;
        }
        catch (Exception ex) when (IsSettingsError(ex))
        {
            warning?.Invoke($"Read tutorial hints preference '{settingsPath}': {ex.Message}");
            return true;
        }
    }

    internal static bool TryWrite(string settingsPath, bool enabled, Action<string>? warning = null)
    {
        string? temp = null;
        string path = settingsPath;
        try
        {
            path = Path.GetFullPath(path);
            var root = ReadObject(path);
            JsonObject settings;
            if (!root.TryGetPropertyValue(SettingsGroup, out var node))
            {
                settings = new JsonObject();
                root.Add(SettingsGroup, settings);
            }
            else
            {
                settings = node as JsonObject ?? throw new FormatException($"{SettingsGroup} must be an object");
            }

            if (settings[Property] is JsonValue current && current.GetValueKind() is JsonValueKind.True or JsonValueKind.False &&
                current.GetValue<bool>() == enabled)
                return true;

            settings[Property] = enabled;
            string directory = Path.GetDirectoryName(path)!;
            string candidate = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
            using (var stream = new FileStream(candidate, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                temp = candidate;
                using var writer = new Utf8JsonWriter(stream, new() { Indented = true });
                root.WriteTo(writer);
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }

            File.Move(temp!, path, overwrite: true);
            temp = null;
            return true;
        }
        catch (Exception ex) when (IsSettingsError(ex))
        {
            warning?.Invoke($"Save tutorial hints preference '{path}': {ex.Message}");
            return false;
        }
        finally
        {
            if (temp is not null)
            {
                try { File.Delete(temp); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    warning?.Invoke($"Remove temporary preference '{temp}': {ex.Message}");
                }
            }
        }
    }

    private static JsonObject ReadObject(string path)
    {
        try
        {
            return JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? throw new FormatException("settings root must be an object");
        }
        catch (FileNotFoundException)
        {
            return new();
        }
    }

    private static bool IsSettingsError(Exception ex) =>
        ex is IOException or UnauthorizedAccessException or FormatException or JsonException or
            InvalidOperationException or ArgumentException or NotSupportedException;
}
