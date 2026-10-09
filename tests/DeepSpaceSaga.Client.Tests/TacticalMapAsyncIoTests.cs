using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class TacticalMapAsyncIoTests
{
    [Fact]
    public async Task Image_decode_publishes_after_render()
    {
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cache = new AsyncObjectImageCache(_ =>
        {
            entered.TrySetResult();
            if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException();
            return new SKBitmap(2, 2);
        });
        var panel = new ObjectInfoPanel(cache);
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
        var data = new ObjectInfoPanelData("ship", "Ship", 0, 0, null, "sprite.png");
        try
        {
            panel.Render(surface.Canvas, 1280, 8, data, data, 720);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            panel.Render(surface.Canvas, 1280, 8, data, data, 720);
            Assert.Null(cache.Get("sprite.png"));
            Assert.Equal(1, cache.PendingCount);
        }
        finally { release.Set(); }
        await cache.PendingWork.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Null(cache.Get("sprite.png")); // worker completion alone cannot publish
        panel.Render(surface.Canvas, 1280, 8, data, data, 720);
        Assert.NotNull(cache.Get("sprite.png"));
    }

    [Fact]
    public async Task Closing_cache_disposes_late_image_and_bounds_queue()
    {
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        SKBitmap? decoded = null;
        var cache = new AsyncObjectImageCache(_ =>
        {
            entered.TrySetResult();
            if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException();
            return decoded = new SKBitmap(2, 2);
        });
        try
        {
            for (int i = 0; i < 500; i++) cache.Request(i.ToString());
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(16, cache.PendingCount);
            cache.Dispose();
        }
        finally { release.Set(); }
        await cache.ShutdownTask.WaitAsync(TimeSpan.FromSeconds(5));
        cache.Pump();
        Assert.Equal(0, cache.ReadyCount);
        Assert.NotNull(decoded);
        Assert.Equal(IntPtr.Zero, decoded.Handle);
        cache.Dispose();
    }

    [Fact]
    public async Task Missing_images_are_cached_and_cache_is_bounded()
    {
        int calls = 0;
        using var cache = new AsyncObjectImageCache(_ => { calls++; return null; });
        for (int i = 0; i < 140; i++)
        {
            cache.Request(i.ToString());
            await cache.PendingWork;
            cache.Pump();
        }
        Assert.Equal(128, cache.ReadyCount);
        cache.Request("139");
        await cache.PendingWork;
        Assert.Equal(140, calls);
    }

    [Fact]
    public async Task Decoder_failure_is_cached_without_faulting_layout()
    {
        int calls = 0;
        using var cache = new AsyncObjectImageCache(_ => { calls++; throw new InvalidDataException("bad image"); });
        cache.Request("broken.png");
        await cache.PendingWork;
        cache.Pump();
        cache.Request("broken.png");
        Assert.Equal(1, calls);
        Assert.Null(cache.Get("broken.png"));
        Assert.Equal(1, cache.ReadyCount);
    }

    [Fact]
    public async Task Slow_capture_writer_does_not_block_render_and_closing_screen_cancels_publication()
    {
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0, [new("player", 0, 0, 0, 0)], PlayerShipObjectId: "player"));
        using var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        var stack = new ScreenStack();
        stack.SetRoot(screen);
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        TacticalMapSnapshotDocument? document = null;
        screen.SnapshotWriter = (captured, _) =>
        {
            document = captured;
            entered.TrySetResult();
            if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException();
            return "test.json";
        };
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
        screen.Render(surface.Canvas, 1280, 720);
        screen.RequestTacticalMapSnapshot();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            screen.OnDeactivated(); // a modal must not cancel the session's I/O
            screen.OnActivated();
            screen.Render(surface.Canvas, 1280, 720);
            Assert.False(screen.SnapshotSaveTask.IsCompleted);
            stack.DeactivateAll();
        }
        finally { release.Set(); }
        Assert.Null(await screen.SnapshotSaveTask.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Null(screen.LastTacticalMapSnapshotPath);
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        Assert.Throws<OperationCanceledException>(() => TacticalMapSnapshotWriter.Write(document!, directory, cancelled.Token));
        Assert.False(Directory.Exists(directory));
    }
}
