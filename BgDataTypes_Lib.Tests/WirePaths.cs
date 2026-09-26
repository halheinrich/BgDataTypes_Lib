using System.Text.Json;
using System.Text.Json.Nodes;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The two serialization paths every wire pin holds on: the reflection
/// resolver (no options at all, what a consumer without a context runs) and
/// <see cref="BgDataTypesJsonContext"/> alone (what a trimmed consumer runs).
/// Neither registers a converter: the types bundle their own.
/// </summary>
internal static class WirePaths
{
    public static readonly JsonSerializerOptions Reflection = new();

    public static readonly JsonSerializerOptions Context = new()
    {
        TypeInfoResolver = BgDataTypesJsonContext.Default
    };

    /// <summary>Both paths, named for a failure message.</summary>
    public static readonly (string Name, JsonSerializerOptions Options)[] Both =
        [("reflection", Reflection), ("context", Context)];

    /// <summary><paramref name="value"/> written as <typeparamref name="T"/> and read back as <typeparamref name="T"/>.</summary>
    public static T RoundTrip<T>(T value, JsonSerializerOptions options) =>
        JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, options), options)!;

    /// <summary><paramref name="value"/>'s document as <typeparamref name="T"/>, to edit.</summary>
    public static JsonObject Document<T>(T value) =>
        JsonNode.Parse(JsonSerializer.Serialize(value, Context))!.AsObject();

    /// <summary>
    /// Asserts <paramref name="json"/> read as <typeparamref name="T"/> is refused
    /// with a <see cref="JsonException"/> on both paths, and returns the
    /// context path's, so a caller can read what it carries.
    /// </summary>
    public static JsonException AssertRefused<T>(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<T>(json, Reflection));
        return Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<T>(json, Context));
    }
}
