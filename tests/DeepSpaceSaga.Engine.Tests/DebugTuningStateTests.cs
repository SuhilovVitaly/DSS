using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class DebugTuningStateTests
{
    [Fact]
    public void Catalog_defaults_are_complete_and_replacement_preserves_indices_and_other_registries()
    {
        var catalog = PointCombatUS0018TK0002Tests.Registry();
        var original = new DebugTuningState(catalog);
        Assert.Equal(25, original.Snapshot.Defaults.Length);
        Assert.Equal(0, original.Snapshot.Revision);
        Assert.True(original.TryApply(new([new("module.engine.tetrarch", "maxSpeedKmS", 2),
            new("module.torpedo.launcher.basic", "speedKmS", 20, "medium")]), out var updated, out var reason));
        Assert.Null(reason);
        Assert.Equal(1, updated.Snapshot.Revision);
        Assert.Equal(5000, Engine(catalog).MaxSpeedMps);
        Assert.Equal(2000, Engine(updated.Registry).MaxSpeedMps);
        Assert.Equal(5, updated.Snapshot.Defaults.Single(v => v.ModuleTypeId == "module.engine.tetrarch" && v.Field == "maxSpeedKmS").Value);
        Assert.True(original.Snapshot.Defaults == updated.Snapshot.Defaults);
        for (int i = 0; i < catalog.ModuleTypes.Count; i++)
            Assert.Equal(catalog.ModuleTypes.GetDefinition(i).TypeId, updated.Registry.ModuleTypes.GetDefinition(i).TypeId);
        Assert.Same(catalog.CombatRules, updated.Registry.CombatRules);
        Assert.Same(catalog.ItemTypes, updated.Registry.ItemTypes);
        Assert.Same(catalog.StationMarketEvents, updated.Registry.StationMarketEvents);
        Assert.Equal(catalog.CatalogCompatibility, updated.Registry.CatalogCompatibility);
        Assert.Equal(20, updated.Registry.ModuleTypes.GetDefinition(updated.Registry.ModuleTypes.GetIndex("module.torpedo.launcher.basic"))
            .Profiles.Single(p => p.ProfileId == "medium").SpeedKmS);
        Assert.Equal(10, catalog.ModuleTypes.GetDefinition(catalog.ModuleTypes.GetIndex("module.torpedo.launcher.basic"))
            .Profiles.Single(p => p.ProfileId == "medium").SpeedKmS);
    }

    [Theory]
    [InlineData("maxSpeedKmS", -.1)]
    [InlineData("maxSpeedKmS", 50.01)]
    [InlineData("accelerationMps2", 10.5)]
    [InlineData("turnRateDegPerSec", 361)]
    [InlineData("turnRateDegPerSec", double.NaN)]
    [InlineData("maxSpeedKmS", double.PositiveInfinity)]
    public void Invalid_second_value_rejects_the_entire_patch(string field, double value)
    {
        var state = new DebugTuningState(PointCombatUS0018TK0002Tests.Registry());
        Assert.False(state.TryApply(new([new("module.engine.strizh", "maxSpeedKmS", 12),
            new("module.engine.tetrarch", field, value)]), out var next, out var reason));
        Assert.Same(state, next);
        Assert.Equal(DebugTuningReasonCodes.ValueOutOfRange, reason);
        Assert.Equal(0, state.Snapshot.Revision);
    }

    [Theory]
    [InlineData("module.engine.tetrarch", "speedKmS", null)]
    [InlineData("module.engine.tetrarch", "maxSpeedKmS", "fast")]
    [InlineData("MODULE.ENGINE.TETRARCH", "maxSpeedKmS", null)]
    [InlineData("module.torpedo.launcher.basic", "speedKmS", "FAST")]
    [InlineData("module.torpedo.launcher.basic", "reloadMs", "fast")]
    public void Only_explicit_case_sensitive_parameter_keys_are_available(string type, string field, string? profile)
    {
        var state = new DebugTuningState(PointCombatUS0018TK0002Tests.Registry());
        Assert.False(state.TryApply(new([new(type, field, 5, profile)]), out var next, out var reason));
        Assert.Same(state, next);
        Assert.Equal(DebugTuningReasonCodes.UnknownParameter, reason);
    }

    [Fact]
    public void Reset_and_repeated_success_keep_original_defaults_and_increment_revision()
    {
        var state = new DebugTuningState(PointCombatUS0018TK0002Tests.Registry());
        Assert.True(state.TryApply(new([new("module.engine.tetrarch", "accelerationMps2", 20000)]), out var changed, out _));
        Assert.True(changed.TryApply(new(changed.Snapshot.Defaults), out var reset, out _));
        Assert.Equal(state.Snapshot.Defaults.ToArray(), reset.Snapshot.Values.ToArray());
        Assert.Equal(2, reset.Snapshot.Revision);
        Assert.True(reset.TryApply(new(reset.Snapshot.Defaults), out var repeated, out _));
        Assert.Equal(3, repeated.Snapshot.Revision);
    }

    private static ModuleTypeDefinition Engine(GameDataRegistry registry) =>
        registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("module.engine.tetrarch"));

    [Theory]
    [InlineData("module.engine.tetrarch", "maxSpeedKmS", null, .1, 50)]
    [InlineData("module.engine.strizh", "accelerationMps2", null, 10, 20000)]
    [InlineData("module.engine.strizh", "turnRateDegPerSec", null, 1, 360)]
    [InlineData("module.torpedo.launcher.basic", "reloadMs", null, 100, 600000)]
    [InlineData("module.countermeasure.launcher.basic", "speedKmS", "wide", .1, 100)]
    [InlineData("module.torpedo.launcher.light", "blastRadiusKm", "fast", .05, 50)]
    public void Inclusive_bounds_are_accepted_and_values_beyond_each_bound_are_atomic_rejections(
        string type, string field, string? profile, double min, double max)
    {
        var state = new DebugTuningState(PointCombatUS0018TK0002Tests.Registry());
        foreach (double value in new[] { min, max })
        {
            Assert.True(state.TryApply(new([new(type, field, value, profile)]), out var next, out _));
            Assert.Equal(value, next.Snapshot.Values.Single(v => v.ModuleTypeId == type && v.Field == field && v.ProfileId == profile).Value);
        }
        foreach (double value in new[] { Math.BitDecrement(min), Math.BitIncrement(max) })
        {
            Assert.False(state.TryApply(new([new(type, field, value, profile)]), out var next, out var reason));
            Assert.Same(state, next);
            Assert.Equal(DebugTuningReasonCodes.ValueOutOfRange, reason);
        }
    }
}
