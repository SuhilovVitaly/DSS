using System.Text.Json.Nodes;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

public sealed class EnvironmentFieldContentTests
{
    [Fact]
    public void ThreeFieldKindsInContent()
    {
        var config = EngineContentLoader.LoadSolarSystemGenerationConfig(DefaultSystemContentTests.Settings)!;
        Assert.Equal(new EnvironmentGenerationConfig(1, 2, 2, 5000, 100, 250, 0.5), config.Environment);
        foreach (string name in config.EnabledScenarios)
        {
            using var engine = SimulationEngine.CreateFromScenarioFile(DefaultSystemContentTests.Settings,
                Path.Combine(AppContext.BaseDirectory, "Scenarios", name, "scenario.json"));
            var snapshot = engine.CaptureSnapshot();
            var fields = snapshot.AiMap!.Fields;
            Assert.Single(fields, f => f.Kind == "Radiation");
            Assert.Equal(2, fields.Count(f => f.Kind == "Dust")); Assert.Equal(2, fields.Count(f => f.Kind == "Debris"));
            Assert.Equal(5, fields.Select(f => f.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.All(fields, f => Assert.DoesNotContain(snapshot.Objects, o => o.ObjectId == f.Id));
            Assert.All(fields.Where(f => f.Kind == "Radiation"), f => Assert.Equal(50000, f.OuterRadius));
            Assert.All(fields.Where(f => f.Kind == "Debris"), f => Assert.Equal(2500, f.OuterRadius));
        }
    }

    [Fact]
    public void FieldContentHasNoEffects()
    {
        string folder = Path.Combine(Path.GetTempPath(), "dss-fields-" + Guid.NewGuid()); Directory.CreateDirectory(folder);
        try
        {
            string settings = Path.Combine(folder, "Settings.json"), content = Path.Combine(folder, "solar.json");
            File.WriteAllText(settings, """{"typeData":{"solarSystem":"solar.json"},"defaultScenario":"scenario.json"}""");
            var original = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data/Maps/solar-system.json")))!;
            foreach (string property in new[] { "damage", "priceModifier", "sensorRangeMultiplier", "fuelModifier", "radiationRadiusKm" })
            {
                var invalid = original.DeepClone(); invalid["environment"]![property] = -1;
                File.WriteAllText(content, invalid.ToJsonString());
                var error = Assert.Throws<ContentException>(() => EngineContentLoader.LoadSolarSystemGenerationConfig(settings));
                Assert.Contains(property, error.Message); Assert.Contains(content, error.Message);
            }
        }
        finally { Directory.Delete(folder, true); }
    }
}
