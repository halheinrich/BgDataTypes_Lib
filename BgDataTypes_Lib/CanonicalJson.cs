using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace BgDataTypes_Lib;

/// <summary>
/// The shared bodies of the <see cref="IJsonDocument{TSelf}"/> readers: the
/// one place the trio's restore policy is written. An implementer's
/// <c>FromJson</c> forwards to <see cref="Parse{T}"/> and its
/// <c>TryFromJson</c> to <see cref="TryParse{T}"/>, each passing the
/// serializer metadata from its own source-generated context — so the
/// exception taxonomy (<see cref="ArgumentNullException"/> /
/// <see cref="ArgumentException"/> / <see cref="JsonException"/>) and the
/// absorb-exactly-those-three rule are encoded once, not restated per
/// implementer. The writer has no counterpart here: <c>ToJson</c> is the
/// serializer call against the same metadata and carries no policy.
///
/// <para>
/// Every path here takes a <see cref="JsonTypeInfo{T}"/> and none touches
/// the reflection-bound <see cref="JsonSerializer"/> overloads, so a caller
/// passing source-generated metadata is trim-safe end to end.
/// </para>
/// </summary>
public static class CanonicalJson
{
    /// <summary>
    /// The fail-loud reader: deserializes <paramref name="json"/> through
    /// <paramref name="typeInfo"/>, rejecting a null string, the literal
    /// <c>null</c> token, and anything the type's wire contract rejects.
    /// </summary>
    /// <typeparam name="T">The document type.</typeparam>
    /// <param name="json">A JSON string, typically produced by the type's <c>ToJson</c>.</param>
    /// <param name="typeInfo">The type's serializer metadata, from its source-generated context.</param>
    /// <returns>The materialized document.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="json"/> or <paramref name="typeInfo"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="json"/> is the literal <c>null</c> token, which
    /// yields no document.
    /// </exception>
    /// <exception cref="JsonException">
    /// <paramref name="json"/> is malformed or violates the type's wire
    /// contract.
    /// </exception>
    public static T Parse<T>(string json, JsonTypeInfo<T> typeInfo)
        where T : IJsonDocument<T>
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(typeInfo);

        return JsonSerializer.Deserialize(json, typeInfo)
            ?? throw new ArgumentException(
                $"JSON deserialized to null; expected a {typeof(T).Name} object.",
                nameof(json));
    }

    /// <summary>
    /// The non-throwing reader: absorbs a null <paramref name="json"/>, the
    /// literal <c>null</c> token, and any <see cref="JsonException"/>,
    /// yielding <paramref name="fallback"/> in each case. Any other
    /// exception propagates — it is a bug, not bad input.
    /// </summary>
    /// <typeparam name="T">The document type.</typeparam>
    /// <param name="json">The candidate JSON, or null.</param>
    /// <param name="typeInfo">The type's serializer metadata, from its source-generated context.</param>
    /// <param name="fallback">
    /// The type's inert default, yielded on every failure so the out is
    /// always usable.
    /// </param>
    /// <param name="document">
    /// On return, the restored instance on success, or
    /// <paramref name="fallback"/> on failure.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="json"/> was successfully
    /// deserialized; <see langword="false"/> otherwise.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="typeInfo"/> or <paramref name="fallback"/> is null.
    /// </exception>
    public static bool TryParse<T>(string? json, JsonTypeInfo<T> typeInfo, T fallback, out T document)
        where T : IJsonDocument<T>
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        ArgumentNullException.ThrowIfNull(fallback);

        if (json is not null)
        {
            try
            {
                if (JsonSerializer.Deserialize(json, typeInfo) is { } parsed)
                {
                    document = parsed;
                    return true;
                }
            }
            catch (JsonException)
            {
                // Malformed or contract-violating JSON falls through to the
                // fallback below; any other (unexpected) exception is
                // intentionally left to propagate.
            }
        }

        document = fallback;
        return false;
    }
}
