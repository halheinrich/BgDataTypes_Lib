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
        (AnalysisMode, AnalysisLevel, FilterError, Dice, AfterBestBoard, AfterPlayerBoard) = record.Match(
            play =>
            {
                var ranked = play.Decision.RankedBy(ranking);
                var best = ranked.Best;
                return (best.Candidate.AnalysisMode, best.Candidate.AnalysisLevel, ranked.UserPlayError,
                    (DiceRoll?)play.Dice, (BoardPosition?)play.AfterBoardOf(best.Index), play.AfterPlayerBoard);
            },
            static cube => (cube.Decision.AnalysisMode, cube.Decision.AnalysisLevel,
                cube.Decision.UserDoubleError ?? cube.Decision.UserTakeError,
                (DiceRoll?)null, (BoardPosition?)null, (BoardPosition?)null));
    }

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
    public double? FilterError { get; }
    public DiceRoll? Dice { get; }
    public BoardPosition? AfterBestBoard { get; }
    public BoardPosition? AfterPlayerBoard { get; }
}
