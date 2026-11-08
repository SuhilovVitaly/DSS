using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion.TacticalBattle;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed class TacticalBattleForecastTab
{
    internal sealed record Estimate(string Shooter, string Target, double Chance, bool Rear, int DamageMin, int DamageMax)
    {
        public double MeanDamage => ((double)DamageMin + DamageMax) * 0.5;
        public double ExpectedDamage => Chance * MeanDamage;
    }

    internal sealed record MovementEstimate(string Drone, ImmutableArray<Estimate> Outgoing, ImmutableArray<Estimate> Incoming);
    internal sealed record FireEstimate(ImmutableArray<Estimate> Outgoing, ImmutableArray<Estimate> Incoming);

    private readonly ScrollTextPanel _panel = new() { AutoScroll = false, StyleRole = "tabs.forecast" };
    private TacticalBattleSnapshot? _battle;
    private TacticalMovementFrame? _movement;
    private TacticalFirePlan? _firePlan;
    private (bool Results, bool Flying, int Arrived) _salvo;
    internal string Text => _panel.Text;

    internal void Draw(SKCanvas canvas, TacticalBattleFrame frame, SKRect bounds)
    {
        _panel.Layout(bounds, frame.UiScale);
        var salvo = frame.Salvo is { } f ? (true, f.InFlight, f.Flights.Count(HasArrived)) : default;
        if (!ReferenceEquals(frame.Battle, _battle) || !ReferenceEquals(frame.Movement, _movement) ||
            !ReferenceEquals(frame.FirePlan, _firePlan) || _salvo != salvo)
        {
            _battle = frame.Battle; _movement = frame.Movement; _firePlan = frame.FirePlan; _salvo = salvo;
            _panel.SetText(BuildText(frame));
        }
        _panel.Draw(canvas);
    }

    internal static ImmutableArray<MovementEstimate> PredictMovement(TacticalBattleSnapshot battle, TacticalMovementFrame movement)
    {
        var enemy = battle.Units.Where(u => u.Side != battle.PlayerSide && u.State is TacticalUnitState.Air or TacticalUnitState.Alive)
            .Select(u => (Unit: u, Pose: u.Kind == TacticalUnitKind.Carrier ? Pose(u.Pose) :
                TacticalGeometry.Integrate(Pose(u.Pose), u.Speed, 0, battle.Rules.Substeps))).ToArray();
        var h = battle.Rules.Hit;
        var rules = new TacticalHitRules(h.Base, h.MinChance, h.MaxChance, h.RangeBonus, h.AnglePenalty, h.RearBonus, h.RearBonusArc);
        var result = ImmutableArray.CreateBuilder<MovementEstimate>(movement.Orders.Length);
        foreach (var order in movement.Orders)
        {
            if (order.Path.IsDefaultOrEmpty) continue;
            var destination = Pose(order.Path[^1]);
            var outgoing = new List<Estimate>();
            var incoming = new List<Estimate>();
            foreach (var other in enemy)
            {
                if (Predict(order.Unit, destination, other.Unit, other.Pose) is { } shot) outgoing.Add(shot);
                if (Predict(other.Unit, other.Pose, order.Unit, destination) is { } risk) incoming.Add(risk);
            }
            result.Add(new(order.Unit.Id, outgoing.OrderByDescending(s => s.Chance).ToImmutableArray(),
                incoming.OrderByDescending(s => s.Chance).ToImmutableArray()));
        }
        return result.ToImmutable();

        Estimate? Predict(TacticalUnitSnapshot shooter, TacticalPose start, TacticalUnitSnapshot target, TacticalPose end)
        {
            if (shooter.Kind != TacticalUnitKind.Drone || shooter.State != TacticalUnitState.Air || shooter.Jammed ||
                shooter.Build is not { } gun) return null;
            double distance = TacticalGeometry.Dist(start.X, start.Y, end.X, end.Y);
            double angle = TacticalGeometry.Norm(TacticalGeometry.Bearing(start.X, start.Y, end.X, end.Y) - start.Course);
            if (distance > gun.Range || Math.Abs(angle) > gun.ArcDegrees) return null;
            var terms = TacticalHitChance.Terms(rules, new(shooter.Accuracy, target.Maneuver, gun.HitBonus, gun.Range, gun.ArcDegrees,
                distance, angle, TacticalHitChance.RearAngle(start, end), target.Kind == TacticalUnitKind.Drone,
                target.Build?.ShieldDefense ?? 0, target.Build?.ShieldCount ?? 0, battle.Rules.RearArc, gun.RangeBonus));
            return new(shooter.Id, target.Id, terms.Chance, terms.IsRear, gun.DamageMin, gun.DamageMax);
        }
    }

    internal static FireEstimate PredictFire(TacticalBattleSnapshot battle, TacticalFirePlan plan)
    {
        var outgoing = plan.Rows.Where(r => r.Target is not null && r.Drone.Build is not null)
            .Select(row =>
            {
                var target = row.Target!; var gun = row.Drone.Build!;
                return new Estimate(row.Drone.Id, target.TargetId, target.Chance, target.Rear, gun.DamageMin, gun.DamageMax);
            }).ToImmutableArray();
        var incoming = battle.Incoming.Select(shot =>
        {
            var gun = battle.Units.FirstOrDefault(u => u.Id == shot.ShooterId)?.Build;
            return new Estimate(shot.ShooterId, shot.TargetId, shot.Chance, shot.Rear, gun?.DamageMin ?? 0, gun?.DamageMax ?? 0);
        }).ToImmutableArray();
        return new(outgoing, incoming);
    }

    internal static string BuildText(TacticalBattleFrame frame)
    {
        var text = new StringBuilder();
        if (frame.Movement is { } movement)
        {
            text.AppendLine(L("Forecast.MoveNotice"));
            foreach (var row in PredictMovement(frame.Battle, movement))
            {
                text.AppendLine().AppendLine(row.Drone);
                ListShots(text, L("Forecast.InSector"), row.Outgoing, s => FormatChance(s.Target, s));
                ListShots(text, L("Forecast.UnderFire"), row.Incoming, s => FormatChance(s.Shooter, s));
            }
        }
        else if (frame.FirePlan is { } plan)
        {
            var estimate = PredictFire(frame.Battle, plan);
            text.AppendLine(L("Forecast.FireNotice"));
            text.AppendLine(L("FireConditional")).AppendLine().AppendLine(L("FireQueue"));
            var planned = estimate.Outgoing.Concat(estimate.Incoming).ToArray();
            if (frame.Battle.FireQueue.IsDefaultOrEmpty) text.AppendLine(L("FireNoTarget"));
            foreach (var entry in frame.Battle.FireQueue)
            {
                var shot = planned.FirstOrDefault(s => s.Shooter == entry.ShooterId);
                text.Append(BattleFormat.Number(entry.Index + 1)).Append(". ")
                    .Append(shot is null ? entry.ShooterId + " → " + L("FireNoTarget") : FormatFire(shot))
                    .Append(" · ").AppendLine(F("Initiative", BattleFormat.Number(entry.Initiative)));
            }
            text.AppendLine().Append(L("Forecast.Outgoing")).Append(" · ")
                .AppendLine(F("Forecast.Total", BattleFormat.Number(estimate.Outgoing.Sum(s => s.ExpectedDamage), 1)));
            text.Append(L("Forecast.Incoming")).Append(" · ")
                .AppendLine(F("Forecast.Total", BattleFormat.Number(estimate.Incoming.Sum(s => s.ExpectedDamage), 1)));
        }
        else if (frame.Salvo is { } salvo)
        {
            text.AppendLine(L("Phase.Results"));
            foreach (var flight in salvo.Flights)
            {
                var shot = flight.Shot;
                string result = salvo.InFlight && !HasArrived(flight) ? L("Forecast.InFlight") :
                    shot.Hit ? F("Forecast.Hit", BattleFormat.Number(shot.Damage)) : L("SalvoMiss");
                text.AppendLine(F("Forecast.ShotResult", shot.ShooterId, shot.TargetId, BattleFormat.Probability(shot.Chance), result));
            }
            text.AppendLine().AppendLine(L("Forecast.TargetResults"));
            if (salvo.InFlight && salvo.Damage.IsEmpty) text.AppendLine(BattleFormat.Text("…"));
            else if (salvo.Damage.IsEmpty) text.AppendLine(L("Forecast.None"));
            else foreach (var damage in salvo.Damage)
            {
                string delta = F("SalvoDamage", BattleFormat.Signed(-damage.Armor), BattleFormat.Signed(-damage.Hull));
                string state = damage.StateChanged ? " · " + L("UnitState." + damage.Unit.State) : "";
                text.AppendLine(F("Forecast.TargetDelta", damage.Unit.Id, delta + state));
            }
        }
        else if (frame.Pending) text.AppendLine(frame.Status.Length == 0 ? L("Waiting") : frame.Status);
        text.AppendLine().AppendLine(L("Forecast.Legend"));
        for (int i = 1; i <= 8; i++) text.AppendLine(F("Forecast.LegendLine", BattleFormat.Number(i), L("Forecast.Legend" + i)));
        return text.ToString().TrimEnd();
    }

    private static bool HasArrived(TacticalShotFlight shot) => shot.Arrived;

    private static void ListShots(StringBuilder text, string title, ImmutableArray<Estimate> rows, Func<Estimate, string> describe)
    {
        text.AppendLine(title);
        if (rows.IsEmpty) text.AppendLine(L("Forecast.None"));
        else foreach (var row in rows) text.AppendLine(describe(row));
    }

    private static string FormatChance(string unit, Estimate estimate) =>
        F("Forecast.Contact", unit, BattleFormat.Probability(estimate.Chance)) + (estimate.Rear ? " " + L("Forecast.Rear") : "");
    private static string FormatFire(Estimate estimate) =>
        F("Forecast.Shot", estimate.Shooter, estimate.Target, BattleFormat.Probability(estimate.Chance),
            BattleFormat.Number(estimate.DamageMin), BattleFormat.Number(estimate.DamageMax),
            BattleFormat.Number(estimate.ExpectedDamage, 1)) + (estimate.Rear ? " " + L("Forecast.Rear") : "");

    private static TacticalPose Pose(TacticalPoseSnapshot pose) => new(pose.X, pose.Y, pose.Course);
    private static string L(string key) => BattleFormat.Localize(key);
    private static string F(string key, params object[] args) => BattleFormat.Text(string.Format(CultureInfo.InvariantCulture, L(key), args));

    internal bool OnMouseDown(float x, float y, MouseButton button) => _panel.OnMouseDown(x, y, button);
    internal bool OnMouseMove(float x, float y) => _panel.OnMouseMove(x, y);
    internal void OnMouseUp(MouseButton button) => _panel.OnMouseUp(button);
    internal bool OnMouseWheel(float x, float y, float delta) => _panel.OnMouseWheel(x, y, delta);
    internal bool OnKeyDown(Key key) => _panel.OnKeyDown(key);
    internal void ResetInput() { _panel.ResetInput(); _panel.HasFocus = false; }
}
