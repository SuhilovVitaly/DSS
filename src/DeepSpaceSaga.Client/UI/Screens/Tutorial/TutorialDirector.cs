namespace DeepSpaceSaga.Client.UI.Screens.Tutorial;

internal enum TutorialStepState { Waiting, Shown, Completed, Dismissed, Expired }

internal sealed record TutorialEvent(string StepId, TutorialStepState State, string Phase, int Turn, long AtMs);

internal sealed record TutorialLine(TutorialStep Step, long ShownAtMs);

/// <summary>
/// Deterministic, advisory step logic: decides which single line is shown from a sequence of frame observations and closes
/// a step once the player performed the action. It never blocks input and never talks to the Engine.
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
        if (newPhase) { _lastPilot = null; _pilotCycled = false; }
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
                Record(step.Id, TutorialStepState.Completed);
        }

        if (!_enabled || Current is not null || observation.Busy) return;
        TutorialStep? next = null;
        foreach (var step in script.Steps)
        {
            if (_states[step.Id] == TutorialStepState.Waiting && Eligible(step, observation) && (next is null || step.Priority > next.Priority))
                next = step;
        }
        if (next is null) return;
        Current = new TutorialLine(next, observation.NowMs);
        _shownPhase = observation.Phase;
        _shownTurn = observation.Turn;
        _heard.Add(next.Id);
        Record(next.Id, TutorialStepState.Shown);
    }

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
        return when.Signal == TutorialSignal.None;
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
        Record(line.Step.Id, state);
    }

    private void Record(string stepId, TutorialStepState state)
    {
        _states[stepId] = state;
        _events.Add(new TutorialEvent(stepId, state, _last?.Phase ?? "", _last?.Turn ?? 0, _last?.NowMs ?? 0));
    }
}
