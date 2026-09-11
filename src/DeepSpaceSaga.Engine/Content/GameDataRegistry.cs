namespace DeepSpaceSaga.Engine.Content;

internal sealed class GameDataRegistry
{
    private GameDataRegistry(
        TypeRegistry<ModuleCategoryDefinition> moduleCategories,
        TypeRegistry<ModuleTypeDefinition> moduleTypes,
        TypeRegistry<ItemTypeDefinition> itemTypes,
        TypeRegistry<CommandDefinition> commandDefinitions,
        TypeRegistry<FactoryTypeDefinition> factoryTypes,
        TypeRegistry<RecipeDefinition> recipes,
        TypeRegistry<DialogueDefinition>? dialogues = null,
        TypeRegistry<QuestDefinition>? quests = null)
    {
        ModuleCategories = moduleCategories;
        ModuleTypes = moduleTypes;
        ItemTypes = itemTypes;
        CommandDefinitions = commandDefinitions;
        FactoryTypes = factoryTypes;
        Recipes = recipes;
        Dialogues = dialogues ?? TypeRegistry<DialogueDefinition>.Empty;
        Quests = quests ?? TypeRegistry<QuestDefinition>.Empty;
    }

    public TypeRegistry<ModuleCategoryDefinition> ModuleCategories { get; }
    public TypeRegistry<ModuleTypeDefinition> ModuleTypes { get; }
    public TypeRegistry<ItemTypeDefinition> ItemTypes { get; }
    public TypeRegistry<CommandDefinition> CommandDefinitions { get; }
    public TypeRegistry<FactoryTypeDefinition> FactoryTypes { get; }
    public TypeRegistry<RecipeDefinition> Recipes { get; }
    public TypeRegistry<DialogueDefinition> Dialogues { get; }
    public TypeRegistry<QuestDefinition> Quests { get; }

    public static GameDataRegistry Empty { get; } = new(
        TypeRegistry<ModuleCategoryDefinition>.Empty,
        TypeRegistry<ModuleTypeDefinition>.Empty,
        TypeRegistry<ItemTypeDefinition>.Empty,
        TypeRegistry<CommandDefinition>.Empty,
        TypeRegistry<FactoryTypeDefinition>.Empty,
        TypeRegistry<RecipeDefinition>.Empty);

    public static GameDataRegistry Create(
        IEnumerable<ModuleCategoryDefinition> moduleCategories,
        IEnumerable<ModuleTypeDefinition> moduleTypes,
        IEnumerable<ItemTypeDefinition> itemTypes,
        IEnumerable<CommandDefinition> commandDefinitions,
        IEnumerable<FactoryTypeDefinition>? factoryTypes = null,
        IEnumerable<RecipeDefinition>? recipes = null,
        IEnumerable<DialogueDefinition>? dialogues = null,
        IEnumerable<QuestDefinition>? quests = null)
    {
        var commandRegistry = TypeRegistry<CommandDefinition>.Create(commandDefinitions, "command definitions");
        var categoryRegistry = TypeRegistry<ModuleCategoryDefinition>.Create(moduleCategories, "module types");
        var moduleRegistry = TypeRegistry<ModuleTypeDefinition>.Create(moduleTypes, "module implementations");
        var itemRegistry = TypeRegistry<ItemTypeDefinition>.Create(itemTypes, "item types");
        var factoryRegistry = TypeRegistry<FactoryTypeDefinition>.Create(factoryTypes ?? [], "factory types");
        var recipeRegistry = TypeRegistry<RecipeDefinition>.Create(recipes ?? [], "recipes");

        for (int i = 0; i < categoryRegistry.Count; i++)
        {
            var category = categoryRegistry.GetDefinition(i);
            foreach (string commandTypeId in category.CommandTypeIds)
            {
                if (!commandRegistry.Contains(commandTypeId))
                {
                    throw new ContentException(
                        $"Module type '{category.TypeId}' references unknown command definition '{commandTypeId}'.");
                }

                var command = commandRegistry.GetDefinition(commandRegistry.GetIndex(commandTypeId));
                if (!string.Equals(command.Type, category.TypeId, StringComparison.Ordinal))
                {
                    throw new ContentException(
                        $"Command definition '{commandTypeId}' declares owning module type " +
                        $"'{command.Type}' but is referenced by module type '{category.TypeId}' " +
                        $"via commandTypeIds — the two must match.");
                }
            }
        }

        var dialogueRegistry = TypeRegistry<DialogueDefinition>.Create(dialogues ?? [], "dialogues");
        var questRegistry = TypeRegistry<QuestDefinition>.Create(quests ?? [], "quests");
        for (int i = 0; i < dialogueRegistry.Count; i++)
        {
            var dialogue = dialogueRegistry.GetDefinition(i);
            DialogueContentLoader.Validate(dialogue);
            foreach (var effect in dialogue.Nodes.SelectMany(n => n.Choices).SelectMany(c => c.Effects.IsDefault ? [] : c.Effects))
            {
                if (effect.ItemTypeId is not null && !itemRegistry.Contains(effect.ItemTypeId))
                    throw new ContentException($"Unknown dialogue item: {effect.ItemTypeId}");
                if (effect.QuestId is not null)
                {
                    if (!questRegistry.Contains(effect.QuestId)) throw new ContentException($"Unknown dialogue quest: {effect.QuestId}");
                    if (effect.ObjectiveId is not null && !questRegistry.GetDefinition(questRegistry.GetIndex(effect.QuestId)).Objectives.Contains(effect.ObjectiveId))
                        throw new ContentException($"Unknown dialogue objective: {effect.ObjectiveId}");
                }
            }
        }
        return new GameDataRegistry(categoryRegistry, moduleRegistry, itemRegistry, commandRegistry, factoryRegistry, recipeRegistry,
            dialogueRegistry, questRegistry);
    }
}
