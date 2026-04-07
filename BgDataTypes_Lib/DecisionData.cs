namespace BgDataTypes_Lib;

public class DecisionData
{
    /// <summary>Always length 2. Ignored when IsCube is true.</summary>
    public IReadOnlyList<int> Dice { get; init; } = new int[2];

    public IReadOnlyList<PlayCandidate> Plays { get; init; } = [];
    public IReadOnlyList<AnalysisDepthEntry> AnalysisDepths { get; init; } = [];
    public bool IsCube { get; init; }

    // -----------------------------------------------------------------------
    //  Cube decision equity fields
    // -----------------------------------------------------------------------
    public double NoDoubleEquity { get; init; }
    public double DoubleTakeEquity { get; init; }

    public double WinPctAfterNoDouble { get; init; }
    public double GammonPctAfterNoDouble { get; init; }
    public double BgPctAfterNoDouble { get; init; }
    public double LosePctAfterNoDouble { get; init; }
    public double LoseGammonPctAfterNoDouble { get; init; }
    public double LoseBgPctAfterNoDouble { get; init; }

    public double WinPctAfterDoubleTake { get; init; }
    public double GammonPctAfterDoubleTake { get; init; }
    public double BgPctAfterDoubleTake { get; init; }
    public double LosePctAfterDoubleTake { get; init; }
    public double LoseGammonPctAfterDoubleTake { get; init; }
    public double LoseBgPctAfterDoubleTake { get; init; }

    public double ProbOfOpponentErrorJustifyingDouble { get; init; }
}