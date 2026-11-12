using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal static class PilotCardMoveBlock
{
    public const float Height = 104;
    internal const float MoveHeight = 91;

    public static void Draw(SKCanvas canvas, PilotCardColumn column, string drone, TacticalOrderPreview row,
        float left, float right, float top)
    {
        if (TacticalBattleSkinRenderScope.SlimPilots)
        {
            DrawSlim(canvas, column, drone, row, left, right, top);
            return;
        }
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

    private static void DrawSlim(SKCanvas c, PilotCardColumn column, string drone, TacticalOrderPreview row,
        float left, float right, float top)
    {
        var sub = SKColor.Parse("#9AAABB"); var value = SKColor.Parse("#FFE27A");
        var track = SKColor.Parse("#2B241A"); var gold = SKColor.Parse("#C79A3C");
        double speed = row.MaximumSpeed == row.MinimumSpeed ? 0 :
            (row.Order.Speed - row.MinimumSpeed) / (row.MaximumSpeed - row.MinimumSpeed);
        double turn = row.MaximumTurn == 0 ? .5 : (row.Order.Turn + row.MaximumTurn) / (2 * row.MaximumTurn);
        Row(top, "speed", U("Speed"), F(row.Order.Speed), "▼", "▲", speed, F(row.MinimumSpeed), F(row.MaximumSpeed));
        Row(top + 29, "turn", U("TurnLabel"), Deg(row.Order.Turn), "◄", "►", turn, Deg(-row.MaximumTurn), Deg(row.MaximumTurn));
        Control("straight", SKRect.Create(left, top + 58, 67, 14), U("Straight"));
        Control("ai", SKRect.Create(left + 69, top + 58, 67, 14), U("RestoreAi"), row.Order == row.Suggestion);
        PilotCardColumn.SmallText(c, U("MoveHelp"), left, top + 80, 5.6f, sub, italic: true);

        void Row(float y, string axis, string label, string text, string minus, string plus, double fraction, string min, string max)
        {
            PilotCardColumn.SmallText(c, label, left, y + 8, 6.8f, sub);
            using var p = new SKPaint { Color = track, IsAntialias = true };
            c.DrawRect(SKRect.Create(left + 82, y, 30, 11), p);
            PilotCardColumn.SmallText(c, text, left + 109.5f, y + 8.5f, 8, value, true, align: SKTextAlign.Right);
            Control(axis + "-", SKRect.Create(left + 114, y, 11, 11), minus);
            Control(axis + "+", SKRect.Create(left + 127, y, 11, 11), plus);
            float x0 = left + 3, x1 = right - 3, sy = y + 16;
            float thumb = x0 + (x1 - x0) * (float)Math.Clamp(fraction, 0, 1);
            c.DrawRect(new SKRect(x0, sy - 1.8f, x1, sy + 1.8f), p);
            p.Color = gold; c.DrawRect(new SKRect(x0, sy - 1.8f, thumb, sy + 1.8f), p);
            using var gradient = SKShader.CreateLinearGradient(new(thumb, sy - 3.4f), new(thumb, sy + 3.4f),
                [SKColor.Parse("#FFF3C8"), gold, SKColor.Parse("#6A4A18")], [0, .5f, 1], SKShaderTileMode.Clamp);
            p.Color = SKColors.White; p.Shader = gradient; c.DrawCircle(thumb, sy, 3.4f, p);
            column.RegisterCommand(drone, "slider:" + axis, new(left, sy - 4, right, sy + 4));
            PilotCardColumn.SmallText(c, min, left, y + 26, 5.4f, sub);
            PilotCardColumn.SmallText(c, max, right, y + 26, 5.4f, sub, align: SKTextAlign.Right);
        }
        void Control(string action, SKRect rect, string label, bool active = false)
        {
            PilotCardColumn.CommandButton(c, rect, label, active, 6.6f);
            column.RegisterCommand(drone, action, rect);
        }
    }
}
