using System.Text.Json;
namespace DeepSpaceSaga.Contracts.Tests;

public class CountermeasureEventTests
{
    [Fact]
    public void Journal_omits_retired_chance_roll_and_breakdown()
    {
        var op = new WeaponOperatorSnapshot("crew", "Operator", WeaponSkillType.CountermeasureDefense, 50, 55, 55);
        var entry = new CombatJournalEntry(1, 10.5, CombatEventType.Miss, "ship", "torpedo", "pr", 1, 2,
            500, 801, new(op, 5), Damage: 0, LauncherModuleId: "defense-2",
            LaunchMode: LaunchMode.Manual, Accuracy: 55, TargetManeuverability: 5);
        var json = JsonSerializer.Serialize(entry);
        Assert.DoesNotContain("ChanceTenths", json); Assert.DoesNotContain("Roll", json);
        Assert.DoesNotContain("RatingBreakdown", json);
        Assert.Equal(entry with { ChanceTenths = null, Roll = null, RatingBreakdown = null, LaunchMode = null, Accuracy = null, TargetManeuverability = null },
            JsonSerializer.Deserialize<CombatJournalEntry>(json));
    }
    [Fact]
    public void Old_snapshot_has_empty_journal()
    {
        var snapshot = JsonSerializer.Deserialize<AuthoritativeSnapshot>("{\"SnapshotSequence\":1,\"GameTimeMs\":0}");
        Assert.True(snapshot!.CombatJournal.IsDefaultOrEmpty);
    }
}
