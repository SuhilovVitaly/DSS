using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion;

/// <summary>
/// DSS-correct linear motion prediction.
/// Speed: km/s. Direction: degrees, 0° = up, clockwise.
/// 1 km/s = 10 world units/s (since 1 unit = 100 m).
/// </summary>
public sealed class LinearMotionPredictor : IMotionPredictor
{
    private const double UnitsPerKmS = 10.0; // 1 km/s → 10 world units/s

    public ObjectMotionSnapshot Predict(ObjectMotionSnapshot state, long elapsedMs)
    {
        double elapsedSeconds = elapsedMs / 1000.0;
        double distance = state.SpeedKmS * elapsedSeconds * UnitsPerKmS;

        double angleRad = state.Direction * Math.PI / 180.0;

        // DSS convention: 0° = up (negative Y), 90° = right, clockwise
        double dx = distance * Math.Sin(angleRad);
        double dy = -distance * Math.Cos(angleRad);

        return state with
        {
            X = state.X + dx,
            Y = state.Y + dy
        };
    }
}
