using Xunit.Abstractions;
using System.Text.Json;
using DeepSpaceSaga.CombatAcceptance;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class CombatBalanceReport
{
    private readonly ITestOutputHelper _output;

    public CombatBalanceReport(ITestOutputHelper output) => _output = output;

    public static IEnumerable<object[]> ProductionBatches()
    {
        foreach (string policy in new[] { "dodge", "static" })
            for (int first = 3001; first <= 4000; first += 50) yield return [policy, first];
    }

    [Theory]
    [Trait("Category", "Balance")]
    [MemberData(nameof(ProductionBatches))]
    public void Production_balance_1000_seeds_reports_corridors(string policy, int firstSeed)
    {
        var harness = new ProductionCombatBalanceHarness(TorpedoImpactTests.ClientRoot);
        string root = Environment.GetEnvironmentVariable("DSS_BALANCE_OUTPUT") ?? Path.Combine(AppContext.BaseDirectory, "balance-results");
        Directory.CreateDirectory(root);
        string path = Path.Combine(root, $"{policy}-{firstSeed}-{firstSeed + 49}.jsonl"), temporary = path + ".tmp";
        var results = new List<ProductionCombatBalanceHarness.RunResult>();
        using (var writer = new StreamWriter(temporary, false))
            for (int seed = firstSeed; seed < firstSeed + 50; seed++)
            {
                var result = harness.Run((ulong)seed, policy);
                Assert.Equal(0, result.LegacyChanceFacts);
                Assert.True(double.IsFinite(result.RealDurationMs));
                Assert.Equal(result.PhysicalDurationMs * 4, result.RealDurationMs);
                if (policy == "static") Assert.Equal(0, result.Evades);
                Assert.Equal(result.Commands.Length, result.Commands.Select(c => c.Command.CommandId).Distinct().Count());
                writer.WriteLine(JsonSerializer.Serialize(result)); results.Add(result);
            }
        File.Move(temporary, path, true);
        _output.WriteLine($"policy={policy};seeds={firstSeed}..{firstSeed + 49};count={results.Count};" +
            $"wins={results.Count(r => r.Outcome == "victory")};unfinished={results.Count(r => r.Outcome == "unfinished")};" +
            $"raw={path};corridorAcceptance=DEFERRED_TO_COMPLETE_1000_SEED_AGGREGATE;policy={ProductionCombatBalanceHarness.PolicyVersion}");
        Assert.Equal(50, results.Count);
    }

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
