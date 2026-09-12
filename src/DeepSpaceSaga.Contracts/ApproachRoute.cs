namespace DeepSpaceSaga.Contracts;

/// <summary>
/// Immutable constant-speed route to the target's aft ray. Distances are world
/// units; headings use the map convention. ElapsedMs is relative to the route's
/// origin and is persisted, so a save resumes the same curve without replanning.
/// </summary>
public sealed record ApproachRoute(
    double X, double Y, double Direction, double SpeedKmS, int TurnRate,
    string Type, double First, double Second, double Third,
    double TargetX, double TargetY, double TargetDirection, double TargetSpeedKmS,
    double TrailDistance, double ElapsedMs = 0)
{
    public double Length => First + Second + Third;
    public double DurationMs => SpeedKmS > 0 ? Length / (SpeedKmS * 10) * 1000 : 0;
}
