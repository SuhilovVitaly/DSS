using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests;

public sealed class BattleReportSnapshotTests
{
    public static IEnumerable<object[]> KindsAndVariants()
    {
        foreach (var kind in Enum.GetValues<BattleReportKind>())
        {
            if (kind == BattleReportKind.EnemyShipHit)
            {
                yield return [kind, BattleReportVariant.Hit];
                yield return [kind, BattleReportVariant.Destroyed];
            }
            else
                yield return [kind, BattleReportVariant.None];
        }
    }

    [Theory]
    [MemberData(nameof(KindsAndVariants))]
    public void JsonRoundTrip_AllKindsAndVariants(BattleReportKind kind, BattleReportVariant variant)
    {
        var report = new BattleReportSnapshot(long.MaxValue, 1234.125, kind, "torpedo-7/contact/enemy-2",
            new(BattleReportReporterRole.TorpedoOperator, "crew-1", "Оператор", "Images/portrait.png"),
            "player", "Тетрарх", "enemy-2", "Муха-2", "torpedo-7", "launcher-1", variant,
            new("bridge-1", "Мостик", 60, 0, 150, 60));
        var copy = JsonSerializer.Deserialize<BattleReportSnapshot>(JsonSerializer.Serialize(report))!;
        Assert.Equal(report, copy);
        Assert.Equal(1234.125, copy.MotionTimeMs);
        Assert.Equal(long.MaxValue, copy.Sequence);
        Assert.Equal("Муха-2", copy.TargetDisplayName);
    }

    [Fact]
    public void CapturedIdentitySurvivesMissingObjects()
    {
        var report = new BattleReportSnapshot(1, 0, BattleReportKind.EnemyShipHit, "p/t",
            new(BattleReportReporterRole.TorpedoOperator, "crew-1", "Original operator"),
            TargetObjectId: "enemy", TargetDisplayName: "Original target", Variant: BattleReportVariant.Destroyed);
        var batch = new CombatReportBatchSnapshot(0, 1, [report]);
        // Transport contains every displayed fact even in the absence of a world/object collection.
        var encoded = JsonSerializer.Serialize(batch);
        var copy = JsonSerializer.Deserialize<CombatReportBatchSnapshot>(encoded)!;
        Assert.Equal(report, Assert.Single(copy.Reports));
        var replacement = report with { Reporter = new(BattleReportReporterRole.TorpedoOperator, "crew-2", "New operator") };
        Assert.Equal("Original operator", copy.Reports[0].Reporter.DisplayName);
        Assert.NotEqual(replacement.Reporter, copy.Reports[0].Reporter);
    }

    [Theory]
    [InlineData(BattleReportReporterRole.ScannerOperator)]
    [InlineData(BattleReportReporterRole.TorpedoOperator)]
    [InlineData(BattleReportReporterRole.CountermeasureOperator)]
    [InlineData(BattleReportReporterRole.Captain)]
    public void NullableReporterAndDamageArePreserved(BattleReportReporterRole role)
    {
        var report = new BattleReportSnapshot(1, 0, BattleReportKind.EnemyTorpedoLaunch, "p", new(role));
        var copy = JsonSerializer.Deserialize<BattleReportSnapshot>(JsonSerializer.Serialize(report))!;
        Assert.Equal(report, copy);
        Assert.Null(copy.Reporter.CrewId);
        Assert.Null(copy.Reporter.DisplayName);
        Assert.Null(copy.Reporter.PortraitImage);
        Assert.Null(copy.TargetObjectId);
        Assert.Null(copy.TargetDisplayName);
        Assert.Null(copy.Damage);
    }
}
