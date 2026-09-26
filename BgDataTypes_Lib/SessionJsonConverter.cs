using System.Text.Json;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// The wire form of a <see cref="Session"/>: it dispatches on the session's
/// explicit <c>"Kind"</c> member and delegates the document to that kind's
/// generated contract — <see cref="MoneySession"/>'s or
/// <see cref="MatchSession"/>'s, resolved through the active options — and does
/// nothing else. It is the decision kind's mechanism reused, not a second one:
/// finding the kind wherever it sits and refusing a document without exactly
/// one known kind are stated once, on the internal <see cref="KindDispatch"/>,
/// and this converter supplies only the mapping from each kind to its
/// contract. It names no member of either kind. Bundled on the type;
/// consumers register nothing.
/// </summary>
/// <remarks>
/// Why a dispatching converter and not <see cref="JsonPolymorphicAttribute"/>
/// is stated on <see cref="BgDecisionDataJsonConverter"/>, whose measurements
/// hold for this family alike. Public by necessity, as every converter named
/// by an attribute here: a downstream context whose documents embed a session
/// instantiates it from its own generated code (see
/// <see cref="BgDataTypesJsonContext"/>). It stops the generator's walk at the
/// base, so the two kinds are declared on the context explicitly and resolved
/// through the active options at run time.
/// </remarks>
public sealed class SessionJsonConverter : JsonConverter<Session>
{
    /// <inheritdoc/>
    public override Session? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        KindDispatch.Read<Session, SessionKind>(
            ref reader, options, "session", static kind => kind switch
            {
                SessionKind.Money => typeof(MoneySession),
                SessionKind.Match => typeof(MatchSession),
                _ => null,
            });

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Session value, JsonSerializerOptions options) =>
        KindDispatch.Write(writer, value, options);
}
