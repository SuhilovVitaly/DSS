using DeepSpaceSaga.Contracts;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed class HoverCard
{
    public SKRect Bounds { get; private set; }
    public void Draw(SKCanvas c, TacticalBattleFrame f, PilotPortraitCatalog portraits)
    {
        Bounds = SKRect.Empty;
        var unit = f.Battle.Units.FirstOrDefault(u => u.Id == f.HoverUnitId); if (unit is null) return;
        bool own = unit.Side == f.Battle.PlayerSide;
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
