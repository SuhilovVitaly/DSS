using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class DialoguePortraitGenerationTests
{
    [Fact]
    public void Discovered_portraits_generate_per_person_and_survive_catalog_changes_on_reload()
    {
        string root = Path.Combine(Path.GetTempPath(), "dss-dialogue-catalog-" + Guid.NewGuid().ToString("N"));
        string folder = Path.Combine(root, CharacterPortraits.FemaleFolder);
        Directory.CreateDirectory(Path.Combine(folder, "helpers"));
        try
        {
            File.WriteAllText(Path.Combine(folder, "Zeta.PNG"), "fixture");
            File.WriteAllText(Path.Combine(folder, "Alpha.png"), "fixture");
            File.WriteAllText(Path.Combine(folder, "ignored.jpg"), "fixture");
            File.WriteAllText(Path.Combine(folder, "helpers", "preview.png"), "fixture");
            string settings = Path.Combine(root, "Settings.json");
            var catalog = EngineContentLoader.LoadCrewPortraits(settings);
            Assert.Equal(new[] { CharacterPortraits.FemaleFolder + "/Alpha.png", CharacterPortraits.FemaleFolder + "/Zeta.PNG" }, catalog);
            var scenario = ScenarioLoader.LoadFromJson("""
                { "scenarioMetadata": { "scenarioId": "portraits", "name": "Portraits" },
                  "gameState": { "gameTimeMs": 0, "currentSpeed": "Speed0", "masterSeed": 1, "playerShipObjectId": "ship",
                    "spaceObjects": [
                      { "objectId": "ship", "objectType": "PlayerShip", "persistenceType": "Permanent", "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0, "movementType": "Stationary" },
                      { "objectId": "station", "objectType": "Station", "persistenceType": "Permanent", "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0, "movementType": "Stationary" }
                    ] } }
                """);
            scenario = scenario with { GameState = scenario.GameState with { SpaceObjects = scenario.GameState.SpaceObjects.Select(o =>
                o.ObjectId == "station" ? o with { StationCrew = Enumerable.Range(0, 32).Select(i =>
                    new StationCrewMemberData("crew-" + i, "Dock Operator")).ToArray() } : o).ToArray() } };
            using var first = new SimulationEngine(GameDataRegistry.Empty, catalog);
            first.LoadScenario(scenario);
            var saved = first.CaptureSaveState();
            var crew = saved.GameState.SpaceObjects.Single(o => o.ObjectId == "station").StationCrew!;
            Assert.All(crew, c => Assert.Contains(c.PortraitImage, catalog));
            Assert.Equal(2, crew.Select(c => c.PortraitImage).Distinct().Count());
            Assert.Equal(CharacterPortraits.DefaultMale, saved.GameState.SpaceObjects.Single(o => o.ObjectId == "ship").CaptainPortraitImage);

            using var same = new SimulationEngine(GameDataRegistry.Empty, catalog);
            same.LoadScenario(scenario);
            Assert.Equal(crew.Select(c => c.PortraitImage), same.CaptureSaveState().GameState.SpaceObjects.Single(o => o.ObjectId == "station").StationCrew!.Select(c => c.PortraitImage));
            File.WriteAllText(Path.Combine(folder, "New.png"), "fixture");
            var enlarged = EngineContentLoader.LoadCrewPortraits(settings);
            Assert.Equal(3, enlarged.Length);
            using var restored = new SimulationEngine(GameDataRegistry.Empty, enlarged);
            restored.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(saved), allowNonZeroGameTime: true));
            Assert.Equal(crew.Select(c => c.PortraitImage), restored.CaptureSaveState().GameState.SpaceObjects.Single(o => o.ObjectId == "station").StationCrew!.Select(c => c.PortraitImage));
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
