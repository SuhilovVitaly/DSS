namespace DeepSpaceSaga.Engine.Content;

internal sealed class GameDataRegistry
{
    private GameDataRegistry(
        TypeRegistry<ModuleTypeDefinition> moduleTypes,
        TypeRegistry<ItemTypeDefinition> itemTypes,
        TypeRegistry<CommandDefinition> commandDefinitions,
        TypeRegistry<FactoryTypeDefinition> factoryTypes,
        TypeRegistry<RecipeDefinition> recipes)
    {
        ModuleTypes = moduleTypes;
        ItemTypes = itemTypes;
        CommandDefinitions = commandDefinitions;
        FactoryTypes = factoryTypes;
        Recipes = recipes;
    }

    public TypeRegistry<ModuleTypeDefinition> ModuleTypes { get; }
    public TypeRegistry<ItemTypeDefinition> ItemTypes { get; }
    public TypeRegistry<CommandDefinition> CommandDefinitions { get; }
    public TypeRegistry<FactoryTypeDefinition> FactoryTypes { get; }
    public TypeRegistry<RecipeDefinition> Recipes { get; }

    public static GameDataRegistry Empty { get; } = new(
        TypeRegistry<ModuleTypeDefinition>.Empty,
        TypeRegistry<ItemTypeDefinition>.Empty,
        TypeRegistry<CommandDefinition>.Empty,
        TypeRegistry<FactoryTypeDefinition>.Empty,
        TypeRegistry<RecipeDefinition>.Empty);

    public static GameDataRegistry Create(
        IEnumerable<ModuleTypeDefinition> moduleTypes,
        IEnumerable<ItemTypeDefinition> itemTypes,
        IEnumerable<CommandDefinition> commandDefinitions,
        IEnumerable<FactoryTypeDefinition>? factoryTypes = null,
        IEnumerable<RecipeDefinition>? recipes = null)
    {
        var commandRegistry = TypeRegistry<CommandDefinition>.Create(commandDefinitions, "command definitions");
        var moduleRegistry = TypeRegistry<ModuleTypeDefinition>.Create(moduleTypes, "module types");
        var itemRegistry = TypeRegistry<ItemTypeDefinition>.Create(itemTypes, "item types");
        var factoryRegistry = TypeRegistry<FactoryTypeDefinition>.Create(factoryTypes ?? [], "factory types");
        var recipeRegistry = TypeRegistry<RecipeDefinition>.Create(recipes ?? [], "recipes");

        for (int i = 0; i < moduleRegistry.Count; i++)
        {
            var moduleType = moduleRegistry.GetDefinition(i);
            foreach (string commandTypeId in moduleType.CommandTypeIds)
            {
                if (!commandRegistry.Contains(commandTypeId))
                {
                    throw new ContentException(
                        $"Module type '{moduleType.TypeId}' references unknown command definition '{commandTypeId}'.");
                }

                var command = commandRegistry.GetDefinition(commandRegistry.GetIndex(commandTypeId));
                if (!string.Equals(command.Type, moduleType.TypeId, StringComparison.Ordinal))
                {
                    throw new ContentException(
                        $"Command definition '{commandTypeId}' declares owning module type " +
                        $"'{command.Type}' but is referenced by module type '{moduleType.TypeId}' " +
                        $"via commandTypeIds — the two must match.");
                }
            }
        }

        return new GameDataRegistry(moduleRegistry, itemRegistry, commandRegistry, factoryRegistry, recipeRegistry);
    }
}
