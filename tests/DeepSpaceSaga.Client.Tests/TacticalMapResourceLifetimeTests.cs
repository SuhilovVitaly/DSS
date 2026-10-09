using System.Collections;
using System.Reflection;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class TacticalMapResourceLifetimeTests
{
    private static GameSessionScreen Create()
    {
        var buffer = new SnapshotBuffer();
        buffer.Update(new(1, 0, SimulationSpeed.Speed0,
            [new("player", 0, 0, 0, 0)], PlayerShipObjectId: "player"));
        return new(buffer, new LinearMotionPredictor());
    }

    // Inspect native object handles, not managed collection counts or GC timing.
    private static List<SKObject> OwnedNativeObjects(object owner)
    {
        var found = new List<SKObject>();
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        void Visit(object? value)
        {
            if (value is null || !seen.Add(value)) return;
            if (value is SKObject native)
            {
                found.Add(native);
                if (native is SKPaint paint) { Visit(paint.MaskFilter); Visit(paint.PathEffect); }
                return;
            }
            if (value is IDictionary dictionary)
            {
                foreach (var item in dictionary.Values) Visit(item);
                return;
            }
            var type = value.GetType();
            if (type.Namespace?.StartsWith("DeepSpaceSaga.Client.UI", StringComparison.Ordinal) != true) return;
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                Visit(field.GetValue(value));
        }
        Visit(owner);
        return found;
    }

    [Theory]
    [InlineData("SetRoot")]
    [InlineData("Replace")]
    [InlineData("ReplaceAll")]
    [InlineData("DeactivateAll")]
    public void Replacing_root_disposes_owned_resources_once(string transition)
    {
        var stack = new ScreenStack();
        var screen = Create();
        using var replacement = Create();
        stack.SetRoot(screen);
        var owned = OwnedNativeObjects(screen);
        Assert.True(owned.Count > 90);
        Assert.All(owned, resource => Assert.NotEqual(IntPtr.Zero, resource.Handle));
        switch (transition)
        {
            case "SetRoot": stack.SetRoot(replacement); break;
            case "Replace": stack.Replace(replacement); break;
            case "ReplaceAll": stack.ReplaceAll(replacement); break;
            default: stack.DeactivateAll(); break;
        }
        Assert.All(owned, resource => Assert.Equal(IntPtr.Zero, resource.Handle));
        screen.Dispose(); // idempotent, including worker cancellation
        Assert.NotEqual(IntPtr.Zero, XenonStyle.TypefaceRegular.Handle);
        stack.DeactivateAll();
    }

    [Fact]
    public void Pushing_modal_preserves_map_resources()
    {
        var stack = new ScreenStack();
        var screen = Create();
        var modal = Create();
        stack.SetRoot(screen);
        var owned = OwnedNativeObjects(screen);
        var overlayOwned = OwnedNativeObjects(modal);
        stack.Push(modal);
        Assert.All(owned, resource => Assert.NotEqual(IntPtr.Zero, resource.Handle));
        stack.Pop();
        Assert.All(overlayOwned, resource => Assert.Equal(IntPtr.Zero, resource.Handle));
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
        screen.Render(surface.Canvas, 1280, 720);
        Assert.All(owned, resource => Assert.NotEqual(IntPtr.Zero, resource.Handle));
        stack.DeactivateAll();
        Assert.All(owned, resource => Assert.Equal(IntPtr.Zero, resource.Handle));
    }

    [Fact]
    public async Task Repeated_sessions_release_native_resources()
    {
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
        var retainedWrappers = new List<SKObject>();
        for (int i = 0; i < 100; i++)
        {
            var screen = Create();
            screen.Render(surface.Canvas, 1280, 720);
            screen.OnMouseMove(640, 360);
            screen.OnMouseDown(640, 360);
            screen.Render(surface.Canvas, 1280, 720);
            await screen.ObjectInfoPanel.PendingImageWork;
            var owned = OwnedNativeObjects(screen);
            Assert.Contains(owned, resource => resource is SKImage); // lazily generated reticle texture
            retainedWrappers.AddRange(owned);
            screen.Dispose();
            await screen.SnapshotSaveTask;
        }
        // Wrappers are deliberately retained: finalizers/GC cannot hide a missing Dispose.
        Assert.True(retainedWrappers.Count > 9000);
        Assert.All(retainedWrappers, resource => Assert.Equal(IntPtr.Zero, resource.Handle));
        Assert.NotEqual(IntPtr.Zero, XenonStyle.TypefaceRegular.Handle);
    }
}
