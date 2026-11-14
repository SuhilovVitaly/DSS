using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.Tutorial;

/// <summary>
/// Read-only facts of one rendered battle frame. Phase uses the PhasePanel vocabulary (Move, Fire, Salvo, Results, Outcome);
/// Battle is the projection the player sees, never the future engine state.
/// </summary>
internal sealed record TutorialObservation(string Phase, int Turn, TacticalBattleSnapshot Battle, string? SelectedDroneId,
    TacticalMovementFrame? Movement, TacticalFirePlan? FirePlan, long NowMs, bool Interacting = false, bool Busy = false)
{
    internal static TutorialObservation From(TacticalBattleFrame frame, string phase, long nowMs, bool interacting, bool busy) =>
        new(phase, frame.Battle.Turn, frame.Battle, frame.SelectedDroneId, frame.Movement, frame.FirePlan, nowMs, interacting, busy);
}
