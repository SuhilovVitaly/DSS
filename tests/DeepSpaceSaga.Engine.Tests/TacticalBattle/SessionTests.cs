using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Engine.TacticalBattle.Content;
using DeepSpaceSaga.Motion.TacticalBattle;
using Availability = DeepSpaceSaga.Engine.TacticalBattle.Content.TacticalBattleAvailability;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

public sealed class SessionTests
{
    private static readonly Lazy<TacticalBattleCatalog> Catalog = new(AssemblyTests.Catalog);

    [Fact]
    public void OptionsAndSnapshotsAreCompleteImmutableAndQueriesConsumeNoRandom()
    {
        using var engine = Engine();
        var options = engine.GetTacticalBattleOptions();
        Assert.True(options.Availability.Available);
        Assert.Equal(7, options.Weapons.Length);
        Assert.Equal(6, options.Armor.Length);
        Assert.Equal("initial", options.Scenarios[0].Id);
        Assert.Equal(2, options.Scenarios[0].Drones[0].ShieldSlots);
        Assert.Equal(new[] { "raid", "defence", "dogfight", "balanced", "commander" }, options.Tactics.Select(t => t.Id));
        var started = engine.StartTacticalBattle(new("start", 1));
        Assert.True(started.Accepted);
        var frame = started.State!;
        Assert.Equal(TacticalBattlePhase.Move, frame.Phase);
        Assert.Equal(1, frame.Turn);
        Assert.Equal(12, frame.Units.Length);
        Assert.Equal(4, frame.Suggestions.Length);
        Assert.Empty(frame.Targets);
        Assert.Empty(frame.Incoming);
        Assert.Equal(58, Calls(engine));
        string original = JsonSerializer.Serialize(frame);
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal(original, JsonSerializer.Serialize(engine.GetTacticalBattleState()));
            Assert.True(engine.GetTacticalBattleOptions().Availability.Available);
        }
        Assert.Equal(58, Calls(engine));
        Assert.Equal(65, frame.Units[0].MaxHull);
        Assert.Equal(80, frame.Units[0].MaxArmor);
        Assert.Equal(2, frame.Units[0].Engines.Length);
        Assert.Equal(50, frame.Units[0].Build!.DamageMin);
        Assert.Equal(870, frame.Units[0].Limits!.Mass);
        var moved = engine.MoveTacticalBattle(new("move", frame.BattleId, frame.Revision));
        Assert.True(moved.Accepted);
        Assert.Equal(original, JsonSerializer.Serialize(frame));
        Assert.True(moved.State!.FireSkipped);
        Assert.Equal(TacticalBattlePhase.Move, moved.State.Phase);
        Assert.Equal(2, moved.State.Turn);
        Assert.Equal(1, moved.State.Paths[0].Turn);
        Assert.Equal(12, moved.State.BeforeSalvoUnits.Length);
        Assert.Empty(moved.State.Shots);
        Assert.Contains(moved.NewEvents, e => e.Kind == TacticalBattleLogKind.NoTargets && e.Phase == TacticalBattlePhase.Move && e.Turn == 1);
        Assert.Equal(74, Calls(engine));
    }

    [Theory]
    [InlineData("scenario", TacticalBattleRejectionCode.UnknownScenario)]
    [InlineData("ownTactic", TacticalBattleRejectionCode.UnknownTactic)]
    [InlineData("enemyTactic", TacticalBattleRejectionCode.UnknownTactic)]
    [InlineData("drone", TacticalBattleRejectionCode.InvalidDrone)]
    [InlineData("gun", TacticalBattleRejectionCode.UnknownWeapon)]
    [InlineData("plate", TacticalBattleRejectionCode.UnknownArmor)]
    [InlineData("shield", TacticalBattleRejectionCode.UnknownShield)]
    [InlineData("shieldCount", TacticalBattleRejectionCode.InvalidShieldCount)]
    [InlineData("absentShieldCount", TacticalBattleRejectionCode.InvalidShieldCount)]
    [InlineData("duplicate", TacticalBattleRejectionCode.DuplicateDrone)]
    [InlineData("nullChoice", TacticalBattleRejectionCode.InvalidRequest)]
    public void InvalidStartRejectsBeforeCreatingBattle(string defect, TacticalBattleRejectionCode expected)
    {
        using var engine = Engine();
        var choice = new TacticalLoadoutChoice("Вера", "gun.sword.t1", "plate.medium", "shield.t1", 2);
        var request = new StartTacticalBattleRequest("start", 1, Loadout: [choice]);
        request = defect switch
        {
            "scenario" => request with { ScenarioId = "missing" },
            "ownTactic" => request with { OwnTacticId = "missing" },
            "enemyTactic" => request with { EnemyTacticId = "missing" },
            "drone" => request with { Loadout = [choice with { DroneId = "Таг-1" }] },
            "gun" => request with { Loadout = [choice with { GunId = "missing" }] },
            "plate" => request with { Loadout = [choice with { PlateId = "missing" }] },
            "shield" => request with { Loadout = [choice with { ShieldId = "missing" }] },
            "shieldCount" => request with { Loadout = [choice with { ShieldCount = 3 }] },
            "absentShieldCount" => request with { Loadout = [choice with { ShieldId = null, ShieldCount = 1 }] },
            "duplicate" => request with { Loadout = [choice, choice] },
            "nullChoice" => request with { Loadout = [null!] },
            _ => throw new InvalidOperationException()
        };
        var response = engine.StartTacticalBattle(request);
        Assert.False(response.Accepted);
        Assert.Equal(expected, response.Code);
        Assert.Null(response.CurrentPhase);
        Assert.Null(engine.GetTacticalBattleState());
        Assert.False(engine.IsTacticalBattleActive);
    }

    [Theory]
    [InlineData(double.NaN, 0)]
    [InlineData(double.PositiveInfinity, 0)]
    [InlineData(double.NegativeInfinity, 0)]
    [InlineData(60, double.NaN)]
    [InlineData(60, double.PositiveInfinity)]
    [InlineData(60, double.NegativeInfinity)]
    public void NonfiniteMoveRejectsWithoutChangingStateEventsOrRandom(double speed, double turn)
    {
        using var engine = Engine();
        var frame = engine.StartTacticalBattle(new("start", 1)).State!;
        AssertRejectedUnchanged(engine, () => engine.MoveTacticalBattle(new("bad", frame.BattleId, frame.Revision, [new("Вера", speed, turn)])), TacticalBattleRejectionCode.InvalidNumber);
    }

    [Theory]
    [InlineData("Таг-1")]
    [InlineData("Скиталец")]
    [InlineData("missing")]
    public void NonOwnedOrUnknownDroneRejectsAtomicPartialBatch(string badId)
    {
        using var engine = Engine();
        var frame = engine.StartTacticalBattle(new("start", 1)).State!;
        AssertRejectedUnchanged(engine, () => engine.MoveTacticalBattle(new("bad", frame.BattleId, frame.Revision,
            [new("Вера", 0, 0), new(badId, 0, 0)])), TacticalBattleRejectionCode.InvalidDrone);
    }

    [Fact]
    public void InactiveDuplicateAndNullOrdersAreRejected()
    {
        using var engine = Engine();
        var frame = engine.StartTacticalBattle(new("start", 1)).State!;
        AssertRejectedUnchanged(engine, () => engine.MoveTacticalBattle(new("dup", frame.BattleId, frame.Revision,
            [new("Вера", 0, 0), new("Вера", 60, 10)])), TacticalBattleRejectionCode.DuplicateDrone);
        AssertRejectedUnchanged(engine, () => engine.MoveTacticalBattle(new("null", frame.BattleId, frame.Revision, [null!])), TacticalBattleRejectionCode.InvalidRequest);
        engine.TacticalBattleSessionForTests!.Battle.Drones[0].Status = TacticalDroneStatus.Disabled;
        AssertRejectedUnchanged(engine, () => engine.MoveTacticalBattle(new("inactive", frame.BattleId, frame.Revision,
            [new("Вера", 60, 0)])), TacticalBattleRejectionCode.InvalidDrone);
    }

    [Fact]
    public void MovesClampTouchedOrdersWhileOmittedOrdersUseFullAiSuggestions()
    {
        using var engine = Engine();
        var frame = engine.StartTacticalBattle(new("start", 1)).State!;
        var unit = frame.Units[0];
        var l = unit.Limits!;
        var limits = new TacticalLimits(l.Mass, l.MassFactor, l.MinSpeed, l.MaxSpeed, l.Change, l.Turn);
        double speed = limits.ClampSpeed(double.MaxValue, unit.Speed);
        double turn = -limits.TurnLimit(speed, frame.Rules.TurnAtMaxSpeedFraction);
        var expected = TacticalGeometry.Integrate(new(unit.Pose.X, unit.Pose.Y, unit.Pose.Course), speed, turn, frame.Rules.Substeps);
        var result = engine.MoveTacticalBattle(new("move", frame.BattleId, frame.Revision, [new(unit.Id, double.MaxValue, -double.MaxValue)]));
        Assert.True(result.Accepted);
        var path = result.State!.Paths.First(p => p.DroneId == unit.Id);
        Assert.Equal(new TacticalPoseSnapshot(expected.X, expected.Y, expected.Course), path.Points[^1]);
        var untouched = frame.Units[1];
        var suggestion = frame.Suggestions.First(s => s.DroneId == untouched.Id);
        var endpoint = TacticalGeometry.Integrate(new(untouched.Pose.X, untouched.Pose.Y, untouched.Pose.Course), suggestion.Speed, suggestion.Turn, frame.Rules.Substeps);
        Assert.Equal(new TacticalPoseSnapshot(endpoint.X, endpoint.Y, endpoint.Course), result.State.Paths.First(p => p.DroneId == untouched.Id).Points[^1]);
    }

    [Fact]
    public void ReplaysHaveNoEffectsAndConflictingStaleOrWrongBattleCommandsReject()
    {
        using var engine = Engine();
        var start = new StartTacticalBattleRequest("start", 1);
        var first = engine.StartTacticalBattle(start);
        long calls = Calls(engine);
        Assert.Same(first, engine.StartTacticalBattle(start));
        Assert.Equal(calls, Calls(engine));
        AssertRejectedUnchanged(engine, () => engine.StartTacticalBattle(start with { Seed = 2 }), TacticalBattleRejectionCode.IdentityConflict);
        AssertRejectedUnchanged(engine, () => engine.StartTacticalBattle(new("other", 1)), TacticalBattleRejectionCode.AlreadyActive);
        var frame = first.State!;
        AssertRejectedUnchanged(engine, () => engine.FireTacticalBattle(new("phase", frame.BattleId, frame.Revision)), TacticalBattleRejectionCode.WrongPhase);
        var request = new MoveTacticalBattleRequest("move", frame.BattleId, frame.Revision);
        var moved = engine.MoveTacticalBattle(request);
        calls = Calls(engine);
        Assert.Same(moved, engine.MoveTacticalBattle(request));
        Assert.Equal(calls, Calls(engine));
        AssertRejectedUnchanged(engine, () => engine.MoveTacticalBattle(request with { CommandId = "stale" }), TacticalBattleRejectionCode.StaleRevision);
        AssertRejectedUnchanged(engine, () => engine.MoveTacticalBattle(request with { CommandId = "wrong", BattleId = "other", ExpectedRevision = moved.State!.Revision }), TacticalBattleRejectionCode.WrongBattle);
        AssertRejectedUnchanged(engine, () => engine.MoveTacticalBattle(request with { Orders = [new("Вера", 50, 0)] }), TacticalBattleRejectionCode.IdentityConflict);
        AssertRejectedUnchanged(engine, () => engine.ExitTacticalBattle(new("wrong-exit", "other")), TacticalBattleRejectionCode.WrongBattle);
        var exit = engine.ExitTacticalBattle(new("exit", frame.BattleId));
        Assert.True(exit.Accepted);
        Assert.False(engine.IsTacticalBattleActive);
        Assert.Null(engine.GetTacticalBattleState());
        Assert.Equal(TacticalBattleLogKind.Exited, Assert.Single(exit.NewEvents).Kind);
        Assert.Same(exit, engine.ExitTacticalBattle(new("exit", frame.BattleId)));
        var expired = engine.StartTacticalBattle(start);
        Assert.False(expired.Accepted);
        Assert.Equal(TacticalBattleRejectionCode.ReceiptExpired, expired.Code);
        Assert.Null(expired.State);
        Assert.False(engine.IsTacticalBattleActive);
    }

    [Fact]
    public void FullReceiptsAreBoundedDuringPlayAndReleasedOnExit()
    {
        using var engine = Engine();
        var start = new StartTacticalBattleRequest("start", 1);
        var frame = engine.StartTacticalBattle(start).State!;
        var move = new MoveTacticalBattleRequest("move", frame.BattleId, frame.Revision);
        Assert.True(engine.MoveTacticalBattle(move).Accepted);

        for (int i = 0; i < 16; i++)
            Assert.NotNull(engine.MoveTacticalBattle(move with { CommandId = $"stale-{i}" }).State);

        AssertRejectedUnchanged(engine, () =>
        {
            var expired = engine.MoveTacticalBattle(move);
            Assert.Null(expired.State);
            return expired;
        }, TacticalBattleRejectionCode.ReceiptExpired);
        var latest = move with { CommandId = "stale-15" };
        Assert.Same(engine.MoveTacticalBattle(latest), engine.MoveTacticalBattle(latest));
        Assert.True(engine.ExitTacticalBattle(new("exit", frame.BattleId)).Accepted);
        Assert.Null(engine.MoveTacticalBattle(latest).State);
    }

    [Fact]
    public void RetiredIdentitiesStillRejectReplaysAcrossManyBattles()
    {
        using var engine = Engine();
        for (int i = 0; i < 100; i++)
        {
            string battleId = $"start-{i}";
            Assert.True(engine.StartTacticalBattle(new(battleId, 1)).Accepted);
            Assert.True(engine.ExitTacticalBattle(new($"exit-{i}", battleId)).Accepted);
        }

        for (int i = 0; i < 100; i++)
        {
            var response = engine.StartTacticalBattle(new($"start-{i}", 1));
            Assert.False(response.Accepted);
            Assert.Equal(TacticalBattleRejectionCode.ReceiptExpired, response.Code);
            Assert.Null(response.State);
        }
        Assert.False(engine.IsTacticalBattleActive);
        Assert.Equal(TacticalBattleRejectionCode.IdentityConflict, engine.StartTacticalBattle(new("start-0", 2)).Code);

        Assert.True(engine.StartTacticalBattle(new("current", 1)).Accepted);
        var oldExit = engine.ExitTacticalBattle(new("exit-0", "start-0"));
        Assert.False(oldExit.Accepted);
        Assert.Equal(TacticalBattleRejectionCode.ReceiptExpired, oldExit.Code);
        Assert.Equal("current", engine.GetTacticalBattleState()!.BattleId);
    }

    [Fact]
    public void UnavailableNoBattleAndInvalidIdentityHaveTypedResults()
    {
        using var missing = new SimulationEngine();
        Assert.False(missing.GetTacticalBattleOptions().Availability.Available);
        Assert.Equal(TacticalBattleRejectionCode.Unavailable, missing.StartTacticalBattle(new("s", 1)).Code);
        Assert.Equal(TacticalBattleRejectionCode.NoBattle, missing.MoveTacticalBattle(new("m", "b", 1)).Code);
        Assert.Equal(TacticalBattleRejectionCode.NoBattle, missing.FireTacticalBattle(new("f", "b", 1)).Code);
        Assert.Equal(TacticalBattleRejectionCode.NoBattle, missing.ExitTacticalBattle(new("e", "b")).Code);
        Assert.Equal(TacticalBattleRejectionCode.InvalidIdentity, missing.StartTacticalBattle(new(" ", 1)).Code);
        Assert.Equal(TacticalBattleRejectionCode.InvalidRequest, missing.StartTacticalBattle(null!).Code);
    }

    [Fact]
    public void PlayerSideBAndLongSeedAndLoadoutAreCapturedFromScenarioAndRequest()
    {
        var c = Catalog.Value;
        var scenario = c.Scenarios["initial"];
        c = c with { Scenarios = new([("initial", scenario with { Goal = scenario.Goal! with { PlayerSide = "B" } })]) };
        using var engine = Engine(c);
        var response = engine.StartTacticalBattle(new("s", -1, OwnTacticId: "raid", EnemyTacticId: "dogfight",
            Loadout: [new("Таг-1", "gun.auto", "armor.none", null, 0)]));
        Assert.True(response.Accepted);
        var state = response.State!;
        Assert.Equal("B", state.PlayerSide);
        Assert.Equal(-1, state.Seed);
        Assert.Equal("raid", state.Tactics[1].TacticId);
        Assert.Equal("dogfight", state.Tactics[0].TacticId);
        Assert.Equal(6, state.Suggestions.Length);
        var drone = state.Units.First(u => u.Id == "Таг-1");
        Assert.Equal("gun.auto", drone.Build!.WeaponId);
        Assert.Null(drone.Build.ShieldId);
        Assert.Equal(0, drone.Build.ShieldCount);
    }

    [Fact]
    public void FireValidatesBatchAndMissingTargetWithholdsOwnShotButStillPlansNextTurn()
    {
        using var engine = CloseFireEngine();
        var frame = engine.GetTacticalBattleState()!;
        Assert.Equal(TacticalBattlePhase.Fire, frame.Phase);
        Assert.Empty(frame.Suggestions);
        Assert.NotEmpty(frame.Targets[0].List);
        Assert.Single(frame.Incoming);
        string original = JsonSerializer.Serialize(frame);
        long calls = Calls(engine);
        Assert.Equal(original, JsonSerializer.Serialize(engine.GetTacticalBattleState()));
        Assert.Equal(calls, Calls(engine));
        AssertRejectedUnchanged(engine, () => engine.FireTacticalBattle(new("bad-target", frame.BattleId, frame.Revision, [new("Вера", "Вера")])), TacticalBattleRejectionCode.InvalidTarget);
        AssertRejectedUnchanged(engine, () => engine.FireTacticalBattle(new("bad-drone", frame.BattleId, frame.Revision, [new("Таг-1", "Вера")])), TacticalBattleRejectionCode.InvalidDrone);
        AssertRejectedUnchanged(engine, () => engine.FireTacticalBattle(new("dup", frame.BattleId, frame.Revision, [new("Вера", null), new("Вера", null)])), TacticalBattleRejectionCode.DuplicateDrone);
        AssertRejectedUnchanged(engine, () => engine.FireTacticalBattle(new("null", frame.BattleId, frame.Revision, [null!])), TacticalBattleRejectionCode.InvalidRequest);
        AssertRejectedUnchanged(engine, () => engine.MoveTacticalBattle(new("phase", frame.BattleId, frame.Revision)), TacticalBattleRejectionCode.WrongPhase);
        var fired = engine.FireTacticalBattle(new("fire", frame.BattleId, frame.Revision));
        Assert.True(fired.Accepted);
        Assert.DoesNotContain(fired.State!.Shots, s => s.ShooterId == "Вера");
        Assert.Single(fired.State.Shots);
        Assert.Equal(2, fired.State.Turn);
        Assert.Equal(TacticalBattlePhase.Move, fired.State.Phase);
        Assert.Empty(fired.State.Targets);
        Assert.Empty(fired.State.Incoming);
        Assert.NotEmpty(fired.State.Suggestions);
        Assert.Contains(fired.NewEvents, e => e.Phase == TacticalBattlePhase.Fire && e.Turn == 1);
        Assert.Equal(1, fired.State.Shots[0].Turn);
        Assert.Equal(12, fired.State.BeforeSalvoUnits.Length);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompleteSeedOneThroughEngineApiMatchesIndependentSummary(bool explicitSuggestions)
    {
        using var engine = Engine();
        var frame = engine.StartTacticalBattle(new("s", 1)).State!;
        int command = 0;
        while (frame.Phase != TacticalBattlePhase.Over)
        {
            Assert.True(command < 100, "Engine session failed to reach the published outcome.");
            TacticalBattleResponse response = frame.Phase == TacticalBattlePhase.Move
                ? engine.MoveTacticalBattle(new("m" + command++, frame.BattleId, frame.Revision, explicitSuggestions ? frame.Suggestions : []))
                : engine.FireTacticalBattle(new("f" + command++, frame.BattleId, frame.Revision,
                    frame.Targets.Select(t => new TacticalFireTarget(t.DroneId, t.DefaultTargetId)).ToImmutableArray()));
            Assert.True(response.Accepted, response.Code.ToString());
            frame = response.State!;
        }
        using var source = ReferenceFixtureTests.Open("battles");
        var expected = source.RootElement.GetProperty("data")[0];
        Assert.Equal(TacticalBattleOutcomeCode.A, frame.Outcome!.Kind);
        Assert.Equal(expected.GetProperty("turns").GetInt32(), frame.Outcome.Turns);
        Assert.Equal(expected.GetProperty("shots").GetInt64(), frame.Outcome.Statistics.Shots);
        Assert.Equal(expected.GetProperty("hits").GetInt64(), frame.Outcome.Statistics.Hits);
        Assert.Equal(expected.GetProperty("totalCalls").GetInt64(), frame.Outcome.RandomCalls);
        Assert.Equal(expected.GetProperty("carrierHull")[1].GetInt32(), frame.Units.First(u => u.Id == "Логово").Hull);
        Assert.Equal(new[] { 0, 6 }, frame.Outcome.Losses.Select(l => l.Count));
        Assert.Empty(frame.Suggestions);
        Assert.Empty(frame.Targets);
        Assert.Empty(frame.Incoming);
        Assert.Equal(frame.Log.Length, frame.Log.Select(e => e.Sequence).Distinct().Count());
    }

    private static SimulationEngine CloseFireEngine()
    {
        var engine = Engine();
        var frame = engine.StartTacticalBattle(new("s", 1)).State!;
        var battle = engine.TacticalBattleSessionForTests!.Battle;
        foreach (var d in battle.Drones) d.Status = TacticalDroneStatus.Disabled;
        battle.Drones[0].Status = battle.Drones[4].Status = TacticalDroneStatus.Air;
        battle.Drones[0].Pose = new(0, 0, 0);
        battle.Drones[4].Pose = new(150, 0, 180);
        battle.Carrier("A").Pose = new(-10000, 0, 0);
        battle.Carrier("B").Pose = new(10000, 0, 0);
        var moved = engine.MoveTacticalBattle(new("m", frame.BattleId, frame.Revision, [new("Вера", 50, 0)]));
        Assert.True(moved.Accepted);
        Assert.Equal(TacticalBattlePhase.Fire, moved.State!.Phase);
        return engine;
    }
    private static void AssertRejectedUnchanged(SimulationEngine engine, Func<TacticalBattleResponse> action, TacticalBattleRejectionCode code)
    {
        string before = JsonSerializer.Serialize(engine.GetTacticalBattleState());
        long calls = Calls(engine);
        var result = action();
        Assert.False(result.Accepted);
        Assert.Equal(code, result.Code);
        Assert.Empty(result.NewEvents);
        Assert.Equal(before, JsonSerializer.Serialize(engine.GetTacticalBattleState()));
        Assert.Equal(calls, Calls(engine));
    }
    private static long Calls(SimulationEngine engine) => engine.TacticalBattleSessionForTests!.Battle.Random.Calls;
    private static SimulationEngine Engine(TacticalBattleCatalog? catalog = null)
    {
        var engine = new SimulationEngine(GameDataRegistry.Create([], [], [], [], tacticalBattle: Availability.Available(catalog ?? Catalog.Value)));
        engine.SetSpeed(SimulationSpeed.Speed0);
        return engine;
    }
}
