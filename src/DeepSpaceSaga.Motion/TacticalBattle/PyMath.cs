namespace DeepSpaceSaga.Motion.TacticalBattle;

/// <summary>Selection and arithmetic semantics required by V4 Spec/Combat §§1–1.1.</summary>
public static class PyMath
{
    public static double Clamp(double value, double lo, double hi) => value < lo ? lo : value > hi ? hi : value;
    public static double Min(double first, double second) => second < first ? second : first;
    public static double Max(double first, double second) => second > first ? second : first;

    public static double FloorNonNeg(double value)
    {
        if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        return Math.Floor(value);
    }

    public static double Fmod(double value, double divisor) => value % divisor;

    public static T MinByFirst<T>(IEnumerable<T> source, Func<T, double> key)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(key);
        using var iterator = source.GetEnumerator();
        if (!iterator.MoveNext()) throw new InvalidOperationException("Minimum requires a nonempty sequence.");
        T best = iterator.Current;
        double bestKey = key(best);
        while (iterator.MoveNext())
        {
            T candidate = iterator.Current;
            double candidateKey = key(candidate);
            if (candidateKey < bestKey) { best = candidate; bestKey = candidateKey; }
        }
        return best;
    }

    public static IReadOnlyList<T> OrderedUnique<T>(IEnumerable<T> source, IEqualityComparer<T>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        comparer ??= EqualityComparer<T>.Default;
        var result = new List<T>();
        foreach (T item in source)
            if (!result.Any(existing => comparer.Equals(existing, item))) result.Add(item);
        return result.AsReadOnly();
    }
}
