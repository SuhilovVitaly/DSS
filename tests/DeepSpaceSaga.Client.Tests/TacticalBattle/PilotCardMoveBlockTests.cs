using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class PilotCardMoveBlockTests
{
    [Fact]
    public async Task Commands_use_the_selected_drone_and_match_keyboard_edits()
    {
        await using var card = await Ep0019Scenes.Create("1-move-no-hover");
        await using var keys = await Ep0019Scenes.Create("1-move-no-hover");
        using var image = card.Render();
        var screen = card.Screen;
        Assert.Equal(new SKRect(262.5f, 141, 285, 160.5f), screen.CardCommandBounds("speed+"));
        Assert.Equal(new SKRect(154.5f, 261, 285, 285), screen.CardCommandBounds("ai"));
        Click("speed+");
        Click("turn+");
        keys.Screen.OnKeyDown(Key.Up);
        keys.Screen.OnKeyDown(Key.Right);
        Assert.Equal(Assert.Single(keys.Screen.BuildMoveRequest().Orders), Assert.Single(screen.BuildMoveRequest().Orders));

        Click("straight");
        Assert.Equal(0, Assert.Single(screen.BuildMoveRequest().Orders).Turn);
        Click("ai");
        Assert.Equal(screen.Movement.SelectedOrder!.Suggestion, Assert.Single(screen.BuildMoveRequest().Orders));

        var speed = screen.CardCommandBounds("slider:speed");
        screen.OnMouseDown(speed.MidX, speed.MidY, MouseButton.Left);
        Assert.True(screen.OnMouseMove(speed.Right + 100, speed.MidY));
        screen.OnMouseUp(speed.Right + 100, speed.MidY, MouseButton.Left);
        Assert.Equal(screen.Movement.SelectedOrder!.MaximumSpeed, Assert.Single(screen.BuildMoveRequest().Orders).Speed);
        screen.OnKeyDown(Key.Tab);
        var before = screen.BuildMoveRequest().Orders;
        Click("speed-");
        Assert.Equal(before.ToArray(), screen.BuildMoveRequest().Orders.ToArray());

        void Click(string action)
        {
            var r = screen.CardCommandBounds(action);
            Assert.False(r.IsEmpty);
            screen.OnMouseDown(r.MidX, r.MidY, MouseButton.Left);
            screen.OnMouseUp(r.MidX, r.MidY, MouseButton.Left);
        }
    }
}
