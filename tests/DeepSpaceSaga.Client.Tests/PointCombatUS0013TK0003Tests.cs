using System.Collections.Immutable;
using System.Diagnostics;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0013TK0003Tests
{
    private static AuthoritativeSnapshot Scene(EnemyShipStatusSnapshot? status = null, long motion = 0) => new(1, motion, SimulationSpeed.Speed0,
        [new("player", 0, 0, 0, 0, RenderObjectType: SpaceObjectType.PlayerShip),
            new("enemy", 240, 80, 0, 0, RenderObjectType: SpaceObjectType.NpcShip, RelationToPlayer: PlayerRelation.Enemy, EnemyStatus: status)],
        "player", CombatJournal: [], Encounter: new(CombatEncounterState.Active, 0, null, new()));

    private sealed class View : IDisposable
    {
        internal long NowMs;
        internal readonly SnapshotBuffer Buffer;
        internal readonly GameSessionScreen Screen;
        internal readonly SKBitmap Bitmap = new(1920, 1080);
        private readonly SKCanvas _canvas;
        internal View(AuthoritativeSnapshot snapshot, float scale = 1)
        {
            Buffer = new(() => NowMs * Stopwatch.Frequency / 1000); Buffer.Update(snapshot);
            Screen = new(Buffer, new LinearMotionPredictor(), timestampProvider: () => NowMs * Stopwatch.Frequency / 1000, uiScale: scale);
            _canvas = new(Bitmap); Render();
        }
        internal void Render() => Screen.Render(_canvas, 1920, 1080);
        public void Dispose() { Screen.OnDeactivated(); _canvas.Dispose(); Bitmap.Dispose(); }
    }

    [Fact]
    public void Enemy_badges_use_aggregate_status_and_no_damage_number()
    {
        using var v = new View(Scene(new(true, 8000, false)));
        string text = v.Screen.EnemyStatusBadges["enemy"].Text;
        Assert.Contains(GameSessionScreen.CombatText("EnemyDamaged", "Повреждён"), text);
        Assert.Contains("32.0", text);
        v.NowMs = 1000; v.Render(); Assert.Equal(text, v.Screen.EnemyStatusBadges["enemy"].Text); // paused physical clock
        var snapshot = Scene(new(true, null, true)) with { SnapshotSequence = 2 };
        v.Buffer.Update(snapshot); v.Render();
        Assert.Equal(GameSessionScreen.CombatText("EnemyDestroyed", "Уничтожен"), v.Screen.EnemyStatusBadges["enemy"].Text);
        Assert.DoesNotContain("32.0", v.Screen.EnemyStatusBadges["enemy"].Text);
    }

    [Theory]
    [InlineData(SpaceObjectType.UnknownSpaceObject, PlayerRelation.Enemy)]
    [InlineData(SpaceObjectType.NpcShip, PlayerRelation.Neutral)]
    [InlineData(SpaceObjectType.PlayerShip, PlayerRelation.Enemy)]
    public void Badges_ignore_unknown_or_non_enemy_module_payloads(string type, string relation)
    {
        var source = Scene(new(true, 8000, false));
        var enemy = source.Objects[1] with { RenderObjectType = type, RelationToPlayer = relation, ModuleHealth = [new("private", "Private", 9999, 10000, 9999, 10000, 8000, false)] };
        using var v = new View(source with { Objects = [source.Objects[0], enemy] });
        Assert.Empty(v.Screen.EnemyStatusBadges);
    }

    [Theory]
    [InlineData(ProjectileHitResult.Core, "Hit.Core", "Ядро")]
    [InlineData(ProjectileHitResult.Edge, "Hit.Edge", "Край")]
    [InlineData(ProjectileHitResult.Miss, "Hit.Miss", "Мимо")]
    [InlineData(ProjectileHitResult.ShotDown, "Hit.ShotDown", "Сбита")]
    public void Actual_result_labels_use_public_result_without_enemy_damage(ProjectileHitResult result, string key, string fallback)
    {
        using var v = new View(Scene());
        var fact = new CombatJournalEntry(1, 0, CombatEventType.Hit, "player", "enemy", "torpedo", 240, 80, Damage: 7777, HitResult: result);
        Assert.Empty(v.Screen.PointHitLabels);
        v.Buffer.Update(Scene() with { SnapshotSequence = 2, CombatJournal = [fact] }); v.Render();
        Assert.Equal(GameSessionScreen.CombatText(key, fallback), v.Screen.PointHitLabels[1].Text);
        Assert.DoesNotContain("7777", v.Screen.PointHitLabels[1].Text);
        v.Render(); Assert.Single(v.Screen.PointHitLabels);
        v.NowMs = 2000; v.Render(); Assert.Empty(v.Screen.PointHitLabels);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    public void Initial_history_is_not_replayed_and_feedback_stays_inside_map(float scale)
    {
        var old = new CombatJournalEntry(1, 0, CombatEventType.Hit, "player", "enemy", "old", 240, 80, HitResult: ProjectileHitResult.Core);
        using var v = new View(Scene(new(true, 8000, false)) with { CombatJournal = [old] }, scale);
        Assert.Empty(v.Screen.PointHitLabels);
        var fresh = old with { EventId = 2, ProjectileObjectId = "new", HitResult = ProjectileHitResult.Edge };
        v.Buffer.Update(Scene(new(true, 8000, false)) with { SnapshotSequence = 2, CombatJournal = [old, fresh] }); v.Render();
        Assert.Single(v.Screen.PointHitLabels);
        Assert.True(v.Screen.AvailableMapRect().Contains(v.Screen.PointHitLabels[2].Bounds));
        Assert.True(v.Screen.AvailableMapRect().Contains(v.Screen.EnemyStatusBadges["enemy"].Bounds));
        if (scale == 1 && Environment.GetEnvironmentVariable("DSS_POINT_RENDER_ARTIFACT_DIRECTORY") is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory); using var image = SKImage.FromBitmap(v.Bitmap); using var png = image.Encode(SKEncodedImageFormat.Png, 100);
            using var file = File.Create(Path.Combine(directory, "enemy-feedback-headless.png")); png.SaveTo(file);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Empty_blast_produces_authoritative_miss_without_client_inference(bool counter)
    {
        await using var world = new PointCombatSessionFixture(launchHostile: false);
        var module = counter ? "MOD-PLAYER-COUNTERMEASURE-01" : PointCombatSessionFixture.Torpedo;
        world.Engine.ReceiveCommand(new("empty", 1, PointCombatSessionFixture.Player, module,
            counter ? DefenseCommandTypes.Launch : CombatCommandTypes.Launch, TargetWorldX: 200, TargetWorldY: 0, ProfileId: "fast"));
        var launched = world.Engine.CaptureSnapshotForTests();
        Assert.Contains(launched.Objects, o => o.OwnProjectile?.Kind == (counter ? ProjectileKind.Countermissile : ProjectileKind.Torpedo));
        double deadline = Assert.Single(launched.Objects, o => o.OwnProjectile is not null).OwnProjectile!.DetonationMotionTimeMs;
        var resolved = world.Engine.CaptureSnapshotForTests((long)Math.Ceiling(deadline), simulationTimeMs: (long)Math.Ceiling(deadline));
        var miss = Assert.Single(resolved.CombatJournal, e => e.HitResult == ProjectileHitResult.Miss);
        Assert.Equal(CombatEventType.Miss, miss.Type); Assert.Null(miss.Damage); Assert.Null(miss.Roll); Assert.Null(miss.ChanceTenths);
        using var v = new View(launched);
        v.Buffer.Update(resolved); v.Render();
        Assert.Contains(v.Screen.PointHitLabels.Values, l => l.Text == GameSessionScreen.CombatText("Hit.Miss", "Мимо"));
    }

    [Fact]
    public async Task Actual_destroyed_enemy_retains_public_badge_on_its_wreck()
    {
        var source = PointCombatSessionFixture.Registry();
        var rules = source.CombatRules! with { ShipDestruction = source.CombatRules!.ShipDestruction with { DestroyedModuleFraction = .01m } };
        await using var world = new PointCombatSessionFixture(PointCombatSessionFixture.Registry(rules: rules), launchHostile: false);
        world.Engine.ReceiveCommand(new("fatal", 1, PointCombatSessionFixture.Player, PointCombatSessionFixture.Torpedo,
            CombatCommandTypes.Launch, TargetWorldX: 400, TargetWorldY: 0, ProfileId: "fast"));
        var launched = world.Engine.CaptureSnapshotForTests();
        var flight = Assert.Single(launched.Objects, o => o.OwnProjectile is not null).OwnProjectile!;
        long time = (long)Math.Ceiling(flight.DetonationMotionTimeMs);
        var resolved = world.Engine.CaptureSnapshotForTests(time, simulationTimeMs: time);
        var death = Assert.Single(resolved.CombatJournal, e => e.Type == CombatEventType.ShipDestroyed && e.TargetObjectId == PointCombatSessionFixture.Enemy);
        var wreck = Assert.Single(resolved.Objects, o => o.ObjectId == death.Result);
        Assert.Equal(PlayerRelation.Enemy, wreck.RelationToPlayer);
        Assert.Equal(new EnemyShipStatusSnapshot(true, null, true), wreck.EnemyStatus);
        Assert.Empty(wreck.ModuleHealth);
        using var v = new View(resolved);
        Assert.Equal(GameSessionScreen.CombatText("EnemyDestroyed", "Уничтожен"), v.Screen.EnemyStatusBadges[wreck.ObjectId].Text);
        var saved = world.Engine.CaptureSaveStateForTests(time, SimulationSpeed.Speed0, time);
        var capturedWreck = Assert.Single(saved.GameState.SpaceObjects, o => o.ObjectId == wreck.ObjectId);
        // Isolate this existing persisted object field. Full point-state Save18 migration is a later ticket;
        // Save17 cannot validate point defense state, so this is scenario/object persistence evidence only.
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client", "Scenarios", "PlayerShipOnly", "scenario.json"));
        var scenario = ScenarioLoader.LoadFromFile(path);
        scenario = scenario with { GameState = scenario.GameState with { SpaceObjects = scenario.GameState.SpaceObjects.Append(capturedWreck).ToArray() } };
        var parsed = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(scenario));
        Assert.Equal(PlayerRelation.Enemy, Assert.Single(parsed.GameState.SpaceObjects, o => o.ObjectId == wreck.ObjectId).RelationToPlayer);
        foreach (string invalid in new[] { PlayerRelation.Self, "invalid-relation" })
        {
            var rejected = parsed with
            {
                GameState = parsed.GameState with
                {
                    SpaceObjects = parsed.GameState.SpaceObjects.Select(o => o.ObjectId == wreck.ObjectId ? o with { RelationToPlayer = invalid } : o).ToArray()
                }
            };
            Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(rejected)));
        }
        using var reloaded = new SimulationEngine(PointCombatSessionFixture.Registry(rules: rules));
        reloaded.LoadScenario(parsed);
        Assert.Equal(wreck.EnemyStatus, Assert.Single(reloaded.CaptureSnapshot().Objects, o => o.ObjectId == wreck.ObjectId).EnemyStatus);
    }
}
