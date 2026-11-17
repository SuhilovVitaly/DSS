using System.Collections.Immutable;

namespace DeepSpaceSaga.Contracts;

public sealed record HirePilotCommand(string CommandId, long ExpectedRevision, string OfferId)
    : DemoCampaignCommand(CommandId, ExpectedRevision);
public sealed record AssignPilotCommand(string CommandId, long ExpectedRevision, string CrewId, string DroneId)
    : DemoCampaignCommand(CommandId, ExpectedRevision);
public sealed record RecruitmentOffer(string Id, string CrewId, string Name, string Portrait, int Accuracy, int Maneuver,
    int Initiative, long Fee, long DailyWage, long ExpiresAt);
public sealed record PilotAssignment(string CrewId, string Name, string? DroneId);
public sealed record RecruitmentSnapshot(ImmutableArray<RecruitmentOffer> Offers, ImmutableArray<PilotAssignment> Pilots,
    ImmutableArray<string> AvailableDroneIds, int FreeCabins, string? Reason = null);
