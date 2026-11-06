using System.Text.Json;
using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Engine.TacticalBattle.Ai;
using DeepSpaceSaga.Engine.TacticalBattle.Content;
using Xunit.Abstractions;
using Battle = DeepSpaceSaga.Engine.TacticalBattle.TacticalBattle;

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
        using var fixture = ReferenceFixtureTests.Open("series");
        var rows = fixture.RootElement.GetProperty("data").EnumerateArray().ToArray();
        var scenario = Catalog.Value.Scenarios["initial"];
        var goal = Assert.IsType<BattleGoal>(scenario.Goal);
        Assert.Equal("A", goal.PlayerSide);
        Assert.Equal(Catalog.Value.Ai.Tactics.Entries.Select(t => t.Id).Order(StringComparer.Ordinal),
            rows.Select(r => r.GetProperty("tactic").GetString()!).Order(StringComparer.Ordinal));
        var expected = Assert.Single(rows, r => r.GetProperty("tactic").GetString() == tactic);
        long first = goal.Seeds[0], last = goal.Seeds[1];
        int count = checked((int)(last - first + 1));
        Assert.Equal(expected.GetProperty("battles").GetInt32(), count);

        int won = 0, one = 0, clean = 0, lost = 0, drawn = 0, timedOut = 0;
        var signatures = Environment.GetEnvironmentVariable("DSS_RAID_SIGNATURES") is { Length: > 0 } && tactic == "raid" ? new List<string>() : null;
        long turns = 0, lossesA = 0, lossesB = 0, carriersA = 0, carriersB = 0;
        var tactics = new Dictionary<string, string>(StringComparer.Ordinal) { ["A"] = tactic };
        for (long seed = first; seed <= last; seed++)
        {
            var result = Battle.Create(Catalog.Value, "initial", seed, tactics).Run(TacticalBattleAi.Plan);
            signatures?.Add(FormattableString.Invariant($"{seed} {result.Kind} {result.Turns} {result.Losses[0]} {result.Losses[1]} {result.CarrierHull[0]} {result.CarrierHull[1]} {result.RandomCalls}"));
            switch (result.Kind)
            {
                case TacticalOutcomeKind.A:
                    won++;
                    if (result.Losses[0] <= 1) one++;
                    if (result.Losses[0] == 0) clean++;
                    break;
                case TacticalOutcomeKind.B: lost++; break;
                case TacticalOutcomeKind.Draw: drawn++; break;
                case TacticalOutcomeKind.Timeout: timedOut++; break;
                default: throw new InvalidOperationException("Unknown result.");
            }
            turns += result.Turns;
            lossesA += result.Losses[0];
            lossesB += result.Losses[1];
            if (result.CarrierHull[0] <= 0) carriersA++;
            if (result.CarrierHull[1] <= 0) carriersB++;
        }

        if (signatures is not null) File.WriteAllLines(Environment.GetEnvironmentVariable("DSS_RAID_SIGNATURES")!, signatures);

        output.WriteLine(FormattableString.Invariant(
            $"{tactic} {first}..{last}: W={won} W<=1={one} W0={clean} L={lost} D={drawn} Timeout={timedOut}; turns={turns}, losses={lossesA}/{lossesB}, carriers={carriersA}/{carriersB}"));
        // The independent table publishes 0.1 percentage point resolution over 1,000
        // runs, which determines these five integer counters. Means do not determine sums.
        int Exact(string name) => checked((int)(expected.GetProperty(name).GetDecimal() * count / 100));
        Assert.Equal(Exact("winPercent"), won);
        Assert.Equal(Exact("winMaxOneLossPercent"), one);
        Assert.Equal(Exact("winNoLossPercent"), clean);
        Assert.Equal(Exact("defeatPercent"), lost);
        Assert.Equal(Exact("drawPercent"), drawn);
        Assert.Equal(count - Exact("winPercent") - Exact("defeatPercent") - Exact("drawPercent"), timedOut);
        Assert.Equal(expected.GetProperty("averageTurns").GetDecimal(), Math.Round((decimal)turns / count, 1));
        Assert.Equal(expected.GetProperty("averageLosses")[0].GetDecimal(), Math.Round((decimal)lossesA / count, 2));
        Assert.Equal(expected.GetProperty("averageLosses")[1].GetDecimal(), Math.Round((decimal)lossesB / count, 2));
        Assert.Equal(expected.GetProperty("carrierDeathPercent")[0].GetDecimal(), Math.Round((decimal)carriersA * 100 / count));
        Assert.Equal(expected.GetProperty("carrierDeathPercent")[1].GetDecimal(), Math.Round((decimal)carriersB * 100 / count));

        if (tactic == scenario.Sides.First(s => s.Id == goal.PlayerSide).Tactic)
        {
            Assert.InRange(one / (double)count, goal.WinMaxOneLoss - goal.Tolerance, goal.WinMaxOneLoss + goal.Tolerance);
            Assert.InRange(clean / (double)count, goal.WinNoLoss - goal.Tolerance, goal.WinNoLoss + goal.Tolerance);
        }
    }
}
