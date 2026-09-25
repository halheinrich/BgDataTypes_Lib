using System.Text.Json;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// Serialises <see cref="BoardPosition"/> as a JSON array of its 26 counts,
/// in slot order — the wire form every stored board has always had, so a
/// document's bytes are unchanged by the board's type. Reading refuses an
/// array that does not form a position (a count that is not an integer, a
/// count too many or too few, or a board breaking the invariant) with a
/// <see cref="JsonException"/> naming the fault: a malformed board on the
/// wire is a malformed document.
///
/// <para>
/// Public, like every converter a type-level <c>[JsonConverter]</c> here
/// names: a downstream <see cref="JsonSerializerContext"/> whose documents
/// embed the annotated type must instantiate the converter from its own
/// generated code (see <see cref="PlayJsonConverter"/>). The optional
/// after-boards use <see cref="NullableBoardPositionJsonConverter"/>, which
/// shares this wire form.
/// </para>
/// </summary>
public sealed class BoardPositionJsonConverter : JsonConverter<BoardPosition>
{
    /// <inheritdoc/>
    public override BoardPosition Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
        => ReadCounts(ref reader);

    /// <inheritdoc/>
    public override void Write(
        Utf8JsonWriter writer,
        BoardPosition value,
        JsonSerializerOptions options)
        => WriteCounts(writer, value);

    /// <summary>
    /// Reads the array the reader stands on as a position, leaving the reader
    /// on its closing token. The one reading of the wire form.
    /// </summary>
    /// <exception cref="JsonException">The array does not form a position; the message names the fault.</exception>
    internal static BoardPosition ReadCounts(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException(
                $"Expected an array of {BoardPosition.SlotCount} counts for a board, got {reader.TokenType}.");

        Span<int> counts = stackalloc int[BoardPosition.SlotCount];
        int read = 0;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                if (!BoardPosition.TryCreate(counts[..read], out var position, out var fault))
                    throw new JsonException($"The board is not a position: {fault}");
                return position;
            }

            if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out int count))
                throw new JsonException(
                    $"Each count on a board is an integer; slot {read} holds a {reader.TokenType} that is not one.");
            if (read == BoardPosition.SlotCount)
                throw new JsonException(
                    $"The board is not a position: it holds more than {BoardPosition.SlotCount} counts.");
            counts[read++] = count;
        }

        throw new JsonException("Unexpected end of JSON while reading a board.");
    }

    /// <summary>The one writing of the wire form: the 26 counts as numbers, in slot order.</summary>
    internal static void WriteCounts(Utf8JsonWriter writer, BoardPosition value)
    {
        writer.WriteStartArray();
        for (int i = 0; i < BoardPosition.SlotCount; i++)
            writer.WriteNumberValue(value[i]);
        writer.WriteEndArray();
    }
}
