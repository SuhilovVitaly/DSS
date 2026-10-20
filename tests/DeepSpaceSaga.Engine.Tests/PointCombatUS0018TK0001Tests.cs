using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Combat;
using Point = DeepSpaceSaga.Engine.Tests.PointCounterFixture;
namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0018TK0001Tests
{
    [Fact]
    public void Legacy_interception_api_has_no_consumers_or_runtime_type()
    {
        string type = "Interception" + "Math";
        Assert.Null(typeof(SimulationEngine).Assembly.GetType("DeepSpaceSaga.Engine.Combat." + type));
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/DeepSpaceSaga.Engine"));
        Assert.False(File.Exists(Path.Combine(root, "Combat", type + ".cs")));
        foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories).Where(p => !p.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)))
            Assert.DoesNotContain(type + ".", File.ReadAllText(file));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Operator_failure_still_consumes_no_launch_or_interception_rng(bool counter)
    {
        using var engine = Point.Create(configure: o => o with { Modules = o.Modules!.Select(m => m with { OperatorCrewId = null }).ToArray() });
        var before = Point.Save(engine);
        engine.ReceiveCommand(counter ? Point.Shot() : new("launch", 1, Point.Player, Point.Torpedo, CombatCommandTypes.Launch, TargetWorldX: 100, TargetWorldY: 0, ProfileId: "slow"));
        var snapshot = Point.At(engine); Assert.Equal(CommandReasonCodes.NoWeaponOperator, Assert.Single(snapshot.CommandResults).ReasonCode);
        var after = Point.Save(engine); Assert.Equal(before.GameState.PointCombatState!.LaunchScatter, after.GameState.PointCombatState!.LaunchScatter);
        Assert.Equal(before.GameState.DefenseState!.RngCounter, after.GameState.DefenseState!.RngCounter);
        Assert.Equal(before.GameState.CombatState!.ProjectileSequence, after.GameState.CombatState!.ProjectileSequence);
        Assert.Equal(before.GameState.DefenseState.ProjectileSequence, after.GameState.DefenseState.ProjectileSequence);
        Assert.Empty(after.GameState.PointCombatState.Projectiles);
    }

    [Fact]
    public void Current_scatter_stream_resumes_exactly_without_cached_samples_or_interception_draws()
    {
        var rng = new LaunchScatterRng(42); var first = rng.NextPair(); Assert.True(double.IsFinite(first.X)); Assert.True(double.IsFinite(first.Y)); Assert.Equal(2ul, rng.Counter);
        var resumed = new LaunchScatterRng(rng.State, rng.Counter);
        Assert.Equal(rng.NextPair(), resumed.NextPair()); Assert.Equal(rng.State, resumed.State); Assert.Equal(4ul, resumed.Counter);
    }
}
