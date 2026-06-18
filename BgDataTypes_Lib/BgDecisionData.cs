namespace BgDataTypes_Lib;

public class BgDecisionData : IDecisionFilterData
{
    /// <summary>
    /// Stable, persistent identifier for this decision within its source file.
    /// Producer-supplied at the build site (see <c>ConvertXgToJson_Lib</c>) —
    /// required so that uninitialized cases surface at construction rather than
    /// later as silent null reads. Not part of <see cref="IDecisionFilterData"/>
    /// (the filter passes records through unchanged and never needs to see the
    /// ID).
    /// </summary>
    public required DecisionId Id { get; init; }

    /// <summary>
    /// XGID position string. Lives at the top level rather than inside
    /// <see cref="Position"/> because it is a digest of the whole decision
    /// context (position, cube/match state, and the decision itself), not a
    /// property of the minimal derived <see cref="PositionData"/>. Mirrors
    /// <see cref="DecisionRow.Xgid"/>.
    /// </summary>
    public string Xgid { get; init; } = string.Empty;

    public PositionData    Position    { get; init; } = new();
    public DecisionData    Decision    { get; init; } = new();
    public DescriptiveData Descriptive { get; init; } = new();
    public PlayOutcomeData Outcome     { get; init; } = new();

    // -----------------------------------------------------------------------
    //  IDecisionFilterData
    // -----------------------------------------------------------------------

    public string Player => Descriptive.OnRollName;
    public bool IsCube => Decision.IsCube;
    public int OnRollNeeds => Position.OnRollNeeds;
    public int OpponentNeeds => Position.OpponentNeeds;
    public bool IsCrawford => Position.IsCrawford;
    public int MatchLength => Descriptive.MatchLength;
    public int MoveNumber => Descriptive.MoveNumber;
    public bool IsStandardStart => Descriptive.IsStandardStart;
    public double? FilterError => Decision.IsCube
        ? Decision.UserDoubleError ?? Decision.UserTakeError
        : Decision.UserPlayError;
    public IReadOnlyList<int> Board => Position.Mop;
    public IReadOnlyList<int> AfterBestBoard => Outcome.AfterBestBoard;
    public IReadOnlyList<int> AfterPlayerBoard => Outcome.AfterPlayerBoard;
}