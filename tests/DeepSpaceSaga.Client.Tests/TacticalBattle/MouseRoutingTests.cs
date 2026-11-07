using System.Numerics;
using System.Reflection;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class MouseRoutingTests
{
    [Theory]
    [InlineData(MouseButton.Left)]
    [InlineData(MouseButton.Right)]
    [InlineData(MouseButton.Middle)]
    public void WindowForwardsButtonAwareReleaseWithExactCoordinates(MouseButton button)
    {
        var screen = new BattleScreen();
        using var window = new SkiaWindow(screen, new Factory(), null, createNativeWindow: false);
        var mouse = Mouse();
        Invoke(window, "OnMouseUp", mouse, button);
        Assert.Equal((125.5f, 75.25f, button), Assert.Single(screen.Releases));
    }

    [Fact]
    public void LegacyScreensStillReceiveOnlyLeftReleaseAndRawWheelSteps()
    {
        var screen = new LegacyScreen();
        using var window = new SkiaWindow(screen, new Factory(), null, createNativeWindow: false);
        var mouse = Mouse();
        Invoke(window, "OnMouseUp", mouse, MouseButton.Right);
        Invoke(window, "OnMouseUp", mouse, MouseButton.Middle);
        Assert.Equal(0, screen.LeftReleases);
        Invoke(window, "OnMouseUp", mouse, MouseButton.Left);
        Assert.Equal(1, screen.LeftReleases);
        Invoke(window, "OnMouseScroll", mouse, new ScrollWheel(0, 1));
        Assert.Equal(1, screen.WheelDelta);
    }

    [Theory]
    [InlineData(1f, -100f)]
    [InlineData(-1f, 100f)]
    [InlineData(0.25f, -25f)]
    [InlineData(0f, 0f)]
    public void WindowNormalizesBattleWheelSignAndNotchWithoutLosingFractionalSteps(float silk, float browser)
    {
        var screen = new BattleScreen();
        using var window = new SkiaWindow(screen, new Factory(), null, createNativeWindow: false);
        Invoke(window, "OnMouseScroll", Mouse(), new ScrollWheel(0, silk));
        Assert.Equal(browser, screen.WheelDelta);
        Assert.Equal(Math.Exp(silk * 0.15), MouseWheelInput.ZoomFactor(screen.WheelDelta), 12);
    }

    [Fact]
    public void ZoomDirectionsAreReciprocalAndInvalidDeviceValuesAreIgnored()
    {
        double up = MouseWheelInput.ZoomFactor(MouseWheelInput.NormalizeSilk(1));
        double down = MouseWheelInput.ZoomFactor(MouseWheelInput.NormalizeSilk(-1));
        Assert.True(up > 1);
        Assert.True(down < 1);
        Assert.Equal(1, up * down, 12);
        foreach (float value in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            Assert.Equal(0, MouseWheelInput.NormalizeSilk(value));
            Assert.Equal(1, MouseWheelInput.ZoomFactor(value));
        }
    }

    [Fact]
    public void WindowMoveUsesCurrentScreenHoverAndClosingSuppressesRelease()
    {
        var screen = new BattleScreen();
        using var window = new SkiaWindow(screen, new Factory(), null, createNativeWindow: false);
        var mouse = Mouse();
        Invoke(window, "OnMouseMove", mouse, new Vector2(200.75f, 99.5f));
        Assert.Equal((200.75f, 99.5f), screen.Hover);
        typeof(SkiaWindow).GetField("_closing", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, true);
        Invoke(window, "OnMouseUp", mouse, MouseButton.Right);
        Assert.Empty(screen.Releases);
    }

    private static IMouse Mouse() => DispatchProxy.Create<IMouse, MouseProxy>();
    private static void Invoke(SkiaWindow window, string name, params object[] args) =>
        typeof(SkiaWindow).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, args);
    public class MouseProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args) => method!.Name switch
        {
            "get_Position" => new Vector2(125.5f, 75.25f),
            _ => throw new NotSupportedException(method.Name)
        };
    }
    private class LegacyScreen : IScreen
    {
        public int LeftReleases { get; private set; }
        public float WheelDelta { get; private set; }
        public virtual bool OnMouseMove(float x, float y) => false;
        public void OnMouseUp(float x, float y) => LeftReleases++;
        public ScreenEvent OnMouseWheel(float x, float y, float delta) { WheelDelta = delta; return ScreenEvent.None; }
        public ScreenEvent OnMouseDown(float x, float y, MouseButton button) => ScreenEvent.None;
        public ScreenEvent OnKeyDown(Key key) => ScreenEvent.None;
        public void Render(SKCanvas canvas, int width, int height) { }
        public void OnActivated() { }
        public void OnDeactivated() { }
    }
    private sealed class BattleScreen : LegacyScreen, IScreen
    {
        public bool UsesBrowserWheelDelta => true;
        public List<(float, float, MouseButton)> Releases { get; } = [];
        public (float, float) Hover { get; private set; }
        public void OnMouseUp(float x, float y, MouseButton button) => Releases.Add((x, y, button));
        public override bool OnMouseMove(float x, float y) { Hover = (x, y); return true; }
    }
    private sealed class Factory : IGameSessionFactory
    {
        public IGameSessionConnection CreateSession() => throw new NotSupportedException();
        public IGameSessionConnection CreateSessionFromSave(string slotId) => throw new NotSupportedException();
        public IGameSessionConnection CreateSessionFromScenario(string path) => throw new NotSupportedException();
        public ScenarioInfo[] ListScenarios() => [];
        public SaveSlotInfo[] ListSaveSlots() => [];
        public bool HasQuickSave() => false;
        public void DeleteSaveSlot(string id) { }
    }
}
