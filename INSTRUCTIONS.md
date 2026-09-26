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

Four projects under `BgDataTypes_Lib.slnx`, governed by repo-root
`Directory.Build.props` (TFM, `TreatWarningsAsErrors`, XML doc generation)
and `Directory.Packages.props` (Central Package Management — no inline
`Version=` anywhere).

**`BgDataTypes_Lib/`** — the library. Seven areas, one file per type:

- **The decision record, its two kinds and their categories** —
  `BgDecisionData`, the abstract record every consumer passes around, and
  its two sealed kinds `CheckerPlayDecision` and `CubeDecision` (with
  `DecisionKind`, the kind as a value, and `BgDecisionDataJsonConverter`,
  which dispatches on it); the categories they hold — the shared
  `PositionData` and `DescriptiveData`, and each kind's `Decision`
  category, `CheckerPlayDecisionData` (with `PlayCandidate` beneath it) and
  `CubeDecisionData`; `DecisionRules`, the internal statement of the rules
  that bind a decision's members, and `DocumentRefusal`, the internal one
  spelling of how a rule a document breaks is refused. `DecisionRow` is the
  flat projection for CSV/JSON export.
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
  register nothing. `BgDecisionDataJsonConverter` is the one that
  dispatches rather than writes: it finds a record's kind and delegates the
  document to that kind's generated contract. All are public —
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

**`BgDataTypes_Lib.TestSupport/`** — `TestRecords`, the record builders,
shipped for test projects to reference: this repository's and every
consumer's. Not a product and not a test project; see "The test-support
project" under Architecture.

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
`BoardPositionJsonConverter` on `BoardPosition`, `DecisionKind` on the
strict enum converter too, and `BgDecisionDataJsonConverter` on
`BgDecisionData`. Consumers do not need to register any of these converters on their
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
`DiceRoll`, `BoardPosition`, the six enums — `CubeClaim` declared ahead of its first
embedding document so the claim vocabulary is born source-genned and
downstream contexts chain rather than re-cover it); composite parts ride
the generator's graph walk. Two converters stop that walk, so what lies
past them is declared explicitly and resolved through the active options
at runtime: `Move` (past `Play`'s converter) and the two decision kinds
`CheckerPlayDecision` and `CubeDecision` (past
`BgDecisionDataJsonConverter`, which reads `DecisionKind` to choose). A completeness test (the halheinrich/backgammon#144
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
  something): `PositionData.IsJacoby`; `CheckerPlayDecisionData.UserPlayIndex`
  and `UnlistedPlayError`; `CubeDecisionData.UserDoublerAction`,
  `UserTakerAction`, `UnstatedDoublerActionError`,
  `UnstatedTakerActionError`, `Depth`, `DepthAbbreviation`;
  `DescriptiveData.OnRollName`, `OpponentName`, `Title`, `Date`, `Event`,
  `IsStandardStart`, `Comment`; `PlayCandidate.Depth`, `DepthAbbreviation`
  and its six probabilities; `DecisionRow.Error`, `Player`,
  `IsStandardStart`, `Roll`, `AnalysisDepth`, `IsJacoby` and both
  after-boards. Every other serialized member is required, each record's
  `Kind` included — through `[JsonRequired]`, since the type states it and
  code never does. `WireAbsenceTests` walks the graph from the context's
  own metadata, from each kind's contract (125 members across four
  documents — each record kind and each row kind — and nine types; 137
  before the stored copies of derivable values left the wire) and pins
  both halves of the rule on both paths, plus that every member is exactly
  one kind. A nullable member's absence reads exactly as its explicit
  `null`: as `null`, or refused when `null` breaks a rule of the decision's
  kind (a checker row states its roll).
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
  required member of the kind they build — no other kind's members exist to
  state. Tests build records through `TestRecords`
  (`BgDataTypes_Lib.TestSupport`), whose builders yield a well-formed record
  of each kind with realistic defaults and take the members a test cares
  about as named arguments (see "The test-support project").

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

Every decision holds the two shared categories; each kind holds its own
`Decision` category (see "The decision kinds"):

| Type | Held by | Fields |
|---|---|---|
| `PositionData` | both kinds | `Mop`, `OnRollNeeds`, `OpponentNeeds`, `CubeSize`, `CubeOwner`, `IsCrawford`, `IsJacoby?`; derived `OnRollPipCount`, `OpponentPipCount` (the board's, by `BoardState`'s one pip rule) |
| `DescriptiveData` | both kinds | `MatchLength`, `OnRollName?`, `OpponentName?`, `Title?`, `Date?`, `Event?`, `IsStandardStart?` (none for a standalone position), `Comment?`, `Flagged` — the game, the move number and the source file are the `Id`'s (see "DecisionId"; `BgDecisionData.SourceFile` derives it) |
| `CheckerPlayDecisionData` | `CheckerPlayDecision` | `Dice` (two faces, rolled order), `Plays` (never empty), `UserPlayIndex?`, `UnlistedPlayError?` (only with no `UserPlayIndex`); derived `BestPlayIndex` (the first candidate of the highest equity), `BestPlay`, `UserPlay?`, `EquityLoss(i)`, `UserPlayError?` |
| `CubeDecisionData` | `CubeDecision` | `Depth?`, `DepthAbbreviation?`, `AnalysisMode`, `AnalysisLevel`, the cube equity and probability fields, `ProbOfOpponentErrorJustifyingDouble`, `UserDoublerAction?`, `UserTakerAction?`, `UnstatedDoublerActionError?`, `UnstatedTakerActionError?` (each only with its half's action unstated); derived `DepthRank`, `UserDoubleError?`, `UserTakeError?` and the scoring policy |

**No stored copy of a derivable value** (the umbrella's verdict on the
records leg of `halheinrich/backgammon#273`). Every member the others
determine is derived from them and never stored, so it cannot disagree
with them — the pip counts, the source file, each depth rank, the best
play, each candidate's equity loss, the user's error where the record
determines it (a listed play's, a stated cube action's). A derived member
is `[JsonIgnore]`d; a document still stating one reads with it ignored,
even in the categories that refuse unknown members — the serializer knows
the member and skips its JSON (measured on .NET 10, both paths) — and the
derivation stands. The audit, member by member, and what stays stored and
why, is under "Stored or derived" below.

**None recorded is `null`** (the same verdict). "None recorded" has one
spelling, for text as for numbers: `null`. A text member states text or,
where it may, nothing — empty or white-space text is refused, by code with
an `ArgumentException` naming the member and by a document with a
`JsonException` (the one statement of the rule is the internal
`StatedText`). The names, the title, the event and the comment on
`DescriptiveData`, a candidate's and the cube analysis's depth label and
abbreviation, and the row's `Player` and `AnalysisDepth` are nullable; a
member that is never none — the XGID, the id's file name — is refused
empty the same way. `DepthRank` is `int?`: `null` where the depth is not
recorded (the mode, or an evaluation's level, `Unknown`), where the
producer wrote the grid's floor 0. `Date` was already `DateOnly?`. The
analysis enums keep `Unknown`, a member the producer states.
`IDecisionFilterData.Player` is `string?` accordingly.

### Stored or derived

The audit behind the rule, over every record type (the umbrella's verdict
asked for it exhaustively). Derived, never stored:

| Member | Derived from |
|---|---|
| `PositionData.OnRollPipCount`, `OpponentPipCount` | `Mop`, by `BoardState`'s one pip rule |
| `BgDecisionData.SourceFile`, `DecisionRow.SourceFile` | `Id.Filename` |
| `PlayCandidate.DepthRank`, `CubeDecisionData.DepthRank` | `AnalysisMode` × `AnalysisLevel`, by the grid the producer used (internal `AnalysisDepthRank`, moved here unchanged) |
| `CheckerPlayDecisionData.BestPlayIndex` | the candidates' equities: the first of the highest |
| `CheckerPlayDecisionData.EquityLoss(i)` | `BestPlay.Equity − Plays[i].Equity` |
| `CheckerPlayDecisionData.UserPlayError` | `EquityLoss(UserPlayIndex)` for a listed play; otherwise the stored `UnlistedPlayError` |
| `CubeDecisionData.UserDoubleError`, `UserTakeError` | the stated action's `DoublerActionError` / `TakerActionError`; otherwise the stored unstated-action error |
| earlier: `Game`, `MoveNumber` (the `Id`), the after-boards (the play rule), `Notation` (the play), `Dice` (the roll), `MatchScore`, `IsMoneyGame` | — |

Stored, as source data:

- **`Xgid`** carries three facts the record holds nowhere else: the cube
  limit (field 10), the beaver rule (bit 2 of field 8, money only) and, for
  a money game, the file's game-header scores (fields 6–7; a money
  record's needs are 0). Its other fields — position, cube, turn, dice,
  match score, Crawford, Jacoby, match length — repeat record members and
  are not checked against them: an unchecked partial copy, left open.
- **`DepthAbbreviation`** (a candidate's, the cube's). The mode and level
  determine it only for an evaluation, a bare book hit and a rollout with
  no recorded context. A rollout's and an enriched book hit's
  abbreviation carries the trial count, and an unrecognised level's the
  raw code, which the record holds only inside `Depth`'s prose; deriving
  it would parse the producer's label grammar. Left open: a structured
  trial count would let both display forms be derived.
- **`Depth`** — the producer's label, carrying the trial count, the book
  edition (V1 / V2) and an unrecognised level's raw code, which no member
  states. **`AnalysisMode` / `AnalysisLevel`** are the structured facts
  behind it.
- **`Id`, with its `IsCube`** — the identity used apart from the record
  (stats keys, file navigation), so it carries its own kind; the record
  holds the two to agreement (`DecisionRules.IdAgrees`).
- **`MatchLength` beside the away scores** — the money/match stand-in, its
  own leg.
- **The equities and probabilities** — the analyser's outputs, verbatim.
  Some are related in theory (a total loss probability is one minus the
  total win's; a money game's cubeless equity follows from its
  probabilities), but whether XG's stored figures satisfy those relations
  exactly is unmeasured, so none is derived here.
- **What was played** — `UserPlayIndex`, `UserDoublerAction`,
  `UserTakerAction` — and **the analyser's error where the record does not
  state what it scores**: `UnlistedPlayError`,
  `UnstatedDoublerActionError`, `UnstatedTakerActionError`, each refused
  beside what would determine it.
- **The source file's facts** — the roll, the candidates, the board, the
  scores, the cube, Crawford, Jacoby, the names, title, date, event,
  standard start, comment and flag.

`DecisionRow` is a projection: within the row its `SourceFile`, `Game`,
`MoveNumber`, `Dice`, `MatchScore` and `IsMoneyGame` derive from its own
columns; `Error`, `Equity`, `AnalysisDepth` and the boards are the record's
values carried as columns, since the row holds nothing they derive from.

**The producer's copies.** `ConvertXgToJson_Lib` still produces each
derived value alongside the record. Its leg drops them, and its corpus test
checks XG's numbers against the derivations here — the user's error above
all (XG's `MoveError` against `EquityLoss` of the played candidate, XG's
cube errors against the scoring policy's). Until then the converter does
not build against this library.

### Shared types

| Type | Notes |
|---|---|
| `CubeOwner` | enum: `OnRoll`, `Opponent`, `Centered` — serializes as string |
| `CubeAction` | enum: `NoDouble`, `Double`, `Take`, `Pass` — a player's cube response, serializes as string. Beaver/raccoon deliberately not yet members (see XML `<remarks>` on the type); enums extend without disturbing existing members. |
| `CubeClaim` | enum: `NoDouble`, `Double`, `TooGood` — the doubler half of a cube answer at the claim layer (SPEC-scoring §1/§3, `halheinrich/backgammon#86`), serializes as string. A claim about the position, not a board action: `NoDouble` and `TooGood` share the identical board action (`CubeAction.NoDouble`), and `CubeClaimExtensions.ToCubeAction` is the single spelling of that collapse. Deliberately *not* a fifth `CubeAction` member — "too good" is a rationale, ruled claim-layer only. Declaration order is the ruled claim axis {No Double, Double, Too Good}, what a UI offering the claims renders. No reverse action→claim mapping exists: the claim is underdetermined by the action alone; the only equities→claim door is `CubeDecisionData.BestDoublerClaim`. |
| `AnalysisMode` | enum: `Unknown`, `Evaluation`, `Rollout`, `BookRollout` — how an XG analysis's numbers were produced; the mode axis of the two-axis depth taxonomy, serializes as string. Always paired with `AnalysisLevel`; together the pair is the taxonomy SSOT for depth filtering, replacing the retired flat `AnalysisDepthClass` (whose single axis could not represent book entries carrying separate moves and cube rollout levels). Classification is producer-side (ConvertXgToJson_Lib stamps both axes). `Unknown = 0` deliberately — "not recorded", which a producer states; the members carrying the pair are required on the wire (see "Absence on the wire"), so JSON lacking them is refused rather than read as `Unknown`, while the retired flat class's property beside them is still ignored on read. `BookRollout` is a book hit — rollout-derived, with parameters in the book database rather than the source file; `BookRollout` + `AnalysisLevel.Unknown` is the graceful-degradation stamp (no book DB available at conversion time, or a V1-book hit recording no levels). The UI renders modes in declaration order. Every member carries a `[Description]` display label (XgFilter_Lib's `EnumLabel.ToLabel` throws without one). Trial counts stay label-only. |
| `AnalysisLevel` | enum: `Unknown`, `Ply1`, `Ply2`, `Ply3Red`, `Ply3`, `XgRoller`, `Ply4`, `XgRollerPlus`, `Ply5`, `Ply6`, `Ply7`, `XgRollerPlusPlus` — the evaluation level; the level axis paired with `AnalysisMode`, serializes as string. For `Evaluation` it is the level of the evaluation itself; for the rollout-family modes it is the inner evaluation level — checker rows carry the inner moves level, cube rows the inner cube level (a single rollout can use different levels for the two; which one a row gets is the producer's concern, the semantics are owned here). Rollout-family modes never pair with a Roller-family level on checker rows but can on cube rows (the shipped book DB contains cube rollout levels of XG Roller). `Unknown = 0` deliberately — "not recorded", a value the producer states, never an absent member (see `AnalysisMode`). **Declaration order is contractual** (ruled 2026-08-28 on the authority of XG's own analysis-level menu, amended the same day): every member after `Unknown` ascends in rigor, and the ply and Roller families *interleave* rather than forming two blocks — `Ply3`, `XgRoller`, `Ply4`, `XgRollerPlus`, `Ply5`. Reordering, or inserting out of rigor order, is a breaking change; live consumers read the order (the diagram's level floor, the filter-panel and quiz level dropdowns). `Unknown` sits *outside* the rigor scale — not "least rigorous" but "not recorded": never excluded by a floor, never offered as a threshold; head-of-list is the zero-value requirement, not a rank. `DepthRank` (a candidate's and the cube analysis's) remains the ordering surface across the mode × level *pair*. Every member carries a `[Description]` display label. `Ply3Red` is XG's "3-ply Red" — its own member between `Ply2` and `Ply3` as of the same ruling, superseding the earlier collapse into `Ply3` as a label variant. |
| `CubeDecisionPair` | `readonly record struct (CubeAction Doubler, CubeAction Taker)` — a complete cube decision as two atomic actions. Validated on construction via the positional-record idiom: `Doubler` ∈ {`NoDouble`, `Double`}, `Taker` ∈ {`Take`, `Pass`}; a cross-half value throws `ArgumentOutOfRangeException`. The verdict aggregate (pair → correct/wrong) is intentionally absent and returns later with `CubeVerdict`. `default` is non-meaningful — see Pitfalls. |
| `CubeClaimPair` | `readonly record struct (CubeClaim Claim, CubeAction Taker)` — the two-part cube answer of SPEC-scoring §3 (`halheinrich/backgammon#86`): the claim-layer counterpart of `CubeDecisionPair`, pairing the three-valued claim with the taker response if doubled. Same construction-guard idiom (`Claim` any defined member, `Taker` ∈ {`Take`, `Pass`}). A closed 3×2 of six named canonical instances: five verdict cells (`NoDoubleTake`, `DoubleTake`, `DoublePass`, `TooGoodTake`, `TooGoodPass`) plus `NoDoublePass`, the incoherent cell — representable *by ruling* (a selectable user answer; cross-disabling the axes was rejected), named by `IsIncoherent` for review surfaces. One type serves both scored roles — a user's submitted answer and the derived truth (`CubeDecisionData.BestClaimPair`). Scoring semantics stay with the consuming legs. No parse/format story: display strings are consumer copy per SPEC-scoring §3, and no wire token is ruled — its wire debut (and wire shape) belongs to the first document that embeds it. `default` is non-meaningful — see Pitfalls. |
| `DecisionKind` | enum: `CheckerPlay`, `Cube` — the kind of a decision as a value (`BgDecisionData.Kind`, `DecisionRow.Kind`, `IDecisionFilterData.Kind`), serializes as its string token through the strict converter. The record's kind is its type; match on the record (`Match` / `Switch`) for exhaustiveness. |
| `DiceRoll` | `readonly record struct` — a dice roll in canonical unordered form: `High`/`Low`, each a validated face 1–6. The constructor accepts either order and canonicalizes (the XG parser stamps dice in rolled order, so both `31` and `13` reach it for a 3-1); canonicalization is single-sourced here, nowhere downstream, and record-struct equality over the canonical form makes 3-1 ≡ 1-3 automatic. `IsDouble`; `Parse`/`TryParse` of the two-digit token form (`IParsable` + `ISpanParsable`, accepting either spelling); `ToString()` → canonical high-first token (`"31"`). Ordered (`IComparable<DiceRoll>` + comparison operators via `IComparisonOperators`) ascending by `High` then `Low` — ascending canonical token. `All` is the SSOT enumeration of the 21 distinct rolls in that order (doubles included). JSON round-trips as the token via bundled `DiceRollJsonConverter`. `default` is non-meaningful (faces 0 — see Pitfalls); "no roll" is `DiceRoll?` null, per `IDecisionFilterData.Dice`. |
| `Move` | `readonly record struct (FrPt, ToPt)`. Encodes regular / bear-off / hit moves via the sign of `ToPt` — see "Move encoding" below. |
| `Play` | mutable `struct`, fixed 4-slot buffer of `Move`. Default value is empty (`Count == 0`). Intent-level construction via `Play.Create` — **five overloads**: four fixed-arity (`Create(m0)` … `Create(m0, m1, m2, m3)`), which construct at parity with the incremental `Add` spelling, and `Create(params ReadOnlySpan<Move>)` for moves already in a span or array (> 4 moves throws `ArgumentException`), which is also the `[CollectionBuilder]` target, so collection expressions build plays — `Play p = [new(13, 10), new(10, 8)];`, with `[]` the empty play, a forced pass. The span overload carries `[OverloadResolutionPriority(-1)]` so a literal argument list binds fixed-arity at every arity including one; see Benchmarks for what that buys. `Add`/`RemoveLast` remain the incremental build primitives for move-generation recursion; every construction path writes slots through one private seam. Read idiom is `foreach` (allocation-free pattern enumerator over a value copy; deliberately no `IEnumerable<T>` — it would box) or the indexer. **No equality** (`halheinrich/backgammon#273`, ruling A): `==`/`!=` are not defined, and `Equals`/`GetHashCode` throw `NotSupportedException` so every runtime route (comparers, hashed collections, `Distinct`, records and tuples holding a play) fails loudly. Play identity is `BoardState.IsSamePlay`, from a starting position — see "Play identity" below. `IsSameEncoding` compares exact encodings (order, hops, marks) for storage and round-trips; it is not identity. `ToNotation()` writes the play in standard notation, the one public way to spell a play (see "Play notation"); the internal `ToCanonical()` is the display form behind it. Serialized as a JSON array of `Move` via `PlayJsonConverter` (the private buffer fields are not visible to default property-based serialization); the raw move sequence round-trips exactly. |
| `BoardPosition` | `readonly struct` — an immutable position: the 26 checker counts of a board in `BoardState`'s frame, well-formed by construction (the invariant is stated once, in the type's `<remarks>`). The one definition of "the same position": `IEquatable<T>` and `==`/`!=` over all 26 counts, both bars included, with a consistent hash that is never identity. Creating, comparing and hashing allocate nothing. `default` is the empty board, which is well-formed, so the default is meaningful (`Empty`). See "BoardPosition" below. |
| `PlayChain` | **internal** `readonly record struct (FrPt, ToPt)` — one chain of a `CanonicalPlay`: a route from a source to a landing point, which the notation writes as one `from/to`, joining consecutive moves and eliding the touch-down points between. It stops where its moves stop or at a hit point whose mark it carries, so it is not a checker's whole trajectory: an intermediate hit splits one trajectory into two chains (`13/10*/8` is written `13/10* 10/8`). Same sign-encoding as `Move`, but may span several dice. A hit only ever sits at a chain's endpoint, and each hit point's mark on exactly one chain, its carrier (see "Canonical play form"). |
| `CanonicalPlay` | **internal** `readonly struct` (`halheinrich/backgammon#273`: consumers spell plays with `Play.ToNotation()` and compare them by position, so the chain form can change without breaking one), fixed 4-slot buffer of `PlayChain` + `Count`, read through `Count` and the indexer. The canonical chain form of a `Play` — its display form (which chains the notation shows, where each `*` goes), not its identity: like `Play` it has no equality (`==` undefined, `Equals`/`GetHashCode` throw). `ToString()` is the play's notation, the one formatter (see "Play notation"). Only produced by the internal `Play.ToCanonical()` — no other constructor path, so every instance is guaranteed canonical. `default` is the canonical form of the empty play (meaningful). |
| `PlayCandidate` | `Play`, `Depth?`, `DepthAbbreviation?`, `AnalysisMode`, `AnalysisLevel`, `Equity` (finite), `WinPct?`, `WinGammonPct?`, `WinBgPct?`, `LosePct?`, `LoseGammonPct?`, `LoseBgPct?`, and the derived `Notation` and `DepthRank`. `Play` is the one stored form of the candidate — applied, matched against the candidates with `BoardState.IndexOfSamePlay`, and displayed through `Notation`, which is `Play` written by the one formatter (`CanonicalPlay.ToString()`), `[JsonIgnore]`d and never stored (`halheinrich/backgammon#273`). The stored `MoveNotation` it replaced could disagree with its play; a document still carrying it reads on both paths with the member ignored, like any retired property. A candidate's loss against the best is the decision's (`CheckerPlayDecisionData.EquityLoss(i)`): it needs the other candidates. `EquityLoss(i) == 0` is the test for "is this a best play"; `BestPlayIndex` names the canonical single best, the first of the highest equity. |
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
  difference. One simplification followed: the port combined the hit marks
  of a `(n)` run, which the carrier rule makes dead — a hit point's mark is
  on the first chain ending there, so a run's mark is on its first chain or
  on none — and the formatter now reads it there. The differential run,
  repeated, still found no difference. BgMoveGen's copy is deleted in its
  own consumer leg.
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

These are pure derivations from `Points`. The pip rule is stated once
(`BoardState.OnRollPips` / `OpponentPips`, internal), and
`PositionData.OnRollPipCount` / `OpponentPipCount` read it from the
record's board: a record's pip counts are derived, never stored, so they
are the board's by construction. (They used to carry the producer's stored
values; the producer computed those from the same board, and the previous
wire golden's stated 130 / 145 were not its own board's counts — the drift
a stored copy allows.)

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
- **One rule, four doors, and one internal one.** `ApplyPlay` and
  `TryApplyPlay` commit what the rule computes; `IsSamePlay` and
  `IndexOfSamePlay` compare what it computes. None of them goes through
  `ApplyMove`, so the order a play's moves are written in never reaches the
  board. Written order was how the tester's play of
  `halheinrich/backgammon#273` corrupted it: the unmarked landing on the
  blot applied first. The internal `PositionAfter` is the fifth: from a
  `BoardPosition` value, the board `ApplyPlay` would leave, with no board
  built and nothing allocated, refusing an invalid play with `ApplyPlay`'s
  own message. It is how a record derives through the rule without holding a
  board.
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

**The one stored place of a decision's game and move number**
(halheinrich/backgammon#124). A standalone `.xgp` position belongs to no
game, so it has no game or move number — not a stamped 1, which the
converter used to write and every consumer then read as a real number. The
records store no copy: `BgDecisionData.Game` / `MoveNumber` and
`DecisionRow.Game` / `MoveNumber` are `int?`, derived from `Id` through the
internal `DecisionId.GameInFile` / `MoveInGame` — the `XgDecisionId`'s
numbers, or `null` for an `XgpDecisionId`. A number therefore cannot
disagree with the identifier, and a standalone position has nowhere to
carry one. The row's CSV writes an empty cell for each.

**The identifier binds the record two ways more** (`DecisionRules`, see
"The decision kinds"): an `XgDecisionId` names its kind in its canonical
form, so a record refuses one naming the other kind; and a standalone
position (an `XgpDecisionId`) states no `DescriptiveData.IsStandardStart`,
while a decision in a game does — the start is a fact about a game.

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

### Played cube actions on CubeDecisionData

Distinct from the scoring helpers below, `CubeDecisionData` carries the record
of what was *actually played* in a cube decision
(`halheinrich/backgammon#123` documents; the fields shipped with the played-
action record work):

- **`UserDoublerAction`** (`CubeAction?`) — the doubler action the player
  on roll played, `NoDouble` or `Double`. Null when the played action is
  not recorded.
- **`UserTakerAction`** (`CubeAction?`) — the taker action the opponent
  played, `Take` or `Pass`. Present only when a double was offered *and* a
  response recorded: in an undoubled game no taker decision exists, so
  this stays null even when `UserDoublerAction` is recorded.

Both halves are guarded on `init` to their own action domain, mirroring
`CubeDecisionPair`'s half-guards — a cross-half value throws
`ArgumentOutOfRangeException`. Cross-half *consistency* (a recorded taker
response implies the doubler doubled) is a producer contract, not guarded
here: init-only halves are set independently. The fields exist because the
played action is source data: it cannot be recovered from an error — a
zero error does not identify the action when the two cube equities tie.
It is the other way round: the error of a stated action is derived from
it (`UserDoubleError` is `DoublerActionError(UserDoublerAction)`,
`UserTakeError` is `TakerActionError(UserTakerAction)`), never stored. The
one cube error stored is the analyser's for a half whose action is not
stated (`UnstatedDoublerActionError`, `UnstatedTakerActionError`), which a
stated action refuses beside it. The actions serialize (they are wire
fields, unlike the computed members below).

These are **actions, never claims**: a stamped game fact cannot carry the
"too good" rationale, because a player can choose not to double in a
position where he is not too good — the rationale is a property of the
analysis, not of the played move (the `halheinrich/backgammon#86` intake's
"analysis vs. stamped action" distinction). The claim layer (`CubeClaim`,
`CubeClaimPair`, above) therefore neither supersedes nor touches this
pair: claims live in quiz answers and derived truth, played actions in
the game record. Do not infer a claim from a played action.

### Cube-decision scoring on CubeDecisionData

`CubeDecisionData` carries the cube-decision scoring policy as computed members
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

- **`CubeDecision.CanBeTooGood`** — the offerability fact of the same
  amendment, on the cube record because only the record sees money, Jacoby
  and cube owner together: `false` iff the session is money
  (`IDecisionFilterData.IsMoneyGame`, the contract's single spelling — never
  a restated `MatchLength == 0`), `IsJacoby == true`, and
  `Position.CubeOwner == Centered` (gammons do not count under Jacoby until
  the cube turns, so the no-double equity never exceeds the cash); `true`
  otherwise, including `IsJacoby == null` — an unknown rule is not a known
  Jacoby rule. The one derivation site: a consumer offering cube answers
  reads it to decide whether the Too Good pair is in the option set and
  never re-derives it from the rules fields. Independent of what the
  equities derive (the claim would still say Too Good if the producer's
  numbers did). `[JsonIgnore]`d like the rest of the record's derived view.

The computed members exist on the cube decision only — asking them of a
checker play does not compile, so the `IsCube` guard they used to share
(and its `InvalidOperationException`) is gone. The two error methods throw
`ArgumentOutOfRangeException` when the action argument is from the wrong
half (e.g. `Take` or `Pass` passed to `DoublerActionError`).

Tie-breaking follows the renderer's existing convention so a downstream
consumer that collapses the inline cube derivation into calls to these
helpers preserves behaviour: `NoDouble` on the doubler-equity tie, `Pass`
on `DoubleTakeEquity == 1`.

The four computed properties (`BestDoublerAction`, `BestTakerAction`,
`BestDoublerClaim`, `BestClaimPair`) carry `[JsonIgnore]`: they are a
derivation, not wire. The error methods are intrinsically not serialised
because they take parameters.

An aggregate verdict layer was removed in the cube-surface rebuild and is
slated to return later on a cleaner footing; the umbrella `INSTRUCTIONS.md`
Deferred section and git history carry that design.

### The decision kinds

**A decision is one of two types** (halheinrich/backgammon#273, Hal's ruling
of 2026-09-25): `CheckerPlayDecision` and `CubeDecision`, the two sealed
kinds of the abstract `BgDecisionData`. Each carries only its own members,
so code that reads one kind's member off the other does not compile, and no
member of either holds a value standing for "not applicable".

```
BgDecisionData (abstract)       Kind, Id, Xgid, Position, Descriptive; Match, Switch;
                                the IDecisionFilterData view
├── CheckerPlayDecision         Decision : CheckerPlayDecisionData
│                               Dice (canonical), AfterBestBoard, AfterPlayerBoard (derived)
└── CubeDecision                Decision : CubeDecisionData
                                CanBeTooGood
```

Design points a maintainer needs before touching it:

- **Where the kind lives: the record.** The kind is the record's type, not a
  category's, because every member only one kind has belongs to that kind's
  type, and some need the whole record: the after-boards need the position
  and the candidates, and `CanBeTooGood` needs money, Jacoby and cube owner.
  A polymorphic `Decision` category under one composite would have left
  those on the shared type, answering (or throwing) for the kind they do
  not apply to. Each kind keeps the category structure: `Decision` is its
  own category, typed per kind (`CheckerPlayDecisionData`,
  `CubeDecisionData`), beside the shared `PositionData` and
  `DescriptiveData`. The cube category's depth members lost their `Cube`
  prefix (`Depth`, `DepthAbbreviation`, `DepthRank`, `AnalysisMode`,
  `AnalysisLevel`), which only disambiguated them in the retired flat type.
- **A closed pair, matched exhaustively.** The base's one constructor is
  `private protected`, so these two kinds are the only ones. `Match<TResult>`
  and `Switch` take one branch per kind, so a consumer's match has no silent
  fall-through and a third kind would break every call at compile time — a
  `switch` over the `DecisionKind` enum cannot promise that (the compiler
  demands a discard arm), so the record's match is the door wherever the
  record is at hand. `DecisionKind` is the kind as a value: what `Kind`
  reports, what the row stores, what `IDecisionFilterData.Kind` filters on.
- **The kind on the wire** is a real member, `"Kind"`, written first
  whatever static type the value is serialized as, and read wherever it
  sits. `BgDecisionDataJsonConverter` finds it, refuses a document without
  exactly one known kind, and hands the whole document to that kind's
  generated contract; it names no member of either kind. Every refusal —
  a missing, duplicated or unknown kind, a member of the other kind (each
  kind and each kind's category disallows unmapped members), a missing
  member, a construction rule broken — is a `JsonException` on both paths.
  Why not `[JsonPolymorphic]` (measured on .NET 10) is stated on the
  converter's doc comment. A kind read directly by its own type bypasses
  the converter and refuses exactly as it does there (next item).
- **A construction rule a document breaks is a `JsonException`, whatever it
  is read as** — a record as `BgDecisionData` or as its kind, or a category
  on its own — carrying the init guard's exception as its inner one; code
  breaking the same rule gets the guard's exception itself. This closed the
  converter's one stated limit (a kind read directly used to surface the
  guard's `ArgumentException`). The mechanism, stated once on
  `BgDataTypesJsonContext`: each type holding its members to a rule
  (`CheckerPlayDecision`, `CubeDecision`, `CheckerPlayDecisionData`,
  `CubeDecisionData`) has an internal `[JsonConstructor]` beside the public
  parameterless one code uses. It binds one wire member (a kind's binds
  `Kind`, which the base then holds to the type), marks the instance as
  read, and runs before any init setter on both paths; each guarded setter
  rethrows its `ArgumentException` through `DocumentRefusal` when the
  instance is read. Measured on .NET 10 before relying on it: a generated
  context sets init and `required` members in an object initializer
  *before* `IJsonOnDeserializing` runs, so a callback flag cannot work, and
  a full-member serializer constructor would need `[SetsRequiredMembers]`,
  which silently turns off the JSON-required meaning of `required`. The
  converter no longer catches anything; it only dispatches.
  `PlayJsonConverter` refuses a play of more than four moves itself — it
  used to let `Play.Add`'s `InvalidOperationException` escape.
- **The members agree by construction.** Each init setter checks its value
  against the members already set and against the kind, which the base's
  constructor fixes before any member is set, so every rule is
  order-independent and needs no sentinel. The rules are stated once, on
  `DecisionRules`, and refused with an `ArgumentException` naming the
  member that completed the contradiction:
  - **Crawford** (`halheinrich/backgammon#201`): a cube decision's
    `Position` refuses a Crawford position. With the kind fixed by the type
    it is one guard; the two-half cross-check and `CrawfordRule` are gone.
    `ProblemKey`'s grammar still accepts a Crawford cube *key*: v3 stats
    documents written before the guard hold such keys and must keep loading
    (SPEC-stats-identity.md §1 keeps the Crawford flag in identity); the
    keys are inert — stats are looked up per pooled problem, never walked
    from the document.
  - **The identifier's kind**: an `XgDecisionId` names its kind in its
    canonical form (`:cube` / `:play`), so `Id` refuses one naming the
    other kind. An `XgpDecisionId` names none.
  - **A standalone position's start** (`halheinrich/backgammon#124`):
    `DescriptiveData.IsStandardStart` is `null` exactly for an
    `XgpDecisionId`; whichever of `Id` and `Descriptive` is set second
    refuses a mismatch.
  - **Every candidate valid from the position** — see "After-boards".
  An explicit null for `Id`, `Position`, `Descriptive` or a kind's
  `Decision` is an `ArgumentNullException` at init
  (`halheinrich/backgammon#221`, extended to every member a guard reads);
  an absent one is a compile error in an initializer and a `JsonException`
  on the wire.
- **A checker play's category is well-formed on its own.** Its roll is two
  faces 1–6 (the `[0, 0]` a cube used to carry is refused); it has at least
  one candidate, each of finite equity; `BestPlayIndex` is derived — the
  first candidate of the highest equity — so `BestPlay` always exists; and
  `UserPlayIndex` identifies one or is `null` — "none", never -1
  (`UserPlay` is that candidate). An `UnlistedPlayError` is stated only
  with no `UserPlayIndex`. The checks are order-independent the same way,
  and the two collections are copied on init, so a caller keeping its own
  list cannot change them afterwards.
- **The view.** The members every kind has are forwarded publicly on the
  base; `AnalysisMode` / `AnalysisLevel` and `FilterError` derive per kind
  through `Match` (a checker play's best candidate, which always exists now,
  so the old `Unknown`-when-no-candidate fallback is gone; a cube's
  analysis; `UserPlayError`, or `UserDoubleError ?? UserTakeError`, each
  derived where the record determines it). The
  checker play's own filter members — `Dice` and the after-boards — are
  explicit interface implementations on the base, `null` for a cube, so
  they are on neither the base's nor the cube's surface; `CheckerPlayDecision`
  exposes them publicly and non-null (`AfterPlayerBoard` aside). `Game` and
  `MoveNumber` derive from `Id` (see "DecisionId"). The whole view carries
  `[JsonIgnore]` (`halheinrich/backgammon#14`): the stored members are the
  wire, so a record's top level is exactly `Kind`, `Id`, `Xgid`,
  `Position`, `Descriptive`, `Decision`, pinned by test for each kind.
- **`CanBeTooGood` lives on `CubeDecision`**, the Too Good offerability of
  SPEC-scoring §3's 2026-09-02 amendment (`halheinrich/backgammon#187`) —
  see "Cube-decision scoring on CubeDecisionData". Only the record sees
  money (`Descriptive`), Jacoby and cube owner (`Position`) together, and
  only a cube decision has the question; the claim itself stays on
  `CubeDecisionData`, derived from equities alone.

### After-boards (derived)

Two boards a checker play leaves: `AfterBestBoard` (after the best
candidate) and `AfterPlayerBoard` (after the user's). **Derived, never
stored** — the arc's rule, no stored copy of a derivable value: the
position the candidate's play reaches from the record's `Position.Mop`,
through `BoardState`'s one play rule (the internal `PositionAfter`), in the
frame `ApplyPlay` leaves. **Frame: the next mover's**: the opponent is on
roll, so the decision-maker's checkers are *negative* and the opponent's
positive. The best play's board always exists; the user's is `null` exactly
when the user's play is not among the candidates. They exist only on a
checker play: a cube decision has no such member, and reads `null` through
`IDecisionFilterData`.

**Every candidate is valid from the decision's position** — a record
holding a play that cannot be played from its own position is corrupt (Hal's
ruling on invalid plays, `halheinrich/backgammon#273`, applied to stored
candidates). The invariant is stated once, beside the play rule it relies
on, on `BoardState.IsSamePlay`. Whichever of `Position` and `Decision` is
set second checks every candidate — not only the best and the user's — and
refuses the first invalid one with an `ArgumentException` naming the
candidate and the fault; read as a `BgDecisionData`, the document is a
`JsonException` on both paths. So the derivation can never fail on a record
that exists.

**Cost.** The same pass computes the two boards, once per record, while the
record is built; reading them allocates nothing (pinned by test), which
matters because the filter reads after-boards over many records.

They are off the record's wire. The row carries them as columns taken from
this derivation (see "DecisionRow"). This is the substrate for
`XgFilter_Lib`'s three-board `IPlayTypeClassifier` contract. The stored
boards the previous shape carried were computed by the converter's own copy
of the play rule (`AfterBoardBuilder`); the derived ones are byte-identical
on the golden record, and whether every candidate in real XG files satisfies
the validity invariant is measured by the converter's consumer leg, against
XG's own after-positions.

### DecisionRow

Flat CSV/JSON export: one row per decision of either kind, one column per
field. **A projection of the record, built one way**: `DecisionRow.From`
takes every column from a `BgDecisionData` — the shared ones through its
`IDecisionFilterData` view, the kind's own from the kind — and the
constructor is internal, so outside this library a row comes from `From`
or from JSON. The after-board columns are the record's derivation, never
computed a second way, and a row and its record cannot disagree on
anything they share (the converter's row/record agreement test becomes
structural). Carries its own CSV methods (`ToCsvLine`, `CsvHeader`, private
`CsvEscape`).

**It carries the kind** (`Kind`, a required column, written first), and the
other kind's columns are empty, never zero: a cube row's `Roll` and
after-boards are `null` — empty CSV cells. `Error` is the record's
`FilterError`, `null` when no user decision is recorded (the 0.0 the
producer wrote was a stand-in). `Game`, `MoveNumber` derive from `Id`, and
`IsStandardStart` is `null` for a standalone position
(`halheinrich/backgammon#124`). `Roll` is the checker play's dice in rolled
order as a two-digit integer; `Dice` is its canonical form, `[JsonIgnore]`d
like `MatchScore`.

**Read back whole.** A row read from JSON is held, once every column is
read (`IJsonOnDeserialized`), to what a projection guarantees: the kind's
columns present and the other kind's empty, the roll two die faces, a
checker row's best after-board present, and `DecisionRules` (Crawford, the
identifier's kind, a standalone position's start). A document breaking one
is a `JsonException` on both paths. The init-guard sentinels the row once
needed (`Roll`'s not-yet-stated backing field) are gone: no initializer can
build a row outside this library, so the whole check runs once, over a
complete row. The retired row shape (no kind, a 0 roll as the cube) is
refused whole.

`Board` is a required `BoardPosition` in the frame of `PositionData.Mop`.
The three boards and `AnalysisMode` / `AnalysisLevel` serialize to JSON but
are **excluded from CSV**; `AnalysisDepth` remains the CSV depth column.
The CSV header is
`Xgid,Error,MatchScore,MatchLength,Player,SourceFile,Game,MoveNumber,Kind,Roll,AnalysisDepth,Equity`:
the `Kind` column is new with the kinds, beside the roll it governs.

**Culture-invariant.** `ToCsvLine` writes every number with the invariant
culture, whatever the ambient one — `Error` and `Equity` as `G6` with a
decimal point and an ASCII minus, the integers as plain digits — and
`MatchScore` spells its away scores the same way. A comma-decimal culture
would otherwise split a cell in two. Pinned under `de-DE` and `sv-SE`
(`DecisionRow_ToCsvLine_IsCultureInvariant`).

`IsJacoby` (`bool?`) is stored, not derived — the tri-state fact
`PositionData.IsJacoby` owns, carried here because the CSV shape spells it
(`halheinrich/backgammon#121`). It reaches CSV the way `IsCrawford` does:
through the computed `MatchScore` token, as an in-grammar suffix on the money
score. A money row is `moneyJ` or `moneyNJ`; a money row whose rule is unknown
(`IsJacoby` `null`) is the bare `money`, which is deliberately neither
rule-bearing token. No column is added for it. Like `IsCrawford`, it also
serializes to JSON.

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

### The test-support project

**Why it exists.** Every record states every member (see "Absence on the
wire") and holds rules its members must agree on (see "The decision
kinds"), so a test that builds one by hand restates the records'
construction — and every consumer's tests did, each with a builder of its
own. The producer owns that knowledge, so the producer ships the builders:
`TestRecords`, in `BgDataTypes_Lib.TestSupport`, is the one way tests build
records, here and in every consumer (each consumer's leg moves its tests
onto it). This suite uses it too, so the builders are exercised against the
real records and a change to a record's construction breaks one place.

**Posture** — the BgUiPrimitives_Razor test-support project is the
precedent, and all three of its properties are mirrored:

- **Not packable.** Consumers reach it by `ProjectReference` from their test
  projects.
- **References only the library** — no other project and no package; in
  particular no test framework, so a consumer's test project keeps its own.
  The library grants it no internals (`InternalsVisibleTo` names this suite
  only), so a builder can build nothing a producer cannot.
- **Production code cannot use it.** The assembly declares
  `[UnsupportedOSPlatform("browser")]`: every project that can reach a
  browser publish declares the browser platform, and there a use of this
  assembly is CA1416, an error under `TreatWarningsAsErrors`. It declares no
  `IsTrimmable`. Stated limit, as in the precedent: a product that declares
  no browser platform (a plain class library) is not stopped by the
  declaration; this repository's half is that the library references no
  project at all.

`TestSupportPostureTests` pins each of these, reading the two project files
staged beside the test assembly.

**The builders.** `CheckerPlay`, `Cube` and `Row` (the projection of a
record), and a builder per category — `Position`, `CheckerPlayData`,
`CubeData`, `Descriptive`, `Candidate`. Each yields a well-formed value of
its type with realistic defaults, and takes the members a test cares about
as named arguments spelled as the members are: a checker play is the
opening 3-1 from the standard start at 0-0 in a 7-point match, with three
candidates (8/5 6/5 best and played); a cube decision is a double/take in a
short race; both sit in a game of an `.xg` match. A record builder's
defaults follow its arguments only where the records' rules demand it — a
standalone id gets no `IsStandardStart`, and a board other than the
standard start gets one candidate, the pass (valid from every position),
unless the test states its own decision — and `TestRecordsTests` pins
them. A test whose subject is construction itself writes its own object
initializer.

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
    DecisionKind Kind { get; }                    // CheckerPlay or Cube (replaces IsCube)
    string? Player { get; }                         // null when no name was recorded
    int OnRollNeeds { get; }
    int OpponentNeeds { get; }
    bool IsCrawford { get; }
    int MatchLength { get; }
    bool IsMoneyGame => MatchLength == 0;         // the interface's only default implementation
    bool? IsJacoby { get; }                       // tri-state; null on a money record matches neither money token
    int? MoveNumber { get; }                      // 1-based within the game; null for a standalone position
    bool? IsStandardStart { get; }                // false for non-standard openings; null for a standalone position
    AnalysisMode AnalysisMode { get; }            // cube analysis for cubes, best-play candidate for checkers
    AnalysisLevel AnalysisLevel { get; }          // level axis of the same analysis AnalysisMode reports
    double? FilterError { get; }                  // ≥ 0, or null when no user decision is recorded
    BoardPosition Board { get; }                  // on-roll frame, see Mop layout

    // The checker play's own members: null for a cube decision.
    DiceRoll? Dice { get; }                       // canonical roll; never null for a checker play
    BoardPosition? AfterBestBoard { get; }        // next mover's frame; never null for a checker play
    BoardPosition? AfterPlayerBoard { get; }      // next mover's frame; null when the user's play is not a candidate
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
// A decision is one of two types (halheinrich/backgammon#273): a closed pair.
public enum DecisionKind { CheckerPlay, Cube }    // the kind as a value; strict string token

[JsonConverter(typeof(BgDecisionDataJsonConverter))]  // dispatches on "Kind"; names no member
public abstract class BgDecisionData : IDecisionFilterData
{
    private protected BgDecisionData(DecisionKind kind);   // the two kinds' only way in

    [JsonInclude, JsonRequired] public DecisionKind Kind { get; internal init; }  // written first; the type's
    public required DecisionId      Id          { get; init; }   // refuses an XgDecisionId naming the other kind
    public required string          Xgid        { get; init; }
    public required PositionData    Position    { get; init; }   // a cube refuses a Crawford position
    public required DescriptiveData Descriptive { get; init; }   // IsStandardStart null exactly for an XgpDecisionId
    // Explicit null for Id, Position, Descriptive → ArgumentNullException (halheinrich/backgammon#221).

    public abstract TResult Match<TResult>(Func<CheckerPlayDecision, TResult> checkerPlay,
                                           Func<CubeDecision, TResult> cube);
    public abstract void Switch(Action<CheckerPlayDecision> checkerPlay, Action<CubeDecision> cube);

    [JsonIgnore] public int? Game { get; }        // from Id; null for a standalone position
    [JsonIgnore] public string SourceFile { get; } // Id.Filename
    // IDecisionFilterData: the shared members public and [JsonIgnore]d; Dice
    // and the after-boards explicit (null for a cube).
}

public sealed class CheckerPlayDecision : BgDecisionData
{
    public required CheckerPlayDecisionData Decision { get; init; }   // every candidate valid from Position
    [JsonIgnore] public DiceRoll Dice { get; }                          // canonical form of Decision.Dice
    [JsonIgnore] public BoardPosition  AfterBestBoard   { get; }        // derived once; next mover's frame
    [JsonIgnore] public BoardPosition? AfterPlayerBoard { get; }        // null when UserPlayIndex is null
}

public sealed class CubeDecision : BgDecisionData
{
    public required CubeDecisionData Decision { get; init; }
    // Offerability of the Too Good verdict (SPEC-scoring §3, 2026-09-02):
    // false iff money && IsJacoby == true && cube centred.
    [JsonIgnore] public bool CanBeTooGood { get; }
}

public sealed class CheckerPlayDecisionData    // unmapped members refused
{
    public required IReadOnlyList<int> Dice { get; init; }            // two faces 1–6, rolled order; copied
    public required IReadOnlyList<PlayCandidate> Plays { get; init; } // never empty; copied
    public int? UserPlayIndex { get; init; }                          // a candidate, or null — never -1
    public double? UnlistedPlayError { get; init; }                   // only with no UserPlayIndex
    [JsonIgnore] public int            BestPlayIndex { get; }         // derived: first of the highest equity
    [JsonIgnore] public PlayCandidate  BestPlay { get; }
    [JsonIgnore] public PlayCandidate? UserPlay { get; }
    [JsonIgnore] public double?        UserPlayError { get; }         // EquityLoss(UserPlayIndex) ?? UnlistedPlayError
    public double EquityLoss(int index);                              // BestPlay.Equity − Plays[index].Equity, ≥ 0
}

public sealed class CubeDecisionData           // unmapped members refused
{
    // Required: Depth, DepthAbbreviation, AnalysisMode, AnalysisLevel,
    // NoDoubleEquity, DoubleTakeEquity, the cubeless equities, the twelve
    // probabilities, ProbOfOpponentErrorJustifyingDouble.
    [JsonIgnore] public int? DepthRank { get; }          // derived from the mode and level; null = not recorded

    // Played cube actions — game facts, guarded per half on init (see
    // "Played cube actions on CubeDecisionData"); serialized; null = not
    // recorded.
    public CubeAction? UserDoublerAction { get; init; }  // NoDouble/Double; wrong half throws
    public CubeAction? UserTakerAction   { get; init; }  // Take/Pass; null in undoubled games

    // The user's errors: a stated action's is derived; the analyser's for an
    // unstated action is the one stored, refused beside a stated action.
    public double? UnstatedDoublerActionError { get; init; }
    public double? UnstatedTakerActionError { get; init; }
    [JsonIgnore] public double? UserDoubleError { get; }  // DoublerActionError(UserDoublerAction) ?? unstated
    [JsonIgnore] public double? UserTakeError { get; }    // TakerActionError(UserTakerAction) ?? unstated

    // Cube-decision scoring (computed; a cube decision's only).
    [JsonIgnore] public CubeAction  BestDoublerAction { get; }   // Double or NoDouble
    [JsonIgnore] public CubeAction  BestTakerAction   { get; }   // Take or Pass

    // Claim-layer truth derivation (SPEC-scoring §3, amended 2026-09-02).
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

public sealed class DecisionRow : IDecisionFilterData, IJsonOnDeserialized
{
    internal DecisionRow();                                   // [JsonConstructor]; no public way in
    public static DecisionRow From(BgDecisionData record);    // the projection: every column from the record

    public required DecisionKind Kind { get; init; }          // written first
    public required DecisionId Id { get; init; }
    public double? Error { get; init; }                       // the record's FilterError
    public bool? IsStandardStart { get; init; }               // null for a standalone position
    public int? Roll { get; init; }                           // rolled dice, e.g. 13; null for a cube
    public required BoardPosition Board { get; init; }        // on-roll frame
    public BoardPosition? AfterBestBoard { get; init; }       // the record's derivation; null for a cube
    public BoardPosition? AfterPlayerBoard { get; init; }     //   and when the user's play is not a candidate
    // Other flat columns required but IsJacoby? — see DecisionRow.cs.
    [JsonIgnore] public int? Game { get; }                    // from Id, as MoveNumber
    [JsonIgnore] public string SourceFile { get; }            // Id.Filename; a CSV column
    [JsonIgnore] public DiceRoll? Dice { get; }               // canonical Roll
    [JsonIgnore] public string MatchScore { get; }            // computed from needs/Crawford/length/Jacoby
    public static string CsvHeader { get; }                   // …,Game,MoveNumber,Kind,Roll,AnalysisDepth,Equity
    public string ToCsvLine();                                // null → empty cell; numbers invariant-culture
    // Read from JSON, a row is checked whole (OnDeserialized): the kind's
    // columns present and the other's empty, the roll two faces, and
    // DecisionRules; a breach is a JsonException. The three boards and
    // AnalysisMode / AnalysisLevel serialize to JSON but not to CSV; Id is
    // JSON-only too.
}

public class PositionData    { /* required init-only properties per the categories table; Mop is a BoardPosition; IsJacoby? */
                               [JsonIgnore] public int OnRollPipCount { get; }    /* Mop's, by BoardState's pip rule */
                               [JsonIgnore] public int OpponentPipCount { get; } }
public class DescriptiveData { /* required init-only properties per the categories table; Title?, Date?, Event?, IsStandardStart? */ }
public class PlayCandidate   { /* required init-only properties per Architecture table; Equity finite; the six probabilities nullable */
                               [JsonIgnore] public string Notation { get; }  /* Play.ToNotation(); never stored */
                               [JsonIgnore] public int? DepthRank { get; }   /* from AnalysisMode × AnalysisLevel; null = not recorded */ }

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
number array; a malformed board is a `JsonException`; an optional board is
a `BoardPosition?`, `null` or that form); `DecisionKind` bundles the strict
enum converter and `BgDecisionData` bundles `BgDecisionDataJsonConverter`
(see "The decision kinds").
Tested without any options-level registration in
`BgDecisionDataSerializationTests`, `DecisionRowSerializationTests`,
`DecisionKindTests`, `DiceRollTests`, `ProblemKeyTests`, and
`BoardWireTests`; `DocumentRefusalTests` reads every rule's breach as each
type that can hold it, on both paths. The bytes of a full record and row of each kind are
pinned by `WireGoldenTests`, beside the retired shapes it refuses; absence
is walked member by member by `WireAbsenceTests`.
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
  (`ConvertXgToJson_Lib`'s `Build*` sites) state every member of the kind
  they build; tests build through `TestRecords`. `Id` was the first such member — the
  "producer-supplied identity" contract, no silent default IDs. Do not add
  a member with an initializer instead: the source-generated context drops
  initializers for absent members, which is the divergence the rule closed,
  and `WireAbsenceTests` fails a member that is neither kind.
- **A Crawford cube cannot be built** (`halheinrich/backgammon#201`). A
  `CubeDecision`'s `Position` refuses a Crawford position with
  `ArgumentException`, from an initializer in any member order; read from
  JSON it is a `JsonException`; a row holding one is refused on
  read. So a fixture that wants the Crawford flag builds a Crawford *play*,
  and a test that needs a Crawford cube *key* (still in `ProblemKey`'s
  grammar, for old stats documents) builds it from the key string, never
  from a record.
- **Embed a record as `BgDecisionData`, not as its kind.** Every read
  refuses malformed input as a `JsonException` — as the base, as a kind, or
  as a category — but only the base reads either kind: a consumer's
  document embeds `BgDecisionData`, never a kind, and a collection of
  records is a consumer's document: its context declares it and chains this
  one.
- **Every candidate of a checker play is valid from its position.** A
  fixture that states a board and a decision states candidates valid from
  that board — a record holding any other cannot be built (the invariant is
  stated on `BoardState.IsSamePlay`). The builders' default candidates are
  the opening's; on another board, state the plays, or take the builder's
  pass.
- **`BgDecisionData.Id`, `Position`, `Descriptive` and each kind's
  `Decision` reject null at init; absent is not null**
  (`halheinrich/backgammon#221`, `halheinrich/backgammon#222`). An explicit
  null — from an initializer or a JSON `"Position":null` — throws
  `ArgumentNullException` naming the member (a `JsonException` carrying it
  when read as a `BgDecisionData`); a document that omits the member is a
  `JsonException` on both paths, refused as absent before any setter runs. There
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
- **After-boards are derived, and `null` only where they do not exist**
  (halheinrich/backgammon#15; the records leg of halheinrich/backgammon#273). A checker play's best
  board always exists; its player board is `null` exactly when the user's
  play is not among the candidates; a cube decision has neither (`null`
  through `IDecisionFilterData`, no member on the type). They are never on
  a record's wire, so no producer states them; the row carries them as
  taken from the record. The legacy `[]` is a malformed board now: the
  shape that wrote it is refused whole.
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
  threshold. `DepthRank` (a candidate's and the cube analysis's) remains the ordering surface for
  consumers comparing whole analyses across the mode × level pair. Do not
  strip a member's `[Description]` label: downstream label readers (XgFilter_Lib's
  `EnumLabel.ToLabel`) throw on a member without one.
- **`IDecisionFilterData.Dice` is null for cube decisions, and never
  malformed.** Null means "no dice apply" (a cube is offered before the roll
  — the `FilterError` null-when-inapplicable convention), never "data was
  bad": a checker play's roll is two faces 1–6 by construction
  (`CheckerPlayDecisionData.Dice`), and a row's `Roll` is refused on read
  unless it is two die faces. Both derivations are `[JsonIgnore]`d; the
  stored forms (`Roll`, `Decision.Dice`, rolled order) remain the wire.
- **`default(DiceRoll)` is non-meaningful.** A `record struct` cannot run
  its face validation on `default`, so `default(DiceRoll)` carries faces of
  0. "No roll" is modelled as `DiceRoll?` null, never as `default`. The
  standard value-type caveat, shared with `Play` and `CubeDecisionPair`.
- **A candidate's equity loss is the decision's, derived; `0.0` means no
  loss vs. best.** `CheckerPlayDecisionData.EquityLoss(i)` is
  `BestPlay.Equity − Plays[i].Equity`, never negative, because the best is
  derived as the first candidate of the highest equity. Identifying the
  best candidate uses `BestPlayIndex` (`BestPlay`); testing membership in
  the best-equity equivalence class uses `EquityLoss(i) == 0.0`. There is
  no per-candidate loss to read (it needs the other candidates) and none
  to state: a producer states equities.
- **The cube-scoring helpers are a cube decision's only.** All six (four
  computed properties — the action pair and the claim pair — plus two
  methods) live on `CubeDecisionData`, so asking them of a checker play
  does not compile; the `IsCube` guard they needed is gone. Callers in
  mixed-decision contexts match on the record (`Match` / `Switch`). The
  four computed properties carry `[JsonIgnore]`; do not strip those
  attributes.
- **Cube-scoring atomic-action methods reject the wrong half.**
  `DoublerActionError(CubeAction)` accepts only `Double` / `NoDouble`;
  `TakerActionError(CubeAction)` accepts only `Take` / `Pass`. The
  other half throws `ArgumentOutOfRangeException`.
- **`UserDoublerAction` / `UserTakerAction`: half-guarded on init,
  cross-half consistency is NOT guarded.** Each rejects the other half's
  actions with `ArgumentOutOfRangeException` at `init`, but "a recorded
  taker response implies the doubler doubled" is a producer contract —
  init-only halves are set independently, so nothing stops constructing
  `(NoDouble, Take)`. Null means "not recorded", not "declined": a null
  `UserTakerAction` alongside a recorded `NoDouble` is the normal
  undoubled-game state — no taker decision ever existed — not missing
  data. And they are *actions, never claims* —
  do not infer `CubeClaim` from a played action (see "Played cube actions
  on CubeDecisionData"); the rationale is a property of the analysis.
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
  `CubeDecisionData.BestDoublerClaim`.
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

None open. (The `DecisionRow` factory split closed with the records leg of
halheinrich/backgammon#273: a row is the projection `DecisionRow.From` of a
record, and tests build it through `TestRecords.Row`.)
