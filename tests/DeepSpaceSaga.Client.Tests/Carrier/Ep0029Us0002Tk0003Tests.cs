using System.Text.Json;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class Ep0029Us0002Tk0003Tests
{
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    private static JsonDocument PilotsJson() => JsonDocument.Parse(File.ReadAllText(
        Path.Combine(ClientRoot, "Data", "TacticalBattle", "pilots.json")));

    private static JsonDocument PortraitsJson() => JsonDocument.Parse(File.ReadAllText(
        Path.Combine(ClientRoot, "Data", "UI", "tactical-battle-pilots.json")));

    [Fact]
    public void FourPilotsFemaleAndAssetsExist()
    {
        using var portraits = PortraitsJson();
        var pilots = portraits.RootElement.GetProperty("pilots");
        string[] ids = ["vera", "yun", "marta", "oleg"];
        Assert.Equal(4, pilots.EnumerateObject().Count());

        foreach (string id in ids)
        {
            var entry = pilots.GetProperty(id);
            Assert.True(entry.GetProperty("female").GetBoolean(), $"{id} must be female");

            string relative = entry.GetProperty("portrait").GetString()!;
            string path = Path.Combine(new[] { ClientRoot, "Images", "Persons" }
                .Concat(relative.Split('/')).ToArray()) + ".png";
            Assert.True(File.Exists(path), $"missing portrait {path}");

            using var bitmap = SKBitmap.Decode(path);
            Assert.NotNull(bitmap);
            Assert.True(bitmap.Width > 0 && bitmap.Height > 0);
        }
    }

    [Fact]
    public void SkillsUnchangedUnderNewNames()
    {
        using var pilots = PilotsJson();
        Assert.Equal("Evelyn Ward", pilots.RootElement.GetProperty("vera").GetProperty("name").GetString());
        Assert.Equal("Audrey Bennett", pilots.RootElement.GetProperty("yun").GetProperty("name").GetString());
        Assert.Equal("Claire Morgan", pilots.RootElement.GetProperty("marta").GetProperty("name").GetString());
        Assert.Equal("Rachel Hayes", pilots.RootElement.GetProperty("oleg").GetProperty("name").GetString());

        (string key, int accuracy, int maneuver, int initiative)[] expected =
        [
            ("vera", 55, 52, 55),
            ("yun", 53, 55, 53),
            ("marta", 59, 54, 59),
            ("oleg", 50, 51, 50),
        ];
        foreach (var (key, accuracy, maneuver, initiative) in expected)
        {
            var entry = pilots.RootElement.GetProperty(key);
            Assert.Equal(accuracy, entry.GetProperty("accuracy").GetInt32());
            Assert.Equal(maneuver, entry.GetProperty("maneuver").GetInt32());
            Assert.Equal(initiative, entry.GetProperty("initiative").GetInt32());
        }
    }

    [Fact]
    public void OfficerPortraitNotReused()
    {
        using var portraits = PortraitsJson();
        var pilots = portraits.RootElement.GetProperty("pilots");
        const string officerPortrait = "CHR-20260915-104309-12BAB0";
        foreach (var pilot in pilots.EnumerateObject())
        {
            Assert.DoesNotContain(officerPortrait, pilot.Value.GetProperty("portrait").GetString(), StringComparison.Ordinal);
        }
    }
}
