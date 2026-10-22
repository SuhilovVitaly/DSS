using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatPresentationTests
{
    private static readonly Guid Session = Guid.Parse("99273a7a-720f-4266-a5c7-71b14b24fa30");
    private static AuthoritativeSnapshot State(long time, string id = "ship", SimulationSpeed speed = SimulationSpeed.Speed4) =>
        new((ulong)time, time * 300, speed, [new(id, time, 0, 1, 90)], SimulationTimeMs: time);
    private static CombatPresentationUpdate Update(long sequence, AuthoritativeSnapshot state, bool full = false) =>
        new(Session, sequence, state.MotionTimeMs, [new(state.MotionTimeMs, state)], full ? state : null, false, 0);
    private static long Ticks(long ms) => ms * Stopwatch.Frequency / 1000;

    [Fact]
    public void Delay_is_applied_once_and_scene_never_exceeds_watermark()
    {
        long now = 0;
        var buffer = new SnapshotBuffer(() => now);
        buffer.UpdatePresentation(Update(1, State(0), true));
        now = Ticks(100);
        buffer.UpdatePresentation(Update(2, State(2500)));
        buffer.PreparePresentation();
        Assert.Equal(0, buffer.LatestPrediction!.EffectivePredictionDeltaMs);
        now = Ticks(200);
        buffer.PreparePresentation();
        Assert.Equal(1250, buffer.LatestPrediction!.EffectivePredictionDeltaMs);
        now = Ticks(300);
        buffer.PreparePresentation();
        Assert.Equal(2500, buffer.Presented!.Snapshot.MotionTimeMs);
        Assert.Equal(0, buffer.LatestPrediction!.EffectivePredictionDeltaMs);
        now = Ticks(800);
        buffer.PreparePresentation();
        Assert.Equal(2500, buffer.Presented!.Snapshot.MotionTimeMs);
    }

    [Fact]
    public void Gap_duplicates_and_reordering_do_not_authorize_a_later_watermark()
    {
        long now = 0;
        var buffer = new SnapshotBuffer(() => now);
        buffer.UpdatePresentation(Update(1, State(0), true));
        var third = Update(3, State(5000));
        buffer.UpdatePresentation(third);
        buffer.UpdatePresentation(JsonSerializer.Deserialize<CombatPresentationUpdate>(JsonSerializer.Serialize(third))!);
        now = Ticks(1200);
        buffer.PreparePresentation();
        Assert.Equal(1, buffer.ContiguousPresentationSequence);
        Assert.Equal(0, buffer.Presented!.Snapshot.MotionTimeMs);
        Assert.True(buffer.PresentationPauseRequested);
        buffer.UpdatePresentation(Update(2, State(2500)));
        Assert.Equal(3, buffer.ContiguousPresentationSequence);
        Assert.False(buffer.PresentationPauseRequested);
        now += Ticks(200);
        buffer.PreparePresentation();
        Assert.Equal(5000, buffer.Presented!.Snapshot.MotionTimeMs);
    }

    [Fact]
    public void Receipt_is_immediate_but_damage_reports_and_terminal_state_wait_for_scene()
    {
        long now = 0;
        var buffer = new SnapshotBuffer(() => now);
        buffer.UpdatePresentation(Update(1, State(0), true));
        var receipt = new CommandResult("fire", "ship", "launcher", CombatCommandTypes.Fire,
            CommandResultStatus.Executed, 2500);
        var report = new BattleReportSnapshot(1, 2500, BattleReportKind.PlayerShipDamaged, "contact",
            new(BattleReportReporterRole.Captain));
        var impact = new CombatImpactSnapshot(1, "torpedo", "enemy", "launcher", "ship", "ship", 2500, 7, 9);
        var terminal = State(2500) with
        {
            CommandResults = [receipt],
            Objects = [],
            CurrentSpeed = SimulationSpeed.Speed0,
            Outcome = new(SessionOutcomeState.Defeated, "ship", DeathMotionTimeMs: 2500),
            CombatReports = new(0, 1, [report]),
            CombatImpacts = [impact]
        };
        buffer.UpdatePresentation(Update(2, terminal) with { Frames = [new(2500, terminal, ["ship"])] });
        Assert.Equal(receipt, buffer.FindCommandResult("fire"));
        Assert.Single(buffer.Presented!.Snapshot.Objects);
        Assert.Empty(buffer.DrainCombatReports());
        Assert.Equal(SessionOutcomeState.Running, buffer.Presented.Snapshot.EffectiveOutcome.State);
        now = Ticks(250);
        buffer.PreparePresentation();
        Assert.Empty(buffer.Presented!.Snapshot.Objects);
        Assert.Equal(SessionOutcomeState.Defeated, buffer.Presented.Snapshot.EffectiveOutcome.State);
        Assert.Equal(impact, Assert.Single(buffer.Presented.Snapshot.CombatImpacts));
        Assert.Equal(report, Assert.Single(buffer.DrainCombatReports()));
        Assert.Equal(SimulationSpeed.Speed0, buffer.LatestPrediction!.CurrentSpeed);
    }

    [Fact]
    public void Resume_revision_at_paused_boundary_unfreezes_scene_and_old_session_is_ignored()
    {
        long now = 0;
        var buffer = new SnapshotBuffer(() => now);
        buffer.UpdatePresentation(Update(1, State(0, speed: SimulationSpeed.Speed0), true));
        buffer.UpdatePresentation(Update(2, State(0)));
        now = Ticks(150);
        buffer.PreparePresentation();
        buffer.UpdatePresentation(Update(3, State(2500)));
        now += Ticks(100);
        buffer.PreparePresentation();
        Assert.Equal(2500, buffer.Presented!.Snapshot.MotionTimeMs);
        var replacement = Update(1, State(0, "new"), true) with { SessionId = Guid.NewGuid() };
        buffer.UpdatePresentation(replacement);
        buffer.UpdatePresentation(Update(4, State(7500), true));
        Assert.Equal("new", Assert.Single(buffer.Presented!.Snapshot.Objects).ObjectId);
    }

    [Fact]
    public void Overflow_is_explicit_instead_of_dropping_a_terminal_event()
    {
        var buffer = new SnapshotBuffer(() => 0);
        buffer.UpdatePresentation(Update(1, State(0), true));
        for (int i = 2; i <= 129; i++) buffer.UpdatePresentation(Update(i + 1, State(i)));
        Assert.Throws<InvalidOperationException>(() => buffer.UpdatePresentation(Update(200, State(200))));
    }

    [Fact]
    public void Effects_start_at_presentation_and_retain_real_time_lifetime()
    {
        long now = Ticks(5000);
        var store = new CombatEffectStore(() => now) { StartOnPresentation = true };
        var fact = new CombatJournalEntry(1, 100, CombatEventType.TorpedoDetonated, "ship", "", "torpedo", 7, 9,
            ProjectileKind: ProjectileKind.Torpedo, ProfileId: "slow", BlastRadiusKm: 9);
        store.ReceiveJournal([fact], 0); var effect = Assert.Single(store.PointEffects);
        Assert.Equal(0, store.PointProgress(effect.Entry, effect.Started, now));
        now += Ticks(599); store.ReceiveJournal([fact], 0); Assert.Single(store.PointEffects);
        now += Ticks(1); store.ReceiveJournal([fact], 0); Assert.Empty(store.PointEffects);
    }

    [Fact]
    public void Recovery_consumes_all_events_and_restores_one_delay_offset()
    {
        long now = 0;
        var buffer = new SnapshotBuffer(() => now);
        buffer.UpdatePresentation(Update(1, State(0), true));
        buffer.UpdatePresentation(Update(2, State(2500)));
        now = Ticks(500);
        buffer.PreparePresentation();
        Assert.Equal(2500, buffer.Presented!.Snapshot.MotionTimeMs);
        now = Ticks(800);
        buffer.UpdatePresentation(Update(3, State(20000)));
        buffer.PreparePresentation();
        var prediction = buffer.LatestPrediction!;
        Assert.Equal(16250, prediction.BufferedSnapshot.Snapshot.MotionTimeMs + prediction.EffectivePredictionDeltaMs);
    }

    [Fact]
    public void Event_overflow_cannot_be_hidden_by_a_small_number_of_frames()
    {
        var buffer = new SnapshotBuffer(() => 0);
        buffer.UpdatePresentation(Update(1, State(0), true));
        var impacts = Enumerable.Range(1, SnapshotBuffer.PresentationHistoryLimit + 1).Select(id =>
            new CombatImpactSnapshot(id, "torpedo-" + id, "owner", "module", "target", "target", 1, 0, 0)).ToImmutableArray();
        Assert.Throws<InvalidOperationException>(() => buffer.UpdatePresentation(Update(2, State(1) with { CombatImpacts = impacts })));
    }

    [Fact]
    public void Compact_omission_retains_static_target_until_explicit_removal_is_presented()
    {
        long now = 0;
        var buffer = new SnapshotBuffer(() => now);
        var asteroid = new ObjectMotionSnapshot("asteroid", 10, 20, 0, 0);
        var bootstrap = State(0) with { Objects = [asteroid] };
        buffer.UpdatePresentation(Update(1, bootstrap, true));
        var targeted = State(100) with { Objects = [asteroid with { X = 12 }] };
        buffer.UpdatePresentation(Update(2, targeted));
        var omitted = State(200) with { Objects = [] };
        buffer.UpdatePresentation(Update(3, omitted));
        Assert.Equal(12, Assert.Single(buffer.AuthoritativeLatest!.Objects).X);
        now = Ticks(250);
        buffer.PreparePresentation();
        Assert.Equal(12, Assert.Single(buffer.Presented!.Snapshot.Objects).X);
        var removed = State(300) with { Objects = [] };
        var deletion = Update(4, removed) with { Frames = [new(300, removed, ["asteroid"])] };
        deletion = JsonSerializer.Deserialize<CombatPresentationUpdate>(JsonSerializer.Serialize(deletion))!;
        buffer.UpdatePresentation(deletion);
        Assert.Empty(buffer.AuthoritativeLatest!.Objects);
        Assert.Single(buffer.Presented.Snapshot.Objects);
        now += Ticks(200);
        buffer.PreparePresentation();
        Assert.Empty(buffer.Presented.Snapshot.Objects);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Rejection_bypasses_sequence_gap_without_advancing_scene_and_is_session_scoped(bool full)
    {
        var buffer = new SnapshotBuffer(() => 0);
        buffer.UpdatePresentation(Update(1, State(0), true));
        var rejection = new CommandResult("rejected", "ship", "launcher", CombatCommandTypes.Fire,
            CommandResultStatus.Rejected, 200, CommandReasonCodes.TargetDestroyed);
        var state = State(200) with { CommandResults = [rejection] };
        var third = Update(3, full ? State(200) : state) with { FullSnapshot = full ? state : null };
        buffer.UpdatePresentation(third);
        buffer.UpdatePresentation(JsonSerializer.Deserialize<CombatPresentationUpdate>(JsonSerializer.Serialize(third))!);
        Assert.Equal(rejection, buffer.FindCommandResult("rejected"));
        Assert.Equal(1, buffer.ContiguousPresentationSequence);
        Assert.Equal(0, buffer.AuthoritativeLatest!.MotionTimeMs);
        Assert.Equal(0, buffer.Presented!.Snapshot.MotionTimeMs);
        buffer.UpdatePresentation(Update(2, State(100)));
        Assert.Equal(rejection, buffer.FindCommandResult("rejected"));
        buffer.UpdatePresentation(Update(1, State(0), true) with { SessionId = Guid.NewGuid() });
        buffer.UpdatePresentation(third);
        Assert.Null(buffer.FindCommandResult("rejected"));
        buffer.UpdatePresentation(third with { SessionId = Guid.NewGuid(), FullSnapshot = null });
        Assert.Null(buffer.FindCommandResult("rejected"));
    }
}
