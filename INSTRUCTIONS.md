# BgDataTypes_Lib — Project Instructions

Part of the Backgammon tools ecosystem: https://github.com/halheinrich/backgammon
**After committing here, return to the Backgammon Umbrella project to update hashes and instructions doc.**

## Repo

https://github.com/halheinrich/BgDataTypes_Lib
**Branch:** main
**Source files commit:** `42778d7`

## Stack

C# / .NET 10 / Class Library / Visual Studio 2026 / Windows

## Solution

`D:\Users\Hal\Documents\Visual Studio 2026\Projects\backgammon\BgDataTypes_Lib\BgDataTypes_Lib.slnx`

## Purpose

Shared type layer for the backgammon ecosystem. Defines `BgDecisionData` and its constituent
data categories — Position, Decision, and Descriptive. No parsing logic, no rendering logic.
All subprojects that need position/decision types depend on this library.

## Repo directory tree
BgDataTypes_Lib/
BgDataTypes_Lib/
AnalysisDepthEntry.cs
BgDecisionData.cs
BgDataTypes_Lib.csproj
CubeOwner.cs
DecisionData.cs
DescriptiveData.cs
PlayCandidate.cs
PositionData.cs
BgDataTypes_Lib.Tests/
BgDataTypes_Lib.Tests.csproj
BgDecisionDataSerializationTests.cs
BgDataTypes_Lib.slnx
.gitignore
INSTRUCTIONS.md

## Key files

| File | URL |
|---|---|
| BgDataTypes_Lib.csproj | https://raw.githack.com/halheinrich/BgDataTypes_Lib/42778d7/BgDataTypes_Lib/BgDataTypes_Lib.csproj |
| BgDecisionData.cs | https://raw.githack.com/halheinrich/BgDataTypes_Lib/42778d7/BgDataTypes_Lib/BgDecisionData.cs |
| PositionData.cs | https://raw.githack.com/halheinrich/BgDataTypes_Lib/42778d7/BgDataTypes_Lib/PositionData.cs |
| DecisionData.cs | https://raw.githack.com/halheinrich/BgDataTypes_Lib/42778d7/BgDataTypes_Lib/DecisionData.cs |
| DescriptiveData.cs | https://raw.githack.com/halheinrich/BgDataTypes_Lib/42778d7/BgDataTypes_Lib/DescriptiveData.cs |
| PlayCandidate.cs | https://raw.githack.com/halheinrich/BgDataTypes_Lib/42778d7/BgDataTypes_Lib/PlayCandidate.cs |
| AnalysisDepthEntry.cs | https://raw.githack.com/halheinrich/BgDataTypes_Lib/42778d7/BgDataTypes_Lib/AnalysisDepthEntry.cs |
| CubeOwner.cs | https://raw.githack.com/halheinrich/BgDataTypes_Lib/42778d7/BgDataTypes_Lib/CubeOwner.cs |
| BgDecisionDataSerializationTests.cs | https://raw.githack.com/halheinrich/BgDataTypes_Lib/42778d7/BgDataTypes_Lib.Tests/BgDecisionDataSerializationTests.cs |

## Dependency files

BgDataTypes_Lib has no dependencies on other subprojects.

## Architecture

### Data categories

| Type | Kind | Fields |
|---|---|---|
| `PositionData` | class | Mop, OnRollNeeds, OpponentNeeds, OnRollPipCount, OpponentPipCount, CubeSize, CubeOwner, IsCrawford |
| `DecisionData` | class | Dice, Plays, AnalysisDepths, BestPlayIndex, UserPlayIndex, UserPlayError?, IsCube, cube equity/percentage fields, UserDoubleError?, UserTakeError? |
| `DescriptiveData` | class | MatchLength, OnRollName, OpponentName, Title, Date, Event |

### Shared types

| Type | Kind | Notes |
|---|---|---|
| `CubeOwner` | enum | OnRoll, Opponent, Centered — serializes as string via `JsonStringEnumConverter` |
| `PlayCandidate` | class | MoveNotation, Equity, EquityLoss?, IsUserPlay |
| `AnalysisDepthEntry` | class | Label |

### Composite type

`BgDecisionData` = `PositionData` + `DecisionData` + `DescriptiveData`

All types use `class` with `init`-only properties. Pure data — no parsing or rendering logic.
Serializes cleanly with `System.Text.Json` + `JsonStringEnumConverter`.

### DecisionData — cube equity fields

When `IsCube` is true, `Dice` is `[0, 0]` and the following fields are populated:
NoDoubleEquity, DoubleTakeEquity
WinPctAfterNoDouble, GammonPctAfterNoDouble, BgPctAfterNoDouble
LosePctAfterNoDouble, LoseGammonPctAfterNoDouble, LoseBgPctAfterNoDouble
WinPctAfterDoubleTake, GammonPctAfterDoubleTake, BgPctAfterDoubleTake
LosePctAfterDoubleTake, LoseGammonPctAfterDoubleTake, LoseBgPctAfterDoubleTake
ProbOfOpponentErrorJustifyingDouble

### Mop format

26-element `IReadOnlyList<int>`:
- `[0]` = opponent's bar (value ≤ 0)
- `[1–24]` = points 1–24 from on-roll player's perspective
- `[25]` = on-roll player's bar (value ≥ 0)
- Positive = on-roll player's checkers; negative = opponent's

### Consumers

| Project | Role |
|---|---|
| `ConvertXgToJson_Lib` | Produces `BgDecisionData` from raw .xg/.xgp parse records |
| `BackgammonDiagram_Lib` | References `BgDataTypes_Lib`; `DiagramRequest.Builder` exposes flat properties; `Build()` constructs nested `PositionData`/`DecisionData`/`DescriptiveData` internally |
| `BgPositionRouter` | Consumes Position data fields from `BgDecisionData` |
| `BgInference` | Consumes `BgDecisionData` |

## Current status

✅ Core types implemented and tested. All serialization round-trip tests pass.
✅ `BackgammonDiagram_Lib` refactor complete — `DiagramRequest` now uses `PositionData`,
   `DecisionData`, `DescriptiveData` from this library. Duplicates removed.

## Key decisions

- `BgDecisionData` and its constituent types live here — no parsing or rendering logic
- All types use `class` with `init`-only properties for consistency with `BackgammonDiagram_Lib`
- `DiagramRequest` stays in `BackgammonDiagram_Lib` — it composes data fields from `BgDecisionData`
  plus rendering-specific options (`DiagramMode`, `PanelPosition`, `HomeBoardOnRight`, etc.)
- `PlayCandidate`, `AnalysisDepthEntry`, and `CubeOwner` live here; `BackgammonDiagram_Lib`
  references this lib and no longer duplicates these types
- `CubeOwner` enum lives here; serializes as string via `JsonStringEnumConverter`
- Cube equity/percentage fields live in `DecisionData` — they are analysis output, not rendering concerns
- `OnRollPipCount` and `OpponentPipCount` added to `PositionData` (commit `df6bd3a`) — display values, required by `BackgammonDiagram_Lib`
- `BestPlayIndex` and `UserPlayIndex` added to `DecisionData` (commit `bcffabf`) — moved from `DiagramRequest`
- `UserPlayError`, `UserDoubleError`, `UserTakeError` added to `DecisionData` (commit `42778d7`) — nullable double, ≥ 0, null when not applicable
- Both `DecisionRow` and `BgDecisionData` are sibling outputs of `ConvertXgToJson_Lib` —
  neither is derived from the other
- Mop conversion utilities — deferred; no decision made yet
- Reference library of interesting positions as JSON collections of `BgDecisionData` — deferred
