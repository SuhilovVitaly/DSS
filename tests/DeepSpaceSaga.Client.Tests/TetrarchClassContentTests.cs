using System.Text.Json.Nodes;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Client.Tests;

public sealed class TetrarchClassContentTests
{
    private const string ClassId = "ship.tetrarch";
    private const string ClassPath = "Data/Ships/ship-classes.json";
    private const string LauncherId = "module.torpedo.launcher.basic";
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    private static ShipClassDefinition Tetrarch(GameDataRegistry registry) =>
        registry.ShipClasses.GetDefinition(registry.ShipClasses.GetIndex(ClassId));
    private static ModuleTypeDefinition Launcher(GameDataRegistry registry) =>
        registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex(LauncherId));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Ship_classes_have_no_hull_hp(bool output)
    {
        string root = output ? AppContext.BaseDirectory : ClientRoot;
        string settingsPath = Path.Combine(root, "Settings.json");
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(settingsPath, out _, out var settings);
        Assert.Equal(ClassPath, settings.TypeData.ShipClasses);
        Assert.Equal(3, registry.ShipClasses.Count);
        Assert.Equal(ClassId, Tetrarch(registry).TypeId);
        Assert.True(registry.ShipClasses.Contains("ship.mukha"));
        Assert.True(registry.ShipClasses.Contains("ship.shmel"));
        Assert.False(registry.ShipClasses.Contains("Tetrarch"));
        Assert.False(registry.ShipClasses.Contains("SHIP.TETRARCH"));

        string classFile = Path.Combine(root, ClassPath);
        Assert.True(File.Exists(classFile), $"Missing class content: {classFile}");
        var json = JsonNode.Parse(File.ReadAllText(classFile))!;
        var classes = json["shipClasses"]!.AsArray();
        Assert.Equal(new[] { ClassId, "ship.mukha", "ship.shmel" }, classes.Select(c => c!["typeId"]!.GetValue<string>()));
        Assert.All(classes, definition => Assert.Equal(new[] { "typeId" }, definition!.AsObject().Select(p => p.Key)));
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(File.ReadAllText(Path.Combine(ClientRoot, ClassPath))), json));

        using var engine = EngineContentLoader.CreateEngineFromSettingsFile(settingsPath);
        Assert.NotNull(engine.PlayerShipObjectId);
    }

    [Fact]
    public void Mukha_and_shmel_load()
    {
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(
            Path.Combine(ClientRoot, "Settings.json"), out _, out _);
        Assert.Equal("ship.mukha", registry.ShipClasses.GetDefinition(registry.ShipClasses.GetIndex("ship.mukha")).TypeId);
        Assert.Equal("ship.shmel", registry.ShipClasses.GetDefinition(registry.ShipClasses.GetIndex("ship.shmel")).TypeId);
    }

    [Fact]
    public void Restart_loads_edited_class_content_without_compilation()
    {
        using var temporary = new TemporaryContent();
        string sourceJson = File.ReadAllText(Path.Combine(ClientRoot, ClassPath));
        var original = EngineContentLoader.LoadFromSettingsFile(temporary.SettingsPath);
        using var firstEngine = EngineContentLoader.CreateEngineFromSettingsFile(temporary.SettingsPath);
        Assert.Equal(3, original.Registry.ShipClasses.Count);

        string classFile = Path.GetFullPath(Path.Combine(temporary.Root, ClassPath));
        var edited = JsonNode.Parse(File.ReadAllText(classFile))!;
        edited["shipClasses"]!.AsArray().Add(new JsonObject { ["typeId"] = "ship.custom" });
        File.WriteAllText(classFile, edited.ToJsonString());

        // Verify content through the same bootstrap loader, then start a fresh engine.
        // Runtime combat and hostile loadouts are migrated by later tickets.
        var restarted = EngineContentLoader.LoadFromSettingsFile(temporary.SettingsPath);
        using var secondEngine = EngineContentLoader.CreateEngineFromSettingsFile(temporary.SettingsPath);
        Assert.True(restarted.Registry.ShipClasses.Contains("ship.custom"));
        Assert.NotNull(secondEngine.PlayerShipObjectId);
        Assert.False(original.Registry.ShipClasses.Contains("ship.custom"));
        Assert.Equal(original.Registry.CatalogCompatibility, restarted.Registry.CatalogCompatibility);
        Assert.Equal(150, Launcher(restarted.Registry).TorpedoDamage);
        Assert.Equal(10, Launcher(restarted.Registry).TorpedoSpeedKmS);
        Assert.Equal(90, Launcher(restarted.Registry).TorpedoTurnRateDegPerSec);
        Assert.Equal(sourceJson, File.ReadAllText(Path.Combine(ClientRoot, ClassPath)));

        // Prove the engine factory rereads the declared file on each restart rather than
        // reusing a cached registry; already-created engines remain usable after an edit.
        edited["shipClasses"]!.AsArray().Add(new JsonObject { ["typeId"] = "ship.custom" });
        File.WriteAllText(classFile, edited.ToJsonString());
        var error = Assert.Throws<ContentException>(() => EngineContentLoader.CreateEngineFromSettingsFile(temporary.SettingsPath));
        Assert.Contains(classFile, error.Message, StringComparison.Ordinal);
        Assert.Contains("duplicate", error.Message, StringComparison.Ordinal);
        Assert.NotEmpty(firstEngine.CaptureSnapshot().Objects);
        Assert.NotEmpty(secondEngine.CaptureSnapshot().Objects);
    }

    [Fact]
    public void Omitting_class_content_keeps_legacy_settings_loadable()
    {
        using var temporary = new TemporaryContent();
        var settings = JsonNode.Parse(File.ReadAllText(temporary.SettingsPath))!;
        settings["typeData"]!.AsObject().Remove("shipClasses");
        File.WriteAllText(temporary.SettingsPath, settings.ToJsonString());
        File.Delete(Path.Combine(temporary.Root, ClassPath));
        // A legacy scenario predates explicit combat identity as well as the class registry.
        string scenarioPath = Path.Combine(temporary.Root, settings["defaultScenario"]!.GetValue<string>());
        var scenario = JsonNode.Parse(File.ReadAllText(scenarioPath))!;
        foreach (var obj in scenario["gameState"]!["spaceObjects"]!.AsArray())
        {
            obj!.AsObject().Remove("shipClassId");
            obj.AsObject().Remove("hullHitPoints");
        }
        File.WriteAllText(scenarioPath, scenario.ToJsonString());

        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(temporary.SettingsPath, out _, out _);
        Assert.Equal(0, registry.ShipClasses.Count);
        using var engine = EngineContentLoader.CreateEngineFromSettingsFile(temporary.SettingsPath);
        Assert.NotNull(engine.PlayerShipObjectId);
    }

    private sealed class TemporaryContent : IDisposable
    {
        public string Root { get; } = Directory.CreateTempSubdirectory("dss-tetrarch-content-").FullName;
        public string SettingsPath => Path.Combine(Root, "Settings.json");

        public TemporaryContent()
        {
            File.Copy(Path.Combine(ClientRoot, "Settings.json"), SettingsPath);
            foreach (string folder in new[] { "Data", "Scenarios" })
            {
                foreach (string file in Directory.EnumerateFiles(Path.Combine(ClientRoot, folder), "*.json", SearchOption.AllDirectories))
                {
                    string destination = Path.Combine(Root, Path.GetRelativePath(ClientRoot, file));
                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    File.Copy(file, destination);
                }
            }
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
