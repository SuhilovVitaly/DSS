using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0018TK0002Tests
{
    internal const string Player = "SPC-0001", Enemy = "SPC-0002", Launcher = "MOD-PLAYER-TORPEDO-01";
    private static readonly string Root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/DeepSpaceSaga.Client"));
    internal static GameDataRegistry Registry() => EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(Root, "Settings.json"), out _, out _);
    internal static SimulationEngine Create(ulong seed = 42)
    {
        var source = ScenarioLoader.LoadFromFile(Path.Combine(Root, "Scenarios/PlayerShipOnly/scenario.json"));
        var engine = new SimulationEngine(Registry(), clock: new SimulationClock(SimulationSpeed.Speed0, () => 0));
        engine.LoadScenario(source with
        {
            GameState = source.GameState with
            {
                MasterSeed = seed,
                CurrentSpeed = "Speed0",
                SpaceObjects = source.GameState.SpaceObjects.Select(o => o with
                {
                    PositionX = o.ObjectId == Player ? 0 : 600,
                    PositionY = 0,
                    SpeedMps = 0,
                    MovementType = "Stationary",
                    Ai = null,
                    Modules = o.Modules!.Select(m => m with { PowerState = o.ObjectId == Player ? "On" : "Off" }).ToArray()
                }).ToArray()
            }
        });
        return engine;
    }
    internal static AuthoritativeSnapshot At(SimulationEngine engine, long time = 0) => engine.CaptureSnapshotForTests(time * 300, SimulationSpeed.Speed0, time);
    internal static PlayerCommand Launch(string id = "point", string profile = "slow") =>
        new(id, 1, Player, Launcher, CombatCommandTypes.Launch, TargetWorldX: 600, TargetWorldY: 0, ProfileId: profile);

    [Theory]
    [InlineData("torpedo.fire")]
    [InlineData("torpedo.selfDestruct")]
    [InlineData("torpedo.setMode")]
    [InlineData("defense.fire")]
    [InlineData("defense.enable")]
    [InlineData("defense.disable")]
    public void Legacy_commands_are_rejected_and_profiles_are_captured(string retired)
    {
        using var engine = Create(); var before = engine.CaptureLaunchScatterState();
        engine.ReceiveCommand(new("retired", 1, Player, Launcher, retired, TargetObjectId: Enemy, ModeId: "overheat", ProfileId: "fast"));
        var state = At(engine); var result = Assert.Single(state.CommandResults);
        Assert.Equal(CommandResultStatus.Rejected, result.Status); Assert.Equal(CommandReasonCodes.UnknownCommandType, result.ReasonCode);
        Assert.Equal(before, engine.CaptureLaunchScatterState()); Assert.Equal(0, state.MotionTimeMs);
        Assert.DoesNotContain(state.Objects, o => o.OwnProjectile is not null);
        var launcher = state.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!;
        Assert.Null(launcher.ReadyAtMotionTimeMs); Assert.Equal(new[] { "slow", "medium", "fast" }, launcher.Profiles!.Value.Select(p => p.ProfileId));
    }

    [Theory]
    [InlineData("slow", 6d, 9d)]
    [InlineData("medium", 10d, 5d)]
    [InlineData("fast", 16d, 2.5d)]
    public void Point_launch_captures_profile_fuse_and_fixed_reload_without_a_standard_mode(string profile, double speed, double radius)
    {
        using var engine = Create(); engine.ReceiveCommand(Launch(profile: profile)); var state = At(engine);
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(state.CommandResults).Status);
        var flight = Assert.Single(state.Objects, o => o.OwnProjectile is not null).OwnProjectile!;
        Assert.Equal(profile, flight.ProfileId); Assert.Equal(speed, flight.SpeedKmS); Assert.Equal(radius, flight.BlastRadiusKm);
        Assert.Equal(200, flight.Damage); Assert.Equal(20m, flight.ArmorPiercingPercent);
        double duration = double.Hypot(flight.DetonationX - flight.StartX, flight.DetonationY - flight.StartY) / (speed * 10) * 1000;
        Assert.Equal(duration, flight.DetonationMotionTimeMs, 8);
        var launcher = state.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!;
        Assert.Empty(launcher.ModeId); Assert.False(launcher.IsBrokenUntilDock); Assert.Equal(10000, launcher.ReadyAtMotionTimeMs);
        Assert.Empty(Registry().FireModes);
        var save = engine.CaptureSaveState();
        using var restored = new SimulationEngine(Registry()); restored.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true), true);
        Assert.Equal(JsonSerializer.Serialize(flight), JsonSerializer.Serialize(Assert.Single(At(restored).Objects, o => o.OwnProjectile is not null).OwnProjectile));
        Assert.Empty(At(restored).InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ModeId);
        Assert.Equal(engine.CaptureLaunchScatterState(), restored.CaptureLaunchScatterState());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Point_modules_reject_retired_modes_even_when_rules_are_omitted(bool omitRules)
    {
        var source = Registry();
        static IEnumerable<T> All<T>(TypeRegistry<T> types) where T : ITypeDefinition => Enumerable.Range(0, types.Count).Select(types.GetDefinition);
        var error = Assert.Throws<ContentException>(() => GameDataRegistry.Create(All(source.ModuleCategories), All(source.ModuleTypes), All(source.ItemTypes),
            All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes), All(source.Dialogues), All(source.Quests),
            stationMarketProfiles: All(source.StationMarketProfiles), shipClasses: All(source.ShipClasses), stationMarketEvents: All(source.StationMarketEvents),
            combatRules: omitRules ? null : source.CombatRules, fireModes: [new("standard", "Standard", 0, 0, 0, 1, ["module.torpedo.launcher", "module.countermeasure.launcher"])]));
        Assert.Contains("point", error.Message, StringComparison.OrdinalIgnoreCase);
    }
}
