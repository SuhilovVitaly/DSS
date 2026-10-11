using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    // Internal actor entry point; it cannot submit commands or write player receipts.
    internal CommandStartOutcome TryStartHostileTorpedoFire(string ownerId, string moduleId, long motionTimeMs)
    {
        lock (_worldStateLock)
        {
            var owner = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == ownerId &&
                o.ObjectType == SpaceObjectType.NpcShip && !o.IsDestroyed && GetRelationToPlayer(o) == PlayerRelation.Enemy);
            if (owner is null) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownObject);
            var player = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId &&
                o.ObjectType == SpaceObjectType.PlayerShip && !o.IsDestroyed);
            if (player is null) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownTarget);
            return TryLaunchTorpedo(owner, moduleId, player.InitialMotion.ObjectId, motionTimeMs);
        }
    }
}
