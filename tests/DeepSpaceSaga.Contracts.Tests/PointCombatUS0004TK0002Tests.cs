using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests;

public sealed class PointCombatUS0004TK0002Tests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Counter_blast_destroyed_ids_roundtrip_without_hidden_projectile_data(bool empty)
    {
        var fact = new CombatJournalEntry(3, 1500.5, CombatEventType.CountermissileDetonated, "player", "", "counter-1", 10, 20,
            ProjectileKind: ProjectileKind.Countermissile, ProfileId: "wide", BlastRadiusKm: 1,
            DestroyedProjectileIds: empty ? [] : ["torpedo-1", "torpedo-10", "torpedo-2"]);
        string json = JsonSerializer.Serialize(fact);
        var restored = JsonSerializer.Deserialize<CombatJournalEntry>(json)!;
        Assert.Equal(fact with { DestroyedProjectileIds = null }, restored with { DestroyedProjectileIds = null });
        Assert.Equal(fact.DestroyedProjectileIds!.Value.ToArray(), restored.DestroyedProjectileIds!.Value.ToArray());
        using var document = JsonDocument.Parse(json);
        Assert.Equal(empty ? 0 : 3, document.RootElement.GetProperty("DestroyedProjectileIds").GetArrayLength());
        foreach (string hidden in new[] { "Damage", "HitModuleId", "DetonationX", "DetonationMotionTimeMs", "Roll", "ChanceTenths" })
            Assert.False(document.RootElement.TryGetProperty(hidden, out _), hidden);
    }

    [Fact]
    public void Absent_destroyed_ids_remain_absent_for_old_and_non_counter_facts()
    {
        var fact = new CombatJournalEntry(1, 0, CombatEventType.Launch, "player", "", "torpedo-1", 0, 0);
        string json = JsonSerializer.Serialize(fact);
        Assert.DoesNotContain("DestroyedProjectileIds", json);
        Assert.Equal(fact, JsonSerializer.Deserialize<CombatJournalEntry>(json));
    }
}
