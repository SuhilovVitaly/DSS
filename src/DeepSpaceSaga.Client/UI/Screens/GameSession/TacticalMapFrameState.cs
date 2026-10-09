using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>Owned immutable publication; later updates cannot mutate its rendered poses.</summary>
internal sealed record TacticalMapFrameState(
    long FrameId, long Timestamp, SnapshotPrediction? Prediction,
    ImmutableArray<ObjectRenderState> Objects, bool AuthoritativeRebase,
    RenderMotion? PlayerRaw, RenderMotion? TargetRaw, ImmutableArray<string> Diagnostics)
{
    internal ulong? SnapshotRevision => Prediction?.BufferedSnapshot.Snapshot.SnapshotSequence;
    internal long MotionTimeMs => Prediction is { } p
        ? p.BufferedSnapshot.Snapshot.MotionTimeMs + p.EffectivePredictionDeltaMs : 0;
    internal RenderMotion? PlayerFocus => Objects.Where(state => state.IsPlayerShip)
        .Select(state => (RenderMotion?)state.Pose).FirstOrDefault();
}
