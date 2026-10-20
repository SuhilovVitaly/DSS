using System.Diagnostics;
using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class PhaseThreeJournalUiTests
{
    private static CombatJournalEntry Entry => new(1, 1000, CombatEventType.Expired, "player", "target", "pr", 0, 0,
        250, RatingBreakdown: new(new("crew", "Operator", WeaponSkillType.CountermeasureDefense, 99, 55, 55), 5),
        Damage: 0, Result: "lifetime_expired", LauncherModuleId: "defense-two", LaunchMode: LaunchMode.Manual,
        Accuracy: 30, TargetManeuverability: 5, TerminationKind: CountermeasureTerminationKind.LifetimeExpired);

    [Fact]
    public void Own_module_and_public_profile_remain_without_retired_modes_or_probability()
    {
        var entry = Entry with
        {
            Type = CombatEventType.Miss,
            Roll = 801,
            TerminationKind = null,
            ProfileId = "wide-profile",
            ProjectileKind = ProjectileKind.Countermissile,
            BlastRadiusKm = 1,
            HitResult = ProjectileHitResult.Miss
        };
        var manual = CombatJournalPanel.Format(entry, "player");
        var automatic = CombatJournalPanel.Format(entry with { LaunchMode = LaunchMode.Auto }, "player");
        Assert.Contains(manual, l => l.Contains("defense-two"));
        Assert.Contains(manual, l => l.Contains("wide-profile"));
        Assert.Equal(manual, automatic);
        Assert.DoesNotContain(manual, l => l.Contains(Localization.Get("Combat.DefenseRating")) || l.Contains("801/1000") || l.Contains("25.0%"));
        Assert.DoesNotContain(manual, l => l.Contains(Localization.Get("Combat.InterceptFormula")) || l.Contains("×") || l.Contains("/50"));
    }

    [Theory]
    [InlineData(CountermeasureTerminationKind.LifetimeExpired, "lifetime_expired")]
    [InlineData(CountermeasureTerminationKind.MissedCoastExpired, "missed_coast_expired")]
    [InlineData(CountermeasureTerminationKind.TargetLost, "target_lost")]
    [InlineData(CountermeasureTerminationKind.OwnerLost, "owner_lost")]
    public void Termination_reason_does_not_claim_a_roll_or_hull_damage(CountermeasureTerminationKind kind, string key)
    {
        var entry = Entry with { TerminationKind = kind };
        var lines = CombatJournalPanel.Format(entry);
        Assert.Contains(lines, l => l.Contains(Localization.Get("Combat.Reason." + key)));
        Assert.DoesNotContain(lines, l => l.Contains(Localization.Get("Combat.NoRoll")));
        Assert.DoesNotContain(lines, l => l.StartsWith(Localization.Get("Combat.Damage")));
        var store = new CombatEffectStore(() => 0);
        store.ReceiveJournal([entry], 0);
        Assert.Empty(store.Results);
        Assert.Empty(store.Active);
    }

    [Fact]
    public void Long_wrapped_entry_scrolls_to_actual_last_reason_and_collapse_preserves_facts()
    {
        var entry = Entry with { LauncherModuleId = "module-" + new string('W', 300) };
        var panel = new CombatJournalPanel();
        using var bitmap = new SKBitmap(640, 360);
        using var canvas = new SKCanvas(bitmap);
        panel.Render(canvas, 640, 360, [entry], playerShipObjectId: "player");
        panel.Click(panel.Bounds.MidX, 60);
        panel.Render(canvas, 640, 360, [entry], playerShipObjectId: "player");
        string reason = Localization.Get("Combat.Reason.lifetime_expired");
        Assert.DoesNotContain(reason, string.Concat(panel.VisibleLines));
        for (int i = 0; i < 500; i++) panel.Scroll(panel.Bounds.MidX, 100, 1);
        panel.Render(canvas, 640, 360, [entry], playerShipObjectId: "player");
        Assert.Contains(reason, string.Concat(panel.VisibleLines));
        panel.Click(panel.Bounds.MidX, 60);
        panel.Render(canvas, 640, 360, [entry], playerShipObjectId: "player");
        Assert.Equal(entry, Assert.Single(panel.Entries));
    }

    [Fact]
    public void Real_file_load_keeps_expiry_history_without_replaying_old_effects()
    {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(root, "Settings.json"), out _, out _);
        var scenario = ScenarioLoader.LoadFromFile(Path.Combine(root, "Scenarios", "PlayerShipOnly", "scenario.json"));
        using var engine = new SimulationEngine(registry);
        engine.LoadScenario(scenario with
        {
            GameState = scenario.GameState with
            {
                MasterSeed = 42,
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o with
                {
                    ShipClassId = o.ObjectId == "SPC-0001" ? null : o.ShipClassId,
                    PositionX = 0,
                    PositionY = o.ObjectId == "SPC-0001" ? 0 : -200,
                    DirectionDegrees = o.ObjectId == "SPC-0001" ? 0 : 180,
                    SpeedMps = 0,
                    MovementType = "Stationary",
                    Modules = o.Modules!.Select(m => m with
                    {
                        AutoDefenseEnabled = false,
                        PowerState = m.ModuleTypeId == "module.engine.basic" ? "Off" : "On"
                    }).ToArray()
                }).ToArray()
            }
        });
        // This fixture tests journal restoration with an already launched threat, not AI reaction timing.
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostileTorpedoFire("SPC-0002", "MOD-PIRATE-TORPEDO-01", 1).Disposition);
        string target = Assert.Single(engine.CaptureSnapshotForTests(1, SimulationSpeed.Speed0, 1).Objects, o => o.Torpedo is not null).ObjectId;
        engine.ReceiveCommand(new("manual", 1, "SPC-0001", "MOD-PLAYER-COUNTERMEASURE-01", DefenseCommandTypes.Fire, target));
        engine.CaptureSnapshotForTests(1, SimulationSpeed.Speed0, 1);
        var before = engine.CaptureSnapshotForTests(4000, SimulationSpeed.Speed0, 4000);
        var expired = Assert.Single(before.CombatJournal, e => e.Type == CombatEventType.Expired);
        Assert.Equal(CountermeasureTerminationKind.MissedCoastExpired, expired.TerminationKind);
        Assert.Null(expired.Roll);
        string path = Path.Combine(Path.GetTempPath(), "dss-journal-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(path, ScenarioLoader.Serialize(engine.CaptureSaveStateForTests(4000, SimulationSpeed.Speed0)));
            using var restored = new SimulationEngine(registry);
            restored.LoadScenario(ScenarioLoader.LoadFromFile(path, allowNonZeroGameTime: true), isSave: true);
            var loaded = restored.CaptureSnapshotForTests(4000, SimulationSpeed.Speed0, 4000);
            Assert.Equal(JsonSerializer.Serialize(before.CombatJournal), JsonSerializer.Serialize(loaded.CombatJournal));
            long now = 0;
            var buffer = new SnapshotBuffer(() => now); buffer.Update(loaded);
            var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => now);
            for (int i = 0; i < 3; i++)
            {
                screen.OnActivated(); TorpedoCommandPanelTests.Render(screen);
                Assert.Empty(screen.CombatEffects.Results); Assert.Empty(screen.CombatEffects.Active);
                screen.OnDeactivated(); now += Stopwatch.Frequency * 3;
            }
            var panel = new CombatJournalPanel();
            using var bitmap = new SKBitmap(1280, 720); using var canvas = new SKCanvas(bitmap);
            panel.Render(canvas, 1280, 720, loaded.CombatJournal);
            Assert.Contains(expired, panel.Entries);
        }
        finally { File.Delete(path); }
    }
}
