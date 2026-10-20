using Xunit.Abstractions;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class CombatBalanceReport
{
    private readonly ITestOutputHelper _output;

    public CombatBalanceReport(ITestOutputHelper output) => _output = output;

    [Theory]
    [Trait("Category", "Balance")]
    [InlineData("CombatMukha", 81.2)]
    public void Compare_one_hundred_real_engine_seeds_to_simulator_reference(string scenario, double referenceWinPercent)
    {
        var runs = Enumerable.Range(1, 100)
            .Select(seed => ModuleCombatEndToEndTests.Run(scenario, (ulong)seed)).ToArray();
        int finished = runs.Count(run => run.Finished);
        double winPercent = runs.Count(run => run.Victory);
        double delta = winPercent - referenceWinPercent;
        _output.WriteLine($"{scenario}: seeds=1..100, finished={finished}/100, wins={winPercent:F1}%, " +
            $"reference={referenceWinPercent:F1}%, delta={delta:+0.0;-0.0;0.0} pp, within10pp={Math.Abs(delta) <= 10}");
        _output.WriteLine($"playerHits={runs.Average(run => run.PlayerHits):F2}, " +
            $"enemyTorpedoes={runs.Average(run => run.EnemyTorpedoes):F2}, " +
            $"playerCountermeasureLaunches={runs.Average(run => run.PlayerCountermeasureLaunches):F2}, " +
            $"lowerMedianDurationSeconds={runs.OrderBy(run => run.DurationMs).ElementAt(49).DurationMs / 1000.0:F1}");
        Assert.Equal(100, runs.Length);
        Assert.All(runs, run => Assert.True(run.HostileLaunches > 0 && run.PlayerLaunches > 0 && run.HitRolls > 0));
    }
}
