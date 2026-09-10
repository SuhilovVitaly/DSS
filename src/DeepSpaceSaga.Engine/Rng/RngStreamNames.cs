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

    public static string StationCrewMemberName(string stationObjectId, string crewId) => $"StationCrewMemberName:{stationObjectId}:{crewId}";

    public static string StationCrewMemberPortrait(string stationObjectId, string crewId) => $"StationCrewMemberPortrait:{stationObjectId}:{crewId}";

    /// <summary>
    /// The player ship's captain — an independent named fact, not tied to a <c>Crew</c>
    /// element, hence no crewId component (unlike the station crew streams above).
    /// </summary>
    public static string ShipCaptainName(string shipObjectId) => $"ShipCaptainName:{shipObjectId}";

    /// <summary>See <see cref="ShipCaptainName"/>.</summary>
    public static string ShipCaptainPortrait(string shipObjectId) => $"ShipCaptainPortrait:{shipObjectId}";

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
