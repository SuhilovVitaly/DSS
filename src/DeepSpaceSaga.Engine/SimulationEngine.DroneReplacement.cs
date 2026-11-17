using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private ReplacementSnapshot? BuildReplacementProjection(DemoCampaignData state, ImmutableArray<OwnedDroneData>? replacement = null, string? lastId = null)
    {
        if (state.CatalogFingerprint is null || _demoDefinitions.Services is not { } services) return null;
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (ship is null || !ship.IsDocked || ship.DockedStationObjectId is not { } station)
            return new([], Reason: CompanyServiceReasons.NotDocked);
        var wing = replacement ?? GetOwnedWing(ship.InitialMotion.ObjectId);
        var slots = ImmutableArray.CreateBuilder<ReplacementSlot>();
        foreach (var module in ship.Modules)
        {
            int capacity = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex).DroneCapacity ?? 0;
            if (!IsHangarReady(module.ModuleId)) continue;
            for (int i = 0; i < capacity; i++)
                if (!wing.Any(d => !d.Destroyed && d.HangarModuleId == module.ModuleId && d.SlotIndex == i))
                    slots.Add(new(module.ModuleId, i));
        }
        var offers = ImmutableArray.CreateBuilder<ReplacementOffer>();
        foreach (var offer in services.DroneOffers.Concat(services.UpgradeOffers))
        {
            if (!offer.StationIds.Contains(station)) continue;
            bool equipment = services.UpgradeOffers.Contains(offer);
            int stock = Math.Max(0, offer.Stock - state.ServiceOfferUsage.GetValueOrDefault(station + "/" + offer.Id));
            var items = ImmutableArray.Create(offer.CatalogId);
            if (!equipment && _registry.TacticalBattle.Catalog is { } catalog)
            {
                var layout = catalog.Layouts[offer.CatalogId];
                items = new[] { layout.Class, layout.Armor, layout.Weapon, layout.Shield, layout.Module }
                    .Where(x => x is not null).Select(x => x!).Concat(layout.Engines).ToImmutableArray();
            }
            offers.Add(new(offer.Id, offer.CatalogId, equipment, offer.Price, stock, items,
                equipment ? [] : slots.ToImmutable(), stock == 0 || (!equipment && slots.Count == 0) ? CompanyServiceReasons.Unavailable : null));
        }
        return new(offers.ToImmutable(), lastId);
    }

    private void PrepareReplacement(DemoCampaignCommand command, ref DemoCampaignData candidate, ref string? reason, ref Action? commit)
    {
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (ship is null || !ship.IsDocked || ship.DockedStationObjectId is not { } station)
        { reason = CompanyServiceReasons.NotDocked; return; }
        var catalog = _registry.TacticalBattle.Catalog;
        if (candidate.CatalogFingerprint is null || _demoDefinitions.Services is not { } services || catalog is null)
        { reason = CompanyServiceReasons.Unavailable; return; }
        bool purchase = command is PurchaseDroneCommand;
        string offerId = purchase ? ((PurchaseDroneCommand)command).OfferId : ((EquipDroneCommand)command).OfferId;
        var offer = (purchase ? services.DroneOffers : services.UpgradeOffers).FirstOrDefault(o => o.Id == offerId && o.StationIds.Contains(station));
        if (offer is null) { reason = CompanyServiceReasons.Unavailable; return; }
        string key = station + "/" + offerId;
        int used = candidate.ServiceOfferUsage.GetValueOrDefault(key);
        if (used >= offer.Stock) { reason = CompanyServiceReasons.Unavailable; return; }
        if (PlayerCredits < offer.Price) { reason = CompanyServiceReasons.InsufficientFunds; return; }
        string shipId = ship.InitialMotion.ObjectId;
        var wing = GetOwnedWing(shipId);
        OwnedDroneData drone;
        long nextId = candidate.NextDroneSequence;
        if (command is PurchaseDroneCommand buy)
        {
            var offered = BuildReplacementProjection(candidate)!.Offers.First(o => o.Id == offerId);
            if (!offered.CompatibleSlots.Contains(new(buy.HangarModuleId, buy.SlotIndex)))
            { reason = CompanyServiceReasons.Unavailable; return; }
            var layout = catalog.Layouts[offer.CatalogId];
            var frame = catalog.Frames[layout.Class];
            if (layout.Weapon is null) { reason = CompanyServiceReasons.Unavailable; return; }
            string id;
            do { id = "demo-drone-" + nextId++; } while (wing.Any(d => d.DroneId == id));
            int armor = catalog.Armor[layout.Armor].Armor;
            int hull = layout.Hull ?? frame.Hull;
            drone = new(id, buy.HangarModuleId, buy.SlotIndex, "", offer.CatalogId, layout.Weapon, layout.Armor,
                layout.Shield, layout.Shield is null ? 0 : layout.ShieldCount ?? 1, frame.ShieldSlots,
                layout.Module, layout.Module is null ? 0 : catalog.Decoys[layout.Module].Charges, 0,
                hull, hull, armor, armor, 0, frame.AmmoCapacity, EngineIds: layout.Engines.ToArray(),
                ReplacesDroneId: wing.FirstOrDefault(d => d.Destroyed && d.HangarModuleId == buy.HangarModuleId && d.SlotIndex == buy.SlotIndex) is { } lost
                    ? lost.ReplacesDroneId ?? lost.DroneId : null);
            wing = wing.Add(drone);
        }
        else
        {
            var equip = (EquipDroneCommand)command;
            int index = -1;
            for (int i = 0; i < wing.Length; i++) if (wing[i].DroneId == equip.DroneId) { index = i; break; }
            if (index < 0 || wing[index].Destroyed || !IsHangarReady(wing[index].HangarModuleId))
            { reason = CompanyServiceReasons.InvalidTarget; return; }
            drone = wing[index];
            if (catalog.Weapons.Contains(offer.CatalogId))
            {
                if (drone.WeaponId == offer.CatalogId) { reason = CompanyServiceReasons.Unavailable; return; }
                drone = drone with { WeaponId = offer.CatalogId };
            }
            else if (catalog.Armor.Contains(offer.CatalogId))
            {
                if (drone.ArmorId == offer.CatalogId) { reason = CompanyServiceReasons.Unavailable; return; }
                int max = catalog.Armor[offer.CatalogId].Armor;
                drone = drone with { ArmorId = offer.CatalogId, Armor = Math.Max(0, max - (drone.MaxArmor - drone.Armor)), MaxArmor = max };
            }
            else if (catalog.Shields.Contains(offer.CatalogId) && drone.ShieldSlots > 0)
                drone = drone with { ShieldId = offer.CatalogId, ShieldCount = 1 };
            else { reason = CompanyServiceReasons.Unavailable; return; }
            wing = wing.SetItem(index, drone);
        }
        try { OwnedWingValidator.Validate(ship, _registry, wing); }
        catch (ScenarioException) { reason = CompanyServiceReasons.Unavailable; return; }
        if (!double.IsFinite(ComputeOwnedDroneMassKg(drone)) || ComputeOwnedDroneMassKg(drone) <= 0)
        { reason = CompanyServiceReasons.Unavailable; return; }
        var repair = candidate.Company?.StationRepair ?? new(Postings: []);
        var postings = repair.Postings.IsDefault ? ImmutableArray<CompanyServicePosting>.Empty : repair.Postings;
        candidate = candidate with { NextDroneSequence = nextId, ServiceOfferUsage = candidate.ServiceOfferUsage.SetItem(key, used + 1) };
        candidate = candidate with
        {
            Company = (candidate.Company ?? new()) with
            {
                Replacement = BuildReplacementProjection(candidate, wing, drone.DroneId),
                StationRepair = repair with { Postings = postings.Add(new(command.CommandId, purchase ? "drone" : "equipment", drone.DroneId, _clock.GameTimeMs, offer.Price)) }
            }
        };
        long credits = checked(PlayerCredits - offer.Price);
        int revision = checked(GetCarrierRevision(shipId) + 1);
        int carrierHull = GetCarrierHullCurrent(shipId, GetCarrierHullMax(shipId));
        commit = () => { PlayerCredits = credits; _ownedWing[shipId] = wing; _carrierHull[shipId] = new(carrierHull, revision); };
    }
}
