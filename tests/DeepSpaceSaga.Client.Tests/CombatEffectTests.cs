using System.Diagnostics;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatEffectTests
{
    private static CombatJournalEntry Blast(long id = 1) => new(id, 0, CombatEventType.TorpedoDetonated, "player", "", "shot-" + id, 100, 100,
        ProjectileKind: ProjectileKind.Torpedo, ProfileId: "slow", BlastRadiusKm: 9);
    private static long Ticks(int ms) => Stopwatch.Frequency * ms / 1000;
    [Fact]
    public void Point_blast_progress_uses_real_time_and_deduplicates_repeated_facts()
    {
        long now = 0; var store = new CombatEffectStore(() => now); var fact = Blast(); store.ReceiveJournal([fact], 0);
        var effect = Assert.Single(store.PointEffects); Assert.Equal(0, store.PointProgress(effect.Entry, effect.Started, now));
        now = Ticks(300); store.ReceiveJournal([fact], 0); Assert.Single(store.PointEffects);
        Assert.Equal(.5, store.PointProgress(effect.Entry, effect.Started, now));
        now = Ticks(600); store.ReceiveJournal([fact], 0); Assert.Empty(store.PointEffects);
        store.ReceiveJournal([fact], now); Assert.Empty(store.PointEffects);
    }
    [Fact]
    public void Session_reset_keeps_load_baseline_silent_and_admits_new_identity()
    {
        var store = new CombatEffectStore(() => 0); store.ResetJournal([Blast()]); store.ReceiveJournal([Blast()], 0); Assert.Empty(store.PointEffects);
        store.ReceiveJournal([Blast(2)], 0); Assert.Equal(2, Assert.Single(store.PointEffects).Entry.EventId);
        store.ResetJournal([]); store.ReceiveJournal([Blast()], 0); Assert.Equal(1, Assert.Single(store.PointEffects).Entry.EventId);
    }
    [Fact]
    public void Delayed_render_does_not_revive_expired_receipt_and_each_fact_keeps_its_own_start()
    {
        long now = Ticks(3000); var store = new CombatEffectStore(() => now); store.ReceiveJournal([Blast()], 0); Assert.Empty(store.PointEffects);
        store.ReceiveJournal([Blast()], now); Assert.Empty(store.PointEffects);
        store.ReceiveJournal([Blast(2)], now); Assert.Single(store.PointEffects);
        now += Ticks(300); store.ReceiveJournal([Blast(2), Blast(3)], now); Assert.Equal(2, store.PointEffects.Count);
        now += Ticks(300); store.ReceiveJournal([], now); Assert.Equal(3, Assert.Single(store.PointEffects).Entry.EventId);
    }
}
