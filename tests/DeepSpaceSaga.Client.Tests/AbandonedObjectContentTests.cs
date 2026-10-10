using System.Text.Json.Nodes;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Client.Tests;

public sealed class AbandonedObjectContentTests
{
    [Fact]
    public void PoiTemplatesValidAndDistinct()
    {
        var config = EngineContentLoader.LoadSolarSystemGenerationConfig(DefaultSystemContentTests.Settings)!;
        Assert.Equal(new[] { "abandoned-platform", "abandoned-relay" }, config.PoiTemplates!.Select(p => p.Id).Order());
        Assert.Contains(config.PoiTemplates!, p => p.Name == "Заброшенный ретранслятор" && p.Description == "Неактивный узел связи. Исследование пока недоступно.");
        Assert.Contains(config.PoiTemplates!, p => p.Name == "Пустая платформа" && p.Description == "Покинутая техническая платформа. Здесь пока нет доступных действий.");
        foreach (string name in config.EnabledScenarios)
        {
            using var engine = SimulationEngine.CreateFromScenarioFile(DefaultSystemContentTests.Settings,
                Path.Combine(AppContext.BaseDirectory, "Scenarios", name, "scenario.json"));
            var snapshot = engine.CaptureSnapshot();
            Assert.Equal(2, snapshot.AiMap!.PointsOfInterest.Length);
            Assert.All(snapshot.AiMap.PointsOfInterest, p =>
            {
                Assert.Contains(config.PoiTemplates!, t => t.Name == p.Name && t.Description == p.Description);
                Assert.DoesNotContain(snapshot.Objects, o => o.ObjectId == p.ObjectId);
            });
        }
    }

    [Fact]
    public void PoiTemplatesDoNotDefineActions()
    {
        string folder = Path.Combine(Path.GetTempPath(), "dss-poi-" + Guid.NewGuid()); Directory.CreateDirectory(folder);
        try
        {
            string settings = Path.Combine(folder, "Settings.json"), content = Path.Combine(folder, "solar.json");
            File.WriteAllText(settings, """{"typeData":{"solarSystem":"solar.json"},"defaultScenario":"scenario.json"}""");
            var original = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data/Maps/solar-system.json")))!;
            foreach (string property in new[] { "reward", "cost", "production", "actions" })
            {
                var invalid = original.DeepClone(); invalid["poiTemplates"]![0]![property] = 1;
                File.WriteAllText(content, invalid.ToJsonString());
                Assert.Contains(property, Assert.Throws<ContentException>(() => EngineContentLoader.LoadSolarSystemGenerationConfig(settings)).Message);
            }
        }
        finally { Directory.Delete(folder, true); }
    }
}
