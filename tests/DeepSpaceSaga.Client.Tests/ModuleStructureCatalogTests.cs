using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Client.Tests;

public sealed class ModuleStructureCatalogTests
{
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
        "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    private static GameDataRegistry Load(bool output) => EngineContentLoader.LoadRegistryFromSettingsFile(
        Path.Combine(output ? AppContext.BaseDirectory : ClientRoot, "Settings.json"), out _, out _);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void All_modules_structure_150(bool output)
    {
        var registry = Load(output);
        var modules = Enumerable.Range(0, registry.ModuleTypes.Count)
            .Select(registry.ModuleTypes.GetDefinition).ToArray();
        Assert.NotEmpty(modules);
        Assert.All(modules, module => Assert.Equal(150, module.StructurePointsMax));
        Assert.Equal("bridge", modules.Single(m => m.TypeId == "module.bridge.navigation.computer.basic").CombatRole);
        Assert.Contains(modules, m => m.TypeId == "living.quarters.mk1.three-cabins");
        Assert.Contains(modules, m => m.TypeId == "module.engine.strizh");
        Assert.DoesNotContain(modules, m => m.TypeId == "module.countermeasure.launcher.single");
    }

    [Fact]
    public void Rule_one_hit_unarmored_two_hits_light()
    {
        var registry = Load(false);
        var launcher = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("module.torpedo.launcher.basic"));
        var rules = Assert.IsType<CombatRulesDefinition>(registry.CombatRules);
        int structure = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("module.engine.basic")).StructurePointsMax;
        decimal damage = launcher.TorpedoDamage!.Value;
        decimal direct = damage * launcher.ArmorPiercingPercent!.Value / 100;
        decimal remainder = damage - direct;
        decimal armorDamageFactor = launcher.ArmorDamagePercent!.Value / 100;
        int lightArmor = rules.ArmorClasses["light"];

        Assert.True(structure - damage <= 0); // no armor: one contact
        decimal absorbed = Math.Min(lightArmor, remainder * armorDamageFactor);
        decimal overflow = remainder - absorbed / armorDamageFactor;
        int afterFirst = structure - (int)decimal.Round(direct + overflow, 0, MidpointRounding.AwayFromZero);
        Assert.True(afterFirst > 0); // light armor: survives first contact
        Assert.True(afterFirst - damage <= 0); // armor is exhausted; second contact destroys
        Assert.Equal(60, lightArmor);
        Assert.Equal(0, rules.ArmorClasses["none"]);
    }
}
