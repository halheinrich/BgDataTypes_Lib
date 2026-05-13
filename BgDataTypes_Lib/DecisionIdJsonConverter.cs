using System.Text.Json;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// Serialises <see cref="DecisionId"/> as its canonical string form (see the
/// <see cref="DecisionId"/> remarks). Default polymorphic serialisation is
/// unsuitable: <see cref="DecisionId"/> is an abstract record, so the
/// reflection-based serialiser cannot construct it on read without
/// <c>[JsonPolymorphic]</c> annotations — and the canonical string form is
/// the intended on-wire shape regardless. Bundled via type-level
/// <c>[JsonConverter]</c> attribute on <see cref="DecisionId"/> so consumers
/// do not need to register the converter on their
/// <see cref="JsonSerializerOptions"/>.
/// </summary>
internal sealed class DecisionIdJsonConverter : JsonConverter<DecisionId>
{
    public override DecisionId? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException(
                $"Expected string for DecisionId, got {reader.TokenType}.");

        var s = reader.GetString();
        if (s is null)
            return null;

        if (!DecisionId.TryParse(s, provider: null, out var result))
            throw new JsonException($"Invalid DecisionId canonical form: '{s}'.");

        return result;
    }

    public override void Write(
        Utf8JsonWriter writer,
        DecisionId value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
