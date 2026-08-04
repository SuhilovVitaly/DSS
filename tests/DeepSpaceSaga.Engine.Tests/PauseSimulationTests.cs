using System.Collections.Immutable;
using System.Threading.Channels;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;

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
        Assert.Equal(t1, s2.GameTimeMs);

        // Resume Speed1
        engine.SetSpeed(SimulationSpeed.Speed1);
        var s3 = await ReadNextAsync(reader, cts.Token);
        Assert.True(s3.GameTimeMs > t1, "GameTime should advance after resume");

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
}
