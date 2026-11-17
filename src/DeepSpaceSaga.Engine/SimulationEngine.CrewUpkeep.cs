using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private const long CrewDayMs = 86400000;

    private void InitializeCrewUpkeep(long time)
    {
        if (_demoCampaign.CatalogFingerprint is null || _demoDefinitions.Services?.Recruitment is not { } terms) return;
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (ship is null) return;
        var employment = _demoCampaign.Employment;
        foreach (var crew in ship.Crew)
            if (!employment.Any(e => e.CrewId == crew.Id)) employment = employment.Add(new(crew.Id, terms.DailyWage, time));
        var company = _demoCampaign.Company ?? new();
        _demoCampaign = _demoCampaign with { Employment = employment, Company = company with
            { Upkeep = company.Upkeep ?? new(0, 0, 0, checked((time / CrewDayMs + 1) * CrewDayMs), 0, 0, 0, time / CrewDayMs) } };
    }

    private CrewUpkeepSnapshot? BuildUpkeepProjection(DemoCampaignData state)
    {
        if (state.CatalogFingerprint is null || state.Company?.Upkeep is not { } ledger) return null;
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (ship is null) return ledger;
        long food = 0;
        if (_registry.ItemTypes.Contains("item.food-rations"))
        {
            int index = _registry.ItemTypes.GetIndex("item.food-rations");
            food = ship.Modules.SelectMany(m => m.Cargo).Where(c => c.ItemTypeIndex == index).Sum(c => c.Quantity);
        }
        return ledger with { PeopleFed = ship.IsDestroyed ? 0 : ship.Crew.Length + (ship.Passengers.IsDefault ? 0 : ship.Passengers.Length),
            FoodRemaining = food, MissingRations = _economyTime.MissingRations };
    }

    partial void TickDemoCampaignSections(long gameTimeMs, ref DemoCampaignData candidate)
    {
        if (candidate.CatalogFingerprint is null || candidate.Company?.Upkeep is not { } ledger) return;
        long day = gameTimeMs / CrewDayMs;
        if (day <= ledger.LastProcessedDay) return;
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        long due = 0;
        if (ship is not null && !ship.IsDestroyed)
            foreach (var employment in candidate.Employment)
                if (ship.Crew.Any(c => c.Id == employment.CrewId))
                    due = checked(due + checked(Math.Max(0, day - Math.Max(ledger.LastProcessedDay, employment.HiredAt / CrewDayMs)) * employment.DailyWage));
        long accrued = checked(ledger.Accrued + due);
        long available = Math.Max(0, PlayerCredits);
        long payment = Math.Min(available, checked(ledger.Outstanding + due));
        long paid = checked(ledger.Paid + payment);
        candidate = candidate with { Revision = checked(candidate.Revision + 1), CommitSequence = checked(candidate.CommitSequence + 1),
            Company = candidate.Company with { Upkeep = ledger with { Accrued = accrued, Paid = paid, Outstanding = accrued - paid,
                LastProcessedDay = day, NextDueAt = checked((day + 1) * CrewDayMs) } } };
        DemoCampaignData.Validate(candidate, gameTimeMs);
        PlayerCredits -= payment;
    }

    private void PrepareCrewDebt(PayCrewDebtCommand command, ref DemoCampaignData candidate, ref string? reason, ref Action? commit)
    {
        var ledger = candidate.Company?.Upkeep;
        if (candidate.CatalogFingerprint is null || ledger is null || command.Amount <= 0 || command.Amount > ledger.Outstanding)
        { reason = CompanyServiceReasons.Unavailable; return; }
        if (command.Amount > PlayerCredits) { reason = CompanyServiceReasons.InsufficientFunds; return; }
        var paid = ledger with { Paid = checked(ledger.Paid + command.Amount), Outstanding = ledger.Outstanding - command.Amount };
        candidate = candidate with { Company = candidate.Company! with { Upkeep = paid } };
        long credits = PlayerCredits - command.Amount;
        commit = () => PlayerCredits = credits;
    }
}
