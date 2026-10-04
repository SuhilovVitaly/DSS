using DeepSpaceSaga.Engine.Rng;
namespace DeepSpaceSaga.Engine.Combat;

/// <summary>SplitMix64 v1, unbiased rejection reduction to [1,1000]. Counter counts resolved encounters.</summary>
public sealed class CountermeasureRng
{
    public const int AlgorithmVersion = 1;
    public ulong State { get; private set; }
    public ulong Counter { get; private set; }
    public CountermeasureRng(ulong masterSeed) => State = RngStreamSeedDerivation.DeriveStreamSeed(masterSeed, RngStreamNames.CountermeasureIntercept);
    public CountermeasureRng(ulong state, ulong counter) { State = state; Counter = counter; }
    public int NextRoll()
    {
        ulong value;
        do
        {
            State = unchecked(State + 0x9E3779B97F4A7C15UL);
            value = State;
            value = unchecked((value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL);
            value = unchecked((value ^ (value >> 27)) * 0x94D049BB133111EBUL);
            value ^= value >> 31;
        } while (value < 616UL); // 2^64 mod 1000; accepted range is divisible by 1000.
        Counter = checked(Counter + 1);
        return (int)(value % 1000) + 1;
    }
}
