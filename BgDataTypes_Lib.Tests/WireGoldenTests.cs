using System.Text.Json;
using System.Text.Json.Nodes;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The bytes of a full record of each kind and of each kind's row, on both
/// the reflection path and the source-generated context. A full record has
/// every member present, and every nullable member non-null that the
/// record's rules let be — the user's listed play and an unlisted play's
/// error exclude each other, as each cube half's stated action and that
/// half's unstated-action error do — so every member is on the wire.
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
/// <para>
/// <b>No stored copy of a derivable value</b> (the umbrella's verdict on the
/// same leg) took the rest off the wire: the pip counts (the board's), the
/// source file (the Id's), each depth rank (the mode and level's), each
/// equity loss, the best play's index and the user's error (the candidates'
/// equities'), and the error of each stated cube action (the equities'). The
/// one error still stored is the one nothing determines.
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
        position: TestRecords.Position(
            mop: Mop, cubeSize: 2, cubeOwner: CubeOwner.Opponent,
            session: TestRecords.MatchSession(length: 7, onRollNeeds: 3, opponentNeeds: 5, isCrawford: false)),
        decision: TestRecords.CheckerPlayData(
            dice: [5, 4],
            plays:
            [
                TestRecords.Candidate(
                    play: Best,
                    analysisMode: AnalysisMode.Evaluation, analysisLevel: AnalysisLevel.Ply3,
                    equity: 0.25, winPct: 0.61, winGammonPct: 0.21, winBgPct: 0.01,
                    loseGammonPct: 0.11, loseBgPct: 0.02),
                TestRecords.Candidate(
                    play: User,
                    analysisMode: AnalysisMode.Rollout, analysisLevel: AnalysisLevel.XgRoller, rolloutTrials: 1296,
                    equity: 0.125, winPct: 0.58, winGammonPct: 0.19, winBgPct: 0.015,
                    loseGammonPct: 0.12, loseBgPct: 0.025),
            ],
            userPlayIndex: 1),
        descriptive: TestRecords.Descriptive(
            onRollName: "Alice", opponentName: "Bob", title: "Golden",
            date: new DateOnly(2026, 9, 25), @event: "Club",
            isStandardStart: true, comment: "note", flagged: true));

    /// <summary>
    /// The cube half the previous golden carried on its checker play, now a
    /// cube decision of its own, every member stated. The user doubled where
    /// no double (+0.75) beats double/take (+0.5) — a doubling error of 0.25,
    /// derived — and the opponent's response is not stated, so the analyser's
    /// take error stands for it. It is played for money without the Jacoby
    /// rule — the match-context leg of halheinrich/backgammon#273 made it a
    /// money session, where the previous golden stated a match's scores beside
    /// a Jacoby stamp, so the goldens pin both session kinds.
    /// </summary>
    internal static CubeDecision FullCubeRecord() => TestRecords.Cube(
        id: new XgDecisionId("golden.xg", 3, 18, true),
        position: TestRecords.Position(
            mop: Mop, cubeSize: 2, cubeOwner: CubeOwner.OnRoll,
            session: TestRecords.MoneySession(isJacoby: false, isBeaver: true, cubeLimit: 64, onRollScore: 3, opponentScore: 5)),
        decision: TestRecords.CubeData(
            analysisMode: AnalysisMode.BookRollout, analysisLevel: AnalysisLevel.Ply4,
            rolloutTrials: 12960, bookEdition: BookEdition.V2,
            noDoubleEquity: 0.75, doubleTakeEquity: 0.5, cubelessNoDoubleEquity: 0.375,
            cubelessDoubleTakeEquity: 0.625,
            winPctAfterNoDouble: 0.51, gammonPctAfterNoDouble: 0.12, bgPctAfterNoDouble: 0.013,
            loseGammonPctAfterNoDouble: 0.14, loseBgPctAfterNoDouble: 0.015,
            winPctAfterDoubleTake: 0.52, gammonPctAfterDoubleTake: 0.16, bgPctAfterDoubleTake: 0.017,
            loseGammonPctAfterDoubleTake: 0.18, loseBgPctAfterDoubleTake: 0.019,
            userDoublerAction: CubeAction.Double, userTakerAction: null, unstatedTakerActionError: 0.04),
        descriptive: TestRecords.Descriptive(
            onRollName: "Alice", opponentName: "Bob", title: "Golden",
            date: new DateOnly(2026, 9, 25), @event: "Club",
            isStandardStart: true, comment: "note", flagged: true));

    // The play row under the default ranking, the cube row under the other,
    // so the goldens pin both of the Ranking column's tokens.
    internal static DecisionRow FullRow() => DecisionRow.From(FullRecord(), PlayRanking.Equity);

    internal static DecisionRow FullCubeRow() => DecisionRow.From(FullCubeRecord(), PlayRanking.DepthFirst);

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
    //  The shape at ca83ab1, before the session kinds — documents the
    //  library must refuse: a position states no session, and a row no
    //  session kind (halheinrich/backgammon#273, the match-context leg). The
    //  match context was spelled flat — away scores and a Crawford flag in
    //  the position, beside a Jacoby stamp; the length in the descriptive
    //  category — where money was spelled with 0s.
    // -----------------------------------------------------------------------

    private const string CheckerPlayGoldenAtCa83ab1 =
        """{"Kind":"CheckerPlay","Id":"golden.xg:g3:m17:play","Xgid":"XGID=golden","Position":{"Mop":[-1,0,2,-1,2,2,4,1,2,-1,0,1,-2,0,0,0,-2,0,0,-2,-2,1,-2,0,-2,0],"OnRollNeeds":3,"OpponentNeeds":5,"CubeSize":2,"CubeOwner":"Opponent","IsCrawford":false,"IsJacoby":false},"Descriptive":{"MatchLength":7,"OnRollName":"Alice","OpponentName":"Bob","Title":"Golden","Date":"2026-09-25","Event":"Club","IsStandardStart":true,"Comment":"note","Flagged":true},"Decision":{"Dice":[5,4],"Plays":[{"Play":[{"FrPt":8,"ToPt":-3},{"FrPt":7,"ToPt":3}],"AnalysisMode":"Evaluation","AnalysisLevel":"Ply3","RolloutTrials":null,"BookEdition":null,"UnrecognizedLevelCode":null,"Equity":0.25,"WinPct":0.61,"WinGammonPct":0.21,"WinBgPct":0.01,"LoseGammonPct":0.11,"LoseBgPct":0.02},{"Play":[{"FrPt":11,"ToPt":6},{"FrPt":7,"ToPt":-3}],"AnalysisMode":"Rollout","AnalysisLevel":"XgRoller","RolloutTrials":1296,"BookEdition":null,"UnrecognizedLevelCode":null,"Equity":0.125,"WinPct":0.58,"WinGammonPct":0.19,"WinBgPct":0.015,"LoseGammonPct":0.12,"LoseBgPct":0.025}],"UserPlayIndex":1,"UnlistedPlayError":null}}""";

    private const string CubeGoldenAtCa83ab1 =
        """{"Kind":"Cube","Id":"golden.xg:g3:m18:cube","Xgid":"XGID=golden-cube","Position":{"Mop":[-1,0,2,-1,2,2,4,1,2,-1,0,1,-2,0,0,0,-2,0,0,-2,-2,1,-2,0,-2,0],"OnRollNeeds":3,"OpponentNeeds":5,"CubeSize":2,"CubeOwner":"OnRoll","IsCrawford":false,"IsJacoby":false},"Descriptive":{"MatchLength":7,"OnRollName":"Alice","OpponentName":"Bob","Title":"Golden","Date":"2026-09-25","Event":"Club","IsStandardStart":true,"Comment":"note","Flagged":true},"Decision":{"AnalysisMode":"BookRollout","AnalysisLevel":"Ply4","RolloutTrials":12960,"BookEdition":"V2","UnrecognizedLevelCode":null,"NoDoubleEquity":0.75,"DoubleTakeEquity":0.5,"CubelessNoDoubleEquity":0.375,"CubelessDoubleTakeEquity":0.625,"WinPctAfterNoDouble":0.51,"GammonPctAfterNoDouble":0.12,"BgPctAfterNoDouble":0.013,"LoseGammonPctAfterNoDouble":0.14,"LoseBgPctAfterNoDouble":0.015,"WinPctAfterDoubleTake":0.52,"GammonPctAfterDoubleTake":0.16,"BgPctAfterDoubleTake":0.017,"LoseGammonPctAfterDoubleTake":0.18,"LoseBgPctAfterDoubleTake":0.019,"ProbOfOpponentErrorJustifyingDouble":0.2,"UserDoublerAction":"Double","UserTakerAction":null,"UnstatedDoublerActionError":null,"UnstatedTakerActionError":0.04}}""";

    private const string CheckerPlayRowGoldenAtCa83ab1 =
        """{"Kind":"CheckerPlay","Id":"golden.xg:g3:m17:play","Xgid":"XGID=golden","Ranking":"Equity","Error":0.125,"Result":"Scored","MatchLength":7,"Player":"Alice","IsStandardStart":true,"Roll":54,"AnalysisDepth":"3-ply","AnalysisMode":"Evaluation","AnalysisLevel":"Ply3","Equity":0.25,"OnRollNeeds":3,"OpponentNeeds":5,"IsCrawford":false,"IsJacoby":false,"Board":[-1,0,2,-1,2,2,4,1,2,-1,0,1,-2,0,0,0,-2,0,0,-2,-2,1,-2,0,-2,0],"AfterBestBoard":[0,2,0,2,-1,2,2,0,0,2,0,0,0,2,-1,0,1,-1,0,-4,-2,-2,-2,-2,0,2],"AfterPlayerBoard":[0,2,0,2,-1,2,2,0,0,2,0,0,0,2,0,0,1,-2,0,-5,-2,-2,-1,-2,0,2]}""";

    private const string CubeRowGoldenAtCa83ab1 =
        """{"Kind":"Cube","Id":"golden.xg:g3:m18:cube","Xgid":"XGID=golden-cube","Ranking":"DepthFirst","Error":0.25,"Result":"Scored","MatchLength":7,"Player":"Alice","IsStandardStart":true,"Roll":null,"AnalysisDepth":"Book V2: 12960 trials. 4-ply","AnalysisMode":"BookRollout","AnalysisLevel":"Ply4","Equity":0.75,"OnRollNeeds":3,"OpponentNeeds":5,"IsCrawford":false,"IsJacoby":false,"Board":[-1,0,2,-1,2,2,4,1,2,-1,0,1,-2,0,0,0,-2,0,0,-2,-2,1,-2,0,-2,0],"AfterBestBoard":null,"AfterPlayerBoard":null}""";

    // -----------------------------------------------------------------------
    //  Today's shape
    // -----------------------------------------------------------------------

    private const string CheckerPlayGolden =
        """{"Kind":"CheckerPlay","Id":"golden.xg:g3:m17:play","Position":{"Mop":[-1,0,2,-1,2,2,4,1,2,-1,0,1,-2,0,0,0,-2,0,0,-2,-2,1,-2,0,-2,0],"CubeSize":2,"CubeOwner":"Opponent","Session":{"Terms":{"Kind":"Match","Length":7},"OnRollNeeds":3,"OpponentNeeds":5,"IsCrawford":false}},"Descriptive":{"OnRollName":"Alice","OpponentName":"Bob","Title":"Golden","Date":"2026-09-25","Event":"Club","IsStandardStart":true,"Comment":"note","Flagged":true},"Decision":{"Dice":[5,4],"Plays":[{"Play":[{"FrPt":8,"ToPt":-3},{"FrPt":7,"ToPt":3}],"AnalysisMode":"Evaluation","AnalysisLevel":"Ply3","RolloutTrials":null,"BookEdition":null,"UnrecognizedLevelCode":null,"Equity":0.25,"WinPct":0.61,"WinGammonPct":0.21,"WinBgPct":0.01,"LoseGammonPct":0.11,"LoseBgPct":0.02},{"Play":[{"FrPt":11,"ToPt":6},{"FrPt":7,"ToPt":-3}],"AnalysisMode":"Rollout","AnalysisLevel":"XgRoller","RolloutTrials":1296,"BookEdition":null,"UnrecognizedLevelCode":null,"Equity":0.125,"WinPct":0.58,"WinGammonPct":0.19,"WinBgPct":0.015,"LoseGammonPct":0.12,"LoseBgPct":0.025}],"UserPlayIndex":1,"UnlistedPlayError":null}}""";

    private const string CubeGolden =
        """{"Kind":"Cube","Id":"golden.xg:g3:m18:cube","Position":{"Mop":[-1,0,2,-1,2,2,4,1,2,-1,0,1,-2,0,0,0,-2,0,0,-2,-2,1,-2,0,-2,0],"CubeSize":2,"CubeOwner":"OnRoll","Session":{"Terms":{"Kind":"Money","IsJacoby":false,"IsBeaver":true,"CubeLimit":64},"OnRollScore":3,"OpponentScore":5}},"Descriptive":{"OnRollName":"Alice","OpponentName":"Bob","Title":"Golden","Date":"2026-09-25","Event":"Club","IsStandardStart":true,"Comment":"note","Flagged":true},"Decision":{"AnalysisMode":"BookRollout","AnalysisLevel":"Ply4","RolloutTrials":12960,"BookEdition":"V2","UnrecognizedLevelCode":null,"NoDoubleEquity":0.75,"DoubleTakeEquity":0.5,"CubelessNoDoubleEquity":0.375,"CubelessDoubleTakeEquity":0.625,"WinPctAfterNoDouble":0.51,"GammonPctAfterNoDouble":0.12,"BgPctAfterNoDouble":0.013,"LoseGammonPctAfterNoDouble":0.14,"LoseBgPctAfterNoDouble":0.015,"WinPctAfterDoubleTake":0.52,"GammonPctAfterDoubleTake":0.16,"BgPctAfterDoubleTake":0.017,"LoseGammonPctAfterDoubleTake":0.18,"LoseBgPctAfterDoubleTake":0.019,"UserDoublerAction":"Double","UserTakerAction":null,"UnstatedDoublerActionError":null,"UnstatedTakerActionError":0.04}}""";

    private const string CheckerPlayRowGolden =
        """{"Kind":"CheckerPlay","Id":"golden.xg:g3:m17:play","Xgid":"XGID=a-BaBBDABa-Ab---b--bbAb-b-:1:-1:1:54:4:2:0:7:10","Ranking":"Equity","Error":0.125,"Result":"Scored","Player":"Alice","IsStandardStart":true,"Roll":54,"AnalysisDepth":"3-ply","AnalysisMode":"Evaluation","AnalysisLevel":"Ply3","Equity":0.25,"SessionKind":"Match","MatchLength":7,"OnRollNeeds":3,"OpponentNeeds":5,"IsCrawford":false,"IsJacoby":null,"IsBeaver":null,"CubeLimit":null,"OnRollScore":null,"OpponentScore":null,"Board":[-1,0,2,-1,2,2,4,1,2,-1,0,1,-2,0,0,0,-2,0,0,-2,-2,1,-2,0,-2,0],"AfterBestBoard":[0,2,0,2,-1,2,2,0,0,2,0,0,0,2,-1,0,1,-1,0,-4,-2,-2,-2,-2,0,2],"AfterPlayerBoard":[0,2,0,2,-1,2,2,0,0,2,0,0,0,2,0,0,1,-2,0,-5,-2,-2,-1,-2,0,2]}""";

    private const string CubeRowGolden =
        """{"Kind":"Cube","Id":"golden.xg:g3:m18:cube","Xgid":"XGID=a-BaBBDABa-Ab---b--bbAb-b-:1:1:1:00:3:5:2:0:6","Ranking":"DepthFirst","Error":0.25,"Result":"Scored","Player":"Alice","IsStandardStart":true,"Roll":null,"AnalysisDepth":"Book V2: 12960 trials. 4-ply","AnalysisMode":"BookRollout","AnalysisLevel":"Ply4","Equity":0.75,"SessionKind":"Money","MatchLength":null,"OnRollNeeds":null,"OpponentNeeds":null,"IsCrawford":null,"IsJacoby":false,"IsBeaver":true,"CubeLimit":64,"OnRollScore":3,"OpponentScore":5,"Board":[-1,0,2,-1,2,2,4,1,2,-1,0,1,-2,0,0,0,-2,0,0,-2,-2,1,-2,0,-2,0],"AfterBestBoard":null,"AfterPlayerBoard":null}""";

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

    /// <summary>
    /// The members the verdict's "no stored copy of a derivable value" took
    /// off the wire: each is derived now from what the record still stores.
    /// </summary>
    private static readonly string[] StoredCopies =
    [
        "OnRollPipCount", "OpponentPipCount", "SourceFile", "DepthRank", "EquityLoss",
        "BestPlayIndex", "UserPlayError", "UserDoubleError", "UserTakeError",
        "LosePct", "LosePctAfterNoDouble", "LosePctAfterDoubleTake",
        "Depth", "DepthAbbreviation",
    ];

    /// <summary>The typed depth facts that replaced the stored depth text.</summary>
    private static readonly string[] DepthFacts = ["RolloutTrials", "BookEdition", "UnrecognizedLevelCode"];

    /// <summary><paramref name="node"/> with every <see cref="StoredCopies"/> member removed, at any depth.</summary>
    private static JsonNode WithoutStoredCopies(JsonNode node) => Without(node, StoredCopies);

    /// <summary><paramref name="node"/> with every member named in <paramref name="names"/> removed, at any depth.</summary>
    private static JsonNode Without(JsonNode node, string[] names)
    {
        switch (node)
        {
            case JsonObject members:
                foreach (var name in names)
                    members.Remove(name);
                foreach (var (_, child) in members)
                    if (child is not null)
                        Without(child, names);
                break;
            case JsonArray items:
                foreach (var item in items)
                    if (item is not null)
                        Without(item, names);
                break;
        }
        return node;
    }

    [Fact]
    public void FullRecord_DropsEveryStandInAndStoredCopy_OfThePreviousShape()
    {
        // Rewritten from FullRecord_DiffersFromA5eca85_OnlyByTheAbsentMoveNotationAndGameAndMove:
        // the delta from the previous shape is no longer one member, so it is
        // pinned member by member. Gone from the checker play: the kind
        // switch, every cube member (they held stand-ins on a checker play),
        // and the stored after-boards and their category. Gone from both
        // kinds: the other kind's members. Rewritten again for the verdict's
        // "no stored copy of a derivable value": gone too is every member of
        // StoredCopies. Kept: every other checker-play value, value for value.
        // Gone since the match-context leg: the stored XGID, derived now.
        string[] retired =
        [
            "\"IsCube\"", "\"CubeDepth\"", "\"CubeAnalysisMode\"", "\"NoDoubleEquity\"",
            "\"UserDoublerAction\"", "\"Outcome\"", "\"AfterBestBoard\"", "\"MoveNotation\"", "\"Game\"", "\"MoveNumber\"",
            "\"Xgid\"",
            .. StoredCopies.Select(member => $"\"{member}\""),
        ];
        foreach (var member in retired)
        {
            Assert.Contains(member, RecordGoldenAtA5eca85);
            Assert.DoesNotContain(member, CheckerPlayGolden);
        }
        foreach (var member in new[] { "\"Dice\"", "\"Plays\"", "\"UserPlayIndex\"", "\"UnlistedPlayError\"" })
            Assert.DoesNotContain(member, CubeGolden);
        foreach (var member in StoredCopies)
        {
            Assert.DoesNotContain($"\"{member}\"", CubeGolden);
            Assert.DoesNotContain($"\"{member}\"", CheckerPlayRowGolden);
        }

        // The typed depth facts are new, not kept: they replaced the stored
        // depth text, which the previous shape spelled as prose. Rewritten
        // again for the session kinds: the match context moved into the
        // position's session, pinned member by member in
        // FullRecord_MovesTheMatchContextIntoTheSession_OfTheCa83ab1Shape, so
        // the categories compare here without it.
        // And again for the derived XGID: it left the wire
        // (FullRecord_DerivesItsXgid_WhichLeftTheWire).
        var previous = WithoutStoredCopies(JsonNode.Parse(RecordGoldenBeforeKinds)!);
        var today = Without(JsonNode.Parse(CheckerPlayGolden)!, DepthFacts);
        Without(previous, [.. MatchContext, "Xgid"]);
        Without(today, ["Session"]);
        foreach (var category in new[] { "Id", "Position", "Descriptive" })
            Assert.True(JsonNode.DeepEquals(previous[category], today[category]), category);
        foreach (var member in new[] { "Dice", "Plays", "UserPlayIndex" })
            Assert.True(JsonNode.DeepEquals(previous["Decision"]![member], today["Decision"]![member]), member);
    }

    /// <summary>The match context's flat members before the session kinds: the position's four and the descriptive length.</summary>
    private static readonly string[] MatchContext = ["OnRollNeeds", "OpponentNeeds", "IsCrawford", "IsJacoby", "MatchLength"];

    [Fact]
    public void FullRecord_MovesTheMatchContextIntoTheSession_OfTheCa83ab1Shape()
    {
        // Added for the session kinds (halheinrich/backgammon#273). The match
        // context left its flat members — the position's away scores,
        // Crawford flag and Jacoby stamp, the descriptive category's length —
        // for one member, the position's session. Rewritten for the composed
        // session: the session's terms come first and state its kind, the
        // length is the terms', and the away scores and Crawford flag stand
        // beside them. The checker play's is a match: its length, away scores
        // and Crawford flag carried value for value. The stamp is gone, not
        // moved: it was a Jacoby fact on a match, which has none. Nothing
        // else changed.
        var previous = JsonNode.Parse(CheckerPlayGoldenAtCa83ab1)!;
        var today = JsonNode.Parse(CheckerPlayGolden)!;

        foreach (var member in MatchContext)
        {
            Assert.False(today["Position"]!.AsObject().ContainsKey(member), member);
            Assert.False(today["Descriptive"]!.AsObject().ContainsKey(member), member);
        }
        var session = today["Position"]!["Session"]!;
        Assert.Null(session["Kind"]);
        Assert.Equal("Match", (string?)session["Terms"]!["Kind"]);
        Assert.Equal((int)previous["Descriptive"]!["MatchLength"]!, (int)session["Terms"]!["Length"]!);
        foreach (var member in new[] { "OnRollNeeds", "OpponentNeeds", "IsCrawford" })
            Assert.True(JsonNode.DeepEquals(previous["Position"]![member], session[member]), member);
        Assert.Null(session["IsJacoby"]);
        Assert.Null(session["Terms"]!["IsJacoby"]);

        // Everything else, value for value — but the stored XGID, derived now
        // (FullRecord_DerivesItsXgid_WhichLeftTheWire).
        var rest = Without(JsonNode.Parse(CheckerPlayGoldenAtCa83ab1)!, [.. MatchContext, "Xgid"]);
        Assert.True(JsonNode.DeepEquals(rest, Without(JsonNode.Parse(CheckerPlayGolden)!, ["Session"])));

        // The cube golden became a money session: the match scores and the
        // stamp it carried are gone, the rules and the limit are the money
        // session's terms, and the scores stand beside them.
        var cube = JsonNode.Parse(CubeGolden)!;
        Assert.True(JsonNode.DeepEquals(
            JsonNode.Parse("""{"Terms":{"Kind":"Money","IsJacoby":false,"IsBeaver":true,"CubeLimit":64},"OnRollScore":3,"OpponentScore":5}"""),
            cube["Position"]!["Session"]));
    }

    [Fact]
    public void FullRecord_DerivesItsXgid_WhichLeftTheWire()
    {
        // Added for the derived XGID (halheinrich/backgammon#273): no record
        // document states one, each record derives it from its members, and
        // the row carries the record's derivation as its column. The literals
        // are the XGIDs of the golden positions, field by field (XgidEncoder):
        // the tester's board; a cube of 2 (exponent 1) the opponent's (-1) or
        // the player on roll's (1); the roll, or 00 for the cube; the match's
        // scores 7-3 and 7-5 won, or the money session's 3 and 5; Crawford off,
        // or no Jacoby plus beaver (2); the length 7, or money's 0; the match's
        // 10, or the money limit's exponent 6.
        const string playXgid = "XGID=a-BaBBDABa-Ab---b--bbAb-b-:1:-1:1:54:4:2:0:7:10";
        const string cubeXgid = "XGID=a-BaBBDABa-Ab---b--bbAb-b-:1:1:1:00:3:5:2:0:6";

        Assert.DoesNotContain("\"Xgid\"", CheckerPlayGolden);
        Assert.DoesNotContain("\"Xgid\"", CubeGolden);
        Assert.Contains("\"Xgid\"", CheckerPlayGoldenAtCa83ab1);
        Assert.Equal(playXgid, FullRecord().Xgid);
        Assert.Equal(cubeXgid, FullCubeRecord().Xgid);
        Assert.Contains($"\"Xgid\":\"{playXgid}\"", CheckerPlayRowGolden);
        Assert.Contains($"\"Xgid\":\"{cubeXgid}\"", CubeRowGolden);
        foreach (var options in new[] { ReflectionOptions, ContextOptions })
            Assert.Equal(cubeXgid, JsonSerializer.Deserialize<BgDecisionData>(CubeGolden, options)!.Xgid);
    }

    [Fact]
    public void ACubeDocumentStatingTheRemovedOpponentErrorFigure_IsRefused_BothPaths()
    {
        // Added when ProbOfOpponentErrorJustifyingDouble left the record
        // (halheinrich/backgammon#273, Hal's ruling of 2026-09-27): XG stores
        // no such value, so the record claimed as stored a figure no source
        // states. The ca83ab1 cube golden carried it and today's does not. A
        // document still stating it is refused as the category refuses any
        // member it does not have — read as the category, as its kind or as a
        // record — never read with the member dropped. Deriving XG's "Pass
        // Justifying Dbl" figure is separate work (halheinrich/backgammon#288).
        const string removed = "ProbOfOpponentErrorJustifyingDouble";
        Assert.Contains($"\"{removed}\"", CubeGoldenAtCa83ab1);
        Assert.DoesNotContain($"\"{removed}\"", CubeGolden);

        var record = JsonNode.Parse(CubeGolden)!.AsObject();
        record["Decision"]![removed] = 0.2;
        (string Json, Type ReadAs)[] documents =
        [
            (record.ToJsonString(), typeof(BgDecisionData)),
            (record.ToJsonString(), typeof(CubeDecision)),
            (record["Decision"]!.ToJsonString(), typeof(CubeDecisionData)),
        ];

        foreach (var (json, readAs) in documents)
            foreach (var options in new[] { ReflectionOptions, ContextOptions })
            {
                var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(json, readAs, options));
                Assert.Contains(removed, ex.Message);
            }
    }

    public static TheoryData<string, string, Type> Ca83ab1Shapes => new()
    {
        { "play", CheckerPlayGoldenAtCa83ab1, typeof(BgDecisionData) },
        { "cube", CubeGoldenAtCa83ab1, typeof(BgDecisionData) },
        { "play row", CheckerPlayRowGoldenAtCa83ab1, typeof(DecisionRow) },
        { "cube row", CubeRowGoldenAtCa83ab1, typeof(DecisionRow) },
    };

    [Theory]
    [MemberData(nameof(Ca83ab1Shapes))]
    public void Ca83ab1Shape_IsRefused_AsAJsonException_BothPaths(string name, string json, Type readAs)
    {
        // Added: a position that states no session, and a row that states no
        // session kind, are not documents of this shape — refused as
        // malformed, never read with money guessed from a 0.
        foreach (var options in new[] { ReflectionOptions, ContextOptions })
        {
            var ex = Record.Exception(() => JsonSerializer.Deserialize(json, readAs, options));
            Assert.True(ex is JsonException, $"{name}: {ex?.GetType().Name ?? "loaded"}");
        }
    }

    [Fact]
    public void FullRecords_DeriveTheirDepthText_FromTheTypedFacts()
    {
        // Added: the label and abbreviation of each analysis in the goldens,
        // derived from the facts they store — an evaluation, a rollout and a
        // book hit whose rollout parameters were recovered.
        var play = FullRecord().Decision.Plays;
        var cube = FullCubeRecord().Decision;

        Assert.Equal(("3-ply", "3-ply"), (play[0].Depth, play[0].DepthAbbreviation));
        Assert.Equal(("Rollout: 1296 trials. XG Roller", "Rp1296"), (play[1].Depth, play[1].DepthAbbreviation));
        Assert.Equal(("Book V2: 12960 trials. 4-ply", "B4_12960"), (cube.Depth, cube.DepthAbbreviation));
        Assert.Contains("\"AnalysisDepth\":\"Book V2: 12960 trials. 4-ply\"", CubeRowGolden);
    }

    [Fact]
    public void FullRecords_DeriveWhatThePreviousShapeStored()
    {
        // Added: the values the previous golden stated are the ones the
        // records now derive — the source file from the id, the best play and
        // the user's error from the equities, under the equity ranking the
        // previous shape's stored ones followed — except the pip counts. The
        // previous fixture stated 130 and 145 for a board whose counts are
        // not those: a stored copy disagreeing with what it copies, which the
        // derivation makes impossible.
        using var previous = JsonDocument.Parse(RecordGoldenBeforeKinds);
        var position = previous.RootElement.GetProperty("Position");
        var decision = previous.RootElement.GetProperty("Decision");
        var play = FullRecord();

        Assert.Equal(new BoardState(Mop).PipCount, play.Position.OnRollPipCount);
        Assert.Equal(new BoardState(Mop).OpponentPipCount, play.Position.OpponentPipCount);
        Assert.NotEqual(position.GetProperty("OnRollPipCount").GetInt32(), play.Position.OnRollPipCount);
        Assert.NotEqual(position.GetProperty("OpponentPipCount").GetInt32(), play.Position.OpponentPipCount);
        Assert.Equal(previous.RootElement.GetProperty("Descriptive").GetProperty("SourceFile").GetString(), play.SourceFile);
        var byEquity = play.Decision.RankedBy(PlayRanking.Equity);
        Assert.Equal(decision.GetProperty("BestPlayIndex").GetInt32(), byEquity.Best.Index);
        Assert.Equal(PlayerResult.Scored(decision.GetProperty("UserPlayError").GetDouble()), byEquity.PlayerResult);
        Assert.Equal(decision.GetProperty("Plays")[1].GetProperty("EquityLoss").GetDouble(), byEquity.ForCandidate(1).Error);
        Assert.Equal(decision.GetProperty("Plays")[0].GetProperty("LosePct").GetDouble(), play.Decision.Plays[0].LosePct!.Value, 12);
        Assert.Equal(decision.GetProperty("LosePctAfterNoDouble").GetDouble(), FullCubeRecord().Decision.LosePctAfterNoDouble, 12);
        Assert.Equal(decision.GetProperty("LosePctAfterDoubleTake").GetDouble(), FullCubeRecord().Decision.LosePctAfterDoubleTake, 12);
        Assert.Equal(0.25, FullCubeRecord().Decision.UserDoubleError);
        Assert.Equal(0.04, FullCubeRecord().Decision.UserTakeError);
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
        // required column — never read off an empty or zero roll. It states
        // no result either, beside an error. Which check reports first
        // differs by path (measured on .NET 10): the generated context checks
        // required members first and names the missing Kind; the reflection
        // path runs the row's read-back check first, which sees an absent
        // member as its default and refuses the error beside no result.
        // Which rule reports is incidental; the refusal is the pin, and the
        // absence walk pins each missing column on its own.
        foreach (var options in new[] { ReflectionOptions, ContextOptions })
        {
            var ex = Record.Exception(() => JsonSerializer.Deserialize<DecisionRow>(RowGoldenBeforeKinds, options));
            Assert.True(ex is JsonException, ex?.GetType().Name ?? "loaded");
        }
    }
}
