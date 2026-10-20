namespace DeepSpaceSaga.Contracts;

/// <summary>
/// Owner-known continuation of an accepted evasion. Times are absolute physical ms.
/// BaseMotion captures the actor's own course/orbit at entry and has no nested evasion.
/// Foreign ships publish observed movement rather than this control schedule.
/// </summary>
public sealed record EvasionSnapshot(double SampleMotionTimeMs, double EndsAtMotionTimeMs,
    string ThreatProjectileId, EvadeSide Side, ObjectMotionSnapshot BaseMotion);
