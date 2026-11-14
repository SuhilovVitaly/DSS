namespace DeepSpaceSaga.Client.UI.Screens.Tutorial;

internal enum TutorialStepState { Waiting, Shown, Completed, Dismissed, Expired }

internal sealed record TutorialEvent(string StepId, TutorialStepState State, string Phase, int Turn, long AtMs);

/// <summary>A shown line; SignalUnitId is the unit of the step's signal at show time (target of the {signal} anchor).</summary>
internal sealed record TutorialLine(TutorialStep Step, long ShownAtMs, string? SignalUnitId = null);

/// <summary>
/// Deterministic, advisory step logic: decides which single line is shown from a sequence of frame observations and closes
/// a step once the player performed the action. It never blocks input and never talks to the Engine.
/// Quiet rules (US-0002-TK-0004, pacing from <see cref="TutorialScript.Limits"/>): no new line while the player is interacting
/// or the frame is busy; a new line only at a speech moment (phase/turn change or a step closed/completed) and at least
/// MinGapMs after the previous line closed; at most MaxLinesPerPhase lines per (phase, turn) except outcome lines; a line
/// left untouched for AutoCollapseMs collapses (an acknowledge-only line expires instead). Time is the observation clock only.
/// </summary>
internal sealed class TutorialDirector(TutorialScript script)
{
    private readonly Dictionary<string, TutorialStepState> _states =
        script.Steps.ToDictionary(s => s.Id, _ => TutorialStepState.Waiting, StringComparer.Ordinal);
    private readonly List<string> _heard = [];
    private readonly List<TutorialEvent> _events = [];
    private TutorialObservation? _last;
    private string? _shownPhase;
    private int _shownTurn;
    private string? _lastPilot;
    private bool _pilotCycled;
    private bool _enabled = true;
    private bool _speechMoment;
    private long? _lastClosedAtMs;
    private int _phaseLines;
    private long _collapseFromMs;

    /// <summary>The shown line was folded into the comms chip by the auto-collapse timer; it still waits for the action.</summary>
    internal bool Collapsed { get; private set; }

    /// <summary>Unfolds a collapsed line; the collapse timer restarts from the last observed frame.</summary>
    internal void Expand()
    {
        if (!Collapsed) return;
        Collapsed = false;
        _collapseFromMs = _last?.NowMs ?? 0;
    }

    /// <summary>False: nothing is shown (a visible line is dismissed); completions are still tracked.</summary>
    internal bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            if (!value && Current is not null) Close(TutorialStepState.Dismissed);
        }
    }

    internal TutorialLine? Current { get; private set; }

    /// <summary>IDs of shown steps in display order (journal source).</summary>
    internal IReadOnlyList<string> Heard => _heard;

    internal TutorialStepState StateOf(string stepId) => _states.GetValueOrDefault(stepId, TutorialStepState.Waiting);

    internal void Observe(TutorialObservation observation)
    {
        bool newPhase = _last is null || _last.Phase != observation.Phase || _last.Turn != observation.Turn;
        if (newPhase) { _lastPilot = null; _pilotCycled = false; _phaseLines = 0; _speechMoment = true; }
        if (observation.SelectedDroneId is { } pilot)
        {
            if (_lastPilot is not null && _lastPilot != pilot) _pilotCycled = true;
            _lastPilot = pilot;
        }
        _last = observation;

        if (Current is { } line)
        {
            bool left = observation.Phase != _shownPhase || observation.Turn != _shownTurn;
            if (Done(line.Step.Complete, observation, left)) Close(TutorialStepState.Completed);
            else if (left) Close(TutorialStepState.Expired);
        }

        foreach (var step in script.Steps)
        {
            if (_states[step.Id] == TutorialStepState.Waiting && Eligible(step, observation) && Done(step.Complete, observation, false))
            {
                Record(step.Id, TutorialStepState.Completed);
                _speechMoment = true; // the player's own action is a natural pause
            }
        }

        bool quiet = observation.Interacting || observation.Busy;
        if (Current is { } shown && !Collapsed && !quiet && observation.NowMs - _collapseFromMs >= script.Limits.AutoCollapseMs)
        {
            if (shown.Step.Complete == TutorialCompletion.Acknowledged) Close(TutorialStepState.Expired);
            else Collapsed = true;
        }

        if (!_enabled || Current is not null || quiet || !_speechMoment) return;
        if (_lastClosedAtMs is { } closed && observation.NowMs - closed < script.Limits.MinGapMs) return;
        bool capped = _phaseLines >= script.Limits.MaxLinesPerPhase;
        TutorialStep? next = null;
        foreach (var step in script.Steps)
        {
            if (_states[step.Id] == TutorialStepState.Waiting && (!capped || IsOutcome(step)) && Eligible(step, observation) &&
                (next is null || step.Priority > next.Priority))
                next = step;
        }
        if (next is null) return;
        string? signalUnit = next.When.Signal == TutorialSignal.None ? null
            : TutorialSignals.Find(observation.Battle, next.When.Signal, observation.Phase).UnitId;
        Current = new TutorialLine(next, observation.NowMs, signalUnit);
        _shownPhase = observation.Phase;
        _shownTurn = observation.Turn;
        _speechMoment = false;
        _collapseFromMs = observation.NowMs;
        if (!IsOutcome(next)) _phaseLines++;
        _heard.Add(next.Id);
        Record(next.Id, TutorialStepState.Shown);
    }

    /// <summary>Outcome debrief lines (outcome signals) are exempt from the per-phase cap.</summary>
    private static bool IsOutcome(TutorialStep step) =>
        step.When.Signal is TutorialSignal.OutcomeVictory or TutorialSignal.OutcomeDefeat or TutorialSignal.OutcomeOther;

    /// <summary>"Got it": an acknowledged step completes; any other shown step is dismissed.</summary>
    internal void Acknowledge()
    {
        if (Current is not { } line) return;
        Close(line.Step.Complete == TutorialCompletion.Acknowledged ? TutorialStepState.Completed : TutorialStepState.Dismissed);
    }

    internal IReadOnlyList<TutorialEvent> DrainEvents()
    {
        var events = _events.ToArray();
        _events.Clear();
        return events;
    }

    private bool Eligible(TutorialStep step, TutorialObservation observation)
    {
        var when = step.When;
        if (when.Phase != observation.Phase) return false;
        if (when.Turn is { } turn && observation.Turn != turn) return false;
        if (when.MinTurn is { } minTurn && observation.Turn < minTurn) return false;
        if (when.After is { } after && StateOf(after) is TutorialStepState.Waiting or TutorialStepState.Shown) return false;
        return when.Signal == TutorialSignal.None ||
            TutorialSignals.Find(observation.Battle, when.Signal, observation.Phase).Signal != TutorialSignal.None;
    }

    private bool Done(TutorialCompletion completion, TutorialObservation observation, bool phaseLeft) => completion switch
    {
        TutorialCompletion.PilotSelected => observation.Phase == "Move" && observation.SelectedDroneId is not null,
        TutorialCompletion.PathEdited => observation.Movement?.Orders.Any(o => o.Touched) == true,
        TutorialCompletion.PhaseLeft => phaseLeft,
        TutorialCompletion.TargetChosen => observation.FirePlan?.Rows.Any(r => r.TargetId != r.Group.DefaultTargetId) == true,
        TutorialCompletion.PilotCycled => _pilotCycled,
        TutorialCompletion.EvadeOrDecoy => observation.FirePlan?.Rows.Any(r => r.Evade || r.Decoy) == true,
        _ => false,
    };

    private void Close(TutorialStepState state)
    {
        var line = Current!;
        Current = null;
        Collapsed = false;
        _speechMoment = true;
        _lastClosedAtMs = _last?.NowMs ?? 0;
        Record(line.Step.Id, state);
    }

    private void Record(string stepId, TutorialStepState state)
    {
        _states[stepId] = state;
        _events.Add(new TutorialEvent(stepId, state, _last?.Phase ?? "", _last?.Turn ?? 0, _last?.NowMs ?? 0));
    }
}
