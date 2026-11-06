using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0018TK0002Tests
{
    internal const string Player = "SPC-0001", Enemy = "SPC-0002", Launcher = "MOD-PLAYER-TORPEDO-01";
    private static readonly string Root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/DeepSpaceSaga.Client"));
    internal static GameDataRegistry Registry() => EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(Root, "Settings.json"), out _, out _);
    internal static SimulationEngine Create(ulong seed = 42)
    {
        var source = ScenarioLoader.LoadFromFile(Path.Combine(Root, "Scenarios/PlayerShipOnly/scenario.json"));
        var engine = new SimulationEngine(Registry(), clock: new SimulationClock(SimulationSpeed.Speed0, () => 0));
        engine.LoadScenario(source with
        {
            GameState = source.GameState with
            {
                MasterSeed = seed,
                CurrentSpeed = "Speed0",
                SpaceObjects = source.GameState.SpaceObjects.Select(o => o with
                {
                    PositionX = o.ObjectId == Player ? 0 : 600,
                    PositionY = 0,
                    SpeedMps = 0,
                    MovementType = "Stationary",
                    Ai = null,
                    Modules = o.Modules!.Select(m => m with { PowerState = o.ObjectId == Player ? "On" : "Off" }).ToArray()
                }).ToArray()
            }
        });
        return engine;
    }
    internal static AuthoritativeSnapshot At(SimulationEngine engine, long time = 0) => engine.CaptureSnapshotForTests(time * 300, SimulationSpeed.Speed0, time);
}
