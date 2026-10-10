using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public class DefenseLaunchPreviewTests
{
    [Fact]
    public void Preview_is_bound_to_module_and_target()
    {
        var first = new InstalledModuleSnapshot("defense-1", "type", "Defense", 1, [DefenseCommandTypes.Fire],
            Defense: new(true, null, DefenseState.Ready, RangeKm: 100, ManualRangeKm: 200, Accuracy: 55),
            DefensePreview: new("owner", "defense-1", "torpedo-a", 0, true, Accuracy: 55, TargetManeuverability: 100));
        var second = first with
        {
            ModuleId = "defense-2",
            DefensePreview = new("owner", "defense-2", "torpedo-b", 500, false, "busy", 55, 5)
        };
        var restored = JsonSerializer.Deserialize<InstalledModuleSnapshot[]>(JsonSerializer.Serialize(new[] { first, second }))!;
        Assert.Equal(first.DefensePreview, restored[0].DefensePreview);
        Assert.Equal(second.DefensePreview, restored[1].DefensePreview);
        Assert.Equal("defense-1", restored[0].DefensePreview!.ModuleId);
        Assert.Equal("torpedo-a", restored[0].DefensePreview!.TargetTorpedoId);
        Assert.True(restored[0].DefensePreview!.CanFire);
        Assert.Equal(0, restored[0].DefensePreview!.ChanceTenths);
        Assert.False(restored[1].DefensePreview!.CanFire);
        Assert.Equal("busy", restored[1].DefensePreview!.Reason);
        Assert.Equal(100, restored[0].Defense!.RangeKm);
        Assert.Equal(200, restored[0].Defense!.ManualRangeKm);
    }

    [Fact]
    public void Missing_target_and_missing_preview_remain_distinct_from_zero_chance()
    {
        var preview = new DefenseLaunchPreview("owner", "defense", null, null, false, "missing_target");
        Assert.Equal(preview, JsonSerializer.Deserialize<DefenseLaunchPreview>(JsonSerializer.Serialize(preview)));
        var legacy = JsonSerializer.Deserialize<InstalledModuleSnapshot>(
            """{"ModuleId":"engine","ModuleTypeId":"engine","DisplayName":"Engine","Position":0,"CommandTypeIds":[]}""")!;
        Assert.Null(legacy.DefensePreview);
        Assert.Null(legacy.Defense);
    }
}
