using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed partial class PhasePanel
{
    private int _dragAxis;
    partial void DrawMove(SKCanvas c)
    {
        if (_frame.Movement?.SelectedOrder is not { } row) return;
        Head(c, U("MoveTitle", PilotCardModel.Name(row.Unit)), "MoveTag");
        if (Recording is { } layout) layout.Rects["slider:speed"] = R(1054, 132, 1260, 148);
        if (Recording is { } l) l.Rects["slider:turn"] = R(1054, 178, 1260, 194);
        Pair(c, U("Speed"), F(row.Order.Speed), 1050, 130, 13);
        Slider(c, 1054, 1260, 140, row.MaximumSpeed == row.MinimumSpeed ? 0 : (row.Order.Speed - row.MinimumSpeed) / (double)(row.MaximumSpeed - row.MinimumSpeed), TacticalBattleSkinRenderScope.Color("ui.slider.speed", "fill", Cyan));
        T(c, F(row.MinimumSpeed), 1050, 156, 10.5f, Muted); T(c, F(row.MaximumSpeed), 1264, 156, 10.5f, Muted, align: SKTextAlign.Right);
        Pair(c, U("TurnLabel"), Deg(row.Order.Turn), 1050, 176, 13);
        Slider(c, 1054, 1260, 186, row.MaximumTurn == 0 ? .5 : (row.Order.Turn + row.MaximumTurn) / (double)(2 * row.MaximumTurn), TacticalBattleSkinRenderScope.Color("ui.slider.turn", "fill", Purple));
        T(c, Deg(-row.MaximumTurn), 1050, 202, 10.5f, Muted); T(c, Deg(row.MaximumTurn), 1264, 202, 10.5f, Muted, align: SKTextAlign.Right);
        ButtonAt(c, "straight", new(1048, 214, 1154, 236), U("Straight")); ButtonAt(c, "ai", new(1160, 214, 1266, 236), U("RestoreAi"));
        T(c, U("MoveHelp"), 1157, 256, 10.5f, Muted, align: SKTextAlign.Center, role: "ui.move.help");
        StandardAction(c, U("AiSuggestions", _frame.Movement.Orders.Count(o => o.Order == o.Suggestion)), U("MoveStatus"), U("MoveAction"));
    }
    internal bool BeginDrag(SKPoint p, TacticalBattleOrders orders)
    {
        if (_frame is null || Phase != "Move" || _frame.Pending || !R(1048, 128, 1266, 196).Contains(p)) return false;
        if (Math.Abs(p.Y - 140) <= 10) _dragAxis = 1;
        else if (Math.Abs(p.Y - 186) <= 10) _dragAxis = 2;
        return Drag(p, orders);
    }
    internal bool Drag(SKPoint p, TacticalBattleOrders orders)
    {
        if (_dragAxis == 0 || orders.Selected is null) return false;
        PilotCardColumn.ApplySlider(orders, _dragAxis == 1, p.X, _offset + 1054, _offset + 1260);
        return true;
    }
    internal void EndDrag() => _dragAxis = 0;
}
