using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.Ship;

/// <summary>Read-only presentation of the persistent wing (one card per owned drone).</summary>
public static class ShipWingPanel
{
    /// <summary>Stable state label; a destroyed drone is never presented as ready, and a disabled drone is not ready either.</summary>
    public static string DroneStateLabel(bool destroyed, bool disabled = false, bool uncrewed = false) =>
        destroyed ? "destroyed" : disabled ? "disabled" : uncrewed ? Localization.Get("Demo.Recruitment.NoPilot") : "ready";

    public static string DroneAmmoText(int current, int capacity) => $"{current}/{capacity}";

    /// <summary>Card state for a wing drone: Disabled (jam) and Destroyed are kept distinct from Ready.</summary>
    public static string DroneCardState(OwnedDroneSnapshot drone)
    {
        if (drone.ReadinessReasons.IsDefaultOrEmpty)
            return "ready";
        if (drone.ReadinessReasons.Contains("destroyed"))
            return "destroyed";
        if (drone.ReadinessReasons.Contains("no_pilot")) return Localization.Get("Demo.Recruitment.NoPilot");
        return "disabled";
    }

    /// <summary>
    /// The largest quantity the player may select: the magazine gap, limited by the torpedoes in the
    /// cargo hold. Never negative.
    /// </summary>
    public static int MaxSelectableQuantity(int gap, int stock) => Math.Max(0, Math.Min(gap, stock));

    /// <summary>Clamp a chosen quantity into [0, max]; the chosen value is preserved, never auto-raised to max.</summary>
    public static int ClampQuantity(int selected, int max) => Math.Clamp(selected, 0, Math.Max(0, max));

    /// <summary>
    /// One-line rearm preview. The transferred count is bounded by the magazine gap and the cargo
    /// stock, so the preview never promises more than the hold can supply.
    /// </summary>
    public static string PreviewText(int current, int capacity, int stock, int quantity)
    {
        int transferred = ClampQuantity(quantity, MaxSelectableQuantity(capacity - current, stock));
        int magazine = Math.Min(capacity, Math.Max(0, current) + transferred);
        int hold = Math.Max(0, stock - transferred);
        return $"magazine {current}→{magazine}, hold {stock}→{hold}";
    }
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
