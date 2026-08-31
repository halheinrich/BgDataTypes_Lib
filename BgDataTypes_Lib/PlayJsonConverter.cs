using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace BgDataTypes_Lib;

/// <summary>
/// Serialises <see cref="Play"/> as a JSON array of <see cref="Move"/> objects.
/// Default property-based serialisation is unsuitable: <see cref="Play"/>'s
/// fixed buffer is stored in private fields and <see cref="Play.Count"/> has a
/// private setter, so the default reflection-based serialiser would emit only
/// <c>{"Count": N}</c> and lose every move.
///
/// <para>
/// <see cref="Move"/> elements are (de)serialised through the active options'
/// type-info resolver (<see cref="JsonSerializerOptions.GetTypeInfo"/>) rather
/// than the reflection-bound <c>JsonSerializer</c> overloads — the trim-safe
/// spelling (halheinrich/backgammon#129): under reflection-backed options it
/// resolves the same metadata as before, byte-identically, and under a
/// trimmed consumer's source-generated options it resolves
/// <see cref="Move"/>'s declaration in <see cref="BgDataTypesJsonContext"/>.
/// Options that can resolve neither (a foreign context chained without this
/// library's) fail loud here, as the reflection overloads would have.
/// </para>
///
/// <para>
/// Public, like every converter a type-level <c>[JsonConverter]</c> here
/// names: a downstream <see cref="JsonSerializerContext"/> whose documents
/// embed the annotated type must instantiate the converter from its own
/// generated code, so an internal converter fails that generator outright
/// (SYSLIB1220). Registration stays bundled via the attribute — nothing to
/// construct by hand.
/// </para>
/// </summary>
public sealed class PlayJsonConverter : JsonConverter<Play>
{
    /// <inheritdoc/>
    public override Play Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of array for Play.");

        var moveTypeInfo = MoveTypeInfo(options);
        var play = new Play();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                return play;

            var move = JsonSerializer.Deserialize(ref reader, moveTypeInfo);
            play.Add(move);
        }

        throw new JsonException("Unexpected end of JSON while reading Play.");
    }

    /// <inheritdoc/>
    public override void Write(
        Utf8JsonWriter writer,
        Play value,
        JsonSerializerOptions options)
    {
        var moveTypeInfo = MoveTypeInfo(options);
        writer.WriteStartArray();
        foreach (var move in value)
            JsonSerializer.Serialize(writer, move, moveTypeInfo);
        writer.WriteEndArray();
    }

    private static JsonTypeInfo<Move> MoveTypeInfo(JsonSerializerOptions options)
        => (JsonTypeInfo<Move>)options.GetTypeInfo(typeof(Move));
}
