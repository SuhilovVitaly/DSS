using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>Local world session over an explicit calibrated Basic6km/s,4deg/s scenario.</summary>
internal sealed class PointCombatSessionFixture : IAsyncDisposable
{
    internal const string Player = "SPC-0001", Enemy = "SPC-0002", Torpedo = "MOD-PLAYER-TORPEDO-01";
    private static readonly string Root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
    internal SimulationEngine Engine { get; }
    internal LocalGameSessionConnection Connection { get; }
    private long _realMs;

    internal static GameDataRegistry Registry() =>
        EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(Root, "Settings.json"), out _, out _);
    internal PointCombatSessionFixture(GameDataRegistry? registry = null, bool engineEnabled = false, double enemyX = 400)
    {
        var source = ScenarioLoader.LoadFromFile(Path.Combine(Root, "Scenarios", "PlayerShipOnly", "scenario.json"));
        Engine = new(registry ?? Registry(), clock: new SimulationClock(SimulationSpeed.Speed0, () => Interlocked.Read(ref _realMs)));
        Engine.LoadScenario(source with
        {
            GameState = source.GameState with
            {
                MasterSeed = 42,
                CurrentSpeed = "Speed0",
                SpaceObjects = source.GameState.SpaceObjects.Select(o => o with
                {
                    PositionX = o.ObjectId == Player ? 0 : enemyX,
                    PositionY = 0,
                    SpeedMps = 0,
                    MovementType = "Stationary",
                    IsKnown = true,
                    Modules = o.Modules!.Select(m => m with
                    {
                        ModuleTypeId = m.ModuleTypeId.StartsWith("module.engine.") ? "module.engine.basic" : m.ModuleTypeId,
                        PowerState = m.ModuleTypeId.StartsWith("module.engine.") && !engineEnabled ? "Off" : "On"
                    }).ToArray()
                }).ToArray()
            }
        });
        Connection = new(Engine);
    }

    internal void SetReal(long milliseconds) => Interlocked.Exchange(ref _realMs, milliseconds);
    public async ValueTask DisposeAsync() => await Connection.DisposeAsync();
}
