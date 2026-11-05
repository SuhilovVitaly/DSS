using DeepSpaceSaga.Motion.TacticalBattle;

namespace DeepSpaceSaga.Engine.TacticalBattle;

/// <summary>The sole battle-owned stream, V4 Spec/Generation §1.</summary>
internal sealed class Mulberry32(uint seed)
{
    private uint _state = seed;
    public long Calls { get; private set; }
    public uint State => _state;
    public Mulberry32(long seed) : this(unchecked((uint)seed)) { }

    public double Random()
    {
        unchecked
        {
            _state += TacticalBattleConstants.RandomIncrement;
            uint t = _state;
            t = Multiply(t ^ (t >> 15), t | 1);
            t ^= t + Multiply(t ^ (t >> 7), t | 61);
            uint bits = t ^ (t >> 14);
            Calls++;
            return (double)bits / TacticalBattleConstants.RandomDivisor;
        }
    }

    public double Uniform(double lo, double hi)
    {
        if (!double.IsFinite(lo) || !double.IsFinite(hi) || !double.IsFinite(hi - lo)) throw new ArgumentOutOfRangeException(nameof(hi));
        return lo + (hi - lo) * Random();
    }

    public int RandInt(int lo, int hi)
    {
        if (lo > hi) throw new ArgumentOutOfRangeException(nameof(hi));
        return checked((int)(lo + PyMath.FloorNonNeg(Random() * ((double)hi - lo + 1.0))));
    }

    public T Choice<T>(IReadOnlyList<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count == 0) throw new ArgumentException("Choice requires a nonempty list.", nameof(values));
        return values[checked((int)PyMath.FloorNonNeg(Random() * values.Count))];
    }

    public T Weighted<T>(IReadOnlyList<(T Value, double Weight)> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count == 0) throw new ArgumentException("Weighted choice requires a nonempty list.", nameof(values));
        double total = 0;
        foreach (var entry in values)
        {
            if (!double.IsFinite(entry.Weight) || entry.Weight < 0) throw new ArgumentOutOfRangeException(nameof(values));
            total += entry.Weight;
        }
        if (!double.IsFinite(total)) throw new ArgumentOutOfRangeException(nameof(values));
        double remainder = Random() * total;
        foreach (var entry in values)
        {
            remainder -= entry.Weight;
            if (remainder < 0) return entry.Value;
        }
        return values[^1].Value;
    }

    private static uint Multiply(uint a, uint b) => unchecked((uint)(((ulong)a * b) & uint.MaxValue));
}
