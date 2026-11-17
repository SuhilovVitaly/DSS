using System.Text.Json;

namespace DeepSpaceSaga.Client.UI.Demo;

public static class DemoStartPresentation
{
    public static bool IsDemoProfile(string settingsPath)
    {
        try
        {
            using var settings = JsonDocument.Parse(File.ReadAllText(settingsPath));
            if (!settings.RootElement.TryGetProperty("gameSettings", out var game) ||
                !game.TryGetProperty("campaignProfile", out var profile)) return false;
            if (profile.ValueKind == JsonValueKind.String && profile.GetString() == "demo") return true;
            if (profile.ValueKind == JsonValueKind.String && profile.GetString() == "development") return false;
            InterfaceLog.Write("Unknown campaign profile; using development menu.");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
        {
            InterfaceLog.Write("Campaign profile unavailable; using development menu: " + error.Message);
        }
        return false;
    }
}
