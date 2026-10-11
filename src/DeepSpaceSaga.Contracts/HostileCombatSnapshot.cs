namespace DeepSpaceSaga.Contracts;

/// <summary>Authoritative pursuit state; presentation must not infer or change these phases.</summary>
public enum HostileCombatPhase
{
    Pursuit,
    OrbitEntry,
    OrbitHold
}

/// <summary>
/// Immutable AI state captured by Engine. Clockwise is retained throughout the encounter,
/// including pursuit fallback and Save/Load. Radius is in km, coordinates in world units.
/// </summary>
public sealed record HostileCombatSnapshot(
    string TargetObjectId,
    string EngineModuleId,
    string WeaponModuleId,
    double DesiredRadiusKm,
    bool Clockwise,
    HostileCombatPhase Phase,
    HostileMotionSegment Segment,
    int Version = 1);

/// <summary>
/// One confirmed physical-time segment with constant tangential acceleration and turn rate.
/// Prediction samples the immutable start, so splitting elapsed time cannot replan the path.
/// ElapsedMs is relative to EpochMs; prediction stops at DurationMs until Engine confirms
/// another segment. Direction uses 0 degrees up and clockwise positive.
/// </summary>
public sealed record HostileMotionSegment(
    long EpochMs,
    long DurationMs,
    long ElapsedMs,
    double StartX,
    double StartY,
    double StartSpeedKmS,
    double StartDirectionDegrees,
    double AccelerationKmS2,
    double TurnRateDegPerSec,
    int Version = 1);
