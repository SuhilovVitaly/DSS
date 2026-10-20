using Xunit.Abstractions;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class CombatBalanceReport
{
    private readonly ITestOutputHelper _output;

    public CombatBalanceReport(ITestOutputHelper output) => _output = output;

    [Theory]
    [Trait("Category", "Balance")]
    [InlineData("CombatMukha")]
    public void Smoke_one_hundred_real_point_engine_seeds_without_claiming_balance_acceptance(string scenario)
    {
        var runs = Enumerable.Range(1, 100)
            .Select(seed => ModuleCombatEndToEndTests.Run(scenario, (ulong)seed)).ToArray();
        int finished = runs.Count(run => run.Finished);
        double winPercent = runs.Count(run => run.Victory);
        _output.WriteLine($"{scenario}: seeds=1..100, finished={finished}/100, wins={winPercent:F1}%, " +
            "balanceAcceptance=NOT_EVALUATED");
        _output.WriteLine($"playerHits={runs.Average(run => run.PlayerHits):F2}, " +
            $"enemyTorpedoes={runs.Average(run => run.EnemyTorpedoes):F2}, " +
            $"playerCountermeasureLaunches={runs.Average(run => run.PlayerCountermeasureLaunches):F2}, " +
            $"lowerMedianDurationSeconds={runs.OrderBy(run => run.DurationMs).ElementAt(49).DurationMs / 1000.0:F1}");
        // Integration smoke only; US14 owns the1000-seed policy comparison and acceptance thresholds.
        Assert.Equal(100, runs.Length);
        Assert.All(runs, run => { Assert.True(run.Finished); Assert.True(run.EnemyTorpedoes > 0); Assert.Equal(0, run.PlayerCountermeasureLaunches); });
        Assert.All(runs, run => Assert.True(run.HostileLaunches > 0 && run.PlayerLaunches > 0 && run.HitRolls == 0 && run.LegacyChanceFacts == 0 && run.Detonations > 0));
    }
}
