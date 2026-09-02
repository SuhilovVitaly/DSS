namespace DeepSpaceSaga.Engine.Rng;

/// <summary>
/// Named RNG stream conventions for <see cref="RngStreamSeedDerivation"/>. One named
/// stream per generated fact (not one shared stream per station) — so adding a future
/// tradeable good or a second generated station fact never shifts the sequence already
/// consumed by an existing fact for the same station.
/// </summary>
internal static class RngStreamNames
{
    public static string StationCredits(string stationObjectId) => $"StationCredits:{stationObjectId}";

    public static string StationPriceCoefficient(string stationObjectId) => $"StationPriceCoefficient:{stationObjectId}";

    public static string StationInventory(string stationObjectId, string itemTypeId) => $"StationInventory:{stationObjectId}:{itemTypeId}";

    public static string AsteroidImage(string asteroidObjectId) => $"AsteroidImage:{asteroidObjectId}";

    /// <summary>
    /// Folds a full 64-bit stream seed into the 32-bit seed System.Random accepts, XORing
    /// both halves together rather than truncating — so all 64 bits of entropy from
    /// RngStreamSeedDerivation contribute, not just the low 32 bits. System.Random(int)'s
    /// algorithm is documented stable across platforms/processes since .NET 6 (this project
    /// targets net8.0), which is what "deterministic after save/load" requires here.
    /// </summary>
    public static Random CreateDeterministicRandom(ulong streamSeed)
    {
        int seed32 = unchecked((int)(streamSeed ^ (streamSeed >> 32)));
        return new Random(seed32);
    }
}
