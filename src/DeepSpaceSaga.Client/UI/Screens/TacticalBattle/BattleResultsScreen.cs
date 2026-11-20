using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Demo;
using DeepSpaceSaga.Client.UI.Screens.Station;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed partial class BattleResultsScreen(TacticalBattleFlow flow, float uiScale)
    : TacticalBattleModalScreen(flow, isSetup: false, uiScale)
{
    internal enum LootStep { Pilots, Components, Scrap }
    private LootStep? _lootStep;
    internal LootStep? CurrentStep => _lootStep ??= CurrentLoot is { } loot
        ? loot.Objects.Any(source => source.Kind == "DisabledDrone" && source.Pilot is { Alive: true, Captured: false, CaptureReason: null })
            ? LootStep.Pilots : LootStep.Components
        : null;

    private SKRect _exitBounds;
    private bool _acknowledgeRequested;
    internal bool ConfirmDiscard { get; private set; }
    private readonly TacticalBattleSnapshot? _terminalBattle = flow.State;
    private readonly BattleLootPresentation _lootPresentation = CreateLootPresentation(flow);
    internal SKRect PanelBounds { get; private set; }
    internal SKRect Contents { get; private set; }
    internal SKRect ExitBounds => _exitBounds;

    private static BattleLootPresentation CreateLootPresentation(TacticalBattleFlow flow)
    {
        var presentation = new BattleLootPresentation(command => flow.Session.ExecuteDemoCampaignAsync(command).AsTask());
        if (flow.OpenedResultsReceipt is { Rewards.Loot: { } loot } receipt && loot.BattleId == flow.State?.BattleId)
            presentation.Observe(loot, receipt.Revision);
        return presentation;
    }

    internal Summary? Report
    {
        get
        {
            var campaign = Flow.Session.DemoCampaign;
            return _terminalBattle is { } battle
                ? Summary.From(battle, campaign?.BattleResults, _lootPresentation.State ?? campaign?.Rewards?.Loot)
                : null;
        }
    }

    public override void Render(SKCanvas canvas, int width, int height)
    {
        canvas.Clear(MapBg);
        float left = StationLayout.PanelLeft(width);
        float top = StationLayout.PanelTop(height);
        PanelBounds = new(left, top, left + StationLayout.PanelWidth, top + StationLayout.PanelHeight);
        MenuStyle.DrawPanel(canvas, PanelBounds);
        // Station chrome keeps its fixed geometry. Keep controls reachable inside its visible part.
        var panel = SKRect.Intersect(PanelBounds, new(0, 0, width, height));
        T(canvas, Localization.Get("TacticalBattle.Results"), panel.Left + 20, panel.Top + 42, 26, Text, true);
        if (Report is { } report) DrawSummary(canvas, report, panel);
        _exitBounds = new(panel.Right - 220, panel.Bottom - 65, panel.Right - 20, panel.Bottom - 20);
        var step = CurrentStep;
        if (step is { } activeStep && !ConfirmDiscard)
            T(canvas, ResultsText("Step" + activeStep), panel.Left + 20, panel.Top + 143, 20, Text, true);
        Contents = new(panel.Left + 20, panel.Top + (step is null || ConfirmDiscard ? 148 : 158), panel.Right - 20, _exitBounds.Top - 53);
        if (ConfirmDiscard) DrawLeaveWarning(canvas);
        else DrawSources(canvas);
        string nextLabel = step switch
        {
            LootStep.Pilots => ResultsText("NextComponents"),
            LootStep.Components => ResultsText("NextScrap"),
            _ => Localization.Get("TacticalBattle.ToMap")
        };
        MenuStyle.DrawButton(canvas, _exitBounds, ConfirmDiscard ? ResultsText("Leave") : nextLabel,
            ExitBlocked ? ButtonState.Disabled : ButtonState.Normal);
        DrawCapacity(canvas);
        if (!ConfirmDiscard) DrawLootFooter(canvas);
        if (Flow.Error.Length > 0)
            T(canvas, Flow.Error, panel.Left + 20, _exitBounds.Top - 35, 16, Red);
    }

    private static void DrawSummary(SKCanvas canvas, Summary report, SKRect panel)
    {
        string outcome = Localization.Get("TacticalBattle.Outcome." + report.Outcome);
        T(canvas, outcome, panel.Left + 20, panel.Top + 78, 22, report.Outcome == "Victory" ? Yellow : Text, true);
        string losses = string.Format(Localization.Get("TacticalBattle.Ui.LossValue"), report.OwnLosses, report.EnemyLosses);
        string stats = string.Join("    ", $"{Localization.Get("TacticalBattle.Ui.Turns")} {report.Turns}",
            $"{Localization.Get("TacticalBattle.Ui.Shots")} {report.Shots}", $"{Localization.Get("TacticalBattle.Ui.Hits")} {report.Hits}",
            $"{Localization.Get("TacticalBattle.Ui.Losses")} · {losses}");
        if (report.ConsumedTorpedoes is { } torpedoes)
            stats += "    " + string.Format(Localization.Get("TacticalBattle.Results.Torpedoes"), torpedoes);
        T(canvas, stats, panel.Left + 20, panel.Top + 112, FitSize(stats, 18, panel.Width - 40), Text);
    }

    internal sealed record Summary(string Outcome, int Turns, long Shots, long Hits, int OwnLosses, int EnemyLosses,
        int? ConsumedTorpedoes, bool CanLoot)
    {
        internal static Summary? From(TacticalBattleSnapshot battle, BattleResultsSnapshot? result, LootSessionSnapshot? loot)
        {
            if (result?.BattleId != battle.BattleId) result = null;
            if ((result?.Outcome ?? battle.Outcome) is not { } outcome) return null;
            bool canLoot = result?.CanLoot ?? (loot is { Phase: LootPhases.Open } && loot.BattleId == battle.BattleId);
            return new(PhasePanel.OutcomeTitle(battle with { Outcome = outcome }, campaign: false), outcome.Turns,
                outcome.Statistics.Shots, outcome.Statistics.Hits, outcome.Losses.Where(loss => loss.Side == battle.PlayerSide).Sum(loss => loss.Count),
                outcome.Losses.Where(loss => loss.Side != battle.PlayerSide).Sum(loss => loss.Count), result?.ConsumedTorpedoes, canLoot);
        }
    }

    private ScreenEvent RequestExit()
    {
        if (ExitBlocked) return ScreenEvent.None;
        if (CurrentStep is { } step && step != LootStep.Scrap)
        {
            _lootStep = step == LootStep.Pilots ? LootStep.Components : LootStep.Scrap;
            _expandedSources = _expandedSources.Clear();
            _sourceHits.Clear();
            _rowHits.Clear();
            _retryBounds = _collectAllBounds = default;
            _scroll = _maxScroll = 0;
            return ScreenEvent.None;
        }
        if (!ConfirmDiscard && CurrentLoot is { HasUnclaimed: true })
        {
            ConfirmDiscard = true;
            _warningScroll = 0;
            return ScreenEvent.None;
        }
        _acknowledgeRequested = true;
        return ScreenEvent.AcknowledgeBattleResults;
    }

    internal bool ConsumeAcknowledgeRequest()
    {
        bool requested = _acknowledgeRequested;
        _acknowledgeRequested = false;
        return requested;
    }

    public override ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left) return ScreenEvent.None;
        if (ConfirmDiscard && StayBounds.Contains(x, y) && !Flow.Pending)
        {
            ConfirmDiscard = false;
            return ScreenEvent.None;
        }
        if (_exitBounds.Contains(x, y)) return RequestExit();
        if (!ConfirmDiscard && !ClickLootAction(x, y) && Contents.Contains(x, y)) ToggleSource(x, y);
        return ScreenEvent.None;
    }
    public override bool OnMouseMove(float x, float y) => _exitBounds.Contains(x, y) || Contents.Contains(x, y);
    public override ScreenEvent OnMouseWheel(float x, float y, float delta)
    {
        if (!Contents.Contains(x, y) || !float.IsFinite(delta)) return ScreenEvent.None;
        if (ConfirmDiscard) _warningScroll = Math.Clamp(_warningScroll + delta, 0, _warningMaxScroll);
        else
        {
            float offset = Math.Clamp(_scroll + delta, 0, _maxScroll);
            if (offset != _scroll)
            {
                _scroll = offset;
                _sourceHits.Clear();
                _rowHits.Clear();
            }
        }
        return ScreenEvent.None;
    }
    public override ScreenEvent OnKeyDown(Key key, KeyModifiers modifiers, bool repeat)
    {
        if (Flow.Pending || repeat || modifiers != KeyModifiers.None) return ScreenEvent.None;
        if (key is Key.F5 or Key.F9)
            return CanSaveLoad ? (key == Key.F5 ? ScreenEvent.QuickSave : ScreenEvent.QuickLoad) : ScreenEvent.None;
        if (key == Key.Escape) ConfirmDiscard = false;
        return !ConfirmDiscard && key is Key.Enter or Key.KeypadEnter ? RequestExit() : ScreenEvent.None;
    }

    private int _saveLoadInProgress;
    internal bool SaveLoadInProgress => Volatile.Read(ref _saveLoadInProgress) != 0;
    internal bool CanSaveLoad => !ConfirmDiscard && !ExitBlocked && !Flow.ResultsCloseCheckpointPending;
    internal bool TryBeginSaveLoad()
    {
        if (!CanSaveLoad || Interlocked.CompareExchange(ref _saveLoadInProgress, 1, 0) != 0) return false;
        if (!ConfirmDiscard && !HasLootActivity && !Flow.ResultsCloseCheckpointPending && CurrentLoot?.Operations.IsEmpty != false) return true;
        EndSaveLoad();
        return false;
    }

    internal void EndSaveLoad() => Volatile.Write(ref _saveLoadInProgress, 0);
}
