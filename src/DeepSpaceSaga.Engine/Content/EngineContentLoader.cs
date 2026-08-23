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

    /// <summary>
    /// Bootstrap a new engine from an explicitly chosen scenario file instead of settings'
    /// defaultScenario (the New Game -&gt; scenario picker path). New Game semantics —
    /// gameTimeMs must be 0, same as CreateEngineFromSettingsFile — unlike
    /// CreateEngineFromSaveFile's allowNonZeroGameTime: true.
    /// </summary>
    public static SimulationEngine CreateEngineFromScenarioFile(string settingsPath, string scenarioPath)
    {
        var registry = LoadRegistryFromSettingsFile(settingsPath, out _, out _);
        var scenario = ScenarioLoader.LoadFromFile(scenarioPath);
        var engine = new SimulationEngine(registry);
        engine.LoadScenario(scenario);
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
        var moduleCategories = LoadModuleCategories(Resolve(basePath, settings.TypeData.ModuleTypes));
        var moduleImplementations = LoadModuleImplementations(
            Resolve(basePath, settings.TypeData.ModuleImplementations), moduleCategories);
        var items = LoadItemTypes(Resolve(basePath, settings.TypeData.ItemTypes));

        // Factory/recipe files are loaded by declaration: a key present in Settings.json makes
        // the file mandatory (missing file → ContentException); an absent key skips loading.
        var factoryTypes = settings.TypeData.FactoryTypes is null
            ? null
            : LoadFactoryTypes(Resolve(basePath, settings.TypeData.FactoryTypes));
        var recipes = settings.TypeData.Recipes is null
            ? null
            : LoadRecipes(Resolve(basePath, settings.TypeData.Recipes));

        return GameDataRegistry.Create(moduleCategories, moduleImplementations, items, commands, factoryTypes, recipes);
    }

    /// <summary>
    /// Loads the abstract module TYPE layer ("module-types.json"): commandTypeIds + slotSize
    /// only. A single file, not a directory (mirrors the pre-split module-types.json layout).
    /// </summary>
    internal static IReadOnlyList<ModuleCategoryDefinition> LoadModuleCategories(string path)
    {
        var file = ReadJson<ModuleTypesFile>(path, "module types");
        if (file.ModuleTypes is null)
            throw new ContentException("module-types file is missing moduleTypes.");

        return file.ModuleTypes.Select(dto =>
        {
            if (dto.CommandTypeIds is null)
                throw new ContentException($"Module type '{dto.TypeId}' is missing commandTypeIds.");

            return new ModuleCategoryDefinition(
                dto.TypeId,
                dto.DisplayName,
                dto.SlotSize,
                dto.CommandTypeIds.ToImmutableArray());
        }).ToArray();
    }

    /// <summary>
    /// Loads the concrete module IMPLEMENTATION layer (e.g. "Data/Modules/Engine/modules-engine.json"):
    /// each implementation's "type" field resolves its owning <see cref="ModuleCategoryDefinition"/>,
    /// from which SlotSize and CommandTypeIds are pulled to construct the flat, unchanged
    /// <see cref="ModuleTypeDefinition"/> runtime record. Supports either a single JSON file or a
    /// directory (recursively merges every "*.json" file found under it), mirroring
    /// <see cref="LoadCommandDefinitions"/>'s dual-mode support.
    /// </summary>
    internal static IReadOnlyList<ModuleTypeDefinition> LoadModuleImplementations(
        string path, IReadOnlyList<ModuleCategoryDefinition> categories)
    {
        var categoriesByTypeId = categories.ToDictionary(c => c.TypeId, StringComparer.Ordinal);

        if (Directory.Exists(path))
        {
            var files = Directory.EnumerateFiles(path, "*.json", SearchOption.AllDirectories)
                .OrderBy(f => f, StringComparer.Ordinal)
                .ToArray();

            if (files.Length == 0)
            {
                throw new ContentException(
                    $"module-implementations directory contains no *.json files: {path}");
            }

            return files.SelectMany(file => ReadModuleImplementationsFile(file, categoriesByTypeId)).ToArray();
        }

        if (File.Exists(path))
            return ReadModuleImplementationsFile(path, categoriesByTypeId).ToArray();

        throw new ContentException(
            $"module-implementations path not found (neither file nor directory): {path}");
    }

    private static IEnumerable<ModuleTypeDefinition> ReadModuleImplementationsFile(
        string filePath, IReadOnlyDictionary<string, ModuleCategoryDefinition> categoriesByTypeId)
    {
        var file = ReadJson<ModuleImplementationsFile>(filePath, "module implementations");
        if (file.ModuleImplementations is null)
            throw new ContentException("module-implementations file is missing moduleImplementations.");

        return file.ModuleImplementations.Select(dto =>
        {
            if (string.IsNullOrWhiteSpace(dto.Type))
            {
                throw new ContentException(
                    $"Module implementation '{dto.TypeId}' is missing required 'type' " +
                    "(owning module type id).");
            }

            if (!categoriesByTypeId.TryGetValue(dto.Type, out var category))
            {
                throw new ContentException(
                    $"Module implementation '{dto.TypeId}' references unknown module type '{dto.Type}'.");
            }

            ValidateEngineParameters(dto, category);

            // A missing/null baseSuccessChancePercent normalizes to 100 (§56.5).
            int baseSuccessChancePercent = dto.BaseSuccessChancePercent ?? 100;
            if (baseSuccessChancePercent is < 0 or > 100)
            {
                throw new ContentException(
                    $"Module type '{dto.TypeId}' has baseSuccessChancePercent " +
                    $"'{baseSuccessChancePercent}' outside the valid range 0..100.");
            }

            long baseCycleTimeMs = dto.BaseCycleTimeMs ?? 0;

            // Active module types (those whose owning category declares commandTypeIds) MUST
            // specify a positive BaseCycleTimeMs (§56.3).
            if (baseCycleTimeMs <= 0 && category.CommandTypeIds.Length > 0)
            {
                throw new ContentException(
                    $"Active module type '{dto.TypeId}' must specify a positive baseCycleTimeMs.");
            }

            return new ModuleTypeDefinition(
                dto.TypeId,
                dto.DisplayName,
                category.SlotSize,
                dto.MassKg,
                dto.StructurePointsMax,
                dto.PowerConsumptionW,
                category.CommandTypeIds,
                dto.CargoCapacityKg,
                dto.MaxSpeedMps,
                dto.TurnStepDegrees,
                dto.LinearInertiaMps2,
                dto.AngularInertiaDegPerSec,
                baseCycleTimeMs,
                dto.FuelCapacityKg,
                baseSuccessChancePercent,
                dto.CabinesCount);
        });
    }

    private static void ValidateEngineParameters(ModuleImplementationDto dto, ModuleCategoryDefinition category)
    {
        if (!string.Equals(category.TypeId, "module.engine", StringComparison.Ordinal))
            return;

        if (dto.MaxSpeedMps is not > 0)
            throw new ContentException($"Module type '{dto.TypeId}' requires maxSpeedMps greater than zero.");
        if (dto.TurnStepDegrees is not > 0)
            throw new ContentException($"Module type '{dto.TypeId}' requires turnStepDegrees greater than zero.");
        if (dto.LinearInertiaMps2 is not > 0)
            throw new ContentException($"Module type '{dto.TypeId}' requires linearInertiaMps2 greater than zero.");
        if (dto.AngularInertiaDegPerSec is not > 0)
            throw new ContentException($"Module type '{dto.TypeId}' requires angularInertiaDegPerSec greater than zero.");
        if (dto.FuelCapacityKg is not > 0)
            throw new ContentException($"Module type '{dto.TypeId}' requires fuelCapacityKg greater than zero.");
    }

    private static IReadOnlyList<ItemTypeDefinition> LoadItemTypes(string path)
    {
        var file = ReadJson<ItemTypesFile>(path, "item types");
        if (file.ItemTypes is null)
            throw new ContentException("item-types file is missing itemTypes.");

        return file.ItemTypes.Select(dto =>
            new ItemTypeDefinition(dto.TypeId, dto.DisplayName, dto.UnitMassKg, dto.BasePriceCredits)).ToArray();
    }

    /// <summary>
    /// Loads command definitions from either a single JSON file (legacy layout) or a
    /// directory (recursively merges every "*.json" file found under it — the physical
    /// per-module-type layout, e.g. "Data/Commands/Engine/commands.json"). Directory
    /// traversal order is sorted (<see cref="StringComparer.Ordinal"/>) so the merge is
    /// deterministic regardless of on-disk enumeration order. All files are merged into a
    /// single sequence before being handed to <see cref="TypeRegistry{TDefinition}.Create"/>
    /// so duplicate typeIds across files are still caught.
    /// </summary>
    internal static IReadOnlyList<CommandDefinition> LoadCommandDefinitions(string path)
    {
        if (Directory.Exists(path))
        {
            var files = Directory.EnumerateFiles(path, "*.json", SearchOption.AllDirectories)
                .OrderBy(f => f, StringComparer.Ordinal)
                .ToArray();

            if (files.Length == 0)
            {
                throw new ContentException(
                    $"command-definitions directory contains no *.json files: {path}");
            }

            return files.SelectMany(ReadCommandDefinitionsFile).ToArray();
        }

        if (File.Exists(path))
            return ReadCommandDefinitionsFile(path).ToArray();

        throw new ContentException(
            $"command-definitions path not found (neither file nor directory): {path}");
    }

    private static IEnumerable<CommandDefinition> ReadCommandDefinitionsFile(string filePath)
    {
        var file = ReadJson<CommandDefinitionsFile>(filePath, "command definitions");
        if (file.CommandDefinitions is null)
            throw new ContentException("command-definitions file is missing commandDefinitions.");

        return file.CommandDefinitions.Select(dto =>
        {
            int activationEnergyCellsCost = dto.ActivationEnergyCellsCost ?? 0;
            if (activationEnergyCellsCost < 0)
            {
                throw new ContentException(
                    $"Command definition '{dto.TypeId}' has activationEnergyCellsCost " +
                    $"'{activationEnergyCellsCost}' which must be >= 0.");
            }

            if (string.IsNullOrWhiteSpace(dto.Type))
            {
                throw new ContentException(
                    $"Command definition '{dto.TypeId}' is missing required 'type' " +
                    "(owning module type id).");
            }

            return new CommandDefinition(
                dto.TypeId,
                dto.DisplayName,
                ParseFixedPointFactor(dto.TimeFactor),
                ParseFixedPointFactor(dto.ComplexityFactor),
                ParseFixedPointFactor(dto.ConsumptionFactor),
                dto.Target ?? "none",
                activationEnergyCellsCost,
                dto.Type,
                dto.RangeKm);
        });
    }

    /// <summary>
    /// Convert a JSON decimal factor into a fixed-point integer where 1000 = 1.0.
    /// A missing (null) value defaults to the neutral factor 1000 (§56.3).
    /// </summary>
    private static int ParseFixedPointFactor(decimal? jsonValue)
    {
        if (jsonValue is null)
            return CommandDefinition.Neutral;

        return (int)Math.Round(jsonValue.Value * CommandDefinition.Neutral, MidpointRounding.AwayFromZero);
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
        [property: JsonPropertyName("defaultScenario")] string DefaultScenario,
        // Display/UI preferences (language, uiScale, selectedMonitorIndex, ...) are properties
        // of the client application, not of a game session — the engine has no business
        // knowing their shape. Typed as an opaque JsonElement (rather than a matching record)
        // so UnmappedMemberHandling.Disallow only ever validates what the engine actually
        // consumes (typeData/defaultScenario); the client (SkiaWindow.cs GetLanguage/
        // SaveLanguage, GetUiScale, etc.) can add, rename, or remove gameSettings fields
        // freely without ever touching engine code again.
        [property: JsonPropertyName("gameSettings")] JsonElement GameSettings = default);

    internal sealed record TypeDataPaths(
        [property: JsonPropertyName("moduleTypes")] string ModuleTypes,
        [property: JsonPropertyName("moduleImplementations")] string ModuleImplementations,
        [property: JsonPropertyName("itemTypes")] string ItemTypes,
        [property: JsonPropertyName("commandDefinitions")] string CommandDefinitions,
        [property: JsonPropertyName("factoryTypes")] string? FactoryTypes,
        [property: JsonPropertyName("recipes")] string? Recipes);

    private sealed record ModuleTypesFile(
        [property: JsonPropertyName("moduleTypes")] IReadOnlyList<ModuleCategoryDefinitionDto> ModuleTypes);

    private sealed record ModuleCategoryDefinitionDto(
        [property: JsonPropertyName("typeId")] string TypeId,
        [property: JsonPropertyName("displayName")] string DisplayName,
        [property: JsonPropertyName("slotSize")] int SlotSize,
        [property: JsonPropertyName("commandTypeIds")] IReadOnlyList<string> CommandTypeIds);

    private sealed record ModuleImplementationsFile(
        [property: JsonPropertyName("moduleImplementations")] IReadOnlyList<ModuleImplementationDto> ModuleImplementations);

    private sealed record ModuleImplementationDto(
        [property: JsonPropertyName("typeId")] string TypeId,
        [property: JsonPropertyName("displayName")] string DisplayName,
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("massKg")] long MassKg,
        [property: JsonPropertyName("structurePointsMax")] int StructurePointsMax,
        [property: JsonPropertyName("powerConsumptionW")] long PowerConsumptionW,
        [property: JsonPropertyName("cargoCapacityKg")] long? CargoCapacityKg,
        [property: JsonPropertyName("maxSpeedMps")] int? MaxSpeedMps,
        [property: JsonPropertyName("turnStepDegrees")] int? TurnStepDegrees,
        [property: JsonPropertyName("linearInertiaMps2")] int? LinearInertiaMps2,
        [property: JsonPropertyName("angularInertiaDegPerSec")] int? AngularInertiaDegPerSec,
        [property: JsonPropertyName("baseCycleTimeMs")] long? BaseCycleTimeMs,
        [property: JsonPropertyName("fuelCapacityKg")] long? FuelCapacityKg,
        [property: JsonPropertyName("baseSuccessChancePercent")] int? BaseSuccessChancePercent,
        [property: JsonPropertyName("cabines")] int? CabinesCount);

    private sealed record ItemTypesFile(
        [property: JsonPropertyName("itemTypes")] IReadOnlyList<ItemTypeDefinitionDto> ItemTypes);

    private sealed record ItemTypeDefinitionDto(
        [property: JsonPropertyName("typeId")] string TypeId,
        [property: JsonPropertyName("displayName")] string DisplayName,
        [property: JsonPropertyName("unitMassKg")] long UnitMassKg,
        [property: JsonPropertyName("basePriceCredits")] long? BasePriceCredits = null);

    private sealed record CommandDefinitionsFile(
        [property: JsonPropertyName("commandDefinitions")] IReadOnlyList<CommandDefinitionDto> CommandDefinitions);

    private sealed record CommandDefinitionDto(
        [property: JsonPropertyName("typeId")] string TypeId,
        [property: JsonPropertyName("displayName")] string DisplayName,
        [property: JsonPropertyName("timeFactor")] decimal? TimeFactor,
        [property: JsonPropertyName("complexityFactor")] decimal? ComplexityFactor,
        [property: JsonPropertyName("consumptionFactor")] decimal? ConsumptionFactor,
        [property: JsonPropertyName("target")] string? Target,
        [property: JsonPropertyName("activationEnergyCellsCost")] int? ActivationEnergyCellsCost,
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("rangeKm")] int? RangeKm = null);

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
