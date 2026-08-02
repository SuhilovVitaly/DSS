namespace DeepSpaceSaga.Contracts;

/// <summary>
/// Minimal motion DTO for the render/prediction pipeline.
/// Position in world units: 1 unit = 100 m, Sun at (0, 0).
/// </summary>
public sealed record ObjectMotionSnapshot(
    string ObjectId,
    double X,
    double Y,
    double Speed,
    double Direction);
