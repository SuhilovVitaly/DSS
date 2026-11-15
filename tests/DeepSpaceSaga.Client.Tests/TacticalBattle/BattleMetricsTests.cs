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
    public void ClientTestsExportBattleMetricsOutsideTheUserProfile()
    {
        // US-0022-TK-0013: Client tests never write battle reports into %LOCALAPPDATA%/DeepSpaceSaga/BattleMetrics.
        string profile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeepSpaceSaga");
        string directory = Path.GetFullPath(TacticalBattleMetrics.DirectoryPath);
        Assert.False(directory.StartsWith(Path.GetFullPath(profile), StringComparison.OrdinalIgnoreCase), directory);
        if (Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is not { Length: > 0 } root || !Path.IsPathFullyQualified(root))
        {
            Assert.Equal(TestAssemblySetup.BattleMetricsDirectory, TacticalBattleMetrics.DirectoryPath);
            Assert.StartsWith(Path.GetFullPath(Path.GetTempPath()), directory, StringComparison.OrdinalIgnoreCase);
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
            string path = TacticalBattleMetrics.Write(state, directory: directory);
            Assert.Equal(TacticalBattleMetrics.ReportPath(state.BattleId, directory), path);
            AssertComparableReport(path, state);
            Assert.True(engine.ExitTacticalBattle(new("exit-accuracy-" + seed, state.BattleId)).Accepted);
        }
    }

    [Fact]
    public void ExportKeepsOnlyTheNewestReportsAndTheirTutorialJournals()
    {
        // US-0022-TK-0013: reports do not grow without bound — the oldest beyond MaxReports are pruned with their journals.
        string directory = Path.Combine(TacticalBattleMetrics.DirectoryPath, "rotation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var origin = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            for (int i = 0; i < TacticalBattleMetrics.MaxReports; i++)
            {
                string old = TacticalBattleMetrics.ReportPath("old-" + i, directory);
                File.WriteAllText(old, "{}");
                File.WriteAllText(Path.ChangeExtension(old, ".tutorial.json"), "{}");
                File.SetLastWriteTimeUtc(old, origin.AddMinutes(i));
            }
            string unrelated = Path.Combine(directory, "notes.json");
            File.WriteAllText(unrelated, "{}");
            File.SetLastWriteTimeUtc(unrelated, origin.AddDays(-1));

            using var engine = SimulationEngine.CreateFromSettingsFile(Path.Combine(AppContext.BaseDirectory, "Settings.json"));
            engine.SetSpeed(SimulationSpeed.Speed0);
            var state = engine.StartTacticalBattle(new("rotation-sample", 1, ScenarioId: "initial")).State!;
            string path = TacticalBattleMetrics.Write(state, directory: directory);
            Assert.True(engine.ExitTacticalBattle(new("exit-rotation", state.BattleId)).Accepted);

            Assert.True(File.Exists(path));
            using (var report = JsonDocument.Parse(File.ReadAllText(path)))
                Assert.Equal("active", report.RootElement.GetProperty("status").GetString());
            string oldest = TacticalBattleMetrics.ReportPath("old-0", directory);
            Assert.False(File.Exists(oldest));
            Assert.False(File.Exists(Path.ChangeExtension(oldest, ".tutorial.json")));
            string kept = TacticalBattleMetrics.ReportPath("old-1", directory);
            Assert.True(File.Exists(kept));
            Assert.True(File.Exists(Path.ChangeExtension(kept, ".tutorial.json")));
            Assert.True(File.Exists(unrelated));
            Assert.Equal(TacticalBattleMetrics.MaxReports,
                Directory.EnumerateFiles(directory, "*.json").Count(file => !file.EndsWith(".tutorial.json", StringComparison.Ordinal)
                    && file != unrelated));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    private static void AssertComparableReport(string path, TacticalBattleSnapshot state)
    {
        using var report = JsonDocument.Parse(File.ReadAllText(path));
        var root = report.RootElement;
        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(state.BattleId, root.GetProperty("battleId").GetString());
        Assert.Equal(state.Seed, root.GetProperty("seed").GetInt64());
        Assert.Equal("initial", root.GetProperty("scenarioId").GetString());
        Assert.Equal("completed", root.GetProperty("status").GetString());
        Assert.Equal(state.Outcome!.Kind.ToString(), root.GetProperty("outcome").GetString());
        var totals = root.GetProperty("totals");
        var shots = root.GetProperty("shots");
        Assert.True(shots.GetArrayLength() > 0);
        Assert.Equal(shots.GetArrayLength(), totals.GetProperty("shots").GetInt32());
        Assert.Equal(shots.EnumerateArray().Sum(shot => shot.GetProperty("chance").GetDouble()),
            totals.GetProperty("expectedHits").GetDouble(), 8);
        Assert.Equal(state.Tactics.Length, root.GetProperty("sides").GetArrayLength());
        foreach (var shot in shots.EnumerateArray())
            Assert.Equal(JsonValueKind.Object, shot.GetProperty("resolved").ValueKind);
    }
}
