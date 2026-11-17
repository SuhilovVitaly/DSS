using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private RepairQuote? _activeRepairQuote;
    private int _repairCarrierRevision;

    private void PrepareStationRepair(DemoCampaignCommand command, ref DemoCampaignData candidate,
        ref string? reason, ref Action? commit)
    {
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (ship is null || !ship.IsDocked || ship.DockedStationObjectId is null)
        { reason = CompanyServiceReasons.NotDocked; return; }
        var services = _demoDefinitions.Services;
        if (_demoCampaign.CatalogFingerprint is null || services is null ||
            !services.StationIds.Contains(ship.DockedStationObjectId, StringComparer.Ordinal))
        { reason = CompanyServiceReasons.Unavailable; return; }
        string targetId = command is RepairQuoteRequest request ? request.TargetId : ((RepairCommand)command).TargetId;
        string shipId = ship.InitialMotion.ObjectId;
        var wing = GetOwnedWing(shipId);
        int index = -1;
        for (int i = 0; i < wing.Length; i++) if (wing[i].DroneId == targetId) { index = i; break; }
        bool carrier = targetId == shipId;
        if (!carrier && index < 0) { reason = CompanyServiceReasons.InvalidTarget; return; }
        var drone = carrier ? null : wing[index];
        if (ship.IsDestroyed || drone?.Destroyed == true) { reason = CompanyServiceReasons.Destroyed; return; }
        int hullMax = carrier ? GetCarrierHullMax(shipId) : drone!.MaxHull;
        var before = carrier ? new RepairCondition(GetCarrierHullCurrent(shipId, hullMax), 0, 0, false)
            : new RepairCondition(drone!.CurrentHull, drone.Armor, drone.EngineFactors?.Count(f => f < 1) ?? 0, drone.Jammed);
        var lines = ImmutableArray.CreateBuilder<RepairLine>();
        void Line(string kind, long quantity, long price)
        { if (quantity > 0) lines.Add(new(kind, quantity, price, checked(quantity * price))); }
        try
        {
            Line("hull", hullMax - before.Hull, services.RepairRates.Hull);
            Line("armor", carrier ? 0 : drone!.MaxArmor - before.Armor, services.RepairRates.Armor);
            Line("engine", before.DamagedEngines, services.RepairRates.Engine);
            Line("jam", before.Jammed ? 1 : 0, services.RepairRates.Jam);
            long total = lines.Sum(l => l.Total);
            if (lines.Count == 0) { reason = CompanyServiceReasons.NoDamage; return; }
            var state = candidate.Company?.StationRepair ?? new(Postings: []);
            if (command is RepairQuoteRequest)
            {
                var quote = new RepairQuote(command.CommandId, ship.DockedStationObjectId, targetId,
                    checked(candidate.Revision + 1), checked(_clock.GameTimeMs + 3600000), total, lines.ToImmutable(), before);
                candidate = candidate with { Company = (candidate.Company ?? new()) with { StationRepair = state with { Quote = quote, Reason = null } } };
                int revision = GetCarrierRevision(shipId);
                commit = () => { _activeRepairQuote = quote; _repairCarrierRevision = revision; };
                return;
            }
            var repair = (RepairCommand)command;
            var active = _activeRepairQuote;
            if (active is null || active.Id != repair.QuoteId || active.TargetId != targetId ||
                active.StationId != ship.DockedStationObjectId || active.Revision != command.ExpectedRevision ||
                active.ExpiresAt <= _clock.GameTimeMs || active.Before != before || active.Total != total ||
                _repairCarrierRevision != GetCarrierRevision(shipId))
            { reason = CompanyServiceReasons.InvalidQuote; return; }
            if (PlayerCredits < total) { reason = CompanyServiceReasons.InsufficientFunds; return; }
            long credits = checked(PlayerCredits - total);
            int nextRevision = checked(GetCarrierRevision(shipId) + 1);
            var after = new RepairCondition(hullMax, carrier ? 0 : drone!.MaxArmor, 0, false);
            var result = new RepairResult(targetId, before, after, command.CommandId, total);
            var postings = state.Postings.IsDefault ? ImmutableArray<CompanyServicePosting>.Empty : state.Postings;
            candidate = candidate with
            {
                Company = (candidate.Company ?? new()) with
                {
                    StationRepair = state with
                    {
                        Quote = null,
                        LastResult = result,
                        Reason = null,
                        Postings = postings.Add(new(command.CommandId, "repair", targetId, _clock.GameTimeMs, total))
                    }
                }
            };
            var newWing = carrier ? wing : wing.SetItem(index, drone! with
            {
                CurrentHull = hullMax,
                Armor = drone.MaxArmor,
                Jammed = false,
                Disabled = false,
                EngineFactors = drone.EngineIds?.Select(_ => 1.0).ToArray()
            });
            int newHull = carrier ? hullMax : GetCarrierHullCurrent(shipId, GetCarrierHullMax(shipId));
            commit = () =>
            {
                PlayerCredits = credits;
                _ownedWing[shipId] = newWing;
                _carrierHull[shipId] = new(newHull, nextRevision);
                _activeRepairQuote = null;
            };
        }
        catch (OverflowException) { reason = CompanyServiceReasons.Unavailable; }
    }
}
