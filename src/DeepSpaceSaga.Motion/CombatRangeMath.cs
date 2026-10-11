using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion;

/// <summary>Shared inclusive weapon-range geometry. One world unit is 100 metres.</summary>
public static class CombatRangeMath
{
    public static bool IsWithinRange(ObjectMotionSnapshot origin, ObjectMotionSnapshot target, double rangeKm)
    {
        if (!double.IsFinite(rangeKm) || rangeKm <= 0) return false;
        double dx = Math.Abs(target.X / 10 - origin.X / 10);
        double dy = Math.Abs(target.Y / 10 - origin.Y / 10);
        double longest = Math.Max(dx, dy);
        double ratio = longest == 0 ? 0 : Math.Min(dx, dy) / longest;
        double distanceKm = longest * Math.Sqrt(1 + ratio * ratio);
        return double.IsFinite(distanceKm) && distanceKm <= rangeKm;
    }
}
