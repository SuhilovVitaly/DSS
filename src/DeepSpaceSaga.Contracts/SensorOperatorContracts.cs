namespace DeepSpaceSaga.Contracts;

/// <summary>Authoritative sensor assignment. Crew identity resolves against CarrierOperations.Crew.</summary>
public sealed record SensorOperatorSnapshot(
    string ModuleId,
    string? CrewId,
    string? AvailabilityReason,
    double BaseRadiusWorldUnits,
    double EffectiveRadiusWorldUnits,
    double OperatorContributionWorldUnits);

public static class SensorOperatorReasons
{
    public const string Unassigned = "unassigned";
    public const string CrewMissing = "crew_missing";
    public const string ModuleUnavailable = "module_unavailable";
}
