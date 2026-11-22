using System.Globalization;
using DeepSpaceSaga.Client.UI.Demo;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Contracts;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.Demo;

/// <summary>
/// EP-0034-US-0002-TK-0003: the Contracts screen shows the passenger contracts from the authoritative snapshot —
/// people aboard (life support), route, payout, deadline, delivery state, the confirmed boarding after an accepted
/// passenger offer and the confirmed delivery with its payout — through the real session transport
/// (LocalGameSessionConnection + GameSessionHandle, as SkiaWindow wires it). Refusals are explained, nothing is
/// resubmitted on reopen, and a delivery confirmed after Save/Load is shown once.
/// </summary>
public sealed class DemoEp0034Us0002Tk0003Tests
{
    private const string Ship = "SPC-0001";
    private const string Start = "SPC-0002";
    private const string FirstOffer = "jobs:SPC-0002:0:0";
    private static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "Settings.json");
    private static string DemoScenarioPath => Path.Combine(AppContext.BaseDirectory, "Scenarios", "Demo", "scenario.json");

    /// <summary>Real demo New Game docked at <paramref name="station"/> through the production Load path.</summary>
    private static SimulationEngine Docked(SimulationEngine? from = null, string station = Start)
    {
        var engine = from ?? EngineContentLoader.CreateEngineFromScenarioFile(SettingsPath, DemoScenarioPath);
        engine.SetSpeed(SimulationSpeed.Speed0);
        var save = engine.CaptureSaveState();
        var ship = save.GameState.SpaceObjects.Single(s => s.ObjectId == Ship) with
        {
            IsDocked = true, DockedStationObjectId = station, FirstPortFeeGameTimeMs = 0, NextPortFeeDueGameTimeMs = GameCalendar.DayMs
        };
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save with
        {
            GameState = save.GameState with { SpaceObjects = save.GameState.SpaceObjects.Select(s => s.ObjectId == Ship ? ship : s).ToArray() }
        }), true), isSave: true);
        engine.SetSpeed(SimulationSpeed.Speed0);
        return engine;
    }

    private static void Render(IScreen screen, bool activate = true)
    {
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        if (activate) screen.OnActivated();
        screen.Render(canvas, 1920, 1080);
    }

    private static async Task WaitFor(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "timed out waiting for the session");
            await Task.Delay(50);
        }
    }

    private static string Name(GameSessionHandle handle, string id) =>
        handle.Buffer.Latest!.Snapshot.Objects.Single(o => o.ObjectId == id).DisplayName!;

    [Theory]
    [InlineData("English")]
    [InlineData("Russian")]
    public async Task ConfirmedSnapshot_RendersOutcome(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);
        using var engine = Docked();
        JobOffer offer;
        await using (var connection = new LocalGameSessionConnection(engine))
        await using (var handle = new GameSessionHandle(connection))
        {
            await WaitFor(() => handle.DemoCampaign?.Transport?.PassengerContracts is not null && handle.Buffer.Latest is not null);
            // Same wiring as SkiaWindow.OpenContractsAsync.
            var screen = new ContractsScreen(handle.Buffer, () => handle.DemoCampaign, command => handle.ExecuteDemoCampaignAsync(command));
            Render(screen);
            Assert.Equal(Localization.Get("Demo.PassengerContracts.Title"), screen.PassengersView.Title);
            Assert.Equal(Localization.Get("Demo.PassengerContracts.Empty"), screen.PassengersView.Empty);
            Assert.Null(screen.PassengerFeedback);

            // Before accepting, the offer shows route, people, deadline and payout from the engine (AC-01).
            offer = handle.DemoCampaign!.Transport!.StationJobs!.Offers.Single(o => o.Id == FirstOffer);
            var row = screen.JobsView.Offers.Single(r => r.OfferId == FirstOffer);
            Assert.Contains(Name(handle, offer.DestinationStationId), row.Headline);
            Assert.Contains(offer.Count.ToString(CultureInfo.InvariantCulture), row.Headline);
            Assert.Contains(offer.Payout.ToString(CultureInfo.InvariantCulture), row.Terms);
            Assert.Contains(StationJobsPresentation.Duration(offer.DeadlineDurationMs), row.Terms);

            // Accept with the real button; the boarding text comes from the confirmed snapshot, not the response.
            var button = screen.AcceptButtons.Single(b => b.OfferId == FirstOffer).Rect;
            Assert.Equal(ScreenEvent.None, screen.OnMouseDown(button.MidX, button.MidY));
            await WaitFor(() => !screen.IsAcceptPending);
            await WaitFor(() => handle.DemoCampaign!.Transport!.PassengerContracts!.Contracts.Length == 1);
            Render(screen, activate: false);
            var contract = handle.DemoCampaign!.Transport!.PassengerContracts!.Contracts[0];
            Assert.Equal(string.Format(Localization.Get("Demo.PassengerContracts.Boarded"), offer.Count, Name(handle, offer.DestinationStationId),
                StationJobsPresentation.Time(contract.DeadlineGameTimeMs)), screen.PassengerFeedback);
            var view = screen.PassengersView;
            Assert.Null(view.Empty);
            Assert.Equal(string.Format(Localization.Get("Demo.PassengerContracts.Aboard"), offer.Count), view.Summary); // AC-02
            string line = Assert.Single(view.Lines);
            Assert.EndsWith(Localization.Get("Demo.PassengerContracts.State.aboard"), line);
            Assert.Contains(Name(handle, offer.DestinationStationId), line);
            Assert.Contains(offer.Payout.ToString(CultureInfo.InvariantCulture), line);
            Assert.DoesNotContain("SPC-", line);
            Assert.DoesNotContain("pax-", line);
        }

        // Confirmed docking at the destination (production Load path, as after a Save/Load at arrival): delivered once.
        long credits = engine.CaptureSnapshot().PlayerCredits;
        Docked(engine, offer.DestinationStationId);
        await using (var connection = new LocalGameSessionConnection(engine))
        await using (var handle = new GameSessionHandle(connection))
        {
            await WaitFor(() => handle.DemoCampaign?.Transport?.PassengerContracts?.Contracts.FirstOrDefault()?.DeliveryState == PassengerDeliveryStates.Delivered);
            var screen = new ContractsScreen(handle.Buffer, () => handle.DemoCampaign, command => handle.ExecuteDemoCampaignAsync(command));
            Render(screen);
            var contract = handle.DemoCampaign!.Transport!.PassengerContracts!.Contracts[0];
            var view = screen.PassengersView;
            Assert.Equal(string.Format(Localization.Get("Demo.PassengerContracts.Aboard"), 0), view.Summary);
            Assert.Equal(2, view.Lines.Length);
            Assert.EndsWith(Localization.Get("Demo.PassengerContracts.State.delivered"), view.Lines[0]);
            Assert.Equal(string.Format(Localization.Get("Demo.PassengerContracts.Delivered"),
                StationJobsPresentation.Time(contract.DeliveredAtGameTimeMs!.Value), offer.Payout), view.Lines[1]);
            Assert.EndsWith(Localization.Get("Demo.Jobs.Status.completed"), Assert.Single(screen.JobsView.Journal));
            await WaitFor(() => handle.Buffer.Latest!.Snapshot.PlayerCredits == credits + offer.Payout);
            Assert.Null(screen.PassengerFeedback); // a reopened screen never claims a boarding it did not send
        }
    }

    [Theory]
    [InlineData("English")]
    [InlineData("Russian")]
    public void RejectedCommand_ShowsReason(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);

        // Every delivery state has a text; unknown states read as "not confirmed", never the raw code.
        foreach (var state in new[] { PassengerDeliveryStates.Aboard, PassengerDeliveryStates.Overdue, PassengerDeliveryStates.Delivered })
        {
            Assert.Equal(Localization.Get("Demo.PassengerContracts.State." + state), PassengerContractsPresentation.State(state));
            Assert.NotEqual(state, PassengerContractsPresentation.State(state));
        }
        Assert.Equal(Localization.Get("Demo.PassengerContracts.State.unavailable"), PassengerContractsPresentation.State("future_state"));
        Assert.Equal(Localization.Get("Demo.PassengerContracts.State.unavailable"), PassengerContractsPresentation.State(null));

        // Not enough cabins: after the first passenger boards, another passenger offer is refused by the engine's
        // verdict without sending anything; nothing is charged.
        using var engine = Docked();
        Assert.True(engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("board-first", 0, FirstOffer)).Applied);
        int calls = 0;
        var screen = new ContractsScreen(null, () => engine.CaptureSnapshot().DemoCampaign, command =>
        {
            calls++;
            return ValueTask.FromResult(engine.ExecuteDemoCampaign(command));
        });
        Render(screen);
        long credits = engine.CaptureSnapshot().PlayerCredits;
        var big = engine.CaptureSnapshot().DemoCampaign!.Transport!.StationJobs!.Offers
            .FirstOrDefault(o => o.OriginStationId == Start && StationJobTypes.CarriesPassengers(o.Type));
        if (big is not null)
        {
            var row = screen.JobsView.Offers.Single(r => r.OfferId == big.Id);
            Assert.False(row.CanAccept);
            Assert.Equal(StationJobsPresentation.Reason(PassengerCapacityReasons.NoFreeCabins), row.Blocker);
            Assert.Null(screen.TryAcceptOffer(big.Id));
            Assert.Equal(StationJobsPresentation.Reason(PassengerCapacityReasons.NoFreeCabins), screen.JobsFeedback);
            Assert.Null(screen.PassengerFeedback);
        }
        // The already accepted offer left the board: re-accepting is refused locally by the missing verdict.
        Assert.Null(screen.TryAcceptOffer(FirstOffer));
        Assert.Equal(StationJobsPresentation.Reason(DemoCampaignReasons.Unavailable), screen.JobsFeedback);
        Assert.Equal(0, calls);
        Assert.Equal(credits, engine.CaptureSnapshot().PlayerCredits);
        Assert.Equal(1, engine.CaptureSnapshot().DemoCampaign!.Transport!.PassengerContracts!.PassengersAboard);

        // No demo passenger contracts at all: an explicit unavailable state.
        var none = new ContractsScreen();
        Render(none);
        Assert.Equal(Localization.Get("Demo.PassengerContracts.Unavailable"), none.PassengersView.Empty);
        Assert.Empty(none.PassengersView.Lines);
        Assert.Null(none.PassengerFeedback);
    }

    [Fact]
    public async Task Reopen_DoesNotResubmit()
    {
        using var locale = Localization.UseLocaleForTests("English");
        using var engine = Docked();
        int calls = 0;
        Func<DemoCampaignCommand, ValueTask<DemoCampaignResponse>> execute = command =>
        {
            Interlocked.Increment(ref calls);
            return ValueTask.FromResult(engine.ExecuteDemoCampaign(command));
        };
        var screen = new ContractsScreen(null, () => engine.CaptureSnapshot().DemoCampaign, execute);
        Render(screen);
        await screen.TryAcceptOffer(FirstOffer)!;
        Assert.Equal(1, calls);
        Assert.NotNull(screen.PassengerFeedback);

        // Closing and reopening never replays the command or boards anyone twice.
        screen.OnDeactivated();
        Render(screen);
        var reopened = new ContractsScreen(null, () => engine.CaptureSnapshot().DemoCampaign, execute);
        Render(reopened);
        Render(reopened, activate: false);
        Assert.Null(reopened.PassengerFeedback);
        Assert.Equal(1, Volatile.Read(ref calls));
        var section = engine.CaptureSnapshot().DemoCampaign!.Transport!.PassengerContracts!;
        Assert.Equal(1, section.PassengersAboard);
        Assert.Single(section.Contracts);
        Assert.Single(reopened.PassengersView.Lines);
    }
}
