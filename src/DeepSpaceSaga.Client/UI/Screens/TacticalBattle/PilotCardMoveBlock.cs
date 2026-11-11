using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal static class PilotCardMoveBlock
{
    public const float Height = 104;

    public static void Draw(SKCanvas canvas, PilotCardColumn column, string drone, TacticalOrderPreview row,
        float left, float right, float top)
    {
        Pair(canvas, U("Speed"), F(row.Order.Speed), left, top + 10, 9.5f);
        StepButtons(top, "speed", "▼", "▲");
        double speedFraction = row.MaximumSpeed == row.MinimumSpeed ? 0 :
            (row.Order.Speed - row.MinimumSpeed) / (row.MaximumSpeed - row.MinimumSpeed);
        Axis(top + 23, "speed", speedFraction, Cyan, F(row.MinimumSpeed), F(row.MaximumSpeed));

        Pair(canvas, U("TurnLabel"), Deg(row.Order.Turn), left, top + 50, 9.5f);
        StepButtons(top + 40, "turn", "◄", "►");
        double turnFraction = row.MaximumTurn == 0 ? .5 : (row.Order.Turn + row.MaximumTurn) / (2 * row.MaximumTurn);
        Axis(top + 63, "turn", turnFraction, Purple, Deg(-row.MaximumTurn), Deg(row.MaximumTurn));

        Control("straight", new(left, top + 80, left + 87, top + 96), U("Straight"));
        Control("ai", new(left + 91, top + 80, right, top + 96), U("RestoreAi"), row.Order == row.Suggestion);
        T(canvas, U("MoveHelp"), (left + right) / 2, top + Height, 7.5f, Muted, align: SKTextAlign.Center);

        void StepButtons(float y, string axis, string minus, string plus)
        {
            Control(axis + "-", new(right - 33, y, right - 18, y + 13), minus, size: 9);
            Control(axis + "+", new(right - 15, y, right, y + 13), plus, size: 9);
        }

        void Axis(float y, string axis, double fraction, SKColor tone, string min, string max)
        {
            var fill = TacticalBattleSkinRenderScope.Color("ui.slider." + axis, "fill", tone);
            Slider(canvas, left + 7, right - 7, y, fraction, fill);
            column.RegisterCommand(drone, "slider:" + axis, new(left, y - 8, right, y + 8));
            T(canvas, min, left, y + 12, 7.5f, Muted);
            T(canvas, max, right, y + 12, 7.5f, Muted, align: SKTextAlign.Right);
        }

        void Control(string action, SKRect rect, string label, bool active = false, float size = 9.5f)
        {
            PilotCardColumn.CommandButton(canvas, rect, label, active, size);
            column.RegisterCommand(drone, action, rect);
        }
    }
}
