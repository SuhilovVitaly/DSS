using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private RecruitmentSnapshot? BuildRecruitmentProjection(DemoCampaignData state, SpaceObjectRuntime? updatedShip = null,
        ImmutableArray<OwnedDroneData>? updatedWing = null)
    {
        if (state.CatalogFingerprint is null || _demoDefinitions.Services?.Recruitment is not { } recruitment) return null;
        var ship = updatedShip ?? _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (ship is null) return null;
        var wing = updatedWing ?? GetOwnedWing(ship.InitialMotion.ObjectId);
        var pilots = ship.Crew.Where(c => string.Equals(c.Role, "Pilot", StringComparison.OrdinalIgnoreCase))
            .Select(c => new PilotAssignment(c.Id, c.DisplayName, wing.FirstOrDefault(d => !d.Destroyed && d.CrewId == c.Id)?.DroneId)).ToImmutableArray();
        var drones = wing.Where(d => !d.Destroyed && IsHangarReady(d.HangarModuleId)).Select(d => d.DroneId).ToImmutableArray();
        int free = BuildCabinReservationProjection(ship).FreeCabins;
        if (!ship.IsDocked || ship.DockedStationObjectId is not { } station || !_demoDefinitions.Services.StationIds.Contains(station))
            return new([], pilots, drones, free, CompanyServiceReasons.NotDocked);
        long bucket = _clock.GameTimeMs / recruitment.RefreshGameTimeMs;
        var offers = ImmutableArray.CreateBuilder<RecruitmentOffer>();
        string[] names = ["Anna Keller", "Mira Novak", "Elena Voss", "Lena Weber", "Nina Falk", "Sofia Berg"];
        for (int i = 0; i < 3; i++)
        {
            string id = $"recruit-{station}-{bucket}-{i}";
            string crewId = "pilot-" + id;
            if (state.HiredOfferIds.Contains(id) || ship.Crew.Any(c => c.Id == crewId) || state.PilotMemorial.Any(m => m.Pilot.Id == crewId)) continue;
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{_demoDefinitions.Cluster!.Seed}/{id}"));
            int range = recruitment.SkillMax - recruitment.SkillMin + 1;
            offers.Add(new(id, crewId, names[bytes[0] % names.Length], recruitment.Portraits[bytes[1] % recruitment.Portraits.Length],
                recruitment.SkillMin + bytes[2] % range, recruitment.SkillMin + bytes[3] % range, recruitment.SkillMin + bytes[4] % range,
                recruitment.Price, recruitment.DailyWage, checked((bucket + 1) * recruitment.RefreshGameTimeMs)));
        }
        return new(offers.ToImmutable(), pilots, drones, free);
    }

    private void PrepareRecruitment(DemoCampaignCommand command, ref DemoCampaignData candidate, ref string? reason, ref Action? commit)
    {
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (ship is null || !ship.IsDocked || ship.DockedStationObjectId is null)
        { reason = CompanyServiceReasons.NotDocked; return; }
        var projection = BuildRecruitmentProjection(candidate);
        if (projection is null || projection.Reason is not null) { reason = CompanyServiceReasons.Unavailable; return; }
        string shipId = ship.InitialMotion.ObjectId;
        var wing = GetOwnedWing(shipId);
        var changed = ship;
        long credits = PlayerCredits;
        if (command is HirePilotCommand hire)
        {
            var offer = projection.Offers.FirstOrDefault(o => o.Id == hire.OfferId);
            if (offer is null || offer.ExpiresAt <= _clock.GameTimeMs || projection.FreeCabins <= 0)
            { reason = CompanyServiceReasons.Unavailable; return; }
            if (credits < offer.Fee) { reason = CompanyServiceReasons.InsufficientFunds; return; }
            credits -= offer.Fee;
            changed = ship with
            {
                Crew = ship.Crew.Add(new(offer.CrewId, offer.Name, offer.Accuracy, offer.Maneuver,
                "Female", offer.Portrait, "Pilot", offer.Initiative))
            };
            var repair = candidate.Company?.StationRepair ?? new(Postings: []);
            var postings = repair.Postings.IsDefault ? ImmutableArray<CompanyServicePosting>.Empty : repair.Postings;
            candidate = candidate with
            {
                HiredOfferIds = candidate.HiredOfferIds.Add(offer.Id),
                NextCrewSequence = checked(candidate.NextCrewSequence + 1),
                Employment = candidate.Employment.Add(new(offer.CrewId, offer.DailyWage, _clock.GameTimeMs)),
                Company = (candidate.Company ?? new()) with
                {
                    StationRepair = repair with
                    { Postings = postings.Add(new(command.CommandId, "hire", offer.CrewId, _clock.GameTimeMs, offer.Fee)) }
                }
            };
        }
        else if (command is AssignPilotCommand assign)
        {
            if (!projection.Pilots.Any(p => p.CrewId == assign.CrewId) || !projection.AvailableDroneIds.Contains(assign.DroneId))
            { reason = CompanyServiceReasons.InvalidTarget; return; }
            wing = wing.Select(d => !d.Destroyed && d.DroneId == assign.DroneId ? d with { CrewId = assign.CrewId }
                : !d.Destroyed && d.CrewId == assign.CrewId ? d with { CrewId = "" } : d).ToImmutableArray();
        }
        try { OwnedWingValidator.Validate(changed, _registry, wing); }
        catch (ScenarioException) { reason = CompanyServiceReasons.Unavailable; return; }
        candidate = candidate with { Company = (candidate.Company ?? new()) with { Recruitment = BuildRecruitmentProjection(candidate, changed, wing) } };
        int revision = checked(GetCarrierRevision(shipId) + 1);
        int hull = GetCarrierHullCurrent(shipId, GetCarrierHullMax(shipId));
        int objectIndex = _objects.IndexOf(ship);
        commit = () => { PlayerCredits = credits; _objects[objectIndex] = changed; _ownedWing[shipId] = wing; _carrierHull[shipId] = new(hull, revision); };
    }
}
