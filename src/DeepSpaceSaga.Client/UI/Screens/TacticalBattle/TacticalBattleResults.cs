using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion.TacticalBattle;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

public sealed record TacticalShotFlight(TacticalShotSnapshot Shot, int Index, double LaunchMs, double ElapsedMs)
{
    public bool Flying => ElapsedMs >= LaunchMs && ElapsedMs < LaunchMs + TacticalBattleResults.FlightMs;
    public bool Flash => ElapsedMs >= LaunchMs + TacticalBattleResults.FlightMs &&
        ElapsedMs < LaunchMs + TacticalBattleResults.FlightMs + TacticalBattleResults.FlashMs;
    public double Progress => PyMath.Clamp((ElapsedMs - LaunchMs) / (double)TacticalBattleResults.FlightMs, 0, 1);
}

public sealed record TacticalSalvoDamage(TacticalUnitSnapshot Unit, int Armor, int Hull, bool StateChanged);

public sealed record TacticalSalvoFrame(bool InFlight, bool AutoFire, ImmutableArray<TacticalShotFlight> Flights,
    ImmutableArray<TacticalSalvoDamage> Damage)
{
    public int Hits => Flights.Count(f => f.Shot.Hit);
}

internal sealed record TacticalBattleResults(TacticalBattleSnapshot Battle, ImmutableArray<TacticalShotSnapshot> Shots,
    ImmutableArray<TacticalSalvoDamage> Damage, long StartedMs, bool Skipped = false)
{
    internal const double InitialDelayMs = 300, NominalStepMs = 380, FlightMs = 650, FlashMs = 450, CapMs = 3000;

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
        return new(state, shots, damage.ToImmutable(), startMs);
    }

    internal double LaunchInterval => Shots.Length <= 1 ? 0 :
        PyMath.Min(NominalStepMs, (CapMs - InitialDelayMs - FlightMs) / (double)(Shots.Length - 1));
    internal double EndMs => InitialDelayMs + LaunchInterval * (Shots.Length - 1) + FlightMs;
    internal bool InFlight(long nowMs) => !Skipped && nowMs - StartedMs < EndMs;

    internal TacticalSalvoFrame Capture(long nowMs)
    {
        double elapsed = Skipped ? EndMs + FlashMs : nowMs - StartedMs;
        var shots = ImmutableArray.CreateBuilder<TacticalShotFlight>(Shots.Length);
        double interval = LaunchInterval;
        for (int i = 0; i < Shots.Length; i++)
            shots.Add(new(Shots[i], i, InitialDelayMs + i * interval, elapsed));
        bool flight = InFlight(nowMs);
        return new(flight, Battle.FireSkipped, shots.MoveToImmutable(), flight ? [] : Damage);
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
