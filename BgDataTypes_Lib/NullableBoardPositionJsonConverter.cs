using System.Text.Json;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// Serialises an optional board — a <see cref="BoardPosition"/>? after-board
/// (<see cref="PlayOutcomeData"/>, <see cref="DecisionRow"/>) — as the
/// <see cref="BoardPositionJsonConverter"/> wire form when present and as
/// JSON <c>null</c> when absent (halheinrich/backgammon#15). An empty array
/// also reads as absent: it is how documents written before the boards were
/// typed spelled a missing after-board, and they keep loading. It is never
/// written.
///
/// <para>
/// Named by a property-level <c>[JsonConverter]</c> on each after-board
/// rather than by the type: a converter for <see cref="BoardPosition"/>
/// cannot own <see cref="Nullable{T}"/>'s read of <c>[]</c>. Public for the
/// same reason as the type-level converters — a downstream source-generated
/// context that embeds an after-board instantiates it from its own
/// generated code.
/// </para>
/// </summary>
public sealed class NullableBoardPositionJsonConverter : JsonConverter<BoardPosition?>
{
    /// <summary>
    /// <see langword="true"/>: the converter reads and writes the absent
    /// board itself, so <c>null</c> and the legacy <c>[]</c> are handled in
    /// one place.
    /// </summary>
    public override bool HandleNull => true;

    /// <inheritdoc/>
    public override BoardPosition? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            // Peek on a copy: an empty array is the legacy absent board.
            var peek = reader;
            if (peek.Read() && peek.TokenType == JsonTokenType.EndArray)
            {
                reader.Read();
                return null;
            }
        }

        return BoardPositionJsonConverter.ReadCounts(ref reader);
    }

    /// <inheritdoc/>
    public override void Write(
        Utf8JsonWriter writer,
        BoardPosition? value,
        JsonSerializerOptions options)
    {
        if (value is { } position)
            BoardPositionJsonConverter.WriteCounts(writer, position);
        else
            writer.WriteNullValue();
    }
}
