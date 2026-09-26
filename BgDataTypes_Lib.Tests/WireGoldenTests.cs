using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The bytes of a full record of each kind and of each kind's row, on both
/// the reflection path and the source-generated context. A full record has
/// every member present and every nullable member non-null, so every member
/// is on the wire.
/// </summary>
/// <remarks>
/// <para>
/// <b>The records leg of halheinrich/backgammon#273 changed the shape
/// deliberately.</b> A decision is one of two types, each carrying only its
/// own members, with its kind written first; the after-boards are derived and
/// leave the record's wire; a standalone position's game facts are none
/// (halheinrich/backgammon#124). The previous shapes are kept below as
/// documents the library must now refuse — as a
/// <see cref="JsonException"/>, on both paths — and the delta from the last of
/// them is pinned member by member.
/// </para>
/// <para>
/// The row's after-boards are the record's derivation through the one play
/// rule; they are byte-identical to the boards the previous golden stored,
/// which the previous fixture computed with <see cref="BoardState.ApplyPlay"/>
/// — the derivation changed where the boards come from, not what they are.
/// </para>
/// </remarks>
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

    internal static CheckerPlayDecision FullRecord() => TestRecords.CheckerPlay(
        id: new XgDecisionId("golden.xg", 3, 17, false),
        xgid: "XGID=golden",
        position: TestRecords.Position(
            mop: Mop, onRollNeeds: 3, opponentNeeds: 5, onRollPipCount: 130, opponentPipCount: 145,
            cubeSize: 2, cubeOwner: CubeOwner.Opponent, isCrawford: false, isJacoby: false),
        decision: TestRecords.CheckerPlayData(
            dice: [5, 4],
            plays:
            [
                TestRecords.Candidate(
                    play: Best, depth: "3-ply", depthAbbreviation: "3p",
                    depthRank: 7, analysisMode: AnalysisMode.Evaluation, analysisLevel: AnalysisLevel.Ply3,
                    equity: 0.25, equityLoss: 0.0, winPct: 0.61, winGammonPct: 0.21, winBgPct: 0.01,
                    losePct: 0.39, loseGammonPct: 0.11, loseBgPct: 0.02),
                TestRecords.Candidate(
                    play: User, depth: "Rollout", depthAbbreviation: "R",
                    depthRank: 9, analysisMode: AnalysisMode.Rollout, analysisLevel: AnalysisLevel.XgRoller,
                    equity: 0.125, equityLoss: 0.125, winPct: 0.58, winGammonPct: 0.19, winBgPct: 0.015,
                    losePct: 0.42, loseGammonPct: 0.12, loseBgPct: 0.025),
            ],
            bestPlayIndex: 0, userPlayIndex: 1, userPlayError: 0.125),
        descriptive: TestRecords.Descriptive(
            matchLength: 7, onRollName: "Alice", opponentName: "Bob", title: "Golden",
            date: new DateOnly(2026, 9, 25), @event: "Club", sourceFile: "golden.xg",
            isStandardStart: true, comment: "note", flagged: true));

    /// <summary>
    /// The cube half the previous golden carried on its checker play, now a
    /// cube decision of its own at the same score (not Crawford), every member
    /// stated.
    /// </summary>
    internal static CubeDecision FullCubeRecord() => TestRecords.Cube(
        id: new XgDecisionId("golden.xg", 3, 18, true),
        xgid: "XGID=golden-cube",
        position: TestRecords.Position(
            mop: Mop, onRollNeeds: 3, opponentNeeds: 5, onRollPipCount: 130, opponentPipCount: 145,
            cubeSize: 2, cubeOwner: CubeOwner.OnRoll, isCrawford: false, isJacoby: false),
        decision: TestRecords.CubeData(
            depth: "cd", depthAbbreviation: "cda", depthRank: 4,
            analysisMode: AnalysisMode.BookRollout, analysisLevel: AnalysisLevel.Ply4,
            noDoubleEquity: 0.5, doubleTakeEquity: 0.75, cubelessNoDoubleEquity: 0.375,
            cubelessDoubleTakeEquity: 0.625,
            winPctAfterNoDouble: 0.51, gammonPctAfterNoDouble: 0.12, bgPctAfterNoDouble: 0.013,
            losePctAfterNoDouble: 0.49, loseGammonPctAfterNoDouble: 0.14, loseBgPctAfterNoDouble: 0.015,
            winPctAfterDoubleTake: 0.52, gammonPctAfterDoubleTake: 0.16, bgPctAfterDoubleTake: 0.017,
            losePctAfterDoubleTake: 0.48, loseGammonPctAfterDoubleTake: 0.18, loseBgPctAfterDoubleTake: 0.019,
            probOfOpponentErrorJustifyingDouble: 0.2, userDoubleError: 0.03, userTakeError: 0.04,
            userDoublerAction: CubeAction.Double, userTakerAction: CubeAction.Take),
        descriptive: TestRecords.Descriptive(
            matchLength: 7, onRollName: "Alice", opponentName: "Bob", title: "Golden",
            date: new DateOnly(2026, 9, 25), @event: "Club", sourceFile: "golden.xg",
            isStandardStart: true, comment: "note", flagged: true));

    internal static DecisionRow FullRow() => DecisionRow.From(FullRecord());

    internal static DecisionRow FullCubeRow() => DecisionRow.From(FullCubeRecord());

    // -----------------------------------------------------------------------
    //  The previous shapes — documents the library must refuse
    // -----------------------------------------------------------------------

    /// <summary>
    /// The full record as a5eca85 wrote it, each candidate carrying its stored
    /// <c>MoveNotation</c> and the record its stored game and move number.
    /// </summary>
    private const string RecordGoldenAtA5eca85 =
        """{"Id":"golden.xg:g3:m17:play","Xgid":"XGID=golden","Position":{"Mop":[-1,0,2,-1,2,2,4,1,2,-1,0,1,-2,0,0,0,-2,0,0,-2,-2,1,-2,0,-2,0],"OnRollNeeds":3,"OpponentNeeds":5,"OnRollPipCount":130,"OpponentPipCount":145,"CubeSize":2,"CubeOwner":"Opponent","IsCrawford":false,"IsJacoby":false},"Decision":{"Dice":[5,4],"Plays":[{"MoveNotation":"8/3* 7/3","Play":[{"FrPt":8,"ToPt":-3},{"FrPt":7,"ToPt":3}],"Depth":"3-ply","DepthAbbreviation":"3p","DepthRank":7,"AnalysisMode":"Evaluation","AnalysisLevel":"Ply3","Equity":0.25,"EquityLoss":0,"WinPct":0.61,"WinGammonPct":0.21,"WinBgPct":0.01,"LosePct":0.39,"LoseGammonPct":0.11,"LoseBgPct":0.02},{"MoveNotation":"11/6 7/3*","Play":[{"FrPt":11,"ToPt":6},{"FrPt":7,"ToPt":-3}],"Depth":"Rollout","DepthAbbreviation":"R","DepthRank":9,"AnalysisMode":"Rollout","AnalysisLevel":"XgRoller","Equity":0.125,"EquityLoss":0.125,"WinPct":0.58,"WinGammonPct":0.19,"WinBgPct":0.015,"LosePct":0.42,"LoseGammonPct":0.12,"LoseBgPct":0.025}],"BestPlayIndex":0,"UserPlayError":0.125,"UserPlayIndex":1,"IsCube":false,"CubeDepth":"cd","CubeDepthAbbreviation":"cda","CubeDepthRank":4,"CubeAnalysisMode":"BookRollout","CubeAnalysisLevel":"Ply4","NoDoubleEquity":0.5,"DoubleTakeEquity":0.75,"CubelessNoDoubleEquity":0.375,"CubelessDoubleTakeEquity":0.625,"WinPctAfterNoDouble":0.51,"GammonPctAfterNoDouble":0.12,"BgPctAfterNoDouble":0.013,"LosePctAfterNoDouble":0.49,"LoseGammonPctAfterNoDouble":0.14,"LoseBgPctAfterNoDouble":0.015,"WinPctAfterDoubleTake":0.52,"GammonPctAfterDoubleTake":0.16,"BgPctAfterDoubleTake":0.017,"LosePctAfterDoubleTake":0.48,"LoseGammonPctAfterDoubleTake":0.18,"LoseBgPctAfterDoubleTake":0.019,"ProbOfOpponentErrorJustifyingDouble":0.2,"UserDoubleError":0.03,"UserTakeError":0.04,"UserDoublerAction":"NoDouble","UserTakerAction":"Take"},"Descriptive":{"MatchLength":7,"OnRollName":"Alice","OpponentName":"Bob","Title":"Golden","Date":"2026-09-25","Event":"Club","SourceFile":"golden.xg","Game":3,"MoveNumber":17,"IsStandardStart":true,"Comment":"note","Flagged":true},"Outcome":{"AfterBestBoard":[0,2,0,2,-1,2,2,0,0,2,0,0,0,2,-1,0,1,-1,0,-4,-2,-2,-2,-2,0,2],"AfterPlayerBoard":[0,2,0,2,-1,2,2,0,0,2,0,0,0,2,0,0,1,-2,0,-5,-2,-2,-1,-2,0,2]}}""";

    /// <summary>
    /// The full record in the one-class shape this leg retired (a25f4cf): an
    /// <c>IsCube</c> switch, the inactive cube half at stated stand-in values,
    /// and the after-boards stored in an <c>Outcome</c>.
    /// </summary>
    private const string RecordGoldenBeforeKinds =
        """{"Id":"golden.xg:g3:m17:play","Xgid":"XGID=golden","Position":{"Mop":[-1,0,2,-1,2,2,4,1,2,-1,0,1,-2,0,0,0,-2,0,0,-2,-2,1,-2,0,-2,0],"OnRollNeeds":3,"OpponentNeeds":5,"OnRollPipCount":130,"OpponentPipCount":145,"CubeSize":2,"CubeOwner":"Opponent","IsCrawford":false,"IsJacoby":false},"Decision":{"Dice":[5,4],"Plays":[{"Play":[{"FrPt":8,"ToPt":-3},{"FrPt":7,"ToPt":3}],"Depth":"3-ply","DepthAbbreviation":"3p","DepthRank":7,"AnalysisMode":"Evaluation","AnalysisLevel":"Ply3","Equity":0.25,"EquityLoss":0,"WinPct":0.61,"WinGammonPct":0.21,"WinBgPct":0.01,"LosePct":0.39,"LoseGammonPct":0.11,"LoseBgPct":0.02},{"Play":[{"FrPt":11,"ToPt":6},{"FrPt":7,"ToPt":-3}],"Depth":"Rollout","DepthAbbreviation":"R","DepthRank":9,"AnalysisMode":"Rollout","AnalysisLevel":"XgRoller","Equity":0.125,"EquityLoss":0.125,"WinPct":0.58,"WinGammonPct":0.19,"WinBgPct":0.015,"LosePct":0.42,"LoseGammonPct":0.12,"LoseBgPct":0.025}],"BestPlayIndex":0,"UserPlayError":0.125,"UserPlayIndex":1,"IsCube":false,"CubeDepth":"cd","CubeDepthAbbreviation":"cda","CubeDepthRank":4,"CubeAnalysisMode":"BookRollout","CubeAnalysisLevel":"Ply4","NoDoubleEquity":0.5,"DoubleTakeEquity":0.75,"CubelessNoDoubleEquity":0.375,"CubelessDoubleTakeEquity":0.625,"WinPctAfterNoDouble":0.51,"GammonPctAfterNoDouble":0.12,"BgPctAfterNoDouble":0.013,"LosePctAfterNoDouble":0.49,"LoseGammonPctAfterNoDouble":0.14,"LoseBgPctAfterNoDouble":0.015,"WinPctAfterDoubleTake":0.52,"GammonPctAfterDoubleTake":0.16,"BgPctAfterDoubleTake":0.017,"LosePctAfterDoubleTake":0.48,"LoseGammonPctAfterDoubleTake":0.18,"LoseBgPctAfterDoubleTake":0.019,"ProbOfOpponentErrorJustifyingDouble":0.2,"UserDoubleError":0.03,"UserTakeError":0.04,"UserDoublerAction":"NoDouble","UserTakerAction":"Take"},"Descriptive":{"MatchLength":7,"OnRollName":"Alice","OpponentName":"Bob","Title":"Golden","Date":"2026-09-25","Event":"Club","SourceFile":"golden.xg","IsStandardStart":true,"Comment":"note","Flagged":true},"Outcome":{"AfterBestBoard":[0,2,0,2,-1,2,2,0,0,2,0,0,0,2,-1,0,1,-1,0,-4,-2,-2,-2,-2,0,2],"AfterPlayerBoard":[0,2,0,2,-1,2,2,0,0,2,0,0,0,2,0,0,1,-2,0,-5,-2,-2,-1,-2,0,2]}}""";

    /// <summary>The row in the shape this leg retired: no kind, the roll as the kind.</summary>
    private const string RowGoldenBeforeKinds =
        """{"Id":"golden.xg:g3:m17:play","Xgid":"XGID=golden","Error":0.125,"MatchLength":7,"Player":"Alice","SourceFile":"golden.xg","IsStandardStart":true,"Roll":54,"AnalysisDepth":"3-ply","AnalysisMode":"Evaluation","AnalysisLevel":"Ply3","Equity":0.25,"OnRollNeeds":3,"OpponentNeeds":5,"IsCrawford":false,"IsJacoby":false,"Board":[-1,0,2,-1,2,2,4,1,2,-1,0,1,-2,0,0,0,-2,0,0,-2,-2,1,-2,0,-2,0],"AfterBestBoard":[0,2,0,2,-1,2,2,0,0,2,0,0,0,2,-1,0,1,-1,0,-4,-2,-2,-2,-2,0,2],"AfterPlayerBoard":[0,2,0,2,-1,2,2,0,0,2,0,0,0,2,0,0,1,-2,0,-5,-2,-2,-1,-2,0,2]}""";

    // -----------------------------------------------------------------------
    //  Today's shape
    // -----------------------------------------------------------------------

    private const string CheckerPlayGolden =
        """{"Kind":"CheckerPlay","Id":"golden.xg:g3:m17:play","Xgid":"XGID=golden","Position":{"Mop":[-1,0,2,-1,2,2,4,1,2,-1,0,1,-2,0,0,0,-2,0,0,-2,-2,1,-2,0,-2,0],"OnRollNeeds":3,"OpponentNeeds":5,"OnRollPipCount":130,"OpponentPipCount":145,"CubeSize":2,"CubeOwner":"Opponent","IsCrawford":false,"IsJacoby":false},"Descriptive":{"MatchLength":7,"OnRollName":"Alice","OpponentName":"Bob","Title":"Golden","Date":"2026-09-25","Event":"Club","SourceFile":"golden.xg","IsStandardStart":true,"Comment":"note","Flagged":true},"Decision":{"Dice":[5,4],"Plays":[{"Play":[{"FrPt":8,"ToPt":-3},{"FrPt":7,"ToPt":3}],"Depth":"3-ply","DepthAbbreviation":"3p","DepthRank":7,"AnalysisMode":"Evaluation","AnalysisLevel":"Ply3","Equity":0.25,"EquityLoss":0,"WinPct":0.61,"WinGammonPct":0.21,"WinBgPct":0.01,"LosePct":0.39,"LoseGammonPct":0.11,"LoseBgPct":0.02},{"Play":[{"FrPt":11,"ToPt":6},{"FrPt":7,"ToPt":-3}],"Depth":"Rollout","DepthAbbreviation":"R","DepthRank":9,"AnalysisMode":"Rollout","AnalysisLevel":"XgRoller","Equity":0.125,"EquityLoss":0.125,"WinPct":0.58,"WinGammonPct":0.19,"WinBgPct":0.015,"LosePct":0.42,"LoseGammonPct":0.12,"LoseBgPct":0.025}],"BestPlayIndex":0,"UserPlayIndex":1,"UserPlayError":0.125}}""";

    private const string CubeGolden =
        """{"Kind":"Cube","Id":"golden.xg:g3:m18:cube","Xgid":"XGID=golden-cube","Position":{"Mop":[-1,0,2,-1,2,2,4,1,2,-1,0,1,-2,0,0,0,-2,0,0,-2,-2,1,-2,0,-2,0],"OnRollNeeds":3,"OpponentNeeds":5,"OnRollPipCount":130,"OpponentPipCount":145,"CubeSize":2,"CubeOwner":"OnRoll","IsCrawford":false,"IsJacoby":false},"Descriptive":{"MatchLength":7,"OnRollName":"Alice","OpponentName":"Bob","Title":"Golden","Date":"2026-09-25","Event":"Club","SourceFile":"golden.xg","IsStandardStart":true,"Comment":"note","Flagged":true},"Decision":{"Depth":"cd","DepthAbbreviation":"cda","DepthRank":4,"AnalysisMode":"BookRollout","AnalysisLevel":"Ply4","NoDoubleEquity":0.5,"DoubleTakeEquity":0.75,"CubelessNoDoubleEquity":0.375,"CubelessDoubleTakeEquity":0.625,"WinPctAfterNoDouble":0.51,"GammonPctAfterNoDouble":0.12,"BgPctAfterNoDouble":0.013,"LosePctAfterNoDouble":0.49,"LoseGammonPctAfterNoDouble":0.14,"LoseBgPctAfterNoDouble":0.015,"WinPctAfterDoubleTake":0.52,"GammonPctAfterDoubleTake":0.16,"BgPctAfterDoubleTake":0.017,"LosePctAfterDoubleTake":0.48,"LoseGammonPctAfterDoubleTake":0.18,"LoseBgPctAfterDoubleTake":0.019,"ProbOfOpponentErrorJustifyingDouble":0.2,"UserDoubleError":0.03,"UserTakeError":0.04,"UserDoublerAction":"Double","UserTakerAction":"Take"}}""";

    private const string CheckerPlayRowGolden =
        """{"Kind":"CheckerPlay","Id":"golden.xg:g3:m17:play","Xgid":"XGID=golden","Error":0.125,"MatchLength":7,"Player":"Alice","SourceFile":"golden.xg","IsStandardStart":true,"Roll":54,"AnalysisDepth":"3-ply","AnalysisMode":"Evaluation","AnalysisLevel":"Ply3","Equity":0.25,"OnRollNeeds":3,"OpponentNeeds":5,"IsCrawford":false,"IsJacoby":false,"Board":[-1,0,2,-1,2,2,4,1,2,-1,0,1,-2,0,0,0,-2,0,0,-2,-2,1,-2,0,-2,0],"AfterBestBoard":[0,2,0,2,-1,2,2,0,0,2,0,0,0,2,-1,0,1,-1,0,-4,-2,-2,-2,-2,0,2],"AfterPlayerBoard":[0,2,0,2,-1,2,2,0,0,2,0,0,0,2,0,0,1,-2,0,-5,-2,-2,-1,-2,0,2]}""";

    private const string CubeRowGolden =
        """{"Kind":"Cube","Id":"golden.xg:g3:m18:cube","Xgid":"XGID=golden-cube","Error":0.03,"MatchLength":7,"Player":"Alice","SourceFile":"golden.xg","IsStandardStart":true,"Roll":null,"AnalysisDepth":"cd","AnalysisMode":"BookRollout","AnalysisLevel":"Ply4","Equity":0.5,"OnRollNeeds":3,"OpponentNeeds":5,"IsCrawford":false,"IsJacoby":false,"Board":[-1,0,2,-1,2,2,4,1,2,-1,0,1,-2,0,0,0,-2,0,0,-2,-2,1,-2,0,-2,0],"AfterBestBoard":null,"AfterPlayerBoard":null}""";

    public static TheoryData<string> Kinds => ["CheckerPlay", "Cube"];

    private static (BgDecisionData Record, string RecordGolden, DecisionRow Row, string RowGolden) Golden(string kind) =>
        kind == "Cube"
            ? (FullCubeRecord(), CubeGolden, FullCubeRow(), CubeRowGolden)
            : (FullRecord(), CheckerPlayGolden, FullRow(), CheckerPlayRowGolden);

    [Theory]
    [MemberData(nameof(Kinds))]
    public void FullRecord_Bytes_BothPaths(string kind)
    {
        // Rewritten from FullRecord_Bytes_BothPaths for each kind: the
        // record's bytes changed deliberately (halheinrich/backgammon#273, the
        // records leg), to exactly these.
        var (record, golden, _, _) = Golden(kind);

        Assert.Equal(golden, JsonSerializer.Serialize(record, ReflectionOptions));
        Assert.Equal(golden, JsonSerializer.Serialize(record, ContextOptions));
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void FullRecord_AsItsOwnType_WritesTheSameBytes_BothPaths(string kind)
    {
        // Added: the kind is a member of each kind's contract, so a record
        // serialized as its own static type writes the same document — the
        // kind included — as one serialized as the wire unit.
        var (record, golden, _, _) = Golden(kind);

        foreach (var options in new[] { ReflectionOptions, ContextOptions })
            Assert.Equal(golden, JsonSerializer.Serialize(record, record.GetType(), options));
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void FullRow_Bytes_BothPaths(string kind)
    {
        // Rewritten from FullRow_Bytes_BothPaths for each kind.
        var (_, _, row, golden) = Golden(kind);

        Assert.Equal(golden, JsonSerializer.Serialize(row, ReflectionOptions));
        Assert.Equal(golden, JsonSerializer.Serialize(row, ContextOptions));
    }

    [Fact]
    public void Goldens_ReadBackToTheSameBytes_BothPaths()
    {
        // The read side of the same contract: a document in today's form
        // loads as its kind, and writes itself back unchanged.
        foreach (var options in new[] { ReflectionOptions, ContextOptions })
        {
            var play = JsonSerializer.Deserialize<BgDecisionData>(CheckerPlayGolden, options)!;
            var cube = JsonSerializer.Deserialize<BgDecisionData>(CubeGolden, options)!;
            var playRow = JsonSerializer.Deserialize<DecisionRow>(CheckerPlayRowGolden, options)!;
            var cubeRow = JsonSerializer.Deserialize<DecisionRow>(CubeRowGolden, options)!;

            Assert.IsType<CheckerPlayDecision>(play);
            Assert.IsType<CubeDecision>(cube);
            Assert.Equal(CheckerPlayGolden, JsonSerializer.Serialize(play, options));
            Assert.Equal(CubeGolden, JsonSerializer.Serialize(cube, options));
            Assert.Equal(CheckerPlayRowGolden, JsonSerializer.Serialize(playRow, options));
            Assert.Equal(CubeRowGolden, JsonSerializer.Serialize(cubeRow, options));
            Assert.Equal(Mop, play.Position.Mop);
        }
    }

    [Fact]
    public void Goldens_KindIsWrittenFirst()
    {
        // Added: the kind is the first member of every decision document.
        Assert.StartsWith("{\"Kind\":\"CheckerPlay\",", CheckerPlayGolden);
        Assert.StartsWith("{\"Kind\":\"Cube\",", CubeGolden);
        Assert.StartsWith("{\"Kind\":\"CheckerPlay\",", CheckerPlayRowGolden);
        Assert.StartsWith("{\"Kind\":\"Cube\",", CubeRowGolden);
    }

    [Fact]
    public void RowAfterBoards_AreTheBoardsThePreviousGoldenStored()
    {
        // Added: the derivation changed where the boards come from, not what
        // they are — the row's boards, taken from the record's derivation,
        // are the previous golden's stored boards byte for byte.
        using var previous = JsonDocument.Parse(RowGoldenBeforeKinds);
        using var today = JsonDocument.Parse(CheckerPlayRowGolden);

        foreach (var board in new[] { "AfterBestBoard", "AfterPlayerBoard" })
            Assert.Equal(
                previous.RootElement.GetProperty(board).GetRawText(),
                today.RootElement.GetProperty(board).GetRawText());
    }

    [Fact]
    public void FullRecord_DropsEveryStandInAndStoredCopy_OfThePreviousShape()
    {
        // Rewritten from FullRecord_DiffersFromA5eca85_OnlyByTheAbsentMoveNotationAndGameAndMove:
        // the delta from the previous shape is no longer one member, so it is
        // pinned member by member. Gone from the checker play: the kind
        // switch, every cube member (they held stand-ins on a checker play),
        // and the stored after-boards and their category. Gone from both
        // kinds: the other kind's members. Kept: every checker-play value.
        string[] retired =
        [
            "\"IsCube\"", "\"CubeDepth\"", "\"CubeAnalysisMode\"", "\"NoDoubleEquity\"", "\"UserDoubleError\"",
            "\"UserDoublerAction\"", "\"Outcome\"", "\"AfterBestBoard\"", "\"MoveNotation\"", "\"Game\"", "\"MoveNumber\"",
        ];
        foreach (var member in retired)
        {
            Assert.Contains(member, RecordGoldenAtA5eca85);
            Assert.DoesNotContain(member, CheckerPlayGolden);
        }
        foreach (var member in new[] { "\"Dice\"", "\"Plays\"", "\"BestPlayIndex\"", "\"UserPlayIndex\"", "\"UserPlayError\"" })
            Assert.DoesNotContain(member, CubeGolden);

        using var previous = JsonDocument.Parse(RecordGoldenBeforeKinds);
        using var today = JsonDocument.Parse(CheckerPlayGolden);
        foreach (var category in new[] { "Id", "Xgid", "Position", "Descriptive" })
            Assert.Equal(
                previous.RootElement.GetProperty(category).GetRawText(),
                today.RootElement.GetProperty(category).GetRawText());
        foreach (var member in new[] { "Dice", "Plays", "BestPlayIndex", "UserPlayIndex", "UserPlayError" })
            Assert.Equal(
                previous.RootElement.GetProperty("Decision").GetProperty(member).GetRawText(),
                today.RootElement.GetProperty("Decision").GetProperty(member).GetRawText());
    }

    public static TheoryData<string, string> OldShapeRecords => new()
    {
        { "a5eca85", RecordGoldenAtA5eca85 },
        { "before the kinds", RecordGoldenBeforeKinds },
    };

    [Theory]
    [MemberData(nameof(OldShapeRecords))]
    public void OldShapeRecord_IsRefused_AsAJsonException_BothPaths(string name, string json)
    {
        // Rewritten from A5eca85Record_CarryingMoveNotation_ReadsOnBothPaths_TheRetiredMemberIgnored:
        // a document without a kind is not a decision of this shape, and it
        // is refused as malformed — a JsonException, absorbed wherever the
        // library absorbs malformed input — never read as a guessed kind.
        foreach (var options in new[] { ReflectionOptions, ContextOptions })
        {
            var ex = Record.Exception(() => JsonSerializer.Deserialize<BgDecisionData>(json, options));
            Assert.True(ex is JsonException, $"{name}: {ex?.GetType().Name ?? "loaded"}");
            Assert.Contains("Kind", ex!.Message);
        }
    }

    [Theory]
    [MemberData(nameof(OldShapeRecords))]
    public void OldShapeRecord_WithAKindAdded_IsStillRefused_BothPaths(string name, string json)
    {
        // Added: a kind prepended does not make an old document readable —
        // its members contradict the kind (a checker play has no IsCube, no
        // cube member, no Outcome) and are refused as members of no decision.
        var withKind = "{\"Kind\":\"CheckerPlay\"," + json[1..];

        foreach (var options in new[] { ReflectionOptions, ContextOptions })
        {
            var ex = Record.Exception(() => JsonSerializer.Deserialize<BgDecisionData>(withKind, options));
            Assert.True(ex is JsonException, $"{name}: {ex?.GetType().Name ?? "loaded"}");
        }
    }

    [Fact]
    public void OldShapeRow_IsRefused_AsAJsonException_BothPaths()
    {
        // Added: the retired row shape states no kind, and the kind is a
        // required column — never read off an empty or zero roll.
        foreach (var options in new[] { ReflectionOptions, ContextOptions })
        {
            var ex = Record.Exception(() => JsonSerializer.Deserialize<DecisionRow>(RowGoldenBeforeKinds, options));
            Assert.True(ex is JsonException, ex?.GetType().Name ?? "loaded");
            Assert.Contains("Kind", ex!.Message);
        }
    }
}
