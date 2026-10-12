using DeepSpaceSaga.Engine.Rng;

namespace DeepSpaceSaga.Engine.Combat;

/// <summary>Independent SplitMix64 stream for mass-weighted hit location.</summary>
internal sealed class ModuleHitLocationRng
{
    internal ulong State { get; private set; }
    internal ulong Counter { get; private set; }

    internal ModuleHitLocationRng(ulong masterSeed)
        : this(RngStreamSeedDerivation.DeriveStreamSeed(masterSeed, RngStreamNames.CombatHitLocation), 0) { }

    internal ModuleHitLocationRng(ulong state, ulong counter)
    {
        State = state;
        Counter = counter;
    }

    internal ulong NextBelow(ulong exclusiveUpperBound)
    {
        if (exclusiveUpperBound == 0) throw new ArgumentOutOfRangeException(nameof(exclusiveUpperBound));
        ulong threshold = unchecked(0UL - exclusiveUpperBound) % exclusiveUpperBound;
        ulong value;
        do
        {
            State = unchecked(State + 0x9E3779B97F4A7C15UL);
            value = State;
            value = unchecked((value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL);
            value = unchecked((value ^ (value >> 27)) * 0x94D049BB133111EBUL);
            value ^= value >> 31;
        } while (value < threshold);
        Counter = checked(Counter + 1);
        return value % exclusiveUpperBound;
    }
}
