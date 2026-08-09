using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>
/// Player-visible label policy for the tactical map. Pure text policy —
/// no Skia/graphics dependencies, so it is unit-testable in isolation.
/// Builds labels ONLY from the client-visible render projection
/// (<c>RenderObjectType</c>), never from authoritative factual data.
/// </summary>
internal static class ObjectLabelText
{
    /// <summary>Label for objects the player has not discovered yet.</summary>
    public const string UnknownLabel = "Неизвестный объект";

    /// <summary>
    /// Build the label text for one object.
    /// </summary>
    /// <param name="renderObjectType">Client-visible type projection
    /// (<c>SpaceObjectType.UnknownSpaceObject</c> for undiscovered objects).</param>
    /// <param name="displayName">Player-visible name (already null for unknown objects).</param>
    /// <param name="objectId">Authoritative object ID — shown for asteroids and stations.</param>
    public static string Build(string? renderObjectType, string? displayName, string objectId)
    {
        return renderObjectType switch
        {
            SpaceObjectType.UnknownSpaceObject => UnknownLabel,
            SpaceObjectType.Asteroid => objectId,
            SpaceObjectType.Station =>
                string.IsNullOrWhiteSpace(displayName) ? objectId : $"{displayName} [{objectId}]",
            SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip =>
                string.IsNullOrWhiteSpace(displayName) ? UnknownLabel : displayName,
            _ => string.IsNullOrWhiteSpace(displayName) ? UnknownLabel : displayName
        };
    }
}
