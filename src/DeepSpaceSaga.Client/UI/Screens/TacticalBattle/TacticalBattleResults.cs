using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion.TacticalBattle;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

public sealed record TacticalShotFlight(TacticalShotSnapshot Shot, int Index, double LaunchMs, double ElapsedMs)
{
    public double DurationMs { get; init; } = TacticalBattleResults.FlightMs;
    public bool Arrived => ElapsedMs >= LaunchMs + DurationMs;
    public bool Flying => ElapsedMs >= LaunchMs && !Arrived;
    public bool Flash => Arrived &&
        ElapsedMs < LaunchMs + DurationMs + TacticalBattleResults.FlashMs;
    public double Progress => PyMath.Clamp((ElapsedMs - LaunchMs) / DurationMs, 0, 1);
}

public sealed record TacticalSalvoDamage(TacticalUnitSnapshot Unit, int Armor, int Hull, bool StateChanged);

public sealed record TacticalSalvoFrame(bool InFlight, bool AutoFire, ImmutableArray<TacticalShotFlight> Flights,
    ImmutableArray<TacticalSalvoDamage> Damage)
{
    public int Hits => Flights.Count(f => f.Arrived && f.Shot.Hit);
}

internal sealed record TacticalBattleResults(TacticalBattleSnapshot Battle, ImmutableArray<TacticalShotSnapshot> Shots,
    ImmutableArray<TacticalSalvoDamage> Damage, long StartedMs, bool Skipped = false)
{
    internal const double InitialDelayMs = 300, NominalStepMs = 570, FlightMs = 975, FlashMs = 450, CapMs = 4500;

    internal static TacticalBattleResults? Create(TacticalBattleSnapshot? before, TacticalBattleResponse response, long startMs)
    {
        if (before is null || !response.Accepted || response.State is not { } state ||
            state.BattleId != before.BattleId || state.Revision <= before.Revision ||
            before.Phase != TacticalBattlePhase.Fire && !state.FireSkipped) return null;
        var shots = state.Shots.Where(s => s.Turn == before.Turn).ToImmutableArray();
        if (shots.IsEmpty || state.BeforeSalvoUnits.IsDefaultOrEmpty) return null;
        var damage = ImmutableArray.CreateBuilder<TacticalSalvoDamage>();
        foreach (var unit in state.Units)
        {
            var original = state.BeforeSalvoUnits.FirstOrDefault(u => u.Id == unit.Id);
            if (original is null) continue;
            int armor = original.Armor - unit.Armor, hull = original.Hull - unit.Hull;
            bool stateChanged = original.State != unit.State;
            if (armor > 0 || hull > 0 || stateChanged) damage.Add(new(unit, armor, hull, stateChanged));
        }
        return new(state, shots, damage.ToImmutable(), startMs) { BeforeLog = before.Log };
    }

    internal ImmutableArray<TacticalBattleLogEvent> BeforeLog { get; init; } = [];
    private int _cachedStep = int.MinValue;
    private TacticalBattleSnapshot? _cachedProjection;
    internal double DurationMs => Shots.IsEmpty ? 0 : PyMath.Min(FlightMs, (CapMs - InitialDelayMs) / Shots.Length);
    internal double LaunchInterval => DurationMs;
    internal double EndMs => Shots.IsEmpty ? 0 : InitialDelayMs + DurationMs * Shots.Length;
    internal bool InFlight(long nowMs) => !Skipped && nowMs - StartedMs < EndMs;

    internal TacticalBattleSnapshot Project(long nowMs)
    {
        if (!InFlight(nowMs)) return Battle;
        double elapsed = nowMs - StartedMs;
        int reached = -1, fired = 0;
        for (int i = 0; i < Battle.FireSteps.Length; i++)
        {
            if (Battle.FireSteps[i].Shot is not null) fired++;
            if (elapsed >= InitialDelayMs + fired * DurationMs) reached = i;
            else break;
        }
        if (_cachedProjection is not null && _cachedStep == reached) return _cachedProjection;
        var units = reached >= 0 ? Battle.FireSteps[reached].UnitsAfter : Battle.BeforeSalvoUnits;
        var outcome = Battle.FireSteps.Take(reached + 1).Select(step => step.Outcome).LastOrDefault(value => value is not null);
        _cachedStep = reached;
        _cachedProjection = Battle with
        {
            Turn = Shots.IsEmpty ? Battle.Turn : Shots[0].Turn, Phase = TacticalBattlePhase.Fire,
            Units = units, Suggestions = [], Targets = [], Incoming = [], Outcome = outcome,
            Losses = Battle.Losses.Select(loss => loss with { Count = units.Count(u => u.Side == loss.Side &&
                u.Kind == TacticalUnitKind.Drone && u.State is TacticalUnitState.Disabled or TacticalUnitState.Destroyed) }).ToImmutableArray(),
            Log = BeforeLog.AddRange(Battle.FireSteps.Take(reached + 1).SelectMany(step => step.Events))
        };
        return _cachedProjection;
    }

    internal TacticalSalvoFrame Capture(long nowMs)
    {
        double elapsed = Skipped ? EndMs + FlashMs : nowMs - StartedMs;
        var flights = Shots.Select((shot, i) => new TacticalShotFlight(shot, i, InitialDelayMs + i * DurationMs, elapsed)
            { DurationMs = DurationMs }).ToImmutableArray();
        var projection = Project(nowMs);
        var damage = ImmutableArray.CreateBuilder<TacticalSalvoDamage>();
        foreach (var unit in projection.Units)
        {
            var before = Battle.BeforeSalvoUnits.FirstOrDefault(u => u.Id == unit.Id);
            if (before is null) continue;
            int armor = before.Armor - unit.Armor, hull = before.Hull - unit.Hull;
            if (armor > 0 || hull > 0 || before.State != unit.State)
                damage.Add(new(unit, armor, hull, before.State != unit.State));
        }
        return new(InFlight(nowMs), Battle.FireSkipped, flights, damage.ToImmutable());
    }

    internal static SKPoint Endpoint(SKPoint from, SKPoint to, bool hit, int index, float scale)
    {
        if (hit) return to;
        double dx = to.X - from.X, dy = to.Y - from.Y;
        double length = TacticalGeometry.Hypot(dx, dy);
        if (length == 0) { dx = 1; length = 1; }
        double ux = dx / (double)length, uy = dy / (double)length;
        float side = (index % 2 == 0 ? 13 : -13) * scale;
        float beyond = 28 * scale;
        return new(to.X + (float)(ux * beyond - uy * side), to.Y + (float)(uy * beyond + ux * side));
    }
}
