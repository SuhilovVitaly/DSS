using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0003TK0002Tests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0008TK0002Tests
{
    private static IEnumerable<T> All<T>(TypeRegistry<T> registry) where T : ITypeDefinition => Enumerable.Range(0, registry.Count).Select(registry.GetDefinition);
    internal static SimulationEngine Battle(SimulationClock clock, SimulationSpeed initial = SimulationSpeed.Speed3,
        double distance = 40, double npcSpeed = 0, int remainder = 3, bool station = false)
    {
        var source = Registry(scatter: 0);
        var registry = GameDataRegistry.Create(All(source.ModuleCategories), All(source.ModuleTypes), All(source.ItemTypes),
            All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes), All(source.Dialogues), All(source.Quests),
            stationMarketProfiles: All(source.StationMarketProfiles), shipClasses: All(source.ShipClasses), stationMarketEvents: All(source.StationMarketEvents),
            combatRules: source.CombatRules! with
            { ShipDestruction = source.CombatRules.ShipDestruction with { DestroyedModuleFraction = .01m } });
        var scenario = TorpedoImpactTests.Scenario(o => o with
        {
            PositionX = o.ObjectId == Player ? 0 : distance * 10,
            PositionY = 0,
            SpeedMps = o.ObjectId == Player ? 0 : npcSpeed * 1000,
            DirectionDegrees = 270,
            MovementType = "Linear",
            Ai = o.ObjectId == Target ? new(25, 8, 35) : null,
            Modules = o.Modules!.Select(m => m with { StructurePoints = 100, Armor = 0, ArmorClass = "none" }).ToArray()
        });
        var objects = scenario.GameState.SpaceObjects;
        if (station) objects = objects.Append(new SpaceObjectData("station", SpaceObjectType.Station, "Permanent", "Station", 1, 0, 0, 270,
            "Stationary", null, null, null, IsKnown: true, Credits: 10000,
            StationCrew: [new("dock-operator", StationCrewRoles.DockOperator, "Operator")],
            PortFeeCreditsPerDay: 100, SecurityZoneRadiusKm: 200, PiracyWarningGracePeriodMs: 60000)).ToArray();
        var engine = new SimulationEngine(registry, clock: clock);
        engine.LoadScenario(scenario with
        {
            GameState = scenario.GameState with
            { CurrentSpeed = initial.ToString(), MotionQuarterRemainder = remainder, SpaceObjects = objects }
        });
        return engine;
    }

    [Theory]
    [InlineData(SimulationSpeed.Speed2)]
    [InlineData(SimulationSpeed.Speed3)]
    [InlineData(SimulationSpeed.Speed4)]
    public void Combat_speed_locks_and_restores_previous(SimulationSpeed initial)
    {
        var clock = new SimulationClock(SimulationSpeed.Speed0, () => 0);
        using var engine = Battle(clock, initial);
        var active = engine.CaptureSnapshot();
        Assert.Equal(CombatEncounterState.Active, active.Encounter!.State);
        Assert.Equal(SimulationSpeed.Speed1, active.CurrentSpeed);
        foreach (var forbidden in new[] { SimulationSpeed.Speed3, SimulationSpeed.Speed4 })
        {
            Assert.Equal(CommandReasonCodes.CombatSpeedLocked, engine.TrySetSpeed(forbidden));
            Assert.Equal(SimulationSpeed.Speed1, engine.CurrentSpeed);
        }
        Assert.Null(engine.TrySetSpeed(SimulationSpeed.Speed0));
        Assert.Equal(SimulationSpeed.Speed0, engine.CurrentSpeed);
        Assert.Null(engine.TrySetSpeed(SimulationSpeed.Speed1));
        var won = PointCombatUS0008TK0001Tests.Win(engine);
        Assert.Equal(CombatEncounterState.Won, won.Encounter!.State);
        Assert.Equal(initial, won.CurrentSpeed);
        Assert.Equal(initial, engine.CurrentSpeed);
        Assert.Equal(3, clock.Capture().MotionQuarterRemainder);
    }

    [Fact]
    public void Combat_admission_preserves_an_existing_pause()
    {
        var clock = new SimulationClock(SimulationSpeed.Speed0, () => 0);
        using var engine = Battle(clock, SimulationSpeed.Speed0);
        Assert.Equal(CombatEncounterState.Active, engine.CaptureSnapshot().Encounter!.State);
        Assert.Equal(SimulationSpeed.Speed0, engine.CurrentSpeed);
        Assert.Equal(CommandReasonCodes.CombatSpeedLocked, engine.TrySetSpeed(SimulationSpeed.Speed3));
        Assert.Null(engine.TrySetSpeed(SimulationSpeed.Speed1));
    }

    [Fact]
    public void High_speed_interval_switches_to_x1_when_combat_begins()
    {
        long real = 0;
        var clock = new SimulationClock(SimulationSpeed.Speed0, () => real);
        using var engine = Battle(clock, SimulationSpeed.Speed4, distance: 101, npcSpeed: 1, remainder: 0);
        Assert.Equal(CombatEncounterState.Idle, engine.CaptureSnapshot().Encounter!.State);
        real = 4000;
        var snapshot = engine.CaptureSnapshot(advanceClock: true);
        // First 40 real ms at x100 advance 1000 physical ms to the 100 km boundary.
        // Remaining 3960 real ms at x1 advance 990 physical ms.
        Assert.Equal(1000, snapshot.Encounter!.StartedAtMotionMs);
        Assert.Equal(1990, snapshot.MotionTimeMs);
        Assert.Equal(597000, snapshot.GameTimeMs);
        Assert.Equal(SimulationSpeed.Speed1, snapshot.CurrentSpeed);
    }

    [Fact]
    public void Compact_frames_publish_confirmed_speed_after_high_speed_combat_admission()
    {
        long real = 0;
        var clock = new SimulationClock(SimulationSpeed.Speed0, () => real);
        using var engine = Battle(clock, SimulationSpeed.Speed4, distance: 101, npcSpeed: 1, remainder: 0);
        engine.SetPresentationTimestampForTests(() => 0);
        engine.EnablePresentationLease();
        var initial = engine.CapturePresentationUpdate(fullSnapshot: true);
        Assert.Equal(CombatEncounterState.Idle, initial.FullSnapshot!.Encounter!.State);
        Assert.Equal(SimulationSpeed.Speed4, initial.FullSnapshot.CurrentSpeed);
        real = 4000;
        var update = engine.CapturePresentationUpdate(fullSnapshot: true);
        Assert.Equal(1990, update.FullSnapshot!.MotionTimeMs);
        var activeFrames = update.Frames.Where(f => f.State.Encounter!.State == CombatEncounterState.Active).ToArray();
        Assert.NotEmpty(activeFrames);
        Assert.All(activeFrames, f => Assert.Equal(SimulationSpeed.Speed1, f.State.CurrentSpeed));
        Assert.Equal(SimulationSpeed.Speed1, update.FullSnapshot!.CurrentSpeed);
    }

    [Fact]
    public void Quarter_remainder_and_x1_pace_survive_pause_and_resume()
    {
        long real = 0;
        var clock = new SimulationClock(SimulationSpeed.Speed0, () => real);
        using var engine = Battle(clock);
        engine.CaptureSnapshot();
        real = 2;
        engine.CaptureSnapshot(advanceClock: true);
        Assert.Equal(1, clock.Capture().MotionTimeMs);
        Assert.Equal(1, clock.Capture().MotionQuarterRemainder);
        Assert.Null(engine.TrySetSpeed(SimulationSpeed.Speed0));
        real = 10000;
        engine.CaptureSnapshot(advanceClock: true);
        Assert.Equal(1, clock.Capture().MotionTimeMs);
        Assert.Equal(1, clock.Capture().MotionQuarterRemainder);
        Assert.Null(engine.TrySetSpeed(SimulationSpeed.Speed1));
        real = 10003;
        engine.CaptureSnapshot(advanceClock: true);
        Assert.Equal(2, clock.Capture().MotionTimeMs);
        Assert.Equal(0, clock.Capture().MotionQuarterRemainder);
        real += 4000;
        engine.CaptureSnapshot(advanceClock: true);
        Assert.Equal(1002, clock.Capture().MotionTimeMs);
    }

    [Fact]
    public void Remaining_real_interval_uses_restored_speed_after_victory()
    {
        long real = 0;
        var clock = new SimulationClock(SimulationSpeed.Speed0, () => real);
        using var engine = Battle(clock, remainder: 0);
        engine.CaptureSnapshot();
        engine.ReceiveCommand(Launch(x: 400, y: 0, profile: "fast"));
        engine.CaptureSnapshot();
        real = 11000;
        var won = engine.CaptureSnapshot(advanceClock: true);
        // Blast at 2500 physical ms consumes 10000 real ms at x1;
        // remaining 1000 real ms run at the restored x20 (5 physical seconds).
        Assert.Equal(CombatEncounterState.Won, won.Encounter!.State);
        Assert.Equal(2500, won.Encounter.EndedAtMotionMs);
        Assert.Equal(7500, won.MotionTimeMs);
        Assert.Equal(SimulationSpeed.Speed3, won.CurrentSpeed);
    }

    [Fact]
    public void Dialogue_modal_resume_obeys_active_lock_and_preserves_quarter_remainder()
    {
        var clock = new SimulationClock(SimulationSpeed.Speed0, () => 0);
        using var engine = Battle(clock, station: true);
        engine.CaptureSnapshot();
        engine.ReceiveCommand(new("dock", 1, Player, "MOD-PLAYER-BRIDGE-01", NavigationComputerCommandTypes.Dock, TargetObjectId: "station"));
        var paused = engine.CaptureSnapshot();
        Assert.True(paused.ActiveDialogue is not null, string.Join(",", paused.CommandResults.Select(r => r.ReasonCode)));
        Assert.Equal(SimulationSpeed.Speed0, paused.CurrentSpeed);
        Assert.Equal(CommandReasonCodes.CombatSpeedLocked, engine.TrySetSpeed(SimulationSpeed.Speed3));
        var dialogue = paused.ActiveDialogue!;
        engine.ReceiveDialogueCommand(new("abort", DialogueAction.Abort, dialogue.InstanceId, dialogue.Revision));
        var resumed = engine.CaptureSnapshot();
        Assert.Null(resumed.ActiveDialogue);
        Assert.Equal(SimulationSpeed.Speed1, resumed.CurrentSpeed);
        Assert.Equal(3, clock.Capture().MotionQuarterRemainder);
    }

    [Fact]
    public async Task Local_transport_returns_immediate_rejection_with_confirmed_speed()
    {
        var clock = new SimulationClock(SimulationSpeed.Speed0, () => 0);
        using var engine = Battle(clock);
        engine.CaptureSnapshot();
        await using var connection = new LocalGameSessionConnection(engine);
        var rejected = await connection.SetPresentationSpeedAsync(SimulationSpeed.Speed4);
        Assert.Equal(CommandReasonCodes.CombatSpeedLocked, rejected.ReasonCode);
        Assert.Equal(SimulationSpeed.Speed1, rejected.CurrentSpeed);
        var paused = await connection.SetPresentationSpeedAsync(SimulationSpeed.Speed0);
        Assert.Null(paused.ReasonCode);
        Assert.Equal(SimulationSpeed.Speed0, paused.CurrentSpeed);
        var resume = await connection.ResumePresentationAsync(SimulationSpeed.Speed3);
        Assert.Equal(CommandReasonCodes.CombatSpeedLocked, resume.ReasonCode);
        Assert.Equal(SimulationSpeed.Speed0, resume.CurrentSpeed);
        await connection.SetSimulationSpeedAsync(SimulationSpeed.Speed4);
        Assert.Equal(SimulationSpeed.Speed0, engine.CurrentSpeed);
    }

    [Fact]
    public void Watchdog_pause_is_not_overridden_by_victory_restore()
    {
        var clock = new SimulationClock(SimulationSpeed.Speed0, () => 0);
        using var engine = Battle(clock);
        engine.CaptureSnapshot();
        engine.ReportPresentationProgress(0, pauseRequested: true, engine.CapturePresentationControl().SessionId);
        Assert.Equal(SimulationSpeed.Speed0, engine.CurrentSpeed);
        var won = PointCombatUS0008TK0001Tests.Win(engine);
        Assert.Equal(CombatEncounterState.Won, won.Encounter!.State);
        Assert.Equal(SimulationSpeed.Speed0, won.CurrentSpeed);
        Assert.True(engine.CapturePresentationControl().AutomaticPause);
        var resumed = engine.ResumePresentationControl(SimulationSpeed.Speed3);
        Assert.Null(resumed.ReasonCode);
        Assert.Equal(SimulationSpeed.Speed3, resumed.CurrentSpeed);
    }

    [Fact]
    public void Terminal_defeat_rejects_resume_and_never_restores_speed()
    {
        var clock = new SimulationClock(SimulationSpeed.Speed0, () => 0);
        using var engine = Battle(clock);
        engine.CaptureSnapshot();
        engine.ReceiveCommand(Launch(x: 0, y: 0, profile: "fast"));
        PointCombatUS0008TK0001Tests.At(engine);
        var lost = PointCombatUS0008TK0001Tests.At(engine, 1);
        Assert.Equal(CombatEncounterState.Lost, lost.Encounter!.State);
        Assert.Equal(SessionOutcomeState.Defeated, lost.EffectiveOutcome.State);
        Assert.Equal(CommandReasonCodes.SessionDefeated, engine.TrySetSpeed(SimulationSpeed.Speed1));
        Assert.Equal(CommandReasonCodes.SessionDefeated, engine.ResumePresentationControl(SimulationSpeed.Speed3).ReasonCode);
        Assert.Equal(SimulationSpeed.Speed0, engine.CurrentSpeed);
    }
}
