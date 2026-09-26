namespace BgDataTypes_Lib;

/// <summary>
/// One candidate of a checker-play decision as a ranking places it: its rank,
/// its error against the ranking's best play, and whether the ranking scores
/// it (<see cref="PlayRanking"/>). An element of <see cref="RankedPlays"/>,
/// which only this library builds.
/// </summary>
public sealed class RankedPlay
{
    internal RankedPlay(int index, int rank, PlayCandidate candidate, double error, bool isScored)
    {
        Index = index;
        Rank = rank;
        Candidate = candidate;
        Error = error;
        IsScored = isScored;
    }

    /// <summary>The candidate's index into <see cref="CheckerPlayDecisionData.Plays"/>, the stored order.</summary>
    public int Index { get; }

    /// <summary>The candidate's place in the ranking, 1 for the best play — the rank number a solution list shows.</summary>
    public int Rank { get; }

    /// <summary>The candidate itself.</summary>
    public PlayCandidate Candidate { get; }

    /// <summary>
    /// The best play's equity minus this candidate's: 0 for the best play
    /// and for any that ties it, so a play is correct when its error is
    /// exactly 0. Never negative for a scored candidate; negative only for
    /// one the ranking does not score (<see cref="IsScored"/>).
    /// </summary>
    public double Error { get; }

    /// <summary>
    /// Whether the ranking scores this candidate. Under
    /// <see cref="PlayRanking.DepthFirst"/>, a candidate at a different depth
    /// from the best play's whose equity is higher than the best's is not
    /// scored — a consumer treats it as an off-list play; every other
    /// candidate, and every candidate under <see cref="PlayRanking.Equity"/>,
    /// is.
    /// </summary>
    public bool IsScored { get; }
}
