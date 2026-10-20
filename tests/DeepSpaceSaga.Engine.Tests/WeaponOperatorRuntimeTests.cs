using System.Text.Json;
using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using Point = DeepSpaceSaga.Engine.Tests.PointCounterFixture;

namespace DeepSpaceSaga.Engine.Tests;

public class WeaponOperatorRuntimeTests
{
    private static PlayerCommand Launch(bool counter, string id = "launch") => counter ? Point.Shot(id, x: 150, y: 0) :
        new(id, 1, Point.Player, Point.Torpedo, CombatCommandTypes.Launch, TargetWorldX: 150, TargetWorldY: 0, ProfileId: "medium");
    private static string Module(bool counter) => counter ? Point.Counter : Point.Torpedo;
    private static InstalledModuleSnapshot Module(AuthoritativeSnapshot snapshot, bool counter) =>
        snapshot.InstalledModules.Single(m => m.ModuleId == Module(counter));
    private static string Json<T>(T value) => JsonSerializer.Serialize(value);
    private static SpaceObjectData Skill(SpaceObjectData o, int skill) => o.ObjectId != Point.Player ? o : o with
    { Crew = o.Crew!.Select(c => c with { AccuracySkill = skill, ManeuverabilitySkill = 1 }).ToArray() };

    [Fact]
    public void Both_point_weapon_families_project_their_own_accuracy_operator()
    {
        using var engine = Point.Create(configure: o => o.ObjectId != Point.Player ? o : o with
        { Crew = o.Crew!.Select(c => c with { AccuracySkill = c.CrewId == o.Modules!.Single(m => m.ModuleId == Point.Torpedo).OperatorCrewId ? 15 : 30, ManeuverabilitySkill = 1 }).ToArray() });
        var snapshot = Point.At(engine); var attack = Module(snapshot, false).Operator!; var defense = Module(snapshot, true).Operator!;
        Assert.NotEqual(attack.CrewId, defense.CrewId);
        Assert.Equal(WeaponSkillType.TorpedoAttack, attack.SkillType); Assert.Equal(WeaponSkillType.CountermeasureDefense, defense.SkillType);
        Assert.Equal(15, attack.Skill); Assert.Equal(30, defense.Skill);
        Assert.Equal(0, attack.BaseRating); Assert.Equal(15, attack.EffectiveRating);
        Assert.Equal(0, defense.BaseRating); Assert.Equal(30, defense.EffectiveRating);
        Assert.Null(snapshot.InstalledModules.Single(m => m.ModuleId == "MOD-PLAYER-ENGINE-01").Operator);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(15)]
    [InlineData(17)]
    public void Historical_save_versions_are_rejected_atomically_instead_of_inventing_a_migrated_rating(int version)
    {
        using var engine = Point.Create(); engine.ReceiveCommand(Launch(false)); Point.At(engine);
        var save = Point.Save(engine); var old = save with { SaveFormatVersion = version };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(old, true));
        Assert.Equal(ScenarioLoader.Serialize(save), ScenarioLoader.Serialize(Point.Save(engine)));
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(old), true, isSave: true));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Missing_operator_blocks_current_launch_without_side_effects(bool counter)
    {
        using var engine = Point.Create(configure: o => o with { Modules = o.Modules!.Select(m => m with { OperatorCrewId = null }).ToArray() });
        var before = Point.Save(engine); engine.ReceiveCommand(Launch(counter)); var snapshot = Point.At(engine);
        Assert.Equal(CommandReasonCodes.NoWeaponOperator, Assert.Single(snapshot.CommandResults).ReasonCode);
        Assert.DoesNotContain(snapshot.Objects, o => o.OwnProjectile is not null);
        var after = Point.Save(engine);
        Assert.Equal(Json(before.GameState.PointCombatState), Json(after.GameState.PointCombatState));
        Assert.Equal(Json(before.GameState.CombatState), Json(after.GameState.CombatState));
        Assert.Equal(Json(before.GameState.DefenseState), Json(after.GameState.DefenseState));
        Assert.Equal(Json(before.GameState.SpaceObjects), Json(after.GameState.SpaceObjects));
        Assert.Equal(Json(before.GameState.DialogueState), Json(after.GameState.DialogueState));
        Assert.Equal(Json(before.GameState.TradingMap?.RngStreams), Json(after.GameState.TradingMap?.RngStreams));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Captured_point_geometry_and_reporter_survive_file_load_with_live_operator_skill_changes(bool counter)
    {
        using var engine = Point.Create(configure: o => Skill(o, 20), scatter: null, torpedoScatter: null, enemyX: 600);
        engine.ReceiveCommand(Launch(counter)); Point.At(engine); var original = Point.Flight(Point.At(engine, 100), Module(counter));
        var save = Point.Save(engine, 100);
        save = save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(o =>
            o.ObjectId != Point.Player ? o : o with { Crew = o.Crew!.Select(c => c with { AccuracySkill = 1, DisplayName = "Changed" }).ToArray() }).ToArray()
            }
        };
        string path = Path.Combine(Path.GetTempPath(), "point-operator-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(path, ScenarioLoader.Serialize(save));
            using var loaded = EngineContentLoader.CreateEngineFromSaveFile(Path.Combine(Point.Root, "Settings.json"), path);
            var resumed = Point.At(loaded, 100); Assert.Equal(original, Point.Flight(resumed, Module(counter)));
            var op = Module(resumed, counter).Operator!; Assert.Equal(1, op.Skill); Assert.Equal("Changed", op.DisplayName);
            Assert.Equal(1, op.EffectiveRating); Assert.Equal(0, op.BaseRating);
            Assert.NotEqual("Changed", original.Reporter!.DisplayName);
            double expected = counter ? .117296 : .029324;
            Assert.Equal(expected, counter ? Module(resumed, true).Defense!.EffectiveScatterPercent!.Value : Module(resumed, false).LauncherCombat!.EffectiveScatterPercent!.Value, 8);
            Assert.Equal(2ul, loaded.CaptureLaunchScatterState().Counter);
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData(false, .02999)]
    [InlineData(true, .11996)]
    public void Zero_accuracy_skill_changes_scatter_without_interception_probability_or_roll(bool counter, double scatter)
    {
        using var engine = Point.Create(configure: o => Skill(o, 0), scatter: null, torpedoScatter: null, enemyX: 600);
        var module = Module(Point.At(engine), counter); Assert.Equal(0, module.Operator!.Skill); Assert.Equal(0, module.Operator.BaseRating);
        Assert.Equal(scatter, counter ? module.Defense!.EffectiveScatterPercent!.Value : module.LauncherCombat!.EffectiveScatterPercent!.Value, 8);
        engine.ReceiveCommand(Launch(counter)); Point.At(engine); var snapshot = Point.At(engine, 3000);
        Assert.All(snapshot.CombatJournal, e => { Assert.Null(e.Roll); Assert.Null(e.ChanceTenths); Assert.Null(e.RatingBreakdown); });
        Assert.Equal(2ul, engine.CaptureLaunchScatterState().Counter); Assert.Equal(0ul, Point.Save(engine, 3000).GameState.DefenseState!.RngCounter);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Active_captured_profile_survives_future_catalog_speed_and_scatter_changes(bool counter)
    {
        using var engine = Point.Create(scatter: null, torpedoScatter: null, enemyX: 600);
        engine.ReceiveCommand(Launch(counter)); Point.At(engine); var original = Point.Flight(Point.At(engine, 100), Module(counter)); var save = Point.Save(engine, 100);
        var source = Point.Registry(scatter: null, torpedoScatter: null);
        static IEnumerable<T> All<T>(TypeRegistry<T> r) where T : ITypeDefinition => Enumerable.Range(0, r.Count).Select(r.GetDefinition);
        var changed = GameDataRegistry.Create(All(source.ModuleCategories), All(source.ModuleTypes).Select(m => !m.IsPointLauncher ? m : m with
        { ScatterPercent = .5, Profiles = m.Profiles.Select(p => p with { SpeedKmS = 7, BlastRadiusKm = 2 }).ToImmutableArray() }),
            All(source.ItemTypes), All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes), All(source.Dialogues), All(source.Quests),
            stationMarketProfiles: All(source.StationMarketProfiles), shipClasses: All(source.ShipClasses), stationMarketEvents: All(source.StationMarketEvents), combatRules: source.CombatRules);
        using var loaded = new SimulationEngine(changed); loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true, isSave: true), true);
        Assert.Equal(original, Point.Flight(Point.At(loaded, 100), Module(counter)));
        long ready = counter ? 12500 : 10000; Point.At(loaded, ready); loaded.ReceiveCommand(Launch(counter, "future"));
        var next = Point.Flight(Point.At(loaded, ready), Module(counter)); Assert.Equal(7, next.SpeedKmS); Assert.Equal(2, next.BlastRadiusKm);
        Assert.Equal(4ul, loaded.CaptureLaunchScatterState().Counter);
    }

    [Fact]
    public void Own_operator_projection_is_authoritative_and_unknown_enemy_crew_stays_private()
    {
        using var engine = Point.Create(configure: o => o.ObjectId == Point.Enemy ? o with { IsKnown = false } : Skill(o, 20));
        var save = Point.Save(engine); var snapshot = Point.At(engine); var op = Module(snapshot, false).Operator!;
        var player = save.GameState.SpaceObjects.Single(o => o.ObjectId == Point.Player);
        Assert.Equal(player.Modules!.Single(m => m.ModuleId == Point.Torpedo).OperatorCrewId, op.CrewId);
        Assert.Equal(20, op.Skill); Assert.Equal(0, op.BaseRating); Assert.Equal(20, op.EffectiveRating);
        var unknown = snapshot.Objects.Single(o => o.ObjectId == Point.Enemy); Assert.Null(unknown.HullCombat); Assert.Null(unknown.Defense);
        string serialized = Json(snapshot);
        foreach (var crew in save.GameState.SpaceObjects.Single(o => o.ObjectId == Point.Enemy).Crew!) Assert.DoesNotContain(crew.CrewId, serialized);
    }
}
