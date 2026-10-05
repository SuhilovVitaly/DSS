using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private SolarSystemMapSnapshot? _solarSystem;

    private static void SynchronizeOrbitalBindings(List<SpaceObjectRuntime> objects, long simulationTimeMs)
    {
        for (int i = 0; i < objects.Count; i++)
        {
            var ship = objects[i];
            if (!ship.IsDocked) continue;
            var parent = objects.FirstOrDefault(o => o.InitialMotion.ObjectId == ship.DockedStationObjectId);
            if (parent?.InitialMotion.Orbit is not { } orbit) continue;
            if (parent.IsDocked || parent.ObjectType != SpaceObjectType.Station)
                throw new ScenarioException("Orbital docking requires a non-docked station parent.");
            var bound = ship.InitialMotion with
            {
                Orbit = orbit,
                WorldOffsetX = parent.InitialMotion.WorldOffsetX + 1,
                WorldOffsetY = parent.InitialMotion.WorldOffsetY + 1
            };
            objects[i] = ship with { InitialMotion = OrbitalMotionMath.At(bound, orbit, simulationTimeMs), StartGameTimeMs = simulationTimeMs };
        }
    }
}
