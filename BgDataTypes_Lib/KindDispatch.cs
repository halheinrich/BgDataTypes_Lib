using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace BgDataTypes_Lib;

/// <summary>
/// How a document that states its kind is read as that kind — the one
/// mechanism behind every converter here that reads a closed family of kinds
/// (<see cref="BgDecisionDataJsonConverter"/>, a decision's;
/// <see cref="SessionJsonConverter"/>, a session's;
/// <see cref="SessionTermsJsonConverter"/>, a session's terms';
/// <see cref="GameStandingJsonConverter"/>, a game's standing's). Each such
/// converter supplies only its family's mapping from a kind to that kind's
/// generated contract; finding the kind, refusing a document without exactly
/// one known kind, and handing the whole document to the contract are stated
/// here, once.
/// </summary>
/// <remarks>
/// <para>
/// <b>The kind is a real member,</b> <c>"Kind"</c>, on every kinded type: each
/// kind's contract writes it first, whatever static type the value is
/// serialized as, and requires it. A reader finds it wherever it sits — JSON
/// objects are unordered — by scanning a copy of the reader; the serializer
/// hands a converter the whole value buffered, so the look-ahead is safe on
/// every read path. The member is matched as the kinds' contracts match it:
/// its name under the options' naming policy, case-insensitively when the
/// options say so.
/// </para>
/// <para>
/// <b>Or it is a member's.</b> A family whose kind is one of its members'
/// kind — a session's is its terms' (<see cref="Session"/>) — states it once,
/// in that member, never again beside it. The dispatch then finds the member,
/// once, wherever it sits, and the kind inside it, once, wherever it sits
/// there; the refusals are the same, naming the member's kind
/// (<c>"Terms.Kind"</c>).
/// </para>
/// <para>
/// <b>Every refusal is a <see cref="JsonException"/></b>, on the reflection
/// path and through <see cref="BgDataTypesJsonContext"/> alike: a document
/// that is not an object, a missing kind, a kind stated twice, an unknown or
/// non-string kind (the strict enum token of
/// <see cref="StrictJsonStringEnumConverter{TEnum}"/>); for a kind stated in a
/// member, also that member stated twice or not an object. A member of
/// another kind is the kind's own contract's refusal — each kind disallows
/// unmapped members — and so is every construction rule it holds its members
/// to.
/// </para>
/// </remarks>
internal static class KindDispatch
{
    /// <summary>The name of the member every kinded document states its kind in.</summary>
    internal const string KindMember = "Kind";

    /// <summary>
    /// Reads the object at <paramref name="reader"/> as the kind it states,
    /// through that kind's contract.
    /// </summary>
    /// <typeparam name="T">The family's base type.</typeparam>
    /// <typeparam name="TKind">The family's kind, as a value.</typeparam>
    /// <param name="reader">The reader, at the object's start.</param>
    /// <param name="options">The active options, which resolve the kind's contract.</param>
    /// <param name="document">What the family's documents are called, for a refusal's message ("decision record").</param>
    /// <param name="contractOf">The type whose contract reads a kind; <see langword="null"/> for none.</param>
    /// <param name="within">
    /// The member whose object states the kind, for a family whose kind is that
    /// member's; <see langword="null"/> when the document states its own.
    /// </param>
    /// <exception cref="JsonException">The document does not state exactly one known kind, or its kind's contract refuses it.</exception>
    internal static T? Read<T, TKind>(
        ref Utf8JsonReader reader, JsonSerializerOptions options, string document, Func<TKind, Type?> contractOf,
        string? within = null)
        where T : class
        where TKind : struct, Enum
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"A {document} is a JSON object (got {reader.TokenType}).");

        string kindName = Named(KindMember, options);
        TKind kind = within is null
            ? Find<TKind>(reader, options, document, kindName)
            : FindWithin<TKind>(reader, options, document, Named(within, options), kindName);
        Type contract = contractOf(kind)
            ?? throw new JsonException($"Unknown {document} kind {kind}.");

        return (T?)JsonSerializer.Deserialize(ref reader, options.GetTypeInfo(contract));
    }

    /// <summary>
    /// Writes <paramref name="value"/> through its own kind's contract, which
    /// writes the kind first.
    /// </summary>
    internal static void Write<T>(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        where T : class =>
        JsonSerializer.Serialize(writer, value, options.GetTypeInfo(value.GetType()));

    /// <summary>
    /// The one kind the object at <paramref name="scan"/> states, found
    /// wherever it sits. <paramref name="scan"/> is a copy of the caller's
    /// reader, so the caller's position is untouched.
    /// </summary>
    /// <param name="scan">A copy of the reader, at the object's start.</param>
    /// <param name="options">The active options, which name and read the kind.</param>
    /// <param name="document">What the family's documents are called, for a refusal's message.</param>
    /// <param name="label">How a refusal names the kind: its member's name, or the path to it.</param>
    private static TKind Find<TKind>(Utf8JsonReader scan, JsonSerializerOptions options, string document, string label)
        where TKind : struct, Enum
    {
        string name = Named(KindMember, options);
        var token = (JsonTypeInfo<TKind>)options.GetTypeInfo(typeof(TKind));

        TKind? kind = null;
        while (scan.Read() && scan.TokenType == JsonTokenType.PropertyName)
        {
            bool isKind = Is(ref scan, name, options);
            scan.Read();
            if (!isKind)
            {
                scan.Skip();
                continue;
            }
            if (kind is not null)
                throw new JsonException($"A {document} states its {label} once.");
            kind = JsonSerializer.Deserialize(ref scan, token);
        }

        return kind ?? throw Missing<TKind>(document, label);
    }

    /// <summary>
    /// The one kind the object at <paramref name="scan"/> states in its
    /// <paramref name="member"/>, which it states once, wherever it sits.
    /// </summary>
    private static TKind FindWithin<TKind>(
        Utf8JsonReader scan, JsonSerializerOptions options, string document, string member, string kindName)
        where TKind : struct, Enum
    {
        string label = $"{member}.{kindName}";

        TKind? kind = null;
        bool stated = false;
        while (scan.Read() && scan.TokenType == JsonTokenType.PropertyName)
        {
            bool isMember = Is(ref scan, member, options);
            scan.Read();
            if (isMember)
            {
                if (stated)
                    throw new JsonException($"A {document} states its {member} once.");
                if (scan.TokenType != JsonTokenType.StartObject)
                    throw new JsonException($"A {document}'s {member} is a JSON object (got {scan.TokenType}).");
                stated = true;
                kind = Find<TKind>(scan, options, document, label);
            }
            scan.Skip();
        }

        return kind ?? throw Missing<TKind>(document, label);
    }

    /// <summary>The refusal of a document stating no kind.</summary>
    private static JsonException Missing<TKind>(string document, string label)
        where TKind : struct, Enum =>
        new($"A {document} states its {label} — {string.Join(" or ", Enum.GetNames<TKind>())}. A document without one is not a {document} of this shape.");

    /// <summary>Whether the property name at <paramref name="scan"/> is <paramref name="name"/>, as the contracts match names.</summary>
    private static bool Is(ref Utf8JsonReader scan, string name, JsonSerializerOptions options) =>
        options.PropertyNameCaseInsensitive
            ? string.Equals(scan.GetString(), name, StringComparison.OrdinalIgnoreCase)
            : scan.ValueTextEquals(name);

    /// <summary>A member's name under the options' naming policy.</summary>
    private static string Named(string member, JsonSerializerOptions options) =>
        options.PropertyNamingPolicy?.ConvertName(member) ?? member;
}
