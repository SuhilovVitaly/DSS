using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Rng;

namespace DeepSpaceSaga.Engine.Scenario;

internal static class SolarSystemGenerator
{
    internal static ScenarioFile Generate(ScenarioFile source, SolarSystemGenerationConfig config,
        GameDataRegistry registry, ulong masterSeed)
    {
        SolarSystemGeneration.ValidateConfig(config);
        if (source.GameState.SolarSystem is not null)
            throw new ScenarioException("solar-system/v1: New Game already has a materialized system.");
        var objects = source.GameState.SpaceObjects.OrderBy(o => o.ObjectId, StringComparer.Ordinal).ToArray();
        if (objects.Any(o => o.ObjectType.Equals("Sun", StringComparison.OrdinalIgnoreCase) ||
            o.ObjectType.Equals("Planet", StringComparison.OrdinalIgnoreCase)))
            throw new ScenarioException($"solar-system/v1 seed={masterSeed} version=1 stage=input: source already contains celestial bodies.");
        var player = objects.Single(o => string.Equals(o.ObjectId, source.GameState.PlayerShipObjectId, StringComparison.OrdinalIgnoreCase));
        var engine = (player.Modules ?? []).OrderBy(m => m.ModuleId, StringComparer.Ordinal)
            .Where(m => m.PowerState == "On" && m.OperationalState == "Ready" && m.StructurePoints > 0)
            .Select(m => registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex(m.ModuleTypeId)))
            .FirstOrDefault(m => m.MaxSpeedMps is > 0);
        if (engine?.MaxSpeedMps is not > 0)
            throw new ScenarioException($"solar-system/v1 seed={masterSeed} stage=start: no operational engine Vmax.");
        double vmax = engine.MaxSpeedMps.Value / 1000.0;
        string reason = "placement exhausted";
        for (int attempt = 0; attempt < config.MaxPlacementAttempts; attempt++)
        {
            try { return Place(source, objects, player, config, masterSeed, vmax, attempt); }
            catch (PlacementException ex) { reason = ex.Message; }
            catch (OverflowException) { reason = "orbital period overflow"; }
        }
        throw new ScenarioException($"solar-system/v1 seed={masterSeed} version=1 stage=placement attempt={config.MaxPlacementAttempts}: {reason}.");
    }

    private static ScenarioFile Place(ScenarioFile source, SpaceObjectData[] original, SpaceObjectData player,
        SolarSystemGenerationConfig c, ulong seed, double vmax, int attempt)
    {
        var start = new GeneratorRng(seed, "start", attempt);
        double days = c.StartMinDays + (c.StartMaxDays - c.StartMinDays) * start.NextDouble();
        double radius = vmax * days * 86400 / SimulationSpeedExtensions.BaseGameSecondsPerRealSecond * 10;
        double angle = start.NextDouble() * Math.Tau;
        double x = radius * Math.Sin(angle), y = -radius * Math.Cos(angle);
        // Docked starts use the actual station plus the authoritative docking offset.
        double playerX = player.PositionX, playerY = player.PositionY;
        if (player.IsDocked)
        {
            var station = original.Single(o => string.Equals(o.ObjectId, player.DockedStationObjectId, StringComparison.OrdinalIgnoreCase));
            playerX = station.PositionX + 1;
            playerY = station.PositionY + 1;
        }
        var objects = original.Select(o => o with
        {
            PositionX = (o.ObjectId == player.ObjectId ? playerX : o.PositionX) + x - playerX,
            PositionY = (o.ObjectId == player.ObjectId ? playerY : o.PositionY) + y - playerY,
            IsKnown = true
        }).ToList();
        if (objects.Any(o => !double.IsFinite(o.PositionX) || !double.IsFinite(o.PositionY)))
            throw new PlacementException("non-finite translated position");
        double min = objects.Min(o => Math.Sqrt(o.PositionX * o.PositionX + o.PositionY * o.PositionY));
        double max = objects.Max(o => Math.Sqrt(o.PositionX * o.PositionX + o.PositionY * o.PositionY));
        double halfWidth = radius * c.BeltWidthFraction / 2;
        var corridors = new List<(double Inner, double Outer)> { (Math.Min(radius - halfWidth, min - c.OrbitClearanceWorld), Math.Max(radius + halfWidth, max + c.OrbitClearanceWorld)) };
        if (corridors[0].Inner <= 0 || corridors[0].Outer >= radius * 2.5)
            throw new PlacementException("start corridor does not fit");
        var counts = new GeneratorRng(seed, "counts", attempt);
        int planetCount = counts.NextInt(c.MinPlanets, c.MaxPlanets + 1), beltCount = counts.NextInt(c.MinBelts, c.MaxBelts + 1);
        var belts = new List<BeltMapData> { new("SYS-BELT-1", corridors[0].Inner, corridors[0].Outer, counts.Next()) };
        var planets = new List<PlanetMapData>();
        var orbits = new List<OrbitMapData>();
        var placement = new GeneratorRng(seed, "geometry", attempt);
        for (int i = 1; i < beltCount + planetCount; i++)
        {
            bool belt = i < beltCount;
            double r = radius * (0.12 + placement.NextDouble() * 2.1);
            double width = belt ? r * c.BeltWidthFraction / 2 : r * 0.005;
            var corridor = (Inner: r - width, Outer: r + width);
            if (corridors.Any(p => corridor.Inner < p.Outer + c.OrbitClearanceWorld && corridor.Outer > p.Inner - c.OrbitClearanceWorld))
                throw new PlacementException("overlapping orbital corridors");
            corridors.Add(corridor);
            if (belt) belts.Add(new($"SYS-BELT-{i + 1}", corridor.Inner, corridor.Outer, placement.Next()));
            else
            {
                string id = $"SYS-PLANET-{i - beltCount + 1}";
                string kind = new[] { "Rocky", "Icy", "Gas" }[placement.NextInt(0, 3)];
                double phase = placement.NextDouble() * 360;
                var orbit = CircularOrbit(r, phase, vmax, c, source.GameState.MotionTimeMs);
                planets.Add(new(id, kind, width));
                orbits.Add(new(id, orbit));
                objects.Add(new(id, "Planet", "Permanent", id, r * Math.Sin(phase * Math.PI / 180),
                    -r * Math.Cos(phase * Math.PI / 180), 0, 0, "Orbital", null, null, null, IsKnown: true, Orbit: orbit));
            }
        }
        AddBeltAsteroids(objects, belts, orbits, c, seed, vmax, source.GameState.MotionTimeMs, attempt);
        objects.Add(new("SYS-SUN", "Sun", "Permanent", "Sun", 0, 0, 0, 0, "Stationary", null, null, null, IsKnown: true));
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in objects.Select(o => o.ObjectId).Concat(belts.Select(b => b.Id)))
            if (!ids.Add(id)) throw new ScenarioException($"solar-system/v1 seed={seed} version=1 stage=ids attempt={attempt + 1}: collision {id}.");
        double extent = Math.Max(max, corridors.Max(p => p.Outer));
        var map = new SolarSystemMapSnapshot(1, seed, extent * 1.1,
            belts.OrderBy(b => b.InnerRadius).ToImmutableArray(), planets.ToImmutableArray(), orbits.ToImmutableArray());
        var result = source with
        {
            GameState = source.GameState with
            {
                MasterSeed = seed,
                SpaceObjects = objects.OrderBy(o => o.ObjectId, StringComparer.Ordinal).ToArray(),
                SolarSystem = map
            }
        };
        SolarSystemGeneration.ValidateWorld(result.GameState);
        return result;
    }

    private static void AddBeltAsteroids(List<SpaceObjectData> objects, List<BeltMapData> belts,
        List<OrbitMapData> orbits, SolarSystemGenerationConfig config, ulong seed, double vmax, long epoch, int attempt)
    {
        foreach (var belt in belts.OrderBy(b => b.Id, StringComparer.Ordinal))
        {
            var rng = new GeneratorRng(seed, $"belts/{belt.Id}", attempt);
            double orientation = rng.NextDouble() * 360;
            for (int index = 0; index < config.AsteroidsPerBelt; index++)
            {
                bool placed = false;
                for (int placement = 0; placement < config.MaxPlacementAttempts; placement++)
                {
                    double radius = belt.InnerRadius + (belt.OuterRadius - belt.InnerRadius) * rng.NextDouble();
                    // Three clusters separated by empty sectors, independent of the decorative stream.
                    double phase = (orientation + index % 3 * 120 + rng.NextDouble() * 35) % 360;
                    double x = radius * Math.Sin(phase * Math.PI / 180), y = -radius * Math.Cos(phase * Math.PI / 180);
                    if (objects.Any(o => Math.Sqrt(Math.Pow(x - o.PositionX, 2) + Math.Pow(y - o.PositionY, 2)) < config.OrbitClearanceWorld)) continue;
                    var orbit = CircularOrbit(radius, phase, vmax, config, epoch);
                    string id = $"SYS-AST-{belt.Id[9..]}-{index + 1}";
                    string composition = new[] { "Ice", "Silicate", "Iron" }[rng.NextInt(0, 3)];
                    long mass = rng.NextInt(1000000, 1000000001);
                    objects.Add(new(id, "Asteroid", "Permanent", id, x, y, 0, 0, "Orbital", mass,
                        composition, null, IsKnown: true, Orbit: orbit));
                    orbits.Add(new(id, orbit));
                    placed = true;
                    break;
                }
                if (!placed) throw new PlacementException($"belt {belt.Id} asteroid {index + 1} clearance exhausted");
            }
        }
    }

    internal static OrbitalElements CircularOrbit(double radius, double phase, double vmax,
        SolarSystemGenerationConfig config, long epoch)
    {
        long period = checked((long)Math.Round(Math.Tau * radius / (vmax * config.OrbitSpeedFraction * 10)
            * 1000 * SimulationSpeedExtensions.BaseGameSecondsPerRealSecond));
        if (period <= 0) throw new PlacementException("nonpositive orbital period");
        return new(radius, radius, period, (int)Math.Floor(phase), phase - Math.Floor(phase), epoch, "clockwise");
    }

    private sealed class PlacementException(string message) : Exception(message);

    internal sealed class GeneratorRng(ulong seed, string stage, int attempt)
    {
        private ulong _state = RngStreamSeedDerivation.DeriveStreamSeed(seed, $"solar-system/v1/{stage}/{attempt}");
        internal ulong Next()
        {
            unchecked
            {
                ulong z = (_state += 0x9E3779B97F4A7C15UL);
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }
        internal double NextDouble() => (Next() >> 11) * (1.0 / 9007199254740992.0);
        internal int NextInt(int min, int exclusiveMax) => min + (int)(NextDouble() * (exclusiveMax - min));
    }
}
