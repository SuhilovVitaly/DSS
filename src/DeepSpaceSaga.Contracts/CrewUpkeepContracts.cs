namespace DeepSpaceSaga.Contracts;

public sealed record PayCrewDebtCommand(string CommandId, long ExpectedRevision, long Amount)
    : DemoCampaignCommand(CommandId, ExpectedRevision);
public sealed record CrewUpkeepSnapshot(long Accrued, long Paid, long Outstanding, long NextDueAt,
    int PeopleFed, long FoodRemaining, long MissingRations, long LastProcessedDay);
