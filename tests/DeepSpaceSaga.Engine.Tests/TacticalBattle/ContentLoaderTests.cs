using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.TacticalBattle.Content;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

public sealed class ContentLoaderTests
{
    [Fact]
    public void Evasion_and_consciousness_rules_load_from_shipped_data()
    {
        var rules = TacticalBattleContentLoader.Load(Path.Combine(ClientRoot, "Data/TacticalBattle")).Rules;
        Assert.Equal(new ConsciousnessRules(100, 100, 5), rules.Consciousness);
        Assert.Equal(new EvasionRules(20, 10, 10, 0.5), rules.Evasion);
    }

    [Theory]
    [InlineData("/consciousness", "<missing>")]
    [InlineData("/evasion", "null")]
    [InlineData("/consciousness/start", "-1")]
    [InlineData("/consciousness/max", "99")]
    [InlineData("/consciousness/recoveryPerTurn", "-1")]
    [InlineData("/evasion/hitPenalty", "-1")]
    [InlineData("/evasion/cost", "-1")]
    [InlineData("/evasion/minConsciousness", "-1")]
    [InlineData("/evasion/minConsciousness", "101")]
    [InlineData("/evasion/aiHullFraction", "-0.1")]
    [InlineData("/evasion/aiHullFraction", "1.1")]
    [InlineData("/consciousness/start", "1.5")]
    public void Invalid_evasion_or_consciousness_values_are_rejected(string pointer, string value)
    {
        using var fixture = new Fixture();
        fixture.Change("rules.json", pointer, value);
        var error = Assert.Throws<TacticalBattleContentException>(() => TacticalBattleContentLoader.Load(fixture.Battle));
        Assert.Equal("rules.json", error.Issue.File);
        Assert.True(error.Issue.Field.StartsWith("$.consciousness", StringComparison.Ordinal)
            || error.Issue.Field.StartsWith("$.evasion", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("0", 0, 0, false)]
    [InlineData("100", 100, 100, false)]
    [InlineData("[0,100]", 0, 100, true)]
    [InlineData("[50,50]", 50, 50, true)]
    public void InitiativeSchemaAcceptsFixedAndInclusiveRange(string json, int min, int max, bool range)
    {
        using var fixture = new Fixture();
        fixture.Change("pilots.json", "/vera/initiative", json);
        var catalog = TacticalBattleContentLoader.Load(fixture.Battle);
        Assert.Equal(new PilotSkill(min, max, range), catalog.Pilots["vera"].Initiative);
        Assert.Equal(PilotSkill.Fixed(55), catalog.Pilots["vera"].Accuracy);
        Assert.Equal(PilotSkill.Fixed(52), catalog.Pilots["vera"].Maneuver);
    }

    [Theory]
    [InlineData("<missing>")]
    [InlineData("null")]
    [InlineData("1.5")]
    [InlineData("\"50\"")]
    [InlineData("true")]
    [InlineData("[55,40]")]
    [InlineData("[-1,50]")]
    [InlineData("101")]
    [InlineData("[0,101]")]
    [InlineData("[40,50,55]")]
    [InlineData("[40.0,55]")]
    public void InitiativeSchemaRejectsMissingMalformedAndOutOfBounds(string json)
    {
        using var fixture = new Fixture();
        fixture.Change("pilots.json", "/vera/initiative", json);
        var exception = Assert.Throws<TacticalBattleContentException>(() => TacticalBattleContentLoader.Load(fixture.Battle));
        Assert.Equal("pilots.json", exception.Issue.File);
        Assert.Equal("$.vera.initiative", exception.Issue.Field);
        using var engine = EngineContentLoader.CreateEngineFromSettingsFile(fixture.Settings);
        Assert.False(engine.GetTacticalBattleOptions().Availability.Available);
        Assert.NotEmpty(engine.CaptureSnapshot().Objects);
    }

    [Fact]
    public void ProductionCatalogPreservesOrderTypesAndCapturedDefinitions()
    {
        var c = TacticalBattleContentLoader.Load(Path.Combine(ClientRoot, "Data/TacticalBattle"));
        Assert.Equal(4, c.Frames.Count);
        Assert.Equal(7, c.Weapons.Count);
        Assert.Equal(6, c.Engines.Count);
        Assert.Equal(1, c.Shields.Count);
        Assert.Equal(6, c.Armor.Count);
        Assert.Equal(10, c.Pilots.Count);
        Assert.Equal(8, c.Layouts.Count);
        Assert.Equal("initial", c.Scenarios.Entries[0].Id);
        Assert.Equal(new[] { "carrier", "heavy", "light", "thug" }, c.Frames.Entries.Select(e => e.Id));
        Assert.Equal(new[] { "raid", "defence", "dogfight", "balanced", "commander" }, c.Ai.Tactics.Entries.Select(e => e.Id));
        Assert.Equal(new[] { "Вера", "Юн", "Марта", "Олег" }, c.Scenarios["initial"].Sides[0].Drones.Select(d => d.Id));
        Assert.Equal(2, c.Layouts["heavy.sword.shield"].Engines.Length);
        Assert.Equal(PilotSkill.Range(40, 55), c.Pilots["thug1"].Accuracy);
        Assert.Equal(PilotSkill.Fixed(55), c.Pilots["vera"].Accuracy);
        Assert.Equal("weaponJammed", c.Rules.Systems.Table[0].Effect);
        Assert.Equal(400, c.Frames["carrier"].Hull);
        Assert.Null(c.Weapons["gun.spear.t1"].RangeBonus);
    }

    [Theory]
    [InlineData("rules.json", "/version", "\"5.0\"")]
    [InlineData("rules.json", "/version", "<missing>")]
    [InlineData("rules.json", "/substeps", "0")]
    [InlineData("rules.json", "/substeps", "4.0")]
    [InlineData("rules.json", "/hit/base", "true")]
    [InlineData("rules.json", "/hit/minChance", "99")]
    [InlineData("rules.json", "/hit/maxChance", "101")]
    [InlineData("rules.json", "/deployScatter/xy", "-1")]
    [InlineData("rules.json", "/disabled/hullFraction", "2")]
    [InlineData("rules.json", "/systems/minHullLoss", "-1")]
    [InlineData("rules.json", "/systems/table", "[]")]
    [InlineData("rules.json", "/turnAtMaxSpeedFraction", "1e400")]
    [InlineData("Frames/carrier.json", "/maneuver", "35.0")]
    [InlineData("Frames/carrier.json", "/maneuver", "<missing>")]
    [InlineData("Frames/heavy.json", "/id", "\"Heavy\"")]
    [InlineData("Frames/heavy.json", "/hull", "0")]
    [InlineData("Frames/heavy.json", "/frameMassKg", "0")]
    [InlineData("Frames/heavy.json", "/referenceMassKg", "<missing>")]
    [InlineData("Frames/heavy.json", "/maxSpeedCap", "10")]
    [InlineData("Frames/heavy.json", "/engineSlots", "0")]
    [InlineData("Modules/Weapons/gun.spear.t1.json", "/damageMin", "-1")]
    [InlineData("Modules/Weapons/gun.spear.t1.json", "/damageMax", "20")]
    [InlineData("Modules/Weapons/gun.spear.t1.json", "/arcDeg", "0")]
    [InlineData("Modules/Weapons/gun.spear.t1.json", "/range", "0")]
    [InlineData("Modules/Weapons/gun.spear.t1.json", "/piercing", "-1")]
    [InlineData("Modules/Weapons/gun.spear.t1.json", "/rearKillChance", "2")]
    [InlineData("Modules/Engines/drive.light.json", "/speedBonus", "-1")]
    [InlineData("Modules/Shields/shield.t1.json", "/massKg", "-1")]
    [InlineData("Modules/Shields/shield.t1.json", "/defense", "-1")]
    [InlineData("Modules/Armor/plate.medium.json", "/armor", "-1")]
    [InlineData("pilots.json", "/vera/accuracy", "true")]
    [InlineData("pilots.json", "/", "{\"name\":\"Empty ID\",\"accuracy\":50,\"maneuver\":50}")]
    [InlineData("pilots.json", "/thug1/accuracy", "[55,40]")]
    [InlineData("pilots.json", "/thug1/accuracy", "[40.0,55]")]
    [InlineData("layouts.json", "/heavy.sword.shield/class", "\"unknown\"")]
    [InlineData("layouts.json", "/heavy.sword.shield/weapon", "\"gun.spear.T1\"")]
    [InlineData("layouts.json", "/heavy.sword.shield/engines", "[]")]
    [InlineData("layouts.json", "/heavy.sword.shield/engines", "[\"drive.standard\",null]")]
    [InlineData("layouts.json", "/heavy.sword.shield/shieldCount", "3")]
    [InlineData("layouts.json", "/heavy.sword.shield/shield", "null")]
    [InlineData("layouts.json", "/carrier.standard/weapon", "\"gun.sword.t1\"")]
    [InlineData("ai.json", "/pursuit/slowForTurnSteps", "0")]
    [InlineData("ai.json", "/tail/turnSteps", "0")]
    [InlineData("ai.json", "/tactics/raid/striker", "1.5")]
    [InlineData("ai.json", "/tactics/raid/fighter", "<missing>")]
    [InlineData("ai.json", "/commander/startTactic", "\"commander\"")]
    [InlineData("ai.json", "/roles/fighter", "<missing>")]
    [InlineData("scenarios.json", "/initial/sides/0/id", "\"B\"")]
    [InlineData("scenarios.json", "/initial/sides/0/drones", "[]")]
    [InlineData("scenarios.json", "/initial/sides/1/drones/0/id", "\"Вера\"")]
    [InlineData("scenarios.json", "/initial/sides/0/drones/0/pilot", "\"unknown\"")]
    [InlineData("scenarios.json", "/initial/sides/0/drones/0/layout", "\"carrier.standard\"")]
    [InlineData("scenarios.json", "/initial/sides/0/carrier/layout", "\"heavy.sword.shield\"")]
    [InlineData("scenarios.json", "/initial/sides/0/drones/0/course", "<missing>")]
    [InlineData("scenarios.json", "/initial/goal/seeds", "[0,1000]")]
    [InlineData("scenarios.json", "/initial/goal/seeds", "[10,1]")]
    [InlineData("scenarios.json", "/initial/goal/playerSide", "\"C\"")]
    public void CorruptContentReportsStructuredFileAndField(string file, string pointer, string value)
    {
        using var fixture = new Fixture();
        fixture.Change(file, pointer, value);
        var exception = Assert.Throws<TacticalBattleContentException>(() => TacticalBattleContentLoader.Load(fixture.Battle));
        Assert.IsAssignableFrom<ContentException>(exception);
        Assert.Equal(file, exception.Issue.File);
        Assert.False(string.IsNullOrWhiteSpace(exception.Issue.Field));
        Assert.False(string.IsNullOrWhiteSpace(exception.Issue.Message));
    }

    [Fact]
    public void DuplicateKeysAndMissingCarrierAreRejected()
    {
        using var fixture = new Fixture();
        string path = Path.Combine(fixture.Battle, "rules.json");
        string original = File.ReadAllText(path);
        File.WriteAllText(path, original.Replace("\"substeps\": 4", "\"substeps\": 4, \"substeps\": 4"));
        Assert.Equal("$.substeps", Assert.Throws<TacticalBattleContentException>(() => TacticalBattleContentLoader.Load(fixture.Battle)).Issue.Field);
        File.WriteAllText(path, original);
        File.Delete(Path.Combine(fixture.Battle, "Frames/carrier.json"));
        Assert.Equal("$.carrier", Assert.Throws<TacticalBattleContentException>(() => TacticalBattleContentLoader.Load(fixture.Battle)).Issue.Field);
    }

    [Theory]
    [InlineData("<missing>")]
    [InlineData("null")]
    [InlineData("true")]
    [InlineData("123")]
    [InlineData("\"missing-folder\"")]
    public void InvalidBattlePathDoesNotPreventWorldBootstrap(string value)
    {
        using var fixture = new Fixture();
        fixture.ChangeSettingsPath(value);
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(fixture.Settings, out _, out _);
        Assert.False(registry.TacticalBattle.IsAvailable);
        Assert.Equal("typeData.tacticalBattle", registry.TacticalBattle.Error!.Field);
        using var engine = EngineContentLoader.CreateEngineFromSettingsFile(fixture.Settings);
        Assert.NotEmpty(engine.CaptureSnapshot().Objects);
    }

    [Fact]
    public void UnknownDamageEffectReportsExactFieldAndKeepsWorldAvailable()
    {
        using var fixture = new Fixture();
        fixture.Change("rules.json", "/systems/table/0/effect", "\"weaponJamed\"");
        using var engine = EngineContentLoader.CreateEngineFromSettingsFile(fixture.Settings);
        var availability = engine.GetTacticalBattleOptions().Availability;
        Assert.False(availability.Available);
        Assert.Equal("rules.json", availability.Issue!.File);
        Assert.Equal("$.systems.table[0].effect", availability.Issue.Field);
        Assert.NotEmpty(engine.CaptureSnapshot().Objects);
    }

    [Fact]
    public void CorruptBattleDataDoesNotPreventNewScenarioOrFileSaveLoad()
    {
        using var fixture = new Fixture();
        fixture.Change("rules.json", "/version", "\"wrong\"");
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(fixture.Settings, out _, out _);
        Assert.False(registry.TacticalBattle.IsAvailable);
        Assert.Equal("rules.json", registry.TacticalBattle.Error!.File);
        using var engine = EngineContentLoader.CreateEngineFromSettingsFile(fixture.Settings);
        using var scenario = EngineContentLoader.CreateEngineFromScenarioFile(fixture.Settings, Path.Combine(ClientRoot, "Scenarios/PlayerShipOnly/scenario.json"));
        string save = Path.Combine(fixture.Root, "world.json");
        File.WriteAllText(save, ScenarioLoader.Serialize(engine.CaptureSaveState()));
        using var loaded = EngineContentLoader.CreateEngineFromSaveFile(fixture.Settings, save);
        Assert.NotEmpty(loaded.CaptureSnapshot().Objects);
        Assert.NotEmpty(scenario.CaptureSnapshot().Objects);
    }

    [Fact]
    public void RegistryCapturesContentAndModuleOverlayPreservesIt()
    {
        using var fixture = new Fixture();
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(fixture.Settings, out _, out _);
        Assert.True(registry.TacticalBattle.IsAvailable);
        fixture.Change("rules.json", "/version", "\"corrupted-after-load\"");
        Assert.Equal("4.0", registry.TacticalBattle.Catalog!.Rules.Version);
        var overlay = registry.WithModuleTypes(Enumerable.Range(0, registry.ModuleTypes.Count).Select(registry.ModuleTypes.GetDefinition).ToImmutableArray());
        Assert.Same(registry.TacticalBattle.Catalog, overlay.TacticalBattle.Catalog);
        Assert.False(EngineContentLoader.LoadRegistryFromSettingsFile(fixture.Settings, out _, out _).TacticalBattle.IsAvailable);
    }

    private static string ClientRoot
    {
        get
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
                if (File.Exists(Path.Combine(dir.FullName, "DeepSpaceSaga.sln"))) return Path.Combine(dir.FullName, "src/DeepSpaceSaga.Client");
            throw new DirectoryNotFoundException("DSS repository not found.");
        }
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string _validation = Path.GetFullPath(Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT")
            ?? (OperatingSystem.IsWindows() ? "D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle"
                : Path.Combine(Path.GetTempPath(), "dss-artifacts/validation/ep-0015-tactical-carrier-battle")));
        public string Root { get; }
        public string Battle => Path.Combine(Root, "battle");
        public string Settings => Path.Combine(Root, "Settings.json");
        public Fixture()
        {
            Root = Path.Combine(_validation, "loader-fixture-" + Guid.NewGuid().ToString("N"));
            string source = Path.Combine(ClientRoot, "Data/TacticalBattle");
            foreach (string file in Directory.GetFiles(source, "*.json", SearchOption.AllDirectories))
            {
                string destination = Path.Combine(Battle, Path.GetRelativePath(source, file));
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(file, destination);
            }
            var settings = JsonNode.Parse(File.ReadAllText(Path.Combine(ClientRoot, "Settings.json")))!;
            foreach (var pair in settings["typeData"]!.AsObject().ToArray())
                if (pair.Value is JsonValue value && value.TryGetValue<string>(out var path)) settings["typeData"]![pair.Key] = Path.GetFullPath(Path.Combine(ClientRoot, path));
            settings["typeData"]!["tacticalBattle"] = Battle;
            settings["defaultScenario"] = Path.Combine(ClientRoot, "Scenarios/PlayerShipOnly/scenario.json");
            File.WriteAllText(Settings, settings.ToJsonString());
        }
        public void Change(string file, string pointer, string value)
        {
            string path = Path.Combine(Battle, file);
            var json = JsonNode.Parse(File.ReadAllText(path))!;
            string[] parts = pointer.Split('/').Skip(1).ToArray();
            var parent = json;
            foreach (string part in parts.SkipLast(1)) parent = parent is JsonArray array ? array[int.Parse(part)]! : parent[part]!;
            if (value == "<missing>") parent.AsObject().Remove(parts[^1]);
            else if (parent is JsonArray array) array[int.Parse(parts[^1])] = JsonNode.Parse(value);
            else parent[parts[^1]] = JsonNode.Parse(value);
            File.WriteAllText(path, json.ToJsonString());
        }
        public void ChangeSettingsPath(string value)
        {
            var settings = JsonNode.Parse(File.ReadAllText(Settings))!;
            if (value == "<missing>") settings["typeData"]!.AsObject().Remove("tacticalBattle");
            else settings["typeData"]!["tacticalBattle"] = JsonNode.Parse(value);
            File.WriteAllText(Settings, settings.ToJsonString());
        }
        public void Dispose()
        {
            if (!Path.GetFullPath(Root).StartsWith(_validation + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                throw new InvalidOperationException("Fixture cleanup escaped validation root.");
            Directory.Delete(Root, true);
        }
    }
}
