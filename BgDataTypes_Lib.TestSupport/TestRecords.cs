namespace BgDataTypes_Lib.TestSupport;

/// <summary>
/// Full wire records for tests. Every stored member of the record types is
/// <c>required</c> or nullable (the wire rule stated on
/// <see cref="BgDataTypesJsonContext"/>, halheinrich/backgammon#222), so a
/// record states every member; these builders state them all and take the
/// members a test cares about as named arguments, spelled as the members are.
/// </summary>
/// <remarks>
/// Each default is the value the member defaulted to before the rule — empty
/// strings, zeros, <see cref="AnalysisMode.Unknown"/>, no user play (-1), a
/// cube of 1, the empty board, <see langword="null"/> for the nullable
/// members — so a test rewritten onto a builder keeps its meaning. A test
/// whose subject is construction itself (an init guard, or the order members
/// are set in) writes its own object initializer instead.
/// </remarks>
public static class TestRecords
{
    /// <summary>A position category; each argument is the member of the same name.</summary>
    public static PositionData Position(
        BoardPosition mop = default,
        int onRollNeeds = 0,
        int opponentNeeds = 0,
        int onRollPipCount = 0,
        int opponentPipCount = 0,
        int cubeSize = 1,
        CubeOwner cubeOwner = CubeOwner.OnRoll,
        bool isCrawford = false,
        bool? isJacoby = null) => new()
    {
        Mop = mop,
        OnRollNeeds = onRollNeeds,
        OpponentNeeds = opponentNeeds,
        OnRollPipCount = onRollPipCount,
        OpponentPipCount = opponentPipCount,
        CubeSize = cubeSize,
        CubeOwner = cubeOwner,
        IsCrawford = isCrawford,
        IsJacoby = isJacoby,
    };

    /// <summary>A decision category; each argument is the member of the same name.</summary>
    public static DecisionData Decision(
        IReadOnlyList<int>? dice = null,
        IReadOnlyList<PlayCandidate>? plays = null,
        int bestPlayIndex = 0,
        double? userPlayError = null,
        int userPlayIndex = -1,
        bool isCube = false,
        string cubeDepth = "",
        string cubeDepthAbbreviation = "",
        int cubeDepthRank = 0,
        AnalysisMode cubeAnalysisMode = AnalysisMode.Unknown,
        AnalysisLevel cubeAnalysisLevel = AnalysisLevel.Unknown,
        double noDoubleEquity = 0,
        double doubleTakeEquity = 0,
        double cubelessNoDoubleEquity = 0,
        double cubelessDoubleTakeEquity = 0,
        double winPctAfterNoDouble = 0,
        double gammonPctAfterNoDouble = 0,
        double bgPctAfterNoDouble = 0,
        double losePctAfterNoDouble = 0,
        double loseGammonPctAfterNoDouble = 0,
        double loseBgPctAfterNoDouble = 0,
        double winPctAfterDoubleTake = 0,
        double gammonPctAfterDoubleTake = 0,
        double bgPctAfterDoubleTake = 0,
        double losePctAfterDoubleTake = 0,
        double loseGammonPctAfterDoubleTake = 0,
        double loseBgPctAfterDoubleTake = 0,
        double probOfOpponentErrorJustifyingDouble = 0,
        double? userDoubleError = null,
        double? userTakeError = null,
        CubeAction? userDoublerAction = null,
        CubeAction? userTakerAction = null) => new()
    {
        Dice = dice ?? [0, 0],
        Plays = plays ?? [],
        BestPlayIndex = bestPlayIndex,
        UserPlayError = userPlayError,
        UserPlayIndex = userPlayIndex,
        IsCube = isCube,
        CubeDepth = cubeDepth,
        CubeDepthAbbreviation = cubeDepthAbbreviation,
        CubeDepthRank = cubeDepthRank,
        CubeAnalysisMode = cubeAnalysisMode,
        CubeAnalysisLevel = cubeAnalysisLevel,
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

    /// <summary>A descriptive category; each argument is the member of the same name.</summary>
    public static DescriptiveData Descriptive(
        int matchLength = 0,
        string onRollName = "",
        string opponentName = "",
        string? title = null,
        DateOnly? date = null,
        string? @event = null,
        string? sourceFile = null,
        bool isStandardStart = false,
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
    /// A candidate. Its notation is not a member (halheinrich/backgammon#273):
    /// it is derived from <paramref name="play"/>, so a test that cares how a
    /// candidate reads passes the play that reads that way.
    /// </summary>
    public static PlayCandidate Candidate(
        Play play = default,
        string depth = "",
        string depthAbbreviation = "",
        int depthRank = 0,
        AnalysisMode analysisMode = AnalysisMode.Unknown,
        AnalysisLevel analysisLevel = AnalysisLevel.Unknown,
        double equity = 0,
        double equityLoss = 0,
        double? winPct = null,
        double? winGammonPct = null,
        double? winBgPct = null,
        double? losePct = null,
        double? loseGammonPct = null,
        double? loseBgPct = null) => new()
    {
        Play = play,
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

    /// <summary>An outcome category; each argument is the member of the same name.</summary>
    public static PlayOutcomeData Outcome(
        BoardPosition? afterBestBoard = null,
        BoardPosition? afterPlayerBoard = null) => new()
    {
        AfterBestBoard = afterBestBoard,
        AfterPlayerBoard = afterPlayerBoard,
    };

    /// <summary>
    /// A composite record. The halves are set Position first, then Decision,
    /// so a Crawford cube throws naming <c>Decision</c>; a test of the
    /// Crawford guard's order writes its own initializer.
    /// </summary>
    public static BgDecisionData Record(
        DecisionId? id = null,
        string xgid = "",
        PositionData? position = null,
        DecisionData? decision = null,
        DescriptiveData? descriptive = null,
        PlayOutcomeData? outcome = null) => new()
    {
        Id = id ?? new XgpDecisionId("test.xgp"),
        Xgid = xgid,
        Position = position ?? Position(),
        Decision = decision ?? Decision(),
        Descriptive = descriptive ?? Descriptive(),
        Outcome = outcome ?? Outcome(),
    };

    /// <summary>
    /// A flat row. <paramref name="roll"/> has no default: the decision kind
    /// is stated, never defaulted (halheinrich/backgammon#201). Roll is set
    /// before IsCrawford, so a Crawford cube throws naming <c>IsCrawford</c>;
    /// a test of the guard's order writes its own initializer.
    /// </summary>
    public static DecisionRow Row(
        int roll,
        DecisionId? id = null,
        string xgid = "",
        double error = 0,
        int matchLength = 0,
        string player = "",
        string? sourceFile = null,
        bool isStandardStart = false,
        string analysisDepth = "",
        AnalysisMode analysisMode = AnalysisMode.Unknown,
        AnalysisLevel analysisLevel = AnalysisLevel.Unknown,
        double equity = 0,
        int onRollNeeds = 0,
        int opponentNeeds = 0,
        bool isCrawford = false,
        bool? isJacoby = null,
        BoardPosition board = default,
        BoardPosition? afterBestBoard = null,
        BoardPosition? afterPlayerBoard = null) => new()
    {
        Id = id ?? new XgpDecisionId("test.xgp"),
        Roll = roll,
        Xgid = xgid,
        Error = error,
        MatchLength = matchLength,
        Player = player,
        SourceFile = sourceFile,
        IsStandardStart = isStandardStart,
        AnalysisDepth = analysisDepth,
        AnalysisMode = analysisMode,
        AnalysisLevel = analysisLevel,
        Equity = equity,
        OnRollNeeds = onRollNeeds,
        OpponentNeeds = opponentNeeds,
        IsCrawford = isCrawford,
        IsJacoby = isJacoby,
        Board = board,
        AfterBestBoard = afterBestBoard,
        AfterPlayerBoard = afterPlayerBoard,
    };
}
