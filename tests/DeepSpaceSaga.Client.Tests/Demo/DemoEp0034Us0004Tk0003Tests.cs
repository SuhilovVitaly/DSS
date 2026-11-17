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
/// EP-0034-US-0004-TK-0003: the Contracts screen shows, from the authoritative snapshot, the hold split (sealed
/// contract cargo apart from the player's own goods and the space left for trade), every cargo contract with item,
/// route, payout, deadline and hold state, the confirmed sealing, delivery and return results — through the real
/// session transport as SkiaWindow wires it. Refusals are explained, a blocked offer sends nothing, and reopening
/// never resubmits.
/// </summary>
public sealed class DemoEp0034Us0004Tk0003Tests
{
    private const string Ship = "SPC-0001";
    /// <summary>The first publication lists cargo offers at this station (the start station publishes passengers).</summary>
    private const string Origin = "SPC-0006";
    private static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "Settings.json");
    private static string DemoScenarioPath => Path.Combine(AppContext.BaseDirectory, "Scenarios", "Demo", "scenario.json");

    private static SimulationEngine Docked()
    {
        var engine = EngineContentLoader.CreateEngineFromScenarioFile(SettingsPath, DemoScenarioPath);
        engine.SetSpeed(SimulationSpeed.Speed0);
        var save = engine.CaptureSaveState();
        var ship = save.GameState.SpaceObjects.Single(s => s.ObjectId == Ship) with
        {
            IsDocked = true, DockedStationObjectId = Origin, FirstPortFeeGameTimeMs = 0, NextPortFeeDueGameTimeMs = GameCalendar.DayMs
        };
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save with
        {
            GameState = save.GameState with { SpaceObjects = save.GameState.SpaceObjects.Select(s => s.ObjectId == Ship ? ship : s).ToArray() }
        }), true), isSave: true);
        engine.SetSpeed(SimulationSpeed.Speed0);
        return engine;
    }

    private static JobOffer CargoOffer(DemoCampaignSnapshot campaign) => campaign.Transport!.StationJobs!.Offers
        .Where(o => o.Type == StationJobTypes.Cargo && o.OriginStationId == Origin &&
            StationJobReasons.AcceptBlocker(campaign.Transport.StationJobs, o.Id) is null)
        .OrderBy(o => o.DeadlineDurationMs).ThenBy(o => o.Id, StringComparer.Ordinal).First();

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

    private static string T(long kg) => (kg / 1000m).ToString("0.#", CultureInfo.InvariantCulture);

    [Theory]
    [InlineData("English")]
    [InlineData("Russian")]
    public async Task ConfirmedSnapshot_RendersOutcome(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);
        using var engine = Docked();
        await using var connection = new LocalGameSessionConnection(engine);
        await using var handle = new GameSessionHandle(connection);
        await WaitFor(() => handle.DemoCampaign?.Transport?.CargoContracts is not null && handle.Buffer.Latest is not null);
        // Same wiring as SkiaWindow.OpenContractsAsync.
        var screen = new ContractsScreen(handle.Buffer, () => handle.DemoCampaign, command => handle.ExecuteDemoCampaignAsync(command));
        Render(screen);

        // Before the action: no contracts, the hold split shows nothing sealed (AC-01).
        var hold = handle.DemoCampaign!.Transport!.CargoContracts!;
        var view = screen.CargoView;
        Assert.Equal(Localization.Get("Demo.CargoContracts.Title"), view.Title);
        Assert.Equal(Localization.Get("Demo.CargoContracts.Empty"), view.Empty);
        Assert.Empty(view.Lines);
        Assert.Null(view.NotForSale);
        Assert.Equal(string.Format(Localization.Get("Demo.CargoContracts.Hold"), "0", T(hold.PlayerCargoMassKg), T(hold.FreeHoldKg)), view.Hold);

        // Accept through the drawn button: one command, then the confirmed sealing from the snapshot.
        var offer = CargoOffer(handle.DemoCampaign!);
        var button = screen.AcceptButtons.Single(b => b.OfferId == offer.Id).Rect;
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(button.MidX, button.MidY));
        await WaitFor(() => !screen.IsAcceptPending);
        await WaitFor(() => handle.DemoCampaign!.Transport!.CargoContracts!.Contracts.Length == 1);
        Render(screen, activate: false);
        var section = handle.DemoCampaign!.Transport!.CargoContracts!;
        var contract = section.Contracts[0];
        string item = CargoContractsPresentation.ItemName(contract.ItemTypeId);
        Assert.DoesNotContain("item.", item);
        Assert.Equal(string.Format(Localization.Get("Demo.CargoContracts.Sealed"), T(offer.MassKg), item,
            Name(handle, offer.DestinationStationId), StationJobsPresentation.Time(contract.DeadlineGameTimeMs)), screen.CargoFeedback);
        view = screen.CargoView;
        Assert.Null(view.Empty);
        Assert.Equal(Localization.Get("Demo.CargoContracts.NotForSale"), view.NotForSale);
        Assert.Equal(string.Format(Localization.Get("Demo.CargoContracts.Hold"), T(offer.MassKg), T(section.PlayerCargoMassKg), T(section.FreeHoldKg)),
            view.Hold);
        Assert.Equal(string.Format(Localization.Get("Demo.CargoContracts.Line"), T(offer.MassKg), item, Name(handle, Origin),
            Name(handle, offer.DestinationStationId), offer.Payout, StationJobsPresentation.Time(contract.DeadlineGameTimeMs),
            Localization.Get("Demo.CargoContracts.State.aboard")), Assert.Single(view.Lines));
        Assert.All(view.Lines, l => Assert.DoesNotContain("contract:", l));
        Assert.EndsWith(Localization.Get("Demo.Jobs.Status.active"), Assert.Single(screen.JobsView.Journal));
        // The passenger result line stays silent for a cargo acceptance.
        Assert.Null(screen.PassengerFeedback);

        // Confirmed delivery and return results are read from engine values only (AC-02, AC-03).
        var delivered = contract with
        {
            Status = StationJobStatuses.Completed, HoldState = CargoHoldStates.Delivered, DeliveredAtGameTimeMs = 5_000_000, PaidCredits = offer.Payout
        };
        Assert.Equal(string.Format(Localization.Get("Demo.CargoContracts.Delivered"), StationJobsPresentation.Time(5_000_000), offer.Payout),
            CargoContractsPresentation.Result(delivered, id => id));
        var returned = contract with
        {
            Status = StationJobStatuses.Cancelled, HoldState = CargoHoldStates.Returned, ReturnedAtGameTimeMs = 600_000, ReturnStationId = Origin
        };
        Assert.Equal(string.Format(Localization.Get("Demo.CargoContracts.Returned"), Origin, StationJobsPresentation.Time(600_000)),
            CargoContractsPresentation.Result(returned, id => id));
        var built = CargoContractsPresentation.Build(section with { Contracts = [delivered, returned] }, id => id);
        Assert.Equal(4, built.Lines.Length);
        Assert.EndsWith(Localization.Get("Demo.CargoContracts.State.delivered"), built.Lines[0]);
        Assert.EndsWith(Localization.Get("Demo.CargoContracts.State.returned"), built.Lines[2]);
        Assert.Null(CargoContractsPresentation.Result(contract, id => id));
    }

    [Theory]
    [InlineData("English")]
    [InlineData("Russian")]
    public async Task RejectedCommand_ShowsReason(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);

        // Every hold state has a text; unknown or missing states never show the raw code.
        foreach (var state in new[] { CargoHoldStates.Aboard, CargoHoldStates.Overdue, CargoHoldStates.Delivered,
                     CargoHoldStates.ReturnPending, CargoHoldStates.Returned, CargoHoldStates.Lost })
            Assert.Equal(Localization.Get("Demo.CargoContracts.State." + state), CargoContractsPresentation.State(state));
        Assert.Equal(Localization.Get("Demo.CargoContracts.State.unavailable"), CargoContractsPresentation.State("future_state"));
        Assert.Equal(Localization.Get("Demo.CargoContracts.State.unavailable"), CargoContractsPresentation.State(null));
        Assert.Equal(Localization.Get("Demo.Jobs.Type.cargo"), CargoContractsPresentation.ItemName("item.unknown"));

        // The engine verdict blocks a cargo offer (hold full): the button is disabled and a click sends nothing.
        var offer = new JobOffer("jobs:SPC-0006:0:3", StationJobTypes.Cargo, Origin, "SPC-0005", 2000, 2000, 250, 48_600_000, 0,
            172_800_000, StationJobRiskBands.Safe, 81, new JobPayoutTerms(150, 90, 243, 1000, 1000), "item.steel");
        var blocked = new DemoCampaignSnapshot(3, 5, Transport: new DemoTransportSnapshot
        {
            StationJobs = new StationJobsSnapshot([offer], [], 10, Origin,
                [new JobOfferAvailability(offer.Id, StationJobReasons.InsufficientCargoSpace)]),
            CargoContracts = new CargoContractsSnapshot([], 10, 0, 100_000, 99_000)
        });
        var snapshot = blocked;
        int calls = 0;
        DemoCampaignResponse reply = new("r", false, DemoCampaignReasons.StaleRevision, 3);
        var screen = new ContractsScreen(null, () => snapshot, command => { calls++; return ValueTask.FromResult(reply with { CommandId = command.CommandId }); });
        Render(screen);
        var row = Assert.Single(screen.JobsView.Offers);
        Assert.False(row.CanAccept);
        Assert.Equal(Localization.Get("Demo.Jobs.Reason.insufficient_cargo_space"), row.Blocker);
        Assert.Null(screen.TryAcceptOffer(offer.Id));
        Assert.Equal(Localization.Get("Demo.Jobs.Reason.insufficient_cargo_space"), screen.JobsFeedback);
        Assert.Equal(0, calls);
        Assert.Equal(string.Format(Localization.Get("Demo.CargoContracts.Hold"), "0", "99", "1"), screen.CargoView.Hold);

        // An allowed verdict, but the engine answers stale_revision: the reason is shown, nothing is retried or sealed.
        snapshot = blocked with
        {
            Transport = blocked.Transport! with
            {
                StationJobs = blocked.Transport.StationJobs! with { Availability = [new JobOfferAvailability(offer.Id, null)] }
            }
        };
        await screen.TryAcceptOffer(offer.Id)!;
        Assert.Equal(1, calls);
        Assert.Equal(StationJobsPresentation.Reason(DemoCampaignReasons.StaleRevision), screen.JobsFeedback);
        Assert.Null(screen.CargoFeedback);
        Assert.Equal(Localization.Get("Demo.CargoContracts.Empty"), screen.CargoView.Empty);

        // No demo transport at all: explicit unavailable state, no invented hold numbers.
        var none = new ContractsScreen();
        Render(none);
        Assert.Equal(Localization.Get("Demo.CargoContracts.Unavailable"), none.CargoView.Empty);
        Assert.Null(none.CargoView.Hold);
        Assert.Empty(none.CargoView.Lines);
    }

    [Fact]
    public async Task Reopen_DoesNotResubmit()
    {
        using var locale = Localization.UseLocaleForTests("English");
        using var engine = Docked();
        var offer = CargoOffer(engine.CaptureSnapshot().DemoCampaign!);
        int calls = 0;
        Func<DemoCampaignCommand, ValueTask<DemoCampaignResponse>> execute = command =>
        {
            Interlocked.Increment(ref calls);
            return ValueTask.FromResult(engine.ExecuteDemoCampaign(command));
        };
        var screen = new ContractsScreen(null, () => engine.CaptureSnapshot().DemoCampaign, execute);
        Render(screen);
        await screen.TryAcceptOffer(offer.Id)!;
        Assert.Equal(1, calls);
        Assert.NotNull(screen.CargoFeedback);
        var sealedHold = engine.CaptureSnapshot().DemoCampaign!.Transport!.CargoContracts!;
        Assert.Equal(offer.MassKg, sealedHold.ReservedMassKg);

        // Closing and reopening never replays the command or seals the cargo twice; the taken offer cannot be accepted.
        screen.OnDeactivated();
        var reopened = new ContractsScreen(null, () => engine.CaptureSnapshot().DemoCampaign, execute);
        Render(reopened);
        Render(reopened, activate: false);
        Assert.Null(reopened.CargoFeedback);
        Assert.Single(reopened.CargoView.Lines);
        Assert.DoesNotContain(reopened.AcceptButtons, b => b.OfferId == offer.Id);
        Assert.Null(reopened.TryAcceptOffer(offer.Id));
        Assert.Equal(1, Volatile.Read(ref calls));
        var after = engine.CaptureSnapshot().DemoCampaign!.Transport!.CargoContracts!;
        Assert.Equal((1, offer.MassKg), (after.Contracts.Length, after.ReservedMassKg));
    }
}
