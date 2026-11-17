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
    private readonly List<(SKRect Rect, string OfferId)> _acceptButtons = [];
    private string? _pendingCommandId;
    private string? _jobsFeedback;
    private bool _jobsFeedbackIsError;
    private int _hoveredAcceptIndex = -1;
    private string? _acceptedOfferId; // EP-0034 US-0002: last applied acceptance, its boarding read from snapshots

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

    internal IReadOnlyList<(SKRect Rect, string OfferId)> AcceptButtons => _acceptButtons;

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

    public void OnActivated()
    {
        _isStationNameHovered = false;
        _isExitButtonHovered = false;
        _foodRationsHoverStartedAtMs = null;
        _crewHoverStartedAtMs = null;
        _tokensHoverStartedAtMs = null;
        _fuelHoverStartedAtMs = null;
        _hoveredAcceptIndex = -1;
    }

    public void OnDeactivated() { }

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

        foreach (var (rect, offerId) in _acceptButtons)
            if (rect.Contains(x, y))
            {
                _ = TryAcceptOffer(offerId);
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

        return _isStationNameHovered || _isExitButtonHovered || _hoveredAcceptIndex >= 0;
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

        DrawJobs(canvas, pl, pt);

        // Drawn last: the tooltip hangs below the toolbar into the body area and must
        // stay on top of everything the screen drew.
        StationToolbar.DrawTooltips(canvas, pl, pt,
            isFoodRationsHovered: IsFoodRationsTooltipVisible,
            isCrewHovered: IsCrewTooltipVisible,
            isTokensHovered: IsTokensTooltipVisible,
            isFuelHovered: IsFuelTooltipVisible);
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
        Text(canvas, view.JournalTitle, journalLeft, jy, journalWidth, JobsHeadingPaint);
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
        DrawPassengers(canvas, journalLeft, jy + 12, journalWidth, journalBottom);

        string? feedback;
        bool error;
        lock (_jobsGate) { feedback = _jobsFeedback; error = _jobsFeedbackIsError; }
        // A confirmed passenger boarding replaces the generic "accepted" line once the snapshot shows it.
        if (!error && PassengerFeedback is { } boarded) feedback = boarded;
        if (feedback is not null)
            Text(canvas, feedback, left, pt + ContractsLayout.PanelHeight - 44, ContractsLayout.PanelWidth - 2 * JobsLeftMargin,
                error ? JobsWarnPaint : JobsOkPaint);
    }

    /// <summary>EP-0034 US-0002: people aboard (life support), each passenger contract and its confirmed delivery.</summary>
    private void DrawPassengers(SKCanvas canvas, float left, float y, float width, float bottom)
    {
        if (y + ContractsLayout.BodyLineHeight > bottom) return;
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
