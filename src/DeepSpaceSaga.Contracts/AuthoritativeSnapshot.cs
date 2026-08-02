namespace DeepSpaceSaga.Contracts;

/// <summary>
/// Authoritative world snapshot produced by the engine.
/// SnapshotSequence is monotonically increasing, allowing detection of
/// duplicates, gaps, and out-of-order messages in a future network transport.
/// </summary>
public sealed record AuthoritativeSnapshot(
    ulong SnapshotSequence,
    long GameTimeMs,
    IReadOnlyList<ObjectMotionSnapshot> Objects);
