using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Demo;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Contracts;

/// <summary>
/// Contracts overlay (DB/Documentation/02-FirstRelease/Screens/Contracts.md). EP-0034 US-0006: shows the
/// authoritative station job board of the docked station and the single obligations journal (also in flight,
/// opened from the map). Acceptance is sent as a typed campaign command through the session; the screen only
/// displays engine terms, verdicts and responses and never resubmits on reopen. Split out of the original `Hire`
/// screen (which now covers crew hiring specifically — DB/Documentation/02-FirstRelease/Screens/Hire.md)
/// so passenger contracts have their own screen. Opened from
/// <see cref="Station.StationScreen"/>'s `CONTRACTS` button (ScreenEvent.OpenContracts) as
/// a nested modal on top of it; closes via the toolbar's exit-button icon (see
/// StationToolbar), Escape, or a click outside the panel (on the dimmed background),
/// returning to <see cref="Station.StationScreen"/>. Pause-on-open/resume-on-close is
/// handled generically by SkiaWindow's PushModalAsync/PopModalAsync — this screen has no
/// speed/pause logic of its own. Structural twin of <see cref="Hire.HireScreen"/>/
/// <see cref="Trade.TradeScreen"/>/<see cref="Station.StationScreen"/>.
/// </summary>
public sealed class ContractsScreen : IScreen
{
    private readonly SnapshotBuffer? _buffer;

    private int _screenWidth;
    private int _screenHeight;
    private bool _isStationNameHovered;
    private bool _isExitButtonHovered;

    /// <summary>
    /// Real-time (Environment.TickCount64) timestamp the pointer first entered the
    /// food-rations readout, or null while not hovering it — the tooltip only appears
    /// once MenuStyle.TooltipHoverDelaySeconds has elapsed since this moment (checked in
    /// Render every frame, since OnMouseMove alone would never fire again while the
    /// pointer sits still).
    /// </summary>
    private long? _foodRationsHoverStartedAtMs;

    /// <summary>Test seam — true once the food-rations hover delay has elapsed.</summary>
    internal bool IsFoodRationsTooltipVisible =>
        _foodRationsHoverStartedAtMs is { } startedAtMs
        && Environment.TickCount64 - startedAtMs >= MenuStyle.TooltipHoverDelaySeconds * 1000;

    /// <summary>Same real-time hover-delay tracking as <see cref="_foodRationsHoverStartedAtMs"/>, for the crew readout.</summary>
    private long? _crewHoverStartedAtMs;

    /// <summary>Test seam — true once the crew-readout hover delay has elapsed.</summary>
    internal bool IsCrewTooltipVisible =>
        _crewHoverStartedAtMs is { } startedAtMs
        && Environment.TickCount64 - startedAtMs >= MenuStyle.TooltipHoverDelaySeconds * 1000;

    /// <summary>Same real-time hover-delay tracking as <see cref="_foodRationsHoverStartedAtMs"/>, for the tokens readout.</summary>
    private long? _tokensHoverStartedAtMs;

    /// <summary>Test seam — true once the tokens-readout hover delay has elapsed.</summary>
    internal bool IsTokensTooltipVisible =>
        _tokensHoverStartedAtMs is { } startedAtMs
        && Environment.TickCount64 - startedAtMs >= MenuStyle.TooltipHoverDelaySeconds * 1000;

    /// <summary>Same real-time hover-delay tracking as <see cref="_foodRationsHoverStartedAtMs"/>, for the fuel readout.</summary>
    private long? _fuelHoverStartedAtMs;

    /// <summary>Test seam — true once the fuel-readout hover delay has elapsed.</summary>
    internal bool IsFuelTooltipVisible =>
        _fuelHoverStartedAtMs is { } startedAtMs
        && Environment.TickCount64 - startedAtMs >= MenuStyle.TooltipHoverDelaySeconds * 1000;

    // EP-0034 US-0006 job board: typed transport, the in-flight acceptance and its confirmed result.
    private readonly Func<DemoCampaignSnapshot?>? _campaign;
    private readonly Func<DemoCampaignCommand, ValueTask<DemoCampaignResponse>>? _execute;
    private readonly object _jobsGate = new();
    private bool _showBounties;
    private SKRect _bountyTab;
    private readonly List<(SKRect Rect, string BountyId)> _bountyButtons = [];
    internal IReadOnlyList<TargetBountyRow> BountyRows => TargetBountyPresentation.Build(
        Campaign?.Rewards?.Bounties, Campaign?.Transport?.StationJobs?.DockedStationId, StationName);
    private readonly List<(SKRect Rect, string OfferId)> _acceptButtons = [];
    private string? _pendingCommandId;
    private string? _jobsFeedback;
    private bool _jobsFeedbackIsError;
    private int _hoveredAcceptIndex = -1;
    private string? _acceptedOfferId; // EP-0034 US-0002: last applied acceptance, its boarding read from snapshots
    // EP-0034 US-0003: cancellation of active transport orders (two-step confirm, one command in flight).
    private readonly List<(SKRect Rect, string ContractId)> _cancelButtons = [];
    private string? _armedContractId;
    private string? _cancelledContractId; // last applied cancellation, its result read from snapshots
    private int _hoveredCancelIndex = -1;
    private const float CancelButtonWidth = 150f;

    private const float JobsLeftMargin = 40f;
    private const float JobsColumnWidth = 960f;
    private const float JobsRowHeight = 96f;
    private const float AcceptButtonWidth = 150f;
    private const float JournalLeft = 1040f;
    private static readonly SKPaint JobsHeadingPaint = new() { Typeface = MenuStyle.TextButton.Typeface, TextSize = 18f, Color = TabletopStyle.Ink, IsAntialias = true };
    private static readonly SKPaint JobsTextPaint = new() { Typeface = MenuStyle.TextStatus.Typeface, TextSize = 14f, Color = TabletopStyle.Ink, IsAntialias = true };
    private static readonly SKPaint JobsDimPaint = new() { Typeface = MenuStyle.TextStatus.Typeface, TextSize = 13f, Color = TabletopStyle.Secondary, IsAntialias = true };
    private static readonly SKPaint JobsWarnPaint = new() { Typeface = MenuStyle.TextStatus.Typeface, TextSize = 13f, Color = TabletopStyle.Danger, IsAntialias = true };
    private static readonly SKPaint JobsOkPaint = new() { Typeface = MenuStyle.TextStatus.Typeface, TextSize = 14f, Color = TabletopStyle.Success, IsAntialias = true };
    private static readonly SKPaint JobsButtonPaint = new() { Typeface = MenuStyle.TextButton.Typeface, TextSize = MenuStyle.ButtonFontSize, Color = TabletopStyle.Ink, IsAntialias = true };

    public ContractsScreen(SnapshotBuffer? buffer = null, Func<DemoCampaignSnapshot?>? campaign = null,
        Func<DemoCampaignCommand, ValueTask<DemoCampaignResponse>>? execute = null)
    {
        _buffer = buffer;
        _campaign = campaign;
        _execute = execute;
    }

    /// <summary>Test seam — the board/journal exactly as drawn from the latest authoritative campaign snapshot.</summary>
    internal StationJobsView JobsView =>
        StationJobsPresentation.Build((_campaign?.Invoke() ?? _buffer?.Latest?.Snapshot.DemoCampaign)?.Transport?.StationJobs, StationName);

    /// <summary>Test seam — pending/confirmed/rejected line of the last acceptance on this screen.</summary>
    internal string? JobsFeedback { get { lock (_jobsGate) return _jobsFeedback; } }

    internal bool IsAcceptPending { get { lock (_jobsGate) return _pendingCommandId is not null; } }

    private DemoCampaignSnapshot? Campaign => _campaign?.Invoke() ?? _buffer?.Latest?.Snapshot.DemoCampaign;

    /// <summary>Test seam â€” passenger contracts section exactly as drawn (EP-0034 US-0002).</summary>
    internal PassengerContractsView PassengersView =>
        PassengerContractsPresentation.Build(Campaign?.Transport?.PassengerContracts, StationName);

    /// <summary>
    /// Test seam â€” confirmed boarding of the last applied acceptance on this screen, taken from the authoritative
    /// snapshot once it lists the contract; null before that, for cargo offers and on a freshly opened screen.
    /// </summary>
    internal string? PassengerFeedback
    {
        get
        {
            string? offerId;
            lock (_jobsGate) offerId = _acceptedOfferId;
            return PassengerContractsPresentation.Boarded(Campaign?.Transport?.PassengerContracts, offerId, StationName);
        }
    }

    /// <summary>Test seam — cargo contracts section and hold split exactly as drawn (EP-0034 US-0004).</summary>
    internal CargoContractsView CargoView =>
        CargoContractsPresentation.Build(Campaign?.Transport?.CargoContracts, StationName);

    /// <summary>
    /// Test seam — confirmed sealing of the last applied cargo acceptance on this screen, from the authoritative
    /// snapshot once it lists the contract; null before that, for passenger offers and on a freshly opened screen.
    /// </summary>
    internal string? CargoFeedback
    {
        get
        {
            string? offerId;
            lock (_jobsGate) offerId = _acceptedOfferId;
            return CargoContractsPresentation.Sealed(Campaign?.Transport?.CargoContracts, offerId, StationName);
        }
    }

    internal IReadOnlyList<(SKRect Rect, string OfferId)> AcceptButtons => _acceptButtons;

    /// <summary>Test seam — failures and cancellations section exactly as drawn (EP-0034 US-0003).</summary>
    internal TransportFailuresView FailuresView =>
        TransportFailurePresentation.Build(Campaign?.Transport?.TransportFailures, Campaign?.Transport?.StationJobs, StationName);

    /// <summary>
    /// Test seam — confirmed result of the last applied cancellation on this screen, from the authoritative snapshot
    /// once it lists the failure; null before that and on a freshly opened screen.
    /// </summary>
    internal string? CancelFeedback
    {
        get
        {
            string? contractId;
            lock (_jobsGate) contractId = _cancelledContractId;
            return TransportFailurePresentation.Cancelled(Campaign?.Transport?.TransportFailures, contractId);
        }
    }

    internal IReadOnlyList<(SKRect Rect, string ContractId)> CancelButtons => _cancelButtons;

    /// <summary>
    /// Cancel an active order the engine marked cancellable. The first request only shows the published penalty and
    /// asks for confirmation; the second sends one command. A blocked or unknown order, or a request while another
    /// command is in flight, sends nothing.
    /// </summary>
    internal Task? TryCancelContract(string contractId)
    {
        var campaign = Campaign;
        var section = campaign?.Transport?.TransportFailures;
        string? blocker = _execute is null || campaign is null
            ? DemoCampaignReasons.Unavailable
            : TransportFailureReasons.CancelBlocker(section, contractId);
        CancelTransportCommand command;
        lock (_jobsGate)
        {
            if (_pendingCommandId is not null) return null;
            if (blocker is not null)
            {
                _armedContractId = null;
                _jobsFeedback = TransportFailurePresentation.Reason(blocker);
                _jobsFeedbackIsError = true;
                return null;
            }
            if (_armedContractId != contractId)
            {
                _armedContractId = contractId;
                var terms = section!.Terms.First(t => t is not null && t.ContractId == contractId);
                _jobsFeedback = TransportFailurePresentation.Confirm(terms.CancelPenaltyCredits);
                _jobsFeedbackIsError = false;
                return null;
            }
            _armedContractId = null;
            command = new CancelTransportCommand("jobs-cancel-" + Guid.NewGuid().ToString("N"), campaign!.Revision, contractId);
            _pendingCommandId = command.CommandId;
            _acceptedOfferId = null;
            _cancelledContractId = null;
            _jobsFeedback = TransportFailurePresentation.Pending;
            _jobsFeedbackIsError = false;
        }
        return CompleteCancelAsync(command);
    }

    private async Task CompleteCancelAsync(CancelTransportCommand command)
    {
        DemoCampaignResponse response;
        try { response = await _execute!(command).ConfigureAwait(false); }
        catch (Exception) { response = new(command.CommandId, false, DemoCampaignReasons.Unavailable, command.ExpectedRevision); }
        lock (_jobsGate)
        {
            if (_pendingCommandId != command.CommandId) return;
            _pendingCommandId = null;
            _jobsFeedbackIsError = !response.Applied;
            if (response.Applied) _cancelledContractId = command.ContractId;
            // Until the snapshot publishes the failure the line keeps saying "waiting for confirmation".
            _jobsFeedback = response.Applied ? TransportFailurePresentation.Pending : TransportFailurePresentation.Reason(response.Reason);
        }
    }

    /// <summary>
    /// Send one acceptance for an offer the engine marked acceptable. A second click while a command is in flight,
    /// or a click on an offer with an engine blocker, sends nothing and only shows the reason.
    /// </summary>
    internal Task? TryAcceptOffer(string offerId)
    {
        var campaign = _campaign?.Invoke() ?? _buffer?.Latest?.Snapshot.DemoCampaign;
        string? blocker = _execute is null || campaign is null
            ? DemoCampaignReasons.Unavailable
            : StationJobReasons.AcceptBlocker(campaign.Transport?.StationJobs, offerId);
        AcceptJobOfferCommand command;
        lock (_jobsGate)
        {
            if (_pendingCommandId is not null) return null;
            if (blocker is not null)
            {
                _jobsFeedback = StationJobsPresentation.Reason(blocker);
                _jobsFeedbackIsError = true;
                return null;
            }
            command = new AcceptJobOfferCommand("jobs-accept-" + Guid.NewGuid().ToString("N"), campaign!.Revision, offerId);
            _pendingCommandId = command.CommandId;
            _acceptedOfferId = null;
            _cancelledContractId = null;
            _armedContractId = null;
            _jobsFeedback = StationJobsPresentation.Pending;
            _jobsFeedbackIsError = false;
        }
        return CompleteAcceptAsync(command);
    }

    private async Task CompleteAcceptAsync(AcceptJobOfferCommand command)
    {
        DemoCampaignResponse response;
        try { response = await _execute!(command).ConfigureAwait(false); }
        catch (Exception) { response = new(command.CommandId, false, DemoCampaignReasons.Unavailable, command.ExpectedRevision); }
        lock (_jobsGate)
        {
            if (_pendingCommandId != command.CommandId) return;
            _pendingCommandId = null;
            _jobsFeedbackIsError = !response.Applied;
            if (response.Applied) _acceptedOfferId = command.OfferId;
            _jobsFeedback = response.Applied ? StationJobsPresentation.Accepted : StationJobsPresentation.Reason(response.Reason);
        }
    }

    private string StationName(string stationId) =>
        _buffer?.Latest?.Snapshot.Objects.FirstOrDefault(o => o.ObjectId == stationId)?.DisplayName is { Length: > 0 } name ? name : stationId;

    internal Task? TryToggleBounty(string bountyId)
    {
        var campaign = Campaign;
        var row = BountyRows.FirstOrDefault(r => r.Order.BountyId == bountyId);
        if (_execute is null || campaign is null || row?.CanToggle != true) return null;
        DemoCampaignCommand command;
        lock (_jobsGate)
        {
            if (_pendingCommandId is not null) return null;
            string id = "bounty-track-" + Guid.NewGuid().ToString("N");
            command = row.Order.Tracking ? new CancelBountyCommand(id, campaign.Revision, bountyId)
                : new AcceptBountyCommand(id, campaign.Revision, bountyId);
            _pendingCommandId = id;
            _jobsFeedback = Localization.Get("bounty.pending");
            _jobsFeedbackIsError = false;
        }
        return CompleteBountyAsync(command);
    }

    private async Task CompleteBountyAsync(DemoCampaignCommand command)
    {
        DemoCampaignResponse response;
        try { response = await _execute!(command).ConfigureAwait(false); }
        catch (Exception) { response = new(command.CommandId, false, DemoCampaignReasons.Unavailable, command.ExpectedRevision); }
        lock (_jobsGate)
        {
            if (_pendingCommandId != command.CommandId) return;
            _pendingCommandId = null;
            _jobsFeedbackIsError = !response.Applied;
            _jobsFeedback = Localization.Get(response.Applied ? "bounty.confirmed" : "bounty.failed");
            if (!response.Applied && response.Reason is { } reason)
                _jobsFeedback += " " + (reason.StartsWith("bounty_", StringComparison.Ordinal)
                    ? Localization.Get("bounty." + reason) : StationJobsPresentation.Reason(reason));
        }
    }

    public void OnActivated()
    {
        _isStationNameHovered = false;
        _isExitButtonHovered = false;
        _foodRationsHoverStartedAtMs = null;
        _crewHoverStartedAtMs = null;
        _tokensHoverStartedAtMs = null;
        _fuelHoverStartedAtMs = null;
        _hoveredAcceptIndex = -1;
        _hoveredCancelIndex = -1;
    }

    public void OnDeactivated()
    {
        lock (_jobsGate) _armedContractId = null; // a confirmation never survives closing the screen
    }

    public ScreenEvent OnKeyDown(Key key) =>
        key == Key.Escape ? ScreenEvent.CloseContracts : ScreenEvent.None;

    public ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left)
            return ScreenEvent.None;

        if (IsExitButtonHit(x, y))
            return ScreenEvent.CloseContracts;

        if (IsStationNameHit(x, y))
            return ScreenEvent.NavigateToStation;

        if (_bountyTab.Contains(x, y))
        {
            _showBounties = !_showBounties;
            _acceptButtons.Clear();
            _cancelButtons.Clear();
            _bountyButtons.Clear();
            return ScreenEvent.None;
        }
        foreach (var (rect, bountyId) in _bountyButtons)
            if (rect.Contains(x, y)) { _ = TryToggleBounty(bountyId); return ScreenEvent.None; }

        foreach (var (rect, offerId) in _acceptButtons)
            if (rect.Contains(x, y))
            {
                _ = TryAcceptOffer(offerId);
                return ScreenEvent.None;
            }

        foreach (var (rect, contractId) in _cancelButtons)
            if (rect.Contains(x, y))
            {
                _ = TryCancelContract(contractId);
                return ScreenEvent.None;
            }

        // Click on the dimmed background outside the panel also closes it.
        if (!ContractsLayout.IsInsidePanel(x, y, _screenWidth, _screenHeight))
            return ScreenEvent.CloseContracts;

        return ScreenEvent.None;
    }

    /// <summary>Convenience shortcut for a left click — kept for existing call-site/test conventions.</summary>
    public ScreenEvent OnMouseDown(float x, float y) => OnMouseDown(x, y, MouseButton.Left);

    public bool OnMouseMove(float x, float y)
    {
        _isStationNameHovered = IsStationNameHit(x, y);
        _isExitButtonHovered = IsExitButtonHit(x, y);
        _hoveredAcceptIndex = _acceptButtons.FindIndex(b => b.Rect.Contains(x, y));
        _hoveredCancelIndex = _cancelButtons.FindIndex(b => b.Rect.Contains(x, y));

        // Not a button — hovering it only shows a delayed tooltip (see Render), so it must
        // not affect the interactive-cursor swap the way the name link / exit button do.
        if (IsFoodRationsHit(x, y))
            _foodRationsHoverStartedAtMs ??= Environment.TickCount64;
        else
            _foodRationsHoverStartedAtMs = null;

        if (IsCrewHit(x, y))
            _crewHoverStartedAtMs ??= Environment.TickCount64;
        else
            _crewHoverStartedAtMs = null;

        if (IsTokensHit(x, y))
            _tokensHoverStartedAtMs ??= Environment.TickCount64;
        else
            _tokensHoverStartedAtMs = null;

        if (IsFuelHit(x, y))
            _fuelHoverStartedAtMs ??= Environment.TickCount64;
        else
            _fuelHoverStartedAtMs = null;

        return _isStationNameHovered || _isExitButtonHovered || _hoveredAcceptIndex >= 0 || _hoveredCancelIndex >= 0;
    }

    public ScreenEvent OnMouseWheel(float x, float y, float delta) => ScreenEvent.None;

    public void Render(SKCanvas canvas, int width, int height)
    {
        _screenWidth = width;
        _screenHeight = height;

        float pl = ContractsLayout.PanelLeft(width);
        float pt = ContractsLayout.PanelTop(height);
        var panelRect = new SKRect(pl, pt, pl + ContractsLayout.PanelWidth, pt + ContractsLayout.PanelHeight);
        MenuStyle.DrawPanel(canvas, panelRect);

        var snapshot = _buffer?.Latest?.Snapshot;
        string? stationName = StationToolbar.ResolveDockedStationName(snapshot);
        StationToolbar.Draw(canvas, pl, pt, stationName, isStationHub: false, isHovered: _isStationNameHovered,
            windowName: "CONTRACTS", isExitButtonHovered: _isExitButtonHovered,
            foodRationsCount: StationToolbar.ResolveFoodRationsCount(snapshot),
            crewCount: StationToolbar.ResolveCrewCount(snapshot),
            cabinsCount: StationToolbar.ResolveCabinsCount(snapshot),
            creditsCount: StationToolbar.ResolveCreditsCount(snapshot),
            fuelAmountKg: StationToolbar.ResolveFuelAmountKg(snapshot),
            fuelCapacityKg: StationToolbar.ResolveFuelCapacityKg(snapshot), gameTimeMs: snapshot?.GameTimeMs, missingRations: snapshot?.MissingRations ?? 0);

        _bountyTab = new(pl + ContractsLayout.PanelWidth - 270, pt + ContractsLayout.BodyStartY,
            pl + ContractsLayout.PanelWidth - 40, pt + ContractsLayout.BodyStartY + 28);
        TabletopStyle.Button(canvas, _bountyTab, ButtonState.Normal, primary: false);
        JobsButtonPaint.Color = TabletopStyle.Ink;
        TabletopStyle.Label(canvas, _bountyTab, Localization.Get(_showBounties ? "Demo.Jobs.Title" : "bounty.title"), JobsButtonPaint);
        if (_showBounties) DrawBounties(canvas, pl, pt);
        else DrawJobs(canvas, pl, pt);

        // Drawn last: the tooltip hangs below the toolbar into the body area and must
        // stay on top of everything the screen drew.
        StationToolbar.DrawTooltips(canvas, pl, pt,
            isFoodRationsHovered: IsFoodRationsTooltipVisible,
            isCrewHovered: IsCrewTooltipVisible,
            isTokensHovered: IsTokensTooltipVisible,
            isFuelHovered: IsFuelTooltipVisible);
    }

    private void DrawBounties(SKCanvas canvas, float pl, float pt)
    {
        _acceptButtons.Clear();
        _cancelButtons.Clear();
        _bountyButtons.Clear();
        float left = pl + JobsLeftMargin;
        float width = ContractsLayout.PanelWidth - 2 * JobsLeftMargin;
        float y = pt + ContractsLayout.BodyStartY;
        Text(canvas, Localization.Get("bounty.title"), left, y, width - 250, JobsHeadingPaint);
        y += 42;
        Text(canvas, Localization.Get("bounty.cancellation_free"), left, y, width, JobsDimPaint);
        y += 30;
        foreach (var row in BountyRows)
        {
            var card = new SKRect(left, y, left + width, y + 150);
            TabletopStyle.Field(canvas, card);
            float textWidth = width - 220;
            Text(canvas, row.Headline, left + 12, y + 8, textWidth, JobsTextPaint);
            Text(canvas, row.Terms, left + 12, y + 32, textWidth, JobsDimPaint);
            Text(canvas, row.Status, left + 12, y + 56, textWidth, JobsTextPaint);
            Text(canvas, row.PaymentLocation, left + 12, y + 80, textWidth, JobsDimPaint);
            if (row.Order.SearchRegion is { } region)
                Text(canvas, region, left + 12, y + 104, textWidth, JobsDimPaint);
            var button = new SKRect(card.Right - 200, y + 20, card.Right - 12, y + 56);
            var state = row.CanToggle && !IsAcceptPending ? ButtonState.Normal : ButtonState.Disabled;
            TabletopStyle.Button(canvas, button, state, primary: false);
            JobsButtonPaint.Color = TabletopStyle.ButtonInk(state, primary: false);
            TabletopStyle.Label(canvas, button, row.ToggleLabel, JobsButtonPaint);
            _bountyButtons.Add((button, row.Order.BountyId));
            y += 164;
        }
        if (JobsFeedback is { } feedback)
            Text(canvas, feedback, left, pt + ContractsLayout.PanelHeight - 44, width, JobsTextPaint);
    }

    /// <summary>Board of the docked station on the left, the journal on the right, the last result at the bottom.</summary>
    private void DrawJobs(SKCanvas canvas, float pl, float pt)
    {
        var view = JobsView;
        _acceptButtons.Clear();
        float left = pl + JobsLeftMargin;
        float y = pt + ContractsLayout.BodyStartY;

        Text(canvas, view.Title, left, y, JobsColumnWidth, JobsHeadingPaint);
        y += 30;
        if (view.Notice is { } notice)
        {
            Text(canvas, notice, left, y, JobsColumnWidth, JobsDimPaint);
            y += 26;
        }
        // EP-0034 US-0004: hold split before any acceptance — sealed contract cargo apart from the player's goods.
        if (CargoView.Hold is { } hold)
        {
            Text(canvas, hold, left, y, JobsColumnWidth, JobsDimPaint);
            y += 26;
        }
        if (view.BoardEmpty is { } empty)
            Text(canvas, empty, left, y, JobsColumnWidth, JobsDimPaint);

        bool pending = IsAcceptPending;
        foreach (var row in view.Offers)
        {
            var card = new SKRect(left, y, left + JobsColumnWidth, y + JobsRowHeight - 8);
            TabletopStyle.Field(canvas, card);
            float textWidth = JobsColumnWidth - AcceptButtonWidth - 36;
            Text(canvas, row.Headline, card.Left + 12, card.Top + 6, textWidth, JobsTextPaint);
            Text(canvas, row.Terms, card.Left + 12, card.Top + 26, textWidth, JobsDimPaint);
            Text(canvas, row.Payout, card.Left + 12, card.Top + 46, textWidth, JobsDimPaint);
            if (row.Blocker is { } blocker)
                Text(canvas, blocker, card.Left + 12, card.Top + 66, textWidth, JobsWarnPaint);

            var button = new SKRect(card.Right - AcceptButtonWidth - 12, card.MidY - 18, card.Right - 12, card.MidY + 18);
            int index = _acceptButtons.Count;
            _acceptButtons.Add((button, row.OfferId));
            var state = !row.CanAccept || pending ? ButtonState.Disabled
                : index == _hoveredAcceptIndex ? ButtonState.Hovered : ButtonState.Normal;
            TabletopStyle.Button(canvas, button, state, primary: row.CanAccept);
            var label = JobsButtonPaint;
            label.Color = TabletopStyle.ButtonInk(state, primary: row.CanAccept);
            TabletopStyle.Label(canvas, button, StationJobsPresentation.AcceptLabel, label);
            y += JobsRowHeight;
        }

        float journalLeft = pl + JournalLeft;
        float journalWidth = ContractsLayout.PanelWidth - JournalLeft - JobsLeftMargin;
        float jy = pt + ContractsLayout.BodyStartY;
        Text(canvas, view.JournalTitle, journalLeft, jy, journalWidth - 240, JobsHeadingPaint);
        jy += 30;
        if (view.JournalEmpty is { } none)
            Text(canvas, none, journalLeft, jy, journalWidth, JobsDimPaint);
        float journalBottom = pt + ContractsLayout.PanelHeight - 60;
        foreach (string line in view.Journal)
        {
            if (jy + ContractsLayout.BodyLineHeight > journalBottom) break;
            foreach (string wrapped in ControlText.Wrap(ControlText.SafeSymbols(line), journalWidth, JobsTextPaint).Take(2))
            {
                Text(canvas, wrapped, journalLeft, jy, journalWidth, JobsTextPaint);
                jy += 20;
            }
            jy += 8;
        }
        jy = DrawFailures(canvas, journalLeft, jy + 12, journalWidth, journalBottom);
        jy = DrawPassengers(canvas, journalLeft, jy + 12, journalWidth, journalBottom);
        DrawCargo(canvas, journalLeft, jy + 12, journalWidth, journalBottom); // EP-0034 US-0004

        string? feedback;
        bool error;
        lock (_jobsGate) { feedback = _jobsFeedback; error = _jobsFeedbackIsError; }
        // A confirmed passenger boarding replaces the generic "accepted" line once the snapshot shows it.
        if (!error && PassengerFeedback is { } boarded) feedback = boarded;
        if (!error && CancelFeedback is { } cancelled) feedback = cancelled; // EP-0034 US-0003
        if (!error && CargoFeedback is { } sealedCargo) feedback = sealedCargo; // EP-0034 US-0004
        if (feedback is not null)
            Text(canvas, feedback, left, pt + ContractsLayout.PanelHeight - 44, ContractsLayout.PanelWidth - 2 * JobsLeftMargin,
                error ? JobsWarnPaint : JobsOkPaint);
    }

    /// <summary>
    /// EP-0034 US-0003: calendar rule, terms of every active order with its cancel button, failures with penalty split
    /// and passenger whereabouts, contract debt. Returns the next free y.
    /// </summary>
    private float DrawFailures(SKCanvas canvas, float left, float y, float width, float bottom)
    {
        _cancelButtons.Clear();
        if (y + ContractsLayout.BodyLineHeight > bottom) return y;
        var view = FailuresView;
        Text(canvas, view.Title, left, y, width, JobsHeadingPaint);
        y += 30;
        foreach (string? note in new[] { view.Calendar, view.Debt })
            y = Wrapped(canvas, note, left, y, width, bottom, note == view.Debt ? JobsWarnPaint : JobsDimPaint);

        bool pending = IsAcceptPending;
        string? armed;
        lock (_jobsGate) armed = _armedContractId;
        float textWidth = width - CancelButtonWidth - 12;
        foreach (var row in view.Active)
        {
            if (y + 2 * ContractsLayout.BodyLineHeight > bottom) break;
            float top = y;
            Text(canvas, row.Headline, left, y, textWidth, JobsTextPaint);
            y += 20;
            y = Wrapped(canvas, row.Terms, left, y, textWidth, bottom, JobsDimPaint);
            if (row.Blocker is { } blocker) y = Wrapped(canvas, blocker, left, y, textWidth, bottom, JobsWarnPaint);
            var button = new SKRect(left + width - CancelButtonWidth, top, left + width, top + 34);
            int index = _cancelButtons.Count;
            _cancelButtons.Add((button, row.ContractId));
            var state = !row.CanCancel || pending ? ButtonState.Disabled
                : index == _hoveredCancelIndex || armed == row.ContractId ? ButtonState.Hovered : ButtonState.Normal;
            TabletopStyle.Button(canvas, button, state, primary: false);
            var label = JobsButtonPaint;
            label.Color = TabletopStyle.ButtonInk(state, primary: false);
            TabletopStyle.Label(canvas, button, TransportFailurePresentation.CancelLabel, label);
            y = Math.Max(y, button.Bottom + 4) + 4;
        }

        foreach (string? line in view.Lines.Cast<string?>().Append(view.Empty))
            y = Wrapped(canvas, line, left, y, width, bottom, JobsTextPaint);
        return y;
    }

    private static float Wrapped(SKCanvas canvas, string? line, float left, float y, float width, float bottom, SKPaint paint)
    {
        if (line is null || y + ContractsLayout.BodyLineHeight > bottom) return y;
        foreach (string wrapped in ControlText.Wrap(ControlText.SafeSymbols(line), width, paint).Take(2))
        {
            Text(canvas, wrapped, left, y, width, paint);
            y += 20;
        }
        return y + 6;
    }

    /// <summary>EP-0034 US-0002: people aboard (life support), each passenger contract and its confirmed delivery.</summary>
    private float DrawPassengers(SKCanvas canvas, float left, float y, float width, float bottom)
    {
        if (y + ContractsLayout.BodyLineHeight > bottom) return y;
        var view = PassengersView;
        Text(canvas, view.Title, left, y, width, JobsHeadingPaint);
        y += 30;
        foreach (string? line in new[] { view.Summary, view.Empty })
        {
            if (line is null || y + ContractsLayout.BodyLineHeight > bottom) continue;
            foreach (string wrapped in ControlText.Wrap(ControlText.SafeSymbols(line), width, JobsDimPaint).Take(2))
            {
                Text(canvas, wrapped, left, y, width, JobsDimPaint);
                y += 20;
            }
            y += 6;
        }
        foreach (string line in view.Lines)
        {
            if (y + ContractsLayout.BodyLineHeight > bottom) break;
            foreach (string wrapped in ControlText.Wrap(ControlText.SafeSymbols(line), width, JobsTextPaint).Take(2))
            {
                Text(canvas, wrapped, left, y, width, JobsTextPaint);
                y += 20;
            }
            y += 6;
        }
        return y;
    }

    /// <summary>
    /// EP-0034 US-0004: the not-for-sale rule while cargo is sealed, each cargo contract with its hold state and the
    /// confirmed delivery or return. The hold split itself is drawn under the board title.
    /// </summary>
    private void DrawCargo(SKCanvas canvas, float left, float y, float width, float bottom)
    {
        if (y + ContractsLayout.BodyLineHeight > bottom) return;
        var view = CargoView;
        Text(canvas, view.Title, left, y, width, JobsHeadingPaint);
        y += 30;
        foreach (string? note in new[] { view.NotForSale, view.Empty })
            y = Wrapped(canvas, note, left, y, width, bottom, JobsDimPaint);
        foreach (string line in view.Lines)
            y = Wrapped(canvas, line, left, y, width, bottom, JobsTextPaint);
    }

    private static void Text(SKCanvas canvas, string text, float x, float y, float width, SKPaint paint)
    {
        var rect = new SKRect(x, y, x + width, y + paint.TextSize + 6);
        ControlVisuals.DrawLabel(canvas, ControlText.Ellipsize(ControlText.SafeSymbols(text), width, paint), rect, paint);
    }

    /// <summary>True when (x, y) lands on the toolbar's station-name link (see StationToolbar).</summary>
    private bool IsStationNameHit(float x, float y)
    {
        string? stationName = StationToolbar.ResolveDockedStationName(_buffer?.Latest?.Snapshot);
        if (string.IsNullOrEmpty(stationName))
            return false;

        float pl = ContractsLayout.PanelLeft(_screenWidth);
        float pt = ContractsLayout.PanelTop(_screenHeight);
        var local = StationToolbar.NameLocalRect(stationName);
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>True when (x, y) lands on the toolbar's exit-button icon (see StationToolbar).</summary>
    private bool IsExitButtonHit(float x, float y)
    {
        float pl = ContractsLayout.PanelLeft(_screenWidth);
        float pt = ContractsLayout.PanelTop(_screenHeight);
        var local = StationToolbar.ExitButtonLocalRect();
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>True when (x, y) lands on the toolbar's food-rations readout (see StationToolbar).</summary>
    private bool IsFoodRationsHit(float x, float y)
    {
        float pl = ContractsLayout.PanelLeft(_screenWidth);
        float pt = ContractsLayout.PanelTop(_screenHeight);
        var local = StationToolbar.FoodRationsLocalRect();
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>True when (x, y) lands on the toolbar's crew readout (see StationToolbar).</summary>
    private bool IsCrewHit(float x, float y)
    {
        float pl = ContractsLayout.PanelLeft(_screenWidth);
        float pt = ContractsLayout.PanelTop(_screenHeight);
        var local = StationToolbar.CrewLocalRect();
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>True when (x, y) lands on the toolbar's tokens/credits readout (see StationToolbar).</summary>
    private bool IsTokensHit(float x, float y)
    {
        float pl = ContractsLayout.PanelLeft(_screenWidth);
        float pt = ContractsLayout.PanelTop(_screenHeight);
        var local = StationToolbar.TokensLocalRect();
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>True when (x, y) lands on the toolbar's fuel readout (see StationToolbar).</summary>
    private bool IsFuelHit(float x, float y)
    {
        float pl = ContractsLayout.PanelLeft(_screenWidth);
        float pt = ContractsLayout.PanelTop(_screenHeight);
        var local = StationToolbar.FuelLocalRect();
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }
}
