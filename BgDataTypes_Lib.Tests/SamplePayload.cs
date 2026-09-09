using System.Text.Json;
using System.Text.Json.Serialization;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The test suite's own source-generated context, following the pattern
/// the library sets for every implementer of <see cref="IJsonDocument{TSelf}"/>:
/// metadata-only generation, every wire root declared. Test-only — the
/// library's real context is <see cref="BgDataTypesJsonContext"/>.
/// </summary>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(SamplePayload))]
[JsonSerializable(typeof(ThrowingDocument))]
internal sealed partial class SampleJsonContext : JsonSerializerContext
{
}

/// <summary>
/// A synthetic <see cref="IJsonDocument{TSelf}"/> implementer standing in
/// for the real payloads (<c>FilterConfig</c>, <c>QuizMix</c>) that live in
/// other repos. Deliberately mutable — a settable scalar and a growable list
/// — so a container's snapshot contract is testable: a payload the caller
/// keeps mutating must never alias what the container stored. Its wire form
/// is the plain property graph, tolerant of unknown members, which is
/// exactly the "tolerant entry body" a named collection delegates to. The
/// inert default is a fresh instance (the <c>FilterConfig</c> shape), not a
/// shared singleton, because a mutable singleton would be corruptible.
/// </summary>
public sealed record SamplePayload : IJsonDocument<SamplePayload>
{
    public string Label { get; set; } = "";

    public int Weight { get; set; }

    public List<string> Tags { get; init; } = [];

    public string ToJson() => JsonSerializer.Serialize(this, SampleJsonContext.Default.SamplePayload);

    public static SamplePayload FromJson(string json) =>
        CanonicalJson.Parse(json, SampleJsonContext.Default.SamplePayload);

    public static bool TryFromJson(string? json, out SamplePayload document) =>
        CanonicalJson.TryParse(json, SampleJsonContext.Default.SamplePayload, new(), out document);
}

/// <summary>
/// A document whose converter fails with a non-JSON exception on read, so
/// the trio's "only JsonException is absorbed" clause has a case to pin.
/// </summary>
[JsonConverter(typeof(ThrowingDocumentJsonConverter))]
public sealed class ThrowingDocument : IJsonDocument<ThrowingDocument>
{
    public static ThrowingDocument Empty { get; } = new();

    public string ToJson() => JsonSerializer.Serialize(this, SampleJsonContext.Default.ThrowingDocument);

    public static ThrowingDocument FromJson(string json) =>
        CanonicalJson.Parse(json, SampleJsonContext.Default.ThrowingDocument);

    public static bool TryFromJson(string? json, out ThrowingDocument document) =>
        CanonicalJson.TryParse(json, SampleJsonContext.Default.ThrowingDocument, Empty, out document);
}

public sealed class ThrowingDocumentJsonConverter : JsonConverter<ThrowingDocument>
{
    public override ThrowingDocument? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) =>
        throw new InvalidOperationException("Deliberate non-JSON failure on read.");

    public override void Write(
        Utf8JsonWriter writer,
        ThrowingDocument value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteEndObject();
    }
}
