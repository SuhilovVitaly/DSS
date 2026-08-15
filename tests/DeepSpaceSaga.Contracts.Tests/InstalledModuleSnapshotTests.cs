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
    }

    [Fact]
    public void Serialization_round_trip_preserves_commands_metadata()
    {
        var snapshot = new InstalledModuleSnapshot(
            ModuleId: "MOD-ENG-01",
            ModuleTypeId: "module.engine.basic",
            DisplayName: "Engine",
            Position: 0,
            CommandTypeIds: ImmutableArray.Create("engine.accelerate", "engine.navigate-to-point"),
            Commands: ImmutableArray.Create(
                new ModuleCommandSnapshot("engine.accelerate", "Accelerate", "none"),
                new ModuleCommandSnapshot("engine.navigate-to-point", "Navigate To Point", "point")));

        var json = JsonSerializer.Serialize(snapshot);
        var roundTripped = JsonSerializer.Deserialize<InstalledModuleSnapshot>(json);

        Assert.NotNull(roundTripped);
        Assert.False(roundTripped!.Commands.IsDefault);
        Assert.Equal(2, roundTripped.Commands.Length);
        Assert.Equal("engine.accelerate", roundTripped.Commands[0].CommandTypeId);
        Assert.Equal("Accelerate", roundTripped.Commands[0].DisplayName);
        Assert.Equal("none", roundTripped.Commands[0].Target);
        Assert.Equal("engine.navigate-to-point", roundTripped.Commands[1].CommandTypeId);
        Assert.Equal("point", roundTripped.Commands[1].Target);
    }

    [Fact]
    public void Commands_metadata_defaults_to_none_target()
    {
        var command = new ModuleCommandSnapshot("engine.accelerate", "Accelerate");
        Assert.Equal("none", command.Target);
    }
}
