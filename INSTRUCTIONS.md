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
  AnalysisDepthEntry.cs
  BgDecisionData.cs         — composite: Position + Decision + Descriptive
  CubeOwner.cs              — enum (string-serialized)
  DecisionData.cs
  DecisionRow.cs            — flat CSV export record
  DescriptiveData.cs
  IDecisionFilterData.cs    — shared filter contract
  PlayCandidate.cs
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

`BgDecisionData` composes three orthogonal category types:

| Type | Fields |
|---|---|
| `PositionData` | `Mop`, `OnRollNeeds`, `OpponentNeeds`, `OnRollPipCount`, `OpponentPipCount`, `CubeSize`, `CubeOwner`, `IsCrawford` |
| `DecisionData` | `Dice`, `Plays`, `AnalysisDepths`, `BestPlayIndex`, `UserPlayIndex`, `UserPlayError?`, `IsCube`, cube equity/pct fields, `UserDoubleError?`, `UserTakeError?` |
| `DescriptiveData` | `MatchLength`, `OnRollName`, `OpponentName`, `Title`, `Date`, `Event` |

### Shared types

| Type | Notes |
|---|---|
| `CubeOwner` | enum: `OnRoll`, `Opponent`, `Centered` — serializes as string |
| `PlayCandidate` | `MoveNotation`, `Equity`, `EquityLoss?`, `IsUserPlay`, `WinPct?`, `WinGammonPct?`, `WinBgPct?`, `LosePct?`, `LoseGammonPct?`, `LoseBgPct?` |
| `AnalysisDepthEntry` | `Label` |

### Composite type

`BgDecisionData = PositionData + DecisionData + DescriptiveData`. Implements
`IDecisionFilterData` via forwarding properties. `Board` returns `Position.Mop`
directly. `FilterError` routes to `UserDoubleError ?? UserTakeError` for cube
decisions, otherwise `UserPlayError`.

### DecisionRow

Flat CSV export record. Sibling output to `BgDecisionData` — both are produced
by the XG → JSON conversion pipeline, for different consumers. Implements
`IDecisionFilterData` directly (no composition). Carries its own CSV methods
(`ToCsvLine`, `CsvHeader`, private `CsvEscape`). `Board` is stored as
`IReadOnlyList<int>` (26 elements, same layout as `PositionData.Mop`).

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
    double? FilterError { get; }        // ≥ 0 or null
    IReadOnlyList<int> Board { get; }   // 26 elements, see Mop format
}

public class BgDecisionData : IDecisionFilterData
{
    public PositionData    Position    { get; init; }
    public DecisionData    Decision    { get; init; }
    public DescriptiveData Descriptive { get; init; }
    // IDecisionFilterData members implemented as forwarding properties.
}

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
public class AnalysisDepthEntry { public string Label { get; init; } }

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

## Subproject-internal next steps

None — subproject complete. Cross-cutting work (consumer migrations,
downstream refactors) is tracked in the umbrella `INSTRUCTIONS.md`
"Next up" / "Pending" sections, not here.
