using DeepSpaceSaga.Engine.Combat;
using DeepSpaceSaga.Engine.Content;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public class InterceptionChanceTests
{
    private static readonly CombatChanceRules Rules = new(50, 0, 100);

    [Theory]
    [InlineData(60, 60, 500)]
    [InlineData(200, 0, 1000)]
    [InlineData(0, 200, 0)]
    [InlineData(55, 5, 1000)]
    [InlineData(30.05, 30, 501)]
    public void Shared_chance_uses_base_and_clamps(decimal attack, decimal defense, int expected)
        => Assert.Equal(expected, CombatChanceMath.ChanceTenths(attack, defense, Rules));

    [Fact]
    public void Rules_base_changes_result()
    {
        Assert.Equal(500, CombatChanceMath.ChanceTenths(60, 60, Rules));
        Assert.Equal(300, CombatChanceMath.ChanceTenths(60, 60, Rules with { Base = 30 }));
        Assert.Equal(400, CombatChanceMath.ChanceTenths(0, 100, Rules with { Min = 40 }));
    }

    [Fact]
    public void Decimal_extremes_clamp_without_overflow()
    {
        Assert.Equal(1000, CombatChanceMath.ChanceTenths(decimal.MaxValue, 0, Rules));
        Assert.Equal(0, CombatChanceMath.ChanceTenths(0, decimal.MaxValue, Rules));
    }

    [Fact]
    public void Accuracy_skill_changes_live_defense_rating()
    {
        const string moduleId = "MOD-PLAYER-COUNTERMEASURE-01";
        var scenario = TorpedoImpactTests.Scenario(obj => obj.ObjectId == Player ? obj with
        {
            Crew = obj.Crew!.Select(crew => crew.CrewId ==
                obj.Modules!.Single(module => module.ModuleId == moduleId).OperatorCrewId
                ? crew with { AccuracySkill = 20 } : crew).ToArray()
        } : obj);
        using var engine = Create(scenario);
        var defense = At(engine, 0).InstalledModules.Single(module => module.ModuleId == moduleId).Defense!;
        Assert.NotNull(defense.Operator);
        Assert.Equal(defense.Operator.BaseRating + 20, defense.Operator.EffectiveRating);
        Assert.Equal(700, CombatChanceMath.ChanceTenths(defense.Operator.EffectiveRating,
            defense.Operator.BaseRating, Rules));
    }
    [Fact]
    public void Named_stream_golden_vectors_and_resume()
    {
        var rng = new CountermeasureRng(42);
        int[] golden = [801, 820, 781, 237, 617, 381, 400, 470];
        Assert.Equal(golden.Take(4), Enumerable.Range(0, 4).Select(_ => rng.NextRoll()));
        Assert.Equal(4UL, rng.Counter);
        var resumed = new CountermeasureRng(rng.State, rng.Counter);
        Assert.Equal(golden.Skip(4), Enumerable.Range(0, 4).Select(_ => resumed.NextRoll()));
        Assert.Equal(8UL, resumed.Counter);
    }
}
