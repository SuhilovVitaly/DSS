using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion.TacticalBattle;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

/// <summary>Presentation of an accepted movement; the accepted endpoint remains unmodified.</summary>
internal sealed class TacticalBattleMovementAnimation
{
    internal const long DurationMs = 1350;
    internal const long FireTransitionDurationMs = 1929; // 1350 / 0.70, rounded to the nearest millisecond.
    internal long PlaybackDurationMs { get; }
    private readonly TacticalBattleSnapshot _before;
    private readonly ImmutableArray<TacticalMovementPath> _paths;
    private readonly long _startMs;

    private TacticalBattleMovementAnimation(TacticalBattleSnapshot before, ImmutableArray<TacticalMovementPath> paths,
        long startMs, long durationMs)
    {
        _before = before;
        _paths = paths;
        _startMs = startMs;
        PlaybackDurationMs = durationMs;
    }

    internal static TacticalBattleMovementAnimation? Create(TacticalBattleSnapshot? before, TacticalBattleResponse response, long nowMs)
    {
        if (before is not { Phase: TacticalBattlePhase.Move } || !response.Accepted || response.State is not { } after ||
            after.BattleId != before.BattleId || after.Revision <= before.Revision) return null;
        var paths = after.Paths.Where(p => p.Turn == before.Turn && p.Points.Length > 1).ToImmutableArray();
        bool leadsToFire = after.Phase == TacticalBattlePhase.Fire ||
            after.FireSkipped && after.Shots.Any(shot => shot.Turn == before.Turn);
        return paths.IsEmpty ? null : new(before, paths, nowMs, leadsToFire ? FireTransitionDurationMs : DurationMs);
    }

    internal bool IsActive(long nowMs) => nowMs - _startMs < PlaybackDurationMs;

    internal TacticalBattleSnapshot Interpolate(long nowMs)
    {
        double fraction = PyMath.Clamp((nowMs - _startMs) / (double)PlaybackDurationMs, 0, 1);
        var trails = _paths.Select(path => path with { Points = Traversed(path.Points, fraction) }).ToImmutableArray();
        return _before with
        {
            Paths = trails,
            Units = _before.Units.Select(u =>
            {
                var trail = trails.FirstOrDefault(p => p.DroneId == u.Id);
                return trail is null ? u : u with { Pose = trail.Points[^1] };
            }).ToImmutableArray()
        };
    }

    private static ImmutableArray<TacticalPoseSnapshot> Traversed(ImmutableArray<TacticalPoseSnapshot> path, double fraction)
    {
        double progress = fraction * (path.Length - 1);
        int index = (int)progress;
        if (index >= path.Length - 1) return path;
        double f = progress - index;
        if (f == 0) return path[..(index + 1)];
        var a = path[index];
        var b = path[index + 1];
        var points = ImmutableArray.CreateBuilder<TacticalPoseSnapshot>(index + 2);
        points.AddRange(path, index + 1);
        points.Add(new(a.X + (b.X - a.X) * f, a.Y + (b.Y - a.Y) * f,
            TacticalGeometry.Norm(a.Course + TacticalGeometry.Norm(b.Course - a.Course) * f)));
        return points.MoveToImmutable();
    }
}
