using System.Text.Json;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// The wire form of a <see cref="BgDecisionData"/>: it dispatches on the
/// decision's explicit <c>"Kind"</c> member and delegates the document to that
/// kind's generated contract — <see cref="CheckerPlayDecision"/>'s or
/// <see cref="CubeDecision"/>'s, resolved through the active options — and
/// does nothing else. It never names a member of either kind: the members,
/// their order, what is required and what is refused are the kinds' own
/// contracts, generated from the types, so there is no second copy of them
/// here. Bundled on the type; consumers register nothing.
/// </summary>
/// <remarks>
/// <para>
/// <b>What it guarantees</b> (halheinrich/backgammon#273, the records leg):
/// </para>
/// <list type="bullet">
/// <item><description><b>The kind is a real member of every decision</b>
/// (<see cref="BgDecisionData.Kind"/>), written first, whatever static type
/// the value is serialized as — the kinds' contracts write it, so a value
/// serialized as a <see cref="CubeDecision"/> carries it as surely as one
/// serialized as a <see cref="BgDecisionData"/>.</description></item>
/// <item><description><b>Reading is order-independent</b>, because JSON objects
/// are unordered: the kind is found wherever it sits. How — and every refusal
/// of a document without exactly one known kind — is the one mechanism every
/// kinded family here shares, stated on the internal <see cref="KindDispatch"/>;
/// this converter supplies only the mapping from a kind to its
/// contract.</description></item>
/// <item><description><b>Every refusal is a <see cref="JsonException"/></b>, on
/// the reflection path and through <see cref="BgDataTypesJsonContext"/>
/// alike, so it is absorbed wherever malformed input is
/// (<see cref="CanonicalJson"/>'s readers absorb exactly that): a missing
/// kind, a kind stated twice, an unknown or non-string kind (the strict enum
/// token), a member of the other kind (each kind disallows unmapped
/// members), a missing member, and a construction rule the record breaks.
/// That last one is not this converter's doing: each kind refuses a broken
/// rule read from a document as a <see cref="JsonException"/> itself,
/// carrying the init guard's <see cref="ArgumentException"/>, so a kind read
/// directly by its own type refuses exactly as it does here (the wire rule on
/// <see cref="BgDataTypesJsonContext"/>).</description></item>
/// </list>
/// <para>
/// <b>Why not <see cref="JsonPolymorphicAttribute"/>.</b> The built-in
/// polymorphism was measured on .NET 10 (System.Text.Json 10.0.x, 2026-09-25),
/// on the reflection path and through a metadata-only source-generated
/// context alike, over an abstract base with sealed derived types, and it
/// breaks the requirements above three ways:
/// </para>
/// <list type="number">
/// <item><description>a document with no discriminator, or with it anywhere
/// but first, is refused with <see cref="NotSupportedException"/> ("The JSON
/// payload for polymorphic interface or abstract type … must specify a type
/// discriminator"), not <see cref="JsonException"/> — so
/// <see cref="CanonicalJson"/>'s readers and any consumer absorbing malformed
/// input would let it escape, and an old-shape document would read as an
/// unsupported type rather than as malformed;</description></item>
/// <item><description>the discriminator is metadata, not a member: a value
/// serialized through its own static type (<c>CubeDecision</c>) is written
/// with no kind at all, and the base then refuses what it wrote;</description></item>
/// <item><description>and a sealed type cannot carry polymorphism of its own to
/// close that gap ("Specified type … does not support polymorphism").</description></item>
/// </list>
/// <para>
/// Public by necessity, as every converter named by an attribute here: a
/// downstream context whose documents embed a record instantiates it from its
/// own generated code (see <see cref="BgDataTypesJsonContext"/>). Like
/// <see cref="PlayJsonConverter"/>, it stops the generator's walk at the base,
/// so the two kinds are declared on the context explicitly and resolved
/// through the active options at run time — trim-safe, and chained across
/// contexts.
/// </para>
/// </remarks>
public sealed class BgDecisionDataJsonConverter : JsonConverter<BgDecisionData>
{
    /// <inheritdoc/>
    public override BgDecisionData? Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        KindDispatch.Read<BgDecisionData, DecisionKind>(
            ref reader, options, "decision record", static kind => kind switch
            {
                DecisionKind.CheckerPlay => typeof(CheckerPlayDecision),
                DecisionKind.Cube => typeof(CubeDecision),
                _ => null,
            });

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, BgDecisionData value, JsonSerializerOptions options) =>
        KindDispatch.Write(writer, value, options);
}
