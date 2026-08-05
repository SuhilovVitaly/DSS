namespace DeepSpaceSaga.Engine.Content;

internal sealed class GameDataRegistry
{
    private GameDataRegistry(
        TypeRegistry<ModuleTypeDefinition> moduleTypes,
        TypeRegistry<ItemTypeDefinition> itemTypes,
        TypeRegistry<CommandDefinition> commandDefinitions)
    {
        ModuleTypes = moduleTypes;
        ItemTypes = itemTypes;
        CommandDefinitions = commandDefinitions;
    }

    public TypeRegistry<ModuleTypeDefinition> ModuleTypes { get; }
    public TypeRegistry<ItemTypeDefinition> ItemTypes { get; }
    public TypeRegistry<CommandDefinition> CommandDefinitions { get; }

    public static GameDataRegistry Empty { get; } = new(
        TypeRegistry<ModuleTypeDefinition>.Empty,
        TypeRegistry<ItemTypeDefinition>.Empty,
        TypeRegistry<CommandDefinition>.Empty);

    public static GameDataRegistry Create(
        IEnumerable<ModuleTypeDefinition> moduleTypes,
        IEnumerable<ItemTypeDefinition> itemTypes,
        IEnumerable<CommandDefinition> commandDefinitions)
    {
        var commandRegistry = TypeRegistry<CommandDefinition>.Create(commandDefinitions, "command definitions");
        var moduleRegistry = TypeRegistry<ModuleTypeDefinition>.Create(moduleTypes, "module types");
        var itemRegistry = TypeRegistry<ItemTypeDefinition>.Create(itemTypes, "item types");

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
            }
        }

        return new GameDataRegistry(moduleRegistry, itemRegistry, commandRegistry);
    }
}
