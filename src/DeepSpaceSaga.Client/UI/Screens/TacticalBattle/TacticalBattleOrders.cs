using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion.TacticalBattle;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

public sealed record TacticalOrderPreview(TacticalUnitSnapshot Unit, TacticalDroneOrder Order, TacticalDroneOrder Suggestion,
    bool Touched, double MinimumSpeed, double MaximumSpeed, double MaximumTurn, double TurnAtMinimum,
    ImmutableArray<TacticalPoseSnapshot> Path);

public sealed record TacticalMovementFrame(ImmutableArray<TacticalOrderPreview> Orders, string? Selected,
    ImmutableArray<TacticalPoseSnapshot> Reachable)
{
    public TacticalOrderPreview? SelectedOrder => Orders.FirstOrDefault(o => o.Unit.Id == Selected);
}

internal sealed class TacticalBattleOrders
{
    private const double TieTolerance = 0.000001;
    private readonly List<Entry> _entries = [];
    private TacticalBattleSnapshot? _state;
    private TacticalMovementFrame? _cached;
    private Entry? _selected;
    internal string? Selected => _selected?.Unit.Id;

    private sealed class Entry(TacticalUnitSnapshot unit, TacticalDroneOrder suggestion)
    {
        public TacticalUnitSnapshot Unit { get; } = unit;
        public TacticalDroneOrder Suggestion { get; } = suggestion;
        public TacticalDroneOrder Order = suggestion;
        public bool Touched;
        public TacticalLimits Limits { get; } = ToLimits(unit.Limits!);
        public ImmutableArray<AimCandidate> Candidates;
        public ImmutableArray<TacticalPoseSnapshot> Boundary;
        public ImmutableArray<TacticalPoseSnapshot> Path;
        public double MinimumSpeed => Limits.ClampSpeed(Limits.MinSpeed, Unit.Speed);
        public double MaximumSpeed => Limits.ClampSpeed(Limits.MaxSpeed, Unit.Speed);
    }

    private readonly record struct AimCandidate(double Speed, double Turn, TacticalPoseSnapshot End);

    public void Observe(TacticalBattleSnapshot state)
    {
        if (ReferenceEquals(_state, state)) return;
        var previous = _state;
        _state = state;
        if (previous?.BattleId == state.BattleId && previous.Turn == state.Turn && previous.Phase == state.Phase)
            return; // e.g. stale-order refusal with a fresh copy of the same phase
        var previousSelected = Selected;
        _entries.Clear();
        _selected = null;
        _cached = null;
        if (state.Phase != TacticalBattlePhase.Move) return;
        foreach (var unit in state.Units)
        {
            if (unit.Kind != TacticalUnitKind.Drone || unit.State != TacticalUnitState.Air ||
                unit.Side != state.PlayerSide || unit.Limits is null) continue;
            var suggestion = state.Suggestions.FirstOrDefault(s => s.DroneId == unit.Id);
            if (suggestion is not null) _entries.Add(new(unit, suggestion));
        }
        _selected = _entries.FirstOrDefault(e => e.Unit.Id == previousSelected) ?? _entries.FirstOrDefault();
    }

    public ImmutableArray<TacticalDroneOrder> EditedOrders() =>
        _entries.Where(e => e.Touched).Select(e => e.Order).ToImmutableArray();

    public bool Select(string id)
    {
        var next = _entries.FirstOrDefault(e => e.Unit.Id == id);
        if (next is null) return false;
        if (next != _selected) { _selected = next; _cached = null; }
        return true;
    }

    public bool Cycle(bool backwards)
    {
        if (_selected is null || _entries.Count == 0) return false;
        int current = _entries.IndexOf(_selected);
        int index = (current + _entries.Count + (backwards ? -1 : 1)) % _entries.Count;
        return Select(_entries[index].Unit.Id);
    }

    public bool SetSpeed(double value) => _selected is { } entry && Edit(entry, value, entry.Order.Turn);
    public bool SetTurn(double value) => _selected is { } entry && Edit(entry, entry.Order.Speed, value);
    public bool Adjust(double speedDelta, double turnDelta) => _selected is { } entry &&
        Edit(entry, entry.Order.Speed + speedDelta, entry.Order.Turn + turnDelta);
    public bool Straight() => _selected is { } entry && Edit(entry, entry.Unit.Speed, 0);
    public bool Suggest() => _selected is { } entry && Replace(entry, entry.Suggestion, true);
    public void ResetAll()
    {
        foreach (var entry in _entries) Replace(entry, entry.Suggestion, false);
    }

    private bool Edit(Entry entry, double speed, double turn)
    {
        speed = entry.Limits.ClampSpeed(speed, entry.Unit.Speed);
        double limit = TurnLimit(entry, speed);
        return Replace(entry, new(entry.Unit.Id, speed, PyMath.Clamp(turn, -limit, limit)), true);
    }

    private bool Replace(Entry entry, TacticalDroneOrder order, bool touched)
    {
        if (entry.Order != order) entry.Path = default;
        if (entry.Order != order || entry.Touched != touched) _cached = null;
        entry.Order = order;
        entry.Touched = touched;
        return true;
    }

    public bool AimAt(double x, double y)
    {
        if (_selected is not { } entry || !double.IsFinite(x) || !double.IsFinite(y)) return false;
        EnsureCandidates(entry);
        double best = double.PositiveInfinity;
        AimCandidate selected = default;
        foreach (var candidate in entry.Candidates)
        {
            double distance = TacticalGeometry.Dist(candidate.End.X, candidate.End.Y, x, y);
            if (distance >= best - TieTolerance) continue;
            best = distance;
            selected = candidate;
        }
        if (!double.IsFinite(best)) return false;
        return Edit(entry, selected.Speed, selected.Turn);
    }

    public void AdvanceToNearestUntouched()
    {
        if (_selected is not { } current) return;
        Entry? nearest = null;
        double distance = double.PositiveInfinity;
        foreach (var entry in _entries)
        {
            if (entry.Touched || entry == current) continue;
            var a = current.Unit.Pose; var b = entry.Unit.Pose;
            double candidate = TacticalGeometry.Dist(a.X, a.Y, b.X, b.Y);
            if (candidate >= distance) continue;
            nearest = entry;
            distance = candidate;
        }
        if (nearest is not null) Select(nearest.Unit.Id);
    }

    public TacticalMovementFrame Capture()
    {
        if (_cached is { } cached) return cached;
        var rows = ImmutableArray.CreateBuilder<TacticalOrderPreview>(_entries.Count);
        foreach (var e in _entries)
        {
            if (e.Path.IsDefault)
                e.Path = TacticalGeometry.Path(Pose(e.Unit), e.Order.Speed, e.Order.Turn, _state!.Rules.Substeps)
                    .Select(Convert).ToImmutableArray();
            rows.Add(new(e.Unit, e.Order, e.Suggestion, e.Touched, e.MinimumSpeed, e.MaximumSpeed,
                TurnLimit(e, e.Order.Speed), TurnLimit(e, e.MinimumSpeed), e.Path));
        }
        if (_selected is { } selected) EnsureCandidates(selected);
        return _cached = new(rows.MoveToImmutable(), Selected, _selected?.Boundary ?? []);
    }

    public string? Hit(TacticalBattleView view, SKPoint p, float radius, bool endpoint)
    {
        string? nearest = null;
        double best = double.PositiveInfinity;
        foreach (var row in Capture().Orders)
        {
            if (endpoint && row.Path.IsDefaultOrEmpty) continue;
            var target = view.Project(endpoint ? row.Path[^1] : row.Unit.Pose);
            double distance = TacticalGeometry.Dist(p.X, p.Y, target.X, target.Y);
            if (distance > radius || distance >= best) continue;
            nearest = row.Unit.Id; best = distance;
        }
        return nearest;
    }

    private void EnsureCandidates(Entry entry)
    {
        if (!entry.Candidates.IsDefault) return;
        var choices = ImmutableArray.CreateBuilder<AimCandidate>(13 * 61);
        var boundary = ImmutableArray.CreateBuilder<TacticalPoseSnapshot>(144);
        var trailing = new List<TacticalPoseSnapshot>(12);
        var start = Pose(entry.Unit);
        double low = entry.MinimumSpeed, high = entry.MaximumSpeed;
        for (int speedIndex = 0; speedIndex < 13; speedIndex++)
        {
            double speed = low + (high - low) * (speedIndex / 12.0);
            double limit = TurnLimit(entry, speed);
            for (int turnIndex = 0; turnIndex < 61; turnIndex++)
            {
                double turn = -limit + 2 * limit * (turnIndex / 60.0);
                var end = Convert(TacticalGeometry.Integrate(start, speed, turn, _state!.Rules.Substeps));
                choices.Add(new(speed, turn, end));
                if (speedIndex == 0 || turnIndex == 60 && speedIndex < 12) boundary.Add(end);
                if (speedIndex > 0 && speedIndex < 12 && turnIndex == 0) trailing.Add(end);
            }
        }
        // Low-speed arc, right edge, reversed high-speed arc, then left edge.
        int finalRow = 12 * 61;
        for (int j = 60; j >= 0; j--) boundary.Add(choices[finalRow + j].End);
        for (int j = trailing.Count - 1; j >= 0; j--) boundary.Add(trailing[j]);
        entry.Candidates = choices.MoveToImmutable();
        entry.Boundary = boundary.ToImmutable();
    }

    private double TurnLimit(Entry e, double speed) => e.Limits.TurnLimit(speed, _state!.Rules.TurnAtMaxSpeedFraction);
    private static TacticalLimits ToLimits(TacticalMovementLimits l) => new(l.Mass, l.MassFactor, l.MinSpeed, l.MaxSpeed, l.Change, l.Turn);
    private static TacticalPose Pose(TacticalUnitSnapshot unit) => new(unit.Pose.X, unit.Pose.Y, unit.Pose.Course);
    private static TacticalPoseSnapshot Convert(TacticalPose p) => new(p.X, p.Y, p.Course);
}
