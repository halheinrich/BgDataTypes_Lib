namespace BgDataTypes_Lib;

public class BgDecisionData
{
    public PositionData Position { get; init; } = new();
    public DecisionData Decision { get; init; } = new();
    public DescriptiveData Descriptive { get; init; } = new();
}