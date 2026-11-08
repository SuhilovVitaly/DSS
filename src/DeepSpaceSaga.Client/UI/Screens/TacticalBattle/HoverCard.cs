using DeepSpaceSaga.Contracts;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed record AnomalyHoverCard(string Heading, string Name, SKColor Tone, IReadOnlyList<string> Lines);

internal sealed class HoverCard
{
    internal static AnomalyHoverCard? Anomaly(TacticalBattleFrame frame) => AnomalyPresentation.HoverKind(frame) is { } kind
        ? new(M("Anomaly.Hover"), M("Anomaly." + kind), kind == TacticalAnomalyKindCode.Heat ? Heat : Dust,
            M("Anomaly." + kind + "Lines").Split(" / ", StringSplitOptions.None)) : null;
    internal static BuildCardModel? Build(TacticalBattleFrame frame) => BuildCardModel.From(frame);
    public SKRect Bounds { get; private set; }
    public void Draw(SKCanvas c, TacticalBattleFrame f, PilotPortraitCatalog portraits)
    {
        Bounds = SKRect.Empty;
        if (Build(f) is { } build)
        { Bounds = BuildCard.Bounds(f); BuildCard.Draw(c, build, Bounds, portraits); return; }
        var unit = f.Battle.Units.FirstOrDefault(u => u.Id == f.HoverUnitId);
        if (unit is null)
        {
            if (Anomaly(f) is not { } anomaly) return;
            var box = new SKRect(f.Panel.Left, f.Panel.Bottom + 6, f.Panel.Right, f.Panel.Bottom + 110); Bounds = box;
            if (Recording is { } recorded) recorded.Rects["hover"] = box;
            PanelBox(c, box); CursorGlyph(c, box.Left + 9, box.Top + 8, .6f);
            T(c, anomaly.Heading, box.Left + 24, box.Top + 18, 10.5f, Muted, true);
            T(c, anomaly.Name, box.Left + 9, box.Top + 38, 14, anomaly.Tone, true);
            for (int i = 0; i < anomaly.Lines.Count; i++)
                T(c, anomaly.Lines[i], box.Left + 9, box.Top + 58 + 16 * i, FitSize(anomaly.Lines[i], 11, box.Width - 18), Text);
            return;
        }
        bool own = unit.Side == f.Battle.PlayerSide;
        if (own && unit.Kind == TacticalUnitKind.Drone && f.Salvo is null && f.Battle.Phase == TacticalBattlePhase.Over) return;
        var r = new SKRect(f.Panel.Left, f.Panel.Bottom + 6, f.Panel.Right, f.Panel.Bottom + 110); Bounds = r;
        Recording!.Rects["hover"] = r; PanelBox(c, r); CursorGlyph(c, r.Left + 9, r.Top + 8, .6f);
        T(c, U(own ? "HoverOwn" : "HoverEnemy"), r.Left + 24, r.Top + 18, 10.5f, Muted, true);
        float x = r.Left + 9;
        if (own && unit.Kind == TacticalUnitKind.Drone)
        { PilotCardColumn.Portrait(c, portraits, unit.Build?.PilotId ?? "", PilotCardModel.Name(unit), SKRect.Create(r.Left + 9, r.Top + 25, 32, 32)); x = r.Left + 48; }
        T(c, PilotCardModel.Name(unit), x, r.Top + 38, 14, own ? Cyan : Orange, true);
        bool evading = TacticalBattleMapRenderer.IsEvading(f, unit.Id);
        bool decoy = TacticalBattleMapRenderer.HasDecoy(f, unit);
        bool marked = evading || decoy;
        string state = evading ? M("EvadeChip") : decoy ? M("DecoyChip") : PilotCardModel.State(unit); float w = Measure(state, 10.5f, marked) + 12;
        var chip = new SKRect(r.Right - 8 - w, r.Top + 25, r.Right - 8, r.Top + 41);
        Round(c, chip, evading ? PurpleDark : decoy ? GreenDark : Box, evading ? Purple : decoy ? Green : null);
        T(c, state, chip.MidX, chip.MidY + 4, 10.5f, evading ? Purple : decoy ? Green : Text, marked, align: SKTextAlign.Center);
        T(c, U("HullValue", unit.Hull, unit.MaxHull), x, r.Top + 56, 10.5f, Text);
        Bar(c, r.Left + 130, r.Top + 51, r.Width - 138, 5, unit.MaxHull > 0 ? unit.Hull / (double)unit.MaxHull : 0, Cyan);
        T(c, U("ArmorValue", unit.Armor, unit.MaxArmor), r.Left + 9, r.Top + 75, 10.5f, Text);
        Bar(c, r.Left + 130, r.Top + 70, r.Width - 138, 5, unit.MaxArmor > 0 ? unit.Armor / (double)unit.MaxArmor : 0, Purple);
        string details = unit.Build is { } b ? U(own ? "HoverOwnStats" : "HoverEnemyStats", b.WeaponName, b.DamageMin, b.DamageMax,
            own ? unit.Accuracy : Math.Round(b.Range), unit.Initiative?.ToString() ?? "—") : "—";
        T(c, details, r.MidX, r.Top + 96, FitSize(details, 10.5f, r.Width - 16), Muted, align: SKTextAlign.Center);
    }
}
