using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion.TacticalBattle;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

public enum DecoyRowState { Unavailable = 0, Available = 1, Selected = 2 }

public sealed record TacticalFirePlanRow(TacticalUnitSnapshot Drone, TacticalTargetGroupSnapshot Group, string? TargetId)
{
    public bool Evade { get; init; }
    public bool Decoy { get; init; }
    public bool CanDecoy => !Evade && Drone.Build?.DecoyId is not null && Drone.DecoyCharges > 0;
    public DecoyRowState DecoyState => Decoy ? DecoyRowState.Selected : CanDecoy ? DecoyRowState.Available : DecoyRowState.Unavailable;
    public bool CanEvade { get; init; }
    public TacticalTargetCandidateSnapshot? Target => Group.List.FirstOrDefault(c => c.TargetId == TargetId);
}

public sealed record TacticalFirePlan(ImmutableArray<TacticalFirePlanRow> Rows, string? SelectedDroneId)
{
    public TacticalFirePlanRow? Selected => Rows.FirstOrDefault(r => r.Drone.Id == SelectedDroneId);
}

internal sealed class TacticalBattleFireOrders
{
    private readonly List<TacticalFirePlanRow> _rows = [];
    private TacticalBattleSnapshot? _state;
    private TacticalFirePlan? _cached;
    public string? Selected { get; private set; }

    public void Observe(TacticalBattleSnapshot state, string? preferred)
    {
        if (ReferenceEquals(state, _state)) return;
        var before = _state;
        _state = state;
        if (before?.BattleId == state.BattleId && before.Turn == state.Turn && before.Phase == state.Phase) return;
        _rows.Clear();
        _cached = null;
        Selected = null;
        if (state.Phase != TacticalBattlePhase.Fire) return;
        foreach (var unit in state.Units)
        {
            if (unit.Side != state.PlayerSide || unit.Kind != TacticalUnitKind.Drone || unit.State != TacticalUnitState.Air) continue;
            var group = state.Targets.FirstOrDefault(g => g.DroneId == unit.Id) ?? new(unit.Id, [], null);
            if (unit.Jammed || unit.InDust) group = group with { List = [], DefaultTargetId = null };
            _rows.Add(new(unit, group, group.DefaultTargetId)
            { CanEvade = state.Rules.Evasion is { } rules && (unit.Consciousness ?? rules.MaxConsciousness) >= rules.MinConsciousness });
        }
        Selected = _rows.FirstOrDefault(r => r.TargetId is not null)?.Drone.Id ?? _rows.FirstOrDefault()?.Drone.Id;
    }

    public bool Select(string id)
    {
        if (!_rows.Any(r => r.Drone.Id == id)) return false;
        if (Selected != id) { Selected = id; _cached = null; }
        return true;
    }

    public void Cycle(bool backwards)
    {
        if (_rows.Count == 0) return;
        int index = _rows.FindIndex(r => r.Drone.Id == Selected);
        Select(_rows[(index + _rows.Count + (backwards ? -1 : 1)) % _rows.Count].Drone.Id);
    }

    public bool Choose(string? target, bool toggle = false)
    {
        int index = _rows.FindIndex(r => r.Drone.Id == Selected);
        if (index < 0) return false;
        var row = _rows[index];
        if (target is not null && !row.Group.List.Any(c => c.TargetId == target)) return false;
        string? actual = toggle && row.TargetId == target ? null : target;
        if (row.TargetId != actual || row.Evade)
        {
            _rows[index] = row with { TargetId = actual, Evade = false };
            _cached = null;
        }
        return true;
    }

    public void ClearAll()
    {
        for (int i = 0; i < _rows.Count; i++) _rows[i] = _rows[i] with { TargetId = null, Evade = false, Decoy = false };
        _cached = null;
    }

    public bool ToggleEvade(string droneId)
    {
        int index = _rows.FindIndex(r => r.Drone.Id == droneId);
        if (index < 0 || !_rows[index].CanEvade) return false;
        _rows[index] = _rows[index] with { TargetId = null, Evade = !_rows[index].Evade, Decoy = false };
        _cached = null;
        return true;
    }

    public bool ToggleDecoy(string droneId)
    {
        int index = _rows.FindIndex(r => r.Drone.Id == droneId);
        if (index < 0 || !_rows[index].CanDecoy) return false;
        _rows[index] = _rows[index] with { Decoy = !_rows[index].Decoy };
        _cached = null;
        return true;
    }

    public TacticalFirePlan Capture() => _cached ??= new(_rows.ToImmutableArray(), Selected);
    public ImmutableArray<TacticalFireTarget> BuildTargets() => _rows.Select(r => new TacticalFireTarget(r.Drone.Id, r.TargetId, r.Evade, r.Decoy)).ToImmutableArray();

    public bool ChooseAt(TacticalBattleView view, SKPoint pointer, float uiScale)
    {
        if (_state is not { } state || Capture().Selected is not { } selected) return false;
        string? hit = Hit(selected.Group.List.Select(c => c.TargetId), 14 * uiScale);
        if (hit is not null) return Choose(hit);
        hit = Hit(_rows.Select(r => r.Drone.Id), 12 * uiScale);
        return hit is not null && Select(hit);

        string? Hit(IEnumerable<string> ids, float radius)
        {
            double best = double.PositiveInfinity;
            string? nearest = null;
            foreach (var id in ids)
            {
                var unit = state.Units.FirstOrDefault(u => u.Id == id);
                if (unit is null) continue;
                var p = TacticalBattleMapRenderer.MarkerPoint(view, unit);
                double distance = TacticalGeometry.Dist(pointer.X, pointer.Y, p.X, p.Y);
                if (distance > radius || distance >= best) continue;
                nearest = id; best = distance;
            }
            return nearest;
        }
    }
}
