namespace DeepSpaceSaga.Contracts;

/// <summary>
/// Phase of a staged navigation maneuver (engine.navigate-to-point).
/// Published in <see cref="ObjectMotionSnapshot"/> so the client-side predictor
/// and trajectory projector can mirror the authoritative engine behaviour.
/// </summary>
public enum NavigationPhase
{
    /// <summary>Standard approach — the ship turns toward and flies through the target.</summary>
    Approach,

    /// <summary>
    /// Close-target escape turn: the ship is turning away from the target onto the
    /// escape course (bearing from target to ship).
    /// </summary>
    EscapeTurn,

    /// <summary>
    /// Straight departure away from the target until the required turn-reserve
    /// distance is reached.
    /// </summary>
    EscapeDepart,
}
