using System.Collections.Immutable;

namespace DeepSpaceSaga.Contracts;

public sealed record ThreatRouteWaypoint(double X, double Y);
public sealed record ThreatRouteOption(string RouteId, string DestinationStationId, bool IsDetour,
    ImmutableArray<ThreatRouteWaypoint> Waypoints, long EtaGameTimeMs, long? FuelRequiredKg,
    string KnownRiskBand, double KnownExposureWorldUnits, long? DeadlineDeltaGameTimeMs, string? UnavailableReason = null);
public sealed record ThreatRoutesSnapshot(long Revision, ImmutableArray<ThreatRouteOption> Options,
    string? SelectedRouteId = null);
public sealed record SelectDemoRouteCommand(string CommandId, long ExpectedRevision, string RouteId, long RouteRevision)
    : DemoCampaignCommand(CommandId, ExpectedRevision);
