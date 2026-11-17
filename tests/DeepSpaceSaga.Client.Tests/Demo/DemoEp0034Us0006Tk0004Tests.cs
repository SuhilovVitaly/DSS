using DeepSpaceSaga.Client.UI.Demo;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Contracts;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.Demo;

/// <summary>
/// EP-0034-US-0006-TK-0004: the Contracts screen shows the engine's job board of the docked station and the
/// obligations journal, accepts through the real session transport (LocalGameSessionConnection +
/// GameSessionHandle, as SkiaWindow wires it), explains rejections in RU/EN and never resubmits on reopen.
/// The journal is reachable from the map (J button, Ctrl+J).
/// </summary>
public sealed class DemoEp0034Us0006Tk0004Tests
{
    private const string Ship = "SPC-0001";
    private const string Start = "SPC-0002";
    private static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "Settings.json");
    private static string DemoScenarioPath => Path.Combine(AppContext.BaseDirectory, "Scenarios", "Demo", "scenario.json");

    /// <summary>Real demo New Game; docked at the start station through the production Load path when requested.</summary>
    private static SimulationEngine Demo(bool docked = true)
    {
        var engine = EngineContentLoader.CreateEngineFromScenarioFile(SettingsPath, DemoScenarioPath);
        engine.SetSpeed(SimulationSpeed.Speed0);
        if (!docked) return engine;
        var save = engine.CaptureSaveState();
        var ship = save.GameState.SpaceObjects.Single(s => s.ObjectId == Ship);
        var dockedShip = ship with
        {
            IsDocked = true, DockedStationObjectId = Start, FirstPortFeeGameTimeMs = 0, NextPortFeeDueGameTimeMs = GameCalendar.DayMs
        };
        engine.LoadScenario(save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(s => s.ObjectId == Ship ? dockedShip : s).ToArray()
            }
        }, isSave: true);
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

    [Theory]
    [InlineData("English", "Accepted.", "in progress")]
    [InlineData("Russian", "Принято.", "выполняется")]
    public async Task ConfirmedSnapshot_RendersOutcome(string language, string accepted, string active)
    {
        using var locale = Localization.UseLocaleForTests(language);
        using var engine = Demo();
        await using var connection = new LocalGameSessionConnection(engine);
        await using var handle = new GameSessionHandle(connection);
        await WaitFor(() => handle.DemoCampaign?.Transport?.StationJobs is not null && handle.Buffer.Latest is not null);

        // Same wiring as SkiaWindow.OpenContractsAsync.
        var screen = new ContractsScreen(handle.Buffer, () => handle.DemoCampaign, command => handle.ExecuteDemoCampaignAsync(command));
        Render(screen);
        var board = handle.DemoCampaign!.Transport!.StationJobs!;
        var view = screen.JobsView;
        Assert.Equal(Localization.Get("Demo.Jobs.Title"), view.Title);
        Assert.Null(view.Notice);
        Assert.Equal(board.Offers.Where(o => o.OriginStationId == Start).Select(o => o.Id), view.Offers.Select(r => r.OfferId));
        Assert.Equal(view.Offers.Length, screen.AcceptButtons.Count);
        Assert.Equal(Localization.Get("Demo.Jobs.JournalEmpty"), view.JournalEmpty);

        // Terms come from the engine: payout, frozen distance and risk factor are displayed, never recomputed.
        var first = board.Offers.Single(o => o.Id == "jobs:SPC-0002:0:0");
        var row = view.Offers.Single(r => r.OfferId == first.Id);
        Assert.True(row.CanAccept);
        Assert.Contains(first.Payout.ToString(System.Globalization.CultureInfo.InvariantCulture), row.Terms);
        Assert.Contains(first.RouteDistanceKm + " ", row.Payout);
        Assert.StartsWith(Localization.Get("Demo.Jobs.Type.passenger"), row.Headline);
        Assert.DoesNotContain("SPC-", row.Headline); // station names, not ids

        // Accept by clicking the real button: pending, then the confirmed response, then the journal from the snapshot.
        var button = screen.AcceptButtons.Single(b => b.OfferId == first.Id).Rect;
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(button.MidX, button.MidY));
        await WaitFor(() => !screen.IsAcceptPending);
        Assert.StartsWith(accepted, screen.JobsFeedback);
        await WaitFor(() => handle.DemoCampaign!.Transport!.StationJobs!.Obligations.Length == 1);
        Render(screen, activate: false);
        view = screen.JobsView;
        Assert.DoesNotContain(view.Offers, r => r.OfferId == first.Id);
        Assert.Null(view.JournalEmpty);
        string line = Assert.Single(view.Journal);
        Assert.EndsWith(active, line);
        Assert.Contains(first.Payout.ToString(System.Globalization.CultureInfo.InvariantCulture), line);
    }

    [Theory]
    [InlineData("English")]
    [InlineData("Russian")]
    public async Task RejectedCommand_ShowsReason(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);

        // Every stable code has an explanation; unknown codes fall back to a generic text, never the raw code.
        foreach (var code in DemoEp0034Us0006Tk0005Tests.ReasonCodes)
        {
            string text = StationJobsPresentation.Reason(code);
            Assert.Equal(Localization.Get("Demo.Jobs.Reason." + code), text);
            Assert.DoesNotContain(code, text, StringComparison.Ordinal);
        }
        Assert.Equal(Localization.Get("Demo.Jobs.Rejected"), StationJobsPresentation.Reason("future_code"));

        // A stale view: the engine moved on, the screen still shows revision 0 -> the engine's receipt explains it.
        using var engine = Demo();
        var stale = engine.CaptureSnapshot().DemoCampaign!;
        var offers = stale.Transport!.StationJobs!.Offers.Where(o => o.OriginStationId == Start).ToArray();
        Assert.True(engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("elsewhere", 0, offers[0].Id)).Applied);
        int calls = 0;
        var screen = new ContractsScreen(null, () => stale, command =>
        {
            calls++;
            return ValueTask.FromResult(engine.ExecuteDemoCampaign(command));
        });
        Render(screen);
        await screen.TryAcceptOffer(offers[1].Id)!;
        Assert.Equal(1, calls);
        Assert.Equal(StationJobsPresentation.Reason(DemoCampaignReasons.StaleRevision), screen.JobsFeedback);
        Assert.Single(engine.CaptureSnapshot().DemoCampaign!.Transport!.StationJobs!.Obligations);

        // In flight (opened from the map): the journal is shown, acceptance is refused by the engine's verdict
        // without sending anything.
        using var flying = Demo(docked: false);
        var inFlight = new ContractsScreen(null, () => flying.CaptureSnapshot().DemoCampaign, command =>
        {
            calls++;
            return ValueTask.FromResult(flying.ExecuteDemoCampaign(command));
        });
        Render(inFlight);
        var view = inFlight.JobsView;
        Assert.Equal(Localization.Get("Demo.Jobs.InFlight"), view.Notice);
        Assert.Empty(view.Offers);
        Assert.Empty(inFlight.AcceptButtons);
        var remote = flying.CaptureSnapshot().DemoCampaign!.Transport!.StationJobs!.Offers[0];
        Assert.Null(inFlight.TryAcceptOffer(remote.Id));
        Assert.Equal(1, calls);
        Assert.Equal(StationJobsPresentation.Reason(StationJobReasons.NotDockedAtOrigin), inFlight.JobsFeedback);

        // No demo board at all: an explicit unavailable state, not an empty acceptable list.
        var none = new ContractsScreen();
        Render(none);
        Assert.Equal(Localization.Get("Demo.Jobs.Unavailable"), none.JobsView.Notice);
        Assert.Null(none.TryAcceptOffer("jobs:SPC-0002:0:0"));
    }

    [Fact]
    public async Task Reopen_DoesNotResubmit()
    {
        using var locale = Localization.UseLocaleForTests("English");
        using var engine = Demo();
        var gate = new TaskCompletionSource();
        int calls = 0;
        Func<DemoCampaignCommand, ValueTask<DemoCampaignResponse>> execute = async command =>
        {
            Interlocked.Increment(ref calls);
            await gate.Task;
            return engine.ExecuteDemoCampaign(command);
        };
        var screen = new ContractsScreen(null, () => engine.CaptureSnapshot().DemoCampaign, execute);
        Render(screen);
        var button = screen.AcceptButtons[0];

        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(button.Rect.MidX, button.Rect.MidY));
        Assert.True(screen.IsAcceptPending);
        Assert.Equal(StationJobsPresentation.Pending, screen.JobsFeedback);
        // Repeated input while pending sends nothing new.
        screen.OnMouseDown(button.Rect.MidX, button.Rect.MidY);
        Assert.Null(screen.TryAcceptOffer(button.OfferId));
        Assert.Equal(1, Volatile.Read(ref calls));

        // Closing and reopening (same instance and a fresh one) never replays the command.
        screen.OnDeactivated();
        Render(screen);
        var reopened = new ContractsScreen(null, () => engine.CaptureSnapshot().DemoCampaign, execute);
        Render(reopened);
        Assert.Null(reopened.JobsFeedback);
        Assert.Equal(1, Volatile.Read(ref calls));

        gate.SetResult();
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (screen.IsAcceptPending) { Assert.True(DateTime.UtcNow < deadline); await Task.Delay(20); }
        Assert.Equal(StationJobsPresentation.Accepted, screen.JobsFeedback);
        Render(reopened);
        Assert.Equal(1, Volatile.Read(ref calls));
        Assert.Single(engine.CaptureSnapshot().DemoCampaign!.Transport!.StationJobs!.Obligations);
        Assert.Single(reopened.JobsView.Journal);
    }

    [Fact]
    public void Map_opens_the_journal_with_button_and_ctrl_j()
    {
        var screen = new GameSessionScreen(new SnapshotBuffer(), new LinearMotionPredictor());
        Render(screen, activate: false);
        var journal = screen.LastJournalButtonRect;
        Assert.True(journal.Width > 0);
        Assert.True(screen.LastMechanicsPanelRect.Contains(journal));
        Assert.True(screen.LastShipButtonRect.Right <= journal.Left, "J sits right of S without overlap");
        Assert.True(screen.LastTempCharacterImageButtonRect.Left >= journal.Right, "portrait button keeps its own space");
        Assert.Equal(ScreenEvent.OpenContracts, screen.OnMouseDown(journal.MidX, journal.MidY));

        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.J));
        screen.SynchronizeModifiers(true, false);
        Assert.Equal(ScreenEvent.OpenContracts, screen.OnKeyDown(Key.J));
        Assert.Equal(ScreenEvent.OpenShip, screen.OnKeyDown(Key.S));
        Assert.Equal(ScreenEvent.OpenFinance, screen.OnKeyDown(Key.F));
        screen.SynchronizeModifiers(false, false);
    }
}
