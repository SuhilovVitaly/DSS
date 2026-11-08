using System.Globalization;
using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Engine.TacticalBattle.Ai;
using DeepSpaceSaga.Motion.TacticalBattle;
using Xunit.Abstractions;
using Battle = DeepSpaceSaga.Engine.TacticalBattle.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

[Trait("Category", "Balance")]
public sealed class EvasionCalibrationTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> Penalties() => Enumerable.Range(10, 31).Select(p => new object[] { p });

    [Theory]
    [MemberData(nameof(Penalties))]
    public void Report_expected_damage_ratio_for_penalties(int penalty)
    {
        var original = AssemblyTests.Catalog();
        var catalog = original with { Rules = original.Rules with { Evasion = original.Rules.Evasion with { HitPenalty = penalty } } };
        double withEvasion = 0, withoutEvasion = 0;
        long shots = 0, turns = 0, evadingTurns = 0, wins = 0, battles = 0;
        foreach (string tactic in new[] { "dogfight", "raid", "defence", "balanced", "commander" })
            for (long seed = 1; seed <= 1000; seed++)
            {
                var tactics = new Dictionary<string, string> { ["A"] = tactic };
                var direct = Battle.Create(catalog, "initial", seed, tactics);
                direct.Run(TacticalBattleAi.Plan);
                var observed = Battle.Create(catalog, "initial", seed, tactics);
                while (observed.Result is null && observed.Turn < catalog.Rules.SeriesTurnCap)
                {
                    long before = observed.Statistics.Evasions;
                    observed.Step(TacticalBattleAi.Plan);
                    if (observed.Statistics.Evasions > before) evadingTurns++;
                    if (observed.LastSalvo is not { } salvo) continue;
                    foreach (var shot in salvo.Shots.Where(s => s.TargetEvading))
                    {
                        var shooter = shot.Shooter;
                        var target = (TacticalDrone)shot.Target;
                        var hit = catalog.Rules.Hit;
                        var rules = new TacticalHitRules(hit.Base, hit.MinChance, hit.MaxChance, hit.RangeBonus,
                            hit.AnglePenalty, hit.RearBonus, hit.RearBonusArcDeg);
                        var input = new TacticalShotInput(shooter.Accuracy, target.Maneuver, shooter.Weapon.HitBonus,
                            shooter.Weapon.Range, shooter.Weapon.ArcDeg, shot.Distance, shot.Angle,
                            TacticalHitChance.RearAngle(shooter.Pose, target.Pose), true, target.Shield?.Defense ?? 0,
                            target.ShieldCount, catalog.Rules.Rear.ArcHalfDeg, shooter.Weapon.RangeBonus);
                        double mean = ((double)shooter.Weapon.DamageMin + shooter.Weapon.DamageMax) / 2;
                        withEvasion += shot.Chance * mean;
                        withoutEvasion += TacticalHitChance.Chance(rules, input) * mean;
                        shots++;
                    }
                }
                observed.Run(TacticalBattleAi.Plan); // materialize Timeout at exactly the same cap
                Assert.Equal(GoldenBattleTests.Signature(direct), GoldenBattleTests.Signature(observed));
                turns += direct.Turn;
                if (direct.Result!.Kind == TacticalOutcomeKind.A) wins++;
                battles++;
            }
        Assert.Equal(5000, battles);
        Assert.True(shots > 0 && withoutEvasion > 0);
        double ratio = withEvasion / withoutEvasion;
        Assert.InRange(ratio, 0, 1);
        string row = string.Join(",", new[] { (double)penalty, ratio, shots, evadingTurns / (double)turns,
            turns / (double)battles, wins / (double)battles }.Select(n => n.ToString("R", CultureInfo.InvariantCulture)));
        string root = Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT")
            ?? throw new InvalidOperationException("Set an absolute DSS_VALIDATION_ROOT for calibration evidence.");
        Assert.True(Path.IsPathFullyQualified(root));
        string directory = Path.Combine(root, "ep0020-calibration");
        Directory.CreateDirectory(directory);
        File.WriteAllLines(Path.Combine(directory, $"calibration-{penalty}.csv"),
            ["penalty,ratio,shots,evadingTurnFraction,meanTurns,winFractionA", row]);
        output.WriteLine(row);
    }
}
