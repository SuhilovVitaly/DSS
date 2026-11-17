using System.Collections.Immutable;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private void ObservePirateGroups(long gameTimeMs)
    {
        if (_demoCampaign.PirateWorld is not { } world) return;
        var player = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (player is null) return;
        var pose = PredictMotion(player, Math.Max(0, _processedSimulationTimeMs - player.StartGameTimeMs));
        double radius = EvaluateCarrierSensor(player)?.RadiusWorldUnits ?? 0;
        var candidate = _demoCampaign;
        foreach (var group in world.Groups.Where(g => !g.Retired && !g.ReportConsumed).OrderBy(g => g.GroupId, StringComparer.Ordinal))
            if (SensorCovers(radius, pose.X, pose.Y, group.X, group.Y))
            {
                candidate = EnsurePirateComposition(candidate, group.GroupId);
                candidate = AddSensorReport(candidate, group.GroupId, pose, _processedSimulationTimeMs);
            }
        if (ReferenceEquals(candidate, _demoCampaign)) return;
        candidate = candidate with { Revision = checked(candidate.Revision + 1), CommitSequence = checked(candidate.CommitSequence + 1) };
        DemoCampaignData.Validate(candidate, Math.Max(gameTimeMs, candidate.LastTickGameTimeMs));
        ValidatePirateCompositions(candidate);
        _demoCampaign = candidate;
    }

    // Capture calls this same path before its durable prebattle checkpoint, even without player detection.
    private DemoCampaignData EnsurePirateComposition(DemoCampaignData campaign, string groupId)
    {
        var world = campaign.PirateWorld ?? throw new ScenarioException("Missing pirate world.");
        int index = -1;
        for (int i = 0; i < world.Groups.Length; i++) if (world.Groups[i].GroupId == groupId) index = i;
        if (index < 0) throw new ScenarioException("Unknown pirate group.");
        var group = world.Groups[index];
        if (group.CompositionMaterialized) return campaign;
        var compositions = GeneratePirateComposition(MasterSeed, group);
        return campaign with
        {
            PirateCompositions = campaign.PirateCompositions.AddRange(compositions),
            PirateWorld = world with { Groups = world.Groups.SetItem(index, group with { CompositionMaterialized = true }) }
        };
    }

    internal static ImmutableArray<PirateCompositionData> GeneratePirateComposition(ulong seed, PirateGroupData group)
    {
        var result = ImmutableArray.CreateBuilder<PirateCompositionData>();
        foreach (var carrier in group.Members.OrderBy(m => m.CarrierId, StringComparer.Ordinal))
        {
            int count = carrier.SignatureTypeId == "lair" ? 6 : 10;
            var drones = ImmutableArray.CreateBuilder<PirateDroneData>(count);
            for (int slot = 1; slot <= count; slot++)
            {
                string id = $"{carrier.CarrierId}:drone:{slot:D2}";
                bool heavy = PirateRandom(seed, "class:" + id).Next(2) == 1;
                bool spear = PirateRandom(seed, "weapon:" + id).Next(2) == 1;
                string layout = (heavy, spear) switch
                {
                    (false, false) => "light.sword.shield",
                    (false, true) => "light.spear",
                    (true, false) => "heavy.sword.shield",
                    _ => "heavy.spear.shield"
                };
                drones.Add(new(id, layout));
            }
            result.Add(new(group.GroupId, carrier.CarrierId, carrier.SignatureTypeId, 1, drones.ToImmutable()));
        }
        return result.ToImmutable();
    }

    private static void ValidatePirateCompositions(DemoCampaignData campaign)
    {
        if (campaign.PirateWorld is not { } world) return;
        var expected = world.Groups.Where(g => g.CompositionMaterialized)
            .SelectMany(g => g.Members.Select(m => (Group: g.GroupId, Member: m))).ToArray();
        if (expected.Length != campaign.PirateCompositions.Length || campaign.PirateCompositions.Any(c =>
            !expected.Any(e => e.Group == c.GroupId && e.Member.CarrierId == c.CarrierId && e.Member.SignatureTypeId == c.SignatureTypeId)))
            throw new ScenarioException("Saved pirate composition does not match its world group.");
        foreach (var composition in campaign.PirateCompositions)
            for (int i = 0; i < composition.Drones.Length; i++)
                if (composition.Drones[i].DroneId != $"{composition.CarrierId}:drone:{i + 1:D2}")
                    throw new ScenarioException("Invalid stable pirate drone identity.");
        foreach (var group in world.Groups)
        {
            var report = campaign.SensorReports.SingleOrDefault(r => r.GroupId == group.GroupId);
            if (group.ReportConsumed != (report is not null) || report is not null &&
                (!group.CompositionMaterialized || report.ApproximateDroneCount != group.ApproximateDroneCount ||
                 !report.SignatureTypeIds.SequenceEqual(group.Members.Select(m => m.SignatureTypeId)) || report.ObservedMotionTimeMs > world.LastMotionTimeMs))
                throw new ScenarioException("Sensor report does not match its saved group.");
        }
        if (campaign.SensorReports.Any(r => !world.Groups.Any(g => g.GroupId == r.GroupId)))
            throw new ScenarioException("Sensor report refers to an unknown group.");
    }
}
