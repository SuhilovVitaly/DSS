using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public class PointCombatUS0020TK0005Tests
{
    [Fact]
    public void Reload_event_preserves_enum_ids_and_roundtrips_without_projectile()
    {
        Assert.Equal(17, (int)CombatEventType.ManuallyDetonated);
        Assert.Equal(6, (int)BattleReportKind.PlayerShipDamaged);
        var report = new BattleReportSnapshot(1, 10000, BattleReportKind.PlayerTorpedoReloadCompleted,
            "reload:tube:10000", new(BattleReportReporterRole.TorpedoOperator), LauncherModuleId: "tube", LauncherDisplayName: "Tube A");
        Assert.Equal(report, JsonSerializer.Deserialize<BattleReportSnapshot>(JsonSerializer.Serialize(report)));
        Assert.Null(report.ProjectileObjectId);
    }
}
