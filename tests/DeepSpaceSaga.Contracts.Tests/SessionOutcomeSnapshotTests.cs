using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public class SessionOutcomeSnapshotTests
{
    private static SessionOutcomeSnapshot Defeat => new(SessionOutcomeState.Defeated, "player", "wreck-7", 123, 1450.125, 435037);

    [Fact]
    public void Defeat_roundtrips_with_fractional_death_identity_and_save_denial()
    {
        var snapshot = new AuthoritativeSnapshot(17, 435037, SimulationSpeed.Speed0, [], "player", SimulationTimeMs: 1450, Outcome: Defeat);
        var restored = JsonSerializer.Deserialize<AuthoritativeSnapshot>(JsonSerializer.Serialize(snapshot))!;
        Assert.Equal(Defeat, restored.Outcome);
        Assert.False(restored.EffectiveOutcome.CanSave);
        Assert.Equal("session_defeated", restored.EffectiveOutcome.SaveDeniedReason);
        Assert.Empty(restored.Objects); // Destruction and coalesced event buffers do not erase the outcome.
        Assert.Empty(restored.CommandResults);
        Assert.Empty(restored.CombatJournal);
        var later = restored with { SnapshotSequence = 18, CommandResults = [], CombatJournal = [] };
        Assert.Equal(Defeat, later.EffectiveOutcome);
        Assert.Equal(1450.125, later.EffectiveOutcome.DeathMotionTimeMs);
    }

    [Fact]
    public void Running_and_legacy_snapshot_have_no_death_identity_and_allow_save()
    {
        foreach (var outcome in new[] { null, SessionOutcomeSnapshot.Running })
        {
            var snapshot = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0, [], Outcome: outcome);
            var restored = JsonSerializer.Deserialize<AuthoritativeSnapshot>(JsonSerializer.Serialize(snapshot))!;
            Assert.Equal(SessionOutcomeState.Running, restored.EffectiveOutcome.State);
            Assert.True(restored.EffectiveOutcome.CanSave);
            Assert.Null(restored.EffectiveOutcome.SaveDeniedReason);
            Assert.Null(restored.EffectiveOutcome.DeathEventId);
            Assert.Null(restored.EffectiveOutcome.DeathMotionTimeMs);
            Assert.Null(restored.EffectiveOutcome.DeathGameTimeMs);
            Assert.Null(restored.EffectiveOutcome.WreckObjectId);
        }
    }

    [Fact]
    public void Derived_save_permission_cannot_be_overridden_by_wire_fields()
    {
        string json = JsonSerializer.Serialize(Defeat).Replace("\"CanSave\":false", "\"CanSave\":true");
        var restored = JsonSerializer.Deserialize<SessionOutcomeSnapshot>(json)!;
        Assert.False(restored.CanSave);
        Assert.Equal(CommandReasonCodes.SessionDefeated, restored.SaveDeniedReason);
        Assert.False((Defeat with { State = (SessionOutcomeState)99 }).CanSave);
    }
}
