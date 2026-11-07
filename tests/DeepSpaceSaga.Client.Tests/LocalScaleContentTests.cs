using System.Text.Json.Nodes;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Client.Tests;

public sealed class LocalScaleContentTests
{
    [Fact]
    public void ShippedLocalScaleHasExplicitReference()
    {
        var json = JsonNode.Parse(File.ReadAllText(DefaultSystemContentTests.Settings))!;
        var declared = json["gameSettings"]!["tacticalMap"]!;
        Assert.Equal(4, declared["referenceSpeedKmS"]!.GetValue<double>());
        Assert.Equal(4, declared["maxLocalHours"]!.GetValue<double>());
        var settings = TacticalMapSettings.Load(DefaultSystemContentTests.Settings);
        Assert.Equal(100, settings.MetersPerPixel[0]);
        Assert.Equal(4, settings.ReferenceSpeedKmS);
        Assert.Equal(4, settings.MaxLocalHours);
        var screen = new GameSessionScreen(new SnapshotBuffer(), new LinearMotionPredictor(), mapSettings: settings);
        using var locale = Localization.UseLocaleForTests("English");
        Assert.Equal("0.25 h at reference 4 km/s", screen.RulerReferenceText(12000));
    }

    [Fact]
    public void OldSettingsRemainCompatible()
    {
        string folder = Path.Combine("D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale", "local-settings-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var root = JsonNode.Parse(File.ReadAllText(DefaultSystemContentTests.Settings))!;
            var map = root["gameSettings"]!["tacticalMap"]!.AsObject();
            map.Remove("referenceSpeedKmS"); map.Remove("maxLocalHours");
            string path = Path.Combine(folder, "Settings.json");
            File.WriteAllText(path, root.ToJsonString());
            var legacy = TacticalMapSettings.Load(path);
            Assert.Equal(4, legacy.ReferenceSpeedKmS); Assert.Equal(4, legacy.MaxLocalHours);
            Assert.Equal(100, legacy.MetersPerPixel[0]);
            Assert.Equal(1, legacy.MaximumPpu);
            Assert.True(MapScalePolicy.MaximumPpu(legacy, 12288, 3456, double.NaN) > legacy.MaximumPpu);
        }
        finally { Directory.Delete(folder, true); }
    }
}
