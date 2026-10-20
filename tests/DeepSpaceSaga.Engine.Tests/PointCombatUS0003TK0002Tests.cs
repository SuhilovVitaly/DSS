using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Combat;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0003TK0002Tests
{
    internal const string Player = TorpedoImpactTests.Player, Target = TorpedoImpactTests.Target, Launcher = TorpedoImpactTests.Launcher;
    private static IEnumerable<T> All<T>(TypeRegistry<T> registry) where T : ITypeDefinition =>
        Enumerable.Range(0, registry.Count).Select(registry.GetDefinition);

    internal static GameDataRegistry Registry(double scatter = 0.02)
    {
        var source = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(TorpedoImpactTests.ClientRoot, "Settings.json"), out _, out _);
        return GameDataRegistry.Create(
            All(source.ModuleCategories).Select(c => c.TypeId == "module.torpedo.launcher" ? c with { CommandTypeIds = [CombatCommandTypes.Launch] } : c),
            All(source.ModuleTypes).Select(m => m.CategoryTypeId != "module.torpedo.launcher" ? m : m with
            {
                CommandTypeIds = [CombatCommandTypes.Launch],
                TorpedoManeuverability = null,
                TorpedoRangeKm = null,
                TorpedoSpeedKmS = null,
                TorpedoTurnRateDegPerSec = null,
                Accuracy = null,
                LauncherReloadMs = null,
                TorpedoMaxFlightTimeMs = null,
                TorpedoDamage = 200,
                ArmorPiercingPercent = 20,
                ArmorDamagePercent = 100,
                RangeKm = 70,
                ReloadMs = 10000,
                ScatterPercent = scatter,
                Profiles = [new("slow", 6, 9), new("medium", 10, 5), new("fast", 16, 2.5)]
            }),
            All(source.ItemTypes), All(source.CommandDefinitions).Append(new(CombatCommandTypes.Launch, "Launch", Target: "point", Type: "module.torpedo.launcher")),
            All(source.FactoryTypes), All(source.Recipes), All(source.Dialogues), All(source.Quests),
            stationMarketProfiles: All(source.StationMarketProfiles), shipClasses: All(source.ShipClasses),
            stationMarketEvents: All(source.StationMarketEvents), combatRules: source.CombatRules! with
            { Chance = null, Blast = new(0.3), Scatter = new(15, 0.0333, 0.3), Npc = new(1400, 30, 200), CombatStartRangeKm = 100 });
    }

    internal static SimulationEngine Create(Func<SpaceObjectData, SpaceObjectData>? configure = null, double scatter = 0.02, ulong seed = 42)
    {
        var scenario = TorpedoImpactTests.Scenario(o =>
        {
            var value = o with { Ai = null };
            return configure?.Invoke(value) ?? value;
        });
        var engine = new SimulationEngine(Registry(scatter));
        engine.LoadScenario(scenario with { GameState = scenario.GameState with { MasterSeed = seed } });
        return engine;
    }

    internal static PlayerCommand Launch(string id = "launch", double x = 700, double y = 0, string? profile = "slow") =>
        new(id, 1, Player, Launcher, CombatCommandTypes.Launch, TargetWorldX: x, TargetWorldY: y, ProfileId: profile);

    internal static ObjectMotionSnapshot Flight(AuthoritativeSnapshot snapshot) => Assert.Single(snapshot.Objects, o => o.OwnProjectile is not null);

    [Theory]
    [InlineData("ready", null)]
    [InlineData("range", CommandReasonCodes.LaunchOutOfRange)]
    [InlineData("profile", CommandReasonCodes.UnknownProfile)]
    [InlineData("missing", CommandReasonCodes.MissingProfile)]
    [InlineData("coordinates", CommandReasonCodes.InvalidTargetCoordinates)]
    [InlineData("operator", CommandReasonCodes.NoWeaponOperator)]
    [InlineData("off", CommandReasonCodes.ModuleUnavailable)]
    [InlineData("disabled", CommandReasonCodes.ModuleUnavailable)]
    [InlineData("hit-disabled", CommandReasonCodes.ModuleDisabled)]
    [InlineData("destroyed", CommandReasonCodes.ModuleDestroyed)]
    public void Point_launch_validates_readiness_operator_profile_range(string kind, string? reason)
    {
        using var engine = Create(o => o.ObjectId != Player ? o : o with
        {
            Modules = o.Modules!.Select(m => m.ModuleId != Launcher ? m : m with
            {
                OperatorCrewId = kind == "operator" ? null : m.OperatorCrewId,
                PowerState = kind == "off" ? "Off" : m.PowerState,
                OperationalState = kind == "disabled" ? "Disabled" : m.OperationalState,
                DisabledUntilMotionTimeMs = kind == "hit-disabled" ? 1000 : m.DisabledUntilMotionTimeMs,
                StructurePoints = kind == "destroyed" ? 0 : m.StructurePoints,
                Destroyed = kind == "destroyed" || m.Destroyed
            }).ToArray()
        });
        var randomBefore = engine.CaptureLaunchScatterState();
        var before = engine.CaptureSnapshotForTests();
        var command = Launch(x: kind == "range" ? 700.000001 : kind == "coordinates" ? double.NaN : 700,
            profile: kind == "profile" ? "unknown" : kind == "missing" ? null : "slow");
        engine.ReceiveCommand(command);
        var after = engine.CaptureSnapshotForTests();
        var receipt = Assert.Single(after.CommandResults);
        Assert.Equal(reason, receipt.ReasonCode);
        Assert.Equal(reason is null ? CommandResultStatus.Executed : CommandResultStatus.Rejected, receipt.Status);
        if (reason is not null)
        {
            Assert.DoesNotContain(after.Objects, o => o.OwnProjectile is not null);
            Assert.Equal(randomBefore, engine.CaptureLaunchScatterState());
            Assert.Equal(JsonSerializer.Serialize(before.InstalledModules), JsonSerializer.Serialize(after.InstalledModules));
        }
        else
        {
            Assert.Equal(randomBefore.Counter + 2, engine.CaptureLaunchScatterState().Counter);
            Assert.Equal(200, Flight(after).OwnProjectile!.Damage);
        }
    }

    [Fact]
    public void Launch_is_seeded_idempotent_and_captures_parameters()
    {
        using var first = Create();
        using var second = Create();
        foreach (var engine in new[] { first, second }) { engine.ReceiveCommand(Launch()); engine.CaptureSnapshotForTests(); }
        var initial = Flight(first.CaptureSnapshotForTests());
        Assert.Equal(JsonSerializer.Serialize(initial), JsonSerializer.Serialize(Flight(second.CaptureSnapshotForTests())));
        var random = first.CaptureLaunchScatterState();
        first.ReceiveCommand(Launch());
        Assert.Equal(initial, Flight(first.CaptureSnapshotForTests()));
        Assert.Equal(random, first.CaptureLaunchScatterState());
        first.ReceiveCommand(Launch("reload"));
        var rejected = first.CaptureSnapshotForTests();
        Assert.Equal(CommandReasonCodes.LauncherReloading, Assert.Single(rejected.CommandResults).ReasonCode);
        Assert.Equal(random, first.CaptureLaunchScatterState());
        var later = Flight(first.CaptureSnapshotForTests(4000, simulationTimeMs: 1000));
        var captured = later.OwnProjectile!;
        Assert.Equal(initial.OwnProjectile! with { X = captured.X, Y = captured.Y }, captured);
        Assert.Equal(6, captured.SpeedKmS);
        Assert.Equal(9, captured.BlastRadiusKm);
        Assert.Equal(20, captured.ArmorPiercingPercent);
        double movedKm = Math.Sqrt(later.X * later.X + later.Y * later.Y) / 10;
        Assert.Equal(6, movedKm, 9);
        Assert.Equal((later.X, later.Y), (captured.X, captured.Y));
        Assert.Null(later.Torpedo);
    }

    [Fact]
    public void Rejected_commands_do_not_queue_and_reload_boundary_is_inclusive()
    {
        using var engine = Create(scatter: 0);
        engine.ReceiveCommand(Launch());
        engine.CaptureSnapshotForTests();
        engine.ReceiveCommand(Launch("too-soon", profile: "fast"));
        Assert.Equal(CommandReasonCodes.LauncherReloading, Assert.Single(engine.CaptureSnapshotForTests().CommandResults).ReasonCode);
        Assert.Single(engine.CaptureSnapshotForTests(39996, simulationTimeMs: 9999).Objects, o => o.OwnProjectile is not null);
        var ready = engine.CaptureSnapshotForTests(40000, simulationTimeMs: 10000);
        Assert.Single(ready.Objects, o => o.OwnProjectile is not null);
        engine.ReceiveCommand(Launch("boundary", profile: "fast"));
        var launched = engine.CaptureSnapshotForTests(40000, simulationTimeMs: 10000);
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(launched.CommandResults).Status);
        Assert.Equal(2, launched.Objects.Count(o => o.OwnProjectile is not null));
        Assert.Equal(20000, launched.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ReadyAtMotionTimeMs);
    }

    [Fact]
    public void Player_and_npc_use_same_executor_and_foreign_projection_hides_captured_geometry()
    {
        using var engine = Create(o => o.ObjectId != Target ? o : o with
        {
            Modules = o.Modules!.Select(m => m.ModuleId == "MOD-PIRATE-TORPEDO-01" ? m with { PowerState = "On" } : m).ToArray()
        }, scatter: 0);
        var result = engine.TryStartHostilePointLaunch(Target, "MOD-PIRATE-TORPEDO-01", 0, 0, "fast", 0);
        Assert.Equal(CommandStartDisposition.Started, result.Disposition);
        var snapshot = engine.CaptureSnapshotForTests();
        var foreign = Assert.Single(snapshot.Objects, o => o.ObservedProjectile is not null);
        Assert.Null(foreign.OwnProjectile);
        Assert.Null(foreign.Torpedo);
        Assert.Equal(16, foreign.ObservedProjectile!.SpeedKmS);
        Assert.Equal(2.5, foreign.ObservedProjectile.BlastRadiusKm);
        Assert.Empty(snapshot.CommandResults);
        var json = JsonSerializer.Serialize(foreign);
        Assert.DoesNotContain("Detonation", json);
        Assert.DoesNotContain("StartX", json);
        Assert.DoesNotContain("LauncherModuleId", json);
        var entry = Assert.Single(snapshot.CombatJournal, e => e.Type == CombatEventType.Launch);
        Assert.Null(entry.LauncherModuleId);
        Assert.Null(entry.Attack);
        Assert.Null(entry.Damage);
        var later = Assert.Single(engine.CaptureSnapshotForTests(400, simulationTimeMs: 100).Objects, o => o.ObservedProjectile is not null);
        Assert.Equal(16, later.Y - foreign.Y, 10);
    }

    [Fact]
    public void Scatter_normal_pair_has_fixed_sequence_restorable_state_and_distribution()
    {
        var random = new LaunchScatterRng(0, 0);
        var first = random.NextPair();
        Assert.Equal(-0.45275774021745824, first.X, 14);
        Assert.Equal(0.20776603893419182, first.Y, 14);
        Assert.Equal(4354685564936845354UL, random.State);
        Assert.Equal(2UL, random.Counter);
        var restored = new LaunchScatterRng(random.State, random.Counter);
        Assert.Equal(random.NextPair(), restored.NextPair());
        double sumX = 0, sumY = 0, squaresX = 0, squaresY = 0, product = 0;
        for (int i = 0; i < 10000; i++)
        {
            var pair = random.NextPair();
            sumX += pair.X; sumY += pair.Y; squaresX += pair.X * pair.X; squaresY += pair.Y * pair.Y; product += pair.X * pair.Y;
        }
        Assert.InRange(sumX / 10000, -0.04, 0.04);
        Assert.InRange(sumY / 10000, -0.04, 0.04);
        Assert.InRange(squaresX / 10000, 0.94, 1.06);
        Assert.InRange(squaresY / 10000, 0.94, 1.06);
        Assert.InRange(product / 10000, -0.04, 0.04);
        Assert.Equal(20004UL, random.Counter);
    }

    [Fact]
    public void Scatter_endpoint_is_not_clamped_back_to_range_and_pause_does_not_advance_flight()
    {
        bool sawBeyondRange = false;
        for (ulong seed = 0; seed < 10; seed++)
        {
            using var engine = Create(seed: seed);
            engine.ReceiveCommand(Launch());
            var flight = Flight(engine.CaptureSnapshotForTests());
            var own = flight.OwnProjectile!;
            double distance = Math.Sqrt(own.DetonationX * own.DetonationX + own.DetonationY * own.DetonationY) / 10;
            sawBeyondRange |= distance > 70;
            Assert.Equal(distance / 6 * 1000, own.DetonationMotionTimeMs, 9);
            Assert.Equal(flight, Flight(engine.CaptureSnapshotForTests(1000000, SimulationSpeed.Speed0, 0)));
        }
        Assert.True(sawBeyondRange);
    }
}
