using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

/// <summary>
/// JSON converter for <see cref="ImmutableArray{T}"/> that transparently maps
/// <see cref="ImmutableArray{T}.IsDefault"/> (the value produced by
/// <c>default(ImmutableArray&lt;T&gt;)</c>) to an empty JSON array <c>[]</c>.
/// </summary>
/// <remarks>
/// <para>
/// System.Text.Json throws <see cref="InvalidOperationException"/> when asked
/// to enumerate a default <see cref="ImmutableArray{T}"/>. DTOs that use
/// <c>= default</c> as a constructor default (e.g. for backward-compatible
/// record evolution) need this converter so the type remains JSON-serializable
/// regardless of which constructor overload the caller used.
/// </para>
/// <para>
/// Deserialization always produces a non-default <see cref="ImmutableArray{T}"/>
/// (empty if the JSON array is empty).
/// </para>
/// </remarks>
public sealed class ImmutableArrayDefaultJsonConverter<T> : JsonConverter<ImmutableArray<T>>
{
    public override ImmutableArray<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var items = JsonSerializer.Deserialize<List<T>>(ref reader, options);
            return items?.ToImmutableArray() ?? ImmutableArray<T>.Empty;
        }

        // Defensive: unexpected token → empty.
        reader.Skip();
        return ImmutableArray<T>.Empty;
    }

    public override void Write(Utf8JsonWriter writer, ImmutableArray<T> value, JsonSerializerOptions options)
    {
        if (value.IsDefault)
        {
            writer.WriteStartArray();
            writer.WriteEndArray();
            return;
        }

        writer.WriteStartArray();
        foreach (var item in value)
            JsonSerializer.Serialize(writer, item, options);
        writer.WriteEndArray();
    }
}
