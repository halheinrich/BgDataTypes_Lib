using System.Text.Json;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// Serialises <see cref="Play"/> as a JSON array of <see cref="Move"/> objects.
/// Default property-based serialisation is unsuitable: <see cref="Play"/>'s
/// fixed buffer is stored in private fields and <see cref="Play.Count"/> has a
/// private setter, so the default reflection-based serialiser would emit only
/// <c>{"Count": N}</c> and lose every move.
/// </summary>
internal sealed class PlayJsonConverter : JsonConverter<Play>
{
    public override Play Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of array for Play.");

        var play = new Play();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                return play;

            var move = JsonSerializer.Deserialize<Move>(ref reader, options);
            play.Add(move);
        }

        throw new JsonException("Unexpected end of JSON while reading Play.");
    }

    public override void Write(
        Utf8JsonWriter writer,
        Play value,
        JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        for (int i = 0; i < value.Count; i++)
            JsonSerializer.Serialize(writer, value[i], options);
        writer.WriteEndArray();
    }
}
