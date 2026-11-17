using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private DemoStartSnapshot? BuildDemoStartProjection()
    {
        if (_demoCampaign.CatalogFingerprint is null || _demoDefinitions.Cluster is not { } cluster) return null;
        return new(1, "demo", cluster.Seed, cluster.StartStationId,
            cluster.StationIds.Select((id, i) => new DemoStationSnapshot(id, cluster.Roles[i])).ToImmutableArray(),
            "Data/Ships/demo-starter-carrier.json");
    }
}
