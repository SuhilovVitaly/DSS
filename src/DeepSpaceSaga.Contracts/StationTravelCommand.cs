namespace DeepSpaceSaga.Contracts;

public enum StationDistrict { Dock, Market, Habitation, Administration }

/// <summary>Session command; deliberately carries no ship-module address.</summary>
public sealed record StationTravelCommand(string CommandId, StationDistrict Destination);

public sealed record StationTravelResult(bool Accepted, string? Error, AuthoritativeSnapshot Snapshot);
