using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed partial class BattleResultsScreen
{
    private float _warningScroll;
    private float _warningMaxScroll;
    internal SKRect StayBounds { get; private set; }

    internal BattleResultsCapacitySnapshot? Capacity
    {
        get
        {
            if (CurrentLoot is { FreePrisonerCabins: { } cabins } loot)
                return new(loot.FreeCargoKg, cabins);
            var result = Flow.Session.DemoCampaign?.BattleResults;
            return result?.BattleId == _terminalBattle?.BattleId ? result?.Capacity : null;
        }
    }

    private void DrawCapacity(SKCanvas canvas)
    {
        if (Capacity is not { } capacity) return;
        string label = ResultsText("FreeCargo", capacity.FreeCargoKg) + "    ·    " +
            ResultsText("FreeCabins", capacity.FreePrisonerCabins);
        T(canvas, label, Contents.Left, ExitBounds.Top - 13, FitSize(label, 16, Contents.Width), Text);
    }

    internal string[] LeavingRows()
    {
        if (CurrentLoot is not { } loot) return [];
        return loot.Objects.SelectMany(source => LootRows(source))
            .Where(row => row.State is null or LootAttemptStates.NotAttempted or LootAttemptStates.ExtractedWaiting)
            .Select(row => row.Quantity is { } quantity ? row.Name + " · " + ResultsText("Quantity", quantity) : row.Name)
            .ToArray();
    }

    private void DrawLeaveWarning(SKCanvas canvas)
    {
        string title = ResultsText("Remaining");
        Round(canvas, Contents, Panel, BoxEdge);
        T(canvas, title, Contents.Left + 16, Contents.Top + 30, FitSize(title, 21, Contents.Width - 32), Text, true);
        var list = new SKRect(Contents.Left + 16, Contents.Top + 48, Contents.Right - 16, Contents.Bottom - 10);
        var rows = LeavingRows();
        const float lineHeight = 29;
        _warningMaxScroll = Math.Max(0, rows.Length * lineHeight - list.Height);
        _warningScroll = Math.Clamp(_warningScroll, 0, _warningMaxScroll);
        canvas.Save();
        try
        {
            canvas.ClipRect(list);
            for (int i = 0; i < rows.Length; i++)
            {
                float y = list.Top + 19 + i * lineHeight - _warningScroll;
                if (y < list.Top || y > list.Bottom + 19) continue;
                T(canvas, rows[i], list.Left, y, FitSize(rows[i], 16, list.Width), Text);
            }
        }
        finally { canvas.Restore(); }

        StayBounds = new(ExitBounds.Left - 210, ExitBounds.Top, ExitBounds.Left - 10, ExitBounds.Bottom);
        MenuStyle.DrawButton(canvas, StayBounds, ResultsText("Stay"), Flow.Pending ? ButtonState.Disabled : ButtonState.Normal);
    }
}
