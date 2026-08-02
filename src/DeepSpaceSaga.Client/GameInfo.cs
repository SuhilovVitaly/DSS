using System.Reflection;
using System.Text.Json;

namespace DeepSpaceSaga.Client;

public static class GameInfo
{
    private static readonly Lazy<Data> _data = new(Load);

    public static string Title => _data.Value.Title;
    public static string Version => _data.Value.Version;

    private static Data Load()
    {
        string title = "Deep Space Saga";
        string baseVersion = "1.0.0";

        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "gameinfo.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("title", out var t))
                    title = t.GetString() ?? title;

                if (root.TryGetProperty("version", out var v))
                    baseVersion = v.GetString() ?? baseVersion;
            }
        }
        catch
        {
            // Use defaults
        }

        string fullVersion = $"Version {baseVersion} (build {BuildNumber})";
        return new Data(title, fullVersion);
    }

    private static string BuildNumber
    {
        get
        {
            try
            {
                var asm = Assembly.GetEntryAssembly();
                if (asm?.Location is { } path && File.Exists(path))
                {
                    var dt = File.GetLastWriteTimeUtc(path);
                    return dt.ToString("yyyyMMdd.HHmm");
                }
            }
            catch { }
            return "dev";
        }
    }

    private sealed record Data(string Title, string Version);
}
