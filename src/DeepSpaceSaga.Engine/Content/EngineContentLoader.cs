using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Content;

public static class EngineContentLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public static SimulationEngine CreateEngineFromSettingsFile(string settingsPath)
    {
        var loaded = LoadFromSettingsFile(settingsPath);
        var engine = new SimulationEngine(loaded.Registry);
        engine.LoadScenario(loaded.DefaultScenario);
        return engine;
    }

    /// <summary>
    /// Bootstrap a new engine from a save file instead of the settings' defaultScenario.
    /// Same type registry loading as CreateEngineFromSettingsFile; the scenario source is
    /// the save file, loaded with allowNonZeroGameTime: true (a save legitimately carries
    /// gameTimeMs &gt; 0 — New Game scenarios never do).
    /// </summary>
    public static SimulationEngine CreateEngineFromSaveFile(string settingsPath, string savePath)
    {
        var registry = LoadRegistryFromSettingsFile(settingsPath, out _, out _);
        var saveScenario = ScenarioLoader.LoadFromFile(savePath, allowNonZeroGameTime: true);
        var engine = new SimulationEngine(registry);
        engine.LoadScenario(saveScenario);
        return engine;
    }

    internal static LoadedEngineContent LoadFromSettingsFile(string settingsPath)
    {
        var registry = LoadRegistryFromSettingsFile(settingsPath, out string basePath, out var settings);
        var scenario = ScenarioLoader.LoadFromFile(Resolve(basePath, settings.DefaultScenario));

        return new LoadedEngineContent(registry, scenario);
    }

    internal static GameDataRegistry LoadRegistryFromSettingsFile(
        string settingsPath, out string basePath, out EngineSettingsFile settings)
    {
        if (!File.Exists(settingsPath))
            throw new ContentException($"Settings file not found: {settingsPath}");

        basePath = Path.GetDirectoryName(Path.GetFullPath(settingsPath)) ?? AppContext.BaseDirectory;
        settings = ReadJson<EngineSettingsFile>(settingsPath, "settings");

        if (settings.TypeData is null)
            throw new ContentException("Settings file is missing typeData.");
        if (string.IsNullOrWhiteSpace(settings.DefaultScenario))
            throw new ContentException("Settings file is missing defaultScenario.");

        var commands = LoadCommandDefinitions(Resolve(basePath, settings.TypeData.CommandDefinitions));
        var modules = LoadModuleTypes(Resolve(basePath, settings.TypeData.ModuleTypes));
        var items = LoadItemTypes(Resolve(basePath, settings.TypeData.ItemTypes));

        // Factory/recipe files are loaded by declaration: a key present in Settings.json makes
        // the file mandatory (missing file → ContentException); an absent key skips loading.
        var factoryTypes = settings.TypeData.FactoryTypes is null
            ? null
            : LoadFactoryTypes(Resolve(basePath, settings.TypeData.FactoryTypes));
        var recipes = settings.TypeData.Recipes is null
            ? null
            : LoadRecipes(Resolve(basePath, settings.TypeData.Recipes));

        return GameDataRegistry.Create(modules, items, commands, factoryTypes, recipes);
    }

    private static IReadOnlyList<ModuleTypeDefinition> LoadModuleTypes(string path)
    {
        var file = ReadJson<ModuleTypesFile>(path, "module types");
        if (file.ModuleTypes is null)
            throw new ContentException("module-types file is missing moduleTypes.");

        return file.ModuleTypes.Select(dto =>
        {
            if (dto.CommandTypeIds is null)
                throw new ContentException($"Module type '{dto.TypeId}' is missing commandTypeIds.");
            ValidateEngineParameters(dto);

            return new ModuleTypeDefinition(
                dto.TypeId,
                dto.DisplayName,
                dto.SlotSize,
                dto.MassKg,
                dto.StructurePointsMax,
                dto.PowerConsumptionW,
                dto.CommandTypeIds.ToImmutableArray(),
                dto.CargoCapacityKg,
                dto.MaxSpeedMps,
                dto.TurnStepDegrees,
                dto.LinearInertiaMps2);
        }).ToArray();
    }

    private static void ValidateEngineParameters(ModuleTypeDefinitionDto dto)
    {
        if (!string.Equals(dto.TypeId, "module.engine.basic", StringComparison.Ordinal))
            return;

        if (dto.MaxSpeedMps is not > 0)
            throw new ContentException("Module type 'module.engine.basic' requires maxSpeedMps greater than zero.");
        if (dto.TurnStepDegrees is not > 0)
            throw new ContentException("Module type 'module.engine.basic' requires turnStepDegrees greater than zero.");
        if (dto.LinearInertiaMps2 is not > 0)
            throw new ContentException("Module type 'module.engine.basic' requires linearInertiaMps2 greater than zero.");
    }

    private static IReadOnlyList<ItemTypeDefinition> LoadItemTypes(string path)
    {
        var file = ReadJson<ItemTypesFile>(path, "item types");
        if (file.ItemTypes is null)
            throw new ContentException("item-types file is missing itemTypes.");

        return file.ItemTypes.Select(dto =>
            new ItemTypeDefinition(dto.TypeId, dto.DisplayName, dto.UnitMassKg)).ToArray();
    }

    private static IReadOnlyList<CommandDefinition> LoadCommandDefinitions(string path)
    {
        var file = ReadJson<CommandDefinitionsFile>(path, "command definitions");
        if (file.CommandDefinitions is null)
            throw new ContentException("command-definitions file is missing commandDefinitions.");

        return file.CommandDefinitions.Select(dto =>
            new CommandDefinition(dto.TypeId, dto.DisplayName)).ToArray();
    }

    private static IReadOnlyList<FactoryTypeDefinition> LoadFactoryTypes(string path)
    {
        var file = ReadJson<FactoryTypesFile>(path, "factory types");
        if (file.FactoryTypes is null)
            throw new ContentException("factory-types file is missing factoryTypes.");

        return file.FactoryTypes.Select(dto =>
        {
            if (dto.Recipe is null)
                throw new ContentException($"Factory type '{dto.TypeId}' is missing recipe.");
            if (dto.Recipe.Inputs is null)
                throw new ContentException($"Factory type '{dto.TypeId}' recipe is missing inputs.");
            if (dto.Recipe.Outputs is null)
                throw new ContentException($"Factory type '{dto.TypeId}' recipe is missing outputs.");

            return new FactoryTypeDefinition(
                dto.TypeId,
                dto.DisplayName,
                new RecipeDefinition(
                    dto.TypeId,
                    dto.DisplayName,
                    dto.Recipe.Inputs.Select(m => new RecipeMaterial(m.ItemTypeId, m.Count)).ToImmutableArray(),
                    dto.Recipe.Outputs.Select(m => new RecipeMaterial(m.ItemTypeId, m.Count)).ToImmutableArray(),
                    dto.Recipe.CycleTimeMs));
        }).ToArray();
    }

    private static IReadOnlyList<RecipeDefinition> LoadRecipes(string path)
    {
        var file = ReadJson<RecipesFile>(path, "recipes");
        if (file.Recipes is null)
            throw new ContentException("recipes file is missing recipes.");

        return file.Recipes.Select(dto =>
        {
            if (dto.Inputs is null)
                throw new ContentException($"Recipe '{dto.TypeId}' is missing inputs.");
            if (dto.Outputs is null)
                throw new ContentException($"Recipe '{dto.TypeId}' is missing outputs.");

            return new RecipeDefinition(
                dto.TypeId,
                dto.DisplayName,
                dto.Inputs.Select(m => new RecipeMaterial(m.ItemTypeId, m.Count)).ToImmutableArray(),
                dto.Outputs.Select(m => new RecipeMaterial(m.ItemTypeId, m.Count)).ToImmutableArray(),
                dto.CycleDurationMs);
        }).ToArray();
    }

    private static T ReadJson<T>(string path, string description)
    {
        if (!File.Exists(path))
            throw new ContentException($"{description} file not found: {path}");

        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions)
                   ?? throw new ContentException($"{description} JSON deserialized to null.");
        }
        catch (JsonException ex)
        {
            throw new ContentException($"Invalid {description} JSON: {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            throw new ContentException($"Failed to read {description} file: {path}", ex);
        }
    }

    private static string Resolve(string basePath, string relativeOrAbsolutePath)
    {
        if (string.IsNullOrWhiteSpace(relativeOrAbsolutePath))
            throw new ContentException("Settings file contains an empty content path.");

        return Path.IsPathRooted(relativeOrAbsolutePath)
            ? relativeOrAbsolutePath
            : Path.GetFullPath(Path.Combine(basePath, relativeOrAbsolutePath));
    }

    internal sealed record LoadedEngineContent(GameDataRegistry Registry, ScenarioFile DefaultScenario);

    internal sealed record EngineSettingsFile(
        [property: JsonPropertyName("typeData")] TypeDataPaths TypeData,
        [property: JsonPropertyName("defaultScenario")] string DefaultScenario);

    internal sealed record TypeDataPaths(
        [property: JsonPropertyName("moduleTypes")] string ModuleTypes,
        [property: JsonPropertyName("itemTypes")] string ItemTypes,
        [property: JsonPropertyName("commandDefinitions")] string CommandDefinitions,
        [property: JsonPropertyName("factoryTypes")] string? FactoryTypes,
        [property: JsonPropertyName("recipes")] string? Recipes);

    private sealed record ModuleTypesFile(
        [property: JsonPropertyName("moduleTypes")] IReadOnlyList<ModuleTypeDefinitionDto> ModuleTypes);

    private sealed record ModuleTypeDefinitionDto(
        [property: JsonPropertyName("typeId")] string TypeId,
        [property: JsonPropertyName("displayName")] string DisplayName,
        [property: JsonPropertyName("slotSize")] int SlotSize,
        [property: JsonPropertyName("massKg")] long MassKg,
        [property: JsonPropertyName("structurePointsMax")] int StructurePointsMax,
        [property: JsonPropertyName("powerConsumptionW")] long PowerConsumptionW,
        [property: JsonPropertyName("commandTypeIds")] IReadOnlyList<string> CommandTypeIds,
        [property: JsonPropertyName("cargoCapacityKg")] long? CargoCapacityKg,
        [property: JsonPropertyName("maxSpeedMps")] int? MaxSpeedMps,
        [property: JsonPropertyName("turnStepDegrees")] int? TurnStepDegrees,
        [property: JsonPropertyName("linearInertiaMps2")] int? LinearInertiaMps2);

    private sealed record ItemTypesFile(
        [property: JsonPropertyName("itemTypes")] IReadOnlyList<ItemTypeDefinitionDto> ItemTypes);

    private sealed record ItemTypeDefinitionDto(
        [property: JsonPropertyName("typeId")] string TypeId,
        [property: JsonPropertyName("displayName")] string DisplayName,
        [property: JsonPropertyName("unitMassKg")] long UnitMassKg);

    private sealed record CommandDefinitionsFile(
        [property: JsonPropertyName("commandDefinitions")] IReadOnlyList<CommandDefinitionDto> CommandDefinitions);

    private sealed record CommandDefinitionDto(
        [property: JsonPropertyName("typeId")] string TypeId,
        [property: JsonPropertyName("displayName")] string DisplayName);

    private sealed record FactoryTypesFile(
        [property: JsonPropertyName("factoryTypes")] IReadOnlyList<FactoryTypeDefinitionDto> FactoryTypes);

    private sealed record FactoryTypeDefinitionDto(
        [property: JsonPropertyName("typeId")] string TypeId,
        [property: JsonPropertyName("displayName")] string DisplayName,
        [property: JsonPropertyName("recipe")] RecipeDto? Recipe);

    private sealed record RecipeDto(
        [property: JsonPropertyName("inputs")] IReadOnlyList<RecipeMaterialDto> Inputs,
        [property: JsonPropertyName("outputs")] IReadOnlyList<RecipeMaterialDto> Outputs,
        [property: JsonPropertyName("cycleTimeMs")] long CycleTimeMs);

    private sealed record RecipesFile(
        [property: JsonPropertyName("recipes")] IReadOnlyList<RecipeDefinitionDto> Recipes);

    private sealed record RecipeDefinitionDto(
        [property: JsonPropertyName("typeId")] string TypeId,
        [property: JsonPropertyName("displayName")] string DisplayName,
        [property: JsonPropertyName("inputs")] IReadOnlyList<RecipeMaterialDto> Inputs,
        [property: JsonPropertyName("outputs")] IReadOnlyList<RecipeMaterialDto> Outputs,
        [property: JsonPropertyName("cycleDurationMs")] long CycleDurationMs);

    private sealed record RecipeMaterialDto(
        [property: JsonPropertyName("itemTypeId")] string ItemTypeId,
        [property: JsonPropertyName("count")] long Count);
}
