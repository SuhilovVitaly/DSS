using System.Security.Cryptography;
using System.Text;
using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Engine.TacticalBattle.Content;
using Xunit.Abstractions;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

[Trait("Category", "LongRunning")]
public sealed class SeriesParityTests(ITestOutputHelper output)
{
    private static readonly Lazy<TacticalBattleCatalog> Catalog = new(AssemblyTests.Catalog);
    [Fact] public void Dogfight() => Check("dogfight");
    [Fact] public void Raid() => Check("raid");
    [Fact] public void Defence() => Check("defence");
    [Fact] public void Balanced() => Check("balanced");
    [Fact] public void Commander() => Check("commander");

    private void Check(string tactic)
    {
        var goal = Catalog.Value.Scenarios["initial"].Goal!;
        Assert.Equal(new long[] { 1, 1000 }, goal.Seeds);
        int won = 0, lost = 0, drawn = 0, timedOut = 0, clean = 0, oneLoss = 0;
        var signatures = new List<string>();
        for (long seed = goal.Seeds[0]; seed <= goal.Seeds[1]; seed++)
        {
            var direct = GoldenBattleTests.Run(Catalog.Value, seed, tactic, false);
            var explicitBattle = GoldenBattleTests.Run(Catalog.Value, seed, tactic, true);
            string signature = GoldenBattleTests.Signature(direct);
            Assert.Equal(signature, GoldenBattleTests.Signature(explicitBattle));
            if (Environment.GetEnvironmentVariable("DSS_EP0020_NORMALIZE_SLOTS") == "1") signature = GoldenBattleTests.Signature(direct, withoutSlots: true);
            var result = direct.Result!;
            Assert.InRange(result.Turns, 1, Catalog.Value.Rules.SeriesTurnCap);
            Assert.InRange(result.Losses[0], 0, 4); Assert.InRange(result.Losses[1], 0, 6);
            Assert.Equal(result.Statistics.Shots, result.Statistics.Weapons.Sum(w => w.Shots));
            Assert.Equal(result.Statistics.Hits, result.Statistics.Weapons.Sum(w => w.Hits));
            Assert.Equal(result.Statistics.Hull, result.Statistics.Weapons.Sum(w => w.Hull));
            switch (result.Kind)
            {
                case TacticalOutcomeKind.A: won++; if (result.Losses[0] == 0) clean++; if (result.Losses[0] <= 1) oneLoss++; break;
                case TacticalOutcomeKind.B: lost++; break;
                case TacticalOutcomeKind.Draw: drawn++; break;
                case TacticalOutcomeKind.Timeout: timedOut++; break;
            }
            string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(signature)));
            signatures.Add(FormattableString.Invariant($"{seed} {result.Kind} {result.Turns} {result.Losses[0]} {result.Losses[1]} {result.RandomCalls} {hash}"));
        }
        Assert.Equal(1000, won + lost + drawn + timedOut);
        if (Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is { Length: > 0 } root)
            File.WriteAllLines(Path.Combine(root, "series-" + tactic + ".signatures.txt"), signatures);
        output.WriteLine($"EP-0017 {tactic} seeds1..1000: W={won}, W0={clean}, W<=1={oneLoss}, L={lost}, D={drawn}, Timeout={timedOut}; exact direct/explicit parity PASS. Historical simultaneous balance is not acceptance of these new totals.");
    }
}
