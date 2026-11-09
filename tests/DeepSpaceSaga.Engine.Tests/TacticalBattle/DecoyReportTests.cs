using System.Globalization;
using System.Text.Json;
using DeepSpaceSaga.Engine.TacticalBattle.Ai;
using Battle = DeepSpaceSaga.Engine.TacticalBattle.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

[Trait("Category", "Balance")]
public sealed class DecoyReportTests
{
    [Fact]
    public void Report_decoy_effect_over_series()
    {
        var catalog = AssemblyTests.Catalog();
        string directory = Path.Combine(Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT")!, "ep0020-decoy");
        Directory.CreateDirectory(directory);
        var rows = new List<object>();
        var csv = new List<string> { "tactic,battles,launches,decoyed,hits,launchesPerBattle,consumedFraction,meanChanceBefore,meanChanceAfter,missFraction" };
        foreach (string tactic in new[] { "dogfight", "raid", "defence", "balanced", "commander" })
        {
            long launched = 0, shots = 0, hits = 0;
            double before = 0, after = 0;
            for (int seed = 1; seed <= 1000; seed++)
            {
                var battle = Battle.Create(catalog, "initial", seed, new Dictionary<string, string> { ["A"] = tactic });
                long available = battle.Drones.Sum(d => d.DecoyCharges);
                battle.Run(TacticalBattleAi.Plan);
                var stats = battle.Statistics;
                Assert.InRange(stats.DecoysLaunched, 0, available);
                Assert.InRange(stats.ShotsDecoyed, 0, stats.DecoysLaunched);
                Assert.InRange(stats.HitsDecoyed, 0, stats.ShotsDecoyed);
                Assert.True(stats.DecoyChanceAfter <= stats.DecoyChanceBefore);
                launched += stats.DecoysLaunched; shots += stats.ShotsDecoyed; hits += stats.HitsDecoyed;
                before += stats.DecoyChanceBefore; after += stats.DecoyChanceAfter;
            }
            Assert.True(launched > 0 && shots > 0);
            double perBattle = launched / 1000.0, consumed = shots / (double)launched;
            double meanBefore = before / shots, meanAfter = after / shots, miss = 1 - hits / (double)shots;
            rows.Add(new
            {
                tactic,
                battles = 1000,
                launches = launched,
                decoyed = shots,
                hits,
                launchesPerBattle = perBattle,
                consumedFraction = consumed,
                meanChanceBefore = meanBefore,
                meanChanceAfter = meanAfter,
                missFraction = miss
            });
            csv.Add(string.Join(',', new[] { tactic, "1000", launched.ToString(CultureInfo.InvariantCulture),
                shots.ToString(CultureInfo.InvariantCulture), hits.ToString(CultureInfo.InvariantCulture),
                perBattle.ToString("R", CultureInfo.InvariantCulture), consumed.ToString("R", CultureInfo.InvariantCulture),
                meanBefore.ToString("R", CultureInfo.InvariantCulture), meanAfter.ToString("R", CultureInfo.InvariantCulture),
                miss.ToString("R", CultureInfo.InvariantCulture) }));
        }
        File.WriteAllLines(Path.Combine(directory, "decoy-report.csv"), csv);
        File.WriteAllText(Path.Combine(directory, "summary.json"), JsonSerializer.Serialize(new
        { battles = 5000, missFactor = catalog.Decoys["decoy.t1"].MissFactor, aiThreshold = catalog.Rules.Decoy.AiThreatChance, rows },
            new JsonSerializerOptions { WriteIndented = true }));
    }
}
