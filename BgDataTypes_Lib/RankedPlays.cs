using System.Collections;

namespace BgDataTypes_Lib;

/// <summary>
/// A checker-play decision's candidates under one <see cref="PlayRanking"/>:
/// in the ranking's order, each with its rank, whether it is scored and if
/// so its error, and the player's error under the ranking — every derivation
/// SPEC-scoring §2a makes a ranking's. Built by
/// <see cref="CheckerPlayDecisionData.RankedBy"/>, once per decision and
/// ranking; immutable.
/// </summary>
/// <remarks>
/// Enumerating it, or indexing it, walks the ranking's order: position 0 is
/// <see cref="Best"/>. <see cref="ForCandidate"/> finds a candidate by its
/// stored index instead.
/// </remarks>
public sealed class RankedPlays : IReadOnlyList<RankedPlay>
{
    private readonly RankedPlay[] _inOrder;
    private readonly RankedPlay[] _byIndex;

    internal RankedPlays(
        IReadOnlyList<PlayCandidate> plays, PlayRanking ranking, int? userPlayIndex, double? unlistedPlayError)
    {
        Ranking = ranking;

        int[] order = new int[plays.Count];
        for (int i = 0; i < order.Length; i++)
            order[i] = i;
        Array.Sort(order, (a, b) => Compare(plays, ranking, a, b));

        var best = plays[order[0]];
        _inOrder = new RankedPlay[order.Length];
        _byIndex = new RankedPlay[order.Length];
        for (int position = 0; position < order.Length; position++)
        {
            int index = order[position];
            var candidate = plays[index];
            double? error = IsScored(ranking, best, candidate) ? best.Equity - candidate.Equity : null;
            var ranked = new RankedPlay(index, position + 1, candidate, error);
            _inOrder[position] = ranked;
            _byIndex[index] = ranked;
        }

        UserPlay = userPlayIndex is int user ? _byIndex[user] : null;
        UserPlayError = UserPlay is { } played ? played.Error : unlistedPlayError;
    }

    /// <summary>The ranking these are ranked by.</summary>
    public PlayRanking Ranking { get; }

    /// <summary>The best play under the ranking: its first.</summary>
    public RankedPlay Best => _inOrder[0];

    /// <summary>
    /// The user's play under the ranking, or <see langword="null"/> when it is
    /// not among the candidates (<see cref="CheckerPlayDecisionData.UserPlayIndex"/>).
    /// </summary>
    public RankedPlay? UserPlay { get; }

    /// <summary>
    /// The player's error under the ranking: the <see cref="RankedPlay.Error"/>
    /// of the user's play when the ranking scores it; none
    /// (<see langword="null"/>) when it does not — a play the ranking does not
    /// score has no error; and for a play outside the candidates the
    /// analyser's <see cref="CheckerPlayDecisionData.UnlistedPlayError"/>,
    /// which no ranking changes. <see langword="null"/> too when no user play
    /// is recorded.
    /// </summary>
    public double? UserPlayError { get; }

    /// <summary>The number of candidates.</summary>
    public int Count => _inOrder.Length;

    /// <summary>The candidate at <paramref name="position"/> in the ranking's order, 0 for the best.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is outside the candidates.</exception>
    public RankedPlay this[int position]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(position);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(position, _inOrder.Length);
            return _inOrder[position];
        }
    }

    /// <summary>The candidate whose index into <see cref="CheckerPlayDecisionData.Plays"/> is <paramref name="index"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> identifies no candidate.</exception>
    public RankedPlay ForCandidate(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _byIndex.Length);
        return _byIndex[index];
    }

    /// <summary>Walks the candidates in the ranking's order.</summary>
    public IEnumerator<RankedPlay> GetEnumerator() => ((IEnumerable<RankedPlay>)_inOrder).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// The ranking's order between the candidates at <paramref name="a"/> and
    /// <paramref name="b"/>: by the ranking's keys, highest first, and by the
    /// stored order where they tie, so the order is stable.
    /// </summary>
    private static int Compare(IReadOnlyList<PlayCandidate> plays, PlayRanking ranking, int a, int b)
    {
        var x = plays[a];
        var y = plays[b];
        int byDepth = ranking == PlayRanking.DepthFirst
            ? DepthKey(y).CompareTo(DepthKey(x))
            : 0;
        if (byDepth != 0)
            return byDepth;
        int byEquity = y.Equity.CompareTo(x.Equity);
        return byEquity != 0 ? byEquity : a.CompareTo(b);
    }

    /// <summary>A candidate's depth as a sort key: an unrecorded depth below every recorded one.</summary>
    private static int DepthKey(PlayCandidate candidate) => candidate.DepthRank ?? int.MinValue;

    /// <summary>
    /// The not-scored rule: under depth first, a candidate whose equity is
    /// higher than the best play's is not scored; every other candidate is.
    /// SPEC-scoring §2a states the rule for a play analysed at a different
    /// depth from the best play that shows a higher equity. Under depth first
    /// the best play has the highest equity at its own depth, so a higher
    /// equity is always at another depth: the depth condition is implied, and
    /// the rule is the equity comparison alone. That every unscored candidate
    /// is at a different depth from the best is pinned as a property.
    /// </summary>
    private static bool IsScored(PlayRanking ranking, PlayCandidate best, PlayCandidate candidate) =>
        ranking != PlayRanking.DepthFirst || candidate.Equity <= best.Equity;
}
