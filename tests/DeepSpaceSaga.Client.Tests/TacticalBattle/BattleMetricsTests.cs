using System.Text.Json;
using System.Collections.Immutable;
using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class BattleMetricsTests
{
    [Fact]
    public async Task CompletedBattleExportsResolvedShotsAndTotalsFromTheSession()
    {
        await using var scene = await Ep0019Scenes.Create("5-outcome");
        var state = scene.Screen.Flow.State!;
        var path = TacticalBattleMetrics.ReportPath(state.BattleId, TacticalBattleMetrics.DirectoryPath);
        using var report = JsonDocument.Parse(File.ReadAllText(path));
        var totals = report.RootElement.GetProperty("totals");
        var shots = report.RootElement.GetProperty("shots");
        Assert.Equal("completed", report.RootElement.GetProperty("status").GetString());
        Assert.Equal(state.Outcome!.Statistics.Shots, totals.GetProperty("shots").GetInt64());
        Assert.Equal(state.Outcome.Statistics.Hits, totals.GetProperty("hits").GetInt64());
        Assert.Equal(50, totals.GetProperty("hitPercent").GetDouble(), 3);
        Assert.Equal(totals.GetProperty("shots").GetInt32(), shots.GetArrayLength());
        Assert.Equal(shots.EnumerateArray().Sum(shot => shot.GetProperty("chance").GetDouble()),
            totals.GetProperty("expectedHits").GetDouble(), 8);
        foreach (var entry in shots.EnumerateArray())
        {
            var shot = entry.GetProperty("resolved");
            var factors = shot.GetProperty("factors");
            double conditional = factors.EnumerateObject().Where(term => term.Name != "absorption")
                .Sum(term => term.Value.GetDouble()) / 100;
            Assert.Equal(shot.GetProperty("chance").GetDouble(), conditional * (1 - factors.GetProperty("absorption").GetDouble()), 10);
        }
    }

    [Fact]
    public void HeadlessDefaultTargetBattlesProduceComparableAccuracyReports()
    {
        string directory = Path.Combine(TacticalBattleMetrics.DirectoryPath, "headless-initial");
        using var engine = SimulationEngine.CreateFromSettingsFile(Path.Combine(AppContext.BaseDirectory, "Settings.json"));
        engine.SetSpeed(SimulationSpeed.Speed0);
        for (int seed = 1; seed <= 24; seed++)
        {
            var state = engine.StartTacticalBattle(new("accuracy-sample-" + seed, seed, ScenarioId: "initial")).State!;
            for (int turn = 0; turn < 200 && state.Outcome is null; turn++)
            {
                var response = state.Phase == TacticalBattlePhase.Move
                    ? engine.MoveTacticalBattle(new($"move-{seed}-{turn}", state.BattleId, state.Revision))
                    : engine.FireTacticalBattle(new($"fire-{seed}-{turn}", state.BattleId, state.Revision,
                        state.Targets.Select(group => new TacticalFireTarget(group.DroneId, group.DefaultTargetId)).ToImmutableArray()));
                Assert.True(response.Accepted);
                state = response.State!;
            }
            Assert.NotNull(state.Outcome);
            TacticalBattleMetrics.Write(state, directory: directory);
            Assert.True(engine.ExitTacticalBattle(new("exit-accuracy-" + seed, state.BattleId)).Accepted);
        }
    }
}
