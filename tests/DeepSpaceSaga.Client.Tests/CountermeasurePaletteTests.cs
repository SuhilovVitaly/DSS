using System.Text.Json;
namespace DeepSpaceSaga.Client.Tests;

public class CountermeasurePaletteTests
{
    [Fact]
    public void Countermeasure_palette_is_external_and_complete()
    {
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data/UI/combat-visuals.json")));
        foreach (var key in new[] { "countermeasure", "countermeasureTrail", "countermeasurePrediction", "countermeasureIntercept", "defenseRange", "defenseText" })
            Assert.Matches("^#[0-9A-F]{8}$", json.RootElement.GetProperty(key).GetString()!);
        Assert.Equal("#FFFF0066", json.RootElement.GetProperty("prediction").GetString());
    }
}
