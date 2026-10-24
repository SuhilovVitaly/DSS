using System.Text.Json;
using DeepSpaceSaga.Contracts;
using Point = DeepSpaceSaga.Engine.Tests.PointCounterFixture;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>Point launchers expose current own capabilities; the retired server chance/target-lock preview is absent.</summary>
public sealed class DefensePreviewProjectionTests
{
    [Theory]
    [InlineData(0, .11996)]
    [InlineData(15, .08)]
    [InlineData(30, .04004)]
    public void Current_operator_scatter_is_projected_without_chance_attempt_or_random_draw(int skill, double expected)
    {
        using var engine = Point.Create(scatter: null, torpedoScatter: null, configure: o => o.ObjectId != Point.Player ? o : o with
        { Crew = o.Crew!.Select(c => c with { AccuracySkill = skill }).ToArray() });
        var before = engine.CaptureLaunchScatterState();
        for (int repeat = 0; repeat < 3; repeat++)
        {
            var state = Point.At(engine); var modules = state.InstalledModules.Where(m => m.Defense is not null).ToArray(); Assert.Equal(2, modules.Length);
            Assert.All(modules, m =>
            {
                Assert.Null(m.DefensePreview); Assert.NotNull(m.Defense); Assert.Null(typeof(DefenseSnapshot).GetProperty("AutoEnabled")); Assert.Equal(skill, m.Defense.Operator!.Skill);
                Assert.Equal(0m, m.Defense.Operator.BaseRating); Assert.Equal(skill, m.Defense.Operator.EffectiveRating);
                Assert.Equal(expected, m.Defense.EffectiveScatterPercent!.Value, 10); Assert.Equal(30, m.Defense.RangeKm);
                Assert.Null(typeof(DefenseSnapshot).GetProperty("Accuracy")); Assert.Null(typeof(DefenseSnapshot).GetProperty("ManualRangeKm"));
            });
            Assert.Equal(before, engine.CaptureLaunchScatterState()); Assert.Equal(0, Point.Save(engine).GameState.DefenseState!.ProjectileSequence);
        }
    }

    [Fact]
    public void Two_operators_keep_independent_live_scatter_and_no_hidden_server_intercept_preview()
    {
        using var engine = Point.Create(scatter: null, configure: o =>
        {
            if (o.ObjectId != Point.Player) return o;
            string first = o.Modules!.Single(m => m.ModuleId == Point.Counter).OperatorCrewId!;
            string second = o.Modules!.Single(m => m.ModuleId == Point.Counter2).OperatorCrewId!;
            return o with { Crew = o.Crew!.Select(c => c with { AccuracySkill = c.CrewId == first ? 0 : c.CrewId == second ? 30 : c.AccuracySkill }).ToArray() };
        });
        var modules = Point.At(engine).InstalledModules;
        var a = modules.Single(m => m.ModuleId == Point.Counter); var b = modules.Single(m => m.ModuleId == Point.Counter2);
        Assert.NotEqual(a.Operator!.CrewId, b.Operator!.CrewId);
        Assert.Equal(.11996, a.Defense!.EffectiveScatterPercent!.Value, 10); Assert.Equal(.04004, b.Defense!.EffectiveScatterPercent!.Value, 10);
        Assert.Null(a.DefensePreview); Assert.Null(b.DefensePreview); Assert.Equal(0UL, engine.CaptureLaunchScatterState().Counter);
    }

    [Theory]
    [InlineData("off", CommandReasonCodes.ModuleUnavailable)]
    [InlineData("destroyed", CommandReasonCodes.ModuleDestroyed)]
    [InlineData("operator", CommandReasonCodes.NoWeaponOperator)]
    public void Public_readiness_inputs_and_execution_agree_without_a_probability_preview(string cause, string reason)
    {
        using var engine = Point.Create(configure: o => o.ObjectId != Point.Player ? o : o with
        {
            Modules = o.Modules!.Select(m => m.ModuleId != Point.Counter ? m : m with
            {
                PowerState = cause == "off" ? "Off" : m.PowerState,
                Destroyed = cause == "destroyed" || m.Destroyed,
                StructurePoints = cause == "destroyed" ? 0 : m.StructurePoints,
                OperatorCrewId = cause == "operator" ? null : m.OperatorCrewId
            }).ToArray()
        });
        var module = Point.At(engine).InstalledModules.Single(m => m.ModuleId == Point.Counter); Assert.Null(module.DefensePreview);
        if (cause == "off") Assert.Equal("Off", module.PowerState);
        if (cause == "operator") { Assert.Null(module.Defense!.Operator); Assert.Equal(DefenseState.NoOperator, module.Defense.State); }
        engine.ReceiveCommand(Point.Shot()); Assert.Equal(reason, Assert.Single(Point.At(engine).CommandResults).ReasonCode);
        Assert.Equal(0UL, engine.CaptureLaunchScatterState().Counter); Assert.Equal(0, Point.Save(engine).GameState.DefenseState!.ProjectileSequence);
    }

    [Fact]
    public void Selection_and_hover_never_retarget_an_existing_point_counter_or_publish_a_target_lock()
    {
        using var engine = Point.Create(enemyArmed: true, enemyX: 600);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Point.Enemy, Point.EnemyTorpedo, 0, 0, "slow", 0).Disposition);
        engine.ReceiveCommand(Point.Shot()); var original = Point.Flight(Point.At(engine)); var before = engine.CaptureLaunchScatterState();
        foreach (string selected in new[] { Point.Player, Point.Enemy, "torpedo-1", "countermeasure-1", "missing" })
        {
            engine.SetObjectInteractionState("torpedo-1", selected); var state = Point.At(engine);
            Assert.Equal(original, Point.Flight(state)); Assert.Equal(before, engine.CaptureLaunchScatterState());
            Assert.All(state.InstalledModules.Where(m => m.Defense is not null), m => Assert.Null(m.DefensePreview));
            var foreign = Assert.Single(state.Objects, o => o.ObservedProjectile is not null).ObservedProjectile!;
            using var json = JsonDocument.Parse(JsonSerializer.Serialize(foreign));
            Assert.Equal(8, json.RootElement.EnumerateObject().Count());
        }
    }
}
