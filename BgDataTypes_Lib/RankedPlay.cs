namespace BgDataTypes_Lib;

/// <summary>
/// One candidate of a checker-play decision as a ranking places it: its rank,
/// whether the ranking scores it, and if so its error against the ranking's
/// best play (<see cref="PlayRanking"/>). An element of
/// <see cref="RankedPlays"/>, which only this library builds.
/// </summary>
public sealed class RankedPlay
{
    internal RankedPlay(int index, int rank, PlayCandidate candidate, double? error)
    {
        Index = index;
        Rank = rank;
        Candidate = candidate;
        Error = error;
    }

    /// <summary>The candidate's index into <see cref="CheckerPlayDecisionData.Plays"/>, the stored order.</summary>
    public int Index { get; }

    /// <summary>The candidate's place in the ranking, 1 for the best play — the rank number a solution list shows.</summary>
    public int Rank { get; }

    /// <summary>The candidate itself.</summary>
    public PlayCandidate Candidate { get; }

    /// <summary>
    /// The candidate's error under the ranking — the best play's equity
    /// minus its own — or <see langword="null"/> when the ranking does not
    /// score it: a play the ranking does not score has no error
    /// (SPEC-scoring §2a). A scored candidate's error is never
    /// <see langword="null"/> and never negative; it is 0 for the best play
    /// and any that ties it, so a play is correct when its error is exactly 0.
    /// </summary>
    public double? Error { get; }

    /// <summary>
    /// Whether the ranking scores this candidate: exactly when it has an
    /// <see cref="Error"/>, and exactly when its equity is not higher than
    /// the best play's. Only under <see cref="PlayRanking.DepthFirst"/> can a
    /// candidate's be higher — one the ranking does not score, which a
    /// consumer treats as an off-list play; under
    /// <see cref="PlayRanking.Equity"/> every candidate is scored.
    /// </summary>
    public bool IsScored => Error is not null;
}
