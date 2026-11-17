using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private DecoySupplyQuote? _activeDecoyQuote;

    private SupplySnapshot? BuildSupplyProjection(ImmutableArray<OwnedDroneData>? replacement = null, DecoySupplyQuote? quote = null)
    {
        if (_demoCampaign.CatalogFingerprint is null) return null;
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (ship is null) return null;
        long stock = 0;
        if (_registry.ItemTypes.Contains(TorpedoItemTypeId))
        {
            int index = _registry.ItemTypes.GetIndex(TorpedoItemTypeId);
            stock = ship.Modules.SelectMany(m => m.Cargo).Where(c => c.ItemTypeIndex == index).Sum(c => c.Quantity);
        }
        var drones = (replacement ?? GetOwnedWing(ship.InitialMotion.ObjectId)).Where(d => !d.Destroyed)
            .Select(d => new DroneSupply(d.DroneId, d.AmmoCurrent, d.AmmoCapacity, d.DecoyCharges, d.DecoyMaxCharges)).ToImmutableArray();
        bool available = ship.IsDocked && ship.DockedStationObjectId is { } station &&
            _demoDefinitions.Services?.StationIds.Contains(station, StringComparer.Ordinal) == true;
        return new(stock, drones, checked(stock + drones.Sum(d => (long)d.AmmoCurrent)),
            available ? _demoDefinitions.Services!.RepairRates.DecoyCharge : null, quote,
            available ? null : CompanyServiceReasons.Unavailable);
    }

    private void PrepareSupply(DemoCampaignCommand command, ref DemoCampaignData candidate, ref string? reason, ref Action? commit)
    {
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (ship is null || !ship.IsDocked || ship.DockedStationObjectId is null)
        { reason = CompanyServiceReasons.NotDocked; return; }
        var services = _demoDefinitions.Services;
        if (_demoCampaign.CatalogFingerprint is null || services is null || !services.StationIds.Contains(ship.DockedStationObjectId))
        { reason = CompanyServiceReasons.Unavailable; return; }
        string target = command is DecoyQuoteRequest request ? request.DroneId : ((ResupplyDecoyCommand)command).DroneId;
        int quantity = command is DecoyQuoteRequest query ? query.Quantity : ((ResupplyDecoyCommand)command).Quantity;
        string shipId = ship.InitialMotion.ObjectId;
        var wing = GetOwnedWing(shipId);
        int index = -1;
        for (int i = 0; i < wing.Length; i++) if (wing[i].DroneId == target) { index = i; break; }
        if (index < 0) { reason = CompanyServiceReasons.InvalidTarget; return; }
        var drone = wing[index];
        if (drone.Destroyed) { reason = CompanyServiceReasons.Destroyed; return; }
        if (!IsHangarReady(drone.HangarModuleId) || quantity <= 0 || quantity > drone.DecoyMaxCharges - drone.DecoyCharges)
        { reason = CompanyServiceReasons.Unavailable; return; }
        try
        {
            long total = checked(quantity * services.RepairRates.DecoyCharge);
            int revision = GetCarrierRevision(shipId);
            if (command is DecoyQuoteRequest)
            {
                var quote = new DecoySupplyQuote(command.CommandId, ship.DockedStationObjectId, target,
                    checked(candidate.Revision + 1), revision, checked(_clock.GameTimeMs + 3600000), quantity, drone.DecoyCharges, total);
                candidate = candidate with { Company = (candidate.Company ?? new()) with { Supply = BuildSupplyProjection(quote: quote) } };
                commit = () => _activeDecoyQuote = quote;
                return;
            }
            var confirm = (ResupplyDecoyCommand)command;
            var active = _activeDecoyQuote;
            if (active is null || active.Id != confirm.QuoteId || active.DroneId != target || active.Quantity != quantity ||
                active.Revision != command.ExpectedRevision || active.CarrierRevision != revision || active.Before != drone.DecoyCharges ||
                active.Total != total || active.ExpiresAt <= _clock.GameTimeMs || active.StationId != ship.DockedStationObjectId)
            { reason = CompanyServiceReasons.InvalidQuote; return; }
            if (PlayerCredits < total) { reason = CompanyServiceReasons.InsufficientFunds; return; }
            var updated = wing.SetItem(index, drone with { DecoyCharges = drone.DecoyCharges + quantity });
            var repair = candidate.Company?.StationRepair ?? new(Postings: []);
            var postings = repair.Postings.IsDefault ? ImmutableArray<CompanyServicePosting>.Empty : repair.Postings;
            candidate = candidate with
            {
                Company = (candidate.Company ?? new()) with
                {
                    Supply = BuildSupplyProjection(updated),
                    StationRepair = repair with { Postings = postings.Add(new(command.CommandId, "decoy", target, _clock.GameTimeMs, total)) }
                }
            };
            long credits = checked(PlayerCredits - total);
            int nextRevision = checked(revision + 1);
            int hull = GetCarrierHullCurrent(shipId, GetCarrierHullMax(shipId));
            commit = () =>
            {
                PlayerCredits = credits;
                _ownedWing[shipId] = updated;
                _carrierHull[shipId] = new(hull, nextRevision);
                _activeDecoyQuote = null;
            };
        }
        catch (OverflowException) { reason = CompanyServiceReasons.Unavailable; }
    }
}
