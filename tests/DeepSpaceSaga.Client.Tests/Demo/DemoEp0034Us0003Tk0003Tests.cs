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
/// EP-0034-US-0003-TK-0003: the Contracts screen shows, from the authoritative snapshot, the calendar deadline rule,
/// the consequences of cancelling or missing each active order before the action, a two-step cancel with a pending
/// state, the confirmed result (penalty, paid, owed), every outcome, waiting/completed disembarkation, loss and
/// contract debt — through the real session transport as SkiaWindow wires it. Refusals are explained, a blocked or
/// closed order sends nothing, and reopening never resubmits.
/// </summary>
public sealed class DemoEp0034Us0003Tk0003Tests
{
    private const string Ship = "SPC-0001";
    private const string Start = "SPC-0002";
    private const string FirstOffer = "jobs:SPC-0002:0:0";
    private const string ContractId = "contract:" + FirstOffer;
    private static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "Settings.json");
    private static string DemoScenarioPath => Path.Combine(AppContext.BaseDirectory, "Scenarios", "Demo", "scenario.json");

    private static SimulationEngine Docked()
    {
        var engine = EngineContentLoader.CreateEngineFromScenarioFile(SettingsPath, DemoScenarioPath);
        engine.SetSpeed(SimulationSpeed.Speed0);
        var save = engine.CaptureSaveState();
        var ship = save.GameState.SpaceObjects.Single(s => s.ObjectId == Ship) with
        {
            IsDocked = true, DockedStationObjectId = Start, FirstPortFeeGameTimeMs = 0, NextPortFeeDueGameTimeMs = GameCalendar.DayMs
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

    private static string N(long value) => value.ToString(CultureInfo.InvariantCulture);

    [Theory]
    [InlineData("English")]
    [InlineData("Russian")]
    public async Task ConfirmedSnapshot_RendersOutcome(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);
        using var engine = Docked();
        Assert.True(engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("accept", 0, FirstOffer)).Applied);
        await using var connection = new LocalGameSessionConnection(engine);
        await using var handle = new GameSessionHandle(connection);
        await WaitFor(() => handle.DemoCampaign?.Transport?.TransportFailures?.Terms.Length == 1 && handle.Buffer.Latest is not null);
        // Same wiring as SkiaWindow.OpenContractsAsync.
        var screen = new ContractsScreen(handle.Buffer, () => handle.DemoCampaign, command => handle.ExecuteDemoCampaignAsync(command));
        Render(screen);

        // Before the action: the calendar rule and the consequences of cancelling or missing the deadline (AC-01, AC-02).
        var terms = handle.DemoCampaign!.Transport!.TransportFailures!.Terms[0];
        var view = screen.FailuresView;
        Assert.Equal(Localization.Get("Demo.TransportFailures.Title"), view.Title);
        Assert.Equal(Localization.Get("Demo.TransportFailures.Calendar"), view.Calendar);
        Assert.Equal(Localization.Get("Demo.TransportFailures.Empty"), view.Empty);
        Assert.Null(view.Debt);
        var row = Assert.Single(view.Active);
        Assert.True(row.CanCancel);
        Assert.Equal(string.Format(Localization.Get("Demo.TransportFailures.Terms"), terms.CancelPenaltyCredits,
            StationJobsPresentation.Time(terms.DeadlineGameTimeMs), terms.ExpiryPenaltyCredits), row.Terms);
        Assert.Contains(Name(handle, Start), row.Headline);
        Assert.DoesNotContain("contract:", row.Headline);

        // First click only asks for confirmation; nothing is sent.
        long credits = handle.Buffer.Latest!.Snapshot.PlayerCredits;
        var button = screen.CancelButtons.Single(b => b.ContractId == ContractId).Rect;
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(button.MidX, button.MidY));
        Assert.Equal(string.Format(Localization.Get("Demo.TransportFailures.CancelConfirm"), terms.CancelPenaltyCredits), screen.JobsFeedback);
        Assert.False(screen.IsAcceptPending);
        Assert.Empty(engine.CaptureSnapshot().DemoCampaign!.Transport!.TransportFailures!.Failures);

        // Second click sends one command; the result is shown from the confirmed snapshot.
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(button.MidX, button.MidY));
        await WaitFor(() => !screen.IsAcceptPending);
        await WaitFor(() => handle.DemoCampaign!.Transport!.TransportFailures!.Failures.Length == 1);
        Render(screen, activate: false);
        var failure = handle.DemoCampaign!.Transport!.TransportFailures!.Failures[0];
        Assert.Equal(string.Format(Localization.Get("Demo.TransportFailures.Cancelled"), failure.PenaltyCredits, failure.PenaltyPaid, failure.Debt),
            screen.CancelFeedback);
        view = screen.FailuresView;
        Assert.Empty(view.Active);
        Assert.Null(view.Empty);
        Assert.Equal(2, view.Lines.Length);
        Assert.Contains(Localization.Get("Demo.TransportFailures.Outcome.cancelled"), view.Lines[0]);
        Assert.Contains(N(failure.PenaltyCredits), view.Lines[0]);
        // Docked at the start station: they leave here at once, their cabins are free (AC-03).
        Assert.Equal(string.Format(Localization.Get("Demo.TransportFailures.Disembarked"), 3, Name(handle, Start),
            StationJobsPresentation.Time(failure.DisembarkedAtGameTimeMs!.Value)), view.Lines[1]);
        Assert.EndsWith(Localization.Get("Demo.PassengerContracts.State.failed"), Assert.Single(screen.PassengersView.Lines));
        Assert.EndsWith(Localization.Get("Demo.Jobs.Status.cancelled"), Assert.Single(screen.JobsView.Journal));
        await WaitFor(() => handle.Buffer.Latest!.Snapshot.PlayerCredits == credits - failure.PenaltyPaid);
        Assert.All(view.Lines, l => Assert.DoesNotContain("pax-", l));
    }

    [Theory]
    [InlineData("English")]
    [InlineData("Russian")]
    public async Task RejectedCommand_ShowsReason(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);

        // Every outcome and refusal has a text; unknown codes never show the raw code.
        foreach (var code in new[] { TransportFailureReasons.Cancelled, TransportFailureReasons.Expired, TransportFailureReasons.CarrierLost })
            Assert.Equal(Localization.Get("Demo.TransportFailures.Outcome." + code), TransportFailurePresentation.Outcome(code));
        foreach (var code in new[] { TransportFailureReasons.ContractNotFound, TransportFailureReasons.ContractNotActive,
                     TransportFailureReasons.NotCancellable, DemoCampaignReasons.StaleRevision, DemoCampaignReasons.BattleActive })
            Assert.Equal(Localization.Get("Demo.TransportFailures.Reason." + code), TransportFailurePresentation.Reason(code));
        Assert.Equal(Localization.Get("Demo.TransportFailures.Reason.unavailable"), TransportFailurePresentation.Reason("future_code"));
        Assert.Equal(Localization.Get("Demo.TransportFailures.Reason.unavailable"), TransportFailurePresentation.Reason(null));

        // Engine verdict blocks the action (battle): the button is disabled and a click sends nothing.
        var obligation = new JobObligation(ContractId, FirstOffer, StationJobTypes.Passenger, Start, "SPC-0005", 3, 0, 375, 0,
            9_000_000, StationJobStatuses.Active, StationJobRiskBands.Safe);
        var lost = new JobObligation("contract:x", "x", StationJobTypes.UrgentPassenger, Start, "SPC-0005", 2, 0, 400, 0,
            9_000_000, StationJobStatuses.Failed, StationJobRiskBands.Safe, ClosedAtGameTimeMs: 100);
        var expired = new JobObligation("contract:y", "y", StationJobTypes.Passenger, Start, "SPC-0005", 1, 0, 200, 0,
            9_000_000, StationJobStatuses.Failed, StationJobRiskBands.Safe, ClosedAtGameTimeMs: 9_000_001);
        var snapshot = new DemoCampaignSnapshot(4, 6, Transport: new DemoTransportSnapshot
        {
            StationJobs = new StationJobsSnapshot([], [obligation, lost, expired], 10, Start, []),
            TransportFailures = new TransportFailuresSnapshot(
                [new TransportFailureSnapshot("contract:x", StationJobTypes.UrgentPassenger, TransportFailureReasons.CarrierLost, 100, 0, 0, 0,
                    false, "penalty:contract:x", ["contract:x:pax-1", "contract:x:pax-2"]),
                 new TransportFailureSnapshot("contract:y", StationJobTypes.Passenger, TransportFailureReasons.Expired, 9_000_001, 50, 20, 30,
                    true, "penalty:contract:y", ["contract:y:pax-1"])],
                [new TransportCancellationTerms(ContractId, 94, 94, 9_000_000, DemoCampaignReasons.BattleActive)], 30, 10)
        });
        int calls = 0;
        DemoCampaignResponse reply = new("r", false, DemoCampaignReasons.StaleRevision, 4);
        var screen = new ContractsScreen(null, () => snapshot, command => { calls++; return ValueTask.FromResult(reply with { CommandId = command.CommandId }); });
        Render(screen);
        var view = screen.FailuresView;
        var row = Assert.Single(view.Active);
        Assert.False(row.CanCancel);
        Assert.Equal(TransportFailurePresentation.Reason(DemoCampaignReasons.BattleActive), row.Blocker);
        Assert.Null(screen.TryCancelContract(ContractId));
        Assert.Null(screen.TryCancelContract(ContractId));
        Assert.Equal(TransportFailurePresentation.Reason(DemoCampaignReasons.BattleActive), screen.JobsFeedback);
        Assert.Equal(0, calls);
        // Debt, loss and waiting passengers are explained from engine values.
        Assert.Equal(string.Format(Localization.Get("Demo.TransportFailures.Debt"), 30), view.Debt);
        Assert.Contains(string.Format(Localization.Get("Demo.TransportFailures.Lost"), 2), view.Lines);
        Assert.Contains(string.Format(Localization.Get("Demo.TransportFailures.DisembarkPending"), 1), view.Lines);
        Assert.Contains(view.Lines, l => l.Contains(Localization.Get("Demo.TransportFailures.Outcome.expired"), StringComparison.Ordinal) &&
            l.Contains("30", StringComparison.Ordinal));

        // An allowed verdict, but the engine answers stale_revision: the reason is shown, nothing is retried.
        snapshot = snapshot with
        {
            Transport = snapshot.Transport! with
            {
                TransportFailures = snapshot.Transport.TransportFailures! with { Terms = [new TransportCancellationTerms(ContractId, 94, 94, 9_000_000, null)] }
            }
        };
        Assert.Null(screen.TryCancelContract(ContractId)); // arms the confirmation
        await screen.TryCancelContract(ContractId)!;
        Assert.Equal(1, calls);
        Assert.Equal(TransportFailurePresentation.Reason(DemoCampaignReasons.StaleRevision), screen.JobsFeedback);
        Assert.Null(screen.CancelFeedback);
        // A contract the engine does not list as cancellable is refused locally.
        Assert.Null(screen.TryCancelContract("contract:y"));
        Assert.Equal(TransportFailurePresentation.Reason(DemoCampaignReasons.Unavailable), screen.JobsFeedback);
        Assert.Equal(1, calls);

        // No demo transport at all: explicit unavailable state.
        var none = new ContractsScreen();
        Render(none);
        Assert.Equal(Localization.Get("Demo.TransportFailures.Unavailable"), none.FailuresView.Empty);
        Assert.Empty(none.FailuresView.Active);
    }

    [Fact]
    public async Task Reopen_DoesNotResubmit()
    {
        using var locale = Localization.UseLocaleForTests("English");
        using var engine = Docked();
        Assert.True(engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("accept", 0, FirstOffer)).Applied);
        int calls = 0;
        Func<DemoCampaignCommand, ValueTask<DemoCampaignResponse>> execute = command =>
        {
            Interlocked.Increment(ref calls);
            return ValueTask.FromResult(engine.ExecuteDemoCampaign(command));
        };
        var screen = new ContractsScreen(null, () => engine.CaptureSnapshot().DemoCampaign, execute);
        Render(screen);
        Assert.Null(screen.TryCancelContract(ContractId));
        await screen.TryCancelContract(ContractId)!;
        Assert.Equal(1, calls);
        Assert.NotNull(screen.CancelFeedback);
        long credits = engine.CaptureSnapshot().PlayerCredits;

        // Closing and reopening never replays the command or charges again; the closed order cannot be cancelled.
        screen.OnDeactivated();
        var reopened = new ContractsScreen(null, () => engine.CaptureSnapshot().DemoCampaign, execute);
        Render(reopened);
        Render(reopened, activate: false);
        Assert.Null(reopened.CancelFeedback);
        Assert.Empty(reopened.FailuresView.Active);
        Assert.Empty(reopened.CancelButtons);
        Assert.Null(reopened.TryCancelContract(ContractId));
        Assert.Equal(1, Volatile.Read(ref calls));
        Assert.Equal(credits, engine.CaptureSnapshot().PlayerCredits);
        Assert.Single(engine.CaptureSnapshot().DemoCampaign!.Transport!.TransportFailures!.Failures);
        Assert.Empty(engine.CaptureSnapshot().DemoCampaign!.Transport!.TransportFailures!.Terms);
    }
}
