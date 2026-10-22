using DeepSpaceSaga.Engine.Rng;

namespace DeepSpaceSaga.Engine.Combat;

/// <summary>SplitMix64 / Box-Muller v1. Two uniforms per independent normal pair, no cached sample.</summary>
internal sealed class LaunchScatterRng
{
    internal const int AlgorithmVersion = 1;
    internal ulong State { get; private set; }
    internal ulong Counter { get; private set; }

    internal LaunchScatterRng(ulong masterSeed)
        : this(RngStreamSeedDerivation.DeriveStreamSeed(masterSeed, RngStreamNames.LaunchScatter), 0) { }

    internal LaunchScatterRng(ulong state, ulong counter) { State = state; Counter = counter; }

    internal (double X, double Y) NextPair()
    {
        if (Counter > ulong.MaxValue - 2) throw new InvalidOperationException("Launch scatter counter exhausted.");
        double u = NextOpenUniform();
        double angle = Math.Tau * NextOpenUniform();
        double radius = Math.Sqrt(-2 * Math.Log(u));
        return (radius * Math.Cos(angle), radius * Math.Sin(angle));
    }

    private double NextOpenUniform()
    {
        State = unchecked(State + 0x9E3779B97F4A7C15UL);
        ulong value = State;
        value = unchecked((value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL);
        value = unchecked((value ^ (value >> 27)) * 0x94D049BB133111EBUL);
        value ^= value >> 31;
        Counter++;
        // 52 random bits with a half-step offset give a strictly open (0,1) double interval.
        return ((value >> 12) + 0.5) / 4503599627370496d;
    }
}
