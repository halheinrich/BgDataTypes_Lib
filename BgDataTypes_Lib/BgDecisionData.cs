namespace BgDataTypes_Lib;

public class BgDecisionData : IDecisionFilterData
{
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
    public double? FilterError => Decision.IsCube
        ? Decision.UserDoubleError ?? Decision.UserTakeError
        : Decision.UserPlayError;
    public IReadOnlyList<int> Board => Position.Mop;
    public IReadOnlyList<int> AfterBestBoard => Outcome.AfterBestBoard;
    public IReadOnlyList<int> AfterPlayerBoard => Outcome.AfterPlayerBoard;
}