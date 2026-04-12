# BgDataTypes_Lib — Project Instructions

Part of the Backgammon tools ecosystem: https://github.com/halheinrich/backgammon

## Repo

https://github.com/halheinrich/BgDataTypes_Lib
**Branch:** main

## Stack

C# / .NET 10 / Class Library / Visual Studio 2026 / Windows

## Solution

`D:\Users\Hal\Documents\Visual Studio 2026\Projects\backgammon\BgDataTypes_Lib\BgDataTypes_Lib.slnx`

## Purpose

Shared type layer for the backgammon ecosystem. Defines `BgDecisionData` and its constituent
data categories — Position, Decision, and Descriptive. Also owns `DecisionRow` (flat CSV export record).
No parsing logic, no rendering logic. No dependencies on other subprojects.

## Directory tree

```
BgDataTypes_Lib/
  BgDataTypes_Lib/
    AnalysisDepthEntry.cs
    BgDataTypes_Lib.csproj
    BgDecisionData.cs
    CubeOwner.cs
    DecisionData.cs
    DecisionRow.cs
    DescriptiveData.cs
    IDecisionFilterData.cs
    PlayCandidate.cs
    PositionData.cs
  BgDataTypes_Lib.Tests/
    BgDataTypes_Lib.Tests.csproj
    BgDecisionDataSerializationTests.cs
    DecisionRowSerializationTests.cs
  BgDataTypes_Lib.slnx
```

## Architecture

### Data categories

| Type | Fields |
|---|---|
| `PositionData` | Mop, OnRollNeeds, OpponentNeeds, OnRollPipCount, OpponentPipCount, CubeSize, CubeOwner, IsCrawford |
| `DecisionData` | Dice, Plays, AnalysisDepths, BestPlayIndex, UserPlayIndex, UserPlayError?, IsCube, cube equity/pct fields, UserDoubleError?, UserTakeError? |
| `DescriptiveData` | MatchLength, OnRollName, OpponentName, Title, Date, Event |

### Shared types

| Type | Notes |
|---|---|
| `CubeOwner` | enum: OnRoll, Opponent, Centered — serializes as string |
| `PlayCandidate` | MoveNotation, Equity, EquityLoss?, IsUserPlay, WinPct?, WinGammonPct?, WinBgPct?, LosePct?, LoseGammonPct?, LoseBgPct? |
| `AnalysisDepthEntry` | Label |
| `IDecisionFilterData` | Interface: Player, IsCube, OnRollNeeds, OpponentNeeds, IsCrawford, MatchLength, FilterError, Board |

### Composite type

`BgDecisionData` = `PositionData` + `DecisionData` + `DescriptiveData`
Implements `IDecisionFilterData` via forwarding properties. `Board` returns `Position.Mop` directly.

### DecisionRow

Flat CSV export record. Implements `IDecisionFilterData`. CSV methods (`ToCsvLine`, `CsvHeader`, `CsvEscape`) travel with the type.
`MatchScore` is computed from `OnRollNeeds`, `OpponentNeeds`, `IsCrawford`, `MatchLength`.
`Board` is `IReadOnlyList<int>` (26 elements, same layout as `PositionData.Mop`).

### Mop / Board format

26-element `IReadOnlyList<int>`:
- `[0]` = opponent's bar (≤ 0)
- `[1–24]` = points 1–24 from on-roll player's perspective
- `[25]` = on-roll player's bar (≥ 0)
- Positive = on-roll; negative = opponent

All types use `class` with `init`-only properties. Serializes with `System.Text.Json` + `JsonStringEnumConverter`.

## Current status

✅ Complete — all types implemented and tested

## Key decisions

* All types use `class` with `init`-only properties
* `DiagramRequest` stays in BackgammonDiagram_Lib — composes BgDecisionData + rendering options
* CSV methods kept on `DecisionRow` — accepted deviation from pure-data principle
* Both `DecisionRow` and `BgDecisionData` are sibling outputs of ConvertXgToJson_Lib
* `IDecisionFilterData` implemented by both `DecisionRow` and `BgDecisionData`
* `DecisionRow.MatchScore` computed from needs/Crawford/length — not a stored string