using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class TorpedoImpactTests
{
    internal const string Player = "SPC-0001", Target = "SPC-0002", Launcher = "MOD-PLAYER-TORPEDO-01";
    internal static readonly string ClientRoot = SeededWorldBootstrapTests.ClientRoot;
    internal static readonly Lazy<GameDataRegistry> Registry = new(() => PointCombatUS0018TK0002Tests.Registry());
    internal static ScenarioFile Scenario(Func<SpaceObjectData, SpaceObjectData>? configure = null,
        params SpaceObjectData[] extras)
    {
        var source = ScenarioLoader.LoadFromFile(Path.Combine(ClientRoot, "Scenarios", "PlayerShipOnly", "scenario.json"));
        return source with
        {
            GameState = source.GameState with
            {
                MasterSeed = 42,
                SpaceObjects = source.GameState.SpaceObjects.Select(o =>
                {
                    var configured = o with
                    {
                        Ai = null,
                        Modules = o.Modules?.Select(m => m with
                        {
                            // Explicit6km/s,4deg/s Basic engine calibration for generic motion regressions.
                            ModuleTypeId = m.ModuleTypeId.StartsWith("module.engine.", StringComparison.Ordinal) ? "module.engine.basic" : m.ModuleTypeId,
                            AutoDefenseEnabled = false,
                            PowerState = o.ObjectType == SpaceObjectType.NpcShip && (m.ModuleTypeId.StartsWith("module.engine.", StringComparison.Ordinal) || m.ModuleTypeId == "module.torpedo.launcher.basic") ? "Off" : m.PowerState
                        }).ToArray(),
                        PositionX = 0,
                        PositionY = o.ObjectId == Player ? 0 : -100,
                        SpeedMps = 0,
                        DirectionDegrees = 0,
                        MovementType = "Stationary"
                    };
                    return configure?.Invoke(configured) ?? configured;
                }).Concat(extras).ToArray()
            }
        };
    }

    internal static SimulationEngine Create(ScenarioFile? scenario = null, GameDataRegistry? registry = null)
    {
        var engine = new SimulationEngine(registry ?? Registry.Value);
        engine.LoadScenario(scenario ?? Scenario());
        return engine;
    }
    internal static AuthoritativeSnapshot At(SimulationEngine engine, long motion, long? calendar = null) =>
        engine.CaptureSnapshotForTests(calendar ?? motion, simulationTimeMs: motion);
    internal static SpaceObjectData Obstacle(string id, double x, double y) =>
        new(id, SpaceObjectType.Asteroid, "Permanent", id, x, y, 0, 0, "Stationary", 1_000_000, "Silicate", null, IsKnown: true);
}
