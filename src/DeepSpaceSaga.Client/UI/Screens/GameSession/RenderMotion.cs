using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>
/// Frame-local pose plus immutable motion metadata. Linear contacts need only
/// three doubles per frame, not a clone of the entire transport snapshot.
/// </summary>
internal readonly record struct RenderMotion(ObjectMotionSnapshot Motion, double X, double Y, double Direction)
{
    internal RenderMotion(ObjectMotionSnapshot motion) : this(motion, motion.X, motion.Y, motion.Direction) { }
    internal string ObjectId => Motion.ObjectId;
    internal double SpeedKmS => Motion.SpeedKmS;
    internal string? RenderObjectType => Motion.RenderObjectType;
    internal string? RelationToPlayer => Motion.RelationToPlayer;
    internal string? DisplayName => Motion.DisplayName;
    internal string? Image => Motion.Image;
    internal string? ActiveEngineCommandType => Motion.ActiveEngineCommandType;
    internal double? NavigationTargetX => Motion.NavigationTargetX;
    internal double? NavigationTargetY => Motion.NavigationTargetY;
    internal string? NavigationTargetObjectId => Motion.NavigationTargetObjectId;
    internal ApproachRoute? ApproachRoute => Motion.ApproachRoute;
    internal int TurnStepDegrees => Motion.TurnStepDegrees;
    internal long TurnStepRemainingMs => Motion.TurnStepRemainingMs;

    // Materialize only for selected-object forecasts and public test/inspection seams.
    internal ObjectMotionSnapshot ToSnapshot() =>
        Motion is null || (X == Motion.X && Y == Motion.Y && Direction == Motion.Direction)
            ? Motion! : Motion with { X = X, Y = Y, Direction = Direction };
}

internal readonly record struct ObjectRenderState(ObjectMotionSnapshot Source, RenderMotion Pose, bool IsPlayerShip)
{
    internal ObjectRenderState(ObjectMotionSnapshot Source, ObjectMotionSnapshot Predicted, bool IsPlayerShip)
        : this(Source, new RenderMotion(Predicted), IsPlayerShip) { }
    internal ObjectMotionSnapshot Predicted => Pose.ToSnapshot();
}
