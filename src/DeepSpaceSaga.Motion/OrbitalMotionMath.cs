using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion;

/// <summary>Analytic ellipse in world units, evaluated from the absolute motion clock.
/// Periods use calendar milliseconds; no calendar multiplier is applied to ship motion.</summary>
public static class OrbitalMotionMath
{
    public static ObjectMotionSnapshot At(ObjectMotionSnapshot state, OrbitalElements orbit, long simulationTimeMs)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(orbit);
        if (simulationTimeMs < 0 || orbit.EpochSimulationTimeMs < 0 || orbit.OrbitalPeriodMs <= 0 ||
            !double.IsFinite(orbit.SemiMajorAxis) || !double.IsFinite(orbit.SemiMinorAxis) ||
            orbit.SemiMinorAxis <= 0 || orbit.SemiMajorAxis < orbit.SemiMinorAxis ||
            orbit.InitialPhase is < 0 or > 359 || !double.IsFinite(orbit.PhaseOffsetDegrees) ||
            orbit.PhaseOffsetDegrees is < 0 or >= 1 || !double.IsFinite(state.WorldOffsetX) || !double.IsFinite(state.WorldOffsetY) ||
            orbit.OrbitDirection is not ("clockwise" or "counterclockwise"))
            throw new ArgumentOutOfRangeException(nameof(orbit), "Invalid orbital dimensions, phase, direction or timestamp.");

        // Integer remainder before conversion preserves sub-millisecond physical periods
        // and compact phase differences even near Int64.MaxValue, without time overflow.
        Int128 elapsedCalendarMs = ((Int128)simulationTimeMs - orbit.EpochSimulationTimeMs)
            * SimulationSpeedExtensions.BaseGameSecondsPerRealSecond;
        Int128 remainder = elapsedCalendarMs % orbit.OrbitalPeriodMs;
        if (remainder < 0) remainder += orbit.OrbitalPeriodMs;
        double sign = orbit.OrbitDirection == "clockwise" ? 1 : -1;
        double theta = (orbit.InitialPhase + orbit.PhaseOffsetDegrees) * Math.PI / 180
            + sign * Math.Tau * (double)remainder / orbit.OrbitalPeriodMs;
        double omega = sign * Math.Tau * 1000 * SimulationSpeedExtensions.BaseGameSecondsPerRealSecond / orbit.OrbitalPeriodMs;
        double vx = orbit.SemiMajorAxis * Math.Cos(theta) * omega;
        double vy = orbit.SemiMinorAxis * Math.Sin(theta) * omega;
        double scale = Math.Max(Math.Abs(vx), Math.Abs(vy));
        double speed = scale == 0 ? 0 : scale * Math.Sqrt(Math.Pow(vx / scale, 2) + Math.Pow(vy / scale, 2)) / 10;
        double heading = (Math.Atan2(vx, -vy) * 180 / Math.PI + 360) % 360;
        double x = orbit.SemiMajorAxis * Math.Sin(theta) + state.WorldOffsetX;
        double y = -orbit.SemiMinorAxis * Math.Cos(theta) + state.WorldOffsetY;
        if (!double.IsFinite(x) || !double.IsFinite(y) || !double.IsFinite(speed))
            throw new ArgumentOutOfRangeException(nameof(orbit), "Orbital pose or velocity exceeds the finite coordinate range.");
        return state with
        {
            X = x,
            Y = y,
            SpeedKmS = speed,
            Direction = heading,
            Orbit = orbit,
            OrbitSampleSimulationTimeMs = simulationTimeMs
        };
    }
}
