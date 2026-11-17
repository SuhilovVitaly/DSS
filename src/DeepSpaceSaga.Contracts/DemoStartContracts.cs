using System.Collections.Immutable;

namespace DeepSpaceSaga.Contracts;

public sealed record DemoStationSnapshot(string StationId, string Role);

/// <summary>Frozen public campaign scope; resources still come from the ordinary world snapshot.</summary>
public sealed record DemoStartSnapshot(int SchemaVersion, string ProfileId, ulong Seed, string StartStationId,
    ImmutableArray<DemoStationSnapshot> Stations, string StarterManifest);
