using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Engine.TacticalBattle.Content;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private bool HasCapturedEncounter => _demoCampaign.WorldEncounter is { Phase: not WorldEncounterPhases.Finished };
    private bool _encounterStoragePending;
    public void SetEncounterStoragePending(bool pending)
    { lock (_worldStateLock) _encounterStoragePending = pending; }

    /// <summary>Exact restart checkpoint, captured before the tactical session exists. Storage must commit it before Begin.</summary>
    public ScenarioFile CapturePendingEncounterCheckpoint(BeginEncounterBattleCommand command)
    {
        lock (_worldStateLock)
        {
            var save = CaptureSaveState();
            if (command.ExpectedRevision != _demoCampaign.Revision ||
                _demoCampaign.WorldEncounter is not { Phase: WorldEncounterPhases.Captured } encounter ||
                encounter.Binding.EncounterId != command.EncounterId || BuildWorldEncounterProjection()?.CanContinue != true)
                throw new InvalidOperationException("Encounter is not ready for its checkpoint.");
            return save with
            {
                GameState = save.GameState with
                {
                    CurrentSpeed = SimulationSpeed.Speed0.ToString(),
                    DemoCampaign = _demoCampaign with { WorldEncounter = encounter with { Phase = WorldEncounterPhases.PendingBattle } }
                }
            };
        }
    }

    private void RestorePendingEncounterBattle()
    {
        if (_demoCampaign.WorldEncounter is not { Phase: WorldEncounterPhases.PendingBattle } encounter) return;
        var candidate = _demoCampaign with { WorldEncounter = encounter with { Phase = WorldEncounterPhases.Captured } };
        string? reason = null;
        Action? commit = null;
        PrepareEncounterBattle(new("restore:" + encounter.Binding.BattleId, candidate.Revision, encounter.Binding.EncounterId),
            ref candidate, ref reason, ref commit);
        if (reason is not null || commit is null) throw new ScenarioException("Cannot restart pending encounter: " + reason);
        commit();
        _demoCampaign = candidate;
        _clock.Reset(_processedWorldTimeMs, SimulationSpeed.Speed0, _processedSimulationTimeMs, _clock.Capture().MotionQuarterRemainder);
    }

    private static DemoCampaignData PrepareEncounterSettlement(DemoCampaignData campaign, TacticalBattleSession session)
    {
        if (campaign.WorldEncounter is not { Phase: WorldEncounterPhases.PendingBattle } encounter ||
            encounter.Binding.BattleId != session.Id) return campaign;
        if (campaign.EncounterResults.Any(r => r.Binding.BattleId == session.Id)) return campaign;
        var snapshot = session.Capture();
        if (snapshot.Outcome is null) throw new InvalidOperationException("Cannot settle an unfinished campaign encounter.");
        var world = campaign.PirateWorld!;
        int index = Enumerable.Range(0, world.Groups.Length).Single(i => world.Groups[i].GroupId == encounter.Binding.GroupId);
        var group = world.Groups[index];
        var actualCarriers = snapshot.Units.Where(u => u.Kind == TacticalUnitKind.Carrier && u.Side != session.PlayerSide)
            .ToDictionary(u => u.Id, StringComparer.Ordinal);
        group = group with
        {
            Retired = true,
            MotionPlan = null,
            Members = group.Members.Select(m => m with { IsDestroyed = actualCarriers[m.CarrierId].Hull <= 0 }).ToImmutableArray()
        };
        return campaign with
        {
            PirateWorld = world with { Groups = world.Groups.SetItem(index, group) },
            WorldEncounter = encounter with { Phase = WorldEncounterPhases.Finished },
            EncounterResults = campaign.EncounterResults.Add(new(encounter.Binding,
                snapshot.Outcome.Kind.ToString() == session.PlayerSide, snapshot.Outcome, snapshot.Units))
        };
    }

    private void PublishCapturedEncounter()
    {
        if (HasCapturedEncounter || _demoCampaign.PirateWorld is not { } world) return;
        var group = world.Groups.FirstOrDefault(g => !g.Retired && g.Phase == "captured");
        if (group is null) return;
        var player = _objects.Single(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        var pose = PredictMotion(player, Math.Max(0, _processedSimulationTimeMs - player.StartGameTimeMs));
        var candidate = AddSensorReport(EnsurePirateComposition(_demoCampaign, group.GroupId), group.GroupId, pose, _processedSimulationTimeMs);
        string encounterId = "encounter:" + group.GroupId;
        _demoCampaign = candidate with
        {
            WorldEncounter = new(new("battle:" + group.GroupId, encounterId, group.GroupId),
                PirateRandom(MasterSeed, "battle:" + group.GroupId).Next(), WorldEncounterPhases.Captured),
            Revision = checked(candidate.Revision + 1),
            CommitSequence = checked(candidate.CommitSequence + 1)
        };
        _clock.Reset(_processedWorldTimeMs, SimulationSpeed.Speed0, _processedSimulationTimeMs, _clock.Capture().MotionQuarterRemainder);
    }

    private WorldEncounterSnapshot? BuildWorldEncounterProjection()
    {
        var encounter = _demoCampaign.WorldEncounter;
        if (encounter is null) return null;
        string? reason = encounter.Phase == WorldEncounterPhases.Finished ? "encounter_finished"
            : CombatOrLootOpen ? TacticalBattleActiveReason
            : _demoCampaign.PendingSensorReport is not null ? "sensor_report_pending"
            : _dialogue.Active is not null ? "dialogue_active" : null;
        return new(encounter.Binding, encounter.Phase, reason is null, reason);
    }

    private void PrepareEncounterBattle(BeginEncounterBattleCommand command, ref DemoCampaignData candidate,
        ref string? reason, ref Action? commit)
    {
        if (candidate.WorldEncounter is not { Phase: WorldEncounterPhases.Captured } encounter ||
            encounter.Binding.EncounterId != command.EncounterId) { reason = "encounter_not_captured"; return; }
        if (BuildWorldEncounterProjection()?.UnavailableReason is { } unavailable) { reason = unavailable; return; }
        if (_registry.TacticalBattle.Catalog is not { } catalog) { reason = "battle_unavailable"; return; }
        var ship = _objects.Single(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        var wing = GetOwnedWing(PlayerShipObjectId!).OrderBy(d => d.DroneId, StringComparer.Ordinal).ToImmutableArray();
        var deployable = wing.Where(d => !d.Destroyed && !d.Disabled && ship.Crew.Any(c => c.Id == d.CrewId) && IsHangarReady(d.HangarModuleId))
            .Select(d => d.DroneId).ToImmutableHashSet(StringComparer.Ordinal);
        var scenario = BuildPirateEncounterScenario(encounter.Binding.GroupId, catalog);
        var battle = OwnedBattleAssembly.Create(catalog, PlayerShipObjectId!,
            GetCarrierHullCurrent(PlayerShipObjectId!, GetCarrierHullMax(PlayerShipObjectId!)), GetShipManeuver(PlayerShipObjectId!),
            wing, ship.Crew, scenario, encounter.Binding.GroupId, encounter.Seed, "A", deployable);
        // No deployable drones is a real defeat, not a refusal that lets a captured ship escape.
        var session = TacticalBattleSession.Create(battle, encounter.Binding.BattleId, "A");
        battle.CheckVictory();
        candidate = candidate with { WorldEncounter = encounter with { Phase = WorldEncounterPhases.PendingBattle } };
        commit = () =>
        {
            _tacticalBattleSession = session;
            _tacticalBattleSourceShipId = PlayerShipObjectId;
            SetTacticalBattleCommandBlock(true);
        };
    }

    internal BattleScenario BuildPirateEncounterScenario(string groupId, TacticalBattleCatalog catalog)
    {
        var group = _demoCampaign.PirateWorld?.Groups.SingleOrDefault(g => g.GroupId == groupId)
            ?? throw new ScenarioException("Unknown pirate encounter group.");
        if (!group.CompositionMaterialized) throw new ScenarioException("Pirate encounter composition is not materialized.");
        var template = catalog.Scenarios["initial"];
        var own = template.Sides.Single(s => s.Id == "A");
        var enemy = template.Sides.Single(s => s.Id == "B");
        var carriers = ImmutableArray.CreateBuilder<BattleCarrierStart>();
        var drones = ImmutableArray.CreateBuilder<BattleDroneStart>();
        for (int memberIndex = 0; memberIndex < group.Members.Length; memberIndex++)
        {
            var member = group.Members[memberIndex];
            var composition = _demoCampaign.PirateCompositions.Single(c => c.GroupId == groupId && c.CarrierId == member.CarrierId);
            double centerY = group.Members.Length == 2 ? 150 + 300 * memberIndex : 250;
            carriers.Add(new(member.CarrierId, "carrier.standard", 850, centerY, 180));
            for (int i = 0; i < composition.Drones.Length; i++)
            {
                var drone = composition.Drones[i];
                int rows = group.Members.Length == 2 ? 6 : 5;
                double y = centerY - (rows - 1) * 20 + i % rows * 40;
                drones.Add(new(drone.DroneId, enemy.Drones[i % enemy.Drones.Length].Pilot, drone.LayoutId,
                    700 + i / rows * 60, y, 180, enemy.Drones[i % enemy.Drones.Length].Speed, member.CarrierId));
            }
        }
        var ownCarrier = own.Carrier with { Id = PlayerShipObjectId! };
        int slots = GetOwnedWing(PlayerShipObjectId!).Count(d => d.ReplacesDroneId is null);
        own = own with
        {
            Carrier = ownCarrier,
            Carriers = [ownCarrier],
            Drones = Enumerable.Range(0, slots).Select(i => own.Drones[i % own.Drones.Length] with
            { Id = "player-slot:" + i, Y = 100 + 40 * i, OriginCarrierId = ownCarrier.Id }).ToImmutableArray()
        };
        enemy = enemy with { Carrier = carriers[0], Carriers = carriers.ToImmutable(), Drones = drones.ToImmutable() };
        return TacticalBattleContentLoader.NormalizeScenario(template with { Name = "Pirate encounter", Sides = [own, enemy], Goal = null });
    }
}
