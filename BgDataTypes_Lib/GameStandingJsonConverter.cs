using System.Text.Json;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// The wire form of a <see cref="GameStanding"/>: it dispatches on the
/// standing's explicit <c>"Kind"</c> member and delegates the document to that
/// kind's generated contract — <see cref="MoneyStanding"/>'s or
/// <see cref="MatchStanding"/>'s, resolved through the active options — and
/// does nothing else. It is the one dispatch mechanism, the internal
/// <see cref="KindDispatch"/>; this converter supplies only the mapping from
/// each kind to its contract, and names no member of either kind. Bundled on
/// the type; consumers register nothing.
/// </summary>
/// <remarks>
/// Why a dispatching converter and not <see cref="JsonPolymorphicAttribute"/>
/// is stated on <see cref="BgDecisionDataJsonConverter"/>, whose measurements
/// hold for this family alike. Public by necessity, as every converter named
/// by an attribute here: a downstream context whose documents embed a
/// standing (a producer's serialized <see cref="IGameInfo"/> implementer)
/// instantiates it from its own generated code (see
/// <see cref="BgDataTypesJsonContext"/>). It stops the generator's walk at the
/// base, so the two kinds are declared on the context explicitly and resolved
/// through the active options at run time.
/// </remarks>
public sealed class GameStandingJsonConverter : JsonConverter<GameStanding>
{
    /// <inheritdoc/>
    public override GameStanding? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        KindDispatch.Read<GameStanding, SessionKind>(
            ref reader, options, "game standing", static kind => kind switch
            {
                SessionKind.Money => typeof(MoneyStanding),
                SessionKind.Match => typeof(MatchStanding),
                _ => null,
            });

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, GameStanding value, JsonSerializerOptions options) =>
        KindDispatch.Write(writer, value, options);
}
