using System.Text.Json;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Engine.TacticalBattle.Content;
using Availability = DeepSpaceSaga.Engine.TacticalBattle.Content.TacticalBattleAvailability;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

/// <summary>
/// EP-0030 US-0003 TK-0002: authoritative debug force destroy of one drone. Bypasses hit/armor/shields,
/// spends no shot, torpedo or RNG and does not advance the turn, but applies the normal destroy
/// consequences (EngineRequirements "Команды, события и воспроизводимость": no repeated side effects).
/// </summary>
public sealed class DebugDestroyTests : IDisposable
{
    private static readonly Lazy<TacticalBattleCatalog> Catalog = new(AssemblyTests.Catalog);
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"dss-ep0030-us0003-tk0002-{Guid.NewGuid():N}");
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(
        TestPaths.Repository, "src", "DeepSpaceSaga.Client"));

    public void Dispose()
    {
        try { if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public void DestroysEnemyDroneWithoutRngAmmoOrTurnAdvance()
    {
        using var engine = Engine();
        var before = engine.StartTacticalBattle(new("s", 1)).State!;
        var session = engine.TacticalBattleSessionForTests!;
        var battle = session.Battle;
        var target = battle.Drones.First(d => d.Side != session.PlayerSide && d.IsActive);
        var random = (battle.Random.State, battle.Random.Calls);
        uint initiative = battle.InitiativeRandom.State;
        var ammo = battle.Drones.Select(d => (d.Id, d.AmmoCurrent, d.DecoyCharges)).ToArray();

        var response = engine.DebugDestroyTacticalUnit(new("kill", before.BattleId, before.Revision, target.Id));

        Assert.True(response.Accepted);
        Assert.Equal(TacticalBattleRejectionCode.None, response.Code);
        var state = response.State!;
        Assert.Equal(before.Revision + 1, state.Revision);
        Assert.Equal(before.Turn, state.Turn);
        Assert.Equal(TacticalBattlePhase.Move, state.Phase);
        Assert.Null(state.Outcome);
        var killed = state.Units.Single(u => u.Id == target.Id);
        Assert.Equal(TacticalUnitState.Destroyed, killed.State);
        Assert.Equal(0, killed.Hull);
        Assert.Equal(0, killed.Speed);
        Assert.Equal(TacticalDroneStatus.Destroyed, target.Status);
        Assert.Equal(random, (battle.Random.State, battle.Random.Calls));
        Assert.Equal(initiative, battle.InitiativeRandom.State);
        Assert.Equal(ammo, battle.Drones.Select(d => (d.Id, d.AmmoCurrent, d.DecoyCharges)).ToArray());
        Assert.Equal(target.Side, battle.Statistics.FirstLoss);
        Assert.Equal(1, state.Losses.Single(l => l.Side == target.Side).Count);
        Assert.Empty(state.Shots);
        Assert.Empty(state.Paths);
        // No other unit receives any change from the command.
        Assert.Equal(
            before.Units.Where(u => u.Id != target.Id).Select(u => JsonSerializer.Serialize(u)),
            state.Units.Where(u => u.Id != target.Id).Select(u => JsonSerializer.Serialize(u)));
        Assert.DoesNotContain(state.Suggestions, s => s.DroneId == target.Id);
    }

    [Fact]
    public void DestroysOwnDisabledDrone()
    {
        using var engine = Engine();
        var before = engine.StartTacticalBattle(new("s", 1)).State!;
        var session = engine.TacticalBattleSessionForTests!;
        var own = session.Battle.Drones.First(d => d.Side == session.PlayerSide);
        own.Status = TacticalDroneStatus.Disabled;
        own.Speed = 0;
        own.Hull = 10;

        var response = engine.DebugDestroyTacticalUnit(new("kill-own", before.BattleId, before.Revision, own.Id));

        Assert.True(response.Accepted);
        var unit = response.State!.Units.Single(u => u.Id == own.Id);
        Assert.Equal(TacticalUnitState.Destroyed, unit.State);
        Assert.Equal(0, unit.Hull);
        Assert.Equal(TacticalDroneStatus.Destroyed, own.Status);
        // A disabled drone was already lost; FirstLoss is only claimed by an active-to-lost transition.
        Assert.Null(session.Battle.Statistics.FirstLoss);
        Assert.Contains(response.NewEvents, e => e.Kind == TacticalBattleLogKind.Destroyed && e.TargetId == own.Id);
    }

    [Fact]
    public void LogsDestroyedEvent()
    {
        using var engine = Engine();
        var before = engine.StartTacticalBattle(new("s", 1)).State!;
        var session = engine.TacticalBattleSessionForTests!;
        var target = session.Battle.Drones.First(d => d.Side != session.PlayerSide);

        var response = engine.DebugDestroyTacticalUnit(new("kill-log", before.BattleId, before.Revision, target.Id));

        var logged = Assert.Single(response.NewEvents);
        Assert.Equal(TacticalBattleLogKind.Destroyed, logged.Kind);
        Assert.Equal(target.Id, logged.TargetId);
        Assert.Null(logged.ActorId);
        Assert.Null(logged.Shot);
        Assert.Null(logged.Damage);
        Assert.Equal(TacticalBattlePhase.Move, logged.Phase);
        Assert.Equal(before.Turn, logged.Turn);
        Assert.Equal(new TacticalLogUnit(TacticalUnitKind.Drone, 0, target.MaxHull), logged.Unit);
        Assert.Equal(before.Log[^1].Sequence + 1, logged.Sequence);
        Assert.Equal(logged, response.State!.Log[^1]);
        Assert.Equal(before.Log.Length + 1, response.State.Log.Length);
    }

    [Fact]
    public void RejectsCarrierUnknownAndDestroyedUnchanged()
    {
        using var engine = Engine();
        engine.StartTacticalBattle(new("s", 1));
        var session = engine.TacticalBattleSessionForTests!;
        var dead = session.Battle.Drones.First(d => d.Side != session.PlayerSide);
        dead.Status = TacticalDroneStatus.Destroyed;
        dead.Hull = 0;
        dead.Speed = 0;
        var frame = engine.GetTacticalBattleState()!;

        foreach (var (id, unitId) in new[]
        {
            ("own-carrier", session.Battle.Carrier(session.PlayerSide).Id),
            ("enemy-carrier", session.Battle.Carrier(session.Battle.EnemySide(session.PlayerSide)).Id),
            ("unknown", "no-such-unit"),
            ("empty", ""),
            ("dead", dead.Id),
        })
            AssertRejectedUnchanged(engine, () => engine.DebugDestroyTacticalUnit(new(id, frame.BattleId, frame.Revision, unitId)),
                TacticalBattleRejectionCode.InvalidTarget);
        Assert.Null(session.Battle.Statistics.FirstLoss);
        Assert.All(session.Battle.Carriers, c => Assert.Equal(c.MaxHull, c.Hull));
    }

    [Fact]
    public void RejectsWrongBattleStaleRevisionOverPhase()
    {
        using (var idle = Engine())
        {
            Assert.Equal(TacticalBattleRejectionCode.NoBattle,
                idle.DebugDestroyTacticalUnit(new("no-battle", "battle", 1, "Таг-1")).Code);
            Assert.Equal(TacticalBattleRejectionCode.InvalidRequest, idle.DebugDestroyTacticalUnit(null!).Code);
        }

        using var engine = SessionTests.CloseFireEngine();
        var session = engine.TacticalBattleSessionForTests!;
        var frame = engine.GetTacticalBattleState()!;
        var target = session.Battle.Drones.First(d => d.Side != session.PlayerSide && d.IsActive);
        AssertRejectedUnchanged(engine, () => engine.DebugDestroyTacticalUnit(new("wrong-battle", "other", frame.Revision, target.Id)),
            TacticalBattleRejectionCode.WrongBattle);
        AssertRejectedUnchanged(engine, () => engine.DebugDestroyTacticalUnit(new("stale", frame.BattleId, frame.Revision - 1, target.Id)),
            TacticalBattleRejectionCode.StaleRevision);
        AssertRejectedUnchanged(engine, () => engine.DebugDestroyTacticalUnit(new("future", frame.BattleId, frame.Revision + 1, target.Id)),
            TacticalBattleRejectionCode.StaleRevision);
        Assert.Equal(TacticalDroneStatus.Air, target.Status);

        var over = engine.DebugDestroyTacticalUnit(new("last", frame.BattleId, frame.Revision, target.Id)).State!;
        Assert.Equal(TacticalBattlePhase.Over, over.Phase);
        var survivor = session.Battle.Drones.First(d => d.Side == session.PlayerSide && d.IsActive);
        AssertRejectedUnchanged(engine, () => engine.DebugDestroyTacticalUnit(new("after-over", over.BattleId, over.Revision, survivor.Id)),
            TacticalBattleRejectionCode.WrongPhase);
        Assert.Equal(TacticalDroneStatus.Air, survivor.Status);
    }

    [Fact]
    public void SameCommandIdReturnsCachedReceipt()
    {
        using var engine = Engine();
        var before = engine.StartTacticalBattle(new("s", 1)).State!;
        var session = engine.TacticalBattleSessionForTests!;
        var target = session.Battle.Drones.First(d => d.Side != session.PlayerSide);
        var request = new DebugDestroyTacticalUnitRequest("kill-once", before.BattleId, before.Revision, target.Id);

        var first = engine.DebugDestroyTacticalUnit(request);
        string captured = JsonSerializer.Serialize(engine.GetTacticalBattleState());
        var random = (session.Battle.Random.State, session.Battle.Random.Calls);
        var repeat = engine.DebugDestroyTacticalUnit(request);

        Assert.True(first.Accepted);
        Assert.Same(first, repeat);
        Assert.Equal(captured, JsonSerializer.Serialize(engine.GetTacticalBattleState()));
        Assert.Equal(before.Revision + 1, engine.GetTacticalBattleState()!.Revision);
        Assert.Single(engine.GetTacticalBattleState()!.Log, e => e.Kind == TacticalBattleLogKind.Destroyed);
        Assert.Equal(random, (session.Battle.Random.State, session.Battle.Random.Calls));

        var other = session.Battle.Drones.First(d => d.Side != session.PlayerSide && d.IsActive);
        var conflict = engine.DebugDestroyTacticalUnit(request with { UnitId = other.Id, ExpectedRevision = before.Revision + 1 });
        Assert.False(conflict.Accepted);
        Assert.Equal(TacticalBattleRejectionCode.IdentityConflict, conflict.Code);
        Assert.Equal(TacticalDroneStatus.Air, other.Status);
        Assert.Equal(captured, JsonSerializer.Serialize(engine.GetTacticalBattleState()));
    }

    [Fact]
    public void LastEnemyDroneEndsBattleWithOutcome()
    {
        using var engine = SessionTests.CloseFireEngine();
        var session = engine.TacticalBattleSessionForTests!;
        var frame = engine.GetTacticalBattleState()!;
        Assert.Equal(TacticalBattlePhase.Fire, frame.Phase);
        var last = Assert.Single(session.Battle.ActiveDrones(session.Battle.EnemySide(session.PlayerSide)));
        var random = (session.Battle.Random.State, session.Battle.Random.Calls);

        var response = engine.DebugDestroyTacticalUnit(new("kill-last", frame.BattleId, frame.Revision, last.Id));

        Assert.True(response.Accepted);
        var state = response.State!;
        Assert.Equal(TacticalBattlePhase.Over, state.Phase);
        Assert.NotNull(state.Outcome);
        Assert.Equal(Enum.Parse<TacticalBattleOutcomeCode>(session.PlayerSide), state.Outcome!.Kind);
        Assert.Contains(state.Outcome.Reasons, r => r.Kind == TacticalOutcomeReasonCode.NoDrones);
        Assert.Equal(frame.Turn, state.Turn);
        Assert.Equal(new[] { TacticalBattleLogKind.Destroyed, TacticalBattleLogKind.Outcome }, response.NewEvents.Select(e => e.Kind));
        Assert.All(response.NewEvents, e => Assert.Equal(TacticalBattlePhase.Fire, e.Phase));
        // Records with ImmutableArray members compare arrays by reference; compare by value via JSON.
        Assert.Equal(JsonSerializer.Serialize(state.Outcome), JsonSerializer.Serialize(response.NewEvents[^1].Outcome));
        Assert.Equal(random, (session.Battle.Random.State, session.Battle.Random.Calls));
        Assert.Equal(TacticalBattleRejectionCode.WrongPhase,
            engine.FireTacticalBattle(new("fire-after", state.BattleId, state.Revision)).Code);
    }

    [Fact]
    public void KilledDroneNeitherMovesNorFires()
    {
        using var engine = Engine();
        var before = engine.StartTacticalBattle(new("s", 1)).State!;
        var session = engine.TacticalBattleSessionForTests!;
        var target = session.Battle.Drones.First(d => d.Side != session.PlayerSide);
        var pose = target.Pose;

        var killed = engine.DebugDestroyTacticalUnit(new("kill-move", before.BattleId, before.Revision, target.Id)).State!;
        var moved = engine.MoveTacticalBattle(new("move", killed.BattleId, killed.Revision));

        Assert.True(moved.Accepted);
        Assert.DoesNotContain(moved.State!.Paths, p => p.DroneId == target.Id);
        Assert.DoesNotContain(moved.NewEvents, e => e.ActorId == target.Id);
        Assert.Equal(pose, target.Pose);
        if (moved.State.Phase == TacticalBattlePhase.Fire)
        {
            Assert.DoesNotContain(moved.State.FireQueue, q => q.ShooterId == target.Id);
            var fired = engine.FireTacticalBattle(new("fire", moved.State.BattleId, moved.State.Revision));
            Assert.True(fired.Accepted);
            Assert.DoesNotContain(fired.State!.Shots, s => s.ShooterId == target.Id);
            Assert.DoesNotContain(fired.State.Shots, s => s.TargetId == target.Id);
        }
        else
        {
            Assert.DoesNotContain(moved.State.Shots, s => s.ShooterId == target.Id);
        }
        Assert.Equal(TacticalDroneStatus.Destroyed, target.Status);
        Assert.Equal(0, target.Hull);
    }

    [Fact]
    public void FirePhaseKillRemovesItFromTargets()
    {
        using var engine = SessionTests.CloseFireEngine();
        var session = engine.TacticalBattleSessionForTests!;
        var battle = session.Battle;
        var enemySide = battle.EnemySide(session.PlayerSide);
        var target = Assert.Single(battle.ActiveDrones(enemySide));
        // Keep a second enemy alive so the kill does not end the battle.
        var spare = battle.Drones.First(d => d.Side == enemySide && d.Id != target.Id);
        spare.Status = TacticalDroneStatus.Air;
        var shooter = Assert.Single(battle.ActiveDrones(session.PlayerSide));
        var frame = engine.GetTacticalBattleState()!;
        Assert.Contains(frame.Targets.Single(t => t.DroneId == shooter.Id).List, c => c.TargetId == target.Id);
        Assert.Contains(frame.FireQueue, q => q.ShooterId == target.Id);

        var response = engine.DebugDestroyTacticalUnit(new("kill-fire", frame.BattleId, frame.Revision, target.Id));

        Assert.True(response.Accepted);
        var state = response.State!;
        Assert.Equal(TacticalBattlePhase.Fire, state.Phase);
        Assert.DoesNotContain(state.Targets.SelectMany(t => t.List), c => c.TargetId == target.Id);
        Assert.DoesNotContain(state.Targets, t => t.DefaultTargetId == target.Id);
        Assert.DoesNotContain(state.Incoming, i => i.ShooterId == target.Id);
        Assert.DoesNotContain(state.FireQueue, q => q.ShooterId == target.Id);
        Assert.Equal(TacticalBattleRejectionCode.InvalidTarget, engine.FireTacticalBattle(
            new("fire-dead", state.BattleId, state.Revision, [new(shooter.Id, target.Id)])).Code);
        var fired = engine.FireTacticalBattle(new("fire", state.BattleId, state.Revision));
        Assert.True(fired.Accepted);
        Assert.DoesNotContain(fired.State!.Shots, s => s.ShooterId == target.Id || s.TargetId == target.Id);
    }

    [Fact]
    public void AbortedOwnedSortieTombstonesKilledDrone()
    {
        using var engine = StartOwnedBattle(out var frame);
        var before = engine.GetCarrierRevision("SPC-0001");

        var killed = engine.DebugDestroyTacticalUnit(new("kill-owned", frame.BattleId, frame.Revision, "DRN-PLAYER-02"));
        Assert.True(killed.Accepted);
        Assert.Null(killed.State!.Outcome);

        var exit = engine.ExitTacticalBattle(new("exit", frame.BattleId));
        Assert.True(exit.Accepted);
        var settlement = exit.Settlement!;
        Assert.Equal(SortieExitKind.Aborted, settlement.ExitKind);
        Assert.Equal(new[] { "DRN-PLAYER-02" }, settlement.LostDroneIds);
        Assert.Equal(0, settlement.ConsumedTorpedoes);
        Assert.Equal(before + 1, engine.GetCarrierRevision("SPC-0001"));
        var tomb = engine.CaptureSnapshot().CarrierOperations!.OwnedDrones.Single(d => d.Id == "DRN-PLAYER-02");
        Assert.Contains("destroyed", tomb.ReadinessReasons);
        Assert.Same(exit, engine.ExitTacticalBattle(new("exit", frame.BattleId)));
    }

    private static void AssertRejectedUnchanged(SimulationEngine engine, Func<TacticalBattleResponse> action, TacticalBattleRejectionCode code)
    {
        var battle = engine.TacticalBattleSessionForTests!.Battle;
        string before = JsonSerializer.Serialize(engine.GetTacticalBattleState());
        var random = (battle.Random.State, battle.Random.Calls);
        uint initiative = battle.InitiativeRandom.State;
        var result = action();
        Assert.False(result.Accepted);
        Assert.Equal(code, result.Code);
        Assert.Empty(result.NewEvents);
        Assert.Equal(before, JsonSerializer.Serialize(engine.GetTacticalBattleState()));
        Assert.Equal(random, (battle.Random.State, battle.Random.Calls));
        Assert.Equal(initiative, battle.InitiativeRandom.State);
    }

    private static SimulationEngine Engine()
    {
        var engine = new SimulationEngine(GameDataRegistry.Create([], [], [], [], tacticalBattle: Availability.Available(Catalog.Value)));
        engine.SetSpeed(SimulationSpeed.Speed0);
        return engine;
    }

    // Owned-sortie fixture: copied (not extracted) from Carrier/Ep0029Us0006Tk0002Tests.
    private SimulationEngine StartOwnedBattle(out TacticalBattleSnapshot frame)
    {
        Directory.CreateDirectory(_directory);
        string scenarioPath = Path.Combine(_directory, "scenario.json");
        File.WriteAllText(scenarioPath, OwnedScenario().ToJsonString());
        var engine = EngineContentLoader.CreateEngineFromScenarioFile(Path.Combine(ClientRoot, "Settings.json"), scenarioPath);
        engine.SetOwnedWingForTests("SPC-0001", new[]
        {
            OwnedDrone("DRN-PLAYER-01", "CHR-PILOT-1", "BAY-01", 0),
            OwnedDrone("DRN-PLAYER-02", "CHR-PILOT-2", "BAY-01", 1),
            OwnedDrone("DRN-PLAYER-03", "CHR-PILOT-3", "BAY-02", 0),
            OwnedDrone("DRN-PLAYER-04", "CHR-PILOT-4", "BAY-02", 1),
        });
        var start = new StartTacticalBattleRequest("start", 1, "initial") { SourceShipId = "SPC-0001", ExpectedCarrierRevision = 0 };
        var response = engine.StartTacticalBattle(start);
        Assert.True(response.Accepted);
        frame = response.State!;
        return engine;
    }

    private static OwnedDroneData OwnedDrone(string id, string crewId, string bay, int slot) => new(
        DroneId: id, HangarModuleId: bay, SlotIndex: slot, CrewId: crewId,
        FrameId: "heavy.sword.shield", WeaponId: "gun.sword.t1", ArmorId: "plate.medium",
        ShieldId: "shield.t1", ShieldCount: 2, ShieldSlots: 2,
        DecoyId: "decoy.t1", DecoyMaxCharges: 2, DecoyCharges: 2,
        CurrentHull: 100, MaxHull: 100, Armor: 50, MaxArmor: 50,
        AmmoCurrent: 12, AmmoCapacity: 12, EngineIds: ["drive.standard", "drive.standard"]);

    private static JsonObject OwnedScenario()
    {
        var hull = new JsonArray();
        for (int y = 0; y < 5; y++)
            for (int x = 0; x < 5; x++)
                hull.Add(new JsonObject { ["x"] = x, ["y"] = y });
        var ship = new JsonObject
        {
            ["objectId"] = "SPC-0001",
            ["objectType"] = "PlayerShip",
            ["shipClassId"] = "ship.tetrarch",
            ["persistenceType"] = "Permanent",
            ["name"] = "Tetrarch",
            ["positionX"] = 0,
            ["positionY"] = 0,
            ["speedMps"] = 0,
            ["directionDegrees"] = 0,
            ["movementType"] = "Stationary",
            ["isKnown"] = true,
            ["hullLayout"] = new JsonObject { ["width"] = 5, ["height"] = 5, ["cells"] = hull },
            ["modules"] = new JsonArray(Bay("BAY-01", 0), Bay("BAY-02", 2)),
            ["crew"] = new JsonArray(
                Crew("CHR-0001", "Captain"), Crew("CHR-PLAYER-DEFENSE", "Officer"),
                Crew("CHR-PILOT-1", "Pilot"), Crew("CHR-PILOT-2", "Pilot"),
                Crew("CHR-PILOT-3", "Pilot"), Crew("CHR-PILOT-4", "Pilot")),
        };
        return new JsonObject
        {
            ["scenarioMetadata"] = new JsonObject { ["scenarioId"] = "ep0030-us0003", ["name"] = "ep0030-us0003" },
            ["gameState"] = new JsonObject
            {
                ["masterSeed"] = 42,
                ["gameTimeMs"] = 0,
                ["currentSpeed"] = "Speed0",
                ["playerShipObjectId"] = "SPC-0001",
                ["spaceObjects"] = new JsonArray(ship),
            },
        };
    }

    private static JsonObject Bay(string moduleId, int ax)
    {
        var cells = new JsonArray();
        foreach (var (x, y) in new[] { (ax, 0), (ax + 1, 0), (ax, 1), (ax + 1, 1) })
            cells.Add(new JsonObject { ["x"] = x, ["y"] = y });
        return new JsonObject
        {
            ["moduleId"] = moduleId,
            ["moduleTypeId"] = "module.drone-bay.medium",
            ["occupiedCells"] = cells,
            ["structurePoints"] = 250,
            ["powerState"] = "On",
            ["operationalState"] = "Ready",
            ["cargo"] = new JsonArray(),
        };
    }

    private static JsonObject Crew(string id, string role) => new()
    {
        ["crewId"] = id,
        ["displayName"] = id,
        ["role"] = role,
        ["accuracySkill"] = 10,
        ["maneuverabilitySkill"] = 10,
        ["initiative"] = 55,
    };
}
