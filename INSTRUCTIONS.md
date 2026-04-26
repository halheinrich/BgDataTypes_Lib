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

Standalone. No subproject dependencies. `System.Text.Json` (+ `JsonStringEnumConverter`) is the only runtime dependency.

## Directory tree

```
BgDataTypes_Lib.slnx
BgDataTypes_Lib/
  BgDataTypes_Lib.csproj
  BgDecisionData.cs         — composite: Position + Decision + Descriptive + Outcome
  CubeOwner.cs              — enum (string-serialized)
  DecisionData.cs
  DecisionRow.cs            — flat CSV export record
  DescriptiveData.cs
  IDecisionFilterData.cs    — shared filter contract
  PlayCandidate.cs
  PlayOutcomeData.cs        — after-boards derived from play choices
  PositionData.cs
BgDataTypes_Lib.Tests/
  BgDataTypes_Lib.Tests.csproj
  BgDecisionDataSerializationTests.cs
  DecisionRowSerializationTests.cs
```

## Architecture

All types are `class` with `init`-only properties. Serialization uses
`System.Text.Json` with `JsonStringEnumConverter` for `CubeOwner`.

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
| `PlayCandidate` | `MoveNotation`, `Depth`, `DepthAbbreviation`, `DepthRank`, `Equity`, `EquityLoss?`, `IsUserPlay`, `WinPct?`, `WinGammonPct?`, `WinBgPct?`, `LosePct?`, `LoseGammonPct?`, `LoseBgPct?` |

### Composite type

`BgDecisionData = PositionData + DecisionData + DescriptiveData + PlayOutcomeData`.
Implements `IDecisionFilterData` via forwarding properties. `Board` returns
`Position.Mop` directly. `AfterBestBoard` / `AfterPlayerBoard` forward to
`Outcome.AfterBestBoard` / `Outcome.AfterPlayerBoard` — raw, with no conditional
on `IsCube`. The "empty for cube decisions" invariant is producer-enforced:
whoever constructs `BgDecisionData` leaves `Outcome` at its default (empty lists)
for cube decisions. `FilterError` routes to `UserDoubleError ?? UserTakeError`
for cube decisions, otherwise `UserPlayError`.

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

### Mop / Board format

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
    IReadOnlyList<int> Board { get; }             // 26 elements, see Mop format
    IReadOnlyList<int> AfterBestBoard { get; }    // POV flipped; empty for cubes
    IReadOnlyList<int> AfterPlayerBoard { get; }  // POV flipped; empty for cubes
}

public class BgDecisionData : IDecisionFilterData
{
    public PositionData    Position    { get; init; }
    public DecisionData    Decision    { get; init; }
    public DescriptiveData Descriptive { get; init; }
    public PlayOutcomeData Outcome     { get; init; }
    // IDecisionFilterData members implemented as forwarding properties.
}

public class PlayOutcomeData { /* AfterBestBoard, AfterPlayerBoard (each IReadOnlyList<int>) */ }

public sealed class DecisionRow : IDecisionFilterData
{
    // Flat init-only properties — see DecisionRow.cs for the full set.
    public string MatchScore { get; }   // computed from needs/Crawford/length
    public static string CsvHeader { get; }
    public string ToCsvLine();
}

public class PositionData    { /* init-only properties per Architecture table */ }
public class DecisionData    { /* init-only properties per Architecture table */ }
public class DescriptiveData { /* init-only properties per Architecture table */ }
public class PlayCandidate   { /* init-only properties per Architecture table */ }

public enum CubeOwner { OnRoll, Opponent, Centered }
```

Serialization contract: round-trips cleanly through `System.Text.Json` with
`JsonStringEnumConverter` registered. Tested in
`BgDecisionDataSerializationTests` and `DecisionRowSerializationTests`.

## Pitfalls

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

## Subproject-internal next steps

None — subproject complete. Cross-cutting work (consumer migrations,
downstream refactors) is tracked in the umbrella `INSTRUCTIONS.md`
"Next up" / "Pending" sections, not here.
