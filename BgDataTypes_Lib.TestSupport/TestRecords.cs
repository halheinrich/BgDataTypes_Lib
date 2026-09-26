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
            play: [new(13, 10), new(6, 5)], equity: -0.0127,
            winPct: 0.4987, winGammonPct: 0.1352, winBgPct: 0.0061,
            loseGammonPct: 0.1398, loseBgPct: 0.0071),
        Candidate(
            play: [new(24, 23), new(13, 10)], equity: -0.0209,
            winPct: 0.4969, winGammonPct: 0.1307, winBgPct: 0.0055,
            loseGammonPct: 0.1377, loseBgPct: 0.0069),
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

    /// <summary>
    /// The row of <paramref name="record"/> built for <paramref name="ranking"/>
    /// (<see cref="DecisionRow.From"/>); by default, of <see cref="CheckerPlay"/>'s
    /// default, under <see cref="PlayRanking.Equity"/>, the ranking an app
    /// without the setting uses.
    /// </summary>
    public static DecisionRow Row(BgDecisionData? record = null, PlayRanking ranking = PlayRanking.Equity) =>
        DecisionRow.From(record ?? CheckerPlay(), ranking);

    // -----------------------------------------------------------------------
    //  The categories
    // -----------------------------------------------------------------------

    /// <summary>
    /// A position category; each argument is the member of the same name. By
    /// default the standard start at 0-0 in a 7-point match, the cube centred
    /// on 1. The pip counts are not arguments: the category derives them from
    /// <paramref name="mop"/>.
    /// </summary>
    public static PositionData Position(
        BoardPosition? mop = null,
        int onRollNeeds = 7,
        int opponentNeeds = 7,
        int cubeSize = 1,
        CubeOwner cubeOwner = CubeOwner.Centered,
        bool isCrawford = false,
        bool? isJacoby = null) => new()
    {
        Mop = mop ?? BoardPosition.Standard,
        OnRollNeeds = onRollNeeds,
        OpponentNeeds = opponentNeeds,
        CubeSize = cubeSize,
        CubeOwner = cubeOwner,
        IsCrawford = isCrawford,
        IsJacoby = isJacoby,
    };

    /// <summary>
    /// A checker play's decision category; each argument is the member of the
    /// same name. By default a 3-1 with the opening's three candidates, the
    /// first the best (it has the highest equity) and the user's play. The
    /// best play and the user's error are not arguments: the category derives
    /// them from the candidates, and a test wanting a user error either plays
    /// a candidate with that loss or states an unlisted play's error with no
    /// <paramref name="userPlayIndex"/>.
    /// </summary>
    public static CheckerPlayDecisionData CheckerPlayData(
        IReadOnlyList<int>? dice = null,
        IReadOnlyList<PlayCandidate>? plays = null,
        int? userPlayIndex = 0,
        double? unlistedPlayError = null) => new()
    {
        Dice = dice ?? [3, 1],
        Plays = plays ?? OpeningCandidates(),
        UserPlayIndex = userPlayIndex,
        UnlistedPlayError = unlistedPlayError,
    };

    /// <summary>
    /// A cube decision's category; each argument is the member of the same
    /// name. By default a 3-ply double/take (no double +0.512, double/take
    /// +0.634), played as double and take — at no cost, which the category
    /// derives from the equities, as it does the depth's label, abbreviation
    /// and rank from its typed facts.
    /// </summary>
    public static CubeDecisionData CubeData(
        AnalysisMode analysisMode = AnalysisMode.Evaluation,
        AnalysisLevel analysisLevel = AnalysisLevel.Ply3,
        int? rolloutTrials = null,
        BookEdition? bookEdition = null,
        int? unrecognizedLevelCode = null,
        double noDoubleEquity = 0.512,
        double doubleTakeEquity = 0.634,
        double cubelessNoDoubleEquity = 0.418,
        double cubelessDoubleTakeEquity = 0.418,
        double winPctAfterNoDouble = 0.709,
        double gammonPctAfterNoDouble = 0.012,
        double bgPctAfterNoDouble = 0.0,
        double loseGammonPctAfterNoDouble = 0.004,
        double loseBgPctAfterNoDouble = 0.0,
        double winPctAfterDoubleTake = 0.709,
        double gammonPctAfterDoubleTake = 0.012,
        double bgPctAfterDoubleTake = 0.0,
        double loseGammonPctAfterDoubleTake = 0.004,
        double loseBgPctAfterDoubleTake = 0.0,
        double probOfOpponentErrorJustifyingDouble = 0.0,
        CubeAction? userDoublerAction = CubeAction.Double,
        CubeAction? userTakerAction = CubeAction.Take,
        double? unstatedDoublerActionError = null,
        double? unstatedTakerActionError = null) => new()
    {
        AnalysisMode = analysisMode,
        AnalysisLevel = analysisLevel,
        RolloutTrials = rolloutTrials,
        BookEdition = bookEdition,
        UnrecognizedLevelCode = unrecognizedLevelCode,
        NoDoubleEquity = noDoubleEquity,
        DoubleTakeEquity = doubleTakeEquity,
        CubelessNoDoubleEquity = cubelessNoDoubleEquity,
        CubelessDoubleTakeEquity = cubelessDoubleTakeEquity,
        WinPctAfterNoDouble = winPctAfterNoDouble,
        GammonPctAfterNoDouble = gammonPctAfterNoDouble,
        BgPctAfterNoDouble = bgPctAfterNoDouble,
        LoseGammonPctAfterNoDouble = loseGammonPctAfterNoDouble,
        LoseBgPctAfterNoDouble = loseBgPctAfterNoDouble,
        WinPctAfterDoubleTake = winPctAfterDoubleTake,
        GammonPctAfterDoubleTake = gammonPctAfterDoubleTake,
        BgPctAfterDoubleTake = bgPctAfterDoubleTake,
        LoseGammonPctAfterDoubleTake = loseGammonPctAfterDoubleTake,
        LoseBgPctAfterDoubleTake = loseBgPctAfterDoubleTake,
        ProbOfOpponentErrorJustifyingDouble = probOfOpponentErrorJustifyingDouble,
        UserDoublerAction = userDoublerAction,
        UserTakerAction = userTakerAction,
        UnstatedDoublerActionError = unstatedDoublerActionError,
        UnstatedTakerActionError = unstatedTakerActionError,
    };

    /// <summary>
    /// A descriptive category; each argument is the member of the same name.
    /// By default Alice against Bob in a 7-point match, whose game started
    /// from the standard position. The source file is not here: a record
    /// derives it from its id.
    /// </summary>
    public static DescriptiveData Descriptive(
        int matchLength = 7,
        string? onRollName = "Alice",
        string? opponentName = "Bob",
        string? title = null,
        DateOnly? date = null,
        string? @event = null,
        bool? isStandardStart = true,
        string? comment = null,
        bool flagged = false) => new()
    {
        MatchLength = matchLength,
        OnRollName = onRollName,
        OpponentName = opponentName,
        Title = title,
        Date = date,
        Event = @event,
        IsStandardStart = isStandardStart,
        Comment = comment,
        Flagged = flagged,
    };

    /// <summary>
    /// A candidate; each argument is the member of the same name. By default
    /// the opening 3-1's best play, 8/5 6/5, at 3-ply. Its notation, depth
    /// label, abbreviation and rank, loss probability and equity loss are not
    /// members (halheinrich/backgammon#273): the notation is derived from
    /// <paramref name="play"/>, the depth text and rank from the typed depth
    /// facts, the loss probability from <paramref name="winPct"/>, and the
    /// loss on the decision from every candidate's equity, so a test states
    /// the facts those come from.
    /// </summary>
    public static PlayCandidate Candidate(
        Play? play = null,
        AnalysisMode analysisMode = AnalysisMode.Evaluation,
        AnalysisLevel analysisLevel = AnalysisLevel.Ply3,
        int? rolloutTrials = null,
        BookEdition? bookEdition = null,
        int? unrecognizedLevelCode = null,
        double equity = 0.1604,
        double? winPct = 0.5358,
        double? winGammonPct = 0.1598,
        double? winBgPct = 0.0088,
        double? loseGammonPct = 0.1253,
        double? loseBgPct = 0.0055) => new()
    {
        Play = play ?? [new(8, 5), new(6, 5)],
        AnalysisMode = analysisMode,
        AnalysisLevel = analysisLevel,
        RolloutTrials = rolloutTrials,
        BookEdition = bookEdition,
        UnrecognizedLevelCode = unrecognizedLevelCode,
        Equity = equity,
        WinPct = winPct,
        WinGammonPct = winGammonPct,
        WinBgPct = winBgPct,
        LoseGammonPct = loseGammonPct,
        LoseBgPct = loseBgPct,
    };

    /// <summary>The default descriptive category for a record identified by <paramref name="id"/>.</summary>
    private static DescriptiveData DescriptiveFor(DecisionId id) =>
        id is XgpDecisionId
            ? Descriptive(isStandardStart: null)
            : Descriptive();
}
