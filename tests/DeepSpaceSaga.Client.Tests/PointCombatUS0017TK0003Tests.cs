using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0017TK0003Tests
{
    [Fact]
    public async Task Point_payload_survives_local_session_and_explicit_removal()
    {
        await using var fixture = new PointCombatSessionFixture();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var connection = fixture.Connection;
        await using var reader = connection.ReadCombatUpdatesAsync(timeout.Token).GetAsyncEnumerator();
        var updates = new List<CombatPresentationUpdate>();
        async Task<CombatPresentationUpdate> Until(Func<CombatPresentationUpdate, bool> predicate)
        {
            while (await reader.MoveNextAsync())
            {
                var update = reader.Current;
                if (updates.Count > 0) Assert.Equal(updates[^1].Sequence + 1, update.Sequence);
                updates.Add(update);
                await connection.ReportPresentationProgressAsync(update.SessionId, update.Sequence, false, timeout.Token);
                if (predicate(update)) return update;
            }
            throw new InvalidOperationException("Local transport ended before the expected actual point state.");
        }

        await Until(u => u.FullSnapshot is not null);
        var launch = PointCombatSessionFixture.Launch();
        Assert.Equal(new CommandAcceptance(launch.CommandId, true), await connection.SubmitCommandAsync(launch, timeout.Token));
        var launched = await Until(u => u.Frames.Any(f => f.State.CommandResults.Any(r => r.CommandId == launch.CommandId)));
        var scene = launched.Frames[^1].State;
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(scene.CommandResults, r => r.CommandId == launch.CommandId).Status);
        Assert.Equal(CombatEncounterState.Active, scene.Encounter!.State);
        var own = Assert.Single(scene.Objects, o => o.OwnProjectile is not null);
        Assert.Equal(600, own.OwnProjectile!.DetonationX);
        Assert.Equal(3750, own.OwnProjectile.DetonationMotionTimeMs);
        var foreign = Assert.Single(scene.Objects, o => o.ObservedProjectile is not null);
        Assert.Null(foreign.OwnProjectile);
        Assert.Contains(scene.CombatReports!.Reports, r => r.Kind == BattleReportKind.PlayerTorpedoLaunch);
        var rejected = await connection.SetPresentationSpeedAsync(SimulationSpeed.Speed4, timeout.Token);
        Assert.Equal(CommandReasonCodes.CombatSpeedLocked, rejected.ReasonCode);
        Assert.Equal(SimulationSpeed.Speed0, rejected.CurrentSpeed);
        Assert.Null((await connection.SetPresentationSpeedAsync(SimulationSpeed.Speed1, timeout.Token)).ReasonCode);
        fixture.SetReal(2000);
        await Until(u => u.ConfirmedMotionTimeMs >= 500);
        Assert.Equal(SimulationSpeed.Speed0, (await connection.SetPresentationSpeedAsync(SimulationSpeed.Speed0, timeout.Token)).CurrentSpeed);

        var detonate = new PlayerCommand("point-detonate", 2, PointCombatSessionFixture.Player, PointCombatSessionFixture.Torpedo,
            CombatCommandTypes.Detonate, own.ObjectId);
        Assert.True((await connection.SubmitCommandAsync(detonate, timeout.Token)).Accepted);
        var removed = await Until(u => u.Frames.Any(f => f.RemovedObjectIds.Contains(own.ObjectId)));
        var removalFrame = Assert.Single(removed.Frames, f => f.RemovedObjectIds.Contains(own.ObjectId));
        Assert.DoesNotContain(removalFrame.State.Objects, o => o.ObjectId == own.ObjectId);
        Assert.Contains(removalFrame.State.CombatJournal, e => e.Type == CombatEventType.ManuallyDetonated && e.ProjectileObjectId == own.ObjectId);
        var receipt = updates.SelectMany(u => u.Frames).SelectMany(f => f.State.CommandResults).FirstOrDefault(r => r.CommandId == detonate.CommandId);
        if (receipt is null)
        {
            await Until(u => u.Frames.Any(f => f.State.CommandResults.Any(r => r.CommandId == detonate.CommandId)));
            receipt = updates.SelectMany(u => u.Frames).SelectMany(f => f.State.CommandResults).First(r => r.CommandId == detonate.CommandId);
        }
        Assert.Equal(CommandResultStatus.Executed, receipt.Status);
        Assert.True((await connection.SubmitCommandAsync(detonate, timeout.Token)).Accepted);
        var replayed = await Until(u => u.Frames.Any(f => f.State.CommandResults.Any(r => r.CommandId == detonate.CommandId)));
        var final = replayed.Frames[^1].State;
        Assert.DoesNotContain(final.CombatJournal, e => e.Type == CombatEventType.ManuallyDetonated);
        Assert.Single(updates.SelectMany(u => u.Frames).SelectMany(f => f.State.CombatJournal)
            .Where(e => e.Type == CombatEventType.ManuallyDetonated && e.ProjectileObjectId == own.ObjectId).DistinctBy(e => e.EventId));
        Assert.DoesNotContain(final.Objects, o => o.ObjectId == own.ObjectId);
        Assert.Equal(1, final.Encounter!.Stats.TorpedoesLaunched);
        Assert.DoesNotContain(final.CombatReports!.Reports, r => r.Kind == BattleReportKind.PlayerTorpedoLaunch);
        var mailbox = new SnapshotMailbox();
        mailbox.Publish(scene);
        mailbox.Publish(final);
        mailbox.Complete();
        await foreach (var coalesced in mailbox.ReadAllAsync(timeout.Token))
        {
            Assert.DoesNotContain(coalesced.Objects, o => o.ObjectId == own.ObjectId);
            Assert.Equal(final.Encounter, coalesced.Encounter);
            Assert.Contains(coalesced.CommandResults, r => r.CommandId == launch.CommandId);
            Assert.Contains(coalesced.CommandResults, r => r.CommandId == detonate.CommandId);
            Assert.Single(coalesced.CombatReports!.Reports, r => r.Kind == BattleReportKind.PlayerTorpedoLaunch);
            Assert.Equal(coalesced.CombatReports.Reports.Select(r => r.Sequence).Order(), coalesced.CombatReports.Reports.Select(r => r.Sequence));
        }
        // FullSnapshot is deliberately unused after bootstrap: explicit removal and facts must survive compact delivery.
        Assert.All(updates.SelectMany(u => u.Frames).SelectMany(f => f.State.Objects).Where(o => o.ObservedProjectile is not null),
            o => { Assert.Null(o.OwnProjectile); Assert.Null(o.Torpedo); Assert.Null(o.Countermeasure); });
    }
}
