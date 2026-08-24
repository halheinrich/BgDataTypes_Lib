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
that need converters (`CubeOwner`, `CubeAction`, `AnalysisMode`,
`AnalysisLevel`, `Play`, `DecisionId`, `ProblemKey`, `DiceRoll`) each bundle their own
`[JsonConverter]` attribute so consumers do not have to register
converters on their `JsonSerializerOptions`.

## Layout

Three projects under `BgDataTypes_Lib.slnx`, governed by repo-root
`Directory.Build.props` (TFM, `TreatWarningsAsErrors`, XML doc generation)
and `Directory.Packages.props` (Central Package Management — no inline
`Version=` anywhere).

**`BgDataTypes_Lib/`** — the library. Six areas, one file per type:

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
- **Move and board primitives** — `Move`, `Play`, `PlayChain`,
  `CanonicalPlay` (the play-equivalence SSOT), and `BoardState`, the one
  mutable type in the library. Value types here inherit hot-path zero-alloc
  constraints from move generation.
- **Enums and the depth taxonomy** — `CubeOwner`, `CubeAction`, and
  `AnalysisMode` × `AnalysisLevel` (the two-axis depth taxonomy), alongside
  the small validated value types `CubeDecisionPair` (a `CubeAction` pair
  with per-half guards) and `DiceRoll` (a canonical unordered roll).
- **Shared consumer contracts** — `IDecisionFilterData`, the filter-layer
  view implemented by `BgDecisionData` and `DecisionRow`, carrying the score
  context a filter needs (`OnRollNeeds`/`OpponentNeeds`, `IsCrawford`,
  `IsMoneyGame`, and the tri-state `IsJacoby?` the money score tokens read);
  `IGameInfo` and `IMatchInfo`, implemented by producers so filter layers
  never reference a producer's concrete types.
- **JSON converters** — `PlayJsonConverter`, `DiceRollJsonConverter`,
  `DecisionIdJsonConverter`, `ProblemKeyJsonConverter`. Each is bundled onto
  its type by a type-level `[JsonConverter]` attribute; consumers register
  nothing.

**`BgDataTypes_Lib.Benchmarks/`** — BenchmarkDotNet harness, an executable
(`OutputType=Exe`) excluded from `dotnet test` by `IsTestProject=false`.
`Program.cs` is the `BenchmarkSwitcher` entry point;
`PlayConstructionBenchmarks.cs` measures every `Play` construction path
against the incremental `Add` spelling. See Benchmarks below.

**`BgDataTypes_Lib.Tests/`** — xUnit, one test class per type or per
behaviour area of a type (`ProblemKeyTests`, `BoardStateTests`,
`PipCountTests`, `RaceTests`, the `*SerializationTests` pair, …). Fixtures
are constructed in code — pure data types need no corpus, so this project
does not reach into the umbrella `TestData/`.

## Architecture

Composite and category types are `class` with `init`-only properties; the
move primitives `Move` (`readonly record struct`) and `Play` (mutable
`struct`) are value types for hot-path zero-alloc reasons inherited from
their move-generation origins. `BoardState` is a `class` but mutable —
the one deliberate exception (see "Mutability exception" below).
Serialization uses `System.Text.Json` with bundled `[JsonConverter]`
attributes: `JsonStringEnumConverter` on `CubeOwner`, `CubeAction`,
`AnalysisMode`, and `AnalysisLevel`, `PlayJsonConverter` on `Play`,
`DecisionIdJsonConverter` on `DecisionId`, `ProblemKeyJsonConverter` on
`ProblemKey`, and `DiceRollJsonConverter`
on `DiceRoll`. Consumers do not need to
register any of these converters on their `JsonSerializerOptions` — the
attributes carry the contract on the types themselves.

### Mutability exception

All composite and category types in this library are `class` with
`init`-only properties — except `BoardState`, which is mutable for
hot-path move-generation efficiency. The type encapsulates its own
state-management logic (`ApplyMove` / `UndoMove` / `ApplyPlay` maintain
`HighPointOccupied` incrementally), and external mutation of `Points`
is supported but desyncs `HighPointOccupied` unless the caller calls
`RecalcHighPoint`. Hot-path consumers (BgMoveGen's move generator) use
the apply/undo primitives; non-hot-path consumers should advance state
via `ApplyPlay`, never via raw point-array mutation.

### Data categories

`BgDecisionData` composes four orthogonal category types:

| Type | Fields |
|---|---|
| `PositionData` | `Mop`, `OnRollNeeds`, `OpponentNeeds`, `OnRollPipCount`, `OpponentPipCount`, `CubeSize`, `CubeOwner`, `IsCrawford`, `IsJacoby?` |
| `DecisionData` | `Dice`, `Plays`, `BestPlayIndex`, `UserPlayIndex`, `UserPlayError?`, `IsCube`, `CubeDepth`, `CubeDepthAbbreviation`, `CubeDepthRank`, `CubeAnalysisMode`, `CubeAnalysisLevel`, cube equity/pct fields, `UserDoubleError?`, `UserTakeError?` |
| `DescriptiveData` | `MatchLength`, `OnRollName`, `OpponentName`, `Title`, `Date`, `Event`, `SourceFile`, `MoveNumber`, `IsStandardStart` |
| `PlayOutcomeData` | `AfterBestBoard`, `AfterPlayerBoard` |

### Shared types

| Type | Notes |
|---|---|
| `CubeOwner` | enum: `OnRoll`, `Opponent`, `Centered` — serializes as string |
| `CubeAction` | enum: `NoDouble`, `Double`, `Take`, `Pass` — a player's cube response, serializes as string. Beaver/raccoon deliberately not yet members (see XML `<remarks>` on the type); enums extend without disturbing existing members. |
| `AnalysisMode` | enum: `Unknown`, `Evaluation`, `Rollout`, `BookRollout` — how an XG analysis's numbers were produced; the mode axis of the two-axis depth taxonomy, serializes as string. Always paired with `AnalysisLevel`; together the pair is the taxonomy SSOT for depth filtering, replacing the retired flat `AnalysisDepthClass` (whose single axis could not represent book entries carrying separate moves and cube rollout levels). Classification is producer-side (ConvertXgToJson_Lib stamps both axes). `Unknown = 0` deliberately — unstamped/legacy JSON, including JSON stamped with the retired flat class (unrecognized property, ignored on read), deserializes to it. `BookRollout` is a book hit — rollout-derived, with parameters in the book database rather than the source file; `BookRollout` + `AnalysisLevel.Unknown` is the graceful-degradation stamp (no book DB available at conversion time, or a V1-book hit recording no levels). The UI renders modes in declaration order. Every member carries a `[Description]` display label (XgFilter_Lib's `EnumLabel.ToLabel` throws without one). Trial counts stay label-only. |
| `AnalysisLevel` | enum: `Unknown`, `Ply1`–`Ply7`, `XgRoller`, `XgRollerPlus`, `XgRollerPlusPlus` — the evaluation level; the level axis paired with `AnalysisMode`, serializes as string. For `Evaluation` it is the level of the evaluation itself; for the rollout-family modes it is the inner evaluation level — checker rows carry the inner moves level, cube rows the inner cube level (a single rollout can use different levels for the two; which one a row gets is the producer's concern, the semantics are owned here). Rollout-family modes never pair with a Roller-family level on checker rows but can on cube rows (the shipped book DB contains cube rollout levels of XG Roller). `Unknown = 0` deliberately — unstamped/legacy JSON deserializes to it. Declared in ascending-rigor order, plies below the Roller family; the UI renders levels in declaration order (informational, not contractual — filter by membership; `DepthRank` orders). Every member carries a `[Description]` display label; variants sharing a level keep their finer identity only in the label strings ("3-ply red" is `Ply3`). |
| `CubeDecisionPair` | `readonly record struct (CubeAction Doubler, CubeAction Taker)` — a complete cube decision as two atomic actions. Validated on construction via the positional-record idiom: `Doubler` ∈ {`NoDouble`, `Double`}, `Taker` ∈ {`Take`, `Pass`}; a cross-half value throws `ArgumentOutOfRangeException`. The verdict aggregate (pair → correct/wrong) is intentionally absent and returns later with `CubeVerdict`. `default` is non-meaningful — see Pitfalls. |
| `DiceRoll` | `readonly record struct` — a dice roll in canonical unordered form: `High`/`Low`, each a validated face 1–6. The constructor accepts either order and canonicalizes (the XG parser stamps dice in rolled order, so both `31` and `13` reach it for a 3-1); canonicalization is single-sourced here, nowhere downstream, and record-struct equality over the canonical form makes 3-1 ≡ 1-3 automatic. `IsDouble`; `Parse`/`TryParse` of the two-digit token form (`IParsable` + `ISpanParsable`, accepting either spelling); `ToString()` → canonical high-first token (`"31"`). Ordered (`IComparable<DiceRoll>` + comparison operators via `IComparisonOperators`) ascending by `High` then `Low` — ascending canonical token. `All` is the SSOT enumeration of the 21 distinct rolls in that order (doubles included). JSON round-trips as the token via bundled `DiceRollJsonConverter`. `default` is non-meaningful (faces 0 — see Pitfalls); "no roll" is `DiceRoll?` null, per `IDecisionFilterData.Dice`. |
| `Move` | `readonly record struct (FrPt, ToPt)`. Encodes regular / bear-off / hit moves via the sign of `ToPt` — see "Move encoding" below. |
| `Play` | mutable `struct`, fixed 4-slot buffer of `Move`. Default value is empty (`Count == 0`). Intent-level construction via `Play.Create` — **five overloads**: four fixed-arity (`Create(m0)` … `Create(m0, m1, m2, m3)`), which construct at parity with the incremental `Add` spelling, and `Create(params ReadOnlySpan<Move>)` for moves already in a span or array (> 4 moves throws `ArgumentException`), which is also the `[CollectionBuilder]` target, so collection expressions build plays — `Play p = [new(13, 10), new(10, 8)];`, with `[]` the empty play, a forced pass. The span overload carries `[OverloadResolutionPriority(-1)]` so a literal argument list binds fixed-arity at every arity including one; see Benchmarks for what that buys. `Add`/`RemoveLast` remain the incremental build primitives for move-generation recursion; every construction path writes slots through one private seam. Read idiom is `foreach` (allocation-free pattern enumerator over a value copy; deliberately no `IEnumerable<T>` — it would box) or the indexer. Equality / hash delegate to `ToCanonical()` — notation-level equivalence, see "Canonical play form" below. Serialized as a JSON array of `Move` via `PlayJsonConverter` (the private buffer fields are not visible to default property-based serialization); the raw move sequence round-trips exactly — canonicalization affects equality, never storage. |
| `PlayChain` | `readonly record struct (FrPt, ToPt)` — one chain of a `CanonicalPlay`: a single checker's collapsed trajectory for the turn. Same sign-encoding as `Move`, but may span several dice. A hit only ever sits at a chain's endpoint (an intermediate hit splits the trajectory into two chains). |
| `CanonicalPlay` | `readonly struct`, fixed 4-slot buffer of `PlayChain` + `Count`, full equality surface (`IEquatable`, `==`/`!=`, hash). The canonical chain form of a `Play` and the single source of play equivalence. Only produced by `Play.ToCanonical()` — no public constructor path, so every instance is guaranteed canonical. `default` is the canonical form of the empty play (meaningful). |
| `PlayCandidate` | `MoveNotation`, `Play`, `Depth`, `DepthAbbreviation`, `DepthRank`, `AnalysisMode`, `AnalysisLevel`, `Equity`, `EquityLoss` (non-nullable, `0.0` = best), `WinPct?`, `WinGammonPct?`, `WinBgPct?`, `LosePct?`, `LoseGammonPct?`, `LoseBgPct?`. `MoveNotation` is the display string; `Play` is the structural sequence of moves (complement, not duplicate — used for structural comparison and downstream consumers). `EquityLoss == 0.0` is the test for "is this a best play"; `DecisionData.BestPlayIndex` names the canonical single best when one is needed. |
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

`Play.ToCanonical()` produces a `CanonicalPlay` — the single source of play
equivalence. `Play.Equals` / `GetHashCode` / `==` delegate to it, so play
equality is **notation-level, not encoding-level**: insensitive to move order
and to how a checker's trajectory is decomposed into single-die hops, fully
sensitive to hits.

Collapse semantics (the XG chain-collapse rules, previously encoded
display-side in `BgMoveGen.MoveNotationFormatter` — the rule now lives here
and the formatter renders from this form):

- Consecutive single-die hops of one checker merge into a single
  `PlayChain` recording source and final landing point:
  `{(13,10),(10,8)}` and `{(13,8)}` both canonicalize to the chain `13/8`.
- **Hit-visibility rule** (the one predicate gating every join): two
  segments joining at point P may merge only when the segment *ending* at P
  does not hit there — otherwise the hit marking that now-intermediate point
  would be lost. So `13/10*/8` splits into chains `{13/10*, 10/8}` (≠ `13/8`),
  while `13/10 10/8*` collapses to `13/8*`. A hit only ever sits at a chain's
  endpoint.
- Moves are pre-sorted (FrPt desc, |ToPt| desc, hit first) so the same
  multiset of moves always canonicalizes identically; chains are emitted
  sorted the same way. Matching is bidirectional with a fixpoint fuse pass,
  keeping the whole `Move` encoding domain deterministic (bar entry,
  bear-off, doubles, out-of-order legs, even physically-impossible zigzags).
- Duplicate chains (doubles moving two checkers along the same route) are
  kept as repeated entries — `"(2)"` grouping, `"bar"`/`"off"` labels and all
  other notation rendering stay in `BgMoveGen`'s formatter.

### BoardState

Mutable backgammon position. `int[26] Points` plus `int HighPointOccupied`.
Layout matches `PositionData.Mop` / `IDecisionFilterData.Board`:
`Points[0]` = opponent bar, `Points[1..24]` = playing surface, `Points[25]` =
on-roll bar; positive = on-roll, negative = opponent. On-roll moves
high index → low; opponent moves low → high. Borne-off counts are not
tracked — checkers leaving the board simply disappear.

Three layers of mutation, in increasing scope:

- **`ApplyMove(Move)` / `UndoMove(Move)`** — hot-path primitives, zero
  allocation, used by `BgMoveGen`'s move generator to recurse through
  candidate plays. Maintain `HighPointOccupied` incrementally: apply
  scans down only when emptying the highest point; undo raises
  `HighPointOccupied` when a move's `FrPt` exceeds the current high.
  No legality validation — that's the move generator's job.

- **`ApplyPlay(Play)`** — turn-boundary primitive. Applies every move
  in the play (using `play.Count`) then flips perspective so the state
  is re-expressed from the next mover's POV. Empty plays still flip —
  they represent a forced pass. This is the only public way to advance
  past a turn boundary; callers reasoning in on-roll POV never need to
  flip explicitly.

- **`Flip()`** — `private`. Implementation mechanic for `ApplyPlay` and
  `FlippedCopy()`. Negates and reverses the array (point `i` ↔ point
  `25-i`, swapping the bars in the process), then recomputes
  `HighPointOccupied` from scratch. Stays private: live-state flips
  happen only inside `ApplyPlay`, so callers advancing state always
  reason in on-roll POV. `FlippedCopy()` is the public flipped-*copy*
  primitive for querying a position from the other player's frame
  (e.g. cube-response evaluation) without advancing state — the
  receiver is untouched.

Factories: `Standard()`, `Nackgammon()`, `Bg960(int? seed = null)` for
the three starting variants. `FromMop(IReadOnlyList<int>)` and `ToMop()`
bridge to/from the 26-element on-roll-relative point array used by
`PositionData.Mop`. `Copy()` is a deep copy; `FlippedCopy()` is a deep
copy re-expressed from the opponent's perspective (an involution —
flipping twice reproduces the original). `RecalcHighPoint()` is
public for callers that mutate `Points` directly.

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
  position (≤15 checkers per side, per-point counts within ±15, own-side
  bars only, non-empty board). `BoardState.FromMop` tolerates pseudoboards
  because a general board utility should; `ProblemKey` identifies real
  analysed decisions, so a violation is corruption and corruption gets no
  key.

JSON shape: round-trips as the canonical string via the bundled
`ProblemKeyJsonConverter` (type-level `[JsonConverter]` attribute on
`ProblemKey`). Unlike `DecisionIdJsonConverter` it also implements the
property-name overloads, because the stats document keys its per-problem
map by `ProblemKey` and `Dictionary<ProblemKey, …>` must round-trip
without a consumer-side key converter.

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

All four throw `InvalidOperationException` when `IsCube` is false — they
are only meaningful on cube decisions, and silent zero / default returns
on play decisions would mask misuse. The two error methods further
throw `ArgumentOutOfRangeException` when the action argument is from the
wrong half (e.g. `Take` or `Pass` passed to `DoublerActionError`).

Tie-breaking follows the renderer's existing convention so a downstream
consumer that collapses the inline cube derivation into calls to these
helpers preserves behaviour: `NoDouble` on the doubler-equity tie, `Pass`
on `DoubleTakeEquity == 1`.

The two computed properties (`BestDoublerAction`, `BestTakerAction`)
carry `[JsonIgnore]` so `System.Text.Json` does not invoke their throwing
getters when serialising play decisions. The error methods are
intrinsically not serialised because they take parameters.

An aggregate verdict layer was removed in the cube-surface rebuild and is
slated to return later on a cleaner footing; the umbrella `INSTRUCTIONS.md`
Deferred section and git history carry that design.

### Composite type

`BgDecisionData = PositionData + DecisionData + DescriptiveData + PlayOutcomeData`.
Implements `IDecisionFilterData` via forwarding properties. `Board` returns
`Position.Mop` directly. `AfterBestBoard` / `AfterPlayerBoard` forward to
`Outcome.AfterBestBoard` / `Outcome.AfterPlayerBoard` — raw, with no conditional
on `IsCube`. The "empty for cube decisions" invariant is producer-enforced:
whoever constructs `BgDecisionData` leaves `Outcome` at its default (empty lists).
`FilterError` routes to `UserDoubleError ?? UserTakeError` for cube decisions,
otherwise `UserPlayError`. `AnalysisMode` / `AnalysisLevel` derive per the
`DecisionRow.AnalysisDepth` convention: cube decisions report
`Decision.CubeAnalysisMode` / `Decision.CubeAnalysisLevel`, checker plays
report the `BestPlayIndex` candidate's pair (`Unknown`/`Unknown` when
`BestPlayIndex` does not identify a candidate — empty `Plays`, or an
out-of-range index from malformed data); a shared private lookup guarantees
both axes read the same candidate. `Dice` forwards `Decision.Dice` in
canonical `DiceRoll` form — null for cube decisions, fail-loud on malformed
stored faces — and is `[JsonIgnore]`d so the throwing derivation never runs
during serialization and `Decision.Dice` stays the JSON wire form.

### After-boards (PlayOutcomeData)

Two 26-element boards derived from the play choices of a decision:
`AfterBestBoard` (state after the best play) and `AfterPlayerBoard` (state
after the player's actual play). Same layout as `PositionData.Mop`, but **POV
is flipped** — after a play the opponent is on roll, so the decision-maker's
checkers are stored as *negative* values and the opponent's as positive. Both
lists are empty for cube decisions. Consumers of `IDecisionFilterData` must
check `IsCube` before using these boards. This is the substrate for
`XgFilter_Lib`'s three-board `IPlayTypeClassifier` contract.

### DecisionRow

Flat CSV export record. Sibling output to `BgDecisionData` — both are produced
by the XG → JSON conversion pipeline, for different consumers. Implements
`IDecisionFilterData` directly (no composition). Carries its own CSV methods
(`ToCsvLine`, `CsvHeader`, private `CsvEscape`). `Board`, `AfterBestBoard`, and
`AfterPlayerBoard` are all stored as `IReadOnlyList<int>` (26 elements each,
same layout as `PositionData.Mop` — with flipped POV on the after-boards).
All three board fields serialize to JSON but are **excluded from CSV output**,
as are `AnalysisMode` / `AnalysisLevel` (the taxonomy form of
`AnalysisDepth`, which remains the CSV depth column). `Dice` is derived from
the int `Roll` column (`Roll == 0` → null; malformed digits fail loud) and is
`[JsonIgnore]`d like `IsCube` / `MatchScore` — `Roll` stays the wire form on
both CSV and JSON.

`IsJacoby` (`bool?`) is stored, not derived — the tri-state fact
`PositionData.IsJacoby` owns, carried here because the CSV shape spells it
(`halheinrich/backgammon#121`). It reaches CSV the way `IsCrawford` does:
through the computed `MatchScore` token, as an in-grammar suffix on the money
score. A money row is `moneyJ` or `moneyNJ`; a money row whose rule is unknown
(`IsJacoby` `null`) is the bare `money`, which is deliberately neither
rule-bearing token. No CSV column is added — the column set and count are
unchanged. Like `IsCrawford`, it also serializes to JSON.

### Mop layout

26-element `IReadOnlyList<int>` from the on-roll player's perspective:

- `[0]` = opponent's bar (≤ 0)
- `[1–24]` = points 1–24
- `[25]` = on-roll player's bar (≥ 0)
- Positive = on-roll; negative = opponent

The same layout is used by both `PositionData.Mop` and
`IDecisionFilterData.Board`.

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
    bool? IsJacoby { get; }                       // tri-state; null on a money record matches neither money token
    int MoveNumber { get; }                       // 1-based within the game
    bool IsStandardStart { get; }                 // false for non-standard openings
    AnalysisMode AnalysisMode { get; }            // cube analysis for cubes, best-play candidate for checkers
    AnalysisLevel AnalysisLevel { get; }          // level axis of the same analysis AnalysisMode reports
    DiceRoll? Dice { get; }                       // canonical roll; null for cube decisions
    double? FilterError { get; }                  // ≥ 0 or null
    IReadOnlyList<int> Board { get; }             // 26 elements, see Mop layout
    IReadOnlyList<int> AfterBestBoard { get; }    // POV flipped; empty for cubes
    IReadOnlyList<int> AfterPlayerBoard { get; }  // POV flipped; empty for cubes
}

public class BgDecisionData : IDecisionFilterData
{
    public required DecisionId Id { get; init; }    // producer-stamped; throws at ctor if omitted
    public PositionData    Position    { get; init; }
    public DecisionData    Decision    { get; init; }
    public DescriptiveData Descriptive { get; init; }
    public PlayOutcomeData Outcome     { get; init; }
    // IDecisionFilterData members implemented as forwarding properties.
}

public class PlayOutcomeData { /* AfterBestBoard, AfterPlayerBoard (each IReadOnlyList<int>) */ }

public sealed class DecisionRow : IDecisionFilterData
{
    public required DecisionId Id { get; init; }    // producer-stamped; throws at ctor if omitted
    // Flat init-only properties — see DecisionRow.cs for the full set.
    public bool? IsJacoby { get; init; }  // stored tri-state; suffixes the money MatchScore token
    public string MatchScore { get; }   // computed from needs/Crawford/length/Jacoby
    public static string CsvHeader { get; }
    public string ToCsvLine();
    // IsCube, MatchScore, and FilterError are [JsonIgnore]d (computed /
    // derived). The three board lists (Board, AfterBestBoard,
    // AfterPlayerBoard) and AnalysisMode / AnalysisLevel serialize to JSON
    // but are excluded from CSV output.
    // Id is JSON-serialized (as canonical string) but excluded from CSV
    // output — CSV columns are listed explicitly.
}

public class PositionData    { /* init-only properties per Architecture table */ }
public class DescriptiveData { /* init-only properties per Architecture table */ }
public class PlayCandidate   { /* init-only properties per Architecture table */ }

public class DecisionData
{
    // Init-only properties per Architecture table (Dice, Plays, BestPlayIndex,
    // UserPlayIndex, UserPlayError?, IsCube, CubeDepth, CubeDepthAbbreviation,
    // CubeDepthRank, CubeAnalysisMode, CubeAnalysisLevel, NoDoubleEquity,
    // DoubleTakeEquity, the pct fields, ProbOfOpponentErrorJustifyingDouble,
    // UserDoubleError?, UserTakeError?).

    // Cube-decision scoring (computed; throw InvalidOperationException when IsCube is false).
    [JsonIgnore] public CubeAction  BestDoublerAction { get; }   // Double or NoDouble
    [JsonIgnore] public CubeAction  BestTakerAction   { get; }   // Take or Pass

    public double DoublerActionError(CubeAction action);          // 0 if action == BestDoublerAction;
                                                                  // throws ArgumentOutOfRangeException on Take/Pass.
    public double TakerActionError(CubeAction action);            // 0 if action == BestTakerAction;
                                                                  // throws ArgumentOutOfRangeException on Double/NoDouble.
}

public readonly record struct Move(int FrPt, int ToPt);

public readonly record struct PlayChain(int FrPt, int ToPt);

[CollectionBuilder(typeof(Play), nameof(Create))]
public struct Play : IEquatable<Play>
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
    public CanonicalPlay ToCanonical();           // readonly; equality SSOT
    public Enumerator GetEnumerator();            // readonly; foreach pattern, allocation-free
    public struct Enumerator { /* Current, MoveNext() */ }
    public bool Equals(Play other);               // canonical equivalence
    public override bool Equals(object? obj);
    public override int GetHashCode();
    public static bool operator ==(Play left, Play right);
    public static bool operator !=(Play left, Play right);
}

public readonly struct CanonicalPlay : IEquatable<CanonicalPlay>
{
    // No public constructor path — produced by Play.ToCanonical() only,
    // so every instance is guaranteed canonical. default == empty play's form.
    public int Count { get; }                     // 0-4 chains
    public PlayChain this[int index] { get; }     // canonical order (FrPt desc)
    public bool Equals(CanonicalPlay other);
    public override bool Equals(object? obj);
    public override int GetHashCode();
    public static bool operator ==(CanonicalPlay left, CanonicalPlay right);
    public static bool operator !=(CanonicalPlay left, CanonicalPlay right);
}

public class BoardState
{
    public readonly int[] Points = new int[26];   // layout matches PositionData.Mop
    public int HighPointOccupied;                 // 1–25, or 0 if no on-roll checkers

    public BoardState();                          // empty board (all zeros, HighPointOccupied = 0)

    // Factories
    public static BoardState Standard();
    public static BoardState Nackgammon();
    public static BoardState Bg960(int? seed = null);

    // Mop bridge
    public static BoardState FromMop(IReadOnlyList<int> mop);
    public IReadOnlyList<int> ToMop();

    // Maintenance
    public BoardState Copy();
    public BoardState FlippedCopy();              // copy from opponent's perspective; receiver untouched
    public void RecalcHighPoint();

    // Apply / undo (hot-path primitives)
    public void ApplyMove(Move move);
    public void UndoMove(Move move);

    // Turn boundary (apply-all + flip, atomic)
    public void ApplyPlay(Play play);

    // Derived
    public int PipCount { get; }
    public int OpponentPipCount { get; }
    public bool IsRace { get; }
}

public enum CubeOwner { OnRoll, Opponent, Centered }

public enum CubeAction { NoDouble, Double, Take, Pass }

// The two-axis depth taxonomy: mode (how the numbers were produced) ×
// level (the evaluation level — for rollout-family modes, the inner level).
// Unknown = 0 deliberately on both: unstamped/legacy JSON deserializes to
// it. BookRollout + AnalysisLevel.Unknown is the graceful-degradation stamp
// (no book DB, or a V1-book hit). The UI renders both enums in declaration
// order; level ordering is informational — filter by membership; DepthRank
// orders. Every member of both carries a [Description] display label.
public enum AnalysisMode
{
    Unknown, Evaluation, Rollout, BookRollout
}

public enum AnalysisLevel
{
    Unknown, Ply1, Ply2, Ply3, Ply4, Ply5, Ply6, Ply7,
    XgRoller, XgRollerPlus, XgRollerPlusPlus
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
```

Serialization contract: round-trips cleanly through `System.Text.Json` —
no consumer-side converter registration required. `CubeOwner`, `CubeAction`,
`AnalysisMode`, and `AnalysisLevel` bundle `JsonStringEnumConverter` via attribute;
`Play` bundles `PlayJsonConverter`; `DecisionId` bundles
`DecisionIdJsonConverter`; `DiceRoll` bundles `DiceRollJsonConverter`;
`ProblemKey` bundles `ProblemKeyJsonConverter` (the only one implementing
the property-name overloads, so it also works as a dictionary key). Tested
without any options-level registration in `BgDecisionDataSerializationTests`,
`DecisionRowSerializationTests`, `DiceRollTests`, and `ProblemKeyTests`.

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

- **`DecisionId.Filename` must not contain `':'`.** The canonical-form
  separator is the same character used to discriminate the two shapes at
  parse time. The guard is **symmetric** on both subtypes — without it
  on `XgpDecisionId`, a `.xgp` filename containing `':'` would parse back
  as a (malformed) `XgDecisionId`. Both ctors throw `ArgumentException`;
  `TryParse` returns false on the same input. Documented once on the
  base; enforced by each derived ctor via a shared private-protected
  helper.
- **`Id` is `required` on `BgDecisionData` and `DecisionRow`.** Omitting
  it at construction is a compile error, not a runtime null. Producers
  (`ConvertXgToJson_Lib`'s `Build*` sites) must stamp it; tests that
  construct decision records directly must set it. Aligns with the
  "producer-supplied identity" contract — no silent default IDs.
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
- **`IDecisionFilterData.Board` must return the 26-element layout.** New
  implementers of the interface must match the `PositionData.Mop` contract
  exactly — `XgFilter_Lib` filters assume it.
- **After-boards use flipped POV.** `AfterBestBoard` / `AfterPlayerBoard` use
  the same 26-element layout as `Board` but the opponent is on roll after a
  play, so the decision-maker's checkers are *negative* and the opponent's
  are positive. Code that forgets this mirrors the after-boards silently.
- **After-boards are empty for cube decisions.** The "empty list" contract
  is producer-enforced (not guarded in the forwarding implementation on
  `BgDecisionData`). Consumers of the interface must check `IsCube` before
  interpreting these boards. Producers must leave `PlayOutcomeData` at its
  default for cube decisions.
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
- **`Play` equality is notation-level, not encoding-level.** Equality /
  hash / `==` delegate to `ToCanonical()`: insensitive to move order *and*
  to hop decomposition (`{(13,10),(10,8)}` equals `{(13,8)}`; a one-hop
  overshoot bear-off equals its two-hop decomposition), but **fully
  hit-sensitive** (`13/10*/8` ≠ `13/8`). Do not rely on `Equals` to
  distinguish different encodings of the same play — compare move sequences
  directly if encoding identity matters. Conversely, do rely on it to
  distinguish hitting from non-hitting plays: the old hit-stripped
  `DeduplicationKey()` (which compared them equal) is gone, deliberately —
  it let a hit-less encoding of a hitting play validate as legal and apply
  without barring the blot.
- **`Play` requires its bundled `JsonConverter`.** Default property-based
  serialization only sees `Count`, losing every move. The
  `[JsonConverter(typeof(PlayJsonConverter))]` attribute is intrinsic to
  the type — do not strip it, and do not register a different converter
  for `Play` in consumer-side options without understanding the
  consequence.
- **`BoardState` is mutable; `HighPointOccupied` desyncs on raw
  mutation.** The apply/undo helpers maintain `HighPointOccupied`
  incrementally; raw `Points[i] = …` writes do not. Call
  `RecalcHighPoint()` after any direct point-array mutation, or use
  `FromMop` (which recomputes for you). The contract is intentional —
  hot-path move generation needs zero-overhead apply/undo, so the
  per-write maintenance lives in the helpers, not in property setters.
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
  `FlippedCopy()` — never re-encode negate-and-reverse in a consumer.
- **`AnalysisMode` and `AnalysisLevel` always travel as a pair, and
  `Unknown`/`Unknown` is data, not an error.** Both zero values are
  deliberate: unstamped construction sites, JSON written before the pair
  existed, and JSON stamped with the retired flat `AnalysisDepthClass`
  (an unrecognized property, ignored on read) all deserialize to
  `Unknown`/`Unknown` — "depth not recorded". `BookRollout` +
  `AnalysisLevel.Unknown` is additionally a live producer stamp (book hit
  without recoverable levels), so code must not treat `AnalysisLevel.Unknown`
  as implying `AnalysisMode.Unknown`. Declaration order is what the UI
  renders and, for `AnalysisLevel`, is informational ascending rigor — depth
  filtering must use membership; `DepthRank` / `CubeDepthRank` remain the
  ordering surface for consumers that compare depths. Do not strip a member's
  `[Description]` label: downstream label readers (XgFilter_Lib's
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
  All four (two computed properties + two methods) guard on `IsCube` and
  throw `InvalidOperationException` on play decisions — they encode a
  cube-only policy and silent zeros would mask misuse. Callers in
  mixed-decision contexts must check `IsCube` first. The two
  computed properties carry `[JsonIgnore]` so `System.Text.Json` does
  not invoke their throwing getters during serialisation; do not strip
  those attributes.
- **Cube-scoring atomic-action methods reject the wrong half.**
  `DoublerActionError(CubeAction)` accepts only `Double` / `NoDouble`;
  `TakerActionError(CubeAction)` accepts only `Take` / `Pass`. The
  other half throws `ArgumentOutOfRangeException`.
- **`default(CubeDecisionPair)` is non-meaningful.** A `record struct`
  cannot run its half-guards on `default`, so `default(CubeDecisionPair)`
  is `(NoDouble, NoDouble)` — whose `Taker` is not a valid taker action.
  Construct pairs explicitly; do not treat `default` as a "no decision"
  sentinel. This is the standard value-type caveat, shared with `Play`
  and `DiceRoll`.

## Subproject-internal next steps

Cross-cutting work (consumer migrations, downstream refactors) is tracked in
the umbrella `INSTRUCTIONS.md` "Next up" / "Deferred" sections, not here.

- **`DecisionRow` factory split.** Test-helper duplication around cube-row
  construction (`DecisionRowBuilder.Build` / `BuildCube` shapes that consumers
  re-implement) hints at missing factories on `DecisionRow` for the common
  shapes (checker row, cube row). Library gap; consumer glue.
