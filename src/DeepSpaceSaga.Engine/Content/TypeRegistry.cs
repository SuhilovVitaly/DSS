using System.Collections.Immutable;

namespace DeepSpaceSaga.Engine.Content;

internal sealed class TypeRegistry<TDefinition>
    where TDefinition : ITypeDefinition
{
    private readonly ImmutableDictionary<string, int> _indexByTypeId;
    private readonly ImmutableArray<TDefinition> _definitions;

    private TypeRegistry(
        ImmutableDictionary<string, int> indexByTypeId,
        ImmutableArray<TDefinition> definitions)
    {
        _indexByTypeId = indexByTypeId;
        _definitions = definitions;
    }

    public int Count => _definitions.Length;

    public static TypeRegistry<TDefinition> Empty { get; } = new(
        ImmutableDictionary.Create<string, int>(StringComparer.Ordinal),
        ImmutableArray<TDefinition>.Empty);

    public static TypeRegistry<TDefinition> Create(IEnumerable<TDefinition> definitions, string registryName)
    {
        var indexBuilder = ImmutableDictionary.CreateBuilder<string, int>(StringComparer.Ordinal);
        var definitionBuilder = ImmutableArray.CreateBuilder<TDefinition>();

        foreach (var definition in definitions)
        {
            if (string.IsNullOrWhiteSpace(definition.TypeId))
                throw new ContentException($"{registryName} contains a type definition with empty typeId.");

            int index = definitionBuilder.Count;
            if (!indexBuilder.TryAdd(definition.TypeId, index))
                throw new ContentException($"{registryName} contains duplicate typeId '{definition.TypeId}'.");

            definitionBuilder.Add(definition);
        }

        return new TypeRegistry<TDefinition>(
            indexBuilder.ToImmutable(),
            definitionBuilder.ToImmutable());
    }

    public int GetIndex(string typeId)
    {
        if (string.IsNullOrWhiteSpace(typeId))
            throw new ContentException("Missing typeId.");

        if (!_indexByTypeId.TryGetValue(typeId, out int index))
            throw new ContentException($"Unknown typeId '{typeId}'.");

        return index;
    }

    public TDefinition GetDefinition(int index)
    {
        if (index < 0 || index >= _definitions.Length)
            throw new ContentException($"Type registry index {index} is outside 0..{_definitions.Length - 1}.");

        return _definitions[index];
    }

    public bool Contains(string typeId) => _indexByTypeId.ContainsKey(typeId);
}
