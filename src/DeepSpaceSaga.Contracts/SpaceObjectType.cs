namespace DeepSpaceSaga.Contracts;

/// <summary>Well-known space object type identifiers for render metadata.</summary>
public static class SpaceObjectType
{
    public const string PlayerShip = "PlayerShip";
    public const string NpcShip = "NpcShip";
    public const string Asteroid = "Asteroid";
    public const string Station = "Station";
    public const string Container = "Container";
    public const string Missile = "Missile";
    public const string Explosion = "Explosion";
    public const string Planet = "Planet";
    public const string Sun = "Sun";

    /// <summary>
    /// Client-visible projection of an object the player has not yet discovered:
    /// the client must not show the factual type, so the render projection is
    /// this sentinel instead. (Authoritative <see cref="ObjectMotionSnapshot.ObjectType"/>
    /// stays null for such objects.)
    /// </summary>
    public const string UnknownSpaceObject = "UnknownSpaceObject";
}

/// <summary>Relation of an object to the local player (for color selection).</summary>
public static class PlayerRelation
{
    public const string Self = "self";
    public const string Neutral = "neutral";
    public const string Enemy = "enemy";
    public const string Friend = "friend";
}
