namespace BgDataTypes_Lib;

public class DecisionData
{
    /// <summary>Always length 2. Ignored when IsCube is true.</summary>
    public IReadOnlyList<int> Dice { get; init; } = new int[2];

    public IReadOnlyList<PlayCandidate> Plays { get; init; } = [];
    public IReadOnlyList<AnalysisDepthEntry> AnalysisDepths { get; init; } = [];
    /// <summary>Index into Plays identifying the best play. </summary>
    public int BestPlayIndex { get; init; }
    /// <summary>
    /// Equity loss from the user's checker play vs. the best play (≥ 0).
    /// Null when no user play is recorded or IsCube is true.
    /// </summary>
    public double? UserPlayError { get; init; }
    /// <summary>Index into Plays identifying the user's play. -1 if not applicable.</summary>
    public int UserPlayIndex { get; init; } = -1;
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
    /// <summary>
    /// Equity loss from the user's doubling decision vs. the correct cube action (≥ 0).
    /// Null when no cube decision is recorded or IsCube is false.
    /// </summary>
    public double? UserDoubleError { get; init; }

    /// <summary>
    /// Equity loss from the user's take/drop decision vs. the correct response (≥ 0).
    /// Null when no cube decision is recorded or IsCube is false.
    /// </summary>
    public double? UserTakeError { get; init; }
}