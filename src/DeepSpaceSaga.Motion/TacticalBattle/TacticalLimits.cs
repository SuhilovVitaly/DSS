namespace DeepSpaceSaga.Motion.TacticalBattle;

public readonly record struct TacticalFrameLimits(double Mass, double ReferenceMass, double MinSpeed, double MaxSpeedCap, double TurnCap);
public readonly record struct TacticalDrive(double Mass, double SpeedBonus, double SpeedChange, double Turn, double Factor);

/// <summary>Current movement limits, V4 Combat §3. Inputs come from captured battle content/state.</summary>
public readonly record struct TacticalLimits(double Mass, double MassFactor, double MinSpeed, double MaxSpeed, double Change, double Turn)
{
    public static TacticalLimits Of(TacticalFrameLimits frame, double armorMass, double weaponMass,
        double shieldMass, int shieldCount, IReadOnlyList<TacticalDrive> engines)
    {
        ArgumentNullException.ThrowIfNull(engines);
        double engineMass = 0, speedBonus = 0, speedChange = 0, turn = 0;
        foreach (var engine in engines)
        {
            engineMass += engine.Mass;
            speedBonus += engine.SpeedBonus * engine.Factor;
            speedChange += engine.SpeedChange * engine.Factor;
            turn += engine.Turn * engine.Factor;
        }
        double mass = frame.Mass + armorMass + weaponMass + shieldMass * shieldCount + engineMass;
        if (!double.IsFinite(mass) || mass <= 0 || !double.IsFinite(frame.ReferenceMass) || frame.ReferenceMass <= 0)
            throw new ArgumentOutOfRangeException(nameof(frame), "Positive finite total and reference mass are required.");
        double k = frame.ReferenceMass / (double)mass;
        return new(mass, k, frame.MinSpeed, PyMath.Min(frame.MaxSpeedCap, frame.MinSpeed + speedBonus * k),
            speedChange * k, PyMath.Min(frame.TurnCap, turn * k));
    }

    public double ClampSpeed(double requested, double current) => PyMath.Clamp(PyMath.Clamp(requested, current - Change, current + Change), MinSpeed, MaxSpeed);

    public double TurnLimit(double speed, double atMaxSpeedFraction)
    {
        if (MaxSpeed <= MinSpeed) return Turn;
        return Turn * (1.0 - (1.0 - atMaxSpeedFraction) * PyMath.Clamp((speed - MinSpeed) / (double)(MaxSpeed - MinSpeed), 0, 1));
    }
}
