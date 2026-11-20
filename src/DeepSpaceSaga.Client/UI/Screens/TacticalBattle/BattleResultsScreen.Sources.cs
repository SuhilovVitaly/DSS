using DeepSpaceSaga.Contracts;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed partial class BattleResultsScreen
{
    private const float CardHeight = 56;
    private const float DetailsHeight = 48;
    private readonly HashSet<string> _expandedSources = new(StringComparer.Ordinal);
    private readonly List<(string Id, SKRect Bounds)> _sourceHits = [];
    private float _scroll;
    private float _maxScroll;

    internal IReadOnlyList<(string Id, SKRect Bounds)> SourceHits => _sourceHits;
    internal bool IsExpanded(string objectId) => _expandedSources.Contains(objectId);

    private LootSessionSnapshot? CurrentLoot
    {
        get
        {
            var loot = Flow.Session.DemoCampaign?.Rewards?.Loot;
            return loot is { Phase: LootPhases.Open } && loot.BattleId == _terminalBattle?.BattleId && Report?.CanLoot == true
                ? loot : null;
        }
    }

    private void DrawSources(SKCanvas canvas)
    {
        _sourceHits.Clear();
        if (CurrentLoot is not { } loot || Contents.IsEmpty) return;
        float height = loot.Objects.Sum(source => CardHeight + (IsExpanded(source.ObjectId) ? DetailsHeight : 0));
        _maxScroll = Math.Max(0, height - Contents.Height);
        _scroll = Math.Clamp(_scroll, 0, _maxScroll);
        float y = Contents.Top - _scroll;
        canvas.Save();
        try
        {
            canvas.ClipRect(Contents);
            foreach (var source in loot.Objects)
            {
                bool expanded = IsExpanded(source.ObjectId);
                var card = new SKRect(Contents.Left, y, Math.Min(Contents.Left + 350, Contents.Right), y + CardHeight - 5);
                var visible = SKRect.Intersect(card, Contents);
                if (!visible.IsEmpty)
                {
                    _sourceHits.Add((source.ObjectId, visible));
                    DrawSourceCard(canvas, source, card, expanded);
                }
                y += CardHeight;
                if (!expanded) continue;
                var details = new SKRect(Contents.Left, y, Contents.Right, y + DetailsHeight - 5);
                DrawSourceDetails(canvas, source, details);
                y += DetailsHeight;
            }
        }
        finally { canvas.Restore(); }
    }

    private void DrawSourceCard(SKCanvas canvas, LootObjectSnapshot source, SKRect card, bool expanded)
    {
        var unit = _terminalBattle!.Units.FirstOrDefault(unit => unit.Id == source.ObjectId);
        string name = unit is null ? source.ObjectId : PilotCardModel.Name(unit);
        string kind = Localization.Get("loot.source." + source.Kind);
        Round(canvas, card, expanded ? PanelSel : Box, BoxEdge);
        T(canvas, expanded ? "−" : "+", card.Left + 10, card.Top + 24, 18, Yellow, true);
        T(canvas, name, card.Left + 34, card.Top + 21, FitSize(name, 17, card.Width - 46), Text, true);
        T(canvas, kind, card.Left + 34, card.Top + 40, FitSize(kind, 13, card.Width - 46), Muted);
    }

    private static void DrawSourceDetails(SKCanvas canvas, LootObjectSnapshot source, SKRect details)
    {
        Round(canvas, details, Panel, BoxEdge);
        string label = string.Format(Localization.Get("BattleResults.Quantity"),
            source.Modules.Length + source.Consumables.Length + (source.Scrap is null ? 0 : 1) + (source.Pilot is null ? 0 : 1));
        T(canvas, label, details.Left + 16, details.MidY + 5, 16, Text);
    }

    private void ToggleSource(float x, float y)
    {
        foreach (var hit in _sourceHits)
        {
            if (!hit.Bounds.Contains(x, y)) continue;
            if (!_expandedSources.Add(hit.Id)) _expandedSources.Remove(hit.Id);
            break;
        }
    }
}
