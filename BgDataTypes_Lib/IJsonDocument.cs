using System.Text.Json;

namespace BgDataTypes_Lib;

/// <summary>
/// A type with one canonical JSON form and one restore policy — the
/// persistence trio, made a contract. A fail-loud parse
/// (<see cref="FromJson"/>), a non-throwing restore whose out is always
/// usable (<see cref="TryFromJson"/>), and the canonical writer
/// (<see cref="ToJson"/>) — the shape that <c>FilterConfig</c>
/// (XgFilter_Lib) and <c>QuizMix</c> (BgGame_Lib) each hand-wrote and whose
/// doc comments named the other as the convention to copy. Those two are
/// this interface's first implementers (halheinrich/backgammon#190, legs
/// (B) and (C)); <see cref="NamedCollection{TValue, TSelf}"/> is the third
/// and the first generic consumer, storing any implementer by name.
///
/// <para>
/// <b>The contract each member carries.</b> <see cref="FromJson"/> is the
/// inverse of <see cref="ToJson"/> and rejects everything else:
/// <see cref="ArgumentNullException"/> for a null string,
/// <see cref="ArgumentException"/> for the literal <c>null</c> token (a
/// token that parses but yields no document), <see cref="JsonException"/>
/// for malformed input or a violation of the type's own wire contract.
/// <see cref="TryFromJson"/> absorbs exactly those three failures and
/// yields the type's inert default in each — the empty collection, a blank
/// mix, a default config — so a consumer restoring from a store it does not
/// control needs no knowledge of the exception taxonomy; any other exception
/// propagates, because it is a bug, not bad input. <see cref="ToJson"/>
/// writes the canonical form: what <see cref="FromJson"/> reads back to an
/// equal value, and what a document embedding this type stores. The bodies
/// of the two readers are single-sourced in <see cref="CanonicalJson"/>;
/// an implementer forwards to them with its own serializer metadata.
/// </para>
///
/// <para>
/// <b>Trim-safe by construction.</b> An implementer resolves its metadata
/// from a source-generated <see cref="System.Text.Json.Serialization.JsonSerializerContext"/>,
/// never from the reflection-bound <see cref="JsonSerializer"/> overloads,
/// and a type whose wire form is hand-written bundles that form as a
/// type-level <c>[JsonConverter]</c> — public, so a downstream context that
/// embeds the type can instantiate the converter from its generated code
/// (SYSLIB1220 otherwise; see <see cref="BgDataTypesJsonContext"/>). The
/// trio is then usable from a trimmed consumer without that consumer naming
/// anything.
/// </para>
/// </summary>
/// <typeparam name="TSelf">The implementing type itself.</typeparam>
public interface IJsonDocument<TSelf> where TSelf : IJsonDocument<TSelf>?
{
    /// <summary>
    /// Deserializes a <typeparamref name="TSelf"/> from its canonical JSON
    /// representation — the inverse of <see cref="ToJson"/>. Fail-loud.
    /// </summary>
    /// <param name="json">A JSON string, typically produced by <see cref="ToJson"/>.</param>
    /// <returns>The materialized document.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="json"/> is the literal <c>null</c> token, which
    /// yields no document.
    /// </exception>
    /// <exception cref="JsonException">
    /// <paramref name="json"/> is malformed or violates the type's wire
    /// contract.
    /// </exception>
    static abstract TSelf FromJson(string json);

    /// <summary>
    /// Non-throwing counterpart to <see cref="FromJson"/>. Absorbs the three
    /// ways a restore can fail — a null <paramref name="json"/> (a store
    /// never written), the literal <c>null</c> token, or
    /// malformed/contract-violating JSON — and yields the type's inert
    /// default in each case. Any other exception propagates.
    /// </summary>
    /// <param name="json">
    /// The candidate JSON, or null. Typically read straight from a
    /// persistence store whose contents the caller does not control.
    /// </param>
    /// <param name="document">
    /// On return, always a usable document: the restored instance on
    /// success, or the type's inert default on failure.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="json"/> was successfully
    /// deserialized; <see langword="false"/> otherwise, letting a caller
    /// react to a failed restore without catching exceptions.
    /// </returns>
    static abstract bool TryFromJson(string? json, out TSelf document);

    /// <summary>
    /// Serializes this document to its canonical JSON representation — the
    /// inverse of <see cref="FromJson"/>.
    /// </summary>
    /// <returns>The canonical JSON string.</returns>
    string ToJson();
}
