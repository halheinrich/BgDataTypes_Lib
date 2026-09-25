using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The stored boards changed type (halheinrich/backgammon#15) and every wire
/// member became required or nullable (halheinrich/backgammon#222); the
/// bytes of a full record did not change. Each golden below is the JSON the
/// library wrote for the same record at 5a967cc, before either change,
/// captured from both the reflection path and the source-generated context
/// (identical then) and pinned here. A full record is a checker play with
/// every member present and every nullable member non-null, so every member
/// and both after-boards are on the wire; a cube record's absent
/// after-boards are the one intended byte change (<c>[]</c> becomes
/// <c>null</c>), pinned in <see cref="BoardWireTests"/>.
/// </summary>
public class WireGoldenTests
{
    private static readonly JsonSerializerOptions ReflectionOptions = new();

    private static readonly JsonSerializerOptions ContextOptions = new()
    {
        TypeInfoResolver = BgDataTypesJsonContext.Default
    };

    /// <summary>The tester's position of halheinrich/backgammon#273, in the mover's frame.</summary>
    private static readonly BoardPosition Mop = new(
        [-1, 0, 2, -1, 2, 2, 4, 1, 2, -1, 0, 1, -2, 0, 0, 0, -2, 0, 0, -2, -2, 1, -2, 0, -2, 0]);

    private static readonly Play Best = [new(8, -3), new(7, 3)];
    private static readonly Play User = [new(11, 6), new(7, -3)];

    /// <summary>The position <paramref name="play"/> reaches, in the next mover's frame.</summary>
    private static BoardPosition After(Play play)
    {
        var board = new BoardState(Mop);
        board.ApplyPlay(play);
        return board.ToPosition();
    }

    internal static BgDecisionData FullRecord() => TestRecords.Record(
        id: new XgDecisionId("golden.xg", 3, 17, false),
        xgid: "XGID=golden",
        position: TestRecords.Position(
            mop: Mop, onRollNeeds: 3, opponentNeeds: 5, onRollPipCount: 130, opponentPipCount: 145,
            cubeSize: 2, cubeOwner: CubeOwner.Opponent, isCrawford: false, isJacoby: false),
        decision: TestRecords.Decision(
            dice: [5, 4],
            plays:
            [
                TestRecords.Candidate(
                    moveNotation: "8/3* 7/3", play: Best, depth: "3-ply", depthAbbreviation: "3p",
                    depthRank: 7, analysisMode: AnalysisMode.Evaluation, analysisLevel: AnalysisLevel.Ply3,
                    equity: 0.25, equityLoss: 0.0, winPct: 0.61, winGammonPct: 0.21, winBgPct: 0.01,
                    losePct: 0.39, loseGammonPct: 0.11, loseBgPct: 0.02),
                TestRecords.Candidate(
                    moveNotation: "11/6 7/3*", play: User, depth: "Rollout", depthAbbreviation: "R",
                    depthRank: 9, analysisMode: AnalysisMode.Rollout, analysisLevel: AnalysisLevel.XgRoller,
                    equity: 0.125, equityLoss: 0.125, winPct: 0.58, winGammonPct: 0.19, winBgPct: 0.015,
                    losePct: 0.42, loseGammonPct: 0.12, loseBgPct: 0.025),
            ],
            bestPlayIndex: 0, userPlayError: 0.125, userPlayIndex: 1, isCube: false,
            cubeDepth: "cd", cubeDepthAbbreviation: "cda", cubeDepthRank: 4,
            cubeAnalysisMode: AnalysisMode.BookRollout, cubeAnalysisLevel: AnalysisLevel.Ply4,
            noDoubleEquity: 0.5, doubleTakeEquity: 0.75, cubelessNoDoubleEquity: 0.375,
            cubelessDoubleTakeEquity: 0.625,
            winPctAfterNoDouble: 0.51, gammonPctAfterNoDouble: 0.12, bgPctAfterNoDouble: 0.013,
            losePctAfterNoDouble: 0.49, loseGammonPctAfterNoDouble: 0.14, loseBgPctAfterNoDouble: 0.015,
            winPctAfterDoubleTake: 0.52, gammonPctAfterDoubleTake: 0.16, bgPctAfterDoubleTake: 0.017,
            losePctAfterDoubleTake: 0.48, loseGammonPctAfterDoubleTake: 0.18, loseBgPctAfterDoubleTake: 0.019,
            probOfOpponentErrorJustifyingDouble: 0.2, userDoubleError: 0.03, userTakeError: 0.04,
            userDoublerAction: CubeAction.NoDouble, userTakerAction: CubeAction.Take),
        descriptive: TestRecords.Descriptive(
            matchLength: 7, onRollName: "Alice", opponentName: "Bob", title: "Golden",
            date: new DateOnly(2026, 9, 25), @event: "Club", sourceFile: "golden.xg", game: 3,
            moveNumber: 17, isStandardStart: true, comment: "note", flagged: true),
        outcome: TestRecords.Outcome(afterBestBoard: After(Best), afterPlayerBoard: After(User)));

    internal static DecisionRow FullRow() => TestRecords.Row(
        id: new XgDecisionId("golden.xg", 3, 17, false),
        xgid: "XGID=golden", error: 0.125, matchLength: 7, player: "Alice", sourceFile: "golden.xg",
        game: 3, moveNumber: 17, isStandardStart: true, roll: 54, analysisDepth: "3-ply",
        analysisMode: AnalysisMode.Evaluation, analysisLevel: AnalysisLevel.Ply3, equity: 0.25,
        onRollNeeds: 3, opponentNeeds: 5, isCrawford: false, isJacoby: false,
        board: Mop, afterBestBoard: After(Best), afterPlayerBoard: After(User));

    private const string RecordGolden =
        """{"Id":"golden.xg:g3:m17:play","Xgid":"XGID=golden","Position":{"Mop":[-1,0,2,-1,2,2,4,1,2,-1,0,1,-2,0,0,0,-2,0,0,-2,-2,1,-2,0,-2,0],"OnRollNeeds":3,"OpponentNeeds":5,"OnRollPipCount":130,"OpponentPipCount":145,"CubeSize":2,"CubeOwner":"Opponent","IsCrawford":false,"IsJacoby":false},"Decision":{"Dice":[5,4],"Plays":[{"MoveNotation":"8/3* 7/3","Play":[{"FrPt":8,"ToPt":-3},{"FrPt":7,"ToPt":3}],"Depth":"3-ply","DepthAbbreviation":"3p","DepthRank":7,"AnalysisMode":"Evaluation","AnalysisLevel":"Ply3","Equity":0.25,"EquityLoss":0,"WinPct":0.61,"WinGammonPct":0.21,"WinBgPct":0.01,"LosePct":0.39,"LoseGammonPct":0.11,"LoseBgPct":0.02},{"MoveNotation":"11/6 7/3*","Play":[{"FrPt":11,"ToPt":6},{"FrPt":7,"ToPt":-3}],"Depth":"Rollout","DepthAbbreviation":"R","DepthRank":9,"AnalysisMode":"Rollout","AnalysisLevel":"XgRoller","Equity":0.125,"EquityLoss":0.125,"WinPct":0.58,"WinGammonPct":0.19,"WinBgPct":0.015,"LosePct":0.42,"LoseGammonPct":0.12,"LoseBgPct":0.025}],"BestPlayIndex":0,"UserPlayError":0.125,"UserPlayIndex":1,"IsCube":false,"CubeDepth":"cd","CubeDepthAbbreviation":"cda","CubeDepthRank":4,"CubeAnalysisMode":"BookRollout","CubeAnalysisLevel":"Ply4","NoDoubleEquity":0.5,"DoubleTakeEquity":0.75,"CubelessNoDoubleEquity":0.375,"CubelessDoubleTakeEquity":0.625,"WinPctAfterNoDouble":0.51,"GammonPctAfterNoDouble":0.12,"BgPctAfterNoDouble":0.013,"LosePctAfterNoDouble":0.49,"LoseGammonPctAfterNoDouble":0.14,"LoseBgPctAfterNoDouble":0.015,"WinPctAfterDoubleTake":0.52,"GammonPctAfterDoubleTake":0.16,"BgPctAfterDoubleTake":0.017,"LosePctAfterDoubleTake":0.48,"LoseGammonPctAfterDoubleTake":0.18,"LoseBgPctAfterDoubleTake":0.019,"ProbOfOpponentErrorJustifyingDouble":0.2,"UserDoubleError":0.03,"UserTakeError":0.04,"UserDoublerAction":"NoDouble","UserTakerAction":"Take"},"Descriptive":{"MatchLength":7,"OnRollName":"Alice","OpponentName":"Bob","Title":"Golden","Date":"2026-09-25","Event":"Club","SourceFile":"golden.xg","Game":3,"MoveNumber":17,"IsStandardStart":true,"Comment":"note","Flagged":true},"Outcome":{"AfterBestBoard":[0,2,0,2,-1,2,2,0,0,2,0,0,0,2,-1,0,1,-1,0,-4,-2,-2,-2,-2,0,2],"AfterPlayerBoard":[0,2,0,2,-1,2,2,0,0,2,0,0,0,2,0,0,1,-2,0,-5,-2,-2,-1,-2,0,2]}}""";

    private const string RowGolden =
        """{"Id":"golden.xg:g3:m17:play","Xgid":"XGID=golden","Error":0.125,"MatchLength":7,"Player":"Alice","SourceFile":"golden.xg","Game":3,"MoveNumber":17,"IsStandardStart":true,"Roll":54,"AnalysisDepth":"3-ply","AnalysisMode":"Evaluation","AnalysisLevel":"Ply3","Equity":0.25,"OnRollNeeds":3,"OpponentNeeds":5,"IsCrawford":false,"IsJacoby":false,"Board":[-1,0,2,-1,2,2,4,1,2,-1,0,1,-2,0,0,0,-2,0,0,-2,-2,1,-2,0,-2,0],"AfterBestBoard":[0,2,0,2,-1,2,2,0,0,2,0,0,0,2,-1,0,1,-1,0,-4,-2,-2,-2,-2,0,2],"AfterPlayerBoard":[0,2,0,2,-1,2,2,0,0,2,0,0,0,2,0,0,1,-2,0,-5,-2,-2,-1,-2,0,2]}""";

    [Fact]
    public void FullRecord_BytesUnchanged_BothPaths()
    {
        Assert.Equal(RecordGolden, JsonSerializer.Serialize(FullRecord(), ReflectionOptions));
        Assert.Equal(RecordGolden, JsonSerializer.Serialize(FullRecord(), ContextOptions));
    }

    [Fact]
    public void FullRow_BytesUnchanged_BothPaths()
    {
        Assert.Equal(RowGolden, JsonSerializer.Serialize(FullRow(), ReflectionOptions));
        Assert.Equal(RowGolden, JsonSerializer.Serialize(FullRow(), ContextOptions));
    }

    [Fact]
    public void Goldens_ReadBackToTheSameBytes_BothPaths()
    {
        // The read side of the same contract: a document written before the
        // change loads, and writes itself back unchanged.
        foreach (var options in new[] { ReflectionOptions, ContextOptions })
        {
            var record = JsonSerializer.Deserialize<BgDecisionData>(RecordGolden, options)!;
            var row = JsonSerializer.Deserialize<DecisionRow>(RowGolden, options)!;

            Assert.Equal(RecordGolden, JsonSerializer.Serialize(record, options));
            Assert.Equal(RowGolden, JsonSerializer.Serialize(row, options));
            Assert.Equal(Mop, record.Position.Mop);
            Assert.Equal(After(Best), record.Outcome.AfterBestBoard);
        }
    }
}
