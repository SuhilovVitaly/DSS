using System.Collections.Immutable;
using System.Threading.Channels;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public class PauseSimulationTests
{
    /// <summary>
    /// Runs the engine in the background, feeding snapshots into a channel.
    /// Returns the channel reader for consuming snapshots sequentially.
    /// </summary>
    private static (ChannelReader<AuthoritativeSnapshot> Reader, Task LoopTask, CancellationTokenSource Cts)
        StartEngine(SimulationEngine engine, TimeSpan timeout)
    {
        var cts = new CancellationTokenSource(timeout);
        var channel = Channel.CreateUnbounded<AuthoritativeSnapshot>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });

        var loopTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var snapshot in engine.RunAsync(cts.Token))
                {
                    await channel.Writer.WriteAsync(snapshot, cts.Token);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                channel.Writer.TryComplete();
            }
        });

        return (channel.Reader, loopTask, cts);
    }

    private static async Task<AuthoritativeSnapshot> ReadNextAsync(
        ChannelReader<AuthoritativeSnapshot> reader,
        CancellationToken ct)
    {
        await foreach (var snapshot in reader.ReadAllAsync(ct))
        {
            return snapshot;
        }

        throw new InvalidOperationException("No snapshot produced.");
    }

    [Fact]
    public async Task GameTime_stops_on_pause()
    {
        var engine = new SimulationEngine();
        engine.AddTestObject(new ObjectMotionSnapshot("test", 0, 0, SpeedKmS: 0, Direction: 0));

        var (reader, loop, cts) = StartEngine(engine, TimeSpan.FromSeconds(15));

        // Discard immediate snapshot (GameTime=0)
        await ReadNextAsync(reader, cts.Token);

        // Read one snapshot at Speed1 (after the 1-second delay, GameTime > 0)
        var s1 = await ReadNextAsync(reader, cts.Token);
        Assert.True(s1.GameTimeMs > 0);

        // Set pause — SetSpeed may accumulate a tiny partial interval at Speed1,
        // so the next snapshot might have slightly higher GameTimeMs than s1.
        engine.SetSpeed(SimulationSpeed.Speed0);

        // Read two paused snapshots — their GameTime must be identical,
        // proving that time does not advance while paused.
        var s2 = await ReadNextAsync(reader, cts.Token);
        Assert.Equal(SimulationSpeed.Speed0, s2.CurrentSpeed);

        var s3 = await ReadNextAsync(reader, cts.Token);
        Assert.Equal(SimulationSpeed.Speed0, s3.CurrentSpeed);
        Assert.Equal(s2.GameTimeMs, s3.GameTimeMs);

        cts.Cancel();
        try { await loop; } catch { }
    }

    [Fact]
    public async Task Objects_stop_moving_on_pause()
    {
        var engine = new SimulationEngine();
        // Object moving at 5 km/s = 50 world units/s
        engine.AddTestObject(new ObjectMotionSnapshot("mover", X: 0, Y: 0, SpeedKmS: 5, Direction: 90));

        var (reader, loop, cts) = StartEngine(engine, TimeSpan.FromSeconds(10));

        // Discard immediate snapshot (GameTime=0, object at initial position)
        await ReadNextAsync(reader, cts.Token);

        // Read snapshot at Speed1 — object has moved after 1-second delay
        var s1 = await ReadNextAsync(reader, cts.Token);
        var obj1 = s1.Objects[0];
        Assert.True(obj1.X > 0, "Object should have moved right");

        // Pause
        engine.SetSpeed(SimulationSpeed.Speed0);

        // Read another snapshot — position must be effectively unchanged.
        // SetSpeed accumulates partial-interval time at the old speed,
        // so a tiny advance (a few ms of real time between snapshot read and SetSpeed)
        // is expected and correct. The object must NOT have moved a full Speed1 step.
        var s2 = await ReadNextAsync(reader, cts.Token);
        var obj2 = s2.Objects[0];
        double drift = Math.Abs(obj2.X - obj1.X);
        Assert.True(drift < 2.0,
            $"Object drifted {drift:F2} units during pause — expected < 2.0 (partial interval only)");
        Assert.Equal(obj1.Y, obj2.Y, precision: 9);

        cts.Cancel();
        try { await loop; } catch { }
    }

    [Fact]
    public async Task Resume_continues_from_paused_game_time()
    {
        var engine = new SimulationEngine();
        engine.AddTestObject(new ObjectMotionSnapshot("test", 0, 0, SpeedKmS: 0, Direction: 0));

        var (reader, loop, cts) = StartEngine(engine, TimeSpan.FromSeconds(15));

        // Speed1
        var s1 = await ReadNextAsync(reader, cts.Token);
        long t1 = s1.GameTimeMs;

        // Pause
        engine.SetSpeed(SimulationSpeed.Speed0);
        var s2 = await ReadNextAsync(reader, cts.Token);
        long pausedGameTimeMs = s2.GameTimeMs;
        long pauseTransitionDeltaMs = pausedGameTimeMs - t1;
        Assert.True(
            pauseTransitionDeltaMs >= 0 && pauseTransitionDeltaMs < 100,
            $"Pause transition advanced {pauseTransitionDeltaMs}ms; expected only a tiny pre-pause partial interval.");

        // Resume Speed1
        engine.SetSpeed(SimulationSpeed.Speed1);
        var s3 = await ReadNextAsync(reader, cts.Token);
        Assert.True(s3.GameTimeMs > pausedGameTimeMs, "GameTime should advance after resume");

        cts.Cancel();
        try { await loop; } catch { }
    }

    [Fact]
    public async Task Already_paused_stays_paused()
    {
        var engine = new SimulationEngine();
        engine.AddTestObject(new ObjectMotionSnapshot("test", 0, 0, SpeedKmS: 0, Direction: 0));
        engine.SetSpeed(SimulationSpeed.Speed0);

        var (reader, loop, cts) = StartEngine(engine, TimeSpan.FromSeconds(15));

        var s1 = await ReadNextAsync(reader, cts.Token);
        Assert.Equal(SimulationSpeed.Speed0, s1.CurrentSpeed);
        long t1 = s1.GameTimeMs;

        // "Open modal" while already paused
        engine.SetSpeed(SimulationSpeed.Speed0);
        var s2 = await ReadNextAsync(reader, cts.Token);
        Assert.Equal(t1, s2.GameTimeMs);

        // "Close modal" — should stay paused (restore saved speed = Speed0)
        engine.SetSpeed(SimulationSpeed.Speed0);
        var s3 = await ReadNextAsync(reader, cts.Token);
        Assert.Equal(t1, s3.GameTimeMs);
        Assert.Equal(SimulationSpeed.Speed0, s3.CurrentSpeed);

        cts.Cancel();
        try { await loop; } catch { }
    }

    [Fact]
    public async Task Restore_previous_speed_after_pause()
    {
        var engine = new SimulationEngine();
        engine.AddTestObject(new ObjectMotionSnapshot("test", 0, 0, SpeedKmS: 0, Direction: 0));

        // Start at Speed2
        engine.SetSpeed(SimulationSpeed.Speed2);
        Assert.Equal(SimulationSpeed.Speed2, engine.CurrentSpeed);

        // Read one snapshot, then pause, then restore
        var (reader, loop, cts) = StartEngine(engine, TimeSpan.FromSeconds(10));
        await ReadNextAsync(reader, cts.Token);

        engine.SetSpeed(SimulationSpeed.Speed0);
        Assert.Equal(SimulationSpeed.Speed0, engine.CurrentSpeed);

        engine.SetSpeed(SimulationSpeed.Speed2);
        Assert.Equal(SimulationSpeed.Speed2, engine.CurrentSpeed);

        cts.Cancel();
        try { await loop; } catch { }
    }

    [Fact]
    public async Task Snapshot_while_paused_has_speed0()
    {
        var engine = new SimulationEngine();
        engine.AddTestObject(new ObjectMotionSnapshot("test", 0, 0, SpeedKmS: 0, Direction: 0));

        var (reader, loop, cts) = StartEngine(engine, TimeSpan.FromSeconds(10));

        // Get one snapshot at Speed1
        await ReadNextAsync(reader, cts.Token);

        // Pause
        engine.SetSpeed(SimulationSpeed.Speed0);

        // Next snapshot must report Speed0
        var s = await ReadNextAsync(reader, cts.Token);
        Assert.Equal(SimulationSpeed.Speed0, s.CurrentSpeed);

        cts.Cancel();
        try { await loop; } catch { }
    }

    [Fact]
    public async Task Multiple_snapshots_during_pause_have_same_game_time()
    {
        var engine = new SimulationEngine();
        engine.AddTestObject(new ObjectMotionSnapshot("test", 0, 0, SpeedKmS: 0, Direction: 0));

        var (reader, loop, cts) = StartEngine(engine, TimeSpan.FromSeconds(20));

        // Get initial snapshot, then pause
        await ReadNextAsync(reader, cts.Token);
        engine.SetSpeed(SimulationSpeed.Speed0);

        // Read two more snapshots during pause
        var s1 = await ReadNextAsync(reader, cts.Token);
        var s2 = await ReadNextAsync(reader, cts.Token);

        Assert.Equal(s1.GameTimeMs, s2.GameTimeMs);

        cts.Cancel();
        try { await loop; } catch { }
    }

    // --- story-20260822-193700 Batch 4.4: Trade-modal-open-while-paused regression guard ---
    // Not new behavior — BuildSnapshot has always run independently of SimulationSpeed (see
    // CompleteActiveEngineCycles/ApplyPendingCommands calls at the top of BuildSnapshot, which
    // are not gated on speed). This test pins that existing behavior specifically for the new
    // Trade projection: a player who opens the Trade modal (which pauses the world, per
    // Docs\FirstRelease\Mechanics\Trading.md) must still see command confirmations and an
    // up-to-date DockedStationTrade in the next ~1 Hz snapshot even though gameplay is frozen.

    private const string PlayerShipId = "SPC-0001";
    private const string CargoModuleId = "MOD-CARGO-01";
    private const string StationId = "STATION-01";
    private const string EnergyCellsId = "item.energy-cells";

    private static SimulationEngine CreateDockedTradeEngine()
    {
        var registry = GameDataRegistry.Create(
            [
                new ModuleCategoryDefinition(
                    "module.container", "Container", SlotSize: 1,
                    CommandTypeIds: ImmutableArray.Create(TradeCommandTypes.Buy, TradeCommandTypes.Sell))
            ],
            [
                new ModuleTypeDefinition(
                    "module.container.basic", "Container", SlotSize: 1, MassKg: 20000,
                    StructurePointsMax: 400, PowerConsumptionW: 0,
                    CommandTypeIds: ImmutableArray.Create(TradeCommandTypes.Buy, TradeCommandTypes.Sell),
                    CargoCapacityKg: 1000,
                    BaseCycleTimeMs: 1000)
            ],
            [
                new ItemTypeDefinition(EnergyCellsId, "Energy Cells", UnitMassKg: 10, BasePriceCredits: 200)
            ],
            [
                new CommandDefinition(TradeCommandTypes.Buy, "Buy", Target: "none", Type: "module.container"),
                new CommandDefinition(TradeCommandTypes.Sell, "Sell", Target: "none", Type: "module.container")
            ]);

        var engine = new SimulationEngine(registry);
        engine.LoadScenario(ScenarioLoader.LoadFromJson($$"""
        {
          "scenarioMetadata": { "scenarioId": "test", "name": "Test" },
          "gameState": {
            "gameTimeMs": 0,
            "currentSpeed": "Speed0",
            "playerShipObjectId": "{{PlayerShipId}}",
            "playerTokens": 5000,
            "spaceObjects": [
              {
                "objectId": "{{PlayerShipId}}",
                "objectType": "PlayerShip",
                "persistenceType": "Permanent",
                "positionX": 10000,
                "positionY": 10000,
                "speedMps": 0,
                "directionDegrees": 0,
                "movementType": "Stationary",
                "isDocked": true,
                "dockedStationObjectId": "{{StationId}}",
                "hullLayout": { "width": 1, "height": 1, "cells": [ {"x":0,"y":0} ] },
                "modules": [
                  {
                    "moduleId": "{{CargoModuleId}}",
                    "moduleTypeId": "module.container.basic",
                    "occupiedCells": [ {"x":0,"y":0} ],
                    "structurePoints": 400,
                    "powerState": "On",
                    "operationalState": "Ready",
                    "activeCycle": null,
                    "cargo": []
                  }
                ]
              },
              {
                "objectId": "{{StationId}}",
                "objectType": "Station",
                "persistenceType": "Permanent",
                "credits": 1000000,
                "priceCoefficient": 1000,
                "inventory": [ { "itemTypeId": "{{EnergyCellsId}}", "quantity": 100 } ],
                "positionX": 10001,
                "positionY": 10000,
                "speedMps": 0,
                "directionDegrees": 0,
                "movementType": "Stationary"
              }
            ]
          }
        }
        """));

        return engine;
    }

    [Fact]
    public void Snapshot_at_Speed0_still_publishes_command_results_and_trade_projection()
    {
        var engine = CreateDockedTradeEngine();

        engine.ReceiveCommand(new PlayerCommand(
            "cmd-buy", 1, PlayerShipId, CargoModuleId, TradeCommandTypes.Buy,
            ItemTypeId: EnergyCellsId, Quantity: 10));

        // Explicitly Speed0 — the world is paused (as it would be with the Trade modal open).
        var snapshot = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0);

        Assert.Equal(SimulationSpeed.Speed0, snapshot.CurrentSpeed);

        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Executed, result.Status);

        // unitPrice = 200 * StationSizeFactors.Resolve(Medium fallback, Good) 1.15 = 230
        // (story-20260825-084409 Batch 2, U5: no explicit stationSize here, so the station
        // falls back to StationSize.Medium — see SimulationEngine.ResolveStationSize).
        Assert.Equal(2700, snapshot.PlayerCredits); // 5000 - 10*230

        Assert.NotNull(snapshot.DockedStationTrade);
        var energyCells = Assert.Single(snapshot.DockedStationTrade!.Items);
        Assert.Equal(90, energyCells.StockQuantity); // 100 - 10 bought
    }
}
