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

        var (reader, loop, cts) = StartEngine(engine, TimeSpan.FromSeconds(10));

        // Read first snapshot at Speed1
        var s1 = await ReadNextAsync(reader, cts.Token);
        long timeAtS1 = s1.GameTimeMs;
        Assert.True(timeAtS1 > 0);

        // Set pause
        engine.SetSpeed(SimulationSpeed.Speed0);

        // Read another snapshot — GameTime should not advance
        var s2 = await ReadNextAsync(reader, cts.Token);
        Assert.Equal(timeAtS1, s2.GameTimeMs);
        Assert.Equal(SimulationSpeed.Speed0, s2.CurrentSpeed);

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

        // Read snapshot at Speed1 — object is moving
        var s1 = await ReadNextAsync(reader, cts.Token);
        var obj1 = s1.Objects[0];
        Assert.True(obj1.X > 0, "Object should have moved right");

        // Pause
        engine.SetSpeed(SimulationSpeed.Speed0);

        // Read another snapshot — position must be unchanged
        var s2 = await ReadNextAsync(reader, cts.Token);
        var obj2 = s2.Objects[0];
        Assert.Equal(obj1.X, obj2.X);
        Assert.Equal(obj1.Y, obj2.Y);

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
