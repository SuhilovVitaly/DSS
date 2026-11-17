using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Content;

public sealed record DemoClusterDefinition(int SchemaVersion, ImmutableArray<string> StationIds,
    ImmutableArray<string> Roles, ulong Seed, string StartStationId);

public sealed record DemoRepairRates(long Hull, long Armor, long Engine, long Jam, long DecoyCharge);
public sealed record DemoOffer(string Id, string CatalogId, long Price, int Stock, ImmutableArray<string> StationIds);
public sealed record DemoRecruitment(ImmutableArray<string> Portraits, int SkillMin, int SkillMax, long Price, long RefreshGameTimeMs);
public sealed record DemoServicesDefinition(int SchemaVersion, ImmutableArray<string> StationIds,
    DemoRepairRates RepairRates, DemoRecruitment? Recruitment,
    ImmutableArray<DemoOffer> DroneOffers, ImmutableArray<DemoOffer> UpgradeOffers);
// EP-0034 US-0006: station job board. Money is whole Tokens, factors are permille (1000 = 1.0), times are calendar ms.
public sealed record DemoJobLimits(int Offers, long PublicationGameTimeMs, long ExpiryGameTimeMs, int PassengerMax,
    int CargoTonnesMin, int CargoTonnesMax);
public sealed record DemoJobRates(long Base, long PerPassenger, long PerCargoTonne, long PerDistanceKm, int UrgencyPermille);
public sealed record DemoJobDeadlines(int OrdinaryPermille, int UrgentPermille, int CargoPermille, long MinimumGameTimeMs);
public sealed record DemoJobRisk(string RiskProfileId, string Band, int Permille);
public sealed record DemoJobRoleFilter(string Role, int PassengerWeight, int UrgentPassengerWeight, int CargoWeight);
public sealed record DemoJobFirstOffer(string Type, int Count);
public sealed record DemoJobsDefinition(int SchemaVersion, DemoJobLimits Limits, DemoJobRates Rates, DemoJobDeadlines Deadlines,
    ImmutableArray<DemoJobRisk> Risk, ImmutableArray<string> CargoItemTypeIds, ImmutableArray<DemoJobRoleFilter> RoleFilters,
    DemoJobFirstOffer FirstOffer);
public sealed record DemoRegion(double X, double Y, double Radius);
public sealed record DemoPirateGroup(string Id, string LoadoutId, double X, double Y, double SightRadius, double PatrolSpeed);
public sealed record DemoPursuit(double CaptureRadius, double EscapeRadius, long EscapeMotionTimeMs);
public sealed record DemoLootRange(string GroupId, int TorpedoesMin, int TorpedoesMax, int ScrapMin, int ScrapMax);
public sealed record DemoEncountersDefinition(int SchemaVersion, ImmutableArray<DemoPirateGroup> Groups,
    DemoRegion Region, DemoPursuit Pursuit, ImmutableArray<DemoLootRange> Loot);
public sealed record DemoExperienceAwards(int Participation, int VictoryBonus);
public sealed record DemoSpecialization(string Id, string Skill, int Bonus);
public sealed record DemoProgressionDefinition(int SchemaVersion, DemoExperienceAwards Awards,
    ImmutableArray<int> Thresholds, ImmutableArray<DemoSpecialization> Specializations);

/// <summary>Frozen content used by this epic. Further sections are owned by their feature epics.</summary>
public sealed record DemoCampaignDefinitions(DemoClusterDefinition? Cluster, string? Fingerprint,
    DemoServicesDefinition? Services = null, DemoJobsDefinition? Jobs = null,
    DemoEncountersDefinition? Encounters = null, DemoProgressionDefinition? Progression = null)
{
    // Save22 originally hashed the shipped cluster bytes. Accept either checkout EOL,
    // only while the semantic cluster is still the original contract.
    private bool IsInitialLegacyFingerprint(string saved) => Fingerprint == "ED0B0694F34B4DA03E78F3465938AFDB61F0BECEF1730E8F6F9A8622F240248B" &&
        (saved == "DD3D6B26BC6E7EE55064EAE4735CED0335328C42F4886C442B9E91FF5A6899B3" || saved == "4D897CEA83F2EBF04BD11DC0AA5FEB9CF0049BDE58A5965ACB332DBA5F29FA8B");

    public string? LegacyClusterFingerprint { get; init; }

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static DemoCampaignDefinitions Load(string contentRoot)
    {
        string path = Path.Combine(contentRoot, "Data", "Demo", "cluster.json");
        try
        {
            var fingerprint = new StringBuilder();
            T? Read<T>(string name) where T : class
            {
                string file = Path.Combine(contentRoot, "Data", "Demo", name + ".json");
                if (!File.Exists(file)) return null;
                byte[] bytes = File.ReadAllBytes(file);
                if (name == "cluster") fingerprint.Append(name).Append(':').Append(Convert.ToHexString(SHA256.HashData(bytes))).Append(';');
                return JsonSerializer.Deserialize<T>(bytes, Options) ?? throw new ScenarioException($"Missing {name} definition.");
            }
            var cluster = Read<DemoClusterDefinition>("cluster");
            if (cluster is not null && (cluster.SchemaVersion != 1 || cluster.StationIds.IsDefault || cluster.Roles.IsDefault ||
                cluster.StationIds.Length != 5 || cluster.Roles.Length != cluster.StationIds.Length ||
                cluster.Roles.Distinct(StringComparer.Ordinal).Count() != 5 ||
                cluster.StationIds.Any(string.IsNullOrWhiteSpace) || cluster.Roles.Any(string.IsNullOrWhiteSpace) ||
                cluster.StationIds.Distinct(StringComparer.Ordinal).Count() != cluster.StationIds.Length ||
                !cluster.StationIds.Contains(cluster.StartStationId, StringComparer.Ordinal)))
                throw new ScenarioException("Invalid demo cluster version, station identities or roles.");
            var services = Read<DemoServicesDefinition>("services");
            var jobs = Read<DemoJobsDefinition>("jobs");
            var encounters = Read<DemoEncountersDefinition>("encounters");
            var progression = Read<DemoProgressionDefinition>("progression");
            var definitions = new DemoCampaignDefinitions(cluster,
                cluster is null ? null : Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(cluster, Options))),
                services, jobs, encounters, progression)
            { LegacyClusterFingerprint = fingerprint.Length == 0 ? null : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint.ToString()))) };
            definitions.Validate();
            if (services?.Recruitment is { } recruitment && recruitment.Portraits.Any(p => !File.Exists(Path.Combine(contentRoot, p))))
                throw new ScenarioException("Demo recruitment portrait does not exist.");
            return definitions;
        }
        catch (JsonException error)
        {
            throw new ScenarioException($"Invalid demo content: {path}: {error.Message}", error);
        }
    }

    internal void ValidateReferences(GameDataRegistry registry)
    {
        var catalog = registry.TacticalBattle.Catalog;
        if (Services is { } services)
        {
            foreach (var offer in services.DroneOffers)
                if (catalog is null || !catalog.Layouts.Contains(offer.CatalogId))
                    throw new ScenarioException("Unknown demo drone loadout: " + offer.CatalogId);
            foreach (var offer in services.UpgradeOffers)
                if (catalog is null || !(catalog.Weapons.Contains(offer.CatalogId) || catalog.Armor.Contains(offer.CatalogId) || catalog.Shields.Contains(offer.CatalogId)))
                    throw new ScenarioException("Unknown demo upgrade: " + offer.CatalogId);
        }
        if (Jobs is { } jobs)
            foreach (var itemTypeId in jobs.CargoItemTypeIds)
                if (!registry.ItemTypes.Contains(itemTypeId) ||
                    registry.ItemTypes.GetDefinition(registry.ItemTypes.GetIndex(itemTypeId)) is not { UnitMassKg: > 0, StorageKind: ItemStorageKind.Cargo })
                    throw new ScenarioException("Unknown demo job cargo: " + itemTypeId);
        if (Encounters is { } encounters)
            foreach (var group in encounters.Groups)
                if (catalog is null || !catalog.Scenarios.Contains(group.LoadoutId))
                    throw new ScenarioException("Unknown demo encounter composition: " + group.LoadoutId);
    }

    private void Validate()
    {
        static void Require(bool valid, string section)
        {
            if (!valid) throw new ScenarioException("Invalid demo " + section + ".");
        }
        bool Stations(ImmutableArray<string> ids) => !ids.IsDefault && Cluster is not null &&
            ids.Distinct(StringComparer.Ordinal).Count() == ids.Length && ids.All(id => Cluster.StationIds.Contains(id, StringComparer.Ordinal));
        bool Offers(ImmutableArray<DemoOffer> offers) => !offers.IsDefault && offers.All(o => o is not null &&
            !string.IsNullOrWhiteSpace(o.Id) && !string.IsNullOrWhiteSpace(o.CatalogId) && o.Price >= 0 && o.Stock >= 0 && Stations(o.StationIds)) &&
            offers.Select(o => o.Id).Distinct(StringComparer.Ordinal).Count() == offers.Length;
        if (Services is { } s)
        {
            Require(s.SchemaVersion == 1 && Stations(s.StationIds) && s.RepairRates is { Hull: >= 0, Armor: >= 0, Engine: >= 0, Jam: >= 0, DecoyCharge: >= 0 } &&
                Offers(s.DroneOffers) && Offers(s.UpgradeOffers), "services");
            if (s.Recruitment is { } recruitment)
                Require(!recruitment.Portraits.IsDefaultOrEmpty && recruitment.Portraits.All(p => !string.IsNullOrWhiteSpace(p) && !Path.IsPathRooted(p) && !p.Split('/', '\\').Contains("..")) &&
                    recruitment.SkillMin >= 0 && recruitment.SkillMax >= recruitment.SkillMin && recruitment.SkillMax <= 100 &&
                    recruitment.Price >= 0 && recruitment.RefreshGameTimeMs > 0, "recruitment");
        }
        if (Jobs is { } j)
        {
            Require(j.SchemaVersion == 1 && j.Limits is { Offers: > 0 and <= 12, PublicationGameTimeMs: > 0, PassengerMax: > 0, CargoTonnesMin: > 0 } &&
                j.Limits.ExpiryGameTimeMs >= j.Limits.PublicationGameTimeMs && j.Limits.CargoTonnesMax >= j.Limits.CargoTonnesMin &&
                j.Rates is { Base: >= 0, PerPassenger: >= 0, PerCargoTonne: >= 0, PerDistanceKm: >= 0, UrgencyPermille: >= 1000 } &&
                j.Deadlines is { OrdinaryPermille: >= 1000, UrgentPermille: >= 1000, CargoPermille: >= 1000, MinimumGameTimeMs: > 0 } &&
                j.Deadlines.UrgentPermille <= j.Deadlines.OrdinaryPermille, "jobs");
            Require(!j.Risk.IsDefaultOrEmpty && j.Risk.All(r => r is not null && !string.IsNullOrWhiteSpace(r.RiskProfileId) &&
                    DeepSpaceSaga.Contracts.StationJobRiskBands.IsKnown(r.Band) && r.Permille >= 1000) &&
                j.Risk.Select(r => r.RiskProfileId).Distinct(StringComparer.Ordinal).Count() == j.Risk.Length, "jobs risk");
            Require(!j.CargoItemTypeIds.IsDefault && j.CargoItemTypeIds.All(id => !string.IsNullOrWhiteSpace(id)) &&
                j.CargoItemTypeIds.Distinct(StringComparer.Ordinal).Count() == j.CargoItemTypeIds.Length, "jobs cargo");
            Require(!j.RoleFilters.IsDefault && Cluster is not null && j.RoleFilters.All(f => f is not null &&
                    Cluster.Roles.Contains(f.Role, StringComparer.Ordinal) && f.PassengerWeight >= 0 && f.UrgentPassengerWeight >= 0 &&
                    f.CargoWeight >= 0 && (f.CargoWeight == 0 || j.CargoItemTypeIds.Length > 0) &&
                    checked(f.PassengerWeight + f.UrgentPassengerWeight + f.CargoWeight) > 0) &&
                j.RoleFilters.Select(f => f.Role).Distinct(StringComparer.Ordinal).Count() == j.RoleFilters.Length, "jobs role filters");
            Require(j.FirstOffer is { Count: > 0 } && DeepSpaceSaga.Contracts.StationJobTypes.CarriesPassengers(j.FirstOffer.Type) &&
                j.FirstOffer.Count <= j.Limits.PassengerMax &&
                j.RoleFilters.Any(f => f.Role == Cluster!.Roles[Cluster.StationIds.IndexOf(Cluster.StartStationId)]), "jobs first offer");
        }
        if (Encounters is { } e)
        {
            Require(e.SchemaVersion == 1 && e.Region is { Radius: > 0 } && double.IsFinite(e.Region.X) && double.IsFinite(e.Region.Y) && double.IsFinite(e.Region.Radius) &&
                e.Pursuit is { CaptureRadius: > 0, EscapeRadius: > 0, EscapeMotionTimeMs: > 0 } && double.IsFinite(e.Pursuit.CaptureRadius) && double.IsFinite(e.Pursuit.EscapeRadius), "encounters geometry");
            Require(!e.Groups.IsDefault && e.Groups.All(g => g is not null && !string.IsNullOrWhiteSpace(g.Id) && !string.IsNullOrWhiteSpace(g.LoadoutId) &&
                double.IsFinite(g.X) && double.IsFinite(g.Y) && double.IsFinite(g.SightRadius) && g.SightRadius > 0 && double.IsFinite(g.PatrolSpeed) && g.PatrolSpeed >= 0) &&
                e.Groups.Select(g => g.Id).Distinct(StringComparer.Ordinal).Count() == e.Groups.Length, "encounter groups");
            Require(!e.Loot.IsDefault && e.Loot.All(l => l is not null && e.Groups.Any(g => g.Id == l.GroupId) &&
                l.TorpedoesMin >= 0 && l.TorpedoesMax >= l.TorpedoesMin && l.ScrapMin >= 0 && l.ScrapMax >= l.ScrapMin), "loot");
        }
        if (Progression is { } p)
            Require(p.SchemaVersion == 1 && p.Awards is { Participation: >= 0, VictoryBonus: >= 0 } && !p.Thresholds.IsDefaultOrEmpty &&
                p.Thresholds.All(t => t > 0) && p.Thresholds.SequenceEqual(p.Thresholds.Distinct().Order()) && !p.Specializations.IsDefault &&
                p.Specializations.All(s => s is not null && !string.IsNullOrWhiteSpace(s.Id) && !string.IsNullOrWhiteSpace(s.Skill) && s.Bonus >= 0) &&
                p.Specializations.Select(s => s.Id).Distinct(StringComparer.Ordinal).Count() == p.Specializations.Length, "progression");
    }

    internal void ValidateWorld(ScenarioFile scenario)
    {
        bool demo = scenario.Metadata.ScenarioId == "demo" || scenario.GameState.DemoCampaign?.CatalogFingerprint is not null;
        if (!demo) return;
        if (Cluster is null) throw new ScenarioException("Demo campaign requires Data/Demo/cluster.json.");
        if (scenario.GameState.MasterSeed != Cluster.Seed)
            throw new ScenarioException("Demo world seed does not match the frozen cluster.");
        if (scenario.GameState.DemoCampaign?.CatalogFingerprint is { } saved && saved != Fingerprint && saved != LegacyClusterFingerprint && !IsInitialLegacyFingerprint(saved))
            throw new ScenarioException("incompatible_demo_catalog: saved campaign was not modified.");
        var stations = scenario.GameState.SpaceObjects.Where(o => o.ObjectType == "Station")
            .Select(o => o.ObjectId).ToHashSet(StringComparer.Ordinal);
        if (!stations.SetEquals(Cluster.StationIds))
            throw new ScenarioException("Demo world stations do not match the frozen cluster.");
    }
}
