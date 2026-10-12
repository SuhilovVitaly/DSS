using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;
using static DeepSpaceSaga.Engine.Tests.ManualCountermeasureFireTests;

namespace DeepSpaceSaga.Engine.Tests;

public class DefensePreviewProjectionTests
{
    private static DefenseLaunchPreview Preview(AuthoritativeSnapshot snapshot, string module = Defense1) =>
        Assert.IsType<DefenseLaunchPreview>(snapshot.InstalledModules.Single(m => m.ModuleId == module).DefensePreview);

    [Fact]
    public void Distinct_module_accuracies_produce_distinct_previews_without_rng_or_attempt()
    {
        var source = Registry.Value;
        static IEnumerable<T> All<T>(TypeRegistry<T> r) where T : ITypeDefinition => Enumerable.Range(0, r.Count).Select(r.GetDefinition);
        var variant = All(source.ModuleTypes).Single(m => m.TypeId == "module.countermeasure.launcher.basic") with
        { TypeId = "module.countermeasure.launcher.preview", CountermeasureAccuracy = 75.25m };
        var registry = GameDataRegistry.Create(All(source.ModuleCategories), All(source.ModuleTypes).Append(variant),
            All(source.ItemTypes), All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes),
            All(source.Dialogues), All(source.Quests), stationMarketProfiles: All(source.StationMarketProfiles),
            shipClasses: All(source.ShipClasses), stationMarketEvents: All(source.StationMarketEvents),
            combatRules: source.CombatRules, fireModes: source.FireModes);
        var scenario = HostileTorpedoFireTests.HostileScenario();
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o =>
            o with { Modules = o.Modules?.Select(m => m.ModuleId == Defense2 ? m with { ModuleTypeId = variant.TypeId } : m).ToArray() }).ToArray()
            }
        };
        using var engine = new SimulationEngine(registry);
        engine.LoadScenario(scenario);
        string target = LaunchEnemy(engine);
        engine.SetObjectInteractionState(null, target);
        for (int i = 0; i < 3; i++)
        {
            var snapshot = At(engine, 1, 9000000);
            Assert.Equal(500, Preview(snapshot).ChanceTenths);
            Assert.Equal(703, Preview(snapshot, Defense2).ChanceTenths);
            Assert.Equal(55m, Preview(snapshot).Attack);
            Assert.Equal(5m, Preview(snapshot).Defense);
            Assert.Equal(0m, Preview(snapshot).BaseChance);
            Assert.Equal(75.25m, Preview(snapshot, Defense2).Attack);
            Assert.True(Preview(snapshot).CanFire);
            Assert.True(Preview(snapshot, Defense2).CanFire);
            Assert.Equal(target, Preview(snapshot).TargetTorpedoId);
            Assert.False(snapshot.Objects.Single(o => o.ObjectId == target).CountermeasureAttempted);
            Assert.DoesNotContain(snapshot.Objects, o => o.Countermeasure is not null);
        }
        var defense = engine.CaptureSaveStateForTests(1, SimulationSpeed.Speed0).GameState.DefenseState!;
        Assert.Equal(0ul, defense.RngCounter);
        Assert.Equal(0, defense.ProjectileSequence);
        engine.ReceiveCommand(Manual("fire-second", target, Defense2));
        Assert.Equal(703, Assert.Single(At(engine, 1).Objects, o => o.Countermeasure is not null).Countermeasure!.FrozenChanceTenths);
    }

    [Fact]
    public void Preview_tracks_selection_and_does_not_use_hover_or_keep_stale_target()
    {
        using var engine = Create(HostileTorpedoFireTests.HostileScenario());
        string target = LaunchEnemy(engine);
        engine.SetObjectInteractionState(target, null);
        Assert.Null(Preview(At(engine, 1)).TargetTorpedoId);
        engine.SetObjectInteractionState(null, target);
        Assert.True(Preview(At(engine, 1)).CanFire);
        engine.ReceiveCommand(Fire("own"));
        string own = At(engine, 1).Objects.Single(o => o.Torpedo?.OwnerObjectId == Player).ObjectId;
        engine.SetObjectInteractionState(target, own);
        var preview = Preview(At(engine, 1));
        Assert.Equal(own, preview.TargetTorpedoId);
        Assert.Null(preview.ChanceTenths);
        Assert.Equal("target_not_hostile", preview.Reason);
        engine.SetObjectInteractionState(target, Target);
        Assert.Null(Preview(At(engine, 1)).ChanceTenths);
        Assert.False(Preview(At(engine, 1)).CanFire);
        engine.SetObjectInteractionState(null, "missing");
        Assert.Null(Preview(At(engine, 1)).TargetTorpedoId);
    }

    [Theory]
    [InlineData("off", CommandReasonCodes.ModuleUnavailable)]
    [InlineData("broken", CommandReasonCodes.ModuleUnavailable)]
    [InlineData("operator", "no_weapon_operator")]
    public void Preview_and_execution_share_unready_reasons(string cause, string expected)
    {
        var scenario = HostileTorpedoFireTests.HostileScenario();
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o =>
            o with
            {
                Modules = o.Modules?.Select(m => m.ModuleId != Defense1 ? m : m with
                {
                    PowerState = cause == "off" ? "Off" : m.PowerState,
                    StructurePoints = cause == "broken" ? 0 : m.StructurePoints,
                    OperatorCrewId = cause == "operator" ? null : m.OperatorCrewId
                }).ToArray()
            }).ToArray()
            }
        };
        using var engine = Create(scenario);
        string target = LaunchEnemy(engine);
        engine.SetObjectInteractionState(null, target);
        var preview = Preview(At(engine, 1));
        Assert.Equal(expected, preview.Reason);
        Assert.False(preview.CanFire);
        Assert.Equal(500, preview.ChanceTenths);
        Assert.True(Preview(At(engine, 1), Defense2).CanFire);
        engine.ReceiveCommand(Manual("invalid", target));
        Assert.Equal(preview.Reason, Assert.Single(At(engine, 1).CommandResults).ReasonCode);
    }

    [Fact]
    public void Zero_chance_is_eligible_and_busy_is_per_module()
    {
        using var engine = new SimulationEngine(AutomaticDefenseTests.WeaponRegistry(0, 100));
        engine.LoadScenario(HostileTorpedoFireTests.HostileScenario());
        string target = LaunchEnemy(engine);
        engine.SetObjectInteractionState(null, target);
        Assert.Equal(0, Preview(At(engine, 1)).ChanceTenths);
        Assert.True(Preview(At(engine, 1)).CanFire);
        engine.ReceiveCommand(Manual("fire", target));
        var snapshot = At(engine, 1);
        Assert.False(Preview(snapshot).CanFire);
        Assert.Equal(CommandReasonCodes.Busy, Preview(snapshot).Reason);
        Assert.True(Preview(snapshot, Defense2).CanFire);
        Assert.Equal(0, Preview(snapshot, Defense2).ChanceTenths);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void Preview_uses_confirmed_inclusive_manual_range(double offset, bool allowed)
    {
        using var engine = Create(HostileTorpedoFireTests.HostileScenario(2000));
        string target = LaunchEnemy(engine);
        var save = engine.CaptureSaveStateForTests(1, SimulationSpeed.Speed0);
        save = save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(o =>
            o.ObjectId == Player ? o with { PositionX = offset } : o).ToArray()
            }
        };
        engine.LoadScenario(save);
        engine.SetObjectInteractionState(null, target);
        var preview = Preview(At(engine, 1));
        Assert.Equal(allowed, preview.CanFire);
        Assert.Equal(allowed ? null : "target_out_of_range", preview.Reason);
        Assert.Equal(500, preview.ChanceTenths);
    }

    [Fact]
    public void Preview_uses_captured_target_rating_after_catalog_change()
    {
        using var original = Create(HostileTorpedoFireTests.HostileScenario());
        string target = LaunchEnemy(original);
        var save = original.CaptureSaveStateForTests(1, SimulationSpeed.Speed0);
        using var restored = new SimulationEngine(AutomaticDefenseTests.WeaponRegistry(65, 99));
        restored.LoadScenario(save);
        restored.SetObjectInteractionState(null, target);
        var preview = Preview(At(restored, 1));
        Assert.Equal(65m, preview.Accuracy);
        Assert.Equal(5m, preview.TargetManeuverability);
        Assert.Equal(600, preview.ChanceTenths);
        Assert.True(preview.CanFire);
    }

    [Fact]
    public void Removed_target_clears_preview_and_rejects_stale_fire()
    {
        using var engine = Create(HostileTorpedoFireTests.HostileScenario());
        string target = LaunchEnemy(engine);
        engine.SetObjectInteractionState(null, target);
        Assert.True(Preview(At(engine, 1)).CanFire);
        var after = At(engine, 40000);
        Assert.DoesNotContain(after.Objects, o => o.ObjectId == target);
        Assert.Null(after.SelectedObjectId);
        Assert.Null(Preview(after).TargetTorpedoId);
        Assert.False(Preview(after).CanFire);
        engine.ReceiveCommand(Manual("stale", target));
        Assert.Equal(CommandResultStatus.Rejected, Assert.Single(At(engine, 40000).CommandResults).Status);
    }
}
