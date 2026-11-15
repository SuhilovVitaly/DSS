using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.Ship;

/// <summary>Read-only presentation of the persistent wing (one card per owned drone).</summary>
public static class ShipWingPanel
{
    /// <summary>Stable state label; a destroyed drone is never presented as ready.</summary>
    public static string DroneStateLabel(bool destroyed) => destroyed ? "destroyed" : "ready";

    public static string DroneAmmoText(int current, int capacity) => $"{current}/{capacity}";

    /// <summary>Card state for a wing drone: Disabled (jam) and Destroyed are kept distinct from Ready.</summary>
    public static string DroneCardState(OwnedDroneSnapshot drone)
    {
        if (drone.ReadinessReasons.IsDefaultOrEmpty)
            return "ready";
        if (drone.ReadinessReasons.Contains("destroyed"))
            return "destroyed";
        return "disabled";
    }

    /// <summary>The largest quantity the player may select for this drone (never negative).</summary>
    public static int MaxSelectableQuantity(int current, int capacity) => Math.Max(0, capacity - current);

    /// <summary>Clamp a chosen quantity into [0, max]; the chosen value is preserved, never auto-raised to max.</summary>
    public static int ClampQuantity(int selected, int max) => Math.Clamp(selected, 0, Math.Max(0, max));
}

/// <summary>
/// Confirmation gate for a rearm request. A double-click (or a click while a request is already in
/// flight) starts exactly one request; the selected quantity is carried through unchanged and the
/// client never mutates the drone's magazine locally.
/// </summary>
public sealed class ShipRearmController
{
    private bool _pending;
    public bool IsPending => _pending;
    public int SelectedQuantity { get; private set; }

    public bool TryBegin(int selectedQuantity)
    {
        if (_pending) return false;
        _pending = true;
        SelectedQuantity = selectedQuantity;
        return true;
    }

    public void End() => _pending = false;
}
