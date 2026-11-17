using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Rng;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.TacticalBattle;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private static int LootRoll(ulong seed, long draw) => RngStreamNames.CreateDeterministicRandom(
        RngStreamSeedDerivation.DeriveStreamSeed(seed, "battle-loot/v1/draw/" + draw.ToString(System.Globalization.CultureInfo.InvariantCulture))).Next(10000);

    internal BattleLootData InitializeBattleLoot(TacticalBattleSession session)
    {
        var definitions = _demoDefinitions.Loot ?? throw new ScenarioException("Missing loot content.");
        ulong seed = RngStreamSeedDerivation.DeriveStreamSeed(unchecked((ulong)session.Battle.Seed), "battle-loot/v1/" + session.Id);
        long draws = 0;
        int chance = 2500 + (int)Math.Round(GetAssignedEngineerBonusFraction() * 10000);
        var sources = ImmutableArray.CreateBuilder<LootSourceData>();
        var frozen = session.Capture();
        foreach (var drone in session.Battle.Drones.Where(d => d.Side != session.PlayerSide).OrderBy(d => d.Id, StringComparer.Ordinal))
        {
            if (drone.Status == TacticalDroneStatus.Air) continue;
            string layoutId = _demoCampaign.PirateCompositions.SelectMany(c => c.Drones).FirstOrDefault(d => d.DroneId == drone.Id)?.LayoutId
                ?? session.Battle.Catalog.Layouts.Entries.First(p => p.Value == drone.Layout).Id;
            if (drone.Status == TacticalDroneStatus.Destroyed)
            {
                sources.Add(new(drone.Id, "DestroyedDrone", layoutId, drone.Pose.X, drone.Pose.Y, drone.Pose.Course, null, [],
                    new(definitions.ScrapItemTypeId, definitions.DroneScrapQuantityByFrame[drone.Frame.Id], chance), []));
                continue;
            }
            var consumables = ImmutableArray.CreateBuilder<LootConsumableData>();
            void Add(string category, string catalogId, long quantity)
            {
                if (quantity <= 0) return;
                var mapping = definitions.ConsumableItems.Single(m => m.Category == category && m.CatalogId == catalogId);
                bool destroyed = LootRoll(seed, draws++) < 5000;
                consumables.Add(new(mapping.ItemTypeId, quantity, destroyed ? 0 : quantity, destroyed));
            }
            Add("ammo", "torpedo.t1", drone.AmmoCurrent);
            if (drone.Decoy is { } decoy) Add("decoy", decoy.Id, drone.DecoyCharges);
            var build = frozen.Units.Single(u => u.Id == drone.Id).Build!;
            var pilot = new LootPilotSnapshot(LootPilotIdentity(session.Id, drone.Id, build.PilotId), build.PilotName, build.PilotPortraitAssetId,
                true, null, Gender: build.PilotGender ?? CrewGender.Female);
            sources.Add(new(drone.Id, "DisabledDrone", layoutId, drone.Pose.X, drone.Pose.Y, drone.Pose.Course,
                pilot, consumables.ToImmutable(), null, InitializeLootModules(session.Id, drone, seed, ref draws, chance)));
        }
        foreach (var carrier in session.Battle.Carriers.Where(c => c.Side != session.PlayerSide && !c.IsActive).OrderBy(c => c.Id, StringComparer.Ordinal))
        {
            var composition = _demoCampaign.PirateCompositions.SingleOrDefault(c => c.CarrierId == carrier.Id);
            string layout = composition is not null ? _demoDefinitions.Encounters!.Profiles.Single(p => p.SignatureTypeId == composition.SignatureTypeId).CarrierLayoutId
                : "carrier.standard";
            sources.Add(new(carrier.Id, session.Battle.SelfDestructedCarrierIds.Contains(carrier.Id) ? "SelfDestructCarrier" : "Carrier", layout, carrier.Pose.X, carrier.Pose.Y, carrier.Pose.Course, null, [],
                new(definitions.ScrapItemTypeId, definitions.CarrierScrapQuantityByLayout[layout], chance), []));
        }
        return new(session.Id, LootPhases.Open, definitions.Fingerprint, seed, draws, session.Capture(), sources.ToImmutable());
    }

    private bool TryPrepareLootCargo(string itemId, long quantity, out Action? commit)
    {
        commit = null;
        int shipIndex = _objects.FindIndex(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (shipIndex < 0 || quantity <= 0 || !_registry.ItemTypes.Contains(itemId)) return false;
        var ship = _objects[shipIndex];
        if (ship.IsDestroyed) return false;
        int itemIndex = _registry.ItemTypes.GetIndex(itemId);
        long mass = _registry.ItemTypes.GetDefinition(itemIndex).UnitMassKg;
        if (mass <= 0 || quantity > long.MaxValue / mass) return false;
        long reserved = ContractCargoReservedKg(_demoCampaign), totalFree = 0;
        foreach (var module in ship.Modules.Where(m => !m.Destroyed && m.StructurePoints > 0))
            totalFree = checked(totalFree + Math.Max(0, module.AvailableCapacityKg ?? 0));
        if (quantity * mass > totalFree - reserved) return false;
        var modules = ship.Modules;
        long remaining = quantity;
        for (int i = 0; i < modules.Length && remaining > 0; i++)
        {
            var module = modules[i];
            if (module.Destroyed || module.StructurePoints <= 0) continue;
            long amount = Math.Min(remaining, Math.Max(0, module.AvailableCapacityKg ?? 0) / mass);
            if (amount == 0) continue;
            int stackIndex = FindCargoStackIndex(module.Cargo, itemIndex);
            var stack = AddCargoCost(stackIndex < 0 ? null : module.Cargo[stackIndex], itemIndex, amount, 0, "salvaged");
            var cargo = stackIndex < 0 ? module.Cargo.Add(stack) : module.Cargo.SetItem(stackIndex, stack);
            modules = modules.SetItem(i, module with
            {
                Cargo = cargo,
                AvailableCapacityKg = ComputeAvailableCapacityKg(_registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex), cargo)
            });
            remaining -= amount;
        }
        if (remaining != 0) return false;
        var replacement = ship with { Modules = modules };
        commit = () => _objects[shipIndex] = replacement;
        return true;
    }

    internal void PrepareBattleLootCommand(DemoCampaignCommand command, ref DemoCampaignData candidate,
        ref string? reason, ref Action? commit)
    {
        if (command is not ILootCampaignCommand lootCommand || candidate.LootSession is not { } loot || loot.BattleId != lootCommand.BattleId)
        { reason = BattleLootReasons.WrongBattle; return; }
        if (loot.Phase != LootPhases.Open) { reason = BattleLootReasons.LootClosed; return; }
        string? objectId = command switch
        {
            CollectLootConsumablesCommand c => c.ObjectId,
            AttemptScrapCommand c => c.ObjectId,
            AttemptModuleSalvageCommand c => c.ObjectId,
            _ => null
        };
        int index = -1;
        for (int i = 0; i < loot.Sources.Length; i++) if (loot.Sources[i].ObjectId == objectId) index = i;
        if (index < 0) { reason = BattleLootReasons.InvalidTarget; return; }
        var source = loot.Sources[index];
        if (command is CollectLootConsumablesCommand collect)
        {
            var item = source.Consumables.FirstOrDefault(c => c.ItemTypeId == collect.ItemTypeId && !c.Destroyed);
            if (item is null) { reason = BattleLootReasons.InvalidTarget; return; }
            if (collect.Quantity <= 0 || collect.Quantity > item.Remaining) { reason = BattleLootReasons.InvalidQuantity; return; }
            if (!TryPrepareLootCargo(item.ItemTypeId, collect.Quantity, out commit)) { reason = BattleLootReasons.CargoFull; return; }
            source = source with { Consumables = source.Consumables.Replace(item, item with { Remaining = item.Remaining - collect.Quantity }) };
        }
        else if (command is AttemptScrapCommand)
        {
            if (source.Scrap is not { } scrap) { reason = BattleLootReasons.InvalidTarget; return; }
            if (scrap.State != LootAttemptStates.NotAttempted) { reason = BattleLootReasons.AlreadyAttempted; return; }
            if (!TryPrepareLootCargo(scrap.ItemTypeId, scrap.Quantity, out var grant)) { reason = BattleLootReasons.CargoFull; return; }
            bool success = LootRoll(loot.Seed, loot.DrawCount) < scrap.ChanceBasisPoints;
            loot = loot with { DrawCount = checked(loot.DrawCount + 1) };
            source = source with { Scrap = scrap with { State = success ? LootAttemptStates.Collected : LootAttemptStates.Failed } };
            commit = success ? grant : null;
        }
        else if (command is AttemptModuleSalvageCommand salvage)
        {
            PrepareModuleSalvage(salvage, ref loot, ref source, ref reason, ref commit);
            if (reason is not null) return;
        }
        else { reason = DemoCampaignReasons.Unsupported; return; }
        candidate = candidate with { LootSession = loot with { Sources = loot.Sources.SetItem(index, source) } };
    }

    private LootSessionSnapshot? BuildBattleLootProjection()
    {
        if (_demoCampaign.LootSession is not { } loot) return null;
        bool open = loot.Phase == LootPhases.Open;
        var objects = loot.Sources.Select(s => new LootObjectSnapshot(s.ObjectId, s.Kind,
            s.Consumables.Where(c => !c.Destroyed).Select(c =>
            {
                string? reason = !open ? BattleLootReasons.LootClosed : c.Remaining == 0 ? BattleLootReasons.AlreadyAttempted :
                    !TryPrepareLootCargo(c.ItemTypeId, 1, out _) ? BattleLootReasons.CargoFull : null;
                return new LootConsumableSnapshot(c.ItemTypeId, c.Remaining, reason is null, reason);
            }).ToImmutableArray(),
            s.Scrap is { } scrap ? ProjectScrap(scrap, open) : null,
            s.Modules.Where(m => !m.Destroyed).Select(m => ProjectLootModule(m, open)).ToImmutableArray(), ProjectLootPilot(loot, s), s.X, s.Y, s.Direction)).ToImmutableArray();
        return new(loot.BattleId, loot.Phase, objects, open && objects.Any(s =>
            s.Consumables.Any(c => c.Remaining > 0) || s.Scrap?.State == LootAttemptStates.NotAttempted ||
            s.Modules.Any(m => m.State == LootAttemptStates.NotAttempted) || s.Pilot is { Alive: true, Captured: false }));
    }

    private ScrapLootSnapshot ProjectScrap(LootScrapData scrap, bool open)
    {
        string? reason = !open ? BattleLootReasons.LootClosed : scrap.State != LootAttemptStates.NotAttempted ? BattleLootReasons.AlreadyAttempted :
            !TryPrepareLootCargo(scrap.ItemTypeId, scrap.Quantity, out _) ? BattleLootReasons.CargoFull : null;
        return new(scrap.ItemTypeId, scrap.Quantity, scrap.ChanceBasisPoints, scrap.State, reason is null, reason);
    }
}
