using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Content;

public sealed record DemoPirateProfile(string SignatureTypeId, int DroneCount, double SpeedFactor,
    string CarrierLayoutId, string SensorModuleTypeId);
public sealed record DemoPursuit(double CaptureRadius, double StartSafeRadius, double SpawnStationRadius, double PatrolRadius);
public sealed record DemoEncountersDefinition(int SchemaVersion, int GroupCount, DemoRegion Region, DemoPursuit Pursuit,
    ImmutableArray<DemoPirateProfile> Profiles, ImmutableArray<string> DroneLayouts,
    int LairWeight = 45, int TancredWeight = 45, int SquadWeight = 10, ImmutableArray<string> SquadMembers = default)
{
    internal void Validate()
    {
        if (SchemaVersion != 2 || GroupCount != 4 || Region is not { Radius: > 0 } ||
            !double.IsFinite(Region.X) || !double.IsFinite(Region.Y) || !double.IsFinite(Region.Radius) ||
            Pursuit is not { CaptureRadius: 200, StartSafeRadius: 8000, SpawnStationRadius: 4000, PatrolRadius: > 0 } ||
            !double.IsFinite(Pursuit.PatrolRadius) || LairWeight != 45 || TancredWeight != 45 || SquadWeight != 10)
            throw new ScenarioException("Invalid demo encounter geometry or initial distribution.");
        if (Profiles.IsDefault || Profiles.Length != 2 || Profiles.Any(p => p is null ||
            string.IsNullOrWhiteSpace(p.CarrierLayoutId) || string.IsNullOrWhiteSpace(p.SensorModuleTypeId)) ||
            Profiles.Count(p => p.SignatureTypeId == PirateSignatureTypes.Lair && p.DroneCount == 6 && p.SpeedFactor == 1.1) != 1 ||
            Profiles.Count(p => p.SignatureTypeId == PirateSignatureTypes.Tancred && p.DroneCount == 10 && p.SpeedFactor == 0.8) != 1 ||
            DroneLayouts.IsDefault || !DroneLayouts.SequenceEqual(new[] { "light.sword.shield", "light.spear", "heavy.sword.shield", "heavy.spear.shield" }) ||
            SquadMembers.IsDefault || !SquadMembers.SequenceEqual(new[] { "lair", "lair" }))
            throw new ScenarioException("Invalid pirate profiles or independent class/weapon pool.");
    }

    internal void ValidateReferences(GameDataRegistry registry)
    {
        Validate();
        var catalog = registry.TacticalBattle.Catalog;
        foreach (var profile in Profiles)
        {
            if (catalog is null || !catalog.Layouts.Contains(profile.CarrierLayoutId) ||
                catalog.Layouts[profile.CarrierLayoutId].Class != "carrier" ||
                !registry.ModuleTypes.Contains(profile.SensorModuleTypeId) ||
                registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex(profile.SensorModuleTypeId)) is not
                    { CategoryTypeId: "module.sensor", SensorRadiusWorldUnits: > 0 })
                throw new ScenarioException("Invalid pirate carrier or sensor reference.");
        }
        foreach (string layout in DroneLayouts)
            if (catalog is null || !catalog.Layouts.Contains(layout))
                throw new ScenarioException("Unknown pirate drone layout: " + layout);
    }
}
