using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests;

public sealed class CombatReportBatchTests
{
    [Fact]
    public void LegacySnapshotHasNoReports()
    {
        const string json = """{"SnapshotSequence":1,"GameTimeMs":0,"CurrentSpeed":0,"Objects":[]}""";
        var copy = JsonSerializer.Deserialize<AuthoritativeSnapshot>(json)!;
        Assert.Null(copy.CombatReports);
        Assert.Null(JsonSerializer.Deserialize<AuthoritativeSnapshot>(JsonSerializer.Serialize(copy))!.CombatReports);
    }

    [Fact]
    public void BatchBaselineAndLatestRoundTrip()
    {
        foreach (var batch in new CombatReportBatchSnapshot[]
        {
            new(7, 7), new(7, 7, []),
            new(7, 8, [new(8, 0.125, BattleReportKind.EnemyInRange, "range/enemy", new(BattleReportReporterRole.ScannerOperator))])
        })
        {
            var snapshot = new AuthoritativeSnapshot(1, 300000, SimulationSpeed.Speed0, [], CombatReports: batch);
            var encoded = JsonSerializer.Serialize(snapshot);
            var copy = JsonSerializer.Deserialize<AuthoritativeSnapshot>(encoded)!;
            Assert.Equal(encoded, JsonSerializer.Serialize(copy));
            Assert.Equal(7, copy.CombatReports!.BaselineSequence);
            Assert.Equal(batch.LatestSequence, copy.CombatReports.LatestSequence);
            Assert.False(copy.CombatReports.Reports.IsDefault);
            if (batch.LatestSequence == 8)
                Assert.Equal(0.125, Assert.Single(copy.CombatReports.Reports).MotionTimeMs);
            else
                Assert.Empty(copy.CombatReports.Reports);
        }
    }

    [Fact]
    public void ReportSequenceIsIndependentFromSnapshotSequence()
    {
        var report = new BattleReportSnapshot(501, 1234.5, BattleReportKind.PlayerTorpedoLaunch,
            "torpedo-11", new(BattleReportReporterRole.TorpedoOperator));
        var snapshot = new AuthoritativeSnapshot(2, 900000, SimulationSpeed.Speed0, [],
            CombatReports: new(500, 501, [report]));
        var copy = JsonSerializer.Deserialize<AuthoritativeSnapshot>(JsonSerializer.Serialize(snapshot))!;
        Assert.Equal(2UL, copy.SnapshotSequence);
        Assert.Equal(501, Assert.Single(copy.CombatReports!.Reports).Sequence);
        Assert.Equal(1234.5, copy.CombatReports.Reports[0].MotionTimeMs);
        Assert.Empty(copy.CombatJournal);
    }
}
