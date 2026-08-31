using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// The string-token-exact enum converter: a
/// <see cref="JsonStringEnumConverter{TEnum}"/> that rejects numeric tokens on
/// read (and refuses to write an undefined value as a number), for
/// attribute-form registration — where the base type's
/// <c>allowIntegerValues: false</c> knob is otherwise unreachable, because an
/// attribute can only name a converter type, not pass it constructor arguments.
///
/// <para>Every enum in this library serializes as its declared member name, and
/// the reader is the inverse of its writer: it accepts those names and nothing
/// else. The base converter's default would also accept an integer ordinal on
/// read — silently re-coupling wire and durable payloads to member declaration
/// numbering, which <see cref="AnalysisLevel"/> in particular explicitly
/// reserves the right to change (its order is contractual and its members
/// interleave, so an inserted member renumbers everything after it; the 2026-08-28
/// insertion of <see cref="AnalysisLevel.Ply3Red"/> is the precedent). No writer
/// here ever emits an ordinal, so accepting one could only decode a stale
/// numbering or mask corruption (halheinrich/backgammon#164).</para>
///
/// <para>Deliberately no naming policy: the declared name <i>is</i> the wire
/// token for these types, which is what makes renumbering safe, and applying a
/// policy here would silently rewrite every token already in stored JSON. Name
/// matching on read stays case-insensitive — the base converter's behavior,
/// which has no knob to change — so the strictness closed here is token kind,
/// not case.</para>
/// </summary>
/// <typeparam name="TEnum">The enum type the converter handles.</typeparam>
public sealed class StrictJsonStringEnumConverter<TEnum> : JsonStringEnumConverter<TEnum>
    where TEnum : struct, Enum
{
    /// <summary>Creates the converter; attribute-form registration uses this.</summary>
    public StrictJsonStringEnumConverter()
        : base(namingPolicy: null, allowIntegerValues: false)
    {
    }
}
