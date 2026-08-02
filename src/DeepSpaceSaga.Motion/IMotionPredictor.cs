using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion;

/// <summary>
/// Predicts object position between authoritative snapshots.
/// Client-side only — does not modify authoritative state.
/// </summary>
public interface IMotionPredictor
{
    ObjectMotionSnapshot Predict(ObjectMotionSnapshot state, long elapsedMs);
}
