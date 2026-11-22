using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.TacticalBattle.Content;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private bool LootOpen => _demoCampaign.LootSession?.Phase == LootPhases.Open;
    private bool CombatOrLootOpen => _tacticalBattleSession is not null || LootOpen || BattleResultsPending;

    private SortieSettlementSnapshot? BeginPostBattleLoot(bool debugVictory = false)
    {
        var session = _tacticalBattleSession;
        if (session?.Battle.Result is not { } result || result.Kind.ToString() != session.PlayerSide ||
            _tacticalBattleSourceShipId is null) return null;
        bool encounterVictory = _demoCampaign.WorldEncounter is { Phase: WorldEncounterPhases.PendingBattle } encounter &&
            encounter.Binding.BattleId == session.Id;
        if (!encounterVictory && (!debugVictory || HasCapturedEncounter || _demoDefinitions.Loot is null)) return null;
        var loot = InitializeBattleLoot(session, debugSortie: !encounterVictory);
        var settlement = CommitSortieOutcome(session, SortieExitKind.Completed, loot);
        _tacticalBattleSession = null;
        _tacticalBattleSourceShipId = null;
        SetTacticalBattleCommandBlock(true);
        _clock.Reset(_processedWorldTimeMs, SimulationSpeed.Speed0, _processedSimulationTimeMs, _clock.Capture().MotionQuarterRemainder);
        return settlement;
    }

    private void PrepareFinishBattleLoot(FinishBattleLootCommand command, ref DemoCampaignData candidate,
        ref string? reason, ref Action? commit)
    {
        if (candidate.LootSession is not { } loot || loot.BattleId != command.BattleId)
        { reason = BattleLootReasons.WrongBattle; return; }
        if (loot.Phase != LootPhases.Open) { reason = BattleLootReasons.LootClosed; return; }
        if (_lootOperations.Count != 0) { reason = BattleLootReasons.OperationsRunning; return; }
        if (!command.ConfirmDiscard && BuildBattleLootProjection()!.HasUnclaimed)
        { reason = BattleLootReasons.ConfirmDiscardRequired; return; }
        // Keep frozen remaining quantities and outcomes as the discarded audit; Closed makes
        // every remaining source permanently inaccessible without another roll or grant.
        candidate = candidate with { LootSession = loot with { Phase = LootPhases.Closed } };
        commit = () => SetTacticalBattleCommandBlock(false);
    }

    private void ValidateBattleLootForLoad(DemoCampaignData campaign)
    {
        if (campaign.LootSession is not { } loot) return;
        BattleLootData.Validate(loot);
        if (!loot.DebugSortie && loot.Phase == LootPhases.Open && (campaign.WorldEncounter is not { Phase: WorldEncounterPhases.Finished } encounter ||
            encounter.Binding.BattleId != loot.BattleId || campaign.PendingSensorReport is not null))
            throw new ScenarioException("Open loot requires its finished encounter and no pending sensor report.");
        var victory = campaign.EncounterResults.SingleOrDefault(r => r.Binding.BattleId == loot.BattleId && r.PlayerWon);
        if (loot.Map.Phase != TacticalBattlePhase.Over || loot.Map.Outcome?.Kind.ToString() != loot.Map.PlayerSide ||
            (loot.DebugSortie ? victory is not null || string.IsNullOrWhiteSpace(loot.Map.SourceShipId) ||
                loot.Phase == LootPhases.Open && campaign.WorldEncounter is { Phase: not WorldEncounterPhases.Finished }
                : victory is null || victory.PlayerSide != loot.Map.PlayerSide ||
                System.Text.Json.JsonSerializer.Serialize(loot.Map.Outcome) != System.Text.Json.JsonSerializer.Serialize(victory.Outcome) ||
                System.Text.Json.JsonSerializer.Serialize(loot.Map.Units) != System.Text.Json.JsonSerializer.Serialize(victory.Units)))
            throw new ScenarioException("Invalid or incompatible frozen battle loot.");
        void Require(bool valid) { if (!valid) throw new ScenarioException("Loot source does not match frozen battle/content."); }
        var expectedSources = loot.Map.Units.Where(u => u.Side != loot.Map.PlayerSide &&
            (u.Kind == TacticalUnitKind.Carrier ? u.Hull == 0 : u.State is TacticalUnitState.Disabled or TacticalUnitState.Destroyed));
        Require(expectedSources.Select(u => u.Id).Order(StringComparer.Ordinal).SequenceEqual(loot.Sources.Select(s => s.ObjectId).Order(StringComparer.Ordinal)));
        Require(loot.Sources.All(s => expectedSources.Any(u => u.Id == s.ObjectId && s.X == u.Pose.X && s.Y == u.Pose.Y && s.Direction == u.Pose.Course)));
        // Closed ledgers are an immutable audit, not an entitlement to current content.
        if (loot.Phase == LootPhases.Closed) return;
        Require(loot.CatalogFingerprint == _demoDefinitions.Loot?.Fingerprint);
        var definitions = _demoDefinitions.Loot ?? throw new ScenarioException("Missing loot content.");
        var catalog = _registry.TacticalBattle.Catalog ?? throw new ScenarioException("Missing tactical content.");
        BattleSide? scenarioEnemy = null;
        if (loot.DebugSortie)
        {
            Require(catalog.Scenarios.Contains(loot.Map.ScenarioId));
            var scenario = TacticalBattleContentLoader.NormalizeScenario(catalog.Scenarios[loot.Map.ScenarioId]);
            scenarioEnemy = scenario.Sides.Single(s => s.Id != loot.Map.PlayerSide);
        }
        foreach (var source in loot.Sources)
        {
            var unit = expectedSources.Single(u => u.Id == source.ObjectId);
            Require(source.X == unit.Pose.X && source.Y == unit.Pose.Y && source.Direction == unit.Pose.Course);
            string layoutId;
            if (scenarioEnemy is not null)
            {
                layoutId = (unit.Kind == TacticalUnitKind.Carrier
                    ? scenarioEnemy.CarrierGroup.FirstOrDefault(c => c.Id == unit.Id)?.Layout
                    : scenarioEnemy.Drones.FirstOrDefault(d => d.Id == unit.Id)?.Layout)
                    ?? throw new ScenarioException("Missing debug loot scenario layout.");
            }
            else
            {
                var compositions = campaign.PirateCompositions.Where(c => c.CarrierId == (unit.Kind == TacticalUnitKind.Carrier ? unit.Id : unit.OriginCarrierId)).ToArray();
                Require(compositions.Length == 1);
                var composition = compositions[0];
                layoutId = unit.Kind == TacticalUnitKind.Carrier
                    ? _demoDefinitions.Encounters?.Profiles.FirstOrDefault(p => p.SignatureTypeId == composition.SignatureTypeId)?.CarrierLayoutId ?? throw new ScenarioException("Missing loot carrier profile.")
                    : composition.Drones.FirstOrDefault(d => d.DroneId == unit.Id)?.LayoutId ?? throw new ScenarioException("Missing loot drone composition.");
            }
            Require(source.LayoutId == layoutId && catalog.Layouts.Contains(layoutId));
            var layout = catalog.Layouts[layoutId];
            if (source.Scrap is { } scrap)
            {
                Require(unit.Kind == TacticalUnitKind.Carrier ? source.Kind is "Carrier" or "SelfDestructCarrier" && unit.Hull == 0
                    : source.Kind == "DestroyedDrone" && unit.State == TacticalUnitState.Destroyed);
                Require(scrap.ItemTypeId == definitions.ScrapItemTypeId && scrap.Quantity == (unit.Kind == TacticalUnitKind.Carrier
                    ? definitions.CarrierScrapQuantityByLayout[layoutId] : DroneLootScrapQuantity(definitions, layout.Class, loot.DebugSortie)));
                continue;
            }
            Require(source.Kind == "DisabledDrone" && unit.State == TacticalUnitState.Disabled && unit.Kind == TacticalUnitKind.Drone);
            Require(unit.Build is { } build && source.Pilot is { Alive: true, Captured: false } pilot &&
                pilot.SourcePilotId == LootPilotIdentity(loot.BattleId, unit.Id, build.PilotId) && pilot.DisplayName == build.PilotName &&
                pilot.PortraitAssetId == build.PilotPortraitAssetId && pilot.Gender == (build.PilotGender ?? CrewGender.Female));
            var consumables = new Dictionary<string, long>(StringComparer.Ordinal);
            void Consumable(string category, string id, int quantity)
            {
                if (quantity > 0) consumables.Add(definitions.ConsumableItems.Single(m => m.Category == category && m.CatalogId == id).ItemTypeId, quantity);
            }
            Consumable("ammo", "torpedo.t1", unit.AmmoCurrent ?? 0);
            if (layout.Module is { } decoy) Consumable("decoy", decoy, unit.DecoyCharges ?? 0);
            Require(source.Consumables.Length == consumables.Count && source.Consumables.All(c =>
                consumables.TryGetValue(c.ItemTypeId, out long amount) && c.OriginalQuantity == amount));
            var instances = new Dictionary<string, (string Category, string CatalogId)>(StringComparer.Ordinal);
            Require(!unit.Engines.IsDefault && unit.Engines.Length == layout.Engines.Length);
            void Module(string category, string id, int slot)
            {
                if (!loot.DebugSortie || definitions.ModuleItems!.Value.Any(m => m.Category == category && m.CatalogId == id))
                    instances.Add(loot.BattleId + "/" + unit.Id + "/" + category + "/" + slot, (category, id));
            }
            Module("weapon", layout.Weapon!, 0); Module("armor", layout.Armor, 0);
            if (layout.Shield is { } shield) for (int i = 0; i < (layout.ShieldCount ?? 1); i++) Module("shield", shield, i);
            for (int i = 0; i < layout.Engines.Length; i++) if (unit.Engines[i].Factor > 0) Module("engine", layout.Engines[i], i);
            if (layout.Module is { } module) Module("decoy", module, 0);
            Require(source.Modules.Length == instances.Count && source.Modules.All(m =>
                instances.TryGetValue(m.InstanceId, out var expected) && expected == (m.Category, m.CatalogId) &&
                definitions.ModuleItems!.Value.Any(mapping => mapping.Category == m.Category && mapping.CatalogId == m.CatalogId && mapping.ItemTypeId == m.ItemTypeId)));
        }
    }
}
