# BgDataTypes_Lib — Project Instructions

Part of the Backgammon tools ecosystem: https://github.com/halheinrich/backgammon
**After committing here, return to the Backgammon Umbrella project to update hashes and instructions doc.**

## Repo

https://github.com/halheinrich/BgDataTypes_Lib
**Branch:** main
**Current commit:** `025b1ef`

## Stack

C# / .NET 10 / Class Library / Visual Studio 2026 / Windows

## Solution

`D:\Users\Hal\Documents\Visual Studio 2026\Projects\backgammon\BgDataTypes_Lib\BgDataTypes_Lib.slnx`

## Purpose

Shared type layer for the backgammon ecosystem. Defines `BgDecisionData` and its constituent
data categories — Position, Decision, and Descriptive. No parsing logic, no rendering logic.
All subprojects that need position/decision types depend on this library.

## Repo directory tree

```
BgDataTypes_Lib/
  BgDataTypes_Lib/
    BgDataTypes_Lib.csproj
    Class1.cs
  BgDataTypes_Lib.slnx
  .gitignore
  INSTRUCTIONS.md
```

## Key files

* BgDataTypes_Lib.csproj: https://raw.githack.com/halheinrich/BgDataTypes_Lib/025b1ef/BgDataTypes_Lib/BgDataTypes_Lib.csproj

## Dependency files

BgDataTypes_Lib has no dependencies on other subprojects.

## Architecture

### Data categories

| Category | Fields |
| --- | --- |
| Position data | Mop, OnRollNeeds, OpponentNeeds, CubeSize, CubeOwner, IsCrawford |
| Decision data | Dice, Plays, AnalysisDepths, IsCube, equity fields, error fields |
| Descriptive data | MatchLength, Player names, Title, Date, Event |

### Composite types

* `BgDecisionData` = Position + Decision + Descriptive
* Pure data — no parsing or rendering logic
* Serializes cleanly with `System.Text.Json`

### Consumers

* `ConvertXgToJson_Lib` — produces `BgDecisionData` from raw .xg/.xgp parse records
* `BackgammonDiagram_Lib` — consumes `BgDecisionData`; combines with rendering options to produce `DiagramRequest`
* `BgPositionRouter` — consumes all Position data fields from `BgDecisionData`
* `BgInference` — consumes `BgDecisionData`

## Current status

🔧 In progress — initial scaffold only; type definitions not yet implemented

## Key decisions

* `BgDecisionData` and its constituent types live here — no parsing or rendering logic
* `DiagramRequest` lives in BackgammonDiagram_Lib — it is a composition of `BgDecisionData` plus rendering options supplied by the client
* Both `DecisionRow` and `BgDecisionData` are sibling outputs of ConvertXgToJson_Lib — neither is derived from the other
* Mop conversion utilities live here — shared across all consumers
* Reference library of interesting positions stored as JSON collections of `BgDecisionData`