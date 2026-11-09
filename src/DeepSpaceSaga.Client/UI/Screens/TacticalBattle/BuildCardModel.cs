using System.Globalization;
using DeepSpaceSaga.Contracts;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed record BuildSlotCell(TacticalSlotKind Kind, int Index, string Name, string Detail,
    bool Empty, bool NoSlot, bool Alert);

/// <summary>Presentation only: slot identity/state come exclusively from captured Slots.</summary>
internal sealed record BuildCardModel(TacticalUnitSnapshot Unit, bool Hovered, IReadOnlyList<BuildSlotCell> Cells,
    string Heading, string Frame, string Mass, string Motion, string Legend)
{
    internal static string Number(double value, int precision = 1) => Math.Round(value, precision, MidpointRounding.ToEven)
        .ToString(precision == 0 ? "0" : precision == 1 ? "0.#" : "0.##", CultureInfo.InvariantCulture)
        .Replace(".", M("Build.DecimalSeparator"), StringComparison.Ordinal);

    internal static string Fixed(double value) => Math.Round(value, 1, MidpointRounding.ToEven).ToString("F1", CultureInfo.InvariantCulture)
        .Replace(".", M("Build.DecimalSeparator"), StringComparison.Ordinal);

    internal static string Ellipsize(string text, float width, float size = 9.5f, bool bold = true)
    {
        if (Measure(text, size, bold) <= width) return text;
        while (text.Length > 0 && Measure(text + "…", size, bold) > width) text = text[..^1];
        return text + "…";
    }

    internal static BuildCardModel? From(TacticalBattleFrame f)
    {
        if (f.Salvo is null && f.Battle.Phase == TacticalBattlePhase.Over) return null;
        // Foreign units, carriers and anomaly hover retain their dedicated cards.
        if (f.HoverUnitId is null && AnomalyPresentation.HoverKind(f) is not null) return null;
        string? selected = f.Salvo is { InFlight: true } ? PilotCardModel.ActiveShooter(f)
            : f.Salvo is not null ? PilotCardModel.ResultShot(f)?.ShooterId : f.SelectedDroneId;
        if (!f.Battle.Units.Any(u => u.Id == selected && Own(f.Battle, u))) selected = f.LastSelectedDroneId;
        return From(f.Battle, f.HoverUnitId, selected, allowTerminal: f.Salvo is not null);
    }

    public static BuildCardModel? From(TacticalBattleSnapshot battle, string? hoveredId, string? selectedId) =>
        From(battle, hoveredId, selectedId, false);

    private static bool Own(TacticalBattleSnapshot battle, TacticalUnitSnapshot u) => u.Side == battle.PlayerSide && u.Kind == TacticalUnitKind.Drone;
    private static BuildCardModel? From(TacticalBattleSnapshot battle, string? hoveredId, string? selectedId, bool allowTerminal)
    {
        if (battle.Phase == TacticalBattlePhase.Over && !allowTerminal) return null;
        var hovered = battle.Units.FirstOrDefault(u => u.Id == hoveredId);
        if (hovered is not null && !Own(battle, hovered)) return null;
        var u = hovered ?? battle.Units.FirstOrDefault(u => u.Id == selectedId && Own(battle, u));
        if (u is null) return null;
        var cells = new List<BuildSlotCell>();
        void Cell(TacticalSlotKind kind, int index, float width)
        {
            var slot = u.Slots.FirstOrDefault(s => s.Kind == kind && s.Index == index);
            bool noSlot = slot is null, empty = slot?.ModuleId is null;
            string detail = "", name = slot?.ModuleName ?? "";
            if (empty) name = noSlot ? M("Build.NoSlot") : M("Build.Empty", M("Build.Slot" + kind));
            else if (slot!.State == TacticalSlotState.Jammed) detail = M("Build.Jammed");
            else if (slot.State == TacticalSlotState.Damaged) detail = M("Build.Damaged", Number(slot.Factor, 2));
            else if (u.Build is { } b) detail = kind switch
            {
                TacticalSlotKind.Weapon => M("Build.Weapon", b.DamageMin, b.DamageMax, Number(b.Range, 0), Number(b.ArcDegrees, 0)),
                TacticalSlotKind.Armor => M("Build.Armor", u.Armor, u.MaxArmor),
                TacticalSlotKind.Shield => M("Build.Shield", b.ShieldDefense),
                TacticalSlotKind.Module => M("Build.Module", u.DecoyCharges ?? 0, b.DecoyMaxCharges),
                TacticalSlotKind.Engine when index < u.Engines.Length => M("Build.Engine", Number(u.Engines[index].SpeedBonus, 0), Number(u.Engines[index].SpeedChange, 0), Number(u.Engines[index].Turn)),
                _ => ""
            };
            cells.Add(new(kind, index, empty ? name : Ellipsize(name, width - 12), detail, empty, noSlot,
                slot?.State is TacticalSlotState.Jammed or TacticalSlotState.Damaged));
        }
        Cell(TacticalSlotKind.Weapon, 0, 124); Cell(TacticalSlotKind.Armor, 0, 140);
        Cell(TacticalSlotKind.Shield, 0, 64); Cell(TacticalSlotKind.Shield, 1, 64); Cell(TacticalSlotKind.Module, 0, 78);
        int engines = u.Slots.Count(s => s.Kind == TacticalSlotKind.Engine);
        for (int i = 0; i < Math.Max(1, engines); i++) Cell(TacticalSlotKind.Engine, i, engines <= 1 ? 104 : 95);
        var limits = u.Limits;
        return new(u, hovered is not null, cells, M(hovered is null ? "Build.Title" : "Build.HoverTitle"),
            M("Build.Frame", u.Build?.FrameName ?? u.FrameId),
            M("Build.Mass", Number(limits?.Mass ?? 0, 0), Number(u.Build?.ReferenceMass ?? 0, 0)),
            M("Build.Motion", Fixed(limits?.MinSpeed ?? 0), Fixed(limits?.MaxSpeed ?? 0), Fixed(limits?.Change ?? 0), Number(limits?.Turn ?? 0)),
            M("Build.EngineLegend"));
    }
}
