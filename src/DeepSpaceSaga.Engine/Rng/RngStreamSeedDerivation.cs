namespace DeepSpaceSaga.Engine.Rng;

/// <summary>
/// Deterministic derivation of a per-stream RNG seed from a session's masterSeed and a
/// stream name (requirements §15 "Генераторы случайных чисел"). Infrastructure laid down
/// ahead of any actual RNG stream consumer — nothing in Engine currently draws randomness
/// through this (motion is fully deterministic via LinearMotionPredictor); the point of
/// this iteration is only that (masterSeed, streamName) → seed is stable and wired into
/// save/load, exactly as IsKnown was infrastructure-only in the previous iteration.
///
/// Deliberately NOT System.HashCode.Combine or string.GetHashCode(): both use a randomized
/// per-process seed in .NET (by design, for hash-flooding resistance), so the same inputs
/// produce different results across runs/processes — the opposite of what "stable after
/// save/load" requires. This uses a fixed, portable FNV-1a 64-bit hash instead, which has
/// no such randomization and is stable across processes, machines, and .NET versions.
/// </summary>
public static class RngStreamSeedDerivation
{
    private const ulong FnvOffsetBasis = 14695981039346656037UL;
    private const ulong FnvPrime = 1099511628211UL;

    /// <summary>
    /// Deterministically derive a stream's seed from the session's masterSeed and the
    /// stream's name. Pure function: same inputs always produce the same output, in any
    /// process, on any machine.
    /// </summary>
    public static ulong DeriveStreamSeed(ulong masterSeed, string streamName)
    {
        ArgumentNullException.ThrowIfNull(streamName);

        ulong hash = FnvOffsetBasis;
        hash = MixBytes(hash, masterSeed);

        foreach (char c in streamName)
        {
            // Fold each UTF-16 code unit in as two bytes — avoids any dependency on a
            // particular text encoding's runtime behavior while still covering the full
            // character.
            hash = MixByte(hash, (byte)(c & 0xFF));
            hash = MixByte(hash, (byte)(c >> 8));
        }

        return hash;
    }

    private static ulong MixBytes(ulong hash, ulong value)
    {
        for (int i = 0; i < 8; i++)
        {
            hash = MixByte(hash, (byte)(value & 0xFF));
            value >>= 8;
        }

        return hash;
    }

    private static ulong MixByte(ulong hash, byte b)
    {
        hash ^= b;
        hash *= FnvPrime;
        return hash;
    }
}
