using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion.TacticalBattle;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

/// <summary>Presentation of an accepted movement; the accepted endpoint remains unmodified.</summary>
internal sealed class TacticalBattleMovementAnimation
{
    internal const long DurationMs = 1350;
    private readonly TacticalBattleSnapshot _before;
    private readonly ImmutableArray<TacticalMovementPath> _paths;
    private readonly long _startMs;

    private TacticalBattleMovementAnimation(TacticalBattleSnapshot before, ImmutableArray<TacticalMovementPath> paths, long startMs)
    {
        _before = before;
        _paths = paths;
        _startMs = startMs;
    }

    internal static TacticalBattleMovementAnimation? Create(TacticalBattleSnapshot? before, TacticalBattleResponse response, long nowMs)
    {
        if (before is not { Phase: TacticalBattlePhase.Move } || !response.Accepted || response.State is not { } after ||
            after.BattleId != before.BattleId || after.Revision <= before.Revision) return null;
        var paths = after.Paths.Where(p => p.Turn == before.Turn && p.Points.Length > 1).ToImmutableArray();
        return paths.IsEmpty ? null : new(before, paths, nowMs);
    }

    internal bool IsActive(long nowMs) => nowMs - _startMs < DurationMs;

    internal TacticalBattleSnapshot Interpolate(long nowMs)
    {
        double fraction = PyMath.Clamp((nowMs - _startMs) / (double)DurationMs, 0, 1);
        return _before with
        {
            Units = _before.Units.Select(u =>
            {
                var path = _paths.FirstOrDefault(p => p.DroneId == u.Id);
                return path is null ? u : u with { Pose = Sample(path.Points, fraction) };
            }).ToImmutableArray()
        };
    }

    private static TacticalPoseSnapshot Sample(ImmutableArray<TacticalPoseSnapshot> path, double fraction)
    {
        if (fraction <= 0) return path[0];
        double progress = fraction * (path.Length - 1);
        int index = (int)progress;
        if (index >= path.Length - 1) return path[^1];
        double f = progress - index;
        var a = path[index];
        if (f == 0) return a;
        var b = path[index + 1];
        return new(a.X + (b.X - a.X) * f, a.Y + (b.Y - a.Y) * f,
            TacticalGeometry.Norm(a.Course + TacticalGeometry.Norm(b.Course - a.Course) * f));
    }
}
