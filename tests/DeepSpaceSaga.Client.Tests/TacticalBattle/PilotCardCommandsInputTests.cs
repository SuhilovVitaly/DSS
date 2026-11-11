using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

public sealed class PilotCardCommandsInputTests
{
    [Fact]
    public void Commands_are_clipped_and_belong_to_current_selection_only()
    {
        PilotCard[] cards =
        [
            new("one", "One", "", "", true, [], "", false, []),
            new("two", "Two", "", "", false, [], "", false, [])
        ];
        var column = new PilotCardColumn();
        column.Arrange(cards, _ => 114);
        column.RegisterCommand("two", "hold", new(10, 240, 180, 252));
        column.RegisterCommand("one", "speed+", new(176, 94, 190, 107));
        column.RegisterCommand("one", "clipped", new(10, 0, 180, 12));

        Assert.True(column.CommandBounds("hold").IsEmpty);
        var command = Assert.IsType<PilotCardCommand>(column.HitCommand(new(182, 100)));
        Assert.Equal("one", command.UnitId);
        Assert.Equal("speed+", command.Action);
        Assert.Equal(new SKRect(10, 6, 180, 12), column.CommandBounds("clipped"));
        Assert.Null(column.HitCommand(new(100, 5)));
        Assert.Null(column.HitCommand(new(100, 245)));

        column.Arrange([cards[0] with { Selected = false }, cards[1] with { Selected = true }], _ => 114);
        Assert.True(column.CommandBounds("speed+").IsEmpty);
        Assert.Null(column.HitCommand(new(182, 100)));
    }
}
