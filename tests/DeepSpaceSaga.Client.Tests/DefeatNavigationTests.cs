using System.Reflection;
using DeepSpaceSaga.Client.Tests.TacticalBattle;
using DeepSpaceSaga.Client.UI;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests;

[Collection("InterfaceLog")]
public class WorldWindowKeyboardTests
{

    [Fact]
    public async Task Window_edge_buffers_hold_simultaneous_press_and_release_of_all_tracked_keys()
    {
        await using var f = await BattleNavigationTests.Fixture.Create();
        var tracker = Get<KeyboardEdgeTracker>(f.Window, "_keyboardEdges");
        var pressed = Get<Key[]>(f.Window, "_keyboardPressedKeys");
        var released = Get<Key[]>(f.Window, "_keyboardReleasedKeys");
        var down = tracker.PollBoth(_ => true, pressed, released);
        Assert.Contains(Key.Enter, pressed.Take(down.pressedCount));
        Assert.Contains(Key.Tab, pressed.Take(down.pressedCount));
        var up = tracker.PollBoth(_ => false, pressed, released);
        Assert.Equal(down.pressedCount, up.releasedCount);
        tracker.ResyncToCurrentState(_ => true);
        Assert.Equal(0, tracker.PollBoth(_ => true, pressed, released).pressedCount);
    }

    private static T Get<T>(object target, string name) =>
        (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;
}
