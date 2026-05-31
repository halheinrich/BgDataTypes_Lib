# BgDataTypes_Lib

> Session conventions: [`../CLAUDE.md`](../CLAUDE.md)
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
`System.Text.Json` is the only runtime dependency; `CubeOwner` and `Play`
each bundle their own `[JsonConverter]` attribute so consumers do not have
to register converters on their `JsonSerializerOptions`.

## Directory tree

```
BgDataTypes_Lib.slnx
BgDataTypes_Lib/
  BgDataTypes_Lib.csproj
  BgDecisionData.cs         — composite: Position + Decision + Descriptive + Outcome
  BoardState.cs             — mutable int[26] + HighPointOccupied + apply/undo/ApplyPlay
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
  PlayJsonConverter.cs      — JSON-array converter for Play
  PlayOutcomeData.cs        — after-boards derived from play choices
  PositionData.cs
BgDataTypes_Lib.Tests/
  BgDataTypes_Lib.Tests.csproj
  BgDecisionDataSerializationTests.cs
  BoardStateTests.cs
  CubeActionTests.cs
  CubeDecisionPairTests.cs
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
attributes: `JsonStringEnumConverter` on `CubeOwner` and `CubeAction`,
and `PlayJsonConverter` on `Play`. Consumers do not need to
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
| `DecisionData` | `Dice`, `Plays`, `BestPlayIndex`, `UserPlayIndex`, `UserPlayError?`, `IsCube`, `CubeDepth`, `CubeDepthAbbreviation`, `CubeDepthRank`, cube equity/pct fields, `UserDoubleError?`, `UserTakeError?` |
| `DescriptiveData` | `MatchLength`, `OnRollName`, `OpponentName`, `Title`, `Date`, `Event`, `SourceFile`, `MoveNumber`, `IsStandardStart` |
| `PlayOutcomeData` | `AfterBestBoard`, `AfterPlayerBoard` |

### Shared types

| Type | Notes |
|---|---|
| `CubeOwner` | enum: `OnRoll`, `Opponent`, `Centered` — serializes as string |
| `CubeAction` | enum: `NoDouble`, `Double`, `Take`, `Pass` — a player's cube response, serializes as string. Beaver/raccoon deliberately not yet members (see XML `<remarks>` on the type); enums extend without disturbing existing members. |
| `CubeDecisionPair` | `readonly record struct (CubeAction Doubler, CubeAction Taker)` — a complete cube decision as two atomic actions. Validated on construction via the positional-record idiom: `Doubler` ∈ {`NoDouble`, `Double`}, `Taker` ∈ {`Take`, `Pass`}; a cross-half value throws `ArgumentOutOfRangeException`. The verdict aggregate (pair → correct/wrong) is intentionally absent and returns later with `CubeVerdict`. `default` is non-meaningful — see Pitfalls. |
| `Move` | `readonly record struct (FrPt, ToPt)`. Encodes regular / bear-off / hit moves via the sign of `ToPt` — see "Move encoding" below. |
| `Play` | mutable `struct`, fixed 4-slot buffer of `Move`. Default value is empty (`Count == 0`). Equality / hash via order-invariant `DeduplicationKey()`. Serialized as a JSON array of `Move` via `PlayJsonConverter` (the private buffer fields are not visible to default property-based serialization). |
| `PlayCandidate` | `MoveNotation`, `Play`, `Depth`, `DepthAbbreviation`, `DepthRank`, `Equity`, `EquityLoss` (non-nullable, `0.0` = best), `IsUserPlay`, `WinPct?`, `WinGammonPct?`, `WinBgPct?`, `LosePct?`, `LoseGammonPct?`, `LoseBgPct?`. `MoveNotation` is the display string; `Play` is the structural sequence of moves (complement, not duplicate — used for structural comparison and downstream consumers). `EquityLoss == 0.0` is the test for "is this a best play"; `DecisionData.BestPlayIndex` names the canonical single best when one is needed. |
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

- **`Flip()`** — `private`. Implementation mechanic for `ApplyPlay`.
  Negates and reverses the array (point `i` ↔ point `25-i`, swapping
  the bars in the process), then recomputes `HighPointOccupied` from
  scratch. Not exposed: the design intent is that on-roll POV reasoning
  is the only POV consumers ever see.

Factories: `Standard()`, `Nackgammon()`, `Bg960(int? seed = null)` for
the three starting variants. `FromMop(IReadOnlyList<int>)` and `ToMop()`
bridge to/from the 26-element on-roll-relative point array used by
`PositionData.Mop`. `Copy()` is a deep copy. `RecalcHighPoint()` is
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

- `XgpDecisionId(Filename)` — bare filename for `.xgp` single-decision files.
- `XgDecisionId(Filename, Game, MoveNumber, IsCube)` — colon-separated tuple
  for `.xg` multi-game files. `IsCube` disambiguates the cube row from the
  checker-play row XG emits at the same `MoveNumber`.

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
otherwise `UserPlayError`.

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
All three board fields serialize to JSON but are **excluded from CSV output**.

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
    // AfterPlayerBoard) serialize to JSON but are excluded from CSV output.
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
    // CubeDepthRank, NoDoubleEquity, DoubleTakeEquity, the pct fields,
    // ProbOfOpponentErrorJustifyingDouble, UserDoubleError?, UserTakeError?).

    // Cube-decision scoring (computed; throw InvalidOperationException when IsCube is false).
    [JsonIgnore] public CubeAction  BestDoublerAction { get; }   // Double or NoDouble
    [JsonIgnore] public CubeAction  BestTakerAction   { get; }   // Take or Pass

    public double DoublerActionError(CubeAction action);          // 0 if action == BestDoublerAction;
                                                                  // throws ArgumentOutOfRangeException on Take/Pass.
    public double TakerActionError(CubeAction action);            // 0 if action == BestTakerAction;
                                                                  // throws ArgumentOutOfRangeException on Double/NoDouble.
}

public readonly record struct Move(int FrPt, int ToPt);

public struct Play : IEquatable<Play>
{
    public int Count { get; private set; }
    public Move this[int index] { get; }
    public void Add(Move move);
    public void RemoveLast();
    public Play Snapshot();
    public (int, int, int, int, int, int, int, int) DeduplicationKey();
    public override bool Equals(object? obj);
    public override int GetHashCode();
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
no consumer-side converter registration required. `CubeOwner`
bundles `JsonStringEnumConverter` via attribute; `Play` bundles
`PlayJsonConverter` via attribute. Tested without any options-level
registration in `BgDecisionDataSerializationTests` and
`DecisionRowSerializationTests`.

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
- **`Play.DeduplicationKey` is order- and hit-invariant.** Two plays with
  the same `(FrPt, |ToPt|)` multiset have equal hashes and compare equal
  even if their `Move` order differs and even if one hits while the other
  does not.
  This matches the move-generation dedup contract; do not rely on `Equals`
  to discriminate hit vs non-hit or to compare move ordering.
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
  turn must take a `Copy()` *before* calling `ApplyPlay`.
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
