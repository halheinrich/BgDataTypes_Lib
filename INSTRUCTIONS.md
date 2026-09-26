# BgDataTypes_Lib

> Collaboration contract: [`../AGENTS.md`](../AGENTS.md)
> Umbrella status & dependency graph: [`../INSTRUCTIONS.md`](../INSTRUCTIONS.md)
> Mission & principles: [`../VISION.md`](../VISION.md)

## Stack

C# / .NET 10 / Class Library / xUnit / BenchmarkDotNet. Pure data types — no parsing, no rendering, no I/O beyond `System.Text.Json` serialization.

## Solution

`D:\Users\Hal\Documents\Visual Studio 2026\Projects\backgammon\BgDataTypes_Lib\BgDataTypes_Lib.slnx`

## Repo

https://github.com/halheinrich/BgDataTypes_Lib — branch `main`.

## Depends on

Atomic by design. BgDataTypes_Lib has no subproject dependencies and must
not gain any. The shared-data layer is the foundation other subprojects
rest on; introducing a subproject dependency here would either create a
circular reference or force the dependency on every consumer transitively.
`System.Text.Json` is the only runtime dependency; the serialized types
that need converters (`CubeOwner`, `CubeAction`, `CubeClaim`, `AnalysisMode`,
`AnalysisLevel`, `Play`, `DecisionId`, `ProblemKey`, `DiceRoll`, `BoardPosition`)
each bundle their own `[JsonConverter]` attribute, and the optional
after-boards name theirs at the property, so consumers do not have to
register converters on their `JsonSerializerOptions`.

## Layout

Three projects under `BgDataTypes_Lib.slnx`, governed by repo-root
`Directory.Build.props` (TFM, `TreatWarningsAsErrors`, XML doc generation)
and `Directory.Packages.props` (Central Package Management — no inline
`Version=` anywhere).

**`BgDataTypes_Lib/`** — the library. Seven areas, one file per type:

- **The decision record and its categories** — `BgDecisionData`, the
  composite every consumer passes around, plus the four orthogonal category
  types it holds (`PositionData`, `DecisionData`, `DescriptiveData`,
  `PlayOutcomeData`) and `PlayCandidate` beneath `DecisionData`. `DecisionRow`
  is the flat sibling shape for CSV/JSON export.
- **Identity** — two distinct keys, deliberately not one. `DecisionId`
  (with `DecisionIdJsonConverter`) is the file-navigation identity: *where
  did this record come from*. `ProblemKey` (with `ProblemKeyJsonConverter`)
  is the content identity: *which problem is this* — the key stats and
  dedupe recognise across files.
- **Move and board primitives** — `Move`, `Play`, the internal
  `PlayChain` and `CanonicalPlay` (a play's display form, behind
  `Play.ToNotation`), `BoardPosition` (an immutable
  position, the one definition of "the same position"), and `BoardState`,
  the one mutable type in the library and the owner of play identity.
  Value types here inherit hot-path zero-alloc constraints from move
  generation.
- **Enums and the depth taxonomy** — `CubeOwner`, `CubeAction`, `CubeClaim`
  (the three-valued doubler claim of SPEC-scoring §3, with
  `CubeClaimExtensions` for the claim→action collapse), and
  `AnalysisMode` × `AnalysisLevel` (the two-axis depth taxonomy), alongside
  the small validated value types `CubeDecisionPair` (a `CubeAction` pair
  with per-half guards), `CubeClaimPair` (its claim-layer counterpart — the
  two-part cube answer), and `DiceRoll` (a canonical unordered roll).
- **Shared consumer contracts** — `IDecisionFilterData`, the filter-layer
  view implemented by `BgDecisionData` and `DecisionRow`, carrying the score
  context a filter needs (`OnRollNeeds`/`OpponentNeeds`, `IsCrawford`,
  `IsMoneyGame`, and the tri-state `IsJacoby?` the money score tokens read);
  `IGameInfo` and `IMatchInfo`, implemented by producers so filter layers
  never reference a producer's concrete types.
- **JSON converters and the serializer context** — `PlayJsonConverter`,
  `DiceRollJsonConverter`, `DecisionIdJsonConverter`,
  `ProblemKeyJsonConverter`, `BoardPositionJsonConverter`, and
  `StrictJsonStringEnumConverter<TEnum>` (the five enums). Each is bundled
  onto its type by a type-level `[JsonConverter]` attribute; consumers
  register nothing. `NullableBoardPositionJsonConverter` is the one named
  at the property instead, on each optional after-board. All are public —
  a downstream `JsonSerializerContext` whose documents embed an annotated
  type must instantiate its converter from generated code, so an internal
  converter fails that generator outright (SYSLIB1220).
  `BgDataTypesJsonContext` is the source-generated context over the whole
  wire surface — see "Source generation & trimming" below.
- **Named-document machinery** (halheinrich/backgammon#190) —
  `IJsonDocument<TSelf>`, the persistence-trio contract every persisted
  document type implements (fail-loud `FromJson`, always-usable
  `TryFromJson`, canonical `ToJson`), and `CanonicalJson`, the shared bodies
  of its two readers; `NamedCollection<TValue, TSelf>`, the immutable
  named collection over any implementer (name rule, canonical order,
  snapshot contract, the trio), with `INamedCollectionSpecialization` (the
  static half a sealed specialization supplies) and the abstract
  `NamedCollectionJsonConverter<TValue, TSelf>` (the strict envelope, closed
  per specialization with its own two wire names). No payload and no
  specialization lives in this library; `FilterConfig` / `QuizMix` and the
  collections over them are downstream. See "Named documents" below.

**`BgDataTypes_Lib.Benchmarks/`** — BenchmarkDotNet harness, an executable
(`OutputType=Exe`) excluded from `dotnet test` by `IsTestProject=false`.
`Program.cs` is the `BenchmarkSwitcher` entry point;
`PlayConstructionBenchmarks.cs` measures every `Play` construction path
against the incremental `Add` spelling. See Benchmarks below.

**`BgDataTypes_Lib.Tests/`** — xUnit, one test class per type or per
behaviour area of a type (`ProblemKeyTests`, `BoardStateTests`,
`PipCountTests`, `RaceTests`, the `*SerializationTests` pair, …). Fixtures
are constructed in code — pure data types need no corpus, so gating tests
never reach into the umbrella `TestData/`. The one deliberate exception is
local-only: `TooGoodCorpusExerciseTests` links `TestData/BgDecisionData/`
into its output and checks the Too Good predicate is exercised by real
converted data (SPEC-scoring §3's acceptance requirement,
halheinrich/backgammon#86) and that no corpus position derives the retired
Too Good / Take pair (the 2026-09-02 amendment,
halheinrich/backgammon#187) — vacuous on an empty or absent corpus by
design, per the AGENTS.md TestData rule, so it cannot gate and nothing on
CI depends on it.

## Architecture

Composite and category types are `class` with `init`-only properties; the
move primitives `Move` (`readonly record struct`) and `Play` (mutable
`struct`) are value types for hot-path zero-alloc reasons inherited from
their move-generation origins. `BoardState` is a `class` but mutable —
the one deliberate exception (see "Mutability exception" below).
Serialization uses `System.Text.Json` with bundled `[JsonConverter]`
attributes: `StrictJsonStringEnumConverter<TEnum>` on `CubeOwner`,
`CubeAction`, `CubeClaim`, `AnalysisMode`, and `AnalysisLevel`,
`PlayJsonConverter` on `Play`,
`DecisionIdJsonConverter` on `DecisionId`, `ProblemKeyJsonConverter` on
`ProblemKey`, `DiceRollJsonConverter` on `DiceRoll`, and
`BoardPositionJsonConverter` on `BoardPosition` (with
`NullableBoardPositionJsonConverter` on the optional after-boards).
Consumers do not need to register any of these converters on their
`JsonSerializerOptions` — the attributes carry the contract on the types
themselves.

### Source generation & trimming

`BgDataTypesJsonContext` (halheinrich/backgammon#129 leg 1) is the public
source-generated `JsonSerializerContext` over this library's wire surface:
trim-safe serializer metadata produced at compile time, byte-identical to
the reflection path (pinned by `BgDataTypesJsonContextTests`), every
bundled converter honored. Its `[JsonSerializable]` roots are the wire
units — the document roots (`BgDecisionData`, `DecisionRow`) and the
converter-bearing token types (`Play`, `Move`, `DecisionId`, `ProblemKey`,
`DiceRoll`, `BoardPosition`, the five enums — `CubeClaim` declared ahead of its first
embedding document so the claim vocabulary is born source-genned and
downstream contexts chain rather than re-cover it); composite parts ride
the generator's graph walk. `Move` must stay declared explicitly: `Play`'s converter stops the
generator's walk at `Play` and resolves `Move` through the active options
at runtime. A completeness test (the halheinrich/backgammon#144
intersection pattern) walks the serialized-property closure of the roots
by reflection and asserts the context resolves every member.

**The composition pattern** (the arc's standing shape, set here for every
downstream leg — ConvertXgToJson_Lib, BgGame_Lib, XgFilter_Lib, BgQuiz):
each producer repo owns one public context covering its own wire types; a
consumer chains resolvers, most-derived-first:

```csharp
var options = new JsonSerializerOptions
{
    TypeInfoResolver = JsonTypeInfoResolver.Combine(
        TheConsumersOwnContext.Default, BgDataTypesJsonContext.Default)
};
```

Two rules keep the chain sound, both discovered and pinned here:

1. **Converters named by `[JsonConverter]` attributes — type-level or
   property-level — stay public.** A downstream context's generator must
   emit `new PlayJsonConverter()`-style instantiations; internal converters
   fail it with SYSLIB1220/SYSLIB1030 at the consumer's compile.
2. **Every context in the chain declares
   `[JsonSourceGenerationOptions(GenerationMode =
   JsonSourceGenerationMode.Metadata)]`.** The default mode also emits
   fast-path serialize handlers, and a fast-path handler binds nested type
   resolution to the *declaring context's own private options* — bypassing
   the chain. A downstream fast path reaching `Play` would look up `Move`
   in its own options, where it cannot exist, and throw at runtime with
   the chain correctly configured. Metadata-only generation keeps every
   resolution on the combined options. The chained-consumer tests in
   `BgDataTypesJsonContextTests` demonstrate both the failure and the
   working shape.

The library declares `IsTrimmable` and runs `EnableTrimAnalyzer` in its own
build (its half of the arc's trim gate): with `TreatWarningsAsErrors`, a
reflection-serialization regression is a build error here, not a
publish-time warning in BgQuiz. `PlayJsonConverter` (de)serializes `Move`
elements via `options.GetTypeInfo` — the trim-safe spelling — rather than
the reflection-bound `JsonSerializer` overloads.

### Absence on the wire

**The rule is stated once, on `BgDataTypesJsonContext`'s summary**
(halheinrich/backgammon#222, settled by best practice at the umbrella's
2026-09-25 review): every serialized member of the wire graph is either
`required` — absent, it is a `JsonException` on the reflection path and the
context's alike, and an object initializer omitting it does not compile —
or nullable and not required, reading as `null` when absent, with its own
doc saying what `null` means. No member arrives silently as a default.
Design points:

- **C# `required`, not `[JsonRequired]`.** Both make absence a
  `JsonException` on both paths; `required` also makes an initializer that
  omits the member a compile error, which is the same fact at the other
  door — an omitted member in code is a silent default too. It is the
  `Id`/`Roll` precedent, and it keeps one notion of "required" rather than
  a wire-only one beside a code one. `Move` is the exception by necessity:
  its pair is constructor-bound, where `required` cannot reach, so it
  carries `[property: JsonRequired]` (an absent `ToPt` would otherwise read
  as 0, a bear-off).
- **Why the paths now agree.** The divergence halheinrich/backgammon#222
  found — the generated creator dropping a property initializer the
  reflection path honoured — has nothing left to act on: a required member
  has no initializer, and a nullable one defaults to `null` on both.
- **The classification, member by member.** Nullable (absent means
  something): `PositionData.IsJacoby`; `DecisionData.UserPlayError`,
  `UserDoubleError`, `UserTakeError`, `UserDoublerAction`,
  `UserTakerAction`; `DescriptiveData.Title`, `Date`, `Event`,
  `SourceFile`; `PlayCandidate`'s six probabilities; both after-boards on
  `PlayOutcomeData` and `DecisionRow`; `DecisionRow.SourceFile` and
  `IsJacoby`. Every other serialized member is required, the record halves
  and `Xgid` included. `WireAbsenceTests` walks the graph from the context's
  own metadata (98 members across eight types) and pins both halves of the
  rule on both paths, plus that every member is exactly one kind.
- **`Unknown` is a value, not an absence.** `AnalysisMode`/`AnalysisLevel`
  (on candidates, cube analyses and rows) are required: "not recorded" is
  spelled `Unknown` by the producer. Making them nullable would give "not
  recorded" two spellings. A document lacking the pair — JSON written
  before the pair existed — is refused rather than read as `Unknown`. The
  2026-09-25 survey found no committed document of the
  `BgDecisionData`/`DecisionRow` shape in any member (BgQuiz's e2e fixtures
  are `.xgp` files, converted at run time), and none in the local
  `TestData` (its `BgDecisionData/` corpus is a different wrapper shape,
  read leniently by `TooGoodCorpusExerciseTests`).
- **Construction states every member.** Producers and tests set every
  required member, including a checker play's inactive cube half and the
  empty strings that used to be defaults. This repo's tests build records
  through `TestRecords` (the test project), whose builders state every
  member and take the ones a test cares about as named arguments; each
  builder default is the member's pre-rule default, so a rewritten test
  kept its meaning.

### Mutability exception

All composite and category types in this library are `class` with
`init`-only properties — except `BoardState`, which is mutable for
hot-path move-generation efficiency. The mutation is its own, never a
caller's (halheinrich/backgammon#281): `Points` is a `ReadOnlySpan<int>`
and `HighPointOccupied` has a private setter, so a write from outside the
library does not compile, and the type's own members keep
`HighPointOccupied` in step (`ApplyMove` / `UndoMove` incrementally, a
full recompute on construction and flip). Hot-path consumers (BgMoveGen's
move generator) use the apply/undo primitives; non-hot-path consumers
advance state via `ApplyPlay`. The class is sealed: a type that guards an
invariant is not open to subclasses, which could add state or ways in the
invariant never sees. Its immutable counterpart is `BoardPosition`, which is
what a board is compared and stored as.

### Data categories

`BgDecisionData` composes four orthogonal category types:

| Type | Fields |
|---|---|
| `PositionData` | `Mop`, `OnRollNeeds`, `OpponentNeeds`, `OnRollPipCount`, `OpponentPipCount`, `CubeSize`, `CubeOwner`, `IsCrawford`, `IsJacoby?` |
| `DecisionData` | `Dice`, `Plays`, `BestPlayIndex`, `UserPlayIndex`, `UserPlayError?`, `IsCube`, `CubeDepth`, `CubeDepthAbbreviation`, `CubeDepthRank`, `CubeAnalysisMode`, `CubeAnalysisLevel`, cube equity/pct fields, `UserDoubleError?`, `UserTakeError?`, `UserDoublerAction?`, `UserTakerAction?` |
| `DescriptiveData` | `MatchLength`, `OnRollName`, `OpponentName`, `Title`, `Date`, `Event`, `SourceFile`, `MoveNumber`, `IsStandardStart` |
| `PlayOutcomeData` | `AfterBestBoard?`, `AfterPlayerBoard?` |

### Shared types

| Type | Notes |
|---|---|
| `CubeOwner` | enum: `OnRoll`, `Opponent`, `Centered` — serializes as string |
| `CubeAction` | enum: `NoDouble`, `Double`, `Take`, `Pass` — a player's cube response, serializes as string. Beaver/raccoon deliberately not yet members (see XML `<remarks>` on the type); enums extend without disturbing existing members. |
| `CubeClaim` | enum: `NoDouble`, `Double`, `TooGood` — the doubler half of a cube answer at the claim layer (SPEC-scoring §1/§3, `halheinrich/backgammon#86`), serializes as string. A claim about the position, not a board action: `NoDouble` and `TooGood` share the identical board action (`CubeAction.NoDouble`), and `CubeClaimExtensions.ToCubeAction` is the single spelling of that collapse. Deliberately *not* a fifth `CubeAction` member — "too good" is a rationale, ruled claim-layer only. Declaration order is the ruled claim axis {No Double, Double, Too Good}, what a UI offering the claims renders. No reverse action→claim mapping exists: the claim is underdetermined by the action alone; the only equities→claim door is `DecisionData.BestDoublerClaim`. |
| `AnalysisMode` | enum: `Unknown`, `Evaluation`, `Rollout`, `BookRollout` — how an XG analysis's numbers were produced; the mode axis of the two-axis depth taxonomy, serializes as string. Always paired with `AnalysisLevel`; together the pair is the taxonomy SSOT for depth filtering, replacing the retired flat `AnalysisDepthClass` (whose single axis could not represent book entries carrying separate moves and cube rollout levels). Classification is producer-side (ConvertXgToJson_Lib stamps both axes). `Unknown = 0` deliberately — "not recorded", which a producer states; the members carrying the pair are required on the wire (see "Absence on the wire"), so JSON lacking them is refused rather than read as `Unknown`, while the retired flat class's property beside them is still ignored on read. `BookRollout` is a book hit — rollout-derived, with parameters in the book database rather than the source file; `BookRollout` + `AnalysisLevel.Unknown` is the graceful-degradation stamp (no book DB available at conversion time, or a V1-book hit recording no levels). The UI renders modes in declaration order. Every member carries a `[Description]` display label (XgFilter_Lib's `EnumLabel.ToLabel` throws without one). Trial counts stay label-only. |
| `AnalysisLevel` | enum: `Unknown`, `Ply1`, `Ply2`, `Ply3Red`, `Ply3`, `XgRoller`, `Ply4`, `XgRollerPlus`, `Ply5`, `Ply6`, `Ply7`, `XgRollerPlusPlus` — the evaluation level; the level axis paired with `AnalysisMode`, serializes as string. For `Evaluation` it is the level of the evaluation itself; for the rollout-family modes it is the inner evaluation level — checker rows carry the inner moves level, cube rows the inner cube level (a single rollout can use different levels for the two; which one a row gets is the producer's concern, the semantics are owned here). Rollout-family modes never pair with a Roller-family level on checker rows but can on cube rows (the shipped book DB contains cube rollout levels of XG Roller). `Unknown = 0` deliberately — "not recorded", a value the producer states, never an absent member (see `AnalysisMode`). **Declaration order is contractual** (ruled 2026-08-28 on the authority of XG's own analysis-level menu, amended the same day): every member after `Unknown` ascends in rigor, and the ply and Roller families *interleave* rather than forming two blocks — `Ply3`, `XgRoller`, `Ply4`, `XgRollerPlus`, `Ply5`. Reordering, or inserting out of rigor order, is a breaking change; live consumers read the order (the diagram's level floor, the filter-panel and quiz level dropdowns). `Unknown` sits *outside* the rigor scale — not "least rigorous" but "not recorded": never excluded by a floor, never offered as a threshold; head-of-list is the zero-value requirement, not a rank. `DepthRank` / `CubeDepthRank` remain the ordering surface across the mode × level *pair*. Every member carries a `[Description]` display label. `Ply3Red` is XG's "3-ply Red" — its own member between `Ply2` and `Ply3` as of the same ruling, superseding the earlier collapse into `Ply3` as a label variant. |
| `CubeDecisionPair` | `readonly record struct (CubeAction Doubler, CubeAction Taker)` — a complete cube decision as two atomic actions. Validated on construction via the positional-record idiom: `Doubler` ∈ {`NoDouble`, `Double`}, `Taker` ∈ {`Take`, `Pass`}; a cross-half value throws `ArgumentOutOfRangeException`. The verdict aggregate (pair → correct/wrong) is intentionally absent and returns later with `CubeVerdict`. `default` is non-meaningful — see Pitfalls. |
| `CubeClaimPair` | `readonly record struct (CubeClaim Claim, CubeAction Taker)` — the two-part cube answer of SPEC-scoring §3 (`halheinrich/backgammon#86`): the claim-layer counterpart of `CubeDecisionPair`, pairing the three-valued claim with the taker response if doubled. Same construction-guard idiom (`Claim` any defined member, `Taker` ∈ {`Take`, `Pass`}). A closed 3×2 of six named canonical instances: five verdict cells (`NoDoubleTake`, `DoubleTake`, `DoublePass`, `TooGoodTake`, `TooGoodPass`) plus `NoDoublePass`, the incoherent cell — representable *by ruling* (a selectable user answer; cross-disabling the axes was rejected), named by `IsIncoherent` for review surfaces. One type serves both scored roles — a user's submitted answer and the derived truth (`DecisionData.BestClaimPair`). Scoring semantics stay with the consuming legs. No parse/format story: display strings are consumer copy per SPEC-scoring §3, and no wire token is ruled — its wire debut (and wire shape) belongs to the first document that embeds it. `default` is non-meaningful — see Pitfalls. |
| `DiceRoll` | `readonly record struct` — a dice roll in canonical unordered form: `High`/`Low`, each a validated face 1–6. The constructor accepts either order and canonicalizes (the XG parser stamps dice in rolled order, so both `31` and `13` reach it for a 3-1); canonicalization is single-sourced here, nowhere downstream, and record-struct equality over the canonical form makes 3-1 ≡ 1-3 automatic. `IsDouble`; `Parse`/`TryParse` of the two-digit token form (`IParsable` + `ISpanParsable`, accepting either spelling); `ToString()` → canonical high-first token (`"31"`). Ordered (`IComparable<DiceRoll>` + comparison operators via `IComparisonOperators`) ascending by `High` then `Low` — ascending canonical token. `All` is the SSOT enumeration of the 21 distinct rolls in that order (doubles included). JSON round-trips as the token via bundled `DiceRollJsonConverter`. `default` is non-meaningful (faces 0 — see Pitfalls); "no roll" is `DiceRoll?` null, per `IDecisionFilterData.Dice`. |
| `Move` | `readonly record struct (FrPt, ToPt)`. Encodes regular / bear-off / hit moves via the sign of `ToPt` — see "Move encoding" below. |
| `Play` | mutable `struct`, fixed 4-slot buffer of `Move`. Default value is empty (`Count == 0`). Intent-level construction via `Play.Create` — **five overloads**: four fixed-arity (`Create(m0)` … `Create(m0, m1, m2, m3)`), which construct at parity with the incremental `Add` spelling, and `Create(params ReadOnlySpan<Move>)` for moves already in a span or array (> 4 moves throws `ArgumentException`), which is also the `[CollectionBuilder]` target, so collection expressions build plays — `Play p = [new(13, 10), new(10, 8)];`, with `[]` the empty play, a forced pass. The span overload carries `[OverloadResolutionPriority(-1)]` so a literal argument list binds fixed-arity at every arity including one; see Benchmarks for what that buys. `Add`/`RemoveLast` remain the incremental build primitives for move-generation recursion; every construction path writes slots through one private seam. Read idiom is `foreach` (allocation-free pattern enumerator over a value copy; deliberately no `IEnumerable<T>` — it would box) or the indexer. **No equality** (`halheinrich/backgammon#273`, ruling A): `==`/`!=` are not defined, and `Equals`/`GetHashCode` throw `NotSupportedException` so every runtime route (comparers, hashed collections, `Distinct`, records and tuples holding a play) fails loudly. Play identity is `BoardState.IsSamePlay`, from a starting position — see "Play identity" below. `IsSameEncoding` compares exact encodings (order, hops, marks) for storage and round-trips; it is not identity. `ToNotation()` writes the play in standard notation, the one public way to spell a play (see "Play notation"); the internal `ToCanonical()` is the display form behind it. Serialized as a JSON array of `Move` via `PlayJsonConverter` (the private buffer fields are not visible to default property-based serialization); the raw move sequence round-trips exactly. |
| `BoardPosition` | `readonly struct` — an immutable position: the 26 checker counts of a board in `BoardState`'s frame, well-formed by construction (the invariant is stated once, in the type's `<remarks>`). The one definition of "the same position": `IEquatable<T>` and `==`/`!=` over all 26 counts, both bars included, with a consistent hash that is never identity. Creating, comparing and hashing allocate nothing. `default` is the empty board, which is well-formed, so the default is meaningful (`Empty`). See "BoardPosition" below. |
| `PlayChain` | **internal** `readonly record struct (FrPt, ToPt)` — one chain of a `CanonicalPlay`: a route from a source to a landing point, which the notation writes as one `from/to`, joining consecutive moves and eliding the touch-down points between. It stops where its moves stop or at a hit point whose mark it carries, so it is not a checker's whole trajectory: an intermediate hit splits one trajectory into two chains (`13/10*/8` is written `13/10* 10/8`). Same sign-encoding as `Move`, but may span several dice. A hit only ever sits at a chain's endpoint, and each hit point's mark on exactly one chain, its carrier (see "Canonical play form"). |
| `CanonicalPlay` | **internal** `readonly struct` (`halheinrich/backgammon#273`: consumers spell plays with `Play.ToNotation()` and compare them by position, so the chain form can change without breaking one), fixed 4-slot buffer of `PlayChain` + `Count`, read through `Count` and the indexer. The canonical chain form of a `Play` — its display form (which chains the notation shows, where each `*` goes), not its identity: like `Play` it has no equality (`==` undefined, `Equals`/`GetHashCode` throw). `ToString()` is the play's notation, the one formatter (see "Play notation"). Only produced by the internal `Play.ToCanonical()` — no other constructor path, so every instance is guaranteed canonical. `default` is the canonical form of the empty play (meaningful). |
| `PlayCandidate` | `Play`, `Depth`, `DepthAbbreviation`, `DepthRank`, `AnalysisMode`, `AnalysisLevel`, `Equity`, `EquityLoss` (non-nullable, `0.0` = best), `WinPct?`, `WinGammonPct?`, `WinBgPct?`, `LosePct?`, `LoseGammonPct?`, `LoseBgPct?`, and the derived `Notation`. `Play` is the one stored form of the candidate — applied, matched against the candidates with `BoardState.IndexOfSamePlay`, and displayed through `Notation`, which is `Play` written by the one formatter (`CanonicalPlay.ToString()`), `[JsonIgnore]`d and never stored (`halheinrich/backgammon#273`). The stored `MoveNotation` it replaced could disagree with its play; a document still carrying it reads on both paths with the member ignored, like any retired property. `EquityLoss == 0.0` is the test for "is this a best play"; `DecisionData.BestPlayIndex` names the canonical single best when one is needed. |
| `DecisionId` | `abstract record` + two sealed records: `XgpDecisionId(Filename)` and `XgDecisionId(Filename, Game, MoveNumber, IsCube)`. Stable, persistent identifier for a single decision within an XG-family source file. Canonical string form: `"file.xgp"` (Xgp) or `"file.xg:g{N}:m{N}:{cube\|play}"` (Xg). Implements `IParsable<DecisionId>` + `ISpanParsable<DecisionId>`. Filename invariant: `':'` is forbidden on **both** subtypes (the parse dispatcher discriminates by `':'` presence, so an unguarded Xgp filename with `':'` would lose round-trip). JSON-serialised as the canonical string via bundled `DecisionIdJsonConverter`. Set as `required` on both `BgDecisionData` and `DecisionRow`. |
| `ProblemKey` | `sealed class` (not a record — no `with`-expression hatch) — the **content** identity of a decision problem, sibling to `DecisionId`'s file-navigation identity: `DecisionId` answers "where did this record come from", `ProblemKey` answers "which problem is this". Identity over the decomposed facts that can change the correct answer, never over the XGID string; it therefore collapses strictly more than an XGID does, by ruling. Canonical string form is a pinned wire contract with exactly one spelling per value, so ordinal string equality *is* key equality — equality, hashing, ordering and `ToString` all read it. Full surface: `IEquatable`, `IComparable`/`IComparable<ProblemKey>`, `IParsable` + `ISpanParsable`, strict (non-canonicalizing) `Parse`/`TryParse`. Two doors only — `TryDerive` producer-side and `Parse`/`TryParse` on read-back; there is no public constructor. Both doors run the same fact validation, and facts that would force a guess get **no key** rather than a wrong one (see "ProblemKey" below and Pitfalls). JSON round-trips as the canonical string via bundled `ProblemKeyJsonConverter`, which — unlike `DecisionIdJsonConverter` — also implements the property-name overloads, so `Dictionary<ProblemKey, …>` round-trips without consumer-side registration. |

### Move encoding

`Move(FrPt, ToPt)` stores everything callers need to interpret or undo a
move:

- `FrPt`: source point. `1`–`24` is a board point; `25` is bar entry.
- `ToPt`: destination, sign-encoded.
  - `> 0`: regular move — land on `ToPt` (1–24).
  - `== 0`: bear off — checker leaves the board.
  - `< 0`: hit — land on `|ToPt|` and send opponent blot to bar.

Move-generation rules (which `FrPt`/`ToPt` combinations are legal, bearing-off
overshoot, etc.) live in `BgMoveGen` — `Move` here is just the encoding.

### Canonical play form

The internal `Play.ToCanonical()` produces a `CanonicalPlay` — a play's **display form**:
which chains its notation shows and on which chain each hit mark (`*`) goes.
It is not identity, and it has no equality; whether two plays are the same
play is decided by position (see "Play identity" below).

Collapse semantics (the XG chain-collapse rules, once encoded display-side
in BgMoveGen's notation formatter; the rule lives here, and so, since
`halheinrich/backgammon#273`'s notation leg, does the notation — see
"Play notation" below):

- Consecutive single-die hops of one checker merge into a single
  `PlayChain` recording source and final landing point:
  `{(13,10),(10,8)}` and `{(13,8)}` both canonicalize to the chain `13/8`.
- **A hit belongs to its point** (`halheinrich/backgammon#273`). A point
  holds at most one opposing blot, so it is hit at most once, and which move
  or chain landing there records the hit does not change the display.
  Canonicalization lifts the marks off the moves into the play's set of hit
  points, builds chains from the unmarked moves, and places each point's mark
  on its **carrier**: the first chain, in canonical order, ending there. So
  `8/3* 7/3` and `8/3 7/3*` both canonicalize to `{8/3*, 7/3}`. The form
  depends only on the unmarked moves and the hit-point set, so encodings
  differing only in attribution display alike by construction. Two marks on
  one point display as the one hit.
- **Hit-visibility rule** (the one predicate gating every join): a join at
  point P consumes the segment *ending* at P, so it is allowed unless P is
  hit and that segment is P's carrier — the carrier keeps P as its endpoint,
  where the mark is visible. A lone trajectory with an intermediate hit
  therefore splits: `13/10*/8` gives chains `{13/10*, 10/8}`, while
  `13/10 10/8*` collapses to `13/8*`. When several chains land on the
  hit point, only the carrier stops there: `{(15,9),(12,9),(9,6)}` with the
  hit on 9 gives `{15/9*, 12/6}` whichever move recorded it. A hit only ever
  sits at a chain's endpoint.
- Moves are pre-sorted (FrPt desc, |ToPt| desc) so the same multiset of
  moves always canonicalizes identically; chains are emitted sorted the same
  way, and a carrier precedes its unmarked duplicates. Matching is
  bidirectional with a fixpoint fuse pass, keeping the whole `Move` encoding
  domain deterministic (bar entry, bear-off, doubles, out-of-order legs, even
  physically-impossible zigzags).
- **Pairing displays as written.** Encodings holding multi-die moves can
  pair sources with destinations differently for one position —
  `25/10 20/15*` and `25/15* 20/10` (5-5, a hit on 15), `13/9 11/7` and
  `13/7 11/9` — and each shows its own pairing. That is why the display form
  cannot be identity (`halheinrich/backgammon#277`); position identity
  treats them as one play.
- Duplicate chains (doubles moving two checkers along the same route) are
  kept as repeated entries, adjacent in canonical order; the notation groups
  them as `"(2)"`.

### Play notation

**`Play.ToNotation()` is the one public way to spell a play**
(`halheinrich/backgammon#273`): `"24/18*"`, `"bar/22"`, `"6/off"`,
`"8/5(2)"`, `"6/2(2)*"`, and the empty string for a pass. What it writes is
stated once, on that method's doc comment, and not restated here. It forwards
to `CanonicalPlay.ToString()`, the one formatter. Design points:

- **Placement: the display form's text.** The notation is a function of the
  canonical form alone, and its `(n)` grouping leans on the form's own
  order (identical chains adjacent, a carrier first), so the rendering lives
  in the type that owns that order. A form's
  canonical text is its `ToString`, as for `DiceRoll`, `ProblemKey` and
  `DecisionId`; no separate formatter type.
- **The public route: a named method on `Play`.** Every consumer that
  spells a play holds a `Play`, so the route lives there and no consumer
  needs the canonical form, which is internal with `PlayChain` and
  `Play.ToCanonical()`. The tests see internals (`InternalsVisibleTo`
  `BgDataTypes_Lib.Tests`, in the library's csproj) because they pin the
  chain form more precisely than a notation string can;
  `PlayNotationTests` pins the public surface itself.
  A method, because it formats. `Play.ToString()` stays the default and is
  not the notation: a play's encoding (order, hops, marks) is more than its
  notation shows.
- **Moved, not rewritten.** It came from BgMoveGen's `MoveNotationFormatter`
  unchanged: `PlayNotationTests` ports every one of that
  formatter's cases with the same inputs and expected strings, and a
  differential run over random encodings and legal plays found no
  difference. BgMoveGen's copy is deleted in its own consumer leg.
- **Invariant text.** Point numbers are written with the invariant culture,
  so the notation never varies with the machine's locale.
- **Never stored.** Notation is derived wherever it is shown — a candidate's
  is `PlayCandidate.Notation`, from its `Play`. A stored copy can disagree
  with the play it describes (a converted match stored `8/3* 7/3` over a
  play marking the hit on the 7-point checker), so no record carries one:
  `PlayCandidate.MoveNotation` is retired from the wire, and an old document
  carrying it reads with the member ignored. Do not add a notation member to
  a record.

### BoardPosition

An immutable position: the 26 checker counts of a board in `BoardState`'s
frame. **The invariant and the equality contract are stated once, in the
type's `<remarks>`**, and not restated here. Design points a maintainer
needs before touching it:

- **The one "same position"** (halheinrich/backgammon#273). Equality is
  over all 26 counts, both bars included; the hash is consistent with it
  and never identity (`HashCode`, seeded per process). Code that asks
  whether two boards are the same position asks this type, never a hash
  of its own.
- **Well-formed by construction.** The constructor and `TryCreate` refuse
  counts that break the invariant, so no instance holds a malformed board.
  `default` is the empty board, which is well-formed, so unlike `DiceRoll`'s
  the default is meaningful; `Empty` names it.
- **Representation.** The counts are stored inline as 26 `sbyte`s (an
  `[InlineArray]`), which the invariant makes lossless; the storage is
  private and every read widens to `int`. Equality is one 26-byte span
  comparison and the hash one `HashCode.AddBytes` pass, so creating,
  comparing and hashing allocate nothing — pinned by test with
  `GC.GetAllocatedBytesForCurrentThread`, because the move generator
  deduplicates plays by the position they reach, on its hot path.
- **Name and placement.** It names the position of the checkers, distinct
  from `PositionData` (the record category, which adds score and cube) and
  from `BoardState` (the mutable working board), and it collides with no
  member named `Position` or `Board`. It lives beside `BoardState` because
  `BoardState` builds from it and compares through it.
- **No frame of its own.** A position does not record whose turn it
  describes; every member that stores one states its frame.
- **One flip rule, here.** `Flipped()` returns the position seen from the
  other side: slot `i` takes the negated count of slot `25 - i`, so the
  points mirror, the bars swap and the signs invert. It is an involution,
  allocation-free, and needs no check (the bars' signs swap with the bars;
  the side totals swap). It is the only statement of the flip: `BoardState`
  flips — in `ApplyPlay`, `TryApplyPlay` and `FlippedCopy` — by taking its
  position, flipping it here and writing it back, so the rule cannot drift
  between the value and the board.
- **The starting positions are values.** `Standard`, `Nackgammon` and
  `Bg960(seed)` define the layouts once, here; `BoardState`'s factories of
  the same names build a board from them, so "is this the standard start"
  is `position == BoardPosition.Standard`.
- **Read surface.** The indexer (slots 0–25), `CopyTo`, and a `ToString`
  of `slot:count` pairs for the occupied slots (`"empty"` for the empty
  board), which a test failure shows side by side. Deliberately no
  ordering, no text parsing, and no enumeration: it is a value, not a
  collection.

### BoardState

Mutable backgammon board: the working copy play applies to. Private
storage for 26 counts in `BoardPosition`'s layout, read through
`ReadOnlySpan<int> Points`, plus `int HighPointOccupied`:
`Points[0]` = opponent bar, `Points[1..24]` = playing surface, `Points[25]` =
on-roll bar; positive = on-roll, negative = opponent. On-roll moves
high index → low; opponent moves low → high. Borne-off counts are not
tracked — checkers leaving the board simply disappear.

**Callers read it; only its own members write it**
(halheinrich/backgammon#281). `Points` is a read-only span over the
board's storage (allocation-free to read, a write does not compile) and
`HighPointOccupied` has a private setter. A board comes into being only
by construction, and the board is therefore always a well-formed position
(`BoardPosition`'s invariant): every way in is one, and every change
below keeps it one.

- **Construction.** `new BoardState(BoardPosition)` is the construction
  from a value, and every other way in goes through it: `Standard()`,
  `Nackgammon()` and `Bg960(int? seed = null)` build from
  `BoardPosition`'s starting positions (the layouts are defined once, on
  the value); `FromMop(ReadOnlySpan<int>)` is the door from raw outside
  counts, validated by the value's one rule, so the pseudoboards it once
  tolerated are refused (partial boards are fine — borne-off checkers are
  not tracked). `Copy()` is a deep copy; `FlippedCopy()` a deep copy
  re-expressed from the opponent's perspective (an involution — flipping
  twice reproduces the original). There is no public empty constructor:
  the empty board is `new BoardState(BoardPosition.Empty)`.
- **The snapshot.** `ToPosition()` takes the board as it stands as a
  `BoardPosition` value, allocation-free; later changes to the board do
  not reach it. Two boards are compared through it, never through
  `Points`, and it is what the move generator deduplicates by.

After construction a board changes three ways — the reset, the raw pair
and the turn boundary — with `Flip()` the private mechanic behind
`FlippedCopy()`:

- **`SetPosition(BoardPosition)`** — the whole-board reset: replaces every
  slot with the position's counts and recomputes `HighPointOccupied`,
  allocation-free. It is the door for a caller that reuses one board across
  positions rather than building one per position (BgMoveGen's interop,
  which used to reset its board with raw writes), and it needs no check of
  its own: its argument is well-formed by the value's invariant. It is also
  the one write of a whole position inside the type — the constructor,
  `Flip()`, and committing a play all go through it — and `ToPosition()` is
  its read.

- **`ApplyMove(Move)` / `UndoMove(Move)`** — hot-path primitives, zero
  allocation, used by `BgMoveGen`'s move generator to recurse through
  candidate plays. Maintain `HighPointOccupied` incrementally: apply
  scans down only when emptying the highest point; undo raises
  `HighPointOccupied` when a move's `FrPt` exceeds the current high.
  A raw, trusting pair: each move's hit mark is taken as given, so their
  docs state the precondition that it agrees with the board (and, for
  undo, that the board is as the move left it). No legality validation —
  that's the move generator's job.

- **`ApplyPlay(Play)`** / **`TryApplyPlay(Play)`** — turn-boundary
  primitive. Applies the play through the one play rule (see "Play
  identity") then flips perspective so the state is re-expressed from the
  next mover's POV — one write, `SetPosition` of the reached position's
  `Flipped()`. An invalid play is refused — `ApplyPlay` throws
  `ArgumentException`, `TryApplyPlay` returns false — and the board is
  left untouched. Empty plays still flip — they represent a forced pass.
  This is the only public way to advance past a turn boundary; callers
  reasoning in on-roll POV never need to flip explicitly.

- **`Flip()`** — `private`. Implementation mechanic for `FlippedCopy()`.
  It states no rule of its own: it sets the board, through `SetPosition`,
  to its position flipped by `BoardPosition.Flipped()` (point `i` ↔ point
  `25-i`, the bars swapping, the signs inverting), which recomputes
  `HighPointOccupied` from scratch. Stays private: live-state flips
  happen only inside `ApplyPlay`, so callers advancing state always
  reason in on-roll POV. `FlippedCopy()` is the public flipped-*copy*
  primitive for querying a position from the other player's frame
  (e.g. cube-response evaluation) without advancing state — the
  receiver is untouched.

Derived properties:

- `PipCount` — on-roll's pip count: `Σ i·max(Points[i], 0) for i ∈ [1..25]`.
  Bar (index 25) contributes 25 pips per checker.
- `OpponentPipCount` — opponent's pip count: `Σ (25−i)·max(−Points[i], 0) for i ∈ [0..24]`.
  Bar (index 0) contributes 25 pips per checker.
- `IsRace` — true iff no on-roll/opponent collision is possible:
  `max(i where Points[i] > 0) < min(i where Points[i] < 0)`. Vacuously
  true when one side is fully borne off. Bar checkers prevent races
  (on-roll bar at 25 → max = 25; opponent bar at 0 → min = 0).

These are pure derivations from `Points`. They are *distinct from*
`PositionData.OnRollPipCount` / `OpponentPipCount`, which carry
XG-parser-supplied values and may differ if XG ever rounds. Use the
`PositionData` ones when reading parsed decisions; use the `BoardState`
ones when computing from a live state.

### Play identity

**The contract is not restated here.** Plays compare only from a starting
position, and the one statement of what makes two plays the same play — the
position a play reaches, what makes a play invalid, and where the rule stops
short of legality — is the doc comment on `BoardState.IsSamePlay`
(`halheinrich/backgammon#273`, `halheinrich/backgammon#277`; Hal's rulings of
2026-09-25 are recorded on the first). A copy here would be a second source
that rots silently.

Design points a maintainer needs before touching it:

- **Placement: on the starting position.** Identity (`IsSamePlay`), the list
  match (`IndexOfSamePlay`) and the rule behind them are `BoardState`
  members. The rule computes positions from `Points`, and `ApplyPlay` must
  run the same computation, so one type owns one private rule and nothing
  can drift from it. `Play` stays a pure encoding with no board knowledge.
- **One rule, four doors.** `ApplyPlay` and `TryApplyPlay` commit what the
  rule computes; `IsSamePlay` and `IndexOfSamePlay` compare what it
  computes. None of them goes through `ApplyMove`, so the order a play's
  moves are written in never reaches the board. Written order was how the
  tester's play of `halheinrich/backgammon#273` corrupted it: the unmarked
  landing on the blot applied first.
- **Invalid is a distinct outcome.** An invalid play matches nothing, not
  even an identical encoding; the list match returns -1 for it and passes
  over invalid entries. `ApplyPlay` refuses with an `ArgumentException`
  naming the fault and the move; `TryApplyPlay` returns false.
- **How positions compare.** As `BoardPosition` values, the one definition
  of "the same position": the rule computes each reached board into stack
  scratch and the comparison is the value's equality — allocation-free.
  `BoardState` deliberately gets no value equality: it is a mutable class,
  and a hash that changes under mutation corrupts any set or dictionary
  holding it; `ToPosition()` is how a board is compared.
- **The raw pair stays raw.** `ApplyMove`/`UndoMove` trust their moves on
  the generator's hot path. `Debug.Assert` checks their stated
  preconditions in Debug builds only, and a failed assertion terminates the
  process — consumers build this library by project reference, so a Debug
  test run that breaks the precondition stops there rather than failing one
  test.
- **`CanonicalPlay` is the display rule** — see "Canonical play form".

### DecisionId

Two-shape carrier for the stable, persistent reference to a single decision
within an XG-family source file:

- `XgpDecisionId(Filename)` — bare filename for `.xgp` position files.
- `XgDecisionId(Filename, Game, MoveNumber, IsCube)` — colon-separated tuple
  for `.xg` multi-game files. `IsCube` disambiguates the cube row from the
  checker-play row XG emits at the same `MoveNumber`.

The bare filename is a unique key for `.xgp` not because XG writes one decision
per file — it does not. XG always writes a cube pane alongside the move pane,
and a position saved after the dice were rolled can carry analysis in both. The
key holds because the producing iterator's emission policy selects at most one
decision per `.xgp`: the analysed checker play if there is one, otherwise the
analysed cube. That is a producer contract `XgpDecisionId` depends on, not a
property of the file format.

Both records expose `Filename` via the abstract base; both reject `':'` in
`Filename` with `ArgumentException` (symmetric — the parse dispatcher
discriminates the two shapes by the presence of `':'`). Equality follows
record-default semantics: case-sensitive on `Filename`; tuple-equal on
the Xg form. `ToString` emits the canonical string form; `Parse` /
`TryParse` (string and `ReadOnlySpan<char>` overloads) read it back.

Stamping is producer-side — `ConvertXgToJson_Lib` sets `Id` at the four
`Build*` sites. `Id` is `required` on both `BgDecisionData` and
`DecisionRow`, so missing-id cases surface as compile errors at any
construction site that omits the property.

JSON shape: round-trips as the canonical string via the bundled
`DecisionIdJsonConverter` (type-level `[JsonConverter]` attribute on
`DecisionId`) — parallel to the `CubeOwner` / `Play` pattern in this lib.

Not added to `IDecisionFilterData`: the filter passes records through
unchanged and never needs to see the id; adding it would force every
test-fake implementation to construct one.

### ProblemKey

The content-identity key: the key under which lifetime stats and position
dedupe recognise "the same problem" across files. `DecisionId` and
`ProblemKey` are deliberately different questions — provenance versus
content — and neither substitutes for the other.

**The grammar is not restated here.** `SPEC-stats-identity.md` §1 owns the
fact table (which facts participate in identity, and why "iff it can change
the correct answer" is the whole rule) and §2 owns the key type's contract;
the canonical string grammar itself lives in one place only, the
`<remarks>` on `ProblemKey`. Copying either into this file would create a
second source that rots silently — and the grammar is a pinned wire
contract, so a rotted copy is worse than no copy.

Design points a maintainer needs before touching the type:

- **Two doors, no constructor.** `TryDerive(BgDecisionData, out ProblemKey)`
  is the single producer-side factory; `Parse`/`TryParse` is the wire
  read-back. Both run the same fact validation, and the type is a sealed
  class rather than a record precisely so no `with`-expression hatch
  exists. Consumers never assemble a key — repeated consumer glue would be
  a library gap.
- **The no-key rung.** Derivation that would guess is forbidden: a record
  filed under a wrong key is corruption. Malformed, degenerate, or
  inconsistent facts yield `false` and no key, never a throw
  (degrade, never block) — `TryDerive`'s `<returns>` carries the full
  rejection list.
- **Strict parse, deliberately unlike `DiceRoll`.** `DiceRoll` canonicalizes
  human input; `ProblemKey` is a wire format, where two spellings of one key
  would split a problem's tallies. Enforcement is structural — the parser
  re-formats the parsed facts and demands ordinal equality with the input.
- **The Jacoby suffix is money-only, by ruling** (amended 2026-08-20,
  `halheinrich/backgammon#120`). It rides the money score field (`0a0`) and
  nothing else, so every match key stays byte-identical to what the previous
  grammar emitted. Both values are spelled rather than presence-encoding one
  (unlike Crawford's `cr`), because the absent spelling was the old money
  key and admitting it would give one value two spellings — one silently
  wrong. A money record whose `PositionData.IsJacoby` is `null` therefore
  gets no key; a stamp on a *match* record is ignored, not rejected.
- **No version token inside the key.** The containing stats document's
  schema version pins the grammar, and that version is the document's fact
  (BgGame_Lib's), not this library's. A fact entering identity bumps the
  document version rather than the key's shape — the Jacoby suffix is that
  mechanism's first exercise (`SPEC-stats-identity.md` §3).
- **Real-board posture.** Fact validation requires a physically possible
  position — `BoardPosition`'s invariant, not restated in the key: a
  record's board holds it by its type, and the parse door builds the board
  through `BoardPosition.TryCreate` — plus a non-empty board, which is the
  key's own rule. `ProblemKey` identifies real analysed decisions, so a
  violation is corruption and corruption gets no key.

JSON shape: round-trips as the canonical string via the bundled
`ProblemKeyJsonConverter` (type-level `[JsonConverter]` attribute on
`ProblemKey`). Unlike `DecisionIdJsonConverter` it also implements the
property-name overloads, because the stats document keys its per-problem
map by `ProblemKey` and `Dictionary<ProblemKey, …>` must round-trip
without a consumer-side key converter.

### Played cube actions on DecisionData

Distinct from the scoring helpers below, `DecisionData` carries the record
of what was *actually played* in a cube decision
(`halheinrich/backgammon#123` documents; the fields shipped with the played-
action record work):

- **`UserDoublerAction`** (`CubeAction?`) — the doubler action the player
  on roll played, `NoDouble` or `Double`. Null when the played action is
  not recorded — retroactively true of all JSON written before the field
  existed — or when `IsCube` is false.
- **`UserTakerAction`** (`CubeAction?`) — the taker action the opponent
  played, `Take` or `Pass`. Present only when a double was offered *and* a
  response recorded: in an undoubled game no taker decision exists, so
  this stays null even when `UserDoublerAction` is recorded.

Both halves are guarded on `init` to their own action domain, mirroring
`CubeDecisionPair`'s half-guards — a cross-half value throws
`ArgumentOutOfRangeException`. Cross-half *consistency* (a recorded taker
response implies the doubler doubled) is a producer contract, not guarded
here: init-only halves are set independently. The fields exist because the
played action cannot be recovered from `UserDoubleError` /
`UserTakeError` alone — a zero error does not identify the action when
the two cube equities tie. Both serialize (they are wire fields, unlike
the computed members below).

These are **actions, never claims**: a stamped game fact cannot carry the
"too good" rationale, because a player can choose not to double in a
position where he is not too good — the rationale is a property of the
analysis, not of the played move (the `halheinrich/backgammon#86` intake's
"analysis vs. stamped action" distinction). The claim layer (`CubeClaim`,
`CubeClaimPair`, above) therefore neither supersedes nor touches this
pair: claims live in quiz answers and derived truth, played actions in
the game record. Do not infer a claim from a played action.

### Cube-decision scoring on DecisionData

`DecisionData` carries the cube-decision scoring policy as computed members
that derive from `NoDoubleEquity` and `DoubleTakeEquity` (the pass-equity
constant `1.0` is intrinsic to cube-equity normalisation). A cube decision is
scored as **two independent atomic decisions**, each judged on its own with no
cross-decision override:

- **Doubler's double / no-double decision**: `BestDoublerAction` and
  `DoublerActionError(action)`. `BestDoublerAction` is `Double` iff
  `min(DoubleTakeEquity, 1) > NoDoubleEquity`; the error is the equity gap
  between the chosen action and that best.

- **Taker's take / pass decision**: `BestTakerAction` and
  `TakerActionError(action)`. `BestTakerAction` is `Take` iff
  `DoubleTakeEquity < 1`; the error is the equity gap (taker
  perspective) between the chosen action and that best.

Above the action layer sits the claim derivation of SPEC-scoring §3
(`halheinrich/backgammon#86`) — the truth side of the two-part cube answer,
derived producer-side so consumers never re-derive:

- **`BestDoublerClaim`** widens `BestDoublerAction` to the three-valued
  claim: `Double` when doubling is best; otherwise `TooGood` iff
  `NoDoubleEquity > 1` **and** `BestTakerAction == Pass` (the ratified
  predicate as amended 2026-09-02 by `halheinrich/backgammon#187`: Too
  Good requires the pass; the equity comparison is strict — exactly 1 is
  not too good), else `NoDouble`. Cell by cell of the no-double half: Too
  good / Pass — playing on beats the cash and they would pass; No double /
  Take with a no-double equity above 1 — playing on beats being taken, the
  opponent takes, no pass is involved, a No double *by ruling* (XG's "Too
  good to double/Take", `TooGoodAndTake.xgp`, no double +1.1711,
  double/take +0.6004, is the position that decided it); No double / Take
  at or below 1 — the ordinary cell. Reads equities only: no match-score,
  money, or Jacoby context enters (Too Good occurs in money via Jacoby
  redoubles). Whether the verdict *can* occur at a position is the separate
  offerability fact below.

- **`BestClaimPair`** composes the full derived truth,
  `(BestDoublerClaim, BestTakerAction)` — the `CubeClaimPair` a submitted
  answer is scored against half by half, and the producer verdict the
  answer-type classification consumes. Off the tie boundaries it lands in
  one of the four reachable verdict cells — `NoDoubleTake`, `DoubleTake`,
  `DoublePass`, `TooGoodPass`; `TooGoodTake` is never derived since the
  amendment — and the sixth-cell boundary ruling stands; see Pitfalls.

- **`BgDecisionData.CanBeTooGood`** — the offerability fact of the same
  amendment, on the composite record because only it sees money, Jacoby and
  cube owner together: `false` iff the session is money
  (`IDecisionFilterData.IsMoneyGame`, the contract's single spelling — never
  a restated `MatchLength == 0`), `IsJacoby == true`, and
  `Position.CubeOwner == Centered` (gammons do not count under Jacoby until
  the cube turns, so the no-double equity never exceeds the cash); `true`
  otherwise, including `IsJacoby == null` — an unknown rule is not a known
  Jacoby rule. The one derivation site: a consumer offering cube answers
  reads it to decide whether the Too Good pair is in the option set and
  never re-derives it from the rules fields. Independent of what the
  equities derive (the claim would still say Too Good if the producer's
  numbers did). Throws `InvalidOperationException` on a non-cube record
  through the same guard as `BestClaimPair`; `[JsonIgnore]`d like the rest
  of the record's derived view.

All the computed members throw `InvalidOperationException` when `IsCube` is
false — they are only meaningful on cube decisions, and silent zero /
default returns on play decisions would mask misuse. The two error methods
further throw `ArgumentOutOfRangeException` when the action argument is
from the wrong half (e.g. `Take` or `Pass` passed to `DoublerActionError`).

Tie-breaking follows the renderer's existing convention so a downstream
consumer that collapses the inline cube derivation into calls to these
helpers preserves behaviour: `NoDouble` on the doubler-equity tie, `Pass`
on `DoubleTakeEquity == 1`.

The four computed properties (`BestDoublerAction`, `BestTakerAction`,
`BestDoublerClaim`, `BestClaimPair`) carry `[JsonIgnore]` so
`System.Text.Json` does not invoke their throwing getters when serialising
play decisions. The error methods are intrinsically not serialised because
they take parameters.

An aggregate verdict layer was removed in the cube-surface rebuild and is
slated to return later on a cleaner footing; the umbrella `INSTRUCTIONS.md`
Deferred section and git history carry that design.

### Composite type

`BgDecisionData = PositionData + DecisionData + DescriptiveData + PlayOutcomeData`.
Implements `IDecisionFilterData` via forwarding properties. `Board` returns
`Position.Mop` directly. `AfterBestBoard` / `AfterPlayerBoard` forward to
`Outcome.AfterBestBoard` / `Outcome.AfterPlayerBoard` — raw, with no conditional
on `IsCube`. The "null for cube decisions" invariant is producer-enforced:
whoever constructs a cube `BgDecisionData` gives `Outcome` two null boards.
`FilterError` routes to `UserDoubleError ?? UserTakeError` for cube decisions,
otherwise `UserPlayError`. `AnalysisMode` / `AnalysisLevel` derive per the
`DecisionRow.AnalysisDepth` convention: cube decisions report
`Decision.CubeAnalysisMode` / `Decision.CubeAnalysisLevel`, checker plays
report the `BestPlayIndex` candidate's pair (`Unknown`/`Unknown` when
`BestPlayIndex` does not identify a candidate — empty `Plays`, or an
out-of-range index from malformed data); a shared private lookup guarantees
both axes read the same candidate. `Dice` forwards `Decision.Dice` in
canonical `DiceRoll` form — null for cube decisions, fail-loud on malformed
stored faces.

**A Crawford cube is unrepresentable** (`halheinrich/backgammon#201`).
Doubling is prohibited in the Crawford game, so a record with
`Position.IsCrawford` and `Decision.IsCube` both true describes a decision
that cannot exist. The `Position` and `Decision` init setters each check
the other half, and whichever is set second throws `ArgumentException`
naming itself — order-independent for an object initializer in either
member order and for a JSON document in either property order, since
`System.Text.Json` populates init setters (the `UserDoublerAction`
half-guard precedent; the half set first never throws, since until its
other half is set that half is "not cube, not Crawford"). `CrawfordRule` is
the one spelling of the rule and of the throw, shared with `DecisionRow`.
An explicit null half — an initializer's, or a JSON `"Position":null` —
throws `ArgumentNullException` at init (`halheinrich/backgammon#221`); an
absent half is a compile error in an initializer and a `JsonException` on
both wire paths, the halves being `required`
(`halheinrich/backgammon#222`). The halves keep their non-nullable
declaration; their backing fields are null only while construction is
still setting them, so each guard reads an unset other half as "not cube,
not Crawford". The alternative —
`IJsonOnDeserialized` plus an explicit check at the converter's build seam —
was rejected because it guards the wire and one factory and leaves object
initializers open: a future producer using an initializer would fail only
if its record happened to round-trip through JSON, which is not
"unrepresentable at construction". `ProblemKey`'s grammar still accepts a
Crawford cube *key*: v3 stats documents written before the guard hold such
keys and must keep loading (SPEC-stats-identity.md §1 keeps the Crawford
flag in identity); `TryDerive` simply never sees such a record again, and
the orphaned keys are inert — stats are looked up per pooled problem, never
walked from the document.

Beyond the filter view, the composite carries the one claim-layer fact that
needs the whole record: `CanBeTooGood`, the Too Good offerability of
SPEC-scoring §3's 2026-09-02 amendment (`halheinrich/backgammon#187`) —
see "Cube-decision scoring on DecisionData". It lives here rather than on
`DecisionData` because only the composite sees money (`Descriptive`),
Jacoby and cube owner (`Position`) together; the claim itself stays on
`DecisionData`, derived from equities alone.

The whole forwarding view carries `[JsonIgnore]`
(halheinrich/backgammon#14): it is a read-side derivation of the category
members, which are the wire form, so serializing it would write top-level
duplicates of nested data with no read-back path (the members are
get-only) — the same rule `DecisionRow` applies per-member to its derived
properties. The top level of the JSON is therefore exactly the six stored
members (`Id`, `Xgid`, `Position`, `Decision`, `Descriptive`, `Outcome`),
pinned by test. For `Dice` the exclusion is load-bearing beyond
deduplication — it keeps the throwing derivation from running during
serialization, and `Decision.Dice` stays the JSON wire form.

### After-boards (PlayOutcomeData)

Two optional boards (`BoardPosition?`) derived from the play choices of a
decision: `AfterBestBoard` (state after the best play) and
`AfterPlayerBoard` (state after the player's actual play). Each board's
frame is stated on the member (halheinrich/backgammon#15). **Frame: the next
mover's** — the position the play reaches, flipped as `ApplyPlay` leaves it
(re-checked 2026-09-25 against ConvertXgToJson_Lib's `AfterBoardBuilder`,
which applies the play in the mover's frame and flips with
`flipped[i] = -board[25 - i]`, `BoardState`'s own flip): the opponent is on
roll, so the decision-maker's checkers are *negative* and the opponent's
positive. **An absent board is `null`**, never an empty list: always for a
cube decision, and on a checker play whose boards the producer could not
compute (the converter leaves both null when the player's play is not among
the analysed candidates). On the wire an absent board is written as `null`,
and reads from `null`, from a missing member, or from the `[]` documents
wrote before the boards were typed (`NullableBoardPositionJsonConverter`,
named at each after-board). Consumers test each board for `null`. This is
the substrate for `XgFilter_Lib`'s three-board `IPlayTypeClassifier`
contract.

### DecisionRow

Flat CSV export record. Sibling output to `BgDecisionData` — both are produced
by the XG → JSON conversion pipeline, for different consumers. Implements
`IDecisionFilterData` directly (no composition). Carries its own CSV methods
(`ToCsvLine`, `CsvHeader`, private `CsvEscape`). `Board` is a required
`BoardPosition` in the frame of `PositionData.Mop`; `AfterBestBoard` and
`AfterPlayerBoard` are `BoardPosition?`, in the next mover's frame and
`null` when absent, exactly as on `PlayOutcomeData` (see "After-boards").
All three board fields serialize to JSON but are **excluded from CSV output**,
as are `AnalysisMode` / `AnalysisLevel` (the taxonomy form of
`AnalysisDepth`, which remains the CSV depth column). `Dice` is derived from
the int `Roll` column (`Roll == 0` → null; malformed digits fail loud) and is
`[JsonIgnore]`d like `IsCube` / `MatchScore` — `Roll` stays the wire form on
both CSV and JSON.

The Crawford invariant of the composite type binds the row too
(`halheinrich/backgammon#201`): a cube row (`Roll == 0`) with `IsCrawford`
set cannot be constructed, the `Roll` and `IsCrawford` init setters each
checking the other so that whichever is set second throws
`ArgumentException` naming itself, from an initializer or from JSON alike,
through the shared `CrawfordRule`. The row needs one mechanism the
composite does not: **`Roll` must be stated before anything else could
default it**, because cube is the row's *default* kind and the decision
kind must be stated, never defaulted — `Roll` was `required` for this
reason before every member became required or nullable
(halheinrich/backgammon#222). Why
the guards also need `Roll`'s nullable backing field is owned by
`DecisionRow.Roll`'s doc comment, the one spelling of that rationale. On
the wire, `required` means a JSON document without `Roll` is
refused with `JsonException` rather than read as a cube; the 2026-09-14
survey (by bare identifier, every member) found no reader of `DecisionRow`
JSON outside this repo's tests, no document lacking `Roll`, and every
production construction site already stating it. The rejected shape is the
composite's rejected shape, for the same reason.

`IsJacoby` (`bool?`) is stored, not derived — the tri-state fact
`PositionData.IsJacoby` owns, carried here because the CSV shape spells it
(`halheinrich/backgammon#121`). It reaches CSV the way `IsCrawford` does:
through the computed `MatchScore` token, as an in-grammar suffix on the money
score. A money row is `moneyJ` or `moneyNJ`; a money row whose rule is unknown
(`IsJacoby` `null`) is the bare `money`, which is deliberately neither
rule-bearing token. No CSV column is added — the column set and count are
unchanged. Like `IsCrawford`, it also serializes to JSON.

### Shared consumer contracts: IMatchInfo and IGameInfo

The skip-early counterparts of `IDecisionFilterData`
(`halheinrich/backgammon#123` documents; the interfaces shipped with the
filter-layer work): producers (e.g. the XG parser's match- and game-info
types) implement them, and filter layers consume them **without
referencing any producer's concrete types** — the same decoupling
`IDecisionFilterData` provides at decision scope, moved up to the two
scopes where a consumer can skip whole units before any decision is
produced.

- **`IMatchInfo`** — match scope: `Player1`, `Player2` (bottom/top player
  in XG), `MatchLength` (0 = money session), and the default-implemented
  `IsMoneyGame => MatchLength == 0` — the contract's single spelling of
  the money-game rule. Producer contract: XG's raw sentinel for unlimited
  sessions (99999) is normalized to 0 at the parse boundary, before the
  contract ever sees it — which is what makes the `IsMoneyGame`
  derivation valid.
- **`IGameInfo`** — game scope: `IsStandardStart` (false for saved/custom
  openings — the opening-move filter's input), `Away1`, `Away2` (points
  still needed by each player), `IsCrawfordGame`. Money-session
  convention: `Away1 == 0`, `Away2 == 0`, `IsCrawfordGame == false`.

Both are minimal **by design** — members are added on demand, never
mirrored wholesale from a producer. They carry no JSON contract (nothing
serializes an `IMatchInfo`); they are purely the shape a consumer needs
to decide "skip this match / skip this game".

### Named documents: the persistence trio and the named collection

`IJsonDocument<TSelf>` (halheinrich/backgammon#190 leg (A)) is the contract
behind what `FilterConfig` (XgFilter_Lib) and `QuizMix` (BgGame_Lib) each
hand-wrote and cross-referenced as "the persistence trio": a `static
abstract` fail-loud `FromJson` (`ArgumentNullException` on a null string,
`ArgumentException` on the literal `null` token, `JsonException` on a
contract violation), a `static abstract` `TryFromJson` that absorbs exactly
those three failures and yields the type's inert default so its out is
always usable, and an instance `ToJson` writing the canonical form. Generic
over the implementer (`TSelf`), so a container can call the trio on a type
parameter — the way `INumber<TSelf>` exposes `Parse`. The two payloads are
its first implementers (legs (B) and (C)); this library ships the contract
and no implementer of its own.

`CanonicalJson` is where the two readers' bodies live: `Parse<T>` and
`TryParse<T>` take the type's `JsonTypeInfo<T>` and encode the exception
taxonomy and the absorb-only-`JsonException` rule once. An implementer's
`FromJson` / `TryFromJson` are one-line forwards passing its own
source-generated metadata; `ToJson` is the serializer call against the same
metadata and needs no helper. Nothing here touches the reflection-bound
`JsonSerializer` overloads, so the trio is trim-safe from any context that
declares the implementer. The pattern an implementer follows: a type-level
`[JsonConverter]` where the wire form is hand-written (public, per the
composition rules above), metadata from its repo's context, the trio
forwarding to `CanonicalJson`.

**`NamedCollection<TValue, TSelf>`** generalizes XgFilter_Lib's
saved-filter document: an immutable collection of named `TValue` documents
where `TValue : IJsonDocument<TValue>`. It carries once what that document
carried welded to `FilterConfig` — the name rule (one
`OrdinalIgnoreCase` comparer for duplicate rejection, lookup, replace,
removal and the canonical sort; names validated, never coerced: blank and
untrimmed rejected in memory and on the wire), the canonical name order in
`Names` and on the wire, value snapshots by the payload's own
`ToJson`/`FromJson` round-trip so a mutable payload can never alias the
document, `Empty` / `Count` / `Names` / `Contains` / `Get` / `TryGet` /
`With` (add-or-replace, last write wins for spelling) / `Without`
(idempotent; the same instance on a miss), and the trio — the collection is
itself an `IJsonDocument<TSelf>`. Reference equality, deliberately: the
type wraps a map of snapshots, and payload equality is not something the
contract promises.

**The fork, settled: a self-typed base the specializations derive from,
not a value they wrap.** `TSelf` is the sealed specialization deriving with
itself as the argument, so `With` and `Without` return the specialization
and the trio is typed to it — `NamedFilterCollection.With` keeps returning
`NamedFilterCollection`, and `Empty`, `FromJson`, `TryFromJson` and
`CurrentSchemaVersion` resolve through the specialization's name with no
forwarding. The wrapping alternative (a sealed `NamedCollection<TValue>`
value with the specialization forwarding every member and hand-writing its
trio) lost because it restates per specialization exactly the boilerplate
the arc exists to remove: ten forwarders plus a third and fourth copy of
the trio, with the generic value unable to implement the trio at all
(it has no wire names). Neither shape adds a dependency to this library.
A base class cannot declare `static abstract` members, so the two static
things the machinery cannot know — how to make an instance, where the
serializer finds its metadata — live on `INamedCollectionSpecialization<TValue, TSelf>`,
which the specialization lists beside the base and implements explicitly:
`Create(Entries)` forwards to its private constructor, `CanonicalTypeInfo`
returns its own context's metadata. `Entries` is the base's public nested
type with an internal constructor: a specialization's factory can only
ever receive an entry set the base built, so neither a caller nor a
specialization can bypass the name rule or the snapshot contract. The
self-type is a convention the compiler cannot fully enforce (a type
deriving with another specialization as `TSelf` fails at runtime on first
use with `InvalidCastException`); it is the standard CRTP cost, stated on
the type parameter.

**The wire identity is the specialization's, not the generic's.** The
envelope is `{"schemaVersion":1,"<entries>":[{"name":…,"<value>":{…}}]}`
with `<entries>` and `<value>` supplied by the specialization's closed
converter — `filters` and `config` for the saved-filter document, so
`xg-filters.json` files on disk read unchanged. `NamedCollectionJsonConverter<TValue, TSelf>`
is abstract with a protected two-name constructor (names validated:
non-blank, trimmed, not colliding with `schemaVersion` / `name`); a
specialization closes it as a sealed public class with a parameterless
constructor passing its names, and names that closed type in its
type-level `[JsonConverter]`. The envelope is strict and fail-loud (version
other than 1 — a newer one distinguished in its message — missing required
property, unknown property at top or entry level, invalid or duplicate
name), order-tolerant on read at both levels, and each entry body goes
verbatim to `TValue.FromJson`, so the payload's tolerance is the envelope's
tolerance; a body the payload rejects fails the whole file with the entry
named. Reads route through `With`, so the wire-level name rule and the
in-memory one have one definition. No reflection-based generic
instantiation exists on either path: no `MakeGenericType`, no converter
factory — the source generator instantiates the closed converter from the
consumer's generated code, which is why the pattern is closed (see
Pitfalls).

### Mop layout

A `BoardPosition` from the on-roll player's perspective (the value's layout,
well-formed by its invariant):

- `[0]` = opponent's bar (≤ 0)
- `[1–24]` = points 1–24
- `[25]` = on-roll player's bar (≥ 0)
- Positive = on-roll; negative = opponent

`PositionData.Mop`, `DecisionRow.Board` and `IDecisionFilterData.Board` are
in this frame; the after-boards hold the same layout in the next mover's
frame (see "After-boards"). On the wire each is the 26 counts as a JSON
number array, as before the boards were typed.

## Public API

```csharp
public interface IDecisionFilterData
{
    string Player { get; }
    bool IsCube { get; }
    int OnRollNeeds { get; }
    int OpponentNeeds { get; }
    bool IsCrawford { get; }
    int MatchLength { get; }
    bool IsMoneyGame => MatchLength == 0;         // the interface's only default implementation
    bool? IsJacoby { get; }                       // tri-state; null on a money record matches neither money token
    int MoveNumber { get; }                       // 1-based within the game
    bool IsStandardStart { get; }                 // false for non-standard openings
    AnalysisMode AnalysisMode { get; }            // cube analysis for cubes, best-play candidate for checkers
    AnalysisLevel AnalysisLevel { get; }          // level axis of the same analysis AnalysisMode reports
    DiceRoll? Dice { get; }                       // canonical roll; null for cube decisions
    double? FilterError { get; }                  // ≥ 0 or null
    BoardPosition Board { get; }                  // on-roll frame, see Mop layout
    BoardPosition? AfterBestBoard { get; }        // next mover's frame; null when absent (always for cubes)
    BoardPosition? AfterPlayerBoard { get; }      // next mover's frame; null when absent (always for cubes)
}

// Skip-early contracts (see "Shared consumer contracts" above): producers
// implement, filter layers consume without producer-concrete types.
public interface IMatchInfo
{
    string Player1 { get; }                       // bottom player in XG
    string Player2 { get; }                       // top player in XG
    int MatchLength { get; }                      // 0 = money (sentinel pre-normalized)
    bool IsMoneyGame => MatchLength == 0;         // single spelling; never redeclare
}

public interface IGameInfo
{
    bool IsStandardStart { get; }                 // false for saved/custom openings
    int Away1 { get; }                            // 0 for money sessions
    int Away2 { get; }                            // 0 for money sessions
    bool IsCrawfordGame { get; }                  // false for money sessions
}

// Wire records: every stored member is required or nullable, never
// defaulted (halheinrich/backgammon#222; the rule on BgDataTypesJsonContext).
public class BgDecisionData : IDecisionFilterData
{
    public required DecisionId      Id          { get; init; }   // producer-stamped
    public required string          Xgid        { get; init; }
    public required PositionData    Position    { get; init; }   // init guard: a Crawford cube throws ArgumentException
    public required DecisionData    Decision    { get; init; }   //   from whichever half is set second (halheinrich/backgammon#201);
                                                                 //   null throws ArgumentNullException (halheinrich/backgammon#221)
    public required DescriptiveData Descriptive { get; init; }
    public required PlayOutcomeData Outcome     { get; init; }
    // IDecisionFilterData members implemented as forwarding properties —
    // all [JsonIgnore]d; the category members are the wire form
    // (halheinrich/backgammon#14).

    // Offerability of the Too Good verdict (SPEC-scoring §3, 2026-09-02):
    // false iff money && IsJacoby == true && cube centred. Throws
    // InvalidOperationException when IsCube is false.
    [JsonIgnore] public bool CanBeTooGood { get; }
}

public class PlayOutcomeData
{
    // Next mover's frame; null when absent. Written null; null, a missing
    // member, or the legacy [] read as null (halheinrich/backgammon#15).
    [JsonConverter(typeof(NullableBoardPositionJsonConverter))] public BoardPosition? AfterBestBoard { get; init; }
    [JsonConverter(typeof(NullableBoardPositionJsonConverter))] public BoardPosition? AfterPlayerBoard { get; init; }
}

public sealed class DecisionRow : IDecisionFilterData
{
    public required DecisionId Id { get; init; }    // producer-stamped
    public required int Roll { get; init; }         // decision kind: 0 = cube; stated, never defaulted
                                                    //   (halheinrich/backgammon#201)
    public required bool IsCrawford { get; init; }  // init guard with Roll: a Crawford cube throws ArgumentException
    public required BoardPosition Board { get; init; }        // on-roll frame
    public BoardPosition? AfterBestBoard { get; init; }       // next mover's frame; null when absent
    public BoardPosition? AfterPlayerBoard { get; init; }     //   (property-level NullableBoardPositionJsonConverter)
    // Other flat init-only properties, all required but SourceFile? — see DecisionRow.cs.
    public bool? IsJacoby { get; init; }  // stored tri-state; suffixes the money MatchScore token
    public string MatchScore { get; }   // computed from needs/Crawford/length/Jacoby
    public static string CsvHeader { get; }
    public string ToCsvLine();
    // IsCube, MatchScore, and FilterError are [JsonIgnore]d (computed /
    // derived). The three boards (Board, AfterBestBoard,
    // AfterPlayerBoard) and AnalysisMode / AnalysisLevel serialize to JSON
    // but are excluded from CSV output.
    // Id is JSON-serialized (as canonical string) but excluded from CSV
    // output — CSV columns are listed explicitly.
}

public class PositionData    { /* required init-only properties per Architecture table; Mop is a BoardPosition; IsJacoby? */ }
public class DescriptiveData { /* required init-only properties per Architecture table; Title?, Date?, Event?, SourceFile? */ }
public class PlayCandidate   { /* required init-only properties per Architecture table; the six probabilities nullable */
                               [JsonIgnore] public string Notation { get; }  /* Play.ToNotation(); never stored */ }

public class DecisionData
{
    // Init-only properties per Architecture table, required but for the
    // nullable ones (Dice, Plays, BestPlayIndex,
    // UserPlayIndex, UserPlayError?, IsCube, CubeDepth, CubeDepthAbbreviation,
    // CubeDepthRank, CubeAnalysisMode, CubeAnalysisLevel, NoDoubleEquity,
    // DoubleTakeEquity, the pct fields, ProbOfOpponentErrorJustifyingDouble,
    // UserDoubleError?, UserTakeError?).

    // Played cube actions — game facts, guarded per half on init (see
    // "Played cube actions on DecisionData"); serialized; null = not
    // recorded (all pre-field JSON) or IsCube false.
    public CubeAction? UserDoublerAction { get; init; }  // NoDouble/Double; wrong half throws
    public CubeAction? UserTakerAction   { get; init; }  // Take/Pass; null in undoubled games

    // Cube-decision scoring (computed; throw InvalidOperationException when IsCube is false).
    [JsonIgnore] public CubeAction  BestDoublerAction { get; }   // Double or NoDouble
    [JsonIgnore] public CubeAction  BestTakerAction   { get; }   // Take or Pass

    // Claim-layer truth derivation (SPEC-scoring §3, amended 2026-09-02; same IsCube guard).
    [JsonIgnore] public CubeClaim     BestDoublerClaim { get; }  // TooGood iff best is NoDouble
                                                                 // && NoDoubleEquity > 1
                                                                 // && BestTakerAction == Pass
    [JsonIgnore] public CubeClaimPair BestClaimPair    { get; }  // (BestDoublerClaim, BestTakerAction);
                                                                 // never TooGoodTake

    public double DoublerActionError(CubeAction action);          // 0 if action == BestDoublerAction;
                                                                  // throws ArgumentOutOfRangeException on Take/Pass.
    public double TakerActionError(CubeAction action);            // 0 if action == BestTakerAction;
                                                                  // throws ArgumentOutOfRangeException on Double/NoDouble.
}

public readonly record struct Move(               // both required on the wire: an absent
    [property: JsonRequired] int FrPt,            //   ToPt would read as 0, a bear-off
    [property: JsonRequired] int ToPt);

[CollectionBuilder(typeof(Play), nameof(Create))]
public struct Play                                // no equality: see BoardState.IsSamePlay
{
    public static Play Create(Move m0);           // fixed-arity: parity with Add, no argument
    public static Play Create(Move m0, Move m1);  // buffer. Overload resolution picks these for
    public static Play Create(Move m0,            // any literal call site.
                              Move m1, Move m2);
    public static Play Create(Move m0, Move m1, Move m2, Move m3);

    [OverloadResolutionPriority(-1)]
    public static Play Create(params ReadOnlySpan<Move> moves);
                                                  // general arity + collection-builder target
                                                  // ([m1, m2] / [] both work); for moves already
                                                  // in a span or array. > 4 throws
                                                  // ArgumentException. Deprioritised so a literal
                                                  // list binds fixed-arity — including one move.
    public int Count { get; private set; }
    public Move this[int index] { get; }          // readonly
    public void Add(Move move);
    public void RemoveLast();
    public Play Snapshot();                       // readonly
    public string ToNotation();                   // readonly; the notation — the one public way to spell a play
    public bool IsSameEncoding(Play other);       // readonly; exact encodings, for storage
    public Enumerator GetEnumerator();            // readonly; foreach pattern, allocation-free
    public struct Enumerator { /* Current, MoveNext() */ }
    public override bool Equals(object? obj);     // throws NotSupportedException
    public override int GetHashCode();            // throws NotSupportedException
}

// Internal, not sketched here: CanonicalPlay, PlayChain and
// Play.ToCanonical() — the chain form behind Play.ToNotation (see
// "Canonical play form" and "Play notation").

// An immutable position: 26 counts in BoardState's frame, well-formed by
// construction (the invariant: the type's remarks). The one "same
// position": equality over all 26 counts; the hash is never identity.
// Creating, comparing and hashing allocate nothing.
public readonly struct BoardPosition :
    IEquatable<BoardPosition>, IEqualityOperators<BoardPosition, BoardPosition, bool>
{
    public BoardPosition(ReadOnlySpan<int> counts);           // malformed → ArgumentException
    public static bool TryCreate(ReadOnlySpan<int> counts, out BoardPosition position);
    public static BoardPosition Empty { get; }                // == default; well-formed
    public static BoardPosition Standard { get; }             // the starting layouts, defined once
    public static BoardPosition Nackgammon { get; }
    public static BoardPosition Bg960(int? seed = null);      // random, symmetric, no blots
    public int this[int point] { get; }                       // slots 0–25
    public void CopyTo(Span<int> destination);                // at least 26 elements
    public BoardPosition Flipped();                           // the other side's view; the one flip rule
    public bool Equals(BoardPosition other);                  // + ==, !=, Equals(object), GetHashCode
    public override string ToString();                        // "1:-2 6:5 …", or "empty"
}

public sealed class BoardState                    // sealed: it guards an invariant
{
    public ReadOnlySpan<int> Points { get; }      // read-only; layout of BoardPosition
    public int HighPointOccupied { get; }         // 1–25, or 0 if no on-roll checkers; private setter

    // Construction: a board is always a well-formed position (halheinrich/backgammon#281)
    public BoardState(BoardPosition position);    // every other way in goes through this
    public static BoardState Standard();          // from BoardPosition.Standard
    public static BoardState Nackgammon();        // from BoardPosition.Nackgammon
    public static BoardState Bg960(int? seed = null);   // from BoardPosition.Bg960
    public static BoardState FromMop(ReadOnlySpan<int> mop);   // raw counts; malformed → ArgumentException
    public BoardState Copy();
    public BoardState FlippedCopy();              // copy from opponent's perspective; receiver untouched

    // The whole position, out and in
    public BoardPosition ToPosition();            // the snapshot: allocation-free; later changes do not reach it
    public void SetPosition(BoardPosition position);   // the whole-board reset: allocation-free; recomputes
                                                       //   HighPointOccupied; the one whole-position write

    // Apply / undo (hot-path primitives)
    public void ApplyMove(Move move);
    public void UndoMove(Move move);

    // Turn boundary (the play rule + flip, atomic)
    public void ApplyPlay(Play play);             // invalid → ArgumentException, board untouched
    public bool TryApplyPlay(Play play);          // invalid → false, board untouched

    // Play identity, from this position (the contract: IsSamePlay's remarks)
    public bool IsSamePlay(Play first, Play second);
    public int IndexOfSamePlay(Play play, IReadOnlyList<Play> plays);   // -1 when none

    // Derived
    public int PipCount { get; }
    public int OpponentPipCount { get; }
    public bool IsRace { get; }
}

public enum CubeOwner { OnRoll, Opponent, Centered }

public enum CubeAction { NoDouble, Double, Take, Pass }

// The doubler half of a cube answer at the claim layer (SPEC-scoring §3):
// a claim about the position, not a board action. Declaration order is the
// ruled claim axis. Serializes as string (strict converter).
public enum CubeClaim { NoDouble, Double, TooGood }

// The claim→action collapse, single-sourced: NoDouble and TooGood both map
// to CubeAction.NoDouble; Double maps to Double. No reverse mapping exists
// (the claim is underdetermined by the action alone).
public static class CubeClaimExtensions
{
    public static CubeAction ToCubeAction(this CubeClaim claim);  // throws on undefined
}

// The two-axis depth taxonomy: mode (how the numbers were produced) ×
// level (the evaluation level — for rollout-family modes, the inner level).
// Unknown = 0 deliberately on both: unstamped/legacy JSON deserializes to
// it. BookRollout + AnalysisLevel.Unknown is the graceful-degradation stamp
// (no book DB, or a V1-book hit). The UI renders both enums in declaration
// order. AnalysisLevel's declaration order is CONTRACTUAL — ascending rigor
// per XG's own menu, ply and Roller families interleaved; Unknown sits
// outside the scale. DepthRank orders the mode × level pair. Every member
// of both carries a [Description] display label.
public enum AnalysisMode
{
    Unknown, Evaluation, Rollout, BookRollout
}

public enum AnalysisLevel
{
    Unknown,                                  // outside the rigor scale
    Ply1, Ply2, Ply3Red, Ply3, XgRoller, Ply4, XgRollerPlus,
    Ply5, Ply6, Ply7, XgRollerPlusPlus        // ascending rigor; contractual
}

// Canonical unordered dice roll: the ctor accepts either order and
// canonicalizes to High ≥ Low, each face validated 1–6
// (ArgumentOutOfRangeException). Equality is over the canonical form
// (3-1 ≡ 1-3); ordering is ascending High-then-Low (= ascending canonical
// token). Parse/TryParse read the two-digit token in either spelling;
// ToString emits it high-first ("31"). JSON round-trips as that token via
// bundled DiceRollJsonConverter. default is non-meaningful (see Pitfalls).
public readonly record struct DiceRoll :
    IComparable, IComparable<DiceRoll>, IComparisonOperators<DiceRoll, DiceRoll, bool>,
    IParsable<DiceRoll>, ISpanParsable<DiceRoll>
{
    public int High { get; }                      // 1–6; ≥ Low
    public int Low { get; }                       // 1–6
    public DiceRoll(int die1, int die2);          // either order; canonicalizes
    public static IReadOnlyList<DiceRoll> All { get; }  // 21 distinct rolls, ascending canonical; SSOT
    public bool IsDouble { get; }
    public void Deconstruct(out int high, out int low);
    public override string ToString();            // "31", "55"
    public static DiceRoll Parse(string s, IFormatProvider? provider = null);
    public static DiceRoll Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null);
    public static bool TryParse(string? s, IFormatProvider? provider, out DiceRoll result);
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out DiceRoll result);
    public int CompareTo(DiceRoll other);         // + <, <=, >, >= operators
}

// Validated halves: Doubler ∈ {NoDouble, Double}, Taker ∈ {Take, Pass};
// a cross-half value throws ArgumentOutOfRangeException. default is
// non-meaningful (see Pitfalls).
public readonly record struct CubeDecisionPair(CubeAction Doubler, CubeAction Taker);

// The two-part cube answer (SPEC-scoring §3): claim × taker response, a
// closed 3×2 with six named canonical instances — the four reachable
// verdict cells, plus two representable-but-never-offered cells (the
// retired TooGoodTake and the incoherent NoDoublePass). Validated
// halves: Claim any defined CubeClaim member, Taker ∈ {Take, Pass}.
// default is non-meaningful (see Pitfalls).
public readonly record struct CubeClaimPair(CubeClaim Claim, CubeAction Taker)
{
    public static CubeClaimPair NoDoubleTake { get; }
    public static CubeClaimPair NoDoublePass { get; }   // the incoherent cell
    public static CubeClaimPair DoubleTake { get; }
    public static CubeClaimPair DoublePass { get; }
    public static CubeClaimPair TooGoodTake { get; }    // retired as a verdict 2026-09-02
                                                        // (halheinrich/backgammon#187);
                                                        // never derived, not offered
    public static CubeClaimPair TooGoodPass { get; }
    public bool IsIncoherent { get; }                   // == NoDoublePass
}

public abstract record DecisionId : IParsable<DecisionId>, ISpanParsable<DecisionId>
{
    public abstract string Filename { get; init; }
    public static DecisionId Parse(string s, IFormatProvider? provider = null);
    public static DecisionId Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null);
    public static bool TryParse(string? s, IFormatProvider? provider, out DecisionId result);
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out DecisionId result);
}
public sealed record XgpDecisionId(string Filename) : DecisionId;
public sealed record XgDecisionId(
    string Filename, int Game, int MoveNumber, bool IsCube) : DecisionId;

// Content identity — "which problem is this". Sealed class, no public
// constructor and no `with` hatch: the only two doors are TryDerive
// (producer-side) and Parse/TryParse (wire read-back), both guarded by the
// same fact validation. Canonical string form is a pinned wire contract
// with one spelling per value, so equality/hash/ordering are all ordinal
// over it. Parse is strict — no canonicalizing, unlike DiceRoll. Grammar
// lives in the type's XML remarks; the identity rulings live in
// SPEC-stats-identity.md §1/§2. JSON round-trips as the canonical string
// via bundled ProblemKeyJsonConverter, including as a dictionary key.
public sealed class ProblemKey :
    IEquatable<ProblemKey>, IComparable, IComparable<ProblemKey>,
    IParsable<ProblemKey>, ISpanParsable<ProblemKey>
{
    public bool IsCubeDecision { get; }           // decision kind rides on the dice field

    // The single derivation site in the ecosystem. false = no key, per the
    // no-key rung (malformed / degenerate / inconsistent facts — including a
    // money record whose IsJacoby is null). Never throws on bad facts;
    // throws ArgumentNullException on a null record (a caller bug).
    public static bool TryDerive(BgDecisionData data, out ProblemKey? key);

    public static ProblemKey Parse(string s, IFormatProvider? provider = null);
    public static ProblemKey Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null);
    public static bool TryParse(string? s, IFormatProvider? provider, out ProblemKey result);
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out ProblemKey result);

    public override string ToString();            // the canonical string
    public bool Equals(ProblemKey? other);        // ordinal over the canonical string
    public override bool Equals(object? obj);
    public override int GetHashCode();
    public static bool operator ==(ProblemKey? left, ProblemKey? right);
    public static bool operator !=(ProblemKey? left, ProblemKey? right);
    public int CompareTo(ProblemKey? other);      // ordinal; any key > null
}

// The persistence trio as a contract (halheinrich/backgammon#190). Implemented
// downstream by FilterConfig and QuizMix; no implementer in this library.
public interface IJsonDocument<TSelf> where TSelf : IJsonDocument<TSelf>
{
    static abstract TSelf FromJson(string json);      // ArgumentNullException / ArgumentException
                                                      // (null token) / JsonException
    static abstract bool TryFromJson(string? json, out TSelf document);
                                                      // absorbs exactly those three; out is
                                                      // always usable (the inert default)
    string ToJson();                                  // the canonical form
}

// The two readers' shared bodies; an implementer forwards with its own
// source-generated JsonTypeInfo<T>. The writer is the plain serializer call.
public static class CanonicalJson
{
    public static T Parse<T>(string json, JsonTypeInfo<T> typeInfo)
        where T : IJsonDocument<T>;
    public static bool TryParse<T>(string? json, JsonTypeInfo<T> typeInfo, T fallback, out T document)
        where T : IJsonDocument<T>;                   // fallback non-null; only JsonException absorbed
}

// The immutable named collection over any IJsonDocument payload. A sealed
// specialization derives with itself as TSelf (see the pattern under
// INamedCollectionSpecialization); no specialization lives in this library.
public abstract class NamedCollection<TValue, TSelf> : IJsonDocument<TSelf>
    where TValue : IJsonDocument<TValue>
    where TSelf : NamedCollection<TValue, TSelf>, INamedCollectionSpecialization<TValue, TSelf>
{
    public const int CurrentSchemaVersion = 1;        // the envelope's; reads reject any other
    public static TSelf Empty { get; }                // one instance per specialization
    protected NamedCollection(Entries entries);       // the only construction path

    public int Count { get; }
    public IReadOnlyList<string> Names { get; }       // canonical (name-sorted) order
    public bool Contains(string name);                // name rule: OrdinalIgnoreCase
    public TValue Get(string name);                   // fresh snapshot; KeyNotFoundException on a miss
    public bool TryGet(string name, [MaybeNullWhen(false)] out TValue value);
    public TSelf With(string name, TValue value);     // add-or-replace; snapshots; new spelling wins;
                                                      // blank / untrimmed name -> ArgumentException
    public TSelf Without(string name);                // idempotent; same instance on a miss

    public string ToJson();                           // the trio, resolved through
    public static TSelf FromJson(string json);        // TSelf.CanonicalTypeInfo — never reflection
    public static bool TryFromJson(string? json, out TSelf collection);   // Empty on failure

    public sealed class Entries { /* internal ctor — built only by the base */ }
}

// The static half of a specialization; implemented explicitly.
public interface INamedCollectionSpecialization<TValue, TSelf>
    where TValue : IJsonDocument<TValue>
    where TSelf : NamedCollection<TValue, TSelf>, INamedCollectionSpecialization<TValue, TSelf>
{
    static abstract TSelf Create(NamedCollection<TValue, TSelf>.Entries entries);
    static abstract JsonTypeInfo<TSelf> CanonicalTypeInfo { get; }
}

// The strict envelope. Abstract: each specialization closes it as a sealed
// public parameterless class passing its two wire names, and names that
// closed type in its [JsonConverter].
public abstract class NamedCollectionJsonConverter<TValue, TSelf> : JsonConverter<TSelf>
    where TValue : IJsonDocument<TValue>
    where TSelf : NamedCollection<TValue, TSelf>, INamedCollectionSpecialization<TValue, TSelf>
{
    protected NamedCollectionJsonConverter(string entriesPropertyName, string valuePropertyName);
                                                      // non-blank, trimmed, not "schemaVersion" / "name"
    public string EntriesPropertyName { get; }        // "filters" for the saved-filter document
    public string ValuePropertyName { get; }          // "config" for the saved-filter document
    public override TSelf? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options);
    public override void Write(Utf8JsonWriter writer, TSelf value, JsonSerializerOptions options);
}
```

Serialization contract: round-trips cleanly through `System.Text.Json` —
no consumer-side converter registration required. `CubeOwner`, `CubeAction`,
`CubeClaim`, `AnalysisMode`, and `AnalysisLevel` bundle
`StrictJsonStringEnumConverter<TEnum>` via attribute;
`Play` bundles `PlayJsonConverter`; `DecisionId` bundles
`DecisionIdJsonConverter`; `DiceRoll` bundles `DiceRollJsonConverter`;
`ProblemKey` bundles `ProblemKeyJsonConverter` (the only one implementing
the property-name overloads, so it also works as a dictionary key);
`BoardPosition` bundles `BoardPositionJsonConverter` (the 26 counts as a
number array; a malformed board is a `JsonException`), and each optional
after-board names `NullableBoardPositionJsonConverter` at the property.
Tested without any options-level registration in
`BgDecisionDataSerializationTests`, `DecisionRowSerializationTests`,
`DiceRollTests`, `ProblemKeyTests`, and `BoardWireTests`. The bytes of a
full record are pinned against 5a967cc's by `WireGoldenTests`; absence is
walked member by member by `WireAbsenceTests`.
A `NamedCollection` specialization bundles its own closed
`NamedCollectionJsonConverter` the same way, in its own repo; this library
ships the abstract converter and no closed one.

`BgDataTypesJsonContext` is the source-generated `JsonSerializerContext`
over this wire surface, byte-identical to the reflection path and chained
by downstream contexts — roots, the composition rules (public converters,
metadata-only generation), and the trim posture are in "Source generation
& trimming" above.

The five enums are **string-token-exact in both directions**: they write their
declared member names and read only those names — a numeric ordinal is a
`JsonException`, not a value. `AnalysisLevel`'s declaration order is contractual
and its members interleave, so an inserted member renumbers everything after it;
a reader that accepted ordinals would re-couple stored JSON to that numbering
(halheinrich/backgammon#164). Consumers that build their own
`JsonSerializerOptions` inherit this from the attribute and need add nothing —
but an options-level `JsonStringEnumConverter` registration of their own would
*loosen* it back, because an options converter outranks the type attribute.

## Benchmarks

`BgDataTypes_Lib.Benchmarks` is a BenchmarkDotNet harness over `Play`
construction — the one place in this library with a hot-path performance
contract, because BgMoveGen's generator builds a `Play` per candidate
several million times per BgRLEngine training run.

`PlayConstructionBenchmarks` groups its cases by move count (1–4), with the
incremental `Add` spelling as each group's baseline, and covers four
construction paths per group: `Add`, the fixed-arity `Create` overload, the
span overload from an existing array, and a collection expression.
`[MemoryDiagnoser]` is on — every path must allocate exactly nothing.

**The fixed-arity rows are gated at parity with `Add`; the span and
collection-expression rows are documented, not gated** (they carry a
caller-side argument buffer that is outside the method — see Pitfalls). The
`*_Add` baselines are themselves regression guards on `Add`'s own codegen.

Measured on an idle machine, .NET 10.0.11, x64 RyuJIT
(halheinrich/backgammon#137):

| Arity | `Add` | `Create` fixed-arity | `Create` span | collection expr |
|---|---|---|---|---|
| 1 | 1.00 | **0.88** | 1.37 | 1.31 |
| 2 | 1.00 | **0.85** | 1.60 | 1.53 |
| 3 | 1.00 | **0.96** | 1.69 | 1.66 |
| 4 | 1.00 | **0.91** | 1.82 | 2.05 |

Allocation is zero on every row. A `--disasm` pass on the four-move
fixed-arity case confirms the mechanism directly: `Create` inlines fully,
the slot switch folds to direct field writes, and `Count` is stored as a
literal — 126 bytes of code against the raw `Add` site's 107, with no
branch or jump table in either.

Run it in Release:

```
dotnet run -c Release --project BgDataTypes_Lib.Benchmarks
```

Add `--filter '*FourMoves*'` to narrow, or `--disasm` to inspect codegen.
Excluded from `dotnet test` via `IsTestProject=false` in its csproj — a run
takes minutes and asserts nothing, so it is measured on demand, never as
part of the suite.

**Numbers off this machine are contended.** eXtremeGammon rollouts routinely
run here and inflate BenchmarkDotNet's mean by up to 1.8x. Only ever read
the sibling comparison *within* one run — the grouped `Ratio` column — never
one run's absolute means against another's. Sequential "measure, edit,
measure" is not a valid comparison on this hardware.

## Pitfalls

- **A new wire unit must be declared three times, and the test only guards
  two of them.** `BgDataTypesJsonContextTests`' completeness check derives
  the wire closure from the declared roots, so a new *property* anywhere in
  the graph is covered automatically — but a brand-new root (a new document
  type, or a new converter-bearing token type) must be added to
  `BgDataTypesJsonContext`'s `[JsonSerializable]` list *and* to the test's
  root list, or the closure never sees it and the check passes vacuously.
  Byte-identity and converter-respect tests for the new root complete the
  gate. Keep its converter public and the context metadata-only — the
  composition rules in "Source generation & trimming".
- **`DecisionId.Filename` must not contain `':'`.** The canonical-form
  separator is the same character used to discriminate the two shapes at
  parse time. The guard is **symmetric** on both subtypes — without it
  on `XgpDecisionId`, a `.xgp` filename containing `':'` would parse back
  as a (malformed) `XgDecisionId`. Both ctors throw `ArgumentException`;
  `TryParse` returns false on the same input. Documented once on the
  base; enforced by each derived ctor via a shared private-protected
  helper.
- **Every stored member is `required` or nullable — a record states them
  all** (halheinrich/backgammon#222; "Absence on the wire"). Omitting a
  required member at construction is a compile error, and a document
  without it is a `JsonException` on both paths. Producers
  (`ConvertXgToJson_Lib`'s `Build*` sites) state every member, a checker
  play's inactive cube half and the empty strings included; tests build
  through `TestRecords`. `Id` was the first such member — the
  "producer-supplied identity" contract, no silent default IDs. Do not add
  a member with an initializer instead: the source-generated context drops
  initializers for absent members, which is the divergence the rule closed,
  and `WireAbsenceTests` fails a member that is neither kind.
- **A Crawford cube throws at construction, and `DecisionRow.Roll` is
  `required`** (`halheinrich/backgammon#201`). A cube record in a Crawford
  position gets `ArgumentException` from whichever of its two halves is set
  second (`Position`/`Decision` on the composite, `Roll`/`IsCrawford` on
  the row), from an object initializer and from JSON alike — so a fixture
  that wants the Crawford flag builds a Crawford *play*, and a test that
  needs a Crawford cube *key* (still in `ProblemKey`'s grammar, for old
  stats documents) builds it from the key string, never from a record.
  Every `DecisionRow` initializer must state `Roll`: omitting it is a
  compile error, and a JSON document without it is a `JsonException`, not
  a cube. The row's guards are order-independent only together with
  `Roll`'s backing field; `DecisionRow.Roll`'s doc comment owns why, so
  read it before touching either setter.
- **`BgDecisionData.Position` and `Decision` reject null at init; absent
  is not null** (`halheinrich/backgammon#221`, `halheinrich/backgammon#222`).
  An explicit null — from an initializer or a JSON `"Position":null` —
  throws `ArgumentNullException` naming the member, through the reflection
  path and the context alike; a document that omits the half is a
  `JsonException` on both, refused as absent before any setter runs. There
  is no `ProblemKey` no-key rung for a null half any more: the record
  cannot exist, so a test or reader must not expect degrade-to-no-key
  there.
- **The source-generated context passes an absent init-only member as
  `default`, not as its initializer — which is why no member has one.** The
  generated creator for every init-only type in the wire graph is one
  object initializer over an argument array
  (`ObjectWithParameterizedConstructorCreator`), and an absent member
  arrives there as `default(T)` where the reflection path keeps an
  initializer (measured 2026-09-14; `{"Id":"x"}` read with null halves
  through the context). `halheinrich/backgammon#222` closed it by leaving
  nothing to diverge on: a required member is refused absent on both paths
  before the creator runs, and a nullable member's default is the `null`
  both paths give. A member added with an initializer reopens it.
- **A money record with `IsJacoby == null` has no `ProblemKey`, silently.**
  `PositionData.IsJacoby` is not `required` — it cannot be, since match
  records legitimately carry `null` — so the omission compiles, constructs,
  and serializes fine, and only shows up as `TryDerive` returning `false`.
  That is the ratified no-key rung working as designed (guessing "Jacoby
  off" would file the record under a wrong key), but it means a money
  fixture is not a money fixture until it stamps the flag: any test or
  producer building a record with `OnRollNeeds == 0` and
  `OpponentNeeds == 0` must set `IsJacoby` explicitly. The reverse is not a
  hazard — a stamp on a match record is ignored, not rejected.
- **The bare `money` CSV token means "rule unknown", not "no Jacoby".** With
  `DecisionRow.IsJacoby` unset, `MatchScore` writes `money` — the same string
  the pre-`halheinrich/backgammon#121` shape wrote for *every* money row. It
  is the honest spelling (it states the session and withholds the rule, and
  is neither `moneyJ` nor `moneyNJ`, which is the ruled filter behaviour),
  but it is trap-shaped two ways: a producer that forgets to stamp emits
  rows indistinguishable from legacy output, and a reader who reads `money`
  as "Jacoby off" is silently wrong. Fed back through a filter surface it at
  least fails loud — `money` is the retired token there. Same discipline as
  the no-key rung above: any producer building a money row must stamp
  `IsJacoby` explicitly.
- **`DecisionRow.MatchScore` is computed, not stored.** It is derived from
  `OnRollNeeds`, `OpponentNeeds`, `IsCrawford`, `MatchLength`, and
  `IsJacoby` on every access. Do not try to set it, and do not cache it
  across mutations of those fields (though init-only semantics make mutation unusual anyway).
- **CSV methods live on `DecisionRow`.** This is a deliberate, accepted
  deviation from the "pure data, no behavior" principle — the CSV format
  is tightly coupled to the column order and travels with the type. Do
  not move it into a separate formatter class without a strong reason.
- **Mop sign convention is player-relative, not color-relative.** Positive
  always means the on-roll player, regardless of which physical color they
  are playing. Code that forgets this will silently mirror boards.
- **`IDecisionFilterData.Board` is in the on-roll frame.** The layout is the
  type's now (`BoardPosition`); the frame is not, so a new implementer
  returns the `PositionData.Mop` frame exactly — `XgFilter_Lib` filters
  assume it.
- **After-boards use flipped POV.** `AfterBestBoard` / `AfterPlayerBoard` hold
  the same layout as `Board` but in the next mover's frame — the opponent is
  on roll after a play, so the decision-maker's checkers are *negative* and
  the opponent's are positive. Code that forgets this mirrors the
  after-boards silently.
- **After-boards are `null` when absent, never empty** (halheinrich/backgammon#15).
  Always for cube decisions — a producer contract, not guarded in the
  forwarding implementation on `BgDecisionData` — and on a checker play
  whose boards the producer could not compute. Consumers test each board
  for `null`, not `IsCube` and not a length. The legacy `[]` still reads as
  `null`; it is never written.
- **`Move.ToPt` sign-encoding.** `0` is bear off (not "stay on point 0"),
  negative is a hit landing on `|ToPt|` (not a backward move — players
  cannot move backward), positive is a regular move. Code that compares
  `ToPt` numerically without understanding the encoding will silently
  misinterpret hits and bear-offs.
- **`Play` is a mutable value type.** `Add` / `RemoveLast` mutate in place,
  but assigning a `Play` to another variable copies the buffer. Code that
  retains a reference into a `List<Play>` slot and mutates it later is
  modifying the local copy, not the list element. Use `Snapshot()` when
  the intent is an explicit independent copy, and re-assign back to the
  list slot when mutation is intended. `foreach` likewise enumerates a
  value copy — a mid-loop `Add` on the source is invisible to the
  iteration (pinned by test).
- **`Play.Create`'s cost depends on which overload you reach.** The four
  fixed-arity overloads construct at parity with the incremental `Add`
  spelling (0.85–0.96x measured, allocation-identical) and are what a
  literal argument list binds to. `Create(params ReadOnlySpan<Move>)` and
  collection expressions cost 1.3–2.1x — not because of anything inside
  the method, but because the *caller* materialises an argument buffer
  before the call, which no change to `Play` can remove. That is fine for
  tests and readability sites and wrong for a move-generation inner loop:
  in a hot path, pass the moves as separate arguments (or keep using
  `Add`), and do not "tidy" such a site into a collection expression.
  Numbers and the standing guard live in Benchmarks.
- **The slot-write seam is private and must stay the only one.** `Add` and
  all five `Create` overloads write moves through one private `SetSlot`
  primitive, which owns both the ordinal → field mapping and the `Count`
  maintenance that goes with it; no other member touches a slot field or
  `Count`. It carries `[MethodImpl(AggressiveInlining)]` deliberately and
  measurably: without it `Add` does not fold its slot switch and costs
  **8x** (caught by the `*_Add` benchmark rows, which exist as that
  regression guard). A future construction path adds an overload that
  calls `SetSlot` with literal indices — it does not write slots itself,
  and it does not loop over `Add`.
- **`Play` and `CanonicalPlay` have no equality — by ruling, not
  omission.** `==` does not compile; `Equals`, `GetHashCode`, and so any
  comparer, hashed collection, `Distinct`, or record or tuple holding a
  play, throw `NotSupportedException`. Compare plays with
  `BoardState.IsSamePlay` from their starting position, find one in a list
  with `IndexOfSamePlay`, and compare stored encodings with
  `Play.IsSameEncoding`. Do not reintroduce a board-less comparison, a
  notation key, or a `CanonicalPlay`-based dedupe: the display form differs
  between encodings of one play (`halheinrich/backgammon#277`).
- **`Play` requires its bundled `JsonConverter`.** Default property-based
  serialization only sees `Count`, losing every move. The
  `[JsonConverter(typeof(PlayJsonConverter))]` attribute is intrinsic to
  the type — do not strip it, and do not register a different converter
  for `Play` in consumer-side options without understanding the
  consequence.
- **`BoardState` cannot be written by callers, and must not be reopened**
  (halheinrich/backgammon#281). `Points` is a `ReadOnlySpan<int>` and
  `HighPointOccupied` has a private setter, pinned by
  `BoardStateTests.PublicSurface_HasNoWritableState`. A test or consumer
  that wants a particular board builds it — `FromMop` from counts, or
  `new BoardState(position)` — and changes it through the apply methods;
  it never writes counts. Re-exposing a writable array, or a public
  `RecalcHighPoint`, would bring back the desynced high point and the
  corrupt boards `ApplyPlay`'s guarantee cannot survive. The apply/undo
  helpers keep the per-change maintenance, not property setters, because
  hot-path move generation needs zero-overhead apply/undo.
- **Bearing-off overshoot is a property of the data shape.** Bear-off
  legal only from `HighPointOccupied` when `HighPointOccupied <= 6`
  *and* the die exceeds `FrPt`. The `BoardState` data primitive does
  not enforce this — `Move(FrPt, 0)` is encodable for any `FrPt` —
  but `BgMoveGen.MoveGenerator.NextMove` does. Code that hand-builds
  bear-off moves outside the move generator must respect the rule.
- **`Bg960` mirror conflicts.** Point `i` and point `25 - i` can never
  both be made (they'd collide under symmetry). `Bg960` rejects the
  mirror partner as it picks each quadrant representative.
- **Pip-count integer width.** Per-product max is `15 × 25 = 375`, total
  fits comfortably in `int`. Do not narrow to `byte` / `short` if you
  copy this logic elsewhere.
- **`ApplyPlay` flips perspective; the bare flip is private.** After
  `ApplyPlay`, positive values represent the *next* mover's checkers,
  not the previous on-roll's. There is no public `Flip()` — callers
  reasoning in on-roll POV never need to flip explicitly. Code that
  expects to inspect a state "from the original mover's POV" after a
  turn must take a `Copy()` *before* calling `ApplyPlay`. To *view* a
  position from the other player's frame without advancing state, use
  `BoardPosition.Flipped()` (a value, allocation-free) or `FlippedCopy()`
  (a board) — never re-encode negate-and-reverse in a consumer; the
  value's `Flipped()` is the one statement of the rule.
- **`AnalysisMode` and `AnalysisLevel` always travel as a pair, and
  `Unknown`/`Unknown` is data, not an error.** Both zero values are
  deliberate — "depth not recorded" — and a producer states them: the
  members are required (halheinrich/backgammon#222), so JSON written before
  the pair existed is refused rather than read as `Unknown`/`Unknown`, while
  the retired flat `AnalysisDepthClass` beside a full pair is still an
  unrecognized property, ignored on read. `BookRollout` +
  `AnalysisLevel.Unknown` is additionally a live producer stamp (book hit
  without recoverable levels), so code must not treat `AnalysisLevel.Unknown`
  as implying `AnalysisMode.Unknown`. Declaration order is what the UI
  renders and, for `AnalysisLevel`, is *contractual* ascending rigor (ruled
  2026-08-28 on XG's own menu): the ply and Roller families interleave, so a
  reorder or out-of-order insertion breaks the diagram's level floor and the
  level dropdowns. `Unknown` is outside that scale — never a floor, never a
  threshold. `DepthRank` / `CubeDepthRank` remain the ordering surface for
  consumers comparing whole analyses across the mode × level pair. Do not
  strip a member's `[Description]` label: downstream label readers (XgFilter_Lib's
  `EnumLabel.ToLabel`) throw on a member without one.
- **`IDecisionFilterData.Dice` is null for cube decisions, fail-loud on
  malformed storage.** Null means "no dice apply" (a cube is offered before
  the roll — the `FilterError` null-when-inapplicable convention), never
  "data was bad": a checker play whose stored roll is malformed
  (`DecisionRow.Roll` digits outside 1–6, e.g. `70`; `DecisionData.Dice`
  left at its `{0, 0}` default) throws `ArgumentOutOfRangeException` from
  the `DiceRoll` constructor on access. Both derivations are `[JsonIgnore]`d
  so the throwing getter never runs during serialization — the
  `BestDoublerAction` precedent — and the stored forms (`Roll`,
  `Decision.Dice`) remain the wire. Deliberately unlike the
  `AnalysisMode`/`AnalysisLevel` graceful `Unknown`: legacy data genuinely
  lacks a depth stamp, but no legitimate data lacks dice on a checker play,
  so a soft null here could only mask a producer bug.
- **`default(DiceRoll)` is non-meaningful.** A `record struct` cannot run
  its face validation on `default`, so `default(DiceRoll)` carries faces of
  0. "No roll" is modelled as `DiceRoll?` null, never as `default`. The
  standard value-type caveat, shared with `Play` and `CubeDecisionPair`.
- **`PlayCandidate.EquityLoss` is non-nullable; `0.0` means no loss
  vs. best.** Identifying the best candidate uses
  `DecisionData.BestPlayIndex`; testing membership in the best-equity
  equivalence class uses `EquityLoss == 0.0`. Do not filter by
  `EquityLoss == null` — `EquityLoss` is non-nullable.
- **`DecisionData` cube-scoring helpers throw when `IsCube` is false.**
  All six (four computed properties — the action pair and the claim pair —
  plus two methods) guard on `IsCube` and throw
  `InvalidOperationException` on play decisions — they encode a cube-only
  policy and silent zeros would mask misuse. Callers in mixed-decision
  contexts must check `IsCube` first. The four computed properties carry
  `[JsonIgnore]` so `System.Text.Json` does not invoke their throwing
  getters during serialisation; do not strip those attributes.
- **Cube-scoring atomic-action methods reject the wrong half.**
  `DoublerActionError(CubeAction)` accepts only `Double` / `NoDouble`;
  `TakerActionError(CubeAction)` accepts only `Take` / `Pass`. The
  other half throws `ArgumentOutOfRangeException`.
- **`UserDoublerAction` / `UserTakerAction`: half-guarded on init,
  cross-half consistency is NOT guarded.** Each rejects the other half's
  actions with `ArgumentOutOfRangeException` at `init`, but "a recorded
  taker response implies the doubler doubled" is a producer contract —
  init-only halves are set independently, so nothing stops constructing
  `(NoDouble, Take)`. Null means "not recorded" (all JSON written before
  the fields existed, and every play decision), not "declined": a null
  `UserTakerAction` alongside a recorded `NoDouble` is the normal
  undoubled-game state — no taker decision ever existed — not missing
  data. And they are *actions, never claims* —
  do not infer `CubeClaim` from a played action (see "Played cube actions
  on DecisionData"); the rationale is a property of the analysis.
- **`IMatchInfo.IsMoneyGame` has exactly one spelling — the interface's
  default implementation.** Implementers inherit it; redeclaring it with a
  different derivation forks the money-game rule. The derivation is valid
  only because producers normalize XG's unlimited-session sentinel (99999)
  to 0 at the parse boundary — an implementation surfacing the raw
  sentinel would break every `IsMoneyGame` consumer silently.
- **`IGameInfo` money conventions are contractual, not incidental.**
  `Away1 == 0`, `Away2 == 0`, `IsCrawfordGame == false` for money
  sessions. New implementers must honor them — filter layers key off the
  zeros the way `IDecisionFilterData.IsMoneyGame` keys off
  `MatchLength == 0`.
- **`default(CubeDecisionPair)` is non-meaningful.** A `record struct`
  cannot run its half-guards on `default`, so `default(CubeDecisionPair)`
  is `(NoDouble, NoDouble)` — whose `Taker` is not a valid taker action.
  Construct pairs explicitly; do not treat `default` as a "no decision"
  sentinel. This is the standard value-type caveat, shared with `Play`
  and `DiceRoll`.
- **`default(CubeClaimPair)` is non-meaningful.** Same caveat, same shape:
  `default` bypasses the half-guards and carries `(NoDouble, NoDouble)` —
  whose `Taker` is not a valid taker action. "No answer" is
  `CubeClaimPair?` null, never `default`.
- **`CubeClaim.TooGood` and `CubeAction.NoDouble` are the same board
  action.** The claim layer exists precisely because two claims collapse to
  one action (SPEC-scoring §3). Code bridging claims to the action-level
  scoring helpers must go through `CubeClaimExtensions.ToCubeAction` —
  re-encoding the collapse inline creates a second source of the rule. The
  reverse direction does not exist: never infer a claim from an action
  (underdetermined); the only equities→claim door is
  `DecisionData.BestDoublerClaim`.
- **`BestClaimPair` can derive the incoherent cell — on the tie boundary
  only.** At `NoDoubleEquity == 1` exactly with `DoubleTakeEquity >= 1`,
  both halves tie and the ruled tie-breaks (NoDouble; Pass) compose to
  `CubeClaimPair.NoDoublePass` — the cell SPEC-scoring §3 calls "never a
  verdict". Measure-zero and equity-neutral (every answer scores
  identically there), pinned by test as the spec-literal reading of the
  strict `> 1` predicate, and flagged to the umbrella as a candidate spec
  sharpening; the 2026-09-02 amendment left it standing. Off the boundary
  the derived truth is always one of the four reachable verdict cells
  (pinned over a grid). Consumers rendering the derived truth should not
  assume `!IsIncoherent`.
- **`CubeClaimPair.TooGoodTake` is representable but never derived.**
  Since SPEC-scoring §3's 2026-09-02 amendment (`halheinrich/backgammon#187`)
  Too Good requires the pass, so `BestClaimPair` cannot compose it (pinned
  over a grid, and counted at zero over the local corpus). The cell stays
  on the closed 3×2 because a data-types library does not hide cells;
  consumers do not offer it, and must not treat its presence in the type as
  a hint that it is reachable.
- **Never re-derive Too Good offerability from the rules fields.**
  `BgDecisionData.CanBeTooGood` is the one site; spelling
  `IsMoneyGame && IsJacoby == true && CubeOwner == Centered` (or worse, a
  near-miss like `IsJacoby != false`, which admits the unknown-rule record)
  in a consumer creates a second source of the ruling.
- **A `NamedCollection` specialization is a closed pattern, and every part
  of it is load-bearing.** The shape (the saved-filter document of
  halheinrich/backgammon#190 leg (B) and the queued mix document follow it):
  a `sealed` class deriving `NamedCollection<TPayload, TSelf>` *with itself
  as `TSelf`* and listing `INamedCollectionSpecialization<TPayload, TSelf>`;
  a private constructor forwarding `Entries` to the base and nothing else;
  `Create` and `CanonicalTypeInfo` implemented explicitly, the latter
  returning the specialization's entry in *its own repo's* source-generated
  context, where it is declared as a `[JsonSerializable]` root; a
  `sealed`, `public`, parameterless converter class deriving
  `NamedCollectionJsonConverter<TPayload, TSelf>` and passing the two wire
  names; and a type-level `[JsonConverter]` naming that closed converter.
  What breaks if a part is skipped: an open or generic converter type
  cannot be named by an attribute, and a converter without a public
  parameterless constructor, or an internal one, fails every downstream
  context that embeds the document with SYSLIB1220 then SYSLIB1030 — the
  generator emits `new TheConverter()` into the consumer's assembly and
  silently drops the type when it cannot. A converter *factory* activating
  a closed converter at runtime is the reflection path the pattern exists
  to avoid and the trim analyzer here rejects. A specialization deriving
  with a *different* specialization as `TSelf` compiles and fails at
  runtime (`InvalidCastException`) on first `ToJson` or `Without`. The two
  wire names are the specialization's identity: changing them is a file
  migration, not a refactor — the saved-filter document's are `filters` and
  `config`, fixed by every `xg-filters.json` on users' disks. The base's
  `Get` / `TryGet` are the only get pair: a specialization adds no
  domain-spelled forwarder (`GetConfig`) — its callers rename (ruled
  2026-09-09 on halheinrich/backgammon#190).

## Subproject-internal next steps

Cross-cutting work (consumer migrations, downstream refactors) is tracked in
the umbrella `INSTRUCTIONS.md` "Next up" / "Deferred" sections, not here.

- **`DecisionRow` factory split.** Test-helper duplication around cube-row
  construction (`DecisionRowBuilder.Build` / `BuildCube` shapes that consumers
  re-implement) hints at missing factories on `DecisionRow` for the common
  shapes (checker row, cube row). Library gap; consumer glue.
