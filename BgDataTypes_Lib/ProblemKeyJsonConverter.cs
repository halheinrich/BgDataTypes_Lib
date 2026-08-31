using System.Text.Json;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// Serialises <see cref="ProblemKey"/> as its canonical string form (see the
/// <see cref="ProblemKey"/> remarks) — the pinned wire shape of the stats
/// document, whose schema version pins the grammar and is that document's
/// own fact, not this library's (SPEC-stats-identity.md §3).
/// Default property-based serialisation would leak the private
/// canonical field arrangement with no path back; the string is the intended
/// on-wire shape regardless. Bundled via type-level <c>[JsonConverter]</c>
/// attribute on <see cref="ProblemKey"/> so consumers do not need to register
/// the converter on their <see cref="JsonSerializerOptions"/>.
///
/// <para>
/// Unlike <see cref="DecisionIdJsonConverter"/>, this converter also
/// implements the property-name overloads: the stats document keys its
/// per-problem map by <see cref="ProblemKey"/>, so
/// <c>Dictionary&lt;ProblemKey, …&gt;</c> must round-trip without a
/// consumer-side key converter.
/// </para>
///
/// <para>
/// Public, like every converter a type-level <c>[JsonConverter]</c> here
/// names — a downstream <see cref="JsonSerializerContext"/>
/// whose documents embed the annotated type must instantiate the converter
/// from its own generated code (see <see cref="PlayJsonConverter"/>).
/// </para>
/// </summary>
public sealed class ProblemKeyJsonConverter : JsonConverter<ProblemKey>
{
    /// <inheritdoc/>
    public override ProblemKey? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException(
                $"Expected string for ProblemKey, got {reader.TokenType}.");

        return ParseOrThrow(reader.GetString());
    }

    /// <inheritdoc/>
    public override void Write(
        Utf8JsonWriter writer,
        ProblemKey value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }

    /// <inheritdoc/>
    public override ProblemKey ReadAsPropertyName(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return ParseOrThrow(reader.GetString());
    }

    /// <inheritdoc/>
    public override void WriteAsPropertyName(
        Utf8JsonWriter writer,
        ProblemKey value,
        JsonSerializerOptions options)
    {
        writer.WritePropertyName(value.ToString());
    }

    private static ProblemKey ParseOrThrow(string? s)
    {
        if (s is null || !ProblemKey.TryParse(s, provider: null, out var result))
            throw new JsonException($"Invalid ProblemKey canonical form: '{s}'.");
        return result;
    }
}
