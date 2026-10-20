using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0002TK0005Tests
{
    private static readonly string Source = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Production_rules_match_decisions_and_obsolete_content_is_absent(bool deployed)
    {
        string root = deployed ? AppContext.BaseDirectory : Source;
        var settings = JsonNode.Parse(File.ReadAllText(Path.Combine(root, "Settings.json")))!;
        Assert.False(settings["typeData"]!.AsObject().ContainsKey("fireModes"));
        Assert.False(File.Exists(Path.Combine(root, "Data/Combat/fire-modes.json")));
        Assert.Empty(Directory.Exists(Path.Combine(root, "Data/CombatV3")) ? Directory.GetFiles(Path.Combine(root, "Data/CombatV3"), "*.json", SearchOption.AllDirectories) : []);
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(root, "Settings.json"), out _, out _);
        var rules = Assert.IsType<CombatRulesDefinition>(registry.CombatRules);
        Assert.True(rules.IsPointAim); Assert.Null(rules.Chance); Assert.Empty(registry.FireModes);
        Assert.Equal(.3, rules.Blast!.CoreFraction);
        Assert.Equal(8000, rules.Damage.HitDisableMs); Assert.Equal("mass", rules.Damage.HitLocationWeighting);
        Assert.Equal((15, .0333, .3), (rules.Scatter!.SkillPivot, rules.Scatter.SkillSlope, rules.Scatter.MinMultiplier));
        Assert.Equal((1400L, 30L, 200L), (rules.Npc!.ReactionBaseMs, rules.Npc.ReactionPerSkillMs, rules.Npc.ReactionMinMs));
        Assert.Equal(100, rules.CombatStartRangeKm);
        Assert.Equal(.3333m, rules.ShipDestruction.DestroyedModuleFraction);
        Assert.Equal(new[] { "bridge", "engine" }, rules.ShipDestruction.CriticalRoles);
        Assert.Equal(0, rules.ArmorClasses["none"]); Assert.Equal(60, rules.ArmorClasses["light"]);
        foreach (string id in new[] { "torpedo.fire", "torpedo.selfDestruct", "torpedo.setMode", "defense.fire", "defense.enable", "defense.disable" })
            Assert.False(registry.CommandDefinitions.Contains(id));
        foreach (string id in new[] { CombatCommandTypes.Launch, CombatCommandTypes.Detonate, DefenseCommandTypes.Launch })
            Assert.True(registry.CommandDefinitions.Contains(id));
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(File.ReadAllText(Path.Combine(Source, "Data/Combat/combat-rules.json"))),
            JsonNode.Parse(File.ReadAllText(Path.Combine(root, "Data/Combat/combat-rules.json")))));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Point_rules_activate_through_real_session_launches_and_file_roundtrip(bool deployed)
    {
        string root = deployed ? AppContext.BaseDirectory : Source;
        string settings = Path.Combine(root, "Settings.json");
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(settings, out _, out _);
        var scenario = ScenarioLoader.LoadFromFile(Path.Combine(root, "Scenarios/PlayerShipOnly/scenario.json"));
        // Stable geometry isolates catalog/session integration; no weapon/rule overrides.
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                CurrentSpeed = "Speed0",
                MasterSeed = 42,
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o with
                {
                    PositionX = o.ObjectId == PointCombatSessionFixture.Player ? 0 : 900,
                    PositionY = 0,
                    SpeedMps = 0,
                    MovementType = "Stationary",
                    Ai = null,
                    Modules = o.Modules!.Select(m => m with { AutoDefenseEnabled = false }).ToArray()
                }).ToArray()
            }
        };
        using var engine = new SimulationEngine(registry, clock: new SimulationClock(SimulationSpeed.Speed0, () => 0));
        engine.LoadScenario(scenario);
        string directory = Path.Combine(Path.GetTempPath(), "dss-catalog-v3-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            await using var connection = new LocalGameSessionConnection(engine, directory);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await using var reader = connection.ReadCombatUpdatesAsync(timeout.Token).GetAsyncEnumerator();
            async Task<AuthoritativeSnapshot> Until(Func<AuthoritativeSnapshot, bool> predicate)
            {
                while (await reader.MoveNextAsync())
                {
                    var update = reader.Current;
                    await connection.ReportPresentationProgressAsync(update.SessionId, update.Sequence, false, timeout.Token);
                    if (update.FullSnapshot is { } full && predicate(full)) return full;
                    foreach (var frame in update.Frames) if (predicate(frame.State)) return frame.State;
                }
                throw new InvalidOperationException("Session ended before catalog launch result.");
            }
            await Until(_ => true);
            var launch = PointCombatSessionFixture.Launch() with { TargetWorldX = 600 };
            var counter = new PlayerCommand("catalog-counter", 2, PointCombatSessionFixture.Player, "MOD-PLAYER-COUNTERMEASURE-01",
                DefenseCommandTypes.Launch, TargetWorldX: 250, TargetWorldY: 0, ProfileId: "wide");
            Assert.True((await connection.SubmitCommandAsync(launch, timeout.Token)).Accepted);
            await Until(s => s.CommandResults.Any(r => r.CommandId == launch.CommandId && r.Status == CommandResultStatus.Executed));
            Assert.True((await connection.SubmitCommandAsync(counter, timeout.Token)).Accepted);
            var snapshot = await Until(s => s.CommandResults.Any(r => r.CommandId == counter.CommandId && r.Status == CommandResultStatus.Executed));
            Assert.Equal(2, snapshot.Objects.Count(o => o.OwnProjectile is not null));
            var torpedo = Assert.Single(snapshot.Objects, o => o.OwnProjectile?.Kind == ProjectileKind.Torpedo).OwnProjectile!;
            Assert.Equal((16d, 2.5, 200, 20m), (torpedo.SpeedKmS, torpedo.BlastRadiusKm, torpedo.Damage, torpedo.ArmorPiercingPercent));
            Assert.Equal(8000, registry.CombatRules!.Damage.HitDisableMs);
            await connection.SaveAsync("catalog", timeout.Token);
            string path = Path.Combine(directory, "catalog.json");
            var saved = ScenarioLoader.LoadFromFile(path, true, true);
            Assert.Equal(18, saved.SaveFormatVersion); Assert.Equal(2, saved.GameState.PointCombatState!.Projectiles.Count);
            using var restored = SimulationEngine.CreateFromSaveFile(settings, path);
            var a = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0, 0);
            var b = restored.CaptureSaveStateForTests(0, SimulationSpeed.Speed0, 0);
            Assert.Equal(ScenarioLoader.Serialize(a), ScenarioLoader.Serialize(b));
        }
        finally { Directory.Delete(directory, true); }
    }
}
