using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public sealed class TacticalBattleContractTests
{
    [Fact]
    public void InitiativeRoundTripDistinguishesZeroNullAndRange()
    {
        var unit = Frame().Units[0];
        Assert.Equal(0, RoundTrip(unit with { Initiative = 0 }).Initiative);
        Assert.Equal(100, RoundTrip(unit with { Initiative = 100 }).Initiative);
        Assert.Null(RoundTrip(unit).Initiative);
        var option = new TacticalDroneSetupOption("d", "A", "light", "Light", "p", "Pilot",
            new(55, 55, false), new(50, 50, false), 65, "gun", "armor", null, 0, 1);
        foreach (var skill in new TacticalSkillRange[] { new(0, 0, false), new(40, 55, true), new(50, 50, true) })
            Assert.Equal(skill, RoundTrip(option with { Initiative = skill }).Initiative);
        Assert.Null(RoundTrip(option).Initiative);
    }

    [Fact]
    public void OldBattlePayloadWithoutInitiativeRemainsReadable()
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(Frame()))!;
        foreach (var unit in node["Units"]!.AsArray()) unit!.AsObject().Remove("Initiative");
        var restored = JsonSerializer.Deserialize<TacticalBattleSnapshot>(node.ToJsonString())!;
        Assert.All(restored.Units, unit => Assert.Null(unit.Initiative));
        Assert.Equal("battle", restored.BattleId);
        Assert.Equal(TacticalBattlePhase.Over, restored.Phase);
    }

    [Fact]
    public void JournalPreservesTypedFactsAndOldEventPayloadsRemainReadable()
    {
        const string oldEvent = """{"Sequence":7,"Turn":2,"Phase":1,"Kind":10,"TargetId":"d","EngineIndex":1}""";
        var legacy = JsonSerializer.Deserialize<TacticalBattleLogEvent>(oldEvent)!;
        Assert.Equal(TacticalBattleLogKind.EngineDamaged, legacy.Kind);
        Assert.Null(legacy.Shot);
        Assert.Null(legacy.Damage);
        Assert.Null(legacy.Unit);
        double chance = Math.BitIncrement(0.6123456789);
        var fire = new TacticalBattleLogEvent(8, 2, TacticalBattlePhase.Fire, TacticalBattleLogKind.ShotHit,
            ActorId: "e", TargetId: "d", Amount: 21, Shot: new(chance, true));
        var applied = new TacticalBattleLogEvent(9, 2, TacticalBattlePhase.Fire, TacticalBattleLogKind.DamageApplied,
            TargetId: "d", Damage: new(12, 9, 2, 24));
        var system = legacy with { Sequence = 10, Unit = new(TacticalUnitKind.Drone, 24, 60, "Маршевый", 0.5) };
        var frame = Frame() with { Log = [fire, applied, system] };
        var restored = RoundTrip(frame);
        Assert.Equal(frame.Log.AsEnumerable(), restored.Log);
        Assert.Equal(BitConverter.DoubleToInt64Bits(chance), BitConverter.DoubleToInt64Bits(restored.Log[0].Shot!.Chance));
        Assert.Equal(12, (int)TacticalBattleLogKind.Exited);
        Assert.Equal(13, (int)TacticalBattleLogKind.DamageApplied);
    }

    [Fact]
    public void FullSnapshotRoundTripRetainsNestedEquipmentOutcomePathsAndTypedJournal()
    {
        var frame = Frame();
        var restored = RoundTrip(frame);
        Assert.Equal("battle", restored.BattleId);
        Assert.Equal(TacticalBattlePhase.Over, restored.Phase);
        Assert.Equal("B", restored.PlayerSide);
        Assert.Equal(long.MinValue, restored.Seed);
        Assert.Equal("heavy", restored.Units[0].FrameId);
        Assert.Equal("vera", restored.Units[0].Build!.PilotId);
        Assert.Equal(2, restored.Units[0].Build!.ShieldSlots);
        Assert.Equal(0.5, restored.Units[0].Engines[0].Factor);
        Assert.Equal(12, restored.Units[0].Build!.RangeBonus);
        Assert.Equal(760, restored.Units[0].Limits!.Mass);
        Assert.Equal(3, restored.Paths[0].Points.Length);
        Assert.True(restored.Shots[0].Kill);
        Assert.True(restored.Shots[0].Killed);
        Assert.Single(restored.BeforeSalvoUnits);
        Assert.True(restored.FireSkipped);
        Assert.Equal(4, restored.Rules.Substeps);
        Assert.Equal(TacticalOutcomeReasonCode.NoDrones, restored.Outcome!.Reasons[0].Kind);
        Assert.Equal(308, restored.Outcome.RandomCalls);
        Assert.Equal(3, restored.Outcome.Statistics.RoleChanges);
        Assert.Equal("gun.spear.t1", restored.Outcome.Statistics.Weapons[0].WeaponId);
        Assert.Equal(TacticalBattleLogKind.RoleChanged, restored.Log[0].Kind);
        Assert.Equal(TacticalBattlePhase.Move, restored.Log[0].Phase);
        Assert.True(restored.Log[0].NoAttackers);
        Assert.Equal(TacticalBattleLogKind.Outcome, restored.Log[1].Kind);
        Assert.Equal(TacticalBattleOutcomeCode.A, restored.Log[1].Outcome!.Kind);
        Assert.Equal(BitConverter.DoubleToInt64Bits(frame.Units[0].Pose.X), BitConverter.DoubleToInt64Bits(restored.Units[0].Pose.X));
        Assert.Equal(long.MinValue, BitConverter.DoubleToInt64Bits(restored.Units[0].Pose.Y));
    }

    [Fact]
    public void MoveAndFireFramesRetainUnroundedSuggestionsAndExactCandidateInformation()
    {
        double speed = Math.BitIncrement(77.47126436781609);
        var frame = Frame() with { Phase = TacticalBattlePhase.Move, Outcome = null, Suggestions = [new("d", speed, -0.0)] };
        var move = RoundTrip(frame);
        Assert.Equal(BitConverter.DoubleToInt64Bits(speed), BitConverter.DoubleToInt64Bits(move.Suggestions[0].Speed));
        Assert.Equal(long.MinValue, BitConverter.DoubleToInt64Bits(move.Suggestions[0].Turn));
        var fire = RoundTrip(frame with
        {
            Phase = TacticalBattlePhase.Fire,
            Suggestions = [],
            Targets = [new("d", [new("e", 12.3456789012345, -2.3, true, 0.6, 0.812345678901234)], "e")],
            Incoming = [new("e", "d", 0.25, 90, 10, false, 0)]
        });
        Assert.Equal("e", fire.Targets[0].DefaultTargetId);
        Assert.Equal(0.812345678901234, fire.Targets[0].List[0].Chance);
        Assert.Equal("e", fire.Incoming[0].ShooterId);
        Assert.Empty(fire.Suggestions);
    }

    [Fact]
    public void ResponsesCarryOnlyDeltaWhileStateCarriesFullJournal()
    {
        var frame = Frame();
        var response = RoundTrip(new TacticalBattleResponse("cmd", true, TacticalBattleRejectionCode.None,
            TacticalBattlePhase.Over, frame, [frame.Log[^1]]));
        Assert.Equal(2, response.State!.Log.Length);
        Assert.Single(response.NewEvents);
        Assert.Equal(response.State.Log[^1].Sequence, response.NewEvents[0].Sequence);
        var refusal = RoundTrip(new TacticalBattleResponse("stale", false, TacticalBattleRejectionCode.StaleRevision,
            TacticalBattlePhase.Over, frame, []));
        Assert.False(refusal.Accepted);
        Assert.Equal(TacticalBattleRejectionCode.StaleRevision, refusal.Code);
        Assert.Empty(refusal.NewEvents);
    }

    [Fact]
    public void RequestsRetainIdentityPartialOrdersNullTargetsAndLongSeed()
    {
        var start = RoundTrip(new StartTacticalBattleRequest("start", long.MaxValue, "initial", "dogfight", "commander",
            [new("d", "gun", "plate", null, 0)]));
        Assert.Equal(long.MaxValue, start.Seed);
        Assert.Equal("dogfight", start.OwnTacticId);
        Assert.Null(start.Loadout[0].ShieldId);
        var move = RoundTrip(new MoveTacticalBattleRequest("move", "battle", 123, [new("d", 70.12345678901234, -10)]));
        Assert.Equal(123ul, move.ExpectedRevision);
        Assert.Single(move.Orders);
        Assert.Equal("move", move.CommandId);
        var fire = RoundTrip(new FireTacticalBattleRequest("fire", "battle", 124, [new("d", null)]));
        Assert.Null(fire.Targets[0].TargetId);
        Assert.Equal(new ExitTacticalBattleRequest("exit", "battle"), RoundTrip(new ExitTacticalBattleRequest("exit", "battle")));
        Assert.Empty(RoundTrip(new StartTacticalBattleRequest("empty", 1)).Loadout);
        Assert.Empty(RoundTrip(new MoveTacticalBattleRequest("empty", "battle", 1)).Orders);
        Assert.Empty(RoundTrip(new FireTacticalBattleRequest("empty", "battle", 1)).Targets);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("true")]
    [InlineData("123")]
    [InlineData("[null]")]
    public void MalformedExplicitCommandArraysFailInsteadOfBecomingEmptyCommands(string value) =>
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<MoveTacticalBattleRequest>("{\"CommandId\":\"m\",\"BattleId\":\"b\",\"ExpectedRevision\":1,\"Orders\":" + value + "}"));

    [Fact]
    public void SetupOptionsPreserveOrderedIdsRangesPlayerSideAndSlotLimits()
    {
        var options = new TacticalBattleOptions(new(true),
            [new("initial", "Name", "Description", "B", "commander", "dogfight", new("B", 0.8, 0.6, 0.03, [1, 1000]),
                [new("d", "B", "heavy", "Heavy", "p", "Pilot", new(40, 55, true), new(50, 50, false), 65, "g", "a", "s", 2, 2)])],
            [new("g", "Gun", 1, 45, 135, 40, 60, 10, 5, 12, 0.6, 65)],
            [new("a", "Armor", 1, 80, 120)], [new("s", "Shield", 1, 10, 10)],
            [new("commander", "Commander", true), new("dogfight", "Dogfight", false)], "4.0");
        var restored = RoundTrip(options);
        Assert.True(restored.Availability.Available);
        Assert.Equal("B", restored.Scenarios[0].PlayerSide);
        Assert.True(restored.Scenarios[0].Drones[0].Accuracy.IsRandom);
        Assert.Equal(2, restored.Scenarios[0].Drones[0].ShieldSlots);
        Assert.Equal(new[] { "commander", "dogfight" }, restored.Tactics.Select(t => t.Id));
        Assert.Equal(new long[] { 1, 1000 }, restored.Scenarios[0].Goal!.Seeds);
        var unavailable = RoundTrip(new TacticalBattleOptions(new(false, new("rules.json", "version", "Invalid version")), [], [], [], [], []));
        Assert.False(unavailable.Availability.Available);
        Assert.Equal("version", unavailable.Availability.Issue!.Field);
    }

    [Fact]
    public void WorldFlagDefaultsFalseAndImmutableCollectionsDoNotAliasMutableInputs()
    {
        var legacy = JsonSerializer.Deserialize<AuthoritativeSnapshot>("{\"SnapshotSequence\":1,\"GameTimeMs\":0,\"CurrentSpeed\":0,\"Objects\":[]}")!;
        Assert.False(legacy.IsTacticalBattleActive);
        Assert.True(RoundTrip(legacy with { IsTacticalBattleActive = true }).IsTacticalBattleActive);
        var source = new[] { new TacticalDroneOrder("d", 70, 0) };
        var orders = ImmutableArray.CreateRange(source);
        var request = new MoveTacticalBattleRequest("m", "b", 1, orders);
        source[0] = new("changed", 0, 0);
        Assert.Equal("d", request.Orders[0].DroneId);
        var changed = request with { Orders = request.Orders.SetItem(0, new("other", 80, 10)) };
        Assert.Equal("d", request.Orders[0].DroneId);
        Assert.Equal("other", changed.Orders[0].DroneId);
    }

    private static T RoundTrip<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
    private static TacticalBattleSnapshot Frame()
    {
        var build = new TacticalBuildSnapshot("Heavy", "vera", "Вера", "gun.spear.t1", "Spear", 40, 60, 10, 10,
            135, 45, 12, 0.6, "plate.medium", "Plate", "shield.t1", "Shield", 10, 2, 2, 460, 760, 65, 120, 10);
        var unit = new TacticalUnitSnapshot("d", "B", TacticalUnitKind.Drone, "heavy", new(1.123456789012345, -0.0, 179.99),
            60, 64, 65, 70, 80, TacticalUnitState.Air, "fighter", true,
            [new("engine", "Engine", 0.5, 100, 25, 10, 32.5)], new(760, 1, 50, 100, 20, 60), 12, build, 55, 52);
        var losses = ImmutableArray.Create(new TacticalLossCount("A", 0), new TacticalLossCount("B", 6));
        var outcome = new TacticalOutcomeSnapshot(TacticalBattleOutcomeCode.A, [new(TacticalOutcomeReasonCode.NoDrones, "carrier")],
            19, losses, new(34, 17, 3, 3, 0, 1, 100, 200, 0, "B", 3, [new("gun.spear.t1", 10, 8, 2, 2, 60)]), 308);
        return new("battle", 9, TacticalBattlePhase.Over, 19, "B", long.MinValue, "initial", "Name", [new("A", "dogfight"), new("B", "commander")],
            [unit], [], [new(19, "d", [new(0, 0, 0), new(1, 2, 3), unit.Pose])], [], [],
            [new(19, "d", "e", unit.Pose, new(100, 100, 90), 0.812345678901234, true, 60, true, true, true)], [unit], true,
            new(new(50, 5, 95, 15, 12, 10, 30), 0.5, 45, 4), losses, outcome,
            [new(1, 3, TacticalBattlePhase.Move, TacticalBattleLogKind.RoleChanged, "d", RoleId: "fighter", NoAttackers: true),
                new(2, 19, TacticalBattlePhase.Over, TacticalBattleLogKind.Outcome, Outcome: outcome)]);
    }
}
