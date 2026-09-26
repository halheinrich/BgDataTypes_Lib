using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// The source-generated <see cref="JsonSerializerContext"/> for this
/// library's wire surface — trim-safe <c>System.Text.Json</c> metadata for
/// every type this library puts on a wire, produced at compile time instead
/// of by runtime reflection (halheinrich/backgammon#129 leg 1). The
/// mechanism changes, the bytes do not: serialization through this context
/// is byte-identical to the reflection path, pinned by test, and every
/// bundled <c>[JsonConverter]</c> (the strict enum converters,
/// <see cref="PlayJsonConverter"/>, the canonical-string converters) is
/// honored unchanged.
///
/// <para>
/// <b>What is declared, and why.</b> The <c>[JsonSerializable]</c> roots are
/// the types that are wire units in their own right: the two document roots
/// (<see cref="BgDecisionData"/>, <see cref="DecisionRow"/>) and the types
/// that define their own wire token via a bundled converter
/// (<see cref="Play"/>, <see cref="DecisionId"/>, <see cref="ProblemKey"/>,
/// <see cref="DiceRoll"/>, <see cref="BoardPosition"/>, and the seven enums — <see cref="CubeClaim"/>
/// among them ahead of its first embedding document, so the claim
/// vocabulary of halheinrich/backgammon#86 is born source-genned and
/// downstream contexts chain rather than re-cover it). Composite parts
/// (<see cref="PositionData"/>, <see cref="CheckerPlayDecisionData"/>,
/// <see cref="CubeDecisionData"/>, <see cref="DescriptiveData"/>,
/// <see cref="PlayCandidate"/>) ride the generator's property-graph walk
/// from the roots. Two converters stop that walk, so what lies past them is
/// declared explicitly and resolved through the active
/// <see cref="System.Text.Json.JsonSerializerOptions"/> at runtime — these
/// declarations are what that resolution finds in a trimmed consumer:
/// <see cref="Play"/>'s converter emits <see cref="Move"/> elements, and
/// <see cref="BgDecisionDataJsonConverter"/> delegates to the two kinds,
/// <see cref="CheckerPlayDecision"/> and <see cref="CubeDecision"/>, reading
/// <see cref="DecisionKind"/> to choose. A completeness test keeps the
/// declarations honest: the serialized-property closure of the roots must
/// resolve through this context, member by member.
/// </para>
///
/// <para>
/// <b>Absence on the wire</b> (halheinrich/backgammon#222) — the one
/// statement of the rule for every type in this graph. Every serialized
/// member is one of two kinds, and no member arrives silently as a default:
/// </para>
/// <list type="bullet">
/// <item><description>a member that must be present is <c>required</c>: an
/// object initializer that omits it does not compile, and a document that
/// omits it is a <see cref="System.Text.Json.JsonException"/> on both the
/// reflection path and this context's (C# <c>required</c> on the init
/// members; <see cref="JsonRequiredAttribute"/> on <see cref="Move"/>'s
/// constructor-bound pair, which <c>required</c> cannot reach);</description></item>
/// <item><description>a member whose absence means something is nullable and
/// not required: absent, it reads as <see langword="null"/> on both paths,
/// and its own documentation says what <see langword="null"/>
/// means.</description></item>
/// </list>
/// <para>
/// The paths agree because no member keeps a property initializer for the
/// reflection path to honour and the generated creator to drop: a required
/// member needs none, and a nullable one defaults to
/// <see langword="null"/>. A test walks every member of the graph and pins
/// both halves on both paths.
/// </para>
/// <para>
/// <b>An explicit <c>null</c> for a member that is not nullable is a
/// <see cref="System.Text.Json.JsonException"/></b>, on both paths and
/// whatever the caller's options. A value-type member's <c>null</c> the
/// serializer refuses itself; a reference member's the type refuses — its
/// init guard's <see cref="ArgumentNullException"/>, which a document gets as
/// a <see cref="System.Text.Json.JsonException"/> (the rule below).
/// <see cref="System.Text.Json.JsonSerializerOptions.RespectNullableAnnotations"/>
/// was measured on .NET 10 (2026-09-25): set, it refuses such a
/// <c>null</c> on both paths, but it is off by default, and on the
/// reflection path it is the caller's option to set. So nothing here
/// depends on it, and this context does not set it: one mechanism, the same
/// on every path. The same walk pins it, member by member.
/// </para>
/// <para>
/// <b>A decision's kind is explicit, and a member of the other kind is
/// refused</b> (halheinrich/backgammon#273). Every decision states its
/// <see cref="DecisionKind"/> as a real member, and no reader infers it from
/// which members are present; see <see cref="BgDecisionDataJsonConverter"/>.
/// The types whose members belong to one kind — the two records and their
/// two <c>Decision</c> categories — disallow unmapped members, so a document
/// whose kind contradicts its members is a
/// <see cref="System.Text.Json.JsonException"/> rather than a record that
/// silently dropped the other kind's data. The shared categories
/// (<see cref="PositionData"/>, <see cref="DescriptiveData"/>,
/// <see cref="PlayCandidate"/>) keep the serializer's default and ignore a
/// member they do not know: no member of theirs can belong to the other kind.
/// </para>
/// <para>
/// <b>A construction rule a document breaks is a
/// <see cref="System.Text.Json.JsonException"/></b>, whatever type the
/// document is read as: a record as <see cref="BgDecisionData"/> or as its own
/// kind, or one of its categories on its own. Code that breaks a rule gets
/// the init guard's <see cref="ArgumentException"/>; a document gets the same
/// fault as a <see cref="System.Text.Json.JsonException"/> carrying it, so a
/// reader that absorbs malformed input absorbs every one. A type tells the
/// two apart by how it was constructed — see the next paragraph.
/// </para>
/// <para>
/// <b>The serializer constructors, and why the platform forces them</b> —
/// the one statement; each such constructor points here. A type must know,
/// inside an init setter, whether code or a document is building it, and
/// .NET 10's serializer offers no hook that runs in time on both paths. Two
/// were measured and fail (SDK 10.0.401, 2026-09-25). A flag set in
/// <see cref="IJsonOnDeserializing"/> comes too late: a generated context
/// sets the init and <c>required</c> members in an object initializer before
/// that callback runs, though the reflection path runs it first. A
/// constructor taking every member needs <c>[SetsRequiredMembers]</c>, which
/// silently drops the JSON-required meaning of <c>required</c>. What does run
/// before every init setter on both paths is a
/// <see cref="JsonConstructorAttribute"/> constructor. So each type that holds
/// its members to a rule — <see cref="CheckerPlayDecision"/>,
/// <see cref="CubeDecision"/>, <see cref="CheckerPlayDecisionData"/>,
/// <see cref="CubeDecisionData"/>, <see cref="PlayCandidate"/>,
/// <see cref="DescriptiveData"/> — has an internal one-parameter serializer
/// constructor beside the public parameterless one code uses. It marks the
/// instance as read, and each guard then refuses a breach as a
/// <see cref="System.Text.Json.JsonException"/> carrying the guard's exception.
/// A serializer constructor must bind one wire member. A kind binds its
/// <c>"Kind"</c>, which the base holds to the type. <b>A category binds its
/// first member only because a serializer constructor must bind one</b>:
/// nothing about that member is special, and binding it changes nothing
/// about how it is read. It stays required, and the absence walk
/// (<c>WireAbsenceTests</c>) fails if it ever stops being so, since a member
/// that is neither required nor nullable breaks the walk's first rule. The
/// constructors are internal, so code can never mark an instance as read.
/// This is the separate-internal-constructor pattern answering a measured
/// ordering, not a way around validation: every rule still runs, on the same
/// setters. <see cref="DecisionRow"/> needs no such constructor. It has no
/// public one, and it holds a row read from JSON to its rules whole, in
/// <see cref="IJsonOnDeserialized"/>.
/// </para>
///
/// <para>
/// <b>The composition pattern</b> (the halheinrich/backgammon#129 arc's
/// standing shape, set here for every downstream leg). Each producer repo
/// owns one public context covering its own wire types; a consumer combines
/// the contexts of every producer whose types appear in its documents by
/// chaining type-info resolvers — no consumer-side converter registration,
/// no glue types:
/// <code>
/// var options = new JsonSerializerOptions
/// {
///     TypeInfoResolver = JsonTypeInfoResolver.Combine(
///         TheConsumersOwnContext.Default,
///         BgDataTypesJsonContext.Default)
/// };
/// </code>
/// (equivalently, add each context to
/// <c>JsonSerializerOptions.TypeInfoResolverChain</c>). The chain is
/// searched in order, first resolver claiming a type wins — order contexts
/// most-derived-first so a downstream repo could shadow a type it owns,
/// though none should need to. A downstream context whose documents embed
/// these types generates metadata for them transitively in its own assembly
/// too; chaining this context instead keeps the coverage and its tests
/// single-sourced here, where the converters live.
/// </para>
///
/// <para>
/// <b>Metadata-only generation, deliberately — part of the pattern.</b>
/// The default generation mode also emits fast-path serialize handlers,
/// and a fast-path handler binds every nested type resolution to the
/// <em>declaring context's own private options</em>, not the runtime
/// options it was invoked with — silently bypassing the resolver chain.
/// That breaks exactly this arc's seam: a downstream context's fast path
/// reaching <see cref="Play"/> would look up <see cref="Move"/> (which
/// <see cref="PlayJsonConverter"/> resolves through the active options at
/// runtime) in its own options, where it cannot exist, and throw — with
/// this context correctly chained one resolver over. With
/// <see cref="JsonSourceGenerationMode.Metadata"/> on every context in the
/// chain there is no context-private options capture: resolution always
/// flows through the combined options. Downstream contexts must declare
/// the same mode; the chained-consumer test in this repo demonstrates
/// both the failure and the working shape.
/// </para>
/// </summary>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(BgDecisionData))]
[JsonSerializable(typeof(CheckerPlayDecision))]
[JsonSerializable(typeof(CubeDecision))]
[JsonSerializable(typeof(DecisionRow))]
[JsonSerializable(typeof(Play))]
[JsonSerializable(typeof(Move))]
[JsonSerializable(typeof(DecisionId))]
[JsonSerializable(typeof(ProblemKey))]
[JsonSerializable(typeof(DiceRoll))]
[JsonSerializable(typeof(BoardPosition))]
[JsonSerializable(typeof(AnalysisMode))]
[JsonSerializable(typeof(AnalysisLevel))]
[JsonSerializable(typeof(CubeAction))]
[JsonSerializable(typeof(CubeClaim))]
[JsonSerializable(typeof(CubeOwner))]
[JsonSerializable(typeof(DecisionKind))]
[JsonSerializable(typeof(BookEdition))]
public sealed partial class BgDataTypesJsonContext : JsonSerializerContext
{
}
