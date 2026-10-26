using System.Text.Json;
using System.Text.Json.Nodes;

namespace DeepSpaceSaga.Client;

/// <summary>Installation content is read-only; writable state belongs to the user's profile.</summary>
public sealed record AppPaths(string Content, string UserData, string UserConfig, bool Portable)
{
    private static readonly Lazy<AppPaths> Default = new(() => Resolve(AppContext.BaseDirectory,
        OperatingSystem.IsWindows() ? "windows" : "unix",
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), Environment.GetEnvironmentVariable));
    private static readonly AsyncLocal<AppPaths?> Override = new();
    public static AppPaths Current => Override.Value ?? Default.Value;
    public string Settings => Path.Combine(UserConfig, "Settings.json");
    public string Saves => Path.Combine(UserData, "Saves");
    public string DebugTuning => Path.Combine(UserData, "DebugTuning");
    public string CombatMetrics => Path.Combine(UserData, "CombatMetrics");
    public string Scenarios => Path.Combine(Content, "Scenarios");

    internal static IDisposable Use(AppPaths paths)
    {
        var previous = Override.Value;
        Override.Value = paths;
        return new Scope(() => Override.Value = previous);
    }
    private sealed class Scope(Action restore) : IDisposable { public void Dispose() => restore(); }

    internal static AppPaths Resolve(string content, string platform, string home, Func<string, string?> environment)
    {
        content = Path.GetFullPath(content);
        if (File.Exists(Path.Combine(content, "portable.txt"))) return new(content, content, content, true);
        string AbsoluteOr(string variable, string fallback) => environment(variable) is { } path && Path.IsPathFullyQualified(path) ? path : fallback;
        string data, config;
        if (platform == "windows")
        {
            data = AbsoluteOr("LOCALAPPDATA", Path.Combine(home, "AppData", "Local"));
            config = data;
        }
        else
        {
            data = AbsoluteOr("XDG_DATA_HOME", Path.Combine(home, ".local", "share"));
            config = AbsoluteOr("XDG_CONFIG_HOME", Path.Combine(home, ".config"));
        }
        return new(content, Path.Combine(data, "DeepSpaceSaga"), Path.Combine(config, "DeepSpaceSaga"), false);
    }

    public void Initialize(Action<string>? report = null)
    {
        Directory.CreateDirectory(UserConfig);
        Directory.CreateDirectory(UserData);
        Directory.CreateDirectory(Saves);
        if (Portable) return;
        string template = Path.Combine(Content, "Settings.json");
        if (!File.Exists(Settings))
        {
            var defaults = JsonNode.Parse(File.ReadAllText(template))!.AsObject();
            defaults["contentRoot"] = Content;
            WriteSettings(defaults, overwrite: false);
        }
        // contentRoot is installation-owned; retain every user preference when an install moves.
        var settings = JsonNode.Parse(File.ReadAllText(Settings))!.AsObject();
        if (settings["contentRoot"]?.GetValue<string>() != Content)
        {
            settings["contentRoot"] = Content;
            WriteSettings(settings, overwrite: true);
        }
        string marker = Path.Combine(UserData, ".saves-migrated");
        if (File.Exists(marker)) return;
        string legacy = Path.Combine(Content, "Saves");
        int copied = 0;
        if (!Directory.EnumerateFileSystemEntries(Saves).Any() && Directory.Exists(legacy))
        {
            foreach (string file in Directory.EnumerateFiles(legacy, "*", SearchOption.AllDirectories))
            {
                string target = Path.Combine(Saves, Path.GetRelativePath(legacy, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                try { File.Copy(file, target, overwrite: false); copied++; }
                catch (IOException) when (File.Exists(target)) { }
            }
        }
        File.WriteAllText(marker, "Legacy saves checked; originals retained.\n");
        if (copied > 0) report?.Invoke($"Migrated {copied} legacy save files from '{legacy}' to '{Saves}'; originals retained.");
    }

    private void WriteSettings(JsonObject settings, bool overwrite)
    {
        string temporary = Settings + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllText(temporary, settings.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
            try { File.Move(temporary, Settings, overwrite); }
            catch (IOException) when (!overwrite && File.Exists(Settings)) { }
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
