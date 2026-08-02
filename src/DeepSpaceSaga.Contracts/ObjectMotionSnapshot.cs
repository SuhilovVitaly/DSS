namespace DeepSpaceSaga.Contracts;

/// <summary>
/// Minimal motion DTO for the render/prediction pipeline.
/// Speed in km/s. Direction in degrees: 0° = up, 90° = right, clockwise.
/// World coordinates: 1 unit = 100 m, Sun at (0, 0).
/// </summary>
public sealed record ObjectMotionSnapshot(
    string ObjectId,
    double X,          // world units
    double Y,          // world units
    double SpeedKmS,   // km/s
    double Direction); // degrees, 0° = up, clockwise
