using System.Collections.Immutable;

namespace DeepSpaceSaga.Contracts;

/// <summary>
/// Authoritative world snapshot produced by the engine.
/// Immutable — safe to share between threads and over the network.
/// SnapshotSequence is monotonically increasing.
/// </summary>
public sealed record AuthoritativeSnapshot(
    ulong SnapshotSequence,
    long GameTimeMs,
    ImmutableArray<ObjectMotionSnapshot> Objects);
