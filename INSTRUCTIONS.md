# BgDataTypes_Lib

> Collaboration contract: [`../AGENTS.md`](../AGENTS.md)
> Umbrella status & dependency graph: [`../INSTRUCTIONS.md`](../INSTRUCTIONS.md)
> Mission & principles: [`../VISION.md`](../VISION.md)
> Open work: the [tracker](https://github.com/halheinrich/backgammon/issues), label `BgDataTypes_Lib`.

## What it is for

The shared data layer every other in-tree library rests on: the decision
record a producer builds and every consumer reads, its flat row, the board
and play primitives, and the JSON wire contract they share. The charter, and
what stays out of it, is in the umbrella's `INSTRUCTIONS.md`.

## Stack

C# / .NET 10 / Class Library / xUnit / BenchmarkDotNet. Pure data types — no parsing, no rendering, no I/O beyond `System.Text.Json` serialization.

## Solution

`D:\Users\Hal\Documents\Visual Studio 2026\Projects\backgammon\BgDataTypes_Lib\BgDataTypes_Lib.slnx`

From this directory:

```bash
dotnet build BgDataTypes_Lib.slnx
```

```bash
dotnet test BgDataTypes_Lib.slnx
```

The benchmarks run on demand, never as part of the suite (see Benchmarks).

## Repo

https://github.com/halheinrich/BgDataTypes_Lib — branch `main`.

## Depends on

Atomic by design. BgDataTypes_Lib has no subproject dependencies and must
not gain any. The shared-data layer is the foundation other subprojects
rest on; introducing a subproject dependency here would either create a
circular reference or force the dependency on every consumer transitively.
`System.Text.Json` is the only runtime dependency, and consumers register
no converter on their `JsonSerializerOptions`: each serialized type bundles
its own (see "JSON converters and the serializer context" under Layout).

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
  `CubeDecisionData`; the position's session — the abstract `Session` and
  its two sealed kinds `MoneySession` and `MatchSession`, each composing
  its kind's terms (with `SessionKind`, the kind as a value,
  `SessionJsonConverter`, which dispatches on the kind the terms state,
  `Session.Create`, the one operation building a session from its header's
  terms, the game's standing and the seat on roll, a `Seat`, and the
  internal `SessionRules`, the statement of the session's rules; see
  "Money and match: the session kinds"); the game's session — the abstract
  `GameSession` and its two sealed kinds `MoneyGameSession` and
  `MatchGameSession`, each composing its kind's terms and standing as the
  header states them, with `GameSession.Create`, the one operation pairing
  the terms with the game's standing, which takes no seat and through which
  `Session.Create` pairs;
  `DecisionRules`, the internal statement of the rules
  that bind a decision's members, `FiniteNumber`, the internal statement
  of the rule every stored number keeps, `DocumentRefusal`, the internal one
  spelling of how a rule a document breaks is refused, and `XgidEncoder`,
  the internal derivation of a record's XGID. The ranking —
  `PlayRanking`, and `RankedPlays` of `RankedPlay`, what a checker play
  derives for one — the player's result, `PlayerResult` and
  `PlayerResultKind`, and the internal `DecisionView`, a record's filter
  view for one ranking. `DecisionRow` is the record's flat projection, one
  row per decision, with its JSON wire form.
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
- **Enums and the depth taxonomy** — `CubeOwner`, `CubeAction`,
  `CubeAnswer` (the four cube answers of SPEC-scoring §3, with
  `CubeAnswerExtensions` for its projections), `CubeClaim` (the three-way
  claim an answer reads as at a decision), and
  `AnalysisMode` × `AnalysisLevel` (the two-axis depth taxonomy) with `BookEdition`, and the internal `DepthTaxonomy` (the depth's rank, label and abbreviation, derived from its typed facts), alongside
  the small value types `CubeAnswerCost` (what a cube answer costs at a
  decision, in its two parts), and `DiceRoll` (a canonical unordered roll),
  and `EquityDisplay`, the one display of every equity figure and the zero
  rule of every cost and loss.
- **Shared consumer contracts** — `IDecisionFilterData`, the filter-layer
  view implemented by a record's view and by `DecisionRow`, carrying the
  score context a filter needs as the decision's `Session` (a money
  session with its Jacoby and beaver rules, cube limit and scores, or a
  match with its length, away scores and Crawford flag), and a cube
  record's two decisions: the doubler's (`Player`, `PlayerResult`) and,
  where a double was offered, the taker's (`Taker`, `TakerResult`;
  SPEC-filtering §8, halheinrich/backgammon#440);
  `IGameInfo` and `IMatchInfo`, implemented by producers so filter layers
  never reference a producer's concrete types, stating money versus match
  as the kind of their `GameStanding` and `SessionTerms`.
- **JSON converters and the serializer context** — the one list of which
  type bundles which converter. Each is a `[JsonConverter]` attribute on the
  type, so consumers register nothing; the attributes are the code's
  statement, and `BgDataTypesJsonContextTests` pins them, byte identity with
  the reflection path included. All are public: a downstream
  `JsonSerializerContext` whose documents embed an annotated type must
  instantiate its converter from generated code, so an internal converter
  fails that generator outright (SYSLIB1220).
  - *Every enum*: `StrictJsonStringEnumConverter<TEnum>`, `CubeAnswer` and
    `CubeClaim` included though no document embeds either. A token is the
    declared member name; a numeric ordinal is a `JsonException`; name
    matching on read is case-insensitive, the base converter's behaviour,
    which has no knob. The owner of the rule and its reason is the
    converter's doc.
  - *A token of its own*: `PlayJsonConverter` on `Play` (a JSON array of
    `Move`), `DiceRollJsonConverter` on `DiceRoll`,
    `DecisionIdJsonConverter` on `DecisionId`, `ProblemKeyJsonConverter` on
    `ProblemKey` (the only one with the property-name overloads, so it also
    works as a dictionary key), and `BoardPositionJsonConverter` on
    `BoardPosition` (the 26 counts as a number array; a malformed board is a
    `JsonException`; a `BoardPosition?` is `null` or that form).
  - *Dispatching*: `BgDecisionDataJsonConverter` on `BgDecisionData`,
    `SessionJsonConverter` on `Session`, `SessionTermsJsonConverter` on
    `SessionTerms` and `GameStandingJsonConverter` on `GameStanding` find a
    document's kind and delegate the document to that kind's generated
    contract. How a kind is found, and a document without exactly one known
    kind refused, is stated once, on the internal `KindDispatch`; each
    supplies its mapping from kind to contract and nothing else.
  - *Downstream*: `NamedCollectionJsonConverter<TValue, TSelf>` is abstract;
    a specialization bundles its own closed form, in its own repo (see
    "Named documents").

  `BgDataTypesJsonContext` is the source-generated context over the whole
  wire surface — see "Source generation & trimming" below. Round trips with
  no options-level registration are tested in
  `BgDecisionDataSerializationTests`, `DecisionRowSerializationTests`,
  `DecisionKindTests`, `SessionKindTests`, `HeaderWireTests`, `DiceRollTests`,
  `ProblemKeyTests` and `BoardWireTests`; `DocumentRefusalTests` reads every
  rule's breach as each type that can hold it, on both paths;
  `WireGoldenTests` pins the bytes of a full record and row of each kind,
  beside the retired shapes it refuses; `WireAbsenceTests` walks absence
  member by member.
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

**`BgDataTypes_Lib.TestSupport/`** — what consumers' tests share, shipped
for test projects to reference, this repository's and every consumer's:
`TestRecords`, the record builders; `AllocationProbe`, the allocation
pins' one measurement; and the test loggers, `RecordingLogger<T>` (with its
`LogEntry`) and `ThrowingLogger<T>`. Not a product and not a test project; see "The
test-support project" under Architecture.

**`BgDataTypes_Lib.Tests/`** — xUnit, one test class per type or per
behaviour area of a type (`ProblemKeyTests`, `BoardStateTests`,
`PipCountTests`, `RaceTests`, the `*SerializationTests` pair, …). Fixtures
are constructed in code — pure data types need no corpus, so no test reads
the umbrella `TestData/` (the evidence for the cube rules and its owners are
under "Cube answers and their costs").

Every pin that says a path allocates nothing measures through
`AllocationProbe`, in the test-support project, so this suite's pins and
every consumer's measure one way: a warm-up of a whole window's calls, then
the fewest bytes any of several measured windows allocated on the test
thread, stopping at the first that allocates nothing. A one-off allocation
the runtime makes there lands in one window at most, so it cannot fail a
pin, while a path that allocates on its calls allocates in every window and
still does; `AllocationProbeTests`, in this suite, pins both halves. (A
single measured loop failed once on an allocation that was not the path's —
the rider of halheinrich/backgammon#273's match-context leg.)

## Architecture

Composite and category types are `class` with `init`-only properties; the
move primitives `Move` (`readonly record struct`) and `Play` (mutable
`struct`) are value types for hot-path zero-alloc reasons inherited from
their move-generation origins. `BoardState` is a `class` but mutable —
the one deliberate exception (see "Mutability exception" below).
Serialization uses `System.Text.Json`, with each converter bundled on its
type (see "JSON converters and the serializer context" under Layout).

### Source generation & trimming

`BgDataTypesJsonContext` (halheinrich/backgammon#129 leg 1) is the public
source-generated `JsonSerializerContext` over this library's wire surface:
trim-safe serializer metadata produced at compile time, byte-identical to
the reflection path (pinned by `BgDataTypesJsonContextTests`), every
bundled converter honored. Its `[JsonSerializable]` roots are the wire
units — the document roots (`BgDecisionData`, `DecisionRow`), the two
header types a producer's serialized header embeds (`SessionTerms`,
`GameStanding`), and the converter-bearing token types (`Play`,
`DecisionId`, `ProblemKey`, `DiceRoll`, `BoardPosition`, every enum a
document or a producer's header holds); composite parts ride
the generator's graph walk. `CubeAnswer` and `CubeClaim` are not roots: no
document in the umbrella embeds either, so neither is wire surface. Each
still bundles the strict converter, so the first document that holds one
reaches it through the graph walk with its token already strict;
`Context_HoldsNeitherTheCubeAnswerNorTheClaim` fails at that moment, which
is when to declare it. Converters stop that walk, so what lies
past them is declared explicitly and resolved through the active options
at runtime: `Move` (past `Play`'s converter), the two decision kinds
`CheckerPlayDecision` and `CubeDecision` (past
`BgDecisionDataJsonConverter`, which reads `DecisionKind` to choose), and,
each read by `SessionKind`, the two kinds of session (`MoneySession`,
`MatchSession`), of terms (`MoneyTerms`, `MatchTerms`) and of standing
(`MoneyStanding`, `MatchStanding`), past their families' converters. A completeness test (the halheinrich/backgammon#144
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
- **Why the paths agree.** The generated creator for every init-only type
  in the wire graph is one object initializer over an argument array
  (`ObjectWithParameterizedConstructorCreator`), and an absent member
  arrives there as `default(T)` where the reflection path keeps an
  initializer (measured 2026-09-14: `{"Id":"x"}` read with null halves
  through the context). halheinrich/backgammon#222 left that nothing to
  act on: a required member has no initializer and is refused absent on
  both paths before the creator runs, and a nullable one defaults to `null`
  on both.
- **The classification is the members' own.** Each nullable member's doc
  says what `null` means, and `WireAbsenceTests` walks every member of every
  type of the wire graph, taken from the context's own metadata through each
  kind's contract and each session kind's, and pins both halves of the rule
  on both paths, plus that every member is exactly one kind. Its
  completeness check, `TheWalk_ReachesEveryTypeOfTheGraph`, fails when the
  walk stops reaching a type of the graph or reaches one it does not list,
  so a member added to any type the walk reaches is a new case without an
  edit. Three shapes are worth knowing:
  - The one member both required and nullable is
    `DecisionRow.TakerResultKind` (see "DecisionRow"): its `null` means no
    double was offered, so a document must state it; the walk names it as
    the one exception, so a second fails.
  - Each record's, each set of terms' and each standing's `Kind` is
    required through `[JsonRequired]`, since the type states it and code
    never does, and so is each of a session's members: their setters are
    internal, since code outside the library builds a session through
    `Session.Create` alone.
  - A session's facts are all required: a money session's Jacoby rule has
    no "unknown".

  A nullable member's absence reads exactly as its explicit `null`: as
  `null`, or refused when `null` breaks a rule of the decision's kind (a
  checker row states its roll).
- **An explicit `null` for a member that is not nullable is a
  `JsonException`**, on both paths and whatever the caller's options. A
  value-type member's `null` the serializer refuses itself; a reference
  member's the type refuses — its init guard's `ArgumentNullException`,
  which a document gets as a `JsonException` (see "The decision kinds" for
  how a type tells a document from code). Nothing here depends on
  `RespectNullableAnnotations`, and the context does not set it (what was
  measured, on .NET 10, is on the context's doc). `WireAbsenceTests` sets
  `null` on every member that is not nullable, on both paths, with the
  option off and on.
- **`Unknown` is a value, not an absence.** `AnalysisMode`/`AnalysisLevel`
  (on candidates, cube analyses and rows) are required: "not recorded" is
  spelled `Unknown` by the producer. Making them nullable would give "not
  recorded" two spellings. A document lacking the pair — JSON written
  before the pair existed — is refused rather than read as `Unknown`. The
  2026-09-25 survey found no committed document of the
  `BgDecisionData`/`DecisionRow` shape in any member (BgQuiz's e2e fixtures
  are `.xgp` files, converted at run time), and none in the local
  `TestData` (its `BgDecisionData/` corpus is a different wrapper shape,
  generated output that no test here reads).
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
`Decision` category (see "The decision kinds"). This is the map; each type's
members and rules are on its XML doc.

| Type | Held by | What it holds |
|---|---|---|
| `PositionData` | both kinds | the board (`Mop`), the cube and the `Session` at the decision; the pip counts, derived from the board |
| `DescriptiveData` | both kinds | the source's descriptive facts: the names, title, date, event, standard start, comment and flag. The game, the move number and the source file are the `Id`'s, and a match's length is the session's |
| `CheckerPlayDecisionData` | `CheckerPlayDecision` | the roll, the candidates and which the user played, or the analyser's error for a play off the list; everything best or error is a ranking's (see "The ranking") |
| `CubeDecisionData` | `CubeDecision` | the analysis's depth facts, equities and probabilities, and `Recorded`, the recorded cube actions as one value; the best actions, their errors and the truth among the four answers are derived (see "Cube answers and their costs") |
| `MoneySession`, `MatchSession` | `PositionData.Session` | the terms, and the game's standing from the player on roll's side (see "Money and match: the session kinds") |

**No stored copy of a derivable value** (the umbrella's verdict on the
records leg of `halheinrich/backgammon#273`). Every member the others
determine is derived from them and never stored, so it cannot disagree
with them — the pip counts, the source file, each depth rank, and under a
ranking the best play, each candidate's error and the player's error; a
stated cube action's error. A derived member is `[JsonIgnore]`d; a
document still stating one reads with it ignored, even in the categories
that refuse unknown members — the serializer knows the member and skips
its JSON (measured on .NET 10, both paths) — and the derivation stands. A
member retired outright is refused like any unknown member where the
category refuses them; nothing outside the repo reads the exported JSON, so
wire changes need no compatibility layer (the umbrella's third-round
ruling). The audit, member by member, and what stays stored and why, is
under "Stored or derived" below.

**None recorded is `null`** (the same verdict). "None recorded" has one
spelling, for text as for numbers: `null`. A text member states text or,
where it may, nothing — empty or white-space text is refused, by code with
an `ArgumentException` naming the member and by a document with a
`JsonException` (the one statement of the rule is the internal
`StatedText`). The names, the title, the event and the comment on
`DescriptiveData`, a candidate's and the cube analysis's depth label and
abbreviation (derived, `null` where no depth is recorded), and the
row's `Player` and `AnalysisDepth` are nullable; a
member that is never none — the row's XGID column, the id's file name — is
refused empty the same way (the record's XGID is derived, never stated).
`DepthRank` is `int?`: `null` where the depth is not recorded (the mode, or
an evaluation's level, `Unknown`). `Date` is `DateOnly?`. The
analysis enums keep `Unknown`, a member the producer states.
`IDecisionFilterData.Player` is `string?` accordingly.

**Every stored number is finite** (the umbrella's fourth-round ruling on
the records leg). Each stored floating-point value in the records — the
candidate's equity and probabilities, the cube's equities and
probabilities, and the analyser's stored errors (`UnlistedPlayError`, `UnstatedDoublerActionError`,
`UnstatedTakerActionError`) — is a finite number, or `null` where a
nullable one records none. A NaN or an infinity is refused by code with an
`ArgumentOutOfRangeException` naming the member, and by a document with a
`JsonException` carrying it, on both paths (where the reader's options
admit a named non-finite literal at all; by default the token itself is
refused). The one statement of the rule is the internal `FiniteNumber`.
Range is not checked: a
probability's bounds are not measured against real data. A test finds the
stored numbers by reflection, so one added without the rule fails it.
`DecisionRow`, a projection, carries the record's numbers and is not
checked again.

**A decision position has a checker of each side**
(`halheinrich/backgammon#273`, Hal's ruling of 2026-09-27).
`PositionData.Mop` holds at least one of the player on roll's checkers and
one of the opponent's, each on a point or on the bar. A side with none has
borne off all fifteen, so the game is over and no decision is made; the
empty board is the case where neither has one. The rule is the category's,
stated once on `PositionData` (its remarks, and the public
`IsDecisionPosition` with the internal `BoardMessage`, which its guard and
the row's read-back share), so both decision kinds inherit it. It is
refused as the category's other rules are: by code with an
`ArgumentException` naming `Mop`, and by a document with a `JsonException`
carrying it, read as the category or as a record of either kind. A row read
back holds its `Board` to it too (see "DecisionRow"). `BoardPosition` stays
broad by the same ruling (see "BoardPosition"): XG and XGP data hold
terminal positions — a transcription commonly records the position a final
bear-off leaves — and such a position may be read from a source file but
never becomes a decision record. So every record has a `ProblemKey`, and
`ProblemKey.From` has no failure case (see "ProblemKey").

**`PositionData.IsDecisionPosition(board)` is the guard's non-throwing
form** (`halheinrich/backgammon#273`, Hal's ruling of 2026-09-27), as
`BoardPosition.TryCreate` is the constructor's. A producer reading outside
data asks it before building a record and passes by a position that is not
a decision — ordinary filtering, not a fault — without restating the rule
or catching the guard's `ArgumentException`, which this library throws for
other faults too. It is the rule's one statement: the `Mop` guard and the
row's read-back call it, so it answers `true` exactly when the category
accepts the board. It never throws, the empty board included, and its
answer does not depend on the board's frame (a board and its `Flipped()`
view answer alike). Design points:

- **It lives on `PositionData`, not `BoardPosition`.** The question is the
  category's, and `BoardPosition` stays broad (see "BoardPosition"); a
  member there would put the category's rule on the value. A static on the
  category reads as the category's question, and needs no record to ask it:
  a producer asks it with only the board in hand.
- **It answers admissibility, not facts to combine.** A consumer asks it
  directly, never rebuilding the answer from the borne-off counts
  `BoardPosition` publishes (a side with no checker on the board or the bar
  has borne off all `CheckersPerSide`), so the rule has no second statement
  to drift.
- **The message stays internal.** A consumer that filters has no refusal to
  report; the guard's and the read-back's refusals carry it.

`PositionBoardRuleTests` pins each side, both, every construction path, and
the edge: one checker left, on a point or on the bar, and a side whose one
checker is on the bar while the other has none. It pins `IsDecisionPosition`
against the same boards and ordinary positions: its answer directly, its
agreement with the guard and with a row's read-back on both paths, that it
never throws, and that the frame does not change it.

### Stored or derived

The audit behind the rule, over every record type (the umbrella's verdict
asked for it exhaustively). Derived, never stored:

| Member | Derived from |
|---|---|
| `PositionData.OnRollPipCount`, `OpponentPipCount` | `Mop`, by `BoardState`'s one pip rule |
| `BoardPosition.OnRollBorneOffCount`, `OpponentBorneOffCount` | the position's counts: `CheckersPerSide` less the side's checkers on the points and its bar (see "BoardPosition") |
| `BgDecisionData.SourceFile`, `DecisionRow.SourceFile` | `Id.Filename` |
| `PlayCandidate.DepthRank`, `CubeDecisionData.DepthRank` | `AnalysisMode` × `AnalysisLevel`, by the grid the producer used (internal `DepthTaxonomy`) |
| `PlayCandidate.Depth`, `DepthAbbreviation`, and the cube analysis's | the typed depth facts — `AnalysisMode`, `AnalysisLevel`, `RolloutTrials`, `BookEdition`, `UnrecognizedLevelCode` — by the producer's grammar (`DepthTaxonomy`; see "The depth as typed facts") |
| `RankedBy(ranking)`: the order, `Best`, each `RankedPlay.Error` and `IsScored` | the candidates' equities and, under depth first, their depth ranks (see "The ranking") |
| `RankedBy(ranking).PlayerResult` | the played candidate: scored with its error, or not scored; otherwise `Unstated` with the stored `UnlistedPlayError`, or not recorded |
| `CubeDecisionData.ActionEquity(action)` | `NoDoubleEquity`, `DoubleTakeEquity` and the pass's normalised value +1: each cube action's equity in the doubler's perspective, doubling's the taker's best response's (see "The analysis's cube actions on CubeDecisionData"); the best actions, both halves' errors, the truth and each answer's cost read it |
| `CubeDecisionData.BestAnswer` | the two halves' best actions: the cube answer whose projections they are (see "Cube answers and their costs") |
| `CubeDecision.GammonsPossible` | the position's board, cube and owner and its session's kind, rules and needs (see "Cube answers and their costs") |
| `CubeDecision.ClaimOf(answer)`, `CostOf(answer)` | the answer, `GammonsPossible`, and for the cost the equities through `ActionEquity` |
| `CubeDecisionData.UserDoubleError`, `UserTakeError` | the stated action's `DoublerActionError` / `TakerActionError`; otherwise the stored unstated-action error (confirmed against the corpus, measured once in the halheinrich/backgammon#273 leg, September 2026; figures not retained). A take error exists only under a stated `Double`, since a taker half does (`RecordedCubeActions`) |
| `IDecisionFilterData.Taker`, `TakerResult` (a cube record's view, and the row's columns from it) | a stated `Double`: the opponent's name (`DescriptiveData.OpponentName`), and the response scored with its `TakerActionError`, the stored unstated take error, or not recorded where the record holds neither (a double answered by resigning); `null` for a checker play and wherever no double was offered (SPEC-filtering §8) |
| `PlayCandidate.LosePct`, `CubeDecisionData.LosePctAfterNoDouble`, `LosePctAfterDoubleTake` | `1 −` the matching win probability: every game is won or lost. XG's stored figures were copies: the measurements are on `PlayCandidate.LosePct` and `CubeDecisionData.LosePctAfterNoDouble` |
| `BgDecisionData.Xgid` | the board, the cube and its owner, the session (its kind's facts) and a checker play's roll, by the format stated on the internal `XgidEncoder` (see "The XGID, derived") |

Stored, as source data:

- **The cube limit, the beaver rule and a money session's scores**
  (its terms' `MoneyTerms.CubeLimit` and `IsBeaver`, and
  `MoneySession.OnRollScore` and `OpponentScore`) —
  typed members, from which, with the rest of the record, the XGID is
  derived (its fields 10, 8 and 6–7).
- **The typed depth facts** — `AnalysisMode`, `AnalysisLevel`,
  `RolloutTrials`, `BookEdition`, `UnrecognizedLevelCode` — from which the
  label, the abbreviation and the rank are derived.
- **`Id`, with its `IsCube`** — the identity used apart from the record
  (stats keys, file navigation), so it carries its own kind; the record
  holds the two to agreement (`DecisionRules.IdAgrees`).
- **A match's length beside its away scores** (`MatchSession`: its terms'
  length, its own away scores) — each is
  the source file's; the away scores are the game header's score read
  against the length, but the length is not derivable from them.
- **The equities and the win, gammon and backgammon probabilities** — the
  analyser's outputs, verbatim; the total loss probabilities, measured as
  copies (above), are derived. A money game's cubeless equity follows from
  its probabilities in theory; that relation is unmeasured, so it is not
  derived here.
- **What was played** — `UserPlayIndex`, `UserDoublerAction`,
  `UserTakerAction` — and **the analyser's error where the record does not
  state what it scores**: `UnlistedPlayError`,
  `UnstatedDoublerActionError`, `UnstatedTakerActionError`, each refused
  beside what would determine it. The cube's four are one stored value,
  `CubeDecisionData.Recorded` (`RecordedCubeActions`), which the four wire
  members read through; its constructor also refuses a taker half without
  a stated `Double` (SPEC-scoring §3).
- **The source file's facts** — the roll, the candidates, the board, the
  scores, the cube, Crawford, Jacoby, the names, title, date, event,
  standard start, comment and flag.

`DecisionRow` is a projection: within the row its `SourceFile`, `Game`,
`MoveNumber`, `Dice`, `Session` and `MatchScore` derive from its own
columns; `Error`, `Equity`, `AnalysisDepth` and the boards are the record's
values under the row's `Ranking`, carried as columns, since the row holds
nothing they derive from.

### The depth as typed facts

A candidate's and the cube analysis's depth is stored as typed facts, and
its label, abbreviation and rank are derived from them (the umbrella's
second verdict on the records leg). The facts:

- `AnalysisMode` × `AnalysisLevel`, the taxonomy (below);
- `RolloutTrials` (`int?`) — the games a rollout played: an explicit
  rollout's, or a book hit's whose rollout parameters were recovered;
- `BookEdition` (`BookEdition?`: `V1`, `V2`) — a book hit's edition;
- `UnrecognizedLevelCode` (`int?`) — the analyser's raw code for a level
  this library does not recognize, so it still reads as itself.

Each keeps a rule, checked order-independently like every guard here: a
trial count is at least 1 and belongs to a rollout or a book hit; an
edition belongs to a book hit; a raw code is stated only with the level
`Unknown`. The internal `DepthTaxonomy` is the one home of the derivations
and the rules:

| Facts | `Depth` | `DepthAbbreviation` |
|---|---|---|
| Evaluation, 3-ply | `3-ply` | `3-ply` |
| Evaluation, XG Roller+ | `XG Roller+` | `R+` |
| Rollout, 3-ply, 1296 trials | `Rollout: 1296 trials. 3-ply` | `3p1296` |
| Rollout, no trials recorded | `Rollout` | `Ro` |
| Book hit, V2 | `Book V2` | `Book` |
| Book hit, V2, 4-ply, 12960 trials | `Book V2: 12960 trials. 4-ply` | `B4_12960` |
| Unknown, raw code 7 | `level-7` | `level-7` |
| Unknown, no code | `null` | `null` |

A level's label is its `[Description]`, which a test holds the table to.
The label and abbreviation are `[JsonIgnore]`d; a document still stating
them reads with them ignored and the derivation stands.

### The ranking

**One definition of the best play, for a given ranking** (SPEC-scoring
§2a; Hal's ruling on `halheinrich/backgammon#282`). `PlayRanking` has two
members:

- **`Equity`** (the default, and the zero value): by equity, highest
  first; analysis depth has no effect.
- **`DepthFirst`**: by `DepthRank`, deepest first — an unrecorded depth
  below every recorded one — then by equity within a depth.

Where both keys are equal, the stored order stands (the stored index is
the sort's last key, so the order is deterministic at every size). For a
given ranking, `CheckerPlayDecisionData.RankedBy(ranking)` derives
everything the ruling makes a ranking's, as a `RankedPlays`
(`IReadOnlyList<RankedPlay>` in the ranking's order):

- the order and each candidate's `Rank` (1 for the best);
- `Best`, the ranking's first;
- whether each candidate `IsScored`, and a scored candidate's `Error`: the
  best's equity minus its own, never negative. **Under depth first, a
  candidate at a different depth rank from the best's whose equity is
  higher than the best's is not scored** — a consumer treats it as an
  off-list play — **and has no error**: its `Error` is `null` (the
  umbrella's third-round ruling; a negative number a caller must remember
  to skip is the misuse the ruling prevents). `IsScored` is exactly
  `Error is not null`. Every other candidate is scored; under equity every
  candidate is. An equal equity at another depth is scored, with error 0.
  The rule is applied, under either ranking, as "a candidate is scored
  exactly when its equity is not higher than the best's" (the umbrella's
  fourth-round ruling). Both of SPEC-scoring §2a's conditions are implied
  by the orders: under depth first the best has the highest equity at its
  own depth, so a higher equity is always at another depth; under equity
  the best has the highest equity of all, so every candidate is scored.
  Each is pinned as a property (every unscored candidate is at a different
  depth from the best; under equity every candidate is scored);
- `UserPlay`, and `PlayerResult`, the player's result, each case by name
  (see "The player's result"): `Scored` with the played candidate's error
  when the ranking scores it, **`NotScored`, with no error, when it does
  not**, `Unstated` with the stored `UnlistedPlayError` for a play outside
  the candidates (no ranking changes it), and `NotRecorded` when no play is
  recorded.

`ForCandidate(index)` finds a candidate by its stored index. Each ranking
is built on its first request and cached (published once, thread-safe), so
asking again allocates nothing. An undefined ranking is an
`ArgumentOutOfRangeException` at every door.

**No ranking-free best.** Nothing answers "best" or "error" without a
ranking, and a test walks the public surface for any member saying best,
error or loss that neither takes a ranking nor belongs to a type built for
one. `CheckerPlayDecision.AfterBoardOfBest(ranking)` takes one;
`AfterBoardOf(index)` gives any candidate's board, `AfterPlayerBoard` the
user's.

**The filter view and the row are each built for one ranking.** A record is
not itself an `IDecisionFilterData`: `BgDecisionData.ViewFor(ranking)`
builds the view, whose `Ranking` states the ranking its `PlayerResult`,
`AnalysisMode`, `AnalysisLevel` and `AfterBestBoard` are derived under
(derived once, when the view is built). `DecisionRow.From(record, ranking)`
takes those columns from the view and stores the ranking as a column. So
the filter's "erred by more than x" is expressible under either ranking:
filter views, or rows, built for it. A cube decision's members do not
depend on the ranking. The members every kind has stay forwarded publicly
on the record (`Player`, `Session`, `Board`, …).

### The player's result

**"Not scored" and "not recorded" are told apart by type, never by one
`null`** (the umbrella's third-round ruling). Under depth first, both a
listed move the ranking does not score and a decision with no move
recorded have no error; one `null` could not say which. `PlayerResult` is
a readonly struct naming its case, `PlayerResultKind`:

| Case | Meaning | Error |
|---|---|---|
| `NotRecorded` (the zero value, and `default`) | no player move recorded | none |
| `NotScored` | a listed move the ranking does not score (checker plays, depth first) | none |
| `Scored` | a move this library scores: a listed play the ranking scores, a stated cube action | finite, never negative |
| `Unstated` | the record states no move or action, only the analyser's error: a play off the candidate list, a cube action not recorded | the analyser's, as stated |

The error is read only where it exists: `TryGetError(out error)` yields it
for `Scored` and `Unstated`, and `Match(notRecorded, notScored, scored,
unstated)` hands it to exactly those branches. The four named factories
build it; `Scored` refuses a negative or non-finite error. The filter's
"erred by more than x" is `result.TryGetError(out var e) && e > x`:
scored and unstated moves above x, never a move not scored, never a
decision with nothing recorded. `RankedPlays.PlayerResult` is a checker
play's under a ranking; `IDecisionFilterData.PlayerResult` is the view's —
for a cube decision, the doubler's half alone (its stated action scored,
the analyser's error for an unstated one, or not recorded), never
`NotScored`. The taker's half is its own member,
`IDecisionFilterData.TakerResult` (`PlayerResult?`): `null` where no double
was offered, otherwise the response scored, the analyser's take error, or
`NotRecorded` for a double answered by resigning (Hal, 2026-10-26), never
`NotScored`; `Taker` names the opponent beside it (SPEC-filtering §8,
halheinrich/backgammon#440). Which half a facet reads is XgFilter_Lib's.

Whether a play is correct is judged by the zero rule every equity loss
shares: its error shows as `0.0000` (`EquityDisplay.CountsAsZero`;
halheinrich/backgammon#202, SPEC-scoring §2a), so a play a hair off the
best is correct too. The rule is this library's, and so is the not-scored
classification; applying the rule to a verdict is the consumer's
(BgGame_Lib). The error itself stays exact.

### Shared types

One line per type: what it is, and where its contract lives. The XML doc
builds on every build, so it is the public surface's authoritative rendering;
a quoted name is a section of this file.

| Type | What it is | Contract |
|---|---|---|
| `CubeOwner` | who owns the cube, from the player on roll's side | its XML doc |
| `CubeAction` | a player's cube response; beaver and raccoon deliberately not yet members | its remarks |
| `CubeAnswer` | the four cube answers of SPEC-scoring §3: a submitted answer and the truth alike | "Cube answers and their costs" |
| `CubeClaim` | the three-way claim an answer reads as at a decision; deliberately not a fifth `CubeAction`, since "too good" is a rationale, not a board action | its remarks |
| `CubeAnswerCost` | what a cube answer costs at a decision, in two parts | "Cube answers and their costs" |
| `EquityDisplay` | the one display of every equity figure, and the one zero rule | "When a loss counts as zero" |
| `AnalysisMode` | how an analysis's numbers were produced: the mode axis of the depth taxonomy | its remarks; "The depth as typed facts" |
| `AnalysisLevel` | the evaluation level: the level axis, paired with `AnalysisMode` | its remarks, which own the contractual order; Pitfalls |
| `PlayerResultKind` | the case of a player's result | "The player's result" |
| `PlayRanking` | the ranking of a checker play's candidates, the one definition of which play is best | "The ranking" |
| `DecisionKind` | the kind of a decision as a value | "The decision kinds" |
| `SessionKind` | the kind of a session as a value | "Money and match: the session kinds" |
| `Seat` | a header's seat (XG's bottom and top player); no record holds one | its XML doc |
| `DiceRoll` | a roll in canonical unordered form, canonicalized here and nowhere downstream | its remarks; Pitfalls |
| `Move` | one checker move, `(FrPt, ToPt)`, with the kind of move in the sign of `ToPt` | "Move encoding" |
| `Play` | a mutable fixed-buffer struct of up to four moves, with no equality | its XML doc; "Play identity", "Play notation", Benchmarks, Pitfalls |
| `BoardPosition` | an immutable position, the one definition of "the same position" | its remarks; "BoardPosition" |
| `PlayChain`, `CanonicalPlay` | internal: a play's display form, a play's chains and each hit mark's place | "Canonical play form" |
| `PlayCandidate` | one analysed candidate of a checker play, one row of the analyser's move list | its XML doc; "The ranking" |
| `DecisionId` | the file-navigation identity: where did this record come from | its XML doc; "DecisionId" |
| `ProblemKey` | the content identity: which problem is this | its remarks; "ProblemKey" |

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

Collapse semantics (the XG chain-collapse rules; the notation that writes
them is under "Play notation" below):

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
- **Pinned by the formatter it replaced.** `PlayNotationTests` ports every
  case of BgMoveGen's former `MoveNotationFormatter`, with the same inputs
  and expected strings. A `(n)` run's hit mark is read off its first chain:
  the carrier rule puts a hit point's mark on the first chain ending there,
  so a run's mark is on its first chain or on none.
- **Invariant text.** Point numbers are written with the invariant culture,
  so the notation never varies with the machine's locale.
- **Never stored.** Notation is derived wherever it is shown — a candidate's
  is `PlayCandidate.Notation`, from its `Play`. A stored copy can disagree
  with the play it describes (a converted match stored `8/3* 7/3` over a
  play marking the hit on the 7-point checker), so no record carries one. Do
  not add a notation member to a record.

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
- **Broad, by ruling** (Hal, 2026-09-27, `halheinrich/backgammon#273`). The
  invariant admits the empty board and a side borne off: a terminal position
  is a position, and a working value may be any. Whether a decision can be
  made on a position is a stricter question, and it is `PositionData`'s (see
  "Data categories"), not this type's; so is its test,
  `PositionData.IsDecisionPosition`. Do not tighten this invariant to it,
  and do not add a member here that asks it.
- **Representation.** The counts are stored in the value, a byte each,
  which the invariant makes lossless: packed eight to a word into the four
  `ulong` fields of a private `Counts` struct, where one private map
  (`Counts.Locate`) places a slot, its constructor is the one write and its
  indexer the one read, and every read widens to `int`. Equality compares
  the four words, and the hash takes each word as its two 32-bit halves (a
  `ulong`'s own hash folds them, which collides boards differing in two
  slots four apart — `Hash_ReadsEverySlot` fails on it), so creating,
  comparing and hashing allocate nothing — pinned by test with
  `GC.GetAllocatedBytesForCurrentThread`, because a consumer does them once
  per play: BgMoveGen takes the position each play reaches, for every
  successor it returns and every candidate, and keys its move-entry
  searches by position. Its play generation compares no positions; it
  avoids duplicates by construction (BgMoveGen's INSTRUCTIONS.md, "One same
  position").
- **Never an inline array** (halheinrich/backgammon#320). A probe under
  Mono AOT trapped on generic by-value paths over inline-array-backed
  structs, this type's and others', while desktop JIT and a non-AOT
  WebAssembly build passed every path. No test here runs AOT, so none can
  observe the defect; `BoardPositionTests.Storage_HoldsNoInlineArray` pins
  the representation instead — no type the position is laid out from is an
  inline array — and `StorageWalk_FindsAnInlineArrayHeldInAFieldOfAField`
  pins that its walk can fail. What was run, and why plain words over the
  probe's other passing layouts, is on the `Counts` struct's comment.
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
  is `position == BoardPosition.Standard`. `Bg960`'s constraints (symmetry,
  quadrant coverage, mirror conflicts, the pip floor) are on its doc comment.
- **Read surface.** The indexer (slots 0–25), `CopyTo`, and a `ToString`
  of `slot:count` pairs for the occupied slots (`"empty"` for the empty
  board), which a test failure shows side by side. Deliberately no
  ordering, no text parsing, and no enumeration: it is a value, not a
  collection.
- **Each side's borne-off count, derived** (halheinrich/backgammon#295).
  `OnRollBorneOffCount` and `OpponentBorneOffCount` are `CheckersPerSide`
  less the side's checkers on the points and its bar, derived from the
  counts on each read and never stored. What each answers, and in which
  frame, is on its doc comment; a position and its `Flipped()` view swap
  the two. They answer for every position and never throw — the empty
  board has every checker of both sides off, a terminal position every
  checker of the side that has borne off — and allocate nothing.
  `BorneOffCountTests` pins them.
- **One number of checkers a side, public.** `CheckersPerSide` is the one
  statement of the number: the invariant bounds each side by it and the
  counts subtract from it, and both read a side's checkers through one
  private sum, so a count cannot disagree with what the invariant admits.
  It is public because the number is this library's fact: a consumer that
  states a bound against it — how many checkers a side can have off, or on
  one slot — reads it here rather than restating it.
- **The counts' placement: here.** A side's checkers are the position's
  fact, as the invariant is, and a terminal position has its counts though
  no decision is made on it, so the counts are not `PositionData`'s. Nor
  are they `BoardState`'s: a consumer holding a board reads them through
  `ToPosition()`, which allocates nothing. They are two properties, as the
  library pairs its other per-side facts (`PipCount` and
  `OpponentPipCount`, `OnRollNeeds` and `OpponentNeeds`), rather than a
  method taking a side: the library has no type naming the side on roll
  and its opponent, and would need one for two members.

### BoardState

Mutable backgammon board: the working copy play applies to. Private
storage for 26 counts, in the layout and frame `BoardPosition`'s summary
states, read through `ReadOnlySpan<int> Points`, plus `int
HighPointOccupied`. Borne-off counts are not tracked on `BoardState` —
checkers leaving the board simply disappear; each side's count is derived on
`BoardPosition` (`OnRollBorneOffCount`, `OpponentBorneOffCount`), read from
a board through `ToPosition()`.

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
  counts, validated by the value's one rule (partial boards are fine —
  borne-off checkers are not tracked on `BoardState`; each side's count is
  `BoardPosition`'s). `Copy()` is a deep copy; `FlippedCopy()` a deep copy
  re-expressed from the opponent's perspective (an involution — flipping
  twice reproduces the original). There is no public empty constructor:
  the empty board is `new BoardState(BoardPosition.Empty)`.
- **The snapshot.** `ToPosition()` takes the board as it stands as a
  `BoardPosition` value, allocation-free; later changes to the board do
  not reach it. Two boards are compared through it, never through
  `Points`. It is how a consumer takes the position a play reaches — apply
  the play, snapshot, undo, as BgMoveGen does for each successor and
  candidate — which is why it allocates nothing.

After construction a board changes three ways — the reset, the raw pair
and the turn boundary — with `Flip()` the private mechanic behind
`FlippedCopy()`:

- **`SetPosition(BoardPosition)`** — the whole-board reset: replaces every
  slot with the position's counts and recomputes `HighPointOccupied`,
  allocation-free. It is the door for a caller that reuses one board across
  positions rather than building one per position (BgMoveGen's interop),
  and it needs no check of
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
  to its position flipped by `BoardPosition.Flipped()`, which recomputes
  `HighPointOccupied` from scratch. Stays private: live-state flips
  happen only inside `ApplyPlay`, so callers advancing state always
  reason in on-roll POV. `FlippedCopy()` is the public flipped-*copy*
  primitive for querying a position from the other player's frame
  (e.g. cube-response evaluation) without advancing state — the
  receiver is untouched.

`PipCount`, `OpponentPipCount` and `IsRace` are pure derivations from
`Points`, each formula on its doc comment. The pip rule is stated once
(`BoardState.OnRollPips` / `OpponentPips`, internal), and
`PositionData.OnRollPipCount` / `OpponentPipCount` read it from the
record's board: a record's pip counts are derived, never stored, so they
are the board's by construction.

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
carry one.

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

Both records reject `':'` in `Filename` with `ArgumentException`,
symmetrically, because the parse dispatcher discriminates the two shapes by
its presence: without the guard on `XgpDecisionId`, a filename holding one
would parse back as a malformed `XgDecisionId`. Equality and the canonical
forms are on the type's remarks.

Stamping is producer-side — `ConvertXgToJson_Lib` sets `Id` at the four
`Build*` sites. `Id` is `required` on both `BgDecisionData` and
`DecisionRow`, so missing-id cases surface as compile errors at any
construction site that omits the property.

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

- **Two doors, no constructor.** `From(BgDecisionData)` is the single
  producer-side factory; `Parse`/`TryParse` is the wire read-back. The type
  is a sealed class rather than a record precisely so no `with`-expression
  hatch exists. Consumers never assemble a key — repeated consumer glue
  would be a library gap.
- **Every record has a key** (Hal's ruling of 2026-09-27,
  `halheinrich/backgammon#273`; `SPEC-stats-identity.md` §2, amended the
  same day). `From` returns the key, with no failure case: a record's
  construction holds every fact the key reads at least as strictly as the
  parse door holds a key's text — the board to a checker of each side (see
  "Data categories"), the session to its kind's rules, the cube to a
  positive power of two with a defined owner, a checker play's dice to two
  faces. `From`'s `<returns>` states the implication and a `Debug.Assert`
  checks it; `ProblemKeyTotalityTests` pins it as a property over records
  of both kinds, swept across each rule's edges, each key reading back from
  its text and as a statistics document's key. The producer owns the
  guarantee, so a consumer holding a record relies on it and does not guard
  against a missing key.
- **The no-key rung is the parse door's.** Derivation that would guess is
  forbidden: a problem filed under a wrong key is corruption. So text whose
  facts are malformed, degenerate or inconsistent is no key — `TryParse`
  returns `false` and `Parse` throws `FormatException`. Only text reaches
  the rung.
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
  wrong. Every money session states its rule (`MoneyTerms.IsJacoby`, a
  required `bool`), and a match states none.
- **The text is kept; the construction is the session kinds'** (Hal,
  2026-09-26, halheinrich/backgammon#273). The grammar already writes
  money as its own production — `0a0` is the key's spelling of money, not
  a record's away scores — so every key is byte-identical to what the
  construction at `ca83ab1` wrote, and the statistics format (schema v3) is
  unchanged. `From` reads the record's `Session` by its kind into the
  key's two score productions (a private `Score` of the key's own, which
  the parser also builds, so both doors reach the one emitter).
  `ProblemKeyByteIdentityTests` holds a sweep of the builders' variants —
  every decision kind, every score shape the grammar spells, every cube
  shape — to the literal texts captured from the `ca83ab1` build. The parse door's acceptance set is unchanged: it still accepts
  keys no record can produce now (a Crawford game at `1a1`, as the Crawford
  cube keys before it), so no stored key stops loading.
- **No version token inside the key.** The containing stats document's
  schema version pins the grammar, and that version is the document's fact
  (BgGame_Lib's), not this library's. A fact entering identity bumps the
  document version rather than the key's shape — the Jacoby suffix is that
  mechanism's first exercise (`SPEC-stats-identity.md` §3).
- **Real-board posture: the text's rule and the record's differ, by
  design.** A key's text needs a physically possible position —
  `BoardPosition`'s invariant, not restated in the key: the parse door
  builds the board through `BoardPosition.TryCreate` — plus a non-empty
  board, the key's own rule. A record's board is held to more, a checker of
  each side (`PositionData`'s rule), which is what makes `From` total. The
  text's rule stays the weaker because a key that fails to parse fails its
  whole statistics document (`ProblemKeyJsonConverter` throws) and schema
  v3 is unchanged, so every v3 document that reads today must still read.
  A key with a side borne off, which a record could produce before the
  ruling, still parses; no record produces one now, so it is inert, as a
  Crawford cube key is (see "The decision kinds"). The empty board never
  had a key, and its text still does not parse. The key's remarks state
  the same; `ProblemKeyTests` pins both halves.

`ProblemKeyJsonConverter` implements the property-name overloads because
the stats document keys its per-problem map by `ProblemKey`, so
`Dictionary<ProblemKey, …>` must round-trip without a consumer-side key
converter.

### Played cube actions on CubeDecisionData

Distinct from the analysis's action facts and the answers below,
`CubeDecisionData` carries the record of what was *actually played* in a
cube decision:

- **`UserDoublerAction`** (`CubeAction?`) — the doubler action the player
  on roll played, `NoDouble` or `Double`. Null when the played action is
  not recorded.
- **`UserTakerAction`** (`CubeAction?`) — the taker action the opponent
  played, `Take` or `Pass`. Present only when a double was offered *and* a
  response recorded: in an undoubled game no taker decision exists, so
  this is null unless `UserDoublerAction` is `Double`.

**The recorded cube actions are one value** (halheinrich/backgammon#440):
`CubeDecisionData.Recorded`, a `RecordedCubeActions` holding the two actions
and the two unstated errors, which the four wire members read through. One
rule spans the halves — **a taker half, a response or the analyser's take
error, exists only under a stated `Double`**: a take or pass without an
offered double is invalid data (SPEC-scoring §3, 2026-10-25) — and init-only
members set one at a time could not hold it, hence one value. The type's
remarks state its rules and its six named shapes. A document reaches the
value through the serializer's constructor, which binds the four (see "The
decision kinds"), so it is held to the rule whatever its member order.

The fields exist because the played action is source data: it cannot be
recovered from an error — a zero error does not identify the action when the
two cube equities tie. It is the other way round: the error of a stated
action is derived from it (`UserDoubleError` is
`DoublerActionError(UserDoublerAction)`, `UserTakeError` is
`TakerActionError(UserTakerAction)`), never stored. The one cube error stored
is the analyser's for a half whose action is not stated
(`UnstatedDoublerActionError`, `UnstatedTakerActionError`), which a stated
action refuses beside it.

These are **actions, never claims**: a stamped game fact cannot carry the
"too good" rationale, because a player can choose not to double in a
position where they are not too good — the rationale is a property of the
analysis, not of the played move (the `halheinrich/backgammon#86` intake's
"analysis vs. stamped action" distinction). The cube answer (`CubeAnswer`)
and the claim it reads as (`CubeClaim`) therefore neither supersede nor
touch this pair: answers live in the quiz and the derived truth, played
actions in the game record.

### The analysis's cube actions on CubeDecisionData

`CubeDecisionData` derives the analysis's facts about each cube action
from `NoDoubleEquity` and `DoubleTakeEquity`, through one calculation, each
cube action's equity:

- **`ActionEquity(action)`** (halheinrich/backgammon#273, Hal's ruling of
  2026-09-27) — the equity of any of the four `CubeAction`s, in the player
  on roll's (the doubler's) perspective whichever half the action belongs
  to, in normalised cube-equity units (a single game at the current stake
  is +1), like every equity on the type. Its doc states each action's
  value. Higher is better for the doubler and worse for the taker, whose
  own equity is the negation.

Each half of the decision is judged on its own, with no cross-decision
override, comparing its two actions' `ActionEquity`:

- **Doubler's double / no-double decision**: `BestDoublerAction` and
  `DoublerActionError(action)`. `BestDoublerAction` is `Double` iff
  `ActionEquity(Double) > ActionEquity(NoDouble)`; the error is
  `ActionEquity(BestDoublerAction) − ActionEquity(action)`.

- **Taker's take / pass decision**: `BestTakerAction` and
  `TakerActionError(action)`. `BestTakerAction` is `Take` iff
  `ActionEquity(Take) < ActionEquity(Pass)` (the take leaves the doubler
  less than the cash); the error, in the taker's perspective, is
  `ActionEquity(action) − ActionEquity(BestTakerAction)`.

**These are facts of the analysis, not what an answer costs.** Each action
error is what that action loses against the best of its half, and other
members read them: the diagram's equity and loss table, a stated action's
`UserDoubleError` and `UserTakeError`, the filter view's player result.
What a cube answer costs is SPEC-scoring §3's, `CubeDecision.CostOf` (see
"Cube answers and their costs"): it adds the response an answer commits
to, and §3's two ruled conventions (its "Two conventions" bullet) can make
an answer's cost differ from its action's error. So a consumer showing
each action's equity beside its error shows the analysis's own numbers,
consistent with each other, and the quiz's answer costs are §3's, not
these.

**One calculation, the pass value never on its own** (Hal's ruling). The
pass's value and the rule for doubling's equity are stated once, in
`ActionEquity`: the best actions, both errors, the truth and each answer's
cost read them through it. The value +1 is a private constant it alone
reads, and it is not published on its own, because a consumer that rebuilt
the calculation from a normalization constant would be a second source of
it. The result is a method, as the two errors are, because it takes the
action: one member covers the four actions in one perspective and one sign,
where four properties would publish the pass's value as a member of its own.
Each error is that gap through the private `Gap`: never negative, and +0 at
a tie whatever the tied zeros' signs, since a stored `-0` less a `+0` would
otherwise be `-0`, which a document writes as `-0`.
`CubeDecisionDataActionEquityTests` pins each action and each tie in that
perspective, that each error is the gap between two of its equities over a
grid holding every tie, the `-0` tie, and that no public member names the
pass.

The computed members exist on the cube decision only, so asking them of a
checker play does not compile. The two error methods throw
`ArgumentOutOfRangeException` when the action argument is from the wrong
half (e.g. `Take` or `Pass` passed to `DoublerActionError`); `ActionEquity`
takes any defined action.

Tie-breaking follows the renderer's convention: `NoDouble` on the
doubler-equity tie, `Pass` on `DoubleTakeEquity == 1`. The truth among the
answers keeps both.

The three computed properties (`BestDoublerAction`, `BestTakerAction`,
`BestAnswer`) carry `[JsonIgnore]`: they are a derivation, not wire.
`ActionEquity` and the error methods are intrinsically not serialised
because they take parameters.

### Cube answers and their costs

**A cube answer is one of four** (SPEC-scoring §3, amended 2026-09-30 and
twice on 2026-10-01; Hal's rulings on halheinrich/backgammon#326): a
`CubeAnswer`, `NoDouble`, `DoubleTake`, `DoublePass`, or the fourth,
`NoDoublePass` — "don't double, they'd pass". Hal: "Either you double or
you don't; either you take or you pass. That makes four not six."
SPEC-scoring §3 is the source of truth for every rule below; its tables are
not restated here.

```
CubeAnswer (enum)            NoDouble, DoubleTake, DoublePass, NoDoublePass
  CubeAnswerExtensions       DoublerAction(), TakerAction(), CommitsToResponse(); Of(doubler, taker), the inverse
CubeDecisionData             BestAnswer — the truth, from the equities alone
                             internal CostOf(answer, gammonsPossible) — the calculation, beside the equities
CubeDecision                 GammonsPossible — the position's fact
                             ClaimOf(answer) — what the answer reads as; the fourth's label choice
                             CostOf(answer) — the door: a CubeAnswerCost (DoublingPart, TakePart, Total)
                             ZeroCostAnswers — the answers whose Total counts as zero, in the offered order
```

Design points a maintainer needs before touching it:

- **An enum, like the library's other closed value sets.** `CubeAction`
  and `CubeClaim` are enums whose every door refuses an undefined value
  with `ArgumentOutOfRangeException`; `CubeAnswer` is the same, so every
  operation taking one refuses anything but the four. Declaration order is
  the offered order, §3's column order: all four answers are always
  offered. It carries the strict converter like every enum, though no
  document embeds it (see "Source generation & trimming").
- **The fourth is named by its meaning, never by a label.** Its label
  depends on the decision; either way it is the answer "don't double, they'd
  pass", whose two projections its name spells.
- **Its projections are total, and never stored beside it.**
  `CubeAnswerExtensions.DoublerAction()` is `NoDouble` for No double and
  the fourth, `Double` for the two doubling answers; `TakerAction()` is
  `Take` for No double (implied) and Double / Take, `Pass` for the other
  two. `CommitsToResponse()` is `false` for No double alone: no cube is
  offered, so the take it implies is never charged, while the fourth
  commits to its pass under either label. It is the one statement of
  which answers a take/pass tally counts (SPEC-scoring §3's Take row and
  lifetime take half).
- **Two actions become an answer one way.** `CubeAnswerExtensions.Of(doubler,
  taker)` is the projections' public inverse — each pair of a doubler-half
  and a taker-half action is exactly one answer — and refuses an action
  outside its half (`ArgumentOutOfRangeException`, naming it; the doubler
  is checked first). The truth is read off the best actions this way, and a
  consumer holding two recorded actions (`UserDoublerAction`,
  `UserTakerAction`) forms the answer this way too, then reads its label
  through `ClaimOf`. A record holds a response only under a stated `Double`
  (`RecordedCubeActions`), so a recorded pair is a double with its
  response; no record holds a no double with a pass.
- **The truth is the same type, from the equities alone.**
  `CubeDecisionData.BestAnswer` is the answer whose projections are
  `BestDoublerAction` and `BestTakerAction`: Double / Take or Double /
  Pass when doubling is best, by the taker's best response; otherwise the
  fourth answer when they'd pass and No double when they'd take. The two
  halves' tie-breaks stand, so at the tie of halheinrich/backgammon#293 (a
  no-double equity of exactly 1, with a pass) the truth is the fourth
  answer. It is the single representative that classification and display
  read, not the test of a correct answer (see Pitfalls). Whether gammons are
  possible decides what the truth reads as and what each answer costs, never
  which answer it is.
- **Whether gammons are possible is the record's** (SPEC-scoring §3,
  amended 2026-10-01 on halheinrich/backgammon#326).
  `CubeDecision.GammonsPossible` is `false` exactly when the opponent has a
  checker borne off (`BoardPosition.OpponentBorneOffCount`), when a match's
  cube is at least the points the player on roll needs (`CubeSize` at
  least `MatchSession.OnRollNeeds`), or when money under the Jacoby rule
  has the cube centred. Hal: "If the opponent has checkers borne off,
  gammons are not possible. Also, gammons are only possible when the size
  of the cube is less than the points we need win the match." It is a fact
  of the position, the session, the score and the cube, which only the
  record sees together; the analyser's gammon chances are not part of it.
- **What an answer reads as: the claim, and the fourth answer's label.**
  `CubeDecision.ClaimOf(answer)` is the three-way claim of SPEC-scoring §1
  as a reading of the answer at the decision: No double reads `NoDouble`,
  the two doubling answers `Double`, and the fourth `TooGood` where gammons
  are possible and `NoDouble` where they are not. For the fourth answer the
  reading is which of its two labels applies, Too good or No double /
  Pass. The labels' wording, full and short, is the label home's, in
  BackgammonDiagram_Lib, which renders the reading and re-checks no rule.
  It is the one derivation of a claim: the truth's claim is
  `ClaimOf(Decision.BestAnswer)` — Too good at the halheinrich/backgammon#293
  tie where gammons are possible, though playing on is worth only the cash.
- **What an answer costs.** `CubeDecision.CostOf(answer)` gives a
  `CubeAnswerCost`, exactly per SPEC-scoring §3's two tables: where
  gammons are possible (the fourth reading Too good) and where they are
  not (it reading No double / Pass). Its two parts add up to its `Total`
  by construction: the doubling part and the take part, as §3's 2026-10-01
  amendment defines them, stated once in code on `CostOf`'s documentation.
  The two conventions — No double where Too good is right costs N − 0.6,
  and Too good when they'd take costs 2(1 − T) — apply only where gammons
  are possible. 0.6 is one named private constant,
  `CubeDecisionData.BorderlineDoubleEquity`, whose documentation cites
  Hal's convention and the corpus measurement on halheinrich/backgammon#326 (about 0.6 overall,
  about 0.65 in money). A doubling answer's parts are the analysis's own
  action errors; the conventions live only in the two no-double answers'
  doubling parts.
- **Computed beside the equities, asked of the record.** The calculation
  is the internal `CubeDecisionData.CostOf(answer, gammonsPossible)`,
  where the equities, the pass's value and the convention's constant
  live; the public door is `CubeDecision.CostOf(answer)`, which supplies
  the record's own gammon fact, so no caller ever passes one. A
  `CubeAnswerCost` has no public constructor, so every cost is a
  decision's.
- **Exact.** Costs are computed from XG's stored equities as they are
  (Hal: "That's bad Xg data; we merely echo what's there"), never rounded
  and never corrected: a no-double equity stored at +1.0000015 with
  gammons not possible gives Double / Pass a cost of 0.0000015, which the
  zero rule judges (see "When a loss counts as zero").
- **The answers that cost zero at the display precision.**
  `CubeDecision.ZeroCostAnswers` is every answer whose `CostOf(answer).Total`
  counts as zero (`EquityDisplay.CountsAsZero`), in the offered order: the
  set SPEC-scoring §3's "The tie" has the review's Best line list
  (halheinrich/backgammon#326, step 3a). It is the one derivation, so the
  diagram's Best line and BgQuiz's "best is" wording name the same answers.
  - *Its size is the costs', not an exact tie's.* It is never empty and
    always holds `BestAnswer`, which costs exactly 0, but the truth does
    not size it: off a tie it can hold two (where gammons are not possible
    and the fourth answer is right, No double costs 0 too), and near a
    boundary an answer whose cost is not 0 but shows as `0.0000` joins it,
    below the cash or above it.
  - *Its policy is fixed:* the zero rule and nothing else. It is not "the
    correct answers": an error tolerance (halheinrich/backgammon#30) would
    widen only the quiz's verdict, in BgGame_Lib, never this set.
  - *No caller can change it.* Each read builds its own read-only list
    (`IReadOnlyList<CubeAnswer>` over a fresh array, never an array or a
    `List<T>`), and the decision keeps no collection, so nothing a caller
    does to one result reaches another read.
- **Not wire.** `BestAnswer`, `GammonsPossible` and `ZeroCostAnswers` are
  `[JsonIgnore]`d, and `ClaimOf` and `CostOf` take a parameter, so a
  record's wire is unchanged and the flat row gains no column.

**The evidence, and its owner.** This library verifies the rules with
synthetic pins, which gate, each building full decisions where the gammon
fact matters: `CubeAnswerTests` (the four, their projections and the
inverse), `CubeDecisionDataBestAnswerTests` (the truth in every region and
at each boundary, and from the equities alone), `CubeDecisionGammonsPossibleTests`
(each gate on its own with its complement, every combination of gates,
and that XG's gammon chances decide nothing), `CubeDecisionClaimOfTests`
(each answer's reading under both gammon states, the fourth's label
following each gate, the one derivation), and `CubeDecisionCostOfTests`
(every cell of both tables with exact parts, the boundaries, the positions
ruled on halheinrich/backgammon#326, and over a grid that the parts keep
their shape and the truth always costs nothing), and
`CubeDecisionZeroCostAnswersTests` (every row of both tables, the tie in
both gammon states, the named pairs away from the cash, the threshold
either side below the cash and above it, and over a grid that the set is
never empty, holds the truth, is exactly the zero rule on each whole cost
and keeps the offered order; that no caller can change it; that it is not
wire). Checking the rules'
inputs against what XG stores remains ConvertXgToJson_Lib's
(SPEC-scoring §3); no test here reads `TestData/`.

### When a loss counts as zero

**`EquityDisplay` owns how an equity figure is shown and when a loss
counts as zero** (halheinrich/backgammon#202, folded into
halheinrich/backgammon#326 by Hal on 2026-10-01; SPEC-scoring §2a and §3).
An equity loss is any cost or error in equity: a cube answer's cost or
either part, a checker play's error under a ranking, a cube action's error,
an error the analyser stored. An equity is a position's or an action's
value: a play's `Equity`, a cube action's `ActionEquity`.

- **`FormatLoss(loss)`** — four decimals, culture-invariant: the one
  display of every cost and loss. A loss that rounds to zero shows as
  `0.0000` whatever its sign; a negative one beyond that, which only an
  error the analyser stored can be, shows its sign, echoing the data. A NaN
  or an infinity is refused.
- **`FormatEquity(equity)`** — the one display of an equity, with its sign:
  the digits `FormatLoss` shows for the same number, and `+` ahead of any
  figure that shows no minus, so `+0.6004`, `-0.3000`, and `+0.0000` for
  an equity that rounds to zero from either side, never `-0.0000` or
  `+-0.0000`. A NaN or an infinity is refused, naming `equity`. It adds the
  sign rule only: the precision is the one `FormatLoss` reads, so a loss and
  the equities beside it are shown alike (halheinrich/backgammon#326, step
  3a).
- **`CountsAsZero(loss)`** — exactly when `FormatLoss` shows `0.0000`: for
  a loss that is not negative, below 0.00005. It reads `FormatLoss`'s own
  text rather than a threshold of its own, so what is shown and what is
  judged cannot disagree anywhere, and the precision is stated once, in a
  private format string both displays read. The double nearest 0.00005
  lies just above it and shows as `0.0001`, so the boundary falls where
  "below 0.00005" says.
- **Verdicts only.** The rule changes no stored number: costs, errors and
  equities stay exact, and so does which region of §3's tables a decision
  falls in. A sum is judged as a sum: two parts that each count as zero can
  add up to a cost that does not, so a whole answer is judged on its total
  and each part on its own.
- **The one set it decides here.** `CubeDecision.ZeroCostAnswers` applies
  the rule to each answer's whole cost (see "Cube answers and their
  costs"). Its policy is the rule itself, fixed: a quiz's error tolerance
  (halheinrich/backgammon#30) would widen the quiz's verdict, never the set.
- **Applying it is the consumers'.** BgGame_Lib's `Correctness` judges
  every verdict by it — checker plays, each part of a cube answer, each
  whole answer — and every surface shows through these two displays (see
  Pitfalls).

`EquityDisplayTests` pins the loss display, the threshold just below, at
and just above 0.00005 through the shared formatting, and the two loss
members' agreement over values on both sides of it; then the equity
display's sign, its `+0.0000` for a negative zero and for values just
either side of zero, its digits against `FormatLoss`'s for the same
magnitude, and its refusals.

### The decision kinds

**A decision is one of two types** (halheinrich/backgammon#273, Hal's ruling
of 2026-09-25): `CheckerPlayDecision` and `CubeDecision`, the two sealed
kinds of the abstract `BgDecisionData`. Each carries only its own members,
so code that reads one kind's member off the other does not compile, and no
member of either holds a value standing for "not applicable".

```
BgDecisionData (abstract)       Kind, Id, Position, Descriptive; Match, Switch;
                                the shared members; Xgid (derived); ViewFor(ranking)
├── CheckerPlayDecision         Decision : CheckerPlayDecisionData
│                               Dice (canonical), AfterBoardOf(i), AfterBoardOfBest(ranking),
│                               AfterPlayerBoard (derived)
└── CubeDecision                Decision : CubeDecisionData
                                GammonsPossible (derived), ClaimOf(answer), CostOf(answer)
```

Design points a maintainer needs before touching it:

- **Where the kind lives: the record.** The kind is the record's type, not a
  category's, because every member only one kind has belongs to that kind's
  type, and some need the whole record: the after-boards need the position
  and the candidates, and `GammonsPossible` — with what each cube answer
  reads as and costs, which read it — needs the board, the cube and the
  session.
  A polymorphic `Decision` category under one composite would have left
  those on the shared type, answering (or throwing) for the kind they do
  not apply to. Each kind keeps the category structure: `Decision` is its
  own category, typed per kind (`CheckerPlayDecisionData`,
  `CubeDecisionData`), beside the shared `PositionData` and
  `DescriptiveData`.
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
  breaking the same rule gets the guard's exception itself. **The serializer
  constructors** are how a type tells the two apart: each type holding its
  members to a rule has an internal `[JsonConstructor]` beside the
  parameterless one code uses, which marks the instance as read, and each
  guarded setter then rethrows its `ArgumentException` through
  `DocumentRefusal`. The mechanism, why the platform forces it (what was
  measured, on .NET 10) and what each constructor binds are stated once, on
  `BgDataTypesJsonContext`, and each constructor's doc points there.
  Accepted by the umbrella as the separate internal constructor pattern,
  not a hatch around validation. The converter only dispatches; it catches
  nothing. `PlayJsonConverter` refuses a play of more than four moves
  itself.
- **The members agree by construction.** Each init setter checks its value
  against the members already set and against the kind, which the base's
  constructor fixes before any member is set, so every rule is
  order-independent and needs no sentinel. The rules are stated once, on
  `DecisionRules`, and refused with an `ArgumentException` naming the
  member that completed the contradiction:
  - **Crawford** (`halheinrich/backgammon#201`): a cube decision's
    `Position` refuses a Crawford position — a `MatchSession` whose
    `IsCrawford` is set; a money session has no Crawford game to refuse.
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
  faces 1–6; it has at least
  one candidate, each of finite equity, so every ranking has a best (see
  "The ranking"); and
  `UserPlayIndex` identifies one or is `null` — "none", never -1
  (`UserPlay` is that candidate). An `UnlistedPlayError` is stated only
  with no `UserPlayIndex`. The checks are order-independent the same way,
  and the two collections are copied on init, so a caller keeping its own
  list cannot change them afterwards.
- **The view, built for a ranking.** The members every kind has are
  forwarded publicly on the base. The `IDecisionFilterData` view says best
  and error, which a ranking decides, so the base is not itself a view:
  `ViewFor(ranking)` builds one (internal `DecisionView`), deriving per kind
  through `Match` — a checker play's best candidate under the ranking and
  its `PlayerResult`; a cube's analysis, the doubler's result, and where a
  double was offered the taker's name and result. The checker play's own filter members — `Dice` and the
  after-boards — are `null` in a cube's view and on neither the base's nor
  the cube's surface; `CheckerPlayDecision` exposes them (the best
  after-board for a ranking). `Game` and `MoveNumber` derive from `Id` (see
  "DecisionId"). Every derived member carries `[JsonIgnore]`
  (`halheinrich/backgammon#14`): the stored members are the wire, so a
  record's top level is exactly `Kind`, `Id`, `Position`, `Descriptive`,
  `Decision`, pinned by test for each kind (the XGID left it when it became
  derived: see "The XGID, derived").
- **`GammonsPossible`, `ClaimOf` and `CostOf` live on `CubeDecision`**
  (SPEC-scoring §3, amended 2026-10-01 on halheinrich/backgammon#326) —
  see "Cube answers and their costs". Only the record sees the board, the
  cube and the session together, and only a cube decision has the
  question; the truth, `BestAnswer`, stays on `CubeDecisionData`, derived
  from equities alone, and so does the cost's calculation, which the
  record asks with its gammon fact.

### Money and match: the session kinds

**Money versus match is one of two types** (halheinrich/backgammon#273,
Hal's ruling of 2026-09-26): `MoneySession` and `MatchSession`, the two
sealed kinds of the abstract `Session`, which a record's `PositionData`
holds as `Session`. Each carries only its own facts, so money has no match
stand-in to spell (a length of 0, away scores of 0, a Crawford flag always
false), and `MatchContextStandInTests` pins that none is expressible.

**A session composes its terms instead of restating them** (the umbrella's
review of halheinrich/backgammon#273, 2026-09-26). A session is its kind's
terms — the header's `MoneyTerms` or `MatchTerms` (see "Shared consumer
contracts") — plus the game's standing, seen from the player on roll. So
each kind holds its terms as a member, `Terms`, and states only the
oriented standing itself:

```
Session (abstract)      Kind (its terms'); Create; Match, Switch; value equality
├── MoneySession        Terms (MoneyTerms: IsJacoby, IsBeaver, CubeLimit); OnRollScore, OpponentScore
└── MatchSession        Terms (MatchTerms: Length); OnRollNeeds, OpponentNeeds, IsCrawford
```

Design points a maintainer needs before touching it:

- **Placement: the position, from the player on roll's side.** The session
  is the score context of the decision, beside the board and the cube, and
  on-roll-relative like them — a match's away scores are the player on
  roll's and the opponent's. No record holds a seat: a record is the player
  on roll's view, whatever seat that player sits in.
- **Built by one operation: `Session.Create(terms, standing, onRoll)`.** A
  header states the terms and the game's standing seat by seat (player 1,
  player 2); the operation pairs the two through `GameSession.Create`
  (below), which holds them to each other — terms and a standing of
  different kinds are refused (`ArgumentException` naming `standing`), and
  so is a match standing with an away score above the terms' length
  (`ArgumentOutOfRangeException` naming `standing`), since the standing
  knows no length — and then turns the standing to the `Seat` on roll: the
  seat on roll's score or away score becomes the player on roll's, the
  other's the opponent's, a match's Crawford flag is the game's. A pair the
  pairing refuses is refused before the seat is looked at. It is the one
  orientation rule, and the one way code outside the library builds a
  session: the kinds have no public constructor and no public setter, so
  no producer orients the scores itself. The library's own construction is
  the serializer's and the row's read-back, which states the standing
  already on-roll-relative.
- **A game's session: the pairing, with no seat** (Hal's ruling of
  2026-09-29 on halheinrich/backgammon#273, from XgAnalytics's leg). A
  caller at game scope has a header's terms and a game's standing but no
  decision, so no player on roll; passing `Session.Create` a seat to have
  the two validated would be a stand-in. It pairs them through
  `GameSession.Create(terms, standing)` instead: a `MoneyGameSession` or a
  `MatchGameSession`, each composing its kind's terms and standing as the
  header states them — seat-anchored, claiming no player on roll — so the
  caller tells money from a match, and reads a match's `Terms.Length`,
  `Standing.Away1`, `Standing.Away2` and `Standing.IsCrawford`, through one
  `Match`/`Switch` on the kind, with no kind check of its own and no branch
  that cannot run. It is the one statement of the pairing rule — the kind
  dispatch in `Create`, and the match kind's construction holding each away
  score to the length — and `Session.Create` pairs through it, so the two
  accept exactly the same pairs and refuse the rest alike
  (`GameSessionTests` pins that over a grid crossing each edge of the rule,
  at either seat). A closed pair, a value (equality over the terms and the
  standing), built only through `Create`. Not a wire type: no document
  embeds it, so its wire debut belongs to the first that does; its terms
  and its standing are the wire types a producer serializes.
- **On the wire the kind is stated once, in the terms.** A session is
  `{"Terms":{"Kind":…,…},…}` and its oriented standing — never a `"Kind"`
  of its own beside its terms' (`Session.Kind` is `[JsonIgnore]`d and
  derived; a document stating one reads with it ignored, as every derived
  member is). `SessionJsonConverter` dispatches through the internal
  `KindDispatch`, which finds the `"Terms"` member and the kind inside it
  wherever each sits, and refuses, as a `JsonException` on both paths, a
  missing, duplicated or unknown kind, terms stated twice or not an object
  — read as a `Session`, as its kind, or inside a record. The same
  mechanism the decision kinds, the terms and the standings use: each
  converter supplies its mapping from kind to contract, and the session's
  also the member its kind is stated in. Each kind disallows unmapped
  members, and so do its terms, so a money session stating a match fact,
  a match a Jacoby rule, or either a fact of its terms beside them, is
  refused rather than read with it dropped. Each kind's internal serializer
  constructor binds its `Terms`, whose own contract holds their kind to the
  type.
- **A match is well-formed by construction** (`SessionRules`, internal): its
  terms' length at least 1; each away score at least 1 — a player 0-away
  has won — and at most the length;
  and in the Crawford game exactly one player 1-away (the Crawford game is
  the one after a player first reaches match point, so the other is not
  1-away then, and a 1-point match has none; the corpus held no other
  shape, measured once in the halheinrich/backgammon#273 leg, September
  2026; figures not retained). Each guard is order-independent, naming
  the member that completed the contradiction; a document gets a
  `JsonException` carrying it.
- **Money states its rule.** `MoneyTerms.IsJacoby` is a required `bool`:
  every money session's terms state the Jacoby rule, so there is no unknown
  rule. The in-tree producer always knows it (XG's match header).
- **The facts only the XGID carried, placed by the same rule.** The beaver
  rule is money's (XG spells it in the XGID for money only), and so is the
  cube limit the record carries: XG keeps it among the match header's
  money-session settings (beside the stakes and the automatic-double
  limit). The record carries no Max Cube for a match, and no cube limit is
  added to `MatchTerms` (Hal's ruling of 2026-09-27): XG match input states
  one, with legitimately varying values, but whether it is a match-domain
  rule of its own is not established (halheinrich/backgammon#289; see "The
  XGID, derived"). So a money session's terms
  carry `IsBeaver` and `CubeLimit` (a positive power of two, as XG's
  default 1024), and the session its standing, `OnRollScore` and
  `OpponentScore` — the game header's scores from the player on roll's
  side, each at least 0, the counterpart of a match's away scores. All
  required. The position holds its cube to them: `CubeSize` is a positive
  power of two, never above a money session's terms' `CubeLimit` (whichever
  of the two is set second refuses), and `CubeOwner` is a defined owner —
  so the XGID derived from the record can never be wrong for one that
  exists.
- **A value.** A session is immutable and has value equality (its terms,
  compared as values, and its standing; `==`, `!=`, a consistent hash), so
  a record's view, its row and the row read back from JSON state one
  session, whatever instance each holds.
- **Where it is read.** `IDecisionFilterData.Session` (the view's is the
  record's, the row's is built from its columns and held to the kind's
  rules on read); `DecisionRules.CrawfordAllows(kind, session)`;
  `CubeDecision.GammonsPossible`; `ProblemKey.From`, whose text is
  unchanged (see "ProblemKey"); `DecisionRow.MatchScore`;
  `BgDecisionData.Xgid`.

### The XGID, derived

**The XGID is derived from the record and never stored**
(halheinrich/backgammon#273, the match-context leg). `BgDecisionData.Xgid`
writes eXtreme Gammon's position string from the record's own members on
each read, through the internal `XgidEncoder`, so every field is the
record's and the string cannot disagree with it. The format, field by field, is stated once, on
`XgidEncoder`'s doc comment. Design points:

- **Written from the player on roll's side**, as the record is: the
  position in `Mop`'s layout, the turn always 1, the cube's owner 1, 0 or -1
  from that side, the scores the player on roll's first.
- **The roll high die first**, whatever order the record states it rolled
  in: `CheckerPlayDecision.Dice`'s canonical token, which is how XG writes
  it (no low-first roll among the corpus's 80,497 distinct XGIDs, measured
  2026-09-26). A cube decision's is `00`.
- **The format's spellings of money are the encoder's alone.** For money it
  writes a match length of 0 and the session's scores; for a match, its
  length less each away score. Field 8 is Crawford for a match, Jacoby plus
  twice beaver for money. The maxCube field is an exponent, the highest
  cube value being `2^maxCube`; a money session's is its limit's exponent.
- **A match's maxCube field is the constant 10, because the record states
  none** (Hal's ruling of 2026-09-27 on halheinrich/backgammon#273, which
  withdrew the assumption of the umbrella's 2026-09-26 review that XG writes
  10 for every match). No record member and no `MatchTerms` member holds a
  match's Max Cube, so the field has nothing of the record's to spell, and
  10 is the value XG typically writes. Legitimate XG-authored files state
  others — 3 for a 5-point match and 4 for a 9-point one among them — so the
  XGID derived for such a match differs from XG's own in this one field.
  What the constant does not claim: that a match has no cube limit, or that
  its length bounds what its cube can win — whether a match's Max Cube is a
  match-domain rule of its own is open (halheinrich/backgammon#289), and no
  cube limit is added to `MatchTerms` meanwhile; nor any relation to the
  length — the two values above happen to be the smallest exponent whose
  cube value covers it, but XG does not follow that consistently, so
  nothing encodes it. The encoder's `MatchMaxCubeField` states the constant
  where it is applied.
- **Culture-invariant**: numbers are written invariant, so a culture's minus
  sign (`sv-SE`'s U+2212) cannot change the opponent's `-1`.
- **Off the wire.** `[JsonIgnore]`d; a document still stating one reads
  with it ignored and the derivation standing. The row carries the record's
  as its `Xgid` column (the row holds no cube column to derive it from).
- **Measured against XG** (a scratch differential, not gating, 2026-09-26):
  all 712 corpus samples that carry XG's XGID beside their record facts
  derive it byte for byte, and 80,479 of the 80,497 distinct corpus XGIDs,
  decoded into records and re-derived, are identical; the other 18 are
  Crawford cube XGIDs, which a record refuses. No real state tripped any of
  the new rules. Every match header that corpus held stated a Max Cube of
  10, so the measurement could not find another value and says nothing
  about one (the bullet above). `XgidDerivationTests` pins known strings — XG's own and the
  two the builders once stored — as literals.

### After-boards (derived)

The boards a checker play's candidates leave: `AfterBoardOf(index)` for
any candidate, `AfterBoardOfBest(ranking)` for the best under a ranking, and
`AfterPlayerBoard` for the user's. **Derived, never stored** — the arc's
rule, no stored copy of a derivable value: the position the candidate's
play reaches from the record's `Position.Mop`,
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

**Cost.** The same pass computes every candidate's board, once per record,
while the record is built; reading them allocates nothing, the best's
under either ranking included once the ranking has been asked for (pinned
by test), which matters because the filter reads after-boards over many
records.

They are off the record's wire. The row carries them as columns taken from
this derivation (see "DecisionRow"). This is the substrate for
`XgFilter_Lib`'s three-board `IPlayTypeClassifier` contract.

### DecisionRow

One flat row per decision of either kind, one column per fact, with its
JSON wire form; it carries no file format. **A projection of the record, built one way**: `DecisionRow.From`
takes every column from a `BgDecisionData` — the shared ones through its
`IDecisionFilterData` view for the row's ranking, the kind's own from the
kind — and the constructor is internal, so outside this library a row
comes from `From` or from JSON. **Built for one ranking**: `From(record,
ranking)` takes the checker play's error, depth, mode, level, equity and
best after-board under that ranking (see "The ranking") and stores it as
the required `Ranking` column; a cube row's columns do not depend on it. The after-board columns are the record's derivation, never
computed a second way, and a row and its record cannot disagree on
anything they share.

**It carries the kind** (`Kind`, a required column, written first), and the
other kind's columns are empty, never zero: a cube row's `Roll` and
after-boards are `null`. The player's result is two
columns: `Result`, required, its case (`PlayerResultKind`), and `Error`,
stated exactly when the case is `Scored` or `Unstated` — otherwise `null`,
never the 0.0 the producer wrote as a stand-in; which of
the errorless cases it is, `Result` says. `PlayerResult` is the two typed,
`[JsonIgnore]`d, as `Dice` is `Roll`'s. Read back, a row is held to their
agreement: the error stated exactly for `Scored` and `Unstated`, a scored
error never negative, no cube row `NotScored`. Which check reports first
on a document missing a required column differs by path (measured on
.NET 10): the generated context checks required members before the row's
read-back check; the reflection path runs the read-back check first, which
sees the absent member as its default, so a rule can refuse it first. It is
refused either way. `Game`, `MoveNumber` derive from `Id`, and
`IsStandardStart` is `null` for a standalone position
(`halheinrich/backgammon#124`). `Roll` is the checker play's dice in rolled
order as a two-digit integer; `Dice` is its canonical form, `[JsonIgnore]`d
like `MatchScore`.

**A cube row carries both decisions** (SPEC-filtering §8,
halheinrich/backgammon#440): the doubler's as `Player`, `Result` and
`Error`, and the taker's as three columns after `Player`, taken from the
view as `Result` and `Error` are — `Taker` (`string?`, the opponent's
name), `TakerResultKind` (`PlayerResultKind?`) and `TakerError`
(`double?`). `TakerResult` is the two typed, `[JsonIgnore]`d, as
`PlayerResult` is `Result` with `Error`; the column is `TakerResultKind`
because the row implements the view's `TakerResult` (`PlayerResult?`)
under that name. `TakerResultKind` is **the one required nullable
column**: `null` means no double was offered, so a nullable default would
read every row written before the column as "no double offered"; required,
a document must state it, `null` included. Read back: a checker-play row's
three are empty; a cube row's `TakerResultKind` is never `NotScored`;
`TakerError` is stated exactly when `TakerResultKind` is `Scored` or
`Unstated`, finite and non-negative when `Scored`; a row whose
`TakerResultKind` is `null` names no `Taker`; `Taker` holds stated text.
**A breaking read for row documents:** a row written before these columns
is refused for its missing `TakerResultKind`. No row corpus is in tree and
no production reader reads rows, so no migration machinery exists.

**It carries the session's kind the same way** (halheinrich/backgammon#273):
`SessionKind`, a required column, then one column per fact of each kind —
a match's `MatchLength`, `OnRollNeeds`, `OpponentNeeds`, `IsCrawford`, a
money session's `IsJacoby`, `IsBeaver`, `CubeLimit`, `OnRollScore`,
`OpponentScore` — each nullable, and the other kind's empty,
never zero: a money row's `MatchLength` is `null`. `Session` is the columns
typed as the record's kind,
`[JsonIgnore]`d: a row from `From` holds the record's own session, and a
row read from JSON the session its columns state.

**Read back whole.** A row read from JSON is held, once every column is
read (`IJsonOnDeserialized`), to what a projection guarantees: the kind's
columns present and the other kind's empty, the roll two die faces, a
checker row's best after-board present, a board with a checker of each side
(a decision position's, `PositionData`'s rule), the session kind's columns stated
and the other session kind's empty, the session's own rules (it is built
as a record's is, so a match's away score of 0 is refused as there), and
`DecisionRules` (Crawford, the
identifier's kind, a standalone position's start). A document breaking one
is a `JsonException` on both paths. No initializer can build a row outside
this library, so the whole check runs once, over a complete row.

`Board` is a required `BoardPosition` in the frame of `PositionData.Mop`.
The three boards and `AnalysisMode` / `AnalysisLevel` serialize to JSON,
beside `AnalysisDepth`, the depth's label.

`MatchScore` is the session as one token — `3a5a`, `1a4aC` in the Crawford
game, `moneyJ`, `moneyNJ` — spelled from the row's `Session` on each read,
which the filter grammar and Extract's exports consume; it spells its away
scores with the invariant culture, whatever the ambient one.

`IsJacoby` is the money session's rule, carried as a column
(`halheinrich/backgammon#121`). It reaches `MatchScore` the way `IsCrawford`
does, as an in-grammar suffix on the money score. Like `IsCrawford`, it also
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
  in XG) and `Terms`, a `SessionTerms`: a money session's `MoneyTerms`
  (`IsJacoby`, `IsBeaver`, `CubeLimit`) or a match's `MatchTerms`
  (`Length`, at least 1). Producer contract: XG's raw sentinel for
  unlimited sessions (a length of 99999) is read at the parse boundary as
  money terms, never surfaced as a length.
- **`IGameInfo`** — game scope: `IsStandardStart` (false for saved/custom
  openings — the opening-move filter's input) and `Standing`, a
  `GameStanding`, player 1 and player 2: a money session's
  `MoneyStanding` (`Score1`, `Score2`, each at least 0: the points each has
  won in the session) or a match's `MatchStanding` (`Away1`, `Away2`, each
  at least 1, and `IsCrawford`, with exactly one player 1-away when set).

**Money versus match is the kind at every scope** (halheinrich/backgammon#273,
Hal's ruling of 2026-09-26). The three
scopes — a session's terms, a game's standing (seat-anchored, since both
players roll within a game), a decision's `Session` (the terms, composed,
and the standing from the player on roll's side) — are three closed pairs,
each matched exhaustively (`Match`/`Switch`), each a value (equality over
the kind and its facts), sharing `SessionKind` and the rules of the
internal `SessionRules`. **A producer builds a record's session from the
other two**, through `Session.Create(Terms, Standing, onRoll)` with the
`Seat` on roll at the decision — the one orientation rule, and the only way
code outside the library builds one (see "Money and match: the session
kinds"). **A consumer at game scope pairs the two with no seat**, through
`GameSession.Create(Terms, Standing)`: a game's session, money or a match,
whose match kind reads the length, the away scores and the Crawford flag
seat-anchored, as the header states them — the pairing rule stated once,
and the one `Session.Create` pairs through.

**The terms and the standing are wire types** (the umbrella's review of
halheinrich/backgammon#273). A producer serializes its header types — the
converter's `XgMatchInfo` and `XgGameInfo` are in its own context — so an
abstract member with no contract would be written with its facts dropped
and could not be read back. Each family is a kinded document read through
the one dispatch, as a record and a session are: `"Kind"` written first and
read wherever it sits (`SessionTermsJsonConverter`,
`GameStandingJsonConverter`, both on `KindDispatch`), each kind disallowing
the other's members, every member required, and every refusal a
`JsonException` on both paths, read as the base or as the kind. Each kind
has the internal serializer constructor binding its `Kind`, so a rule a
document breaks is refused as a `JsonException` carrying the guard's
exception.

Both contracts are minimal **by design** — members are added on demand,
never mirrored wholesale from a producer; they are purely the shape a
consumer needs to decide "skip this match / skip this game".

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
records, here and in every consumer. This suite uses it too, so the builders
are exercised against the
real records and a change to a record's construction breaks one place.

**What it holds: what consumers' tests share.** The project began as the
record builders' home; its purpose is what every repository's tests would
otherwise each restate. The second thing so shared is `AllocationProbe`,
the allocation pins' measurement (halheinrich/backgammon#273): a consumer
copied it and the copy drifted within a leg (it dropped the early exit), so
the one statement lives here, public, and consumers' tests reference it
rather than copy it. It depends on the runtime alone; its behaviour is
pinned by `AllocationProbeTests` in this suite, and that the suite keeps no
copy by `TestSupportPostureTests`.

The third is the test loggers (halheinrich/backgammon#424). Three test
projects each stated the same logger — a recording one, and one whose every
entry throws, as a failing logging provider does — and two more suites were
about to. `RecordingLogger<T>` keeps every entry in order as a
`LogEntry(Level, Message, Exception)`, the message fully formatted, with
every level enabled and a scope that does nothing; `ThrowingLogger<T>`
throws `InvalidOperationException` with `Failure` on every entry and counts
`Attempts`, for the pins that logging what happened stops nothing. They are
pinned by `TestLoggersTests`. They are the reason for the one package below.

**Posture** — the BgUiPrimitives_Razor test-support project is the
precedent, and all three of its properties are mirrored, the second revised
for the loggers:

- **Not packable.** Consumers reach it by `ProjectReference` from their test
  projects.
- **References the library and one package** — no other project, and no
  package but `Microsoft.Extensions.Logging.Abstractions`, which the test
  loggers implement and every member already uses; in particular no test
  framework, so a consumer's test project keeps its own. The library itself
  takes no package. The library grants it no internals (`InternalsVisibleTo` names this suite
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
record), a builder per category — `Position`, `CheckerPlayData`,
`CubeData`, `Descriptive`, `Candidate` — and one per session kind,
`MatchSession` (by default a 7-point match at 0-0) and `MoneySession` (by
default under the Jacoby rule), which `Position` takes as its `session`
argument — each built as a producer builds one, through `Session.Create`
with the player on roll in seat 1, so its arguments are the player on
roll's. Each yields a well-formed value of
its type with realistic defaults, and takes the members a test cares about
as named arguments spelled as the members are: a checker play is the
opening 3-1 from the standard start at 0-0 in a 7-point match, with three
candidates (8/5 6/5 best and played); a cube decision is a double/take in a
short race; both sit in a game of an `.xg` match. A record builder's
defaults follow its arguments only where the records' rules demand it — a
standalone id gets no `IsStandardStart`, and a board other than the
standard start gets one candidate, the pass (valid from every position),
unless the test states its own decision — and `TestRecordsTests` pins
them. A builder can build nothing a producer cannot (the posture above), so
`Position` refuses a board with a side borne off, or the empty one, as the
category does. A test whose subject is construction itself writes its own
object initializer.

### Mop layout

The layout and its on-roll frame are `BoardPosition`'s, stated on the type's
summary. `PositionData.Mop`, `DecisionRow.Board` and
`IDecisionFilterData.Board` are in that frame; the after-boards hold the same
layout in the next mover's frame (see "After-boards"). On the wire each is
the 26 counts as a JSON number array. The two ways to get the frame wrong are
under Pitfalls.

## Benchmarks

`BgDataTypes_Lib.Benchmarks` is a BenchmarkDotNet harness over `Play`
construction — the one place in this library with a hot-path performance
contract, because BgMoveGen's generator builds a `Play` per candidate.

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
run here and inflate BenchmarkDotNet's means. Only ever read
the sibling comparison *within* one run — the grouped `Ratio` column — never
one run's absolute means against another's. Sequential "measure, edit,
measure" is not a valid comparison on this hardware.

## Pitfalls

Each entry helps a reader avoid a mistake or make a decision; overlap with the
code is not itself a fault. The reason behind a rule is in the section it
points to; the "don't" is here.

### Wire

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
- **Do not add a wire member with an initializer.** The source-generated
  context passes an absent init-only member as `default`, not as its
  initializer, so one reopens the divergence between the two read paths
  (see "Absence on the wire"), and `WireAbsenceTests` fails a member that is
  neither required nor nullable. Producers (`ConvertXgToJson_Lib`'s `Build*`
  sites) state every member of the kind they build; tests build through
  `TestRecords`.
- **Embed a record as `BgDecisionData`, not as its kind.** Every read
  refuses malformed input as a `JsonException` — as the base, as a kind, or
  as a category — but only the base reads either kind: a consumer's
  document embeds `BgDecisionData`, never a kind, and a collection of
  records is a consumer's document: its context declares it and chains this
  one.
- **An options-level converter outranks the bundled attribute.** A consumer
  building its own `JsonSerializerOptions` inherits every bundled converter
  and need add nothing. A `JsonStringEnumConverter` registered there
  *loosens* the strict enums back to accepting ordinals, and a reader that
  accepts ordinals re-couples stored JSON to `AnalysisLevel`'s numbering,
  where an inserted member renumbers everything after it
  (halheinrich/backgammon#164;
  `OptionsLevelRegistration_OutranksTypeAttribute_SoConsumersMustTightenToo`
  pins the outranking). Do not strip `Play`'s attribute or register a
  different converter for it: default property-based serialization only
  sees `Count`, losing every move.
- **`DecisionRow` carries no file format.** Extract owns its CSV
  (`CsvLayout`) since 2026-10-20 (halheinrich/backgammon#424): while the
  layout lived on the shared row, one app's columns reached the other's
  file. Do not put a file layout back on the row; its writer owns it.

### Records and fixtures

- **A Crawford cube cannot be built** (`halheinrich/backgammon#201`). So a
  fixture that wants the Crawford flag builds a Crawford *play*, and a test
  that needs a Crawford cube *key* (still in `ProblemKey`'s grammar, for old
  stats documents) builds it from the key string, never from a record. A
  Crawford game has exactly one player 1-away: a fixture builds it as
  `TestRecords.MatchSession(onRollNeeds: 1, opponentNeeds: 3, isCrawford: true)`,
  and a test pinning the cube rule states such a standing, or the match
  session's own rule refuses first.
- **A fixture's board and candidates are held to the record's rules.** The
  builders' default candidates are the opening's; on another board, state
  the plays, or take the builder's pass. The empty board and a side borne
  off are `BoardPosition`s, but no record stands on one:
  `TestRecords.Position` refuses them as the category does (see "Data
  categories"). A test that needs such a board as a board builds the
  `BoardPosition`, or a `BoardState` from it.
- **Money is a kind, never a 0, at every scope.** A consumer matches on the
  kind (`Session.Match`, `Terms.Match`, `Standing.Match`), never on a match
  length or away score of 0: no surface carries one. An `IMatchInfo`
  implementer returns `MoneyTerms` for a money session — XG's
  unlimited-session sentinel (a length of 99999) is read at the parse
  boundary as money, never surfaced as a `MatchTerms` length — and an
  `IGameInfo` implementer a `MoneyStanding` for a money game. Every money
  session's terms state the Jacoby rule, so do not look for an unknown one;
  a producer that cannot tell the rule has no record to build.
- **Pass `Session.Create` the seat on roll at the decision**, not the seat
  that started the game: it is the one place the standing is turned to the
  player on roll, and it cannot tell a wrong seat from a right one. Code
  holding on-roll facts already (a builder, a test) passes them as seat 1's
  with `Seat.Player1`, which turns nothing. A caller with no decision — a
  game's header alone — has no seat on roll to pass: it pairs the terms and
  the standing through `GameSession.Create`, never through `Session.Create`
  with a seat standing in for one.
- **`default` is not "none".** `default(DiceRoll)` is non-meaningful: a
  `record struct` cannot run its face validation on `default`, so it carries
  faces of 0. `default(CubeAnswer)` is a real answer, `NoDouble`, so an
  unset field reads as one. "No roll" is `DiceRoll?` null and "no answer"
  `CubeAnswer?` null, never `default`.

### Boards and frames

- **Mop sign convention is player-relative, not color-relative.** Positive
  always means the on-roll player, regardless of which physical color they
  are playing. Code that forgets this will silently mirror boards. A new
  `IDecisionFilterData` implementer returns `Board` in exactly this frame:
  the layout is the type's, the frame is not, and `XgFilter_Lib` filters
  assume it.
- **After-boards use flipped POV.** `AfterBoardOf` / `AfterBestBoard` / `AfterPlayerBoard` hold
  the same layout as `Board` but in the next mover's frame — the opponent is
  on roll after a play, so the decision-maker's checkers are *negative* and
  the opponent's are positive. Code that forgets this mirrors the
  after-boards silently.
- **`ApplyPlay` flips perspective; the bare flip is private.** After
  `ApplyPlay`, positive values represent the *next* mover's checkers, not
  the previous on-roll's. Code that expects to inspect a state "from the
  original mover's POV" after a turn must take a `Copy()` *before* calling
  `ApplyPlay`. To *view* a position from the other player's frame without
  advancing state, use `BoardPosition.Flipped()` (a value, allocation-free)
  or `FlippedCopy()` (a board) — never re-encode negate-and-reverse in a
  consumer; the value's `Flipped()` is the one statement of the rule.
- **`Move.ToPt` is sign-encoded** (see "Move encoding"). `0` is bear off, not
  "stay on point 0", and a negative is a hit landing on `|ToPt|`, not a
  backward move. Code that compares `ToPt` numerically without understanding
  the encoding will silently misinterpret hits and bear-offs.
- **Bearing-off overshoot is a property of the data shape.** Bear-off
  legal only from `HighPointOccupied` when `HighPointOccupied <= 6`
  *and* the die exceeds `FrPt`. The `BoardState` data primitive does
  not enforce this — `Move(FrPt, 0)` is encodable for any `FrPt` —
  but `BgMoveGen.MoveGenerator.NextMove` does. Code that hand-builds
  bear-off moves outside the move generator must respect the rule.
- **`BoardState` cannot be written by callers, and must not be reopened**
  (halheinrich/backgammon#281; `BoardStateTests.PublicSurface_HasNoWritableState`).
  A test or consumer that wants a particular board builds it — `FromMop`
  from counts, or `new BoardState(position)` — and changes it through the
  apply methods. Re-exposing a writable array, or a public
  `RecalcHighPoint`, would bring back the desynced high point and the
  corrupt boards `ApplyPlay`'s guarantee cannot survive.

### Plays

- **`Play` is a mutable value type.** `Add` / `RemoveLast` mutate in place,
  but assigning a `Play` to another variable copies the buffer. Reading a
  `Play` through a `List<Play>` indexer gives a copy; modifying that copy
  does not update the list until it is assigned back. Use `Snapshot()` when
  the intent is an explicit independent copy, and re-assign back to the
  list slot when mutation is intended. `foreach` likewise enumerates a
  value copy — a mid-loop `Add` on the source is invisible to the
  iteration (pinned by test).
- **`Play.Create`'s cost depends on which overload you reach.** The four
  fixed-arity overloads construct at parity with the incremental `Add`
  spelling and are what a literal argument list binds to.
  `Create(params ReadOnlySpan<Move>)` and collection expressions cost more —
  not because of anything inside the method, but because the *caller*
  materialises an argument buffer before the call, which no change to
  `Play` can remove. That is fine for tests and readability sites and wrong
  for a move-generation inner loop: in a hot path, pass the moves as
  separate arguments (or keep using `Add`), and do not "tidy" such a site
  into a collection expression. The numbers and the standing guard are
  under Benchmarks.
- **The slot-write seam is private and must stay the only one.** `Add` and
  all five `Create` overloads write moves through one private `SetSlot`
  primitive, which owns both the ordinal → field mapping and the `Count`
  maintenance that goes with it. It carries
  `[MethodImpl(AggressiveInlining)]` deliberately and measurably: without
  it `Add` does not fold its slot switch, which the `*_Add` benchmark rows
  exist to catch (`PlayConstructionBenchmarks`' summary records what an
  early attempt cost). A future construction path adds an overload that
  calls `SetSlot` with literal indices — it does not write slots itself,
  and it does not loop over `Add`.
- **`Play` and `CanonicalPlay` have no equality — by ruling, not
  omission.** `Play.Equals` and `Play.GetHashCode` throw
  `NotSupportedException`, so default equality and hash operations on a
  `Play` — `Distinct`, `HashSet<Play>`, a `Dictionary` keyed on `Play`,
  `List<Play>.Contains` — can throw when they invoke those methods (an empty
  collection need not). Compare plays with `BoardState.IsSamePlay` from their
  starting position, find one in a list with `IndexOfSamePlay`, and compare
  stored encodings with `Play.IsSameEncoding`. Do not reintroduce a board-less
  comparison, a notation key, or a `CanonicalPlay`-based dedupe: the display
  form differs between encodings of one play
  (`halheinrich/backgammon#277`).

### Depth and verdicts

- **`AnalysisMode` and `AnalysisLevel` always travel as a pair, and
  `Unknown`/`Unknown` is data, not an error.** `BookRollout` +
  `AnalysisLevel.Unknown` is a live producer stamp (a book hit without
  recoverable levels), so code must not treat `AnalysisLevel.Unknown` as
  implying `AnalysisMode.Unknown`. `AnalysisLevel`'s declaration order is
  contractual ascending rigor, ruled on XG's own menu, with the ply and
  Roller families interleaved (the type's remarks own the rule): a reorder
  or an out-of-order insertion breaks the diagram's level floor and the
  level dropdowns. `Unknown` is outside that scale — never a floor, never a
  threshold. To compare whole analyses across the pair, use `DepthRank`. Do
  not strip a member's `[Description]` label: downstream label readers
  (XgFilter_Lib's `EnumLabel.ToLabel`) throw on a member without one.
- **Best, equal-best and correct are three questions.** Identifying the
  best uses `RankedPlays.Best`, under a ranking; testing membership in its
  equivalence class uses `Error == 0.0`; judging a play *correct* is the
  zero rule, `EquityDisplay.CountsAsZero`, which a play a hair off the best
  passes too. A candidate's `Error` is `null` where the ranking does not
  score it, so there is nothing negative to sum by mistake.
- **Judge a loss zero, and show any equity figure, only through
  `EquityDisplay`.** Every verdict — a checker play, each part of a cube
  answer, each whole answer — asks `CountsAsZero`; every display of a cost
  or a loss calls `FormatLoss`, and of an equity `FormatEquity`. An
  `== 0.0` test, a format string, a sign rule or a numeric threshold of a
  consumer's own is a second statement of halheinrich/backgammon#202's
  rule, and can disagree with what is shown. Judge a whole answer on its
  `Total`, never as the conjunction of its parts' verdicts: two parts that
  each count as zero can add up to a cost that does not. To name the
  answers that cost nothing, read `CubeDecision.ZeroCostAnswers` rather
  than filtering the four through `CostOf` again.

### The cube

- **An action error is not an answer's cost.** Do not score an answer from
  `DoublerActionError` and `TakerActionError`, and do not re-spell its
  parts: read `CubeDecision.CostOf` (the reason is under "The analysis's
  cube actions on CubeDecisionData"). A No double answer's implied take is
  never charged (`CommitsToResponse`), and the conventions apply only where
  gammons are possible.
- **Never restate a cube action's equity; read `ActionEquity`.** Its value
  for every action is the doubler's, the taker's actions included: a take
  is `+DoubleTakeEquity` and a pass +1, not their negations. Spelling
  `Math.Min(DoubleTakeEquity, 1)` or a pass constant in a consumer creates a
  second source of the calculation the errors, the truth and the costs
  derive from.
- **A null recorded action means "not recorded", not "declined".** A null
  `UserTakerAction` alongside a recorded `NoDouble` is the normal
  undoubled-game state — no taker decision ever existed — not missing data.
  A test that overrides `TestRecords.CubeData`'s `userDoublerAction` keeps
  the builder's default `Take` unless it says otherwise, and is refused. And
  they are *actions, never claims*: do not infer `CubeClaim` from a played
  action (see "Played cube actions on CubeDecisionData").
- **The fourth answer is not Too good.** `CubeAnswer.NoDoublePass` reads Too
  good only where gammons are possible. Label it from
  `CubeDecision.ClaimOf`, never by a fixed mapping from the answer, and
  never re-derive the choice from the board or the session.
- **Never re-derive whether gammons are possible.**
  `CubeDecision.GammonsPossible` is the one site; spelling a borne-off
  count, a cube-against-needs comparison or a Jacoby-and-centred test in a
  consumer creates a second source of the ruling, and so does reading
  XG's gammon chances as the fact.
- **The truth is a representative, not the correct answer.**
  `CubeDecisionData.BestAnswer` is what classification and display read.
  An answer is correct when its cost counts as zero, so at an equity tie
  more than one is: at the halheinrich/backgammon#293 tie, No double,
  Double / Pass and the fourth answer all cost nothing. Never test
  correctness as equality with the truth.
- **Never map an answer to its actions, or back, by hand.** The
  projections are `CubeAnswerExtensions`' (`DoublerAction`, `TakerAction`,
  `CommitsToResponse`) and `Of` their inverse; a consumer's own switch is a
  second statement of them. No claim leads back to an action, and no action
  or answer to a claim without the decision (`ClaimOf`).

### Named collections

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
