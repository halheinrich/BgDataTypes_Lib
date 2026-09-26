namespace BgDataTypes_Lib.TestSupport;

/// <summary>
/// The record builders — the one way tests build decision records, here and
/// in every consumer. Each builder yields a well-formed value of its type with
/// realistic defaults, and takes the members a test cares about as named
/// arguments spelled as the members are, so a test states only what it cares
/// about and never restates a record's construction.
/// </summary>
/// <remarks>
/// <para>
/// <b>The defaults.</b> A checker play is the opening 3-1 from the standard
/// start at 0-0 in a 7-point match, with three analysed candidates (8/5 6/5
/// best, and the user playing it). A cube decision is a double/take in a
/// short race at the same score. Both sit in a game of an <c>.xg</c> match
/// (an <see cref="XgDecisionId"/>), so their game facts are stated.
/// </para>
/// <para>
/// <b>What adapts.</b> A record builder's defaults follow the arguments a test
/// gives where the rules of the records demand it: a standalone position's id
/// (an <see cref="XgpDecisionId"/>) gets a descriptive category with no
/// <see cref="DescriptiveData.IsStandardStart"/>, and a checker play on a board
/// other than the standard start gets one candidate, the pass — the one play
/// valid from every position — unless the test states its own decision.
/// Nothing else adapts: a test that states a decision and a board states
/// candidates valid from that board, since a record cannot hold any other
/// (<see cref="BoardState.IsSamePlay"/>).
/// </para>
/// <para>
/// A test whose subject is construction itself (an init guard, or the order
/// members are set in) writes its own object initializer instead.
/// </para>
/// </remarks>
public static class TestRecords
{
    // -----------------------------------------------------------------------
    //  The realistic defaults
    // -----------------------------------------------------------------------

    /// <summary>A short race: the player on roll leads 54 pips to 65 — a double/take.</summary>
    private static readonly BoardPosition RacePosition = new(
        [0, 2, 2, 3, 3, 3, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -4, -4, -3, -2, -1, -1, 0]);

    /// <summary>The standard start with a 3-1 to play, at 0-0 in a 7-point match.</summary>
    private const string OpeningXgid = "XGID=-b----E-C---eE---c-e----B-:0:0:1:31:0:0:0:7:10";

    /// <summary>The race, the cube centred, the player on roll to decide on doubling.</summary>
    private const string RaceXgid = "XGID=-BBCCCB------------ddcbaa-:0:0:1:00:0:0:0:7:10";

    private static PlayCandidate[] OpeningCandidates() =>
    [
        Candidate(),
        Candidate(
            play: [new(13, 10), new(6, 5)], equity: -0.0127, equityLoss: 0.1731,
            winPct: 0.4987, winGammonPct: 0.1352, winBgPct: 0.0061,
            losePct: 0.5013, loseGammonPct: 0.1398, loseBgPct: 0.0071),
        Candidate(
            play: [new(24, 23), new(13, 10)], equity: -0.0209, equityLoss: 0.1813,
            winPct: 0.4969, winGammonPct: 0.1307, winBgPct: 0.0055,
            losePct: 0.5031, loseGammonPct: 0.1377, loseBgPct: 0.0069),
    ];

    // -----------------------------------------------------------------------
    //  The records
    // -----------------------------------------------------------------------

    /// <summary>
    /// A checker-play decision. By default the opening 3-1 of game 1 of a
    /// 7-point match; see the class remarks for how the defaults follow
    /// <paramref name="id"/> and <paramref name="position"/>.
    /// </summary>
    public static CheckerPlayDecision CheckerPlay(
        DecisionId? id = null,
        string xgid = OpeningXgid,
        PositionData? position = null,
        CheckerPlayDecisionData? decision = null,
        DescriptiveData? descriptive = null)
    {
        id ??= new XgDecisionId("match.xg", Game: 1, MoveNumber: 1, IsCube: false);
        position ??= Position();
        return new CheckerPlayDecision
        {
            Id = id,
            Xgid = xgid,
            Position = position,
            Decision = decision ?? (position.Mop == BoardPosition.Standard
                ? CheckerPlayData()
                : CheckerPlayData(plays: [Candidate(play: [])])),
            Descriptive = descriptive ?? DescriptiveFor(id),
        };
    }

    /// <summary>
    /// A cube decision. By default a double/take in a short race, the cube
    /// centred, in game 1 of a 7-point match; see the class remarks for how
    /// the defaults follow <paramref name="id"/>.
    /// </summary>
    public static CubeDecision Cube(
        DecisionId? id = null,
        string xgid = RaceXgid,
        PositionData? position = null,
        CubeDecisionData? decision = null,
        DescriptiveData? descriptive = null)
    {
        id ??= new XgDecisionId("match.xg", Game: 1, MoveNumber: 2, IsCube: true);
        return new CubeDecision
        {
            Id = id,
            Xgid = xgid,
            Position = position ?? Position(mop: RacePosition),
            Decision = decision ?? CubeData(),
            Descriptive = descriptive ?? DescriptiveFor(id),
        };
    }

    /// <summary>The row of <paramref name="record"/> (<see cref="DecisionRow.From"/>); by default, of <see cref="CheckerPlay"/>'s default.</summary>
    public static DecisionRow Row(BgDecisionData? record = null) =>
        DecisionRow.From(record ?? CheckerPlay());

    // -----------------------------------------------------------------------
    //  The categories
    // -----------------------------------------------------------------------

    /// <summary>
    /// A position category; each argument is the member of the same name. By
    /// default the standard start at 0-0 in a 7-point match, the cube centred
    /// on 1. The pip counts default to <paramref name="mop"/>'s own.
    /// </summary>
    public static PositionData Position(
        BoardPosition? mop = null,
        int onRollNeeds = 7,
        int opponentNeeds = 7,
        int? onRollPipCount = null,
        int? opponentPipCount = null,
        int cubeSize = 1,
        CubeOwner cubeOwner = CubeOwner.Centered,
        bool isCrawford = false,
        bool? isJacoby = null)
    {
        var board = mop ?? BoardPosition.Standard;
        var counted = new BoardState(board);
        return new PositionData
        {
            Mop = board,
            OnRollNeeds = onRollNeeds,
            OpponentNeeds = opponentNeeds,
            OnRollPipCount = onRollPipCount ?? counted.PipCount,
            OpponentPipCount = opponentPipCount ?? counted.OpponentPipCount,
            CubeSize = cubeSize,
            CubeOwner = cubeOwner,
            IsCrawford = isCrawford,
            IsJacoby = isJacoby,
        };
    }

    /// <summary>
    /// A checker play's decision category; each argument is the member of the
    /// same name. By default a 3-1 with the opening's three candidates, the
    /// first best and played at no cost.
    /// </summary>
    public static CheckerPlayDecisionData CheckerPlayData(
        IReadOnlyList<int>? dice = null,
        IReadOnlyList<PlayCandidate>? plays = null,
        int bestPlayIndex = 0,
        int? userPlayIndex = 0,
        double? userPlayError = 0.0) => new()
    {
        Dice = dice ?? [3, 1],
        Plays = plays ?? OpeningCandidates(),
        BestPlayIndex = bestPlayIndex,
        UserPlayIndex = userPlayIndex,
        UserPlayError = userPlayError,
    };

    /// <summary>
    /// A cube decision's category; each argument is the member of the same
    /// name. By default a 3-ply double/take (no double +0.512, double/take
    /// +0.634), played as double and take at no cost.
    /// </summary>
    public static CubeDecisionData CubeData(
        string depth = "3-ply",
        string depthAbbreviation = "3-ply",
        int depthRank = 3,
        AnalysisMode analysisMode = AnalysisMode.Evaluation,
        AnalysisLevel analysisLevel = AnalysisLevel.Ply3,
        double noDoubleEquity = 0.512,
        double doubleTakeEquity = 0.634,
        double cubelessNoDoubleEquity = 0.418,
        double cubelessDoubleTakeEquity = 0.418,
        double winPctAfterNoDouble = 0.709,
        double gammonPctAfterNoDouble = 0.012,
        double bgPctAfterNoDouble = 0.0,
        double losePctAfterNoDouble = 0.291,
        double loseGammonPctAfterNoDouble = 0.004,
        double loseBgPctAfterNoDouble = 0.0,
        double winPctAfterDoubleTake = 0.709,
        double gammonPctAfterDoubleTake = 0.012,
        double bgPctAfterDoubleTake = 0.0,
        double losePctAfterDoubleTake = 0.291,
        double loseGammonPctAfterDoubleTake = 0.004,
        double loseBgPctAfterDoubleTake = 0.0,
        double probOfOpponentErrorJustifyingDouble = 0.0,
        double? userDoubleError = 0.0,
        double? userTakeError = 0.0,
        CubeAction? userDoublerAction = CubeAction.Double,
        CubeAction? userTakerAction = CubeAction.Take) => new()
    {
        Depth = depth,
        DepthAbbreviation = depthAbbreviation,
        DepthRank = depthRank,
        AnalysisMode = analysisMode,
        AnalysisLevel = analysisLevel,
        NoDoubleEquity = noDoubleEquity,
        DoubleTakeEquity = doubleTakeEquity,
        CubelessNoDoubleEquity = cubelessNoDoubleEquity,
        CubelessDoubleTakeEquity = cubelessDoubleTakeEquity,
        WinPctAfterNoDouble = winPctAfterNoDouble,
        GammonPctAfterNoDouble = gammonPctAfterNoDouble,
        BgPctAfterNoDouble = bgPctAfterNoDouble,
        LosePctAfterNoDouble = losePctAfterNoDouble,
        LoseGammonPctAfterNoDouble = loseGammonPctAfterNoDouble,
        LoseBgPctAfterNoDouble = loseBgPctAfterNoDouble,
        WinPctAfterDoubleTake = winPctAfterDoubleTake,
        GammonPctAfterDoubleTake = gammonPctAfterDoubleTake,
        BgPctAfterDoubleTake = bgPctAfterDoubleTake,
        LosePctAfterDoubleTake = losePctAfterDoubleTake,
        LoseGammonPctAfterDoubleTake = loseGammonPctAfterDoubleTake,
        LoseBgPctAfterDoubleTake = loseBgPctAfterDoubleTake,
        ProbOfOpponentErrorJustifyingDouble = probOfOpponentErrorJustifyingDouble,
        UserDoubleError = userDoubleError,
        UserTakeError = userTakeError,
        UserDoublerAction = userDoublerAction,
        UserTakerAction = userTakerAction,
    };

    /// <summary>
    /// A descriptive category; each argument is the member of the same name.
    /// By default Alice against Bob in a 7-point match from <c>match.xg</c>,
    /// whose game started from the standard position.
    /// </summary>
    public static DescriptiveData Descriptive(
        int matchLength = 7,
        string onRollName = "Alice",
        string opponentName = "Bob",
        string? title = null,
        DateOnly? date = null,
        string? @event = null,
        string? sourceFile = "match.xg",
        bool? isStandardStart = true,
        string comment = "",
        bool flagged = false) => new()
    {
        MatchLength = matchLength,
        OnRollName = onRollName,
        OpponentName = opponentName,
        Title = title,
        Date = date,
        Event = @event,
        SourceFile = sourceFile,
        IsStandardStart = isStandardStart,
        Comment = comment,
        Flagged = flagged,
    };

    /// <summary>
    /// A candidate; each argument is the member of the same name. By default
    /// the opening 3-1's best play, 8/5 6/5, at 3-ply. Its notation is not a
    /// member (halheinrich/backgammon#273): it is derived from
    /// <paramref name="play"/>, so a test that cares how a candidate reads
    /// passes the play that reads that way.
    /// </summary>
    public static PlayCandidate Candidate(
        Play? play = null,
        string depth = "3-ply",
        string depthAbbreviation = "3-ply",
        int depthRank = 3,
        AnalysisMode analysisMode = AnalysisMode.Evaluation,
        AnalysisLevel analysisLevel = AnalysisLevel.Ply3,
        double equity = 0.1604,
        double equityLoss = 0.0,
        double? winPct = 0.5358,
        double? winGammonPct = 0.1598,
        double? winBgPct = 0.0088,
        double? losePct = 0.4642,
        double? loseGammonPct = 0.1253,
        double? loseBgPct = 0.0055) => new()
    {
        Play = play ?? [new(8, 5), new(6, 5)],
        Depth = depth,
        DepthAbbreviation = depthAbbreviation,
        DepthRank = depthRank,
        AnalysisMode = analysisMode,
        AnalysisLevel = analysisLevel,
        Equity = equity,
        EquityLoss = equityLoss,
        WinPct = winPct,
        WinGammonPct = winGammonPct,
        WinBgPct = winBgPct,
        LosePct = losePct,
        LoseGammonPct = loseGammonPct,
        LoseBgPct = loseBgPct,
    };

    /// <summary>The default descriptive category for a record identified by <paramref name="id"/>.</summary>
    private static DescriptiveData DescriptiveFor(DecisionId id) =>
        id is XgpDecisionId xgp
            ? Descriptive(sourceFile: xgp.Filename, isStandardStart: null)
            : Descriptive(sourceFile: id.Filename);
}
