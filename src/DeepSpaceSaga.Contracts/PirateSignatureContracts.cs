namespace DeepSpaceSaga.Contracts;

public static class PirateSignatureTypes
{
    public const string Lair = "lair";
    public const string Tancred = "tancred";
}

/// <summary>Identified carrier type, without exact remaining drones or equipment.</summary>
public sealed record PirateSignatureObservation(string GroupId, string SignatureTypeId, string DisplayNameKey,
    long ObservedMotionTimeMs);
