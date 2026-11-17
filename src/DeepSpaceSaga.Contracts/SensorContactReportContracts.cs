using System.Collections.Immutable;

namespace DeepSpaceSaga.Contracts;

/// <summary>Frozen observed telemetry. ETA is real seconds at 1x; distance is 100 m world units.</summary>
public sealed record SensorContactReport(string ReportId, string GroupId, string? OperatorCrewId,
    long ObservedMotionTimeMs, double BearingDegrees, double RangeWorldUnits,
    double? EtaRealSecondsAt1X, string? EtaUnavailableReason, int ApproximateDroneCount,
    ImmutableArray<string> SignatureTypeIds);

/// <summary>All first acquisitions in one authoritative world step; persisted until acknowledged.</summary>
public sealed record SensorContactReportBatch(string BatchId, ImmutableArray<SensorContactReport> Reports);

public sealed record DismissSensorContactReportRequest(string CommandId, long ExpectedRevision, string BatchId)
    : DemoCampaignCommand(CommandId, ExpectedRevision);
