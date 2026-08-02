using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion;

/// <summary>
/// Simple linear motion: position += speed * direction * elapsedTime.
/// Speed is in world units per second (1 unit = 100 m).
/// Direction is in radians (0 = right, π/2 = up).
/// </summary>
public sealed class LinearMotionPredictor : IMotionPredictor
{
    public ObjectMotionSnapshot Predict(ObjectMotionSnapshot state, long elapsedMs)
    {
        double elapsedSeconds = elapsedMs / 1000.0;
        double dx = state.Speed * Math.Cos(state.Direction) * elapsedSeconds;
        double dy = state.Speed * Math.Sin(state.Direction) * elapsedSeconds;

        return state with
        {
            X = state.X + dx,
            Y = state.Y + dy
        };
    }
}
