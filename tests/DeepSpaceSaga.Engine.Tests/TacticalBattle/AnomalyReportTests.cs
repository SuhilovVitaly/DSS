using System.Globalization;
using System.Text.Json;
using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Engine.TacticalBattle.Ai;
using DeepSpaceSaga.Motion.TacticalBattle;
using Battle = DeepSpaceSaga.Engine.TacticalBattle.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

[Trait("Category", "Balance")]
public sealed class AnomalyReportTests
{
    [Fact] public void Report_anomaly_effect_over_series_dogfight() => Report("dogfight");
    [Fact] public void Report_anomaly_effect_over_series_raid() => Report("raid");
    [Fact] public void Report_anomaly_effect_over_series_defence() => Report("defence");
    [Fact] public void Report_anomaly_effect_over_series_balanced() => Report("balanced");
    [Fact] public void Report_anomaly_effect_over_series_commander() => Report("commander");

    private static void Report(string tactic)
    {
        var catalog = AssemblyTests.Catalog();
        string directory = Path.Combine(Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT")!, "ep0020-anomalies");
        Directory.CreateDirectory(directory);
        long heat = 0, dust = 0, shots = 0, crossing = 0, absorbed = 0, turns = 0, ownTurns = 0, enemyTurns = 0, ownDust = 0, enemyDust = 0;
        double absorptionSum = 0;
        int wins = 0, losses = 0, draws = 0, timeouts = 0;
        var csv = new List<string> { "seed,tactic,heat,dust,turns,outcome,shots,crossing,absorbed,absorptionSum,ownDroneTurns,ownDustTurns,enemyDroneTurns,enemyDustTurns" };
        for (int seed = 1; seed <= 1000; seed++)
        {
            var battle = Battle.Create(catalog, "initial", seed, new Dictionary<string, string> { ["A"] = tactic });
            long h = battle.Anomalies.Count(a => a.Kind == TacticalAnomalyKind.Heat), d = battle.Anomalies.Count(a => a.Kind == TacticalAnomalyKind.Dust);
            long s = 0, c = 0, ab = 0, own = 0, enemy = 0, od = 0, ed = 0; double sum = 0;
            while (battle.Result is null && battle.Turn < catalog.Rules.SeriesTurnCap)
            {
                battle.Planning(TacticalBattleAi.Plan); battle.Movement();
                if (battle.Result is not null) break;
                foreach (var drone in battle.Drones.Where(u => u.IsActive))
                {
                    bool inside = TacticalBattleSalvo.InDust(battle, drone);
                    if (drone.Side == "A") { own++; if (inside) od++; }
                    else { enemy++; if (inside) ed++; }
                }
                battle.Firing();
                foreach (var shot in battle.LastSalvo!.Shots)
                {
                    s++; if (shot.Absorption > 0) c++; if (shot.Absorbed) ab++; sum += shot.Absorption;
                }
            }
            var result = battle.Run(TacticalBattleAi.Plan);
            Assert.Equal(s, battle.Statistics.Shots); Assert.Equal(ab, battle.Statistics.Absorbed);
            Assert.InRange(ab, 0, c); Assert.InRange(c, 0, s);
            heat += h; dust += d; shots += s; crossing += c; absorbed += ab; absorptionSum += sum; turns += result.Turns;
            ownTurns += own; enemyTurns += enemy; ownDust += od; enemyDust += ed;
            switch (result.Kind)
            {
                case TacticalOutcomeKind.A: wins++; break;
                case TacticalOutcomeKind.B: losses++; break;
                case TacticalOutcomeKind.Draw: draws++; break;
                case TacticalOutcomeKind.Timeout: timeouts++; break;
            }
            csv.Add(string.Join(',', new object[] { seed, tactic, h, d, result.Turns, result.Kind, s, c, ab, sum.ToString("R", CultureInfo.InvariantCulture), own, od, enemy, ed }));
        }
        Assert.Equal(1000, wins + losses + draws + timeouts); Assert.True(shots > 0 && crossing > 0 && absorbed > 0);
        var summary = new
        {
            tactic,
            battles = 1000,
            heat,
            dust,
            meanHeat = heat / 1000.0,
            meanDust = dust / 1000.0,
            shots,
            crossing,
            absorbed,
            crossingFraction = crossing / (double)shots,
            meanAbsorption = absorptionSum / shots,
            meanAbsorptionWhenCrossing = absorptionSum / crossing,
            absorbedFraction = absorbed / (double)shots,
            ownDroneTurns = ownTurns,
            enemyDroneTurns = enemyTurns,
            ownDustTurns = ownDust,
            enemyDustTurns = enemyDust,
            ownDustFraction = ownDust / (double)ownTurns,
            enemyDustFraction = enemyDust / (double)enemyTurns,
            meanTurns = turns / 1000.0,
            wins,
            losses,
            draws,
            timeouts
        };
        File.WriteAllLines(Path.Combine(directory, tactic + ".csv"), csv);
        File.WriteAllText(Path.Combine(directory, tactic + ".json"), JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
    }
}
