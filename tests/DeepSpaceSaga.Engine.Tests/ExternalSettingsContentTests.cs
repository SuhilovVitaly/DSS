using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class ExternalSettingsContentTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExternalConfigurationRetainsRealCatalogScenarioPortraitsAndSaveLoad(bool relative)
    {
        string repository = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(repository, "DeepSpaceSaga.sln")))
            repository = Directory.GetParent(repository)?.FullName ?? throw new DirectoryNotFoundException("Repository not found");
        string content = Path.Combine(repository, "src/DeepSpaceSaga.Client");
        string root = Environment.GetEnvironmentVariable("DSS_DIAGNOSTICS_DIRECTORY") ?? Path.GetFullPath(Path.Combine(repository, "../artifacts/validation/ep-0015-ubuntu-support/engine-content-tests"));
        string directory = Path.Combine(root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var settings = JsonNode.Parse(File.ReadAllText(Path.Combine(content, "Settings.json")))!;
            settings["contentRoot"] = relative ? Path.GetRelativePath(directory, content) : content;
            string external = Path.Combine(directory, "Settings.json");
            File.WriteAllText(external, settings.ToJsonString());
            Assert.Equal(EngineContentLoader.LoadCrewPortraits(Path.Combine(content, "Settings.json")).ToArray(), EngineContentLoader.LoadCrewPortraits(external).ToArray());
            Assert.NotEmpty(EngineContentLoader.LoadCrewPortraits(external));
            Assert.Equal(EngineContentLoader.LoadSolarSystemGenerationConfig(Path.Combine(content, "Settings.json"))?.SchemaVersion,
                EngineContentLoader.LoadSolarSystemGenerationConfig(external)?.SchemaVersion);
            using var engine = EngineContentLoader.CreateEngineFromSettingsFile(external);
            string save = Path.Combine(directory, "save.json");
            File.WriteAllText(save, ScenarioLoader.Serialize(engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0)));
            using var restored = EngineContentLoader.CreateEngineFromSaveFile(external, save);
            Assert.Equal(engine.PlayerShipObjectId, restored.PlayerShipObjectId);
            Assert.Equal(engine.CaptureSnapshotForTests(0).Objects.Length, restored.CaptureSnapshotForTests(0).Objects.Length);
            settings["contentRoot"] = " ";
            File.WriteAllText(external, settings.ToJsonString());
            Assert.Throws<ContentException>(() => EngineContentLoader.CreateEngineFromSettingsFile(external));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }
}
