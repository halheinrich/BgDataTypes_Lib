namespace BgDataTypes_Lib;

/// <summary>
/// A decision record as the filter layer reads it, for one
/// <see cref="PlayRanking"/> — what <see cref="BgDecisionData.ViewFor"/>
/// returns. The members that say best or error are the ranking's, and a cube
/// decision's taker members are its own, derived once here; every other
/// member forwards to the record.
/// </summary>
internal sealed class DecisionView : IDecisionFilterData
{
    private readonly BgDecisionData _record;

    internal DecisionView(BgDecisionData record, PlayRanking ranking)
    {
        _record = record;
        Ranking = ranking;
        (AnalysisMode, AnalysisLevel, PlayerResult, Taker, TakerResult, Dice, AfterBestBoard, AfterPlayerBoard) = record.Match(
            play =>
            {
                var ranked = play.Decision.RankedBy(ranking);
                var best = ranked.Best;
                return (best.Candidate.AnalysisMode, best.Candidate.AnalysisLevel, ranked.PlayerResult,
                    (string?)null, (PlayerResult?)null,
                    (DiceRoll?)play.Dice, (BoardPosition?)play.AfterBoardOf(best.Index), play.AfterPlayerBoard);
            },
            static cube =>
            {
                var taker = TakerResultOf(cube.Decision);
                return (cube.Decision.AnalysisMode, cube.Decision.AnalysisLevel, DoublerResultOf(cube.Decision),
                    taker is null ? null : cube.Descriptive.OpponentName, taker,
                    (DiceRoll?)null, (BoardPosition?)null, (BoardPosition?)null);
            });
    }

    /// <summary>
    /// A cube decision's player result: the doubler's half alone, each case
    /// named. A stated action is scored with its error; an unstated one with
    /// an analyser's error is <see cref="PlayerResultKind.Unstated"/>,
    /// carrying that error; otherwise not recorded. Never the taker's: a
    /// record holds no taker half without a stated double
    /// (<see cref="RecordedCubeActions"/>), so there is no doubler-less
    /// taker's result to fall back to.
    /// </summary>
    private static PlayerResult DoublerResultOf(CubeDecisionData cube) =>
        cube.UserDoublerAction is CubeAction doubled ? PlayerResult.Scored(cube.DoublerActionError(doubled))
        : cube.UnstatedDoublerActionError is double unstated ? PlayerResult.Unstated(unstated)
        : PlayerResult.NotRecorded;

    /// <summary>
    /// A cube decision's taker result (<see cref="IDecisionFilterData.TakerResult"/>):
    /// <see langword="null"/> unless a double was offered; then the stated
    /// response scored with its error, the analyser's error for an unstated
    /// one, or not recorded where the record holds neither.
    /// </summary>
    private static PlayerResult? TakerResultOf(CubeDecisionData cube) =>
        cube.UserDoublerAction is not CubeAction.Double ? null
        : cube.UserTakerAction is CubeAction taken ? PlayerResult.Scored(cube.TakerActionError(taken))
        : cube.UnstatedTakerActionError is double unstated ? PlayerResult.Unstated(unstated)
        : PlayerResult.NotRecorded;

    public PlayRanking Ranking { get; }
    public DecisionKind Kind => _record.Kind;
    public string? Player => _record.Player;
    public string? Taker { get; }
    public Session Session => _record.Session;
    public int? MoveNumber => _record.MoveNumber;
    public bool? IsStandardStart => _record.IsStandardStart;
    public BoardPosition Board => _record.Board;
    public AnalysisMode AnalysisMode { get; }
    public AnalysisLevel AnalysisLevel { get; }
    public PlayerResult PlayerResult { get; }
    public PlayerResult? TakerResult { get; }
    public DiceRoll? Dice { get; }
    public BoardPosition? AfterBestBoard { get; }
    public BoardPosition? AfterPlayerBoard { get; }
}
