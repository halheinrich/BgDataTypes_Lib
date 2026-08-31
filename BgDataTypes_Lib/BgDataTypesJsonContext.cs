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
/// <see cref="DiceRoll"/>, and the four enums). Composite parts
/// (<see cref="PositionData"/>, <see cref="DecisionData"/>,
/// <see cref="DescriptiveData"/>, <see cref="PlayOutcomeData"/>,
/// <see cref="PlayCandidate"/>) ride the generator's property-graph walk
/// from the document roots. <see cref="Move"/> is declared explicitly
/// because no walk can reach it: <see cref="Play"/>'s converter stops the
/// generator at <see cref="Play"/>, yet emits <see cref="Move"/> elements by
/// resolving them through the active
/// <see cref="System.Text.Json.JsonSerializerOptions"/> at runtime — this
/// declaration is what that resolution finds in a trimmed consumer. A
/// completeness test keeps the declarations honest: the serialized-property
/// closure of the roots must resolve through this context, member by member.
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
[JsonSerializable(typeof(DecisionRow))]
[JsonSerializable(typeof(Play))]
[JsonSerializable(typeof(Move))]
[JsonSerializable(typeof(DecisionId))]
[JsonSerializable(typeof(ProblemKey))]
[JsonSerializable(typeof(DiceRoll))]
[JsonSerializable(typeof(AnalysisMode))]
[JsonSerializable(typeof(AnalysisLevel))]
[JsonSerializable(typeof(CubeAction))]
[JsonSerializable(typeof(CubeOwner))]
public sealed partial class BgDataTypesJsonContext : JsonSerializerContext
{
}
