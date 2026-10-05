using System.Text.Json;
namespace DeepSpaceSaga.Contracts.Tests;

public class CountermeasureEventTests
{
    [Fact]
    public void Journal_entry_roundtrips_chance_roll_and_breakdown()
    {
        var op = new WeaponOperatorSnapshot("crew", "Operator", WeaponSkillType.CountermeasureDefense, 50, 30, 30);
        var entry = new CombatJournalEntry(1, 10.5, CombatEventType.Miss, "ship", "torpedo", "pr", 1, 2,
            500, 801, new(op, 30));
        Assert.Equal(entry, JsonSerializer.Deserialize<CombatJournalEntry>(JsonSerializer.Serialize(entry)));
    }
    [Fact]
    public void Old_snapshot_has_empty_journal()
    {
        var snapshot = JsonSerializer.Deserialize<AuthoritativeSnapshot>("{\"SnapshotSequence\":1,\"GameTimeMs\":0}");
        Assert.True(snapshot!.CombatJournal.IsDefaultOrEmpty);
    }
}
