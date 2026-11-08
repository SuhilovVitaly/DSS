using DeepSpaceSaga.Contracts;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed record PilotStat(string Kind, string Label, string Value, SKColor Tone, double Fill);
internal sealed record PilotSegment(string Text, SKColor Tone, bool Bold = false);
internal sealed record PilotCard(string UnitId, string Name, string Weapon, string PilotId, bool Selected,
    IReadOnlyList<PilotStat> Stats, string Chip, bool ChipAccent, IReadOnlyList<PilotSegment> Segments)
{
    public string PhaseText => string.Concat(Segments.Select(s => s.Text));
}

internal static class PilotCardModel
{
    internal static string Name(TacticalUnitSnapshot unit) => unit.Build?.PilotName ?? unit.Id;
    internal static string State(TacticalUnitSnapshot unit, bool female = false) => U(unit.State switch
    { TacticalUnitState.Destroyed => female ? "DestroyedFemale" : "DestroyedMale", TacticalUnitState.Disabled => female ? "DisabledFemale" : "DisabledMale", _ => "Alive" });
    internal static string? ActiveShooter(TacticalBattleFrame f) => f.Salvo?.Flights.FirstOrDefault(s => s.Flying)?.Shot.ShooterId ?? f.Salvo?.Flights.FirstOrDefault(s => !s.Arrived)?.Shot.ShooterId ?? f.Salvo?.Flights.LastOrDefault()?.Shot.ShooterId;
    internal static TacticalShotSnapshot? ResultShot(TacticalBattleFrame f) => f.Salvo?.Flights.FirstOrDefault(s => f.Battle.Units.Any(u => u.Id == s.Shot.ShooterId && u.Side == f.Battle.PlayerSide))?.Shot ?? f.Salvo?.Flights.FirstOrDefault()?.Shot;
    internal static int Dealt(TacticalBattleSnapshot b, string shooter, int? turn = null)
    {
        int total = 0;
        TacticalBattleLogEvent? hit = null;
        foreach (var e in b.Log)
        {
            if (e.Kind == TacticalBattleLogKind.ShotHit) hit = e;
            else if (e.Kind == TacticalBattleLogKind.DamageApplied && hit?.ActorId == shooter && hit.Turn == e.Turn && hit.TargetId == e.TargetId && (turn is null || e.Turn == turn))
                total += (e.Damage?.Hull ?? 0) + (e.Damage?.Armor ?? 0);
        }
        return total;
    }
    public static IReadOnlyList<PilotCard> Project(TacticalBattleFrame f, PilotPortraitCatalog? portraits = null)
    {
        portraits ??= PilotPortraitCatalog.Default;
        var cards = new List<PilotCard>();
        var own = f.Battle.Units.Where(u => u.Kind == TacticalUnitKind.Drone && u.Side == f.Battle.PlayerSide).ToArray();
        foreach (var u in own)
        {
            string pilot = u.Build?.PilotId ?? "", chip = "";
            bool accent = false;
            string? selected = f.SelectedDroneId;
            var segments = new List<PilotSegment>();
            void Template(string key, params (string Value, SKColor Tone)[] values)
            {
                string template = U(key);
                for (int i = 0; i < values.Length; i++)
                {
                    string token = "{" + i + "}"; int index = template.IndexOf(token, StringComparison.Ordinal);
                    if (index < 0) continue;
                    segments.Add(new(template[..index], Muted)); segments.Add(new(values[i].Value, values[i].Tone, true)); template = template[(index + token.Length)..];
                }
                segments.Add(new(template, Muted));
            }
            if (f.Salvo is { } salvo)
            {
                var shot = salvo.Flights.FirstOrDefault(s => s.Shot.ShooterId == u.Id)?.Shot;
                if (salvo.InFlight)
                {
                    selected = ActiveShooter(f);
                    bool active = selected == u.Id;
                    int index = Array.FindIndex(f.Battle.FireQueue.ToArray(), q => q.ShooterId == u.Id) + 1;
                    chip = U(shot is null ? "NoTarget" : active ? "Firing" : "Waiting"); accent = active;
                    Template("SalvoStats", ($"{index}/{f.Battle.FireQueue.Length}", active ? Yellow : Text), (shot?.TargetId ?? "—", active ? Yellow : Muted));
                }
                else
                {
                    selected = ResultShot(f)?.ShooterId;
                    int received = salvo.Damage.FirstOrDefault(d => d.Unit.Id == u.Id) is { } d ? d.Armor + d.Hull : 0;
                    chip = U(shot is null ? portraits.IsFemale(pilot) ? "NotFiredFemale" : "NotFiredMale" : shot.Hit ? "HitChip" : "MissChip");
                    accent = received > 0;
                    Template("ResultStats", (shot is null ? "—" : shot.Hit ? Dealt(f.Battle, u.Id, shot.Turn).ToString() : "0", shot?.Hit == true ? Orange : Muted), (received > 0 ? "−" + received : "0", received > 0 ? Red : Muted));
                }
            }
            else if (f.Battle.Phase == TacticalBattlePhase.Over)
            {
                selected = null;
                int dealt = Dealt(f.Battle, u.Id), received = u.MaxArmor - u.Armor + u.MaxHull - u.Hull;
                chip = State(u, portraits.IsFemale(pilot)).ToLowerInvariant(); accent = u.State is TacticalUnitState.Disabled or TacticalUnitState.Destroyed;
                Template("ResultStats", (dealt.ToString(), dealt > 0 ? Orange : Muted), (received > 0 ? "−" + received : "0", received > 0 ? Red : Muted));
            }
            else if (f.Movement?.Orders.FirstOrDefault(o => o.Unit.Id == u.Id) is { } move)
            {
                chip = move.Order == move.Suggestion ? U("Ai") : "";
                Template("MoveStats", (F(move.Order.Speed), Text), (Deg(move.Order.Turn), Text));
            }
            else
            {
                var row = f.FirePlan?.Rows.FirstOrDefault(r => r.Drone.Id == u.Id);
                bool has = row?.Target is not null;
                chip = has ? row!.TargetId + " " + Pct(row.Target!.Chance) : U("NoTargets"); accent = has;
                Template("FireStats", (has ? "1" : "0", has ? Yellow : Muted), (has ? $"{u.Build?.DamageMin}–{u.Build?.DamageMax}" : "—", has ? Orange : Muted), (u.Initiative?.ToString() ?? "—", Text));
            }
            cards.Add(new(u.Id, Name(u), u.Build?.WeaponName ?? "", pilot, selected == u.Id,
                [new("armor", U("Armor"), $"{u.Armor}/{u.MaxArmor}", Purple, u.MaxArmor > 0 ? u.Armor / (double)u.MaxArmor : 0),
                 new("hull", U("Hull"), $"{u.Hull}/{u.MaxHull}", Cyan, u.MaxHull > 0 ? u.Hull / (double)u.MaxHull : 0),
                 new("mind", U("Mind"), "100", Green, 1)], chip, accent, segments));
        }
        return cards;
    }
}
