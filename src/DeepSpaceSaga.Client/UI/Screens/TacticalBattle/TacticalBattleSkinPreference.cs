using System.Text.Json;
using System.Text.Json.Nodes;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal static class TacticalBattleSkinPreference
{
    private const string SettingsGroup = "gameSettings";
    private const string SkinProperty = "tacticalBattleSkinId";

    internal static string? Read(string path, Action<string>? warning = null)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new FormatException("settings root must be an object");
            if (!document.RootElement.TryGetProperty(SettingsGroup, out var settings))
                return null;
            if (settings.ValueKind != JsonValueKind.Object)
                throw new FormatException($"{SettingsGroup} must be an object");
            if (!settings.TryGetProperty(SkinProperty, out var property))
                return null;
            string? id = property.ValueKind == JsonValueKind.String ? property.GetString() : null;
            if (!TacticalBattleSkin.IsValidId(id))
                throw new FormatException($"{SettingsGroup}.{SkinProperty} must be a lower ASCII kebab-case ID");
            return id;
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
        catch (Exception ex) when (IsSettingsError(ex))
        {
            warning?.Invoke($"Read skin preference '{path}': {ex.Message}");
            return null;
        }
    }

    internal static bool TryWrite(string path, string id, Action<string>? warning = null)
    {
        string? temp = null;
        try
        {
            if (!TacticalBattleSkin.IsValidId(id))
                throw new FormatException($"{SkinProperty} must be a lower ASCII kebab-case ID");
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

            if (settings[SkinProperty] is JsonValue current && current.TryGetValue<string>(out var existing) &&
                string.Equals(existing, id, StringComparison.Ordinal))
                return true;

            settings[SkinProperty] = id;
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
            warning?.Invoke($"Save skin preference '{path}': {ex.Message}");
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
