using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.TacticalBattle;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    /// <summary>
    /// Atomically apply a finished/aborted owned sortie's consequences to the world (US-0006).
    /// Runs once per battle; the caller must invoke it before clearing the session. Survivors
    /// return to their slots with actual hull/armor/ammo/jam, destroyed drones become tombstones
    /// (their remaining ammo is lost with the magazine, never cargo), and a zero carrier hull
    /// marks the world ship destroyed.
    /// </summary>
    internal SortieSettlementSnapshot CommitSortieOutcome(TacticalBattleSession session, SortieExitKind exitKind)
    {
        lock (_worldStateLock)
        {
            string shipId = _tacticalBattleSourceShipId!;
            if (_carrierSettlement.TryGetValue(shipId, out var committed) && committed.BattleId == session.Id)
                return ProjectSettlement(shipId, committed);
            var carrier = session.Battle.Carriers.First(c => c.Side == session.PlayerSide);
            bool shipDestroyed = carrier.Hull <= 0;

            var wing = GetOwnedWing(shipId);
            var byId = wing.ToDictionary(d => d.DroneId, StringComparer.Ordinal);
            var updated = ImmutableArray.CreateBuilder<OwnedDroneData>(wing.Length);
            var lostIds = ImmutableArray.CreateBuilder<string>();
            int consumedTorpedoes = 0;

            foreach (var drone in session.Battle.Drones.Where(d => d.Side == session.PlayerSide))
            {
                if (!byId.TryGetValue(drone.Id, out var owned)) continue;
                consumedTorpedoes += owned.AmmoCurrent - drone.AmmoCurrent;
                if (drone.Status == TacticalDroneStatus.Destroyed)
                {
                    updated.Add(owned with { Destroyed = true, CrewId = "" });
                    lostIds.Add(drone.Id);
                }
                else
                {
                    updated.Add(owned with
                    {
                        CurrentHull = drone.Hull,
                        Armor = drone.Armor,
                        AmmoCurrent = drone.AmmoCurrent,
                        Jammed = drone.Jammed,
                        DecoyCharges = drone.DecoyCharges,
                        // A disabled survivor is not repaired by closing the screen (A-05); engine
                        // damage is captured per engine so the next sortie restores it (US-0006).
                        Disabled = drone.Status == TacticalDroneStatus.Disabled,
                        EngineFactors = drone.Engines.Select(e => e.Factor).ToArray(),
                    });
                }
            }
            // Keep any wing drones that were not deployed (defensive; normally none).
            foreach (var owned in wing.Where(d => !session.Battle.Drones.Any(x => x.Id == d.DroneId)))
                updated.Add(owned);

            var pilotLosses = PreparePilotLosses(shipId, session.Id, wing, lostIds.ToArray());
            long campaignRevision = checked(_demoCampaign.Revision + 1);
            long commitSequence = checked(_demoCampaign.CommitSequence + 1);
            _ownedWing[shipId] = updated.ToImmutable();
            int shipIndex = _objects.FindIndex(o => o.InitialMotion.ObjectId == shipId);
            _objects[shipIndex] = pilotLosses.Ship;

            int hullMax = GetCarrierHullMax(shipId);
            int revision = GetCarrierRevision(shipId) + 1;
            _carrierHull[shipId] = new CarrierHullRuntime(shipDestroyed ? 0 : carrier.Hull, revision);

            if (shipDestroyed)
            {
                for (int i = 0; i < _objects.Count; i++)
                    if (_objects[i].InitialMotion.ObjectId == shipId)
                        _objects[i] = _objects[i] with { IsDestroyed = true };
            }

            var lostIdsArray = lostIds.ToImmutable();
            _carrierSettlement[shipId] = new SortieSettlementData(
                BattleId: session.Id,
                CarrierRevision: revision,
                ShipDestroyed: shipDestroyed,
                LostDroneIds: lostIdsArray.IsEmpty ? null : lostIdsArray.ToArray(),
                ConsumedTorpedoes: consumedTorpedoes,
                ExitKind: exitKind.ToString());

            _demoCampaign = pilotLosses.Campaign with
            {
                Revision = campaignRevision,
                CommitSequence = commitSequence
            };

            return new SortieSettlementSnapshot(
                BattleId: session.Id,
                ShipId: shipId,
                CarrierRevision: revision,
                ShipDestroyed: shipDestroyed,
                LostDroneIds: lostIdsArray,
                ConsumedTorpedoes: consumedTorpedoes,
                ExitKind: exitKind);
        }
    }

    internal static SortieExitKind MapSortieExitKind(TacticalOutcomeKind kind, ImmutableArray<TacticalOutcomeReason> reasons) => kind switch
    {
        TacticalOutcomeKind.Timeout => SortieExitKind.Timeout,
        TacticalOutcomeKind.Draw when reasons.Any(r => r is TacticalNoCombatCapability) => SortieExitKind.Stalemate,
        _ => SortieExitKind.Completed,
    };
}
