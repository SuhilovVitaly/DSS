using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0003TK0002Tests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0004TK0001Tests
{
    internal const string Defense = "MOD-PLAYER-COUNTERMEASURE-01", Defense2 = "MOD-PLAYER-COUNTERMEASURE-02";
    private static IEnumerable<T> All<T>(TypeRegistry<T> registry) where T : ITypeDefinition =>
        Enumerable.Range(0, registry.Count).Select(registry.GetDefinition);

    internal static GameDataRegistry CounterRegistry(double scatter = 0.08, double torpedoScatter = 0.02)
    {
        var source = Registry(torpedoScatter);
        return GameDataRegistry.Create(All(source.ModuleCategories).Select(c => c.TypeId == "module.countermeasure.launcher"
                ? c with { CommandTypeIds = [DefenseCommandTypes.Launch] } : c),
            All(source.ModuleTypes).Select(m => m.CategoryTypeId != "module.countermeasure.launcher" ? m : m with
            {
                CommandTypeIds = [DefenseCommandTypes.Launch],
                Profiles = [new("wide", 15, 1), new("fast", 25, 0.5)],
                RangeKm = 30,
                ReloadMs = 12500,
                ScatterPercent = scatter,
                CountermeasureAccuracy = null,
                CountermeasureRangeKm = null,
                CountermeasureManualRangeKm = null,
                CountermeasureSpeedKmS = null,
                CountermeasureTurnRateDegPerSec = null,
                CountermeasureReloadMs = null,
                CountermeasureMaxFlightTimeMs = null,
                Barrels = null,
                BurstIntervalMs = null
            }), All(source.ItemTypes), All(source.CommandDefinitions)
                .Append(new(DefenseCommandTypes.Launch, "Launch countermissile", Target: "point", Type: "module.countermeasure.launcher")),
            All(source.FactoryTypes), All(source.Recipes), All(source.Dialogues), All(source.Quests),
            stationMarketProfiles: All(source.StationMarketProfiles), shipClasses: All(source.ShipClasses),
            stationMarketEvents: All(source.StationMarketEvents), combatRules: source.CombatRules);
    }

    internal static SimulationEngine CreateCounter(Func<SpaceObjectData, SpaceObjectData>? configure = null,
        double scatter = 0.08, double torpedoScatter = 0.02)
    {
        var scenario = TorpedoImpactTests.Scenario(o =>
        {
            var value = o with { Ai = null, Crew = o.Crew?.Select(c => c with { AccuracySkill = 15 }).ToArray() };
            return configure?.Invoke(value) ?? value;
        });
        var engine = new SimulationEngine(CounterRegistry(scatter, torpedoScatter));
        engine.LoadScenario(scenario);
        return engine;
    }

    internal static PlayerCommand CounterLaunch(string id = "counter", double x = 300, string? profile = "wide", string module = Defense) =>
        new(id, 1, Player, module, DefenseCommandTypes.Launch, TargetWorldX: x, TargetWorldY: 0, ProfileId: profile);
    internal static AuthoritativeSnapshot At(SimulationEngine engine, long time = 0) => engine.CaptureSnapshotForTests(time * 4, simulationTimeMs: time);

    [Theory]
    [InlineData("wide", 15, 1)]
    [InlineData("fast", 25, 0.5)]
    public void Countermissile_launch_uses_shared_executor(string profile, double speed, double radius)
    {
        using var engine = CreateCounter();
        var random = engine.CaptureLaunchScatterState();
        engine.ReceiveCommand(CounterLaunch(profile: profile));
        var snapshot = At(engine);
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(snapshot.CommandResults).Status);
        var projectile = Flight(snapshot);
        var flight = projectile.OwnProjectile!;
        Assert.Equal("countermeasure-1", projectile.ObjectId);
        Assert.Equal(SpaceObjectType.Countermeasure, projectile.ObjectType);
        Assert.Null(projectile.Countermeasure);
        Assert.Null(projectile.Torpedo);
        Assert.Equal(ProjectileKind.Countermissile, flight.Kind);
        Assert.Equal(profile, flight.ProfileId);
        Assert.Equal(speed, flight.SpeedKmS);
        Assert.Equal(radius, flight.BlastRadiusKm);
        Assert.Equal(0, flight.Damage);
        Assert.Equal(0m, flight.ArmorPiercingPercent);
        Assert.Equal(random.Counter + 2, engine.CaptureLaunchScatterState().Counter);
        var module = snapshot.InstalledModules.Single(m => m.ModuleId == Defense);
        var defense = module.Defense!;
        Assert.False(defense.AutoEnabled);
        Assert.Equal(DefenseState.Reloading, defense.State);
        Assert.Equal(12500, defense.ReadyAtMotionTimeMs);
        Assert.Equal(12500, defense.ReloadDueMotionTimeMs);
        Assert.Equal(12500, defense.ReloadDurationMs);
        Assert.Equal(30, defense.RangeKm);
        Assert.Equal(0.08, defense.ScatterPercent);
        Assert.Equal(new[] { "wide", "fast" }, defense.Profiles!.Value.Select(p => p.ProfileId));
        Assert.Empty(defense.PendingBurstShots!.Value);
        Assert.Null(module.DefensePreview);
        Assert.Equal(15, module.Operator!.Skill);
        Assert.Equal(projectile, Flight(At(engine)));
        var later = Flight(At(engine, 100));
        Assert.Equal(speed / 10, Math.Sqrt(later.X * later.X + later.Y * later.Y) / 10, 10);
    }

    [Theory]
    [InlineData("range", CommandReasonCodes.LaunchOutOfRange)]
    [InlineData("profile", CommandReasonCodes.UnknownProfile)]
    [InlineData("missing", CommandReasonCodes.MissingProfile)]
    [InlineData("operator", CommandReasonCodes.NoWeaponOperator)]
    [InlineData("off", CommandReasonCodes.ModuleUnavailable)]
    [InlineData("disabled", CommandReasonCodes.ModuleDisabled)]
    [InlineData("destroyed", CommandReasonCodes.ModuleDestroyed)]
    public void Countermissile_rejects_disabled_destroyed_or_unstaffed_launcher(string kind, string expected)
    {
        using var engine = CreateCounter(o => o.ObjectId != Player ? o : o with
        {
            Modules = o.Modules!.Select(m => m.ModuleId != Defense ? m : m with
            {
                OperatorCrewId = kind == "operator" ? null : m.OperatorCrewId,
                PowerState = kind == "off" ? "Off" : m.PowerState,
                DisabledUntilMotionTimeMs = kind == "disabled" ? 1000 : m.DisabledUntilMotionTimeMs,
                StructurePoints = kind == "destroyed" ? 0 : m.StructurePoints,
                Destroyed = kind == "destroyed" || m.Destroyed
            }).ToArray()
        });
        var before = At(engine);
        var random = engine.CaptureLaunchScatterState();
        engine.ReceiveCommand(CounterLaunch(x: kind == "range" ? 300.000001 : 300,
            profile: kind == "profile" ? "unknown" : kind == "missing" ? null : "wide"));
        var after = At(engine);
        Assert.Equal(expected, Assert.Single(after.CommandResults).ReasonCode);
        Assert.Equal(random, engine.CaptureLaunchScatterState());
        Assert.Equal(JsonSerializer.Serialize(before.Objects), JsonSerializer.Serialize(after.Objects));
        Assert.Equal(JsonSerializer.Serialize(before.InstalledModules), JsonSerializer.Serialize(after.InstalledModules));
    }

    [Fact]
    public void Torpedo_and_countermissiles_share_one_named_scatter_stream_and_replay_is_idempotent()
    {
        using var first = CreateCounter();
        using var second = CreateCounter();
        first.ReceiveCommand(CounterLaunch()); At(first);
        var once = first.CaptureLaunchScatterState();
        first.ReceiveCommand(CounterLaunch()); At(first);
        Assert.Equal(once, first.CaptureLaunchScatterState());
        first.ReceiveCommand(Launch());
        Assert.Equal(2, At(first).Objects.Count(o => o.OwnProjectile is not null));
        second.ReceiveCommand(CounterLaunch()); At(second);
        second.ReceiveCommand(CounterLaunch("second", module: Defense2)); At(second);
        Assert.Equal(first.CaptureLaunchScatterState(), second.CaptureLaunchScatterState());
        Assert.Equal(4UL, first.CaptureLaunchScatterState().Counter);
    }

    [Fact]
    public void Reload_begins_at_launch_and_legacy_auto_enable_cannot_activate_point_defense()
    {
        using var engine = CreateCounter(scatter: 0);
        engine.ReceiveCommand(CounterLaunch()); At(engine);
        engine.ReceiveCommand(CounterLaunch("too-soon"));
        Assert.Equal(CommandReasonCodes.LauncherReloading, Assert.Single(At(engine).CommandResults).ReasonCode);
        var before = At(engine);
        engine.ReceiveCommand(new("legacy-enable", 2, Player, Defense, DefenseCommandTypes.Enable));
        var rejected = At(engine);
        Assert.Equal(CommandReasonCodes.UnknownCommandType, Assert.Single(rejected.CommandResults).ReasonCode);
        Assert.Equal(JsonSerializer.Serialize(before.InstalledModules), JsonSerializer.Serialize(rejected.InstalledModules));
        Assert.Equal(DefenseState.Reloading, At(engine, 12499).InstalledModules.Single(m => m.ModuleId == Defense).Defense!.State);
        Assert.Equal(DefenseState.Ready, At(engine, 12500).InstalledModules.Single(m => m.ModuleId == Defense).Defense!.State);
        engine.ReceiveCommand(CounterLaunch("boundary"));
        var after = At(engine, 12500);
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(after.CommandResults).Status);
        Assert.Equal(25000, after.InstalledModules.Single(m => m.ModuleId == Defense).Defense!.ReadyAtMotionTimeMs);
    }
}
