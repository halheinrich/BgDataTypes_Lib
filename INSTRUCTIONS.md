# BgDataTypes_Lib

> Collaboration contract: [`../AGENTS.md`](../AGENTS.md)
> Umbrella status & dependency graph: [`../INSTRUCTIONS.md`](../INSTRUCTIONS.md)
> Mission & principles: [`../VISION.md`](../VISION.md)

## Stack

C# / .NET 10 / Class Library / xUnit. Pure data types — no parsing, no rendering, no I/O beyond `System.Text.Json` serialization.

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
that need converters (`CubeOwner`, `CubeAction`, `AnalysisDepthClass`,
`Play`, `DecisionId`) each bundle their own `[JsonConverter]` attribute so
consumers do not have to register converters on their
`JsonSerializerOptions`.

## Directory tree

```
BgDataTypes_Lib.slnx
Directory.Packages.props
BgDataTypes_Lib/
  BgDataTypes_Lib.csproj
  AnalysisDepthClass.cs     — enum (string-serialized): analysis depth taxonomy
  BgDecisionData.cs         — composite: Position + Decision + Descriptive + Outcome
  BoardState.cs             — mutable int[26] + HighPointOccupied + apply/undo/ApplyPlay
  CanonicalPlay.cs          — canonical chain form of Play; the play-equivalence SSOT
  CubeAction.cs             — enum (string-serialized)
  CubeDecisionPair.cs       — readonly record struct (Doubler, Taker), validated halves
  CubeOwner.cs              — enum (string-serialized)
  DecisionData.cs
  DecisionId.cs             — abstract record + XgpDecisionId / XgDecisionId, IParsable + ISpanParsable
  DecisionIdJsonConverter.cs — canonical-string JSON converter for DecisionId
  DecisionRow.cs            — flat CSV export record
  DescriptiveData.cs
  IDecisionFilterData.cs    — shared filter contract
  Move.cs                   — (FrPt, ToPt) record struct
  Play.cs                   — fixed 4-slot Move buffer
  PlayCandidate.cs
  PlayChain.cs              — (FrPt, ToPt) record struct: one collapsed trajectory
  PlayJsonConverter.cs      — JSON-array converter for Play
  PlayOutcomeData.cs        — after-boards derived from play choices
  PositionData.cs
BgDataTypes_Lib.Tests/
  BgDataTypes_Lib.Tests.csproj
  AnalysisDepthClassTests.cs
  BgDecisionDataFilterErrorTests.cs
  BgDecisionDataSerializationTests.cs
  BoardStateTests.cs
  CanonicalPlayTests.cs
  CubeActionTests.cs
  CubeDecisionPairTests.cs
  DecisionDataCubeScoringTests.cs
  DecisionIdTests.cs
  DecisionRowSerializationTests.cs
  MoveTests.cs
  PipCountTests.cs
  PlayTests.cs
  RaceTests.cs
```

## Architecture

Composite and category types are `class` with `init`-only properties; the
move primitives `Move` (`readonly record struct`) and `Play` (mutable
`struct`) are value types for hot-path zero-alloc reasons inherited from
their move-generation origins. `BoardState` is a `class` but mutable —
the one deliberate exception (see "Mutability exception" below).
Serialization uses `System.Text.Json` with bundled `[JsonConverter]`
attributes: `JsonStringEnumConverter` on `CubeOwner`, `CubeAction`, and
`AnalysisDepthClass`, `PlayJsonConverter` on `Play`, and
`DecisionIdJsonConverter` on `DecisionId`. Consumers do not need to
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
| `PositionData` | `Mop`, `OnRollNeeds`, `OpponentNeeds`, `OnRollPipCount`, `OpponentPipCount`, `CubeSize`, `CubeOwner`, `IsCrawford` |
| `DecisionData` | `Dice`, `Plays`, `BestPlayIndex`, `UserPlayIndex`, `UserPlayError?`, `IsCube`, `CubeDepth`, `CubeDepthAbbreviation`, `CubeDepthRank`, `CubeDepthClass`, cube equity/pct fields, `UserDoubleError?`, `UserTakeError?` |
| `DescriptiveData` | `MatchLength`, `OnRollName`, `OpponentName`, `Title`, `Date`, `Event`, `SourceFile`, `MoveNumber`, `IsStandardStart` |
| `PlayOutcomeData` | `AfterBestBoard`, `AfterPlayerBoard` |

### Shared types

| Type | Notes |
|---|---|
| `CubeOwner` | enum: `OnRoll`, `Opponent`, `Centered` — serializes as string |
| `CubeAction` | enum: `NoDouble`, `Double`, `Take`, `Pass` — a player's cube response, serializes as string. Beaver/raccoon deliberately not yet members (see XML `<remarks>` on the type); enums extend without disturbing existing members. |
| `AnalysisDepthClass` | enum: `Unknown`, `Book`, `Ply1`–`Ply7`, `XgRoller`, `XgRollerPlus`, `XgRollerPlusPlus`, `Rollout`, `RolloutPly1`–`RolloutPly7` — depth class of an XG analysis, serializes as string. The taxonomy SSOT for depth filtering; classification is producer-side (ConvertXgToJson_Lib maps XG level codes onto it). `Unknown = 0` deliberately — unstamped/legacy JSON deserializes to it. Declared in ascending-rigor order mirroring the producer's ranks (informational, not contractual — filter by membership; `DepthRank` orders). Rollout inner ply is a taxonomy axis: `RolloutPly1`–`RolloutPly7` mirror the producer's 100 + inner-ply rank ladder, with `Rollout` as the floor for a rollout whose inner ply is unknown (the producer's no-context rank-100 sentinel). Every member carries a `[Description]` display label (XgFilter_Lib's `EnumLabel.ToLabel` throws without one). Variants sharing a class keep their finer identity only in the label strings ("3-ply red" is `Ply3`; Book V1/V2 are both `Book`; rollout trial counts are label-only). |
| `CubeDecisionPair` | `readonly record struct (CubeAction Doubler, CubeAction Taker)` — a complete cube decision as two atomic actions. Validated on construction via the positional-record idiom: `Doubler` ∈ {`NoDouble`, `Double`}, `Taker` ∈ {`Take`, `Pass`}; a cross-half value throws `ArgumentOutOfRangeException`. The verdict aggregate (pair → correct/wrong) is intentionally absent and returns later with `CubeVerdict`. `default` is non-meaningful — see Pitfalls. |
| `Move` | `readonly record struct (FrPt, ToPt)`. Encodes regular / bear-off / hit moves via the sign of `ToPt` — see "Move encoding" below. |
| `Play` | mutable `struct`, fixed 4-slot buffer of `Move`. Default value is empty (`Count == 0`). Equality / hash delegate to `ToCanonical()` — notation-level equivalence, see "Canonical play form" below. Serialized as a JSON array of `Move` via `PlayJsonConverter` (the private buffer fields are not visible to default property-based serialization); the raw move sequence round-trips exactly — canonicalization affects equality, never storage. |
| `PlayChain` | `readonly record struct (FrPt, ToPt)` — one chain of a `CanonicalPlay`: a single checker's collapsed trajectory for the turn. Same sign-encoding as `Move`, but may span several dice. A hit only ever sits at a chain's endpoint (an intermediate hit splits the trajectory into two chains). |
| `CanonicalPlay` | `readonly struct`, fixed 4-slot buffer of `PlayChain` + `Count`, full equality surface (`IEquatable`, `==`/`!=`, hash). The canonical chain form of a `Play` and the single source of play equivalence. Only produced by `Play.ToCanonical()` — no public constructor path, so every instance is guaranteed canonical. `default` is the canonical form of the empty play (meaningful). |
| `PlayCandidate` | `MoveNotation`, `Play`, `Depth`, `DepthAbbreviation`, `DepthRank`, `DepthClass`, `Equity`, `EquityLoss` (non-nullable, `0.0` = best), `WinPct?`, `WinGammonPct?`, `WinBgPct?`, `LosePct?`, `LoseGammonPct?`, `LoseBgPct?`. `MoveNotation` is the display string; `Play` is the structural sequence of moves (complement, not duplicate — used for structural comparison and downstream consumers). `EquityLoss == 0.0` is the test for "is this a best play"; `DecisionData.BestPlayIndex` names the canonical single best when one is needed. |
| `DecisionId` | `abstract record` + two sealed records: `XgpDecisionId(Filename)` and `XgDecisionId(Filename, Game, MoveNumber, IsCube)`. Stable, persistent identifier for a single decision within an XG-family source file. Canonical string form: `"file.xgp"` (Xgp) or `"file.xg:g{N}:m{N}:{cube\|play}"` (Xg). Implements `IParsable<DecisionId>` + `ISpanParsable<DecisionId>`. Filename invariant: `':'` is forbidden on **both** subtypes (the parse dispatcher discriminates by `':'` presence, so an unguarded Xgp filename with `':'` would lose round-trip). JSON-serialised as the canonical string via bundled `DecisionIdJsonConverter`. Set as `required` on both `BgDecisionData` and `DecisionRow`. |

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
otherwise `UserPlayError`. `AnalysisDepthClass` derives per the
`DecisionRow.AnalysisDepth` convention: cube decisions report
`Decision.CubeDepthClass`, checker plays report the `BestPlayIndex`
candidate's `DepthClass` (`Unknown` when `BestPlayIndex` does not identify
a candidate — empty `Plays`, or an out-of-range index from malformed data).

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
as is `AnalysisDepthClass` (the taxonomy form of `AnalysisDepth`, which
remains the CSV depth column).

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
    int MoveNumber { get; }                       // 1-based within the game
    bool IsStandardStart { get; }                 // false for non-standard openings
    AnalysisDepthClass AnalysisDepthClass { get; } // cube analysis for cubes, best-play candidate for checkers
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
    public string MatchScore { get; }   // computed from needs/Crawford/length
    public static string CsvHeader { get; }
    public string ToCsvLine();
    // IsCube, MatchScore, and FilterError are [JsonIgnore]d (computed /
    // derived). The three board lists (Board, AfterBestBoard,
    // AfterPlayerBoard) and AnalysisDepthClass serialize to JSON but are
    // excluded from CSV output.
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
    // CubeDepthRank, CubeDepthClass, NoDoubleEquity, DoubleTakeEquity, the pct
    // fields, ProbOfOpponentErrorJustifyingDouble, UserDoubleError?, UserTakeError?).

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

public struct Play : IEquatable<Play>
{
    public int Count { get; private set; }
    public Move this[int index] { get; }          // readonly
    public void Add(Move move);
    public void RemoveLast();
    public Play Snapshot();                       // readonly
    public CanonicalPlay ToCanonical();           // readonly; equality SSOT
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

// Ascending-rigor order mirroring the producer's ranks (informational —
// filter by membership; DepthRank orders). Unknown = 0 deliberately:
// unstamped/legacy JSON deserializes to it. Rollout is the floor of the
// rollout tier (inner ply unknown); RolloutPly1-RolloutPly7 mirror the
// producer's 100 + inner-ply ladder. Every member carries a [Description]
// display label.
public enum AnalysisDepthClass
{
    Unknown, Book, Ply1, Ply2, Ply3, Ply4, Ply5, Ply6, Ply7,
    XgRoller, XgRollerPlus, XgRollerPlusPlus,
    Rollout, RolloutPly1, RolloutPly2, RolloutPly3, RolloutPly4,
    RolloutPly5, RolloutPly6, RolloutPly7
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
```

Serialization contract: round-trips cleanly through `System.Text.Json` —
no consumer-side converter registration required. `CubeOwner`, `CubeAction`,
and `AnalysisDepthClass` bundle `JsonStringEnumConverter` via attribute;
`Play` bundles `PlayJsonConverter`; `DecisionId` bundles
`DecisionIdJsonConverter`. Tested without any options-level registration in
`BgDecisionDataSerializationTests` and `DecisionRowSerializationTests`.

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
- **`DecisionRow.MatchScore` is computed, not stored.** It is derived from
  `OnRollNeeds`, `OpponentNeeds`, `IsCrawford`, and `MatchLength` on every
  access. Do not try to set it, and do not cache it across mutations of
  those fields (though init-only semantics make mutation unusual anyway).
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
  list slot when mutation is intended.
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
- **`AnalysisDepthClass` declaration order is informational, not
  contractual.** Members mirror the producer's ascending-rigor ranks, but
  depth filtering must use membership; `DepthRank` / `CubeDepthRank` remain
  the ordering surface for consumers that compare depths. `Unknown` is
  deliberately the zero value — unstamped construction sites and JSON written
  before the field existed read as `Unknown`, which means "depth not
  recorded", not an error. Do not strip a member's `[Description]` label:
  downstream label readers (XgFilter_Lib's `EnumLabel.ToLabel`) throw on a
  member without one.
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
  sentinel. This is the standard value-type caveat, shared with `Play`.

## Subproject-internal next steps

None — subproject complete. Cross-cutting work (consumer migrations,
downstream refactors) is tracked in the umbrella `INSTRUCTIONS.md`
"Next up" / "Pending" sections, not here.
