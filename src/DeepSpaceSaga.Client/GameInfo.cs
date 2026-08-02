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

        var asm = Assembly.GetEntryAssembly();
        var asmVersion = asm?.GetName().Version;
        string fullVersion = asmVersion is { } ver && (ver.Build > 0 || ver.Revision > 0)
            ? $"Version {baseVersion}.{ver.Build}.{ver.Revision}"
            : $"Version {baseVersion}";

        return new Data(title, fullVersion);
    }

    private sealed record Data(string Title, string Version);
}
