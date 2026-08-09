namespace DeepSpaceSaga.Contracts;

/// <summary>
/// Minimal motion DTO for the render/prediction pipeline.
/// Speed in km/s. Direction in degrees: 0° = up, 90° = right, clockwise.
/// Active engine cycle metadata allows the client to predict discrete turns
/// with the same step timing as the authoritative simulation.
/// World coordinates: 1 unit = 100 m, Sun at (0, 0).
/// </summary>
/// <param name="MaxSpeedKmS">km/s, null if the object has no active engine module</param>
public sealed record ObjectMotionSnapshot(
    string ObjectId,
    double X,          // world units
    double Y,          // world units
    double SpeedKmS,   // km/s
    double Direction,  // degrees, 0° = up, clockwise
    string? ActiveEngineCommandType = null,
    int TurnStepDegrees = 0,
    long TurnStepRemainingMs = 0,
    long TurnStepIntervalMs = 0,
    string? ObjectType = null,
    string? RelationToPlayer = null,
    string? DisplayName = null,
    string? RenderObjectType = null,
    double? MaxSpeedKmS = null);
