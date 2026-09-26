namespace BgDataTypes_Lib;

/// <summary>
/// A decision record as the filter layer reads it, for one
/// <see cref="PlayRanking"/> — what <see cref="BgDecisionData.ViewFor"/>
/// returns. The members that say best or error are the ranking's, derived
/// once here; every other member forwards to the record.
/// </summary>
internal sealed class DecisionView : IDecisionFilterData
{
    private readonly BgDecisionData _record;

    internal DecisionView(BgDecisionData record, PlayRanking ranking)
    {
        _record = record;
        Ranking = ranking;
        (AnalysisMode, AnalysisLevel, PlayerResult, Dice, AfterBestBoard, AfterPlayerBoard) = record.Match(
            play =>
            {
                var ranked = play.Decision.RankedBy(ranking);
                var best = ranked.Best;
                return (best.Candidate.AnalysisMode, best.Candidate.AnalysisLevel, ranked.PlayerResult,
                    (DiceRoll?)play.Dice, (BoardPosition?)play.AfterBoardOf(best.Index), play.AfterPlayerBoard);
            },
            static cube => (cube.Decision.AnalysisMode, cube.Decision.AnalysisLevel, CubeResult(cube.Decision),
                (DiceRoll?)null, (BoardPosition?)null, (BoardPosition?)null));
    }

    /// <summary>
    /// A cube decision's player result: the doubler's, or when the record
    /// holds none, the taker's — as <see cref="CubeDecisionData.UserDoubleError"/>
    /// <c>??</c> <see cref="CubeDecisionData.UserTakeError"/> reads, with each
    /// case named. A stated action is scored with its error; an unstated one
    /// with an analyser's error is unlisted.
    /// </summary>
    private static PlayerResult CubeResult(CubeDecisionData cube) =>
        cube.UserDoublerAction is CubeAction doubled ? PlayerResult.Scored(cube.DoublerActionError(doubled))
        : cube.UnstatedDoublerActionError is double unstatedDouble ? PlayerResult.Unlisted(unstatedDouble)
        : cube.UserTakerAction is CubeAction taken ? PlayerResult.Scored(cube.TakerActionError(taken))
        : cube.UnstatedTakerActionError is double unstatedTake ? PlayerResult.Unlisted(unstatedTake)
        : PlayerResult.NotRecorded;

    public PlayRanking Ranking { get; }
    public DecisionKind Kind => _record.Kind;
    public string? Player => _record.Player;
    public int OnRollNeeds => _record.OnRollNeeds;
    public int OpponentNeeds => _record.OpponentNeeds;
    public bool IsCrawford => _record.IsCrawford;
    public int MatchLength => _record.MatchLength;
    public bool? IsJacoby => _record.IsJacoby;
    public int? MoveNumber => _record.MoveNumber;
    public bool? IsStandardStart => _record.IsStandardStart;
    public BoardPosition Board => _record.Board;
    public AnalysisMode AnalysisMode { get; }
    public AnalysisLevel AnalysisLevel { get; }
    public PlayerResult PlayerResult { get; }
    public DiceRoll? Dice { get; }
    public BoardPosition? AfterBestBoard { get; }
    public BoardPosition? AfterPlayerBoard { get; }
}
