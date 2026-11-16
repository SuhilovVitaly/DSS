using System.Text.Json;

namespace DeepSpaceSaga.Client.UI.Demo;

public static class DemoStartPresentation
{
    public static bool IsDemoProfile(string settingsPath)
    {
        using var settings = JsonDocument.Parse(File.ReadAllText(settingsPath));
        if (!settings.RootElement.TryGetProperty("gameSettings", out var game) ||
            !game.TryGetProperty("campaignProfile", out var profile)) return false;
        return profile.GetString() switch
        {
            "demo" => true,
            "development" => false,
            _ => throw new InvalidDataException("Unknown campaign profile.")
        };
    }
}
