using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public class PointCombatUS0017TK0001Tests
{
    [Theory]
    [InlineData(CombatEventType.TorpedoDetonated, ProjectileKind.Torpedo, ProjectileHitResult.Core)]
    [InlineData(CombatEventType.CountermissileDetonated, ProjectileKind.Countermissile, ProjectileHitResult.Miss)]
    [InlineData(CombatEventType.ShotDown, ProjectileKind.Torpedo, ProjectileHitResult.ShotDown)]
    [InlineData(CombatEventType.ManuallyDetonated, ProjectileKind.Torpedo, ProjectileHitResult.Edge)]
    public void Combat_events_roundtrip_and_redact_enemy_details(CombatEventType type, ProjectileKind kind, ProjectileHitResult result)
    {
        // These are already projected public facts; Engine masking is tested at its own boundary.
        var fact = new CombatJournalEntry(81, 1234.625, type, "player", "enemy", "projectile-1", -12.5, 99.25,
            ProjectileKind: kind, ProfileId: "slow", BlastRadiusKm: 9, HitResult: result);
        var report = new BattleReportSnapshot(25, fact.MotionTimeMs, BattleReportKind.EnemyShipHit,
            "projectile-1/enemy", new(BattleReportReporterRole.TorpedoOperator), TargetObjectId: "enemy",
            ProjectileObjectId: "projectile-1", X: fact.X, Y: fact.Y, ProjectileKind: kind,
            ProfileId: "slow", BlastRadiusKm: 9, HitResult: result);
        var state = new AuthoritativeSnapshot(7, 9000000, SimulationSpeed.Speed1, [],
            CombatJournal: [fact], CombatReports: new(20, 25, [report]), SimulationTimeMs: 1300);
        var envelope = new CombatPresentationUpdate(Guid.NewGuid(), 31, 1300,
            [new(fact.MotionTimeMs, state, ["projectile-1"])], null, false, 42);

        var restored = JsonSerializer.Deserialize<CombatPresentationUpdate>(JsonSerializer.Serialize(envelope))!;
        var frame = Assert.Single(restored.Frames);
        Assert.Equal(fact, Assert.Single(frame.State.CombatJournal));
        var batch = Assert.IsType<CombatReportBatchSnapshot>(frame.State.CombatReports);
        Assert.Equal(report, Assert.Single(batch.Reports));
        Assert.Equal(1234.625, frame.MotionTimeMs);
        Assert.Equal("projectile-1", Assert.Single(frame.RemovedObjectIds));
        Assert.Equal(20, batch.BaselineSequence);
        using var journalJson = JsonDocument.Parse(JsonSerializer.Serialize(fact));
        using var reportJson = JsonDocument.Parse(JsonSerializer.Serialize(report));
        foreach (string hidden in new[] { "Damage", "HitModuleId", "ArmorBefore", "ArmorAfter", "StructureBefore",
            "StructureAfter", "ChanceTenths", "Roll", "Attack", "Defense", "LauncherModuleId", "DetonationX", "DetonationMotionTimeMs" })
        {
            Assert.False(journalJson.RootElement.TryGetProperty(hidden, out _), hidden);
            Assert.False(reportJson.RootElement.TryGetProperty(hidden, out _), hidden);
        }
    }

    [Fact]
    public void Own_damage_details_survive_the_same_transport_without_inventing_chance()
    {
        var fact = new CombatJournalEntry(82, 1400.125, CombatEventType.Hit, "enemy", "player", "projectile-2", 1, 2,
            Damage: 40, HitModuleId: "bridge", ArmorBefore: 60, ArmorAfter: 20,
            StructureBefore: 150, StructureAfter: 150, HitResult: ProjectileHitResult.Edge);
        var report = new BattleReportSnapshot(26, fact.MotionTimeMs, BattleReportKind.PlayerShipDamaged,
            "projectile-2/player", new(BattleReportReporterRole.Captain), TargetObjectId: "player",
            Damage: new("bridge", "Bridge", 60, 20, 150, 150), HitResult: ProjectileHitResult.Edge);
        var restored = JsonSerializer.Deserialize<CombatJournalEntry>(JsonSerializer.Serialize(fact))!;
        var restoredReport = JsonSerializer.Deserialize<BattleReportSnapshot>(JsonSerializer.Serialize(report))!;
        Assert.Equal(fact, restored);
        Assert.Equal(report, restoredReport);
        Assert.Null(restored.ChanceTenths);
        Assert.Null(restored.Roll);
        Assert.Equal("bridge", restoredReport.Damage!.ModuleId);
    }

    [Fact]
    public void New_event_values_do_not_renumber_old_journal_or_E01_E07_reports()
    {
        Assert.Equal(13, (int)CombatEventType.LauncherBreakdown);
        Assert.Equal(14, (int)CombatEventType.TorpedoDetonated);
        Assert.Equal(15, (int)CombatEventType.CountermissileDetonated);
        Assert.Equal(16, (int)CombatEventType.ShotDown);
        Assert.Equal(17, (int)CombatEventType.ManuallyDetonated);
        Assert.Equal(7, Enum.GetValues<BattleReportKind>().Length);
    }
}
