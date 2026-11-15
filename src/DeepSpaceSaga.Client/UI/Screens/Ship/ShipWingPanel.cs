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
}
