using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Rng;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private static DemoDebugSettings _processDemoDebugSettings = new();
    private static readonly object DemoDebugSettingsGate = new();
    private static DemoDebugSettings DemoDebugSettings => Volatile.Read(ref _processDemoDebugSettings);

    private DemoCampaignData PreparePirateWorldForLoad(DemoCampaignData campaign,
        IReadOnlyList<SpaceObjectRuntime> objects, GameStateData state, bool isSave, ulong seed)
    {
        if (campaign.PirateWorld is { } saved)
        {
            ValidatePirateWorld(saved, objects, state);
            ValidatePirateCompositions(campaign);
            return campaign;
        }
        if (!campaign.PirateCompositions.IsEmpty || !campaign.SensorReports.IsEmpty || campaign.PendingSensorReport is not null)
            throw new ScenarioException("Pirate compositions and reports require their saved world groups.");
        if (isSave || campaign.CatalogFingerprint is null || _demoDefinitions.Encounters is not { } definition)
            return campaign;
        var player = objects.Single(o => o.InitialMotion.ObjectId == state.PlayerShipObjectId);
        var engine = player.Modules.OrderBy(m => m.ModuleId, StringComparer.Ordinal)
            .Where(CanExecuteModuleCommand).Select(m => _registry.ModuleTypes.GetDefinition(m.ModuleTypeIndex))
            .FirstOrDefault(t => IsEngineCategory(t) && t.MaxSpeedMps is > 0 && t.LinearInertiaMps2 is > 0 && t.AngularInertiaDegPerSec is > 0)
            ?? throw new ScenarioException("Demo pirates require an operational player engine at New Game.");
        double maximum = engine.MaxSpeedMps!.Value / 1000.0;
        double acceleration = engine.LinearInertiaMps2!.Value / 1000.0;
        var start = objects.Single(o => o.InitialMotion.ObjectId == _demoDefinitions.Cluster!.StartStationId).InitialMotion;
        var stations = objects.Where(o => o.ObjectType == SpaceObjectType.Station).Select(o => o.InitialMotion).ToArray();
        var groups = ImmutableArray.CreateBuilder<PirateGroupData>();
        int squadChance = DemoDebugSettings.PirateSquadChancePercent;
        for (int index = 0; index < definition.GroupCount; index++)
        {
            string id = $"pirate:{index + 1:D2}";
            var types = PirateRandom(seed, "type:" + id);
            bool squad = types.Next(100) < squadChance;
            string type = squad || types.Next(2) == 0 ? PirateSignatureTypes.Lair : PirateSignatureTypes.Tancred;
            var profile = definition.Profiles.Single(p => p.SignatureTypeId == type);
            var module = _registry.ModuleTypes.GetDefinition(_registry.ModuleTypes.GetIndex(profile.SensorModuleTypeId));
            var members = Enumerable.Range(1, squad ? definition.SquadMembers.Length : 1).Select(i => new PirateCarrierData($"{id}:carrier:{i}", type,
                new($"{id}:sensor:{i}", module.TypeId, [new(0, 0)], module.StructurePointsMax, "On", "Ready", null, []),
                new($"{id}:engine:{i}", engine.TypeId, [new(0, 0)], engine.StructurePointsMax, "On", "Ready", null, []))).ToImmutableArray();
            var random = PirateRandom(seed, "placement:" + id);
            ImmutableArray<PiratePoint> route = [];
            double reserve = Math.Pow(maximum * 1.1, 2) / (2 * acceleration) * 10 + 10;
            for (int attempt = 0; attempt < 256; attempt++)
            {
                double angle = random.NextDouble() * 2 * Math.PI;
                double radius = Math.Sqrt(random.NextDouble()) * definition.Region.Radius;
                double x = definition.Region.X + Math.Cos(angle) * radius;
                double y = definition.Region.Y + Math.Sin(angle) * radius;
                if (stations.Any(s => Distance(x, y, s.X, s.Y) < definition.Pursuit.SpawnStationRadius)) continue;
                double patrol = definition.Pursuit.PatrolRadius;
                var points = ImmutableArray.Create(new PiratePoint(x, y), new(x + patrol, y), new(x + patrol, y + patrol), new(x, y + patrol));
                if (points.Where((p, i) => SegmentDistance(p, points[(i + 1) % points.Length], new(start.X, start.Y)) <= definition.Pursuit.StartSafeRadius + reserve).Any()) continue;
                route = points;
                break;
            }
            if (route.IsEmpty) throw new ScenarioException("No feasible pirate patrol placement after 256 attempts; world unchanged.");
            groups.Add(new(id, route[0].X, route[0].Y, 90, 0, members, route));
        }
        var world = new PirateWorldData(2, maximum, acceleration, engine.AngularInertiaDegPerSec!.Value, state.MotionTimeMs, groups.ToImmutable());
        ValidatePirateWorld(world, objects, state);
        return campaign with { PirateWorld = world };
    }

    private void ValidatePirateWorld(PirateWorldData world, IReadOnlyList<SpaceObjectRuntime> objects, GameStateData state)
    {
        if (_demoDefinitions.Encounters is not { } definition || world.Version != 2 || world.Groups.IsDefault || world.Groups.Length != 4 ||
            !double.IsFinite(world.CapturedPlayerMaxSpeedKmS) || world.CapturedPlayerMaxSpeedKmS <= 0 ||
            !double.IsFinite(world.AccelerationKmS2) || world.AccelerationKmS2 <= 0 ||
            !double.IsFinite(world.TurnRateDegreesPerSecond) || world.TurnRateDegreesPerSecond <= 0 ||
            world.LastMotionTimeMs < 0 || world.LastMotionTimeMs > state.MotionTimeMs ||
            world.LastPlayerPosition is { } oldPlayer && (!double.IsFinite(oldPlayer.X) || !double.IsFinite(oldPlayer.Y)))
            throw new ScenarioException("Invalid saved pirate world.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var start = objects.Single(o => o.InitialMotion.ObjectId == _demoDefinitions.Cluster!.StartStationId).InitialMotion;
        foreach (var group in world.Groups)
        {
            if (group is null || string.IsNullOrWhiteSpace(group.GroupId) || !ids.Add(group.GroupId) ||
                !double.IsFinite(group.X) || !double.IsFinite(group.Y) || !double.IsFinite(group.Direction) ||
                !double.IsFinite(group.SpeedKmS) || group.SpeedKmS < 0 || group.SpeedKmS > world.CapturedPlayerMaxSpeedKmS * 1.1 ||
                group.Members.IsDefault || group.Members.Length is < 1 or > 2 || group.PatrolPoints.IsDefault || group.PatrolPoints.Length != 4 ||
                group.NextPatrolPoint is < 0 or > 3 || group.Phase is not ("patrol" or "pursuit" or "last_known" or "return" or "captured") ||
                group.ApproximateDroneCount is < 0 || group.ContactEpoch < 0 ||
                group.LastPursuitEndReason is not (null or "start_safe" or "contact_lost") ||
                group.CapturedMotionTimeMs is { } captured && (!double.IsFinite(captured) || captured < 0 || captured > state.MotionTimeMs) ||
                (group.Phase == "captured") != (group.CapturedMotionTimeMs is not null) ||
                group.LastObservedPlayer is { } seen && (!double.IsFinite(seen.X) || !double.IsFinite(seen.Y)))
                throw new ScenarioException("Invalid saved pirate group.");
            if (group.LastObservedPlayerTimeMs is { } seenTime &&
                (seenTime < 0 || seenTime > world.LastMotionTimeMs || group.LastObservedPlayer is null))
                throw new ScenarioException("Invalid pirate last contact time.");
            if (group.PlayerObservation is { } observed &&
                (!group.ReportConsumed || !double.IsFinite(observed.X) || !double.IsFinite(observed.Y) ||
                 !double.IsFinite(observed.VelocityX) || !double.IsFinite(observed.VelocityY) ||
                 observed.MotionTimeMs < 0 || observed.MotionTimeMs > world.LastMotionTimeMs ||
                 observed.Pursuit is not { } pursuit || pursuit.GroupId != group.GroupId ||
                 string.IsNullOrWhiteSpace(pursuit.EventId) ||
                 pursuit.ObservedStatus is not (PursuitStatuses.Approaching or PursuitStatuses.Receding or PursuitStatuses.Captured or PursuitStatuses.Escaped) ||
                 pursuit.EndReason is not (null or "start_safe" or "contact_lost") ||
                 pursuit.ObservedMotionTimeMs != observed.MotionTimeMs || !double.IsFinite(pursuit.RangeWorldUnits) ||
                 pursuit.RangeWorldUnits < 0 || !double.IsFinite(pursuit.ClosingSpeedWorldUnitsPerSecond)))
                throw new ScenarioException("Invalid saved player observation.");
            if (!group.Retired && Distance(group.X, group.Y, start.X, start.Y) <= definition.Pursuit.StartSafeRadius)
                throw new ScenarioException("Pirate group is inside START safe zone.");
            if (!group.Retired && !SafePirateManeuver(group,
                new(group.GroupId, group.X, group.Y, group.SpeedKmS, group.Direction), new(start.X, start.Y), world.AccelerationKmS2, 0, 0))
                throw new ScenarioException("Saved pirate momentum cannot brake outside START.");
            if (group.MotionPlan is { } plan)
            {
                var origin = plan.Origin;
                if (origin is null || plan.StartMs < 0 || plan.StartMs > world.LastMotionTimeMs || plan.EndMs - plan.StartMs != PirateControlStepMs ||
                    plan.EndMs < world.LastMotionTimeMs || origin.ObjectId != group.GroupId ||
                    !double.IsFinite(origin.X) || !double.IsFinite(origin.Y) || !double.IsFinite(origin.Direction) ||
                    !double.IsFinite(origin.SpeedKmS) || origin.SpeedKmS < 0 || origin.SpeedKmS > world.CapturedPlayerMaxSpeedKmS * 1.1 ||
                    origin.MaxSpeedKmS is not { } maximum || !double.IsFinite(maximum) || maximum < 0 || maximum > world.CapturedPlayerMaxSpeedKmS * 1.1 ||
                    origin.SpeedHoldAccelerationKmS2 is not { } acceleration || !double.IsFinite(acceleration) || Math.Abs(acceleration) > world.AccelerationKmS2 ||
                    origin.TurnHoldRateDegPerSec is not { } turn || !double.IsFinite(turn) || Math.Abs(turn) > world.TurnRateDegreesPerSecond ||
                    origin.Orbit is not null || origin.EngineManeuver is not null || origin.ApproachRoute is not null || origin.ActiveEngineCommandType is not null)
                    throw new ScenarioException("Invalid saved pirate maneuver.");
                var current = DeepSpaceSaga.Motion.HoldMotion.Sample(origin, world.LastMotionTimeMs - plan.StartMs);
                var end = DeepSpaceSaga.Motion.HoldMotion.Sample(origin, PirateControlStepMs);
                var initialGroup = group with { X = origin.X, Y = origin.Y, Direction = origin.Direction, SpeedKmS = origin.SpeedKmS };
                if (current.X != group.X || current.Y != group.Y || current.SpeedKmS != group.SpeedKmS || current.Direction != group.Direction ||
                    !SafePirateManeuver(initialGroup, end, new(start.X, start.Y), world.AccelerationKmS2, PirateControlStepMs / 1000.0, turn))
                    throw new ScenarioException("Saved pirate maneuver disagrees with position or START corridor.");
            }
            for (int i = 0; i < group.PatrolPoints.Length; i++)
            {
                var point = group.PatrolPoints[i];
                if (point is null || !double.IsFinite(point.X) || !double.IsFinite(point.Y) ||
                    group.PatrolPoints[(i + 1) % 4] is not { } next ||
                    SegmentDistance(point, next, new(start.X, start.Y)) <= definition.Pursuit.StartSafeRadius)
                    throw new ScenarioException("Invalid pirate patrol corridor.");
            }
            foreach (var member in group.Members)
            {
                if (member is null || string.IsNullOrWhiteSpace(member.CarrierId) || !ids.Add(member.CarrierId) ||
                    member.SignatureTypeId is not (PirateSignatureTypes.Lair or PirateSignatureTypes.Tancred) ||
                    group.Members.Length == 2 && member.SignatureTypeId != PirateSignatureTypes.Lair || member.Sensor is not { } sensor ||
                    string.IsNullOrWhiteSpace(sensor.ModuleId) || !ids.Add(sensor.ModuleId) ||
                    sensor.PowerState is not ("On" or "Off") || sensor.OperationalState is not ("Ready" or "Disabled") ||
                    sensor.OccupiedCells is not { Count: 1 } || sensor.OccupiedCells[0] is not { X: 0, Y: 0 } ||
                    sensor.ActiveCycle is not null || sensor.OperatorCrewId is not null || sensor.Cargo is not { Count: 0 } ||
                    sensor.FuelAmountKg is not null || sensor.FuelCostBasisCredits is not null || sensor.LastTurnGameTimeMs is not null ||
                    sensor.ModuleTypeId != definition.Profiles.Single(p => p.SignatureTypeId == member.SignatureTypeId).SensorModuleTypeId ||
                    sensor.StructurePoints < 0 || sensor.StructurePoints > _registry.ModuleTypes.GetDefinition(_registry.ModuleTypes.GetIndex(sensor.ModuleTypeId)).StructurePointsMax)
                    throw new ScenarioException("Invalid saved pirate carrier or sensor.");
                if (member.Engine is not { } drive || string.IsNullOrWhiteSpace(drive.ModuleId) || !ids.Add(drive.ModuleId) ||
                    !_registry.ModuleTypes.Contains(drive.ModuleTypeId) ||
                    !IsEngineCategory(_registry.ModuleTypes.GetDefinition(_registry.ModuleTypes.GetIndex(drive.ModuleTypeId))) ||
                    drive.StructurePoints < 0 || drive.StructurePoints > _registry.ModuleTypes.GetDefinition(_registry.ModuleTypes.GetIndex(drive.ModuleTypeId)).StructurePointsMax ||
                    drive.PowerState is not ("On" or "Off") || drive.OperationalState is not ("Ready" or "Disabled") ||
                    drive.OccupiedCells is not { Count: 1 } || drive.OccupiedCells[0] is not { X: 0, Y: 0 } ||
                    drive.ActiveCycle is not null || drive.OperatorCrewId is not null || drive.Cargo is not { Count: 0 })
                    throw new ScenarioException("Invalid saved pirate engine module.");
            }
        }
    }

    private DemoEncountersSnapshot? BuildPirateGroupsProjection()
    {
        if (_demoCampaign.PirateWorld is not { } world) return null;
        var player = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (player is null) return new();
        long time = _processedSimulationTimeMs;
        var pose = PredictMotion(player, Math.Max(0, time - player.StartGameTimeMs));
        double radius = EvaluateCarrierSensor(player)?.RadiusWorldUnits ?? 0;
        var result = ImmutableArray.CreateBuilder<PirateGroupSnapshot>();
        foreach (var group in world.Groups.Where(g => !g.Retired).OrderBy(g => g.GroupId, StringComparer.Ordinal))
        {
            bool detected = SensorCovers(radius, pose.X, pose.Y, group.X, group.Y);
            if (!detected && DemoDebugSettings.FogOfWar)
            {
                if (group.PlayerObservation is { } observed)
                    result.Add(new(group.GroupId, observed.X, observed.Y, observed.VelocityX, observed.VelocityY,
                        observed.MotionTimeMs, false, PirateGroupStatuses.LastKnown, group.ApproximateDroneCount,
                        group.Members.Select(m => new PirateSignatureObservation(group.GroupId, m.SignatureTypeId,
                            m.SignatureTypeId == "lair" ? "Demo.Pirate.Lair" : "Demo.Pirate.Tancred", observed.MotionTimeMs)).ToImmutableArray(),
                        group.Members.Select(m => new PirateMemberSnapshot(m.CarrierId, m.SignatureTypeId)).ToImmutableArray(),
                        observed.Pursuit, Math.Max(0, time - observed.MotionTimeMs)));
                continue;
            }
            double angle = group.Direction * Math.PI / 180;
            result.Add(new(group.GroupId, group.X, group.Y, Math.Sin(angle) * group.SpeedKmS * 10,
                -Math.Cos(angle) * group.SpeedKmS * 10, time, detected,
                group.Phase == "captured" ? PirateGroupStatuses.Captured : detected ? PirateGroupStatuses.Observed : PirateGroupStatuses.DebugVisible,
                detected ? group.ApproximateDroneCount : null,
                detected ? group.Members.Select(m => new PirateSignatureObservation(group.GroupId, m.SignatureTypeId,
                    m.SignatureTypeId == "lair" ? "Demo.Pirate.Lair" : "Demo.Pirate.Tancred", time)).ToImmutableArray() : [],
                detected ? group.Members.Select(m => new PirateMemberSnapshot(m.CarrierId, m.SignatureTypeId)).ToImmutableArray() : [],
                detected ? ObservePursuit(group, pose, time) : null));
        }
        return new() { Groups = result.ToImmutable() };
    }

    private static Random PirateRandom(ulong seed, string fact) =>
        RngStreamNames.CreateDeterministicRandom(RngStreamSeedDerivation.DeriveStreamSeed(seed, "pirate/v1/" + fact));

    internal (double MaxSpeedKmS, double SensorRadius) PirateGroupCapabilities(PirateGroupData group, PirateWorldData world)
    {
        double speed = double.PositiveInfinity, radius = 0;
        foreach (var member in group.Members.Where(m => !m.IsDestroyed))
        {
            var profile = _demoDefinitions.Encounters!.Profiles.Single(p => p.SignatureTypeId == member.SignatureTypeId);
            speed = Math.Min(speed, PirateModuleOperational(member.Engine) ? world.CapturedPlayerMaxSpeedKmS * profile.SpeedFactor : 0);
            if (PirateModuleOperational(member.Sensor))
                radius = Math.Max(radius, _registry.ModuleTypes.GetDefinition(_registry.ModuleTypes.GetIndex(member.Sensor.ModuleTypeId)).SensorRadiusWorldUnits ?? 0);
        }
        return (double.IsPositiveInfinity(speed) ? 0 : speed, radius);
    }

    private static bool PirateModuleOperational(ShipModuleData module) =>
        !module.Destroyed && module.StructurePoints > 0 && module.PowerState == "On" && module.OperationalState == "Ready";
    private static double Distance(double x, double y, double x2, double y2) => Math.Sqrt((x - x2) * (x - x2) + (y - y2) * (y - y2));
    internal static double SegmentDistance(PiratePoint a, PiratePoint b, PiratePoint point)
    {
        double scale = new[] { a.X, a.Y, b.X, b.Y, point.X, point.Y }.Max(Math.Abs);
        if (!double.IsFinite(scale)) throw new ScenarioException("Non-finite pirate geometry.");
        if (scale == 0) return 0;
        double ax = a.X / scale, ay = a.Y / scale, px = point.X / scale, py = point.Y / scale;
        double dx = b.X / scale - ax, dy = b.Y / scale - ay, squared = dx * dx + dy * dy;
        double t = squared == 0 ? 0 : Math.Clamp(((px - ax) * dx + (py - ay) * dy) / squared, 0, 1);
        return Distance(ax + dx * t, ay + dy * t, px, py) * scale;
    }
}
