using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

/// <summary>
/// Tests for <see cref="InstalledModuleSnapshot"/> runtime-state fields
/// (ТЗ-03, module/command panel status projection).
/// </summary>
public class InstalledModuleSnapshotTests
{
    [Fact]
    public void Serialization_round_trip_preserves_populated_runtime_state_fields()
    {
        var snapshot = new InstalledModuleSnapshot(
            ModuleId: "MOD-ENG-01",
            ModuleTypeId: "module.engine.basic",
            DisplayName: "Engine",
            Position: 0,
            CommandTypeIds: ImmutableArray.Create("engine.accelerate"),
            PowerState: "On",
            OperationalState: "Busy",
            StructurePoints: 87,
            ActiveCommandType: "engine.accelerate",
            FuelAmountKg: 950);

        var json = JsonSerializer.Serialize(snapshot);
        var roundTripped = JsonSerializer.Deserialize<InstalledModuleSnapshot>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal("On", roundTripped!.PowerState);
        Assert.Equal("Busy", roundTripped.OperationalState);
        Assert.Equal(87, roundTripped.StructurePoints);
        Assert.Equal("engine.accelerate", roundTripped.ActiveCommandType);
        Assert.Equal(950, roundTripped.FuelAmountKg);
    }

    [Fact]
    public void Serialization_round_trip_preserves_cabines_count()
    {
        var snapshot = new InstalledModuleSnapshot(
            ModuleId: "MOD-LQ-01",
            ModuleTypeId: "living.quarters.mk1",
            DisplayName: "Living Quarters",
            Position: 1,
            CommandTypeIds: ImmutableArray<string>.Empty,
            CabinesCount: 2);

        var json = JsonSerializer.Serialize(snapshot);
        var roundTripped = JsonSerializer.Deserialize<InstalledModuleSnapshot>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal(2, roundTripped!.CabinesCount);
    }

    [Fact]
    public void Serialization_round_trip_preserves_available_capacity_kg()
    {
        var snapshot = new InstalledModuleSnapshot(
            ModuleId: "MOD-CNT-01",
            ModuleTypeId: "module.container.basic",
            DisplayName: "Cargo Bay",
            Position: 2,
            CommandTypeIds: ImmutableArray<string>.Empty,
            AvailableCapacityKg: 640);

        var json = JsonSerializer.Serialize(snapshot);
        var roundTripped = JsonSerializer.Deserialize<InstalledModuleSnapshot>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal(640, roundTripped!.AvailableCapacityKg);
    }

    [Fact]
    public void Serialization_round_trip_preserves_default_null_fields_for_inactive_non_engine_module()
    {
        var snapshot = new InstalledModuleSnapshot(
            ModuleId: "MOD-SCN-01",
            ModuleTypeId: "module.scanner.mk1",
            DisplayName: "Scanner MK I",
            Position: 1,
            CommandTypeIds: ImmutableArray<string>.Empty);

        var json = JsonSerializer.Serialize(snapshot);
        var roundTripped = JsonSerializer.Deserialize<InstalledModuleSnapshot>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal("Off", roundTripped!.PowerState);
        Assert.Equal("Ready", roundTripped.OperationalState);
        Assert.Equal(0, roundTripped.StructurePoints);
        Assert.Null(roundTripped.ActiveCommandType);
        Assert.Null(roundTripped.FuelAmountKg);
        Assert.Null(roundTripped.AvailableCapacityKg);
        Assert.Null(roundTripped.CabinesCount);
    }

    [Fact]
    public void Serialization_round_trip_preserves_commands_metadata()
    {
        var snapshot = new InstalledModuleSnapshot(
            ModuleId: "MOD-ENG-01",
            ModuleTypeId: "module.engine.basic",
            DisplayName: "Engine",
            Position: 0,
            CommandTypeIds: ImmutableArray.Create("engine.accelerate", "engine.orbit"),
            Commands: ImmutableArray.Create(
                new ModuleCommandSnapshot("engine.accelerate", "Accelerate", "none"),
                new ModuleCommandSnapshot("engine.orbit", "Orbit", "point")));

        var json = JsonSerializer.Serialize(snapshot);
        var roundTripped = JsonSerializer.Deserialize<InstalledModuleSnapshot>(json);

        Assert.NotNull(roundTripped);
        Assert.False(roundTripped!.Commands.IsDefault);
        Assert.Equal(2, roundTripped.Commands.Length);
        Assert.Equal("engine.accelerate", roundTripped.Commands[0].CommandTypeId);
        Assert.Equal("Accelerate", roundTripped.Commands[0].DisplayName);
        Assert.Equal("none", roundTripped.Commands[0].Target);
        Assert.Equal("engine.orbit", roundTripped.Commands[1].CommandTypeId);
        Assert.Equal("point", roundTripped.Commands[1].Target);
    }

    [Fact]
    public void Commands_metadata_defaults_to_none_target()
    {
        var command = new ModuleCommandSnapshot("engine.accelerate", "Accelerate");
        Assert.Equal("none", command.Target);
    }

    [Fact]
    public void Cargo_defaults_to_default_or_empty()
    {
        var snapshot = new InstalledModuleSnapshot(
            ModuleId: "MOD-CNT-01",
            ModuleTypeId: "module.container.basic",
            DisplayName: "Cargo Bay",
            Position: 2,
            CommandTypeIds: ImmutableArray<string>.Empty);

        Assert.True(snapshot.Cargo.IsDefaultOrEmpty);
    }

    [Fact]
    public void Cargo_round_trips_via_json_with_populated_stacks()
    {
        var snapshot = new InstalledModuleSnapshot(
            ModuleId: "MOD-CNT-01",
            ModuleTypeId: "module.container.basic",
            DisplayName: "Cargo Bay",
            Position: 2,
            CommandTypeIds: ImmutableArray<string>.Empty,
            Cargo: ImmutableArray.Create(
                new CargoStackSnapshot("item.ice", 12),
                new CargoStackSnapshot("item.energyCells", 3)));

        var json = JsonSerializer.Serialize(snapshot);
        var roundTripped = JsonSerializer.Deserialize<InstalledModuleSnapshot>(json);

        Assert.NotNull(roundTripped);
        Assert.False(roundTripped!.Cargo.IsDefault);
        Assert.Equal(2, roundTripped.Cargo.Length);
        Assert.Equal("item.ice", roundTripped.Cargo[0].ItemTypeId);
        Assert.Equal(12, roundTripped.Cargo[0].Quantity);
        Assert.Equal("item.energyCells", roundTripped.Cargo[1].ItemTypeId);
        Assert.Equal(3, roundTripped.Cargo[1].Quantity);
    }
}
