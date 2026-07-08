namespace BgDataTypes_Lib;

/// <summary>
/// The canonical chain form of a <see cref="Play"/> — the single source of play
/// equivalence. Produced by <see cref="Play.ToCanonical"/>; <see cref="Play"/>
/// equality and hashing delegate here.
///
/// <para>
/// Consecutive single-die hops of one checker collapse into a single
/// <see cref="PlayChain"/> recording only the source and final landing point:
/// {(13,10),(10,8)} and {(13,8)} both canonicalize to the one chain 13/8, so
/// differently-decomposed entries of the same play compare equal. Equality is
/// therefore notation-level (XG's candidate-list semantics), not
/// encoding-level: it is insensitive to move order and to how a trajectory is
/// split into hops, but fully sensitive to hits.
/// </para>
///
/// <para>
/// Hits are preserved, never merged away. The single hit-visibility rule
/// (<see cref="MayFuseAt"/>): two segments joining at a shared point P may
/// collapse only when the segment <i>ending</i> at P does not hit there —
/// otherwise the hit marking that now-intermediate point would be lost. A
/// trajectory with an intermediate hit is thus split at the hit into two
/// chains, so a hit only ever sits at a chain's endpoint: 13/10*/8 canonicalizes
/// to the two chains {13/10*, 10/8} and is distinct from 13/8, while
/// 13/10/8* collapses to the one chain 13/8* and equals any other encoding
/// of 13/8*.
/// </para>
///
/// <para>
/// Canonical order: chains are sorted by FrPt descending, then |ToPt|
/// descending, then hit-first. Equality and hash compare the sorted chain
/// sequence, so equal canonical forms are structurally identical and the form
/// is deterministic for any multiset of moves.
/// </para>
///
/// <para>
/// The full <see cref="Move"/> encoding domain is handled: bar entry
/// (FrPt 25), bear-off (ToPt 0 — a borne-off chain cannot extend further),
/// hits (negative ToPt), doubles (up to 4 moves), out-of-order legs, and the
/// empty play. <c>default(CanonicalPlay)</c> is the canonical form of the
/// empty play and is meaningful.
/// </para>
///
/// <para>
/// Display stays downstream: notation strings, "(2)" duplicate-grouping and
/// "bar"/"off" labels belong to BgMoveGen's <c>MoveNotationFormatter</c>,
/// which renders from this form. Duplicate chains (doubles moving two
/// checkers identically) appear here as repeated entries.
/// </para>
/// </summary>
public readonly struct CanonicalPlay : IEquatable<CanonicalPlay>
{
    // Fixed buffer: at most one chain per move, max 4 moves (doubles).
    private readonly PlayChain _c0, _c1, _c2, _c3;

    /// <summary>Number of chains (0-4; at most the source play's move count).</summary>
    public int Count { get; }

    /// <summary>The chain at <paramref name="index"/>, in canonical order.</summary>
    public PlayChain this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Count) throw new IndexOutOfRangeException();
            return index switch { 0 => _c0, 1 => _c1, 2 => _c2, _ => _c3 };
        }
    }

    private CanonicalPlay(ReadOnlySpan<PlayChain> chains)
    {
        Count = chains.Length;
        if (Count > 0) _c0 = chains[0];
        if (Count > 1) _c1 = chains[1];
        if (Count > 2) _c2 = chains[2];
        if (Count > 3) _c3 = chains[3];
    }

    /// <summary>
    /// Canonicalizes <paramref name="play"/>. Internal: <see cref="Play.ToCanonical"/>
    /// is the public gateway, so every instance is guaranteed canonical.
    /// </summary>
    internal static CanonicalPlay FromPlay(in Play play)
    {
        int n = play.Count;
        if (n == 0) return default;

        // Deterministic processing order — FrPt desc, |ToPt| desc, hit first.
        // Chain-building matches greedily, so the same multiset of moves must
        // always be walked in the same order or two encodings of one play
        // could canonicalize differently (e.g. {(13,11),(11,9),(11,8)}: the
        // leg reaching 11 first grabs whichever continuation it meets first).
        // FrPt-descending also visits each checker's legs in journey order,
        // since every legal move decreases the point number.
        Span<Move> moves = stackalloc Move[n];
        for (int i = 0; i < n; i++) moves[i] = play[i];
        for (int i = 1; i < n; i++)
            for (int j = i; j > 0 && Precedes(moves[j], moves[j - 1]); j--)
                (moves[j], moves[j - 1]) = (moves[j - 1], moves[j]);

        Span<Segment> chains = stackalloc Segment[n];
        int chainCount = 0;

        for (int i = 0; i < n; i++)
        {
            int from = moves[i].FrPt;
            bool hit = moves[i].ToPt < 0;
            int to = hit ? -moves[i].ToPt : moves[i].ToPt;

            int matchIdx = -1;
            bool isForward = false;

            if (from is >= 1 and <= 24)
            {
                // Forward: an existing chain ends where this leg starts. The
                // chain is the segment ending at the join, so its endpoint
                // hit gates the merge.
                for (int j = 0; j < chainCount; j++)
                {
                    if (chains[j].To == from && MayFuseAt(chains[j].Hit))
                    {
                        matchIdx = j;
                        isForward = true;
                        break;
                    }
                }
            }

            if (matchIdx < 0 && to is >= 1 and <= 24 && MayFuseAt(hit))
            {
                // Backward: this leg ends where an existing chain starts. The
                // leg is the segment ending at the join, so its own hit gates.
                // Unreachable for legal plays once moves are journey-ordered,
                // but kept so the whole encoding domain stays deterministic.
                for (int j = 0; j < chainCount; j++)
                {
                    if (chains[j].From == to)
                    {
                        matchIdx = j;
                        break;
                    }
                }
            }

            if (matchIdx >= 0)
            {
                var c = chains[matchIdx];
                chains[matchIdx] = isForward
                    ? new Segment(c.From, to, hit)
                    : new Segment(from, c.To, c.Hit);
            }
            else
            {
                chains[chainCount++] = new Segment(from, to, hit);
            }
        }

        // A leg only ever merges into one chain, so an extension can leave two
        // chains adjacent (one's endpoint equals the other's start). Fuse to a
        // fixpoint, honouring the same hit-visibility rule at each join. As
        // with backward matching, legal journey-ordered plays never get here.
        bool fused = true;
        while (fused)
        {
            fused = false;
            for (int a = 0; a < chainCount && !fused; a++)
            {
                int joinPt = chains[a].To;
                if (joinPt is < 1 or > 24 || !MayFuseAt(chains[a].Hit)) continue;

                for (int b = 0; b < chainCount; b++)
                {
                    if (b == a || chains[b].From != joinPt) continue;

                    // a is the segment ending at the join; b's endpoint hit
                    // becomes the merged chain's endpoint, still visible.
                    chains[a] = new Segment(chains[a].From, chains[b].To, chains[b].Hit);
                    chains[b] = chains[chainCount - 1];
                    chainCount--;
                    fused = true;
                    break;
                }
            }
        }

        // Canonical order: FrPt desc, |To| desc, hit first.
        for (int i = 1; i < chainCount; i++)
            for (int j = i; j > 0 && Precedes(chains[j], chains[j - 1]); j--)
                (chains[j], chains[j - 1]) = (chains[j - 1], chains[j]);

        Span<PlayChain> result = stackalloc PlayChain[chainCount];
        for (int i = 0; i < chainCount; i++)
            result[i] = new PlayChain(chains[i].From, chains[i].Hit ? -chains[i].To : chains[i].To);
        return new CanonicalPlay(result);
    }

    /// <summary>
    /// The single hit-visibility rule for joining two segments at a shared
    /// point P: the segment whose endpoint is P (the one being extended
    /// forward across P) must not hit at P, or the hit marking that
    /// now-intermediate point would be lost. Forward leg-matching, backward
    /// leg-matching, and chain-to-chain fusing all reduce to this predicate.
    /// </summary>
    private static bool MayFuseAt(bool leftSegmentHitsAtJoin) => !leftSegmentHitsAtJoin;

    private static bool Precedes(Move a, Move b)
    {
        if (a.FrPt != b.FrPt) return a.FrPt > b.FrPt;
        int aTo = Math.Abs(a.ToPt), bTo = Math.Abs(b.ToPt);
        if (aTo != bTo) return aTo > bTo;
        return a.ToPt < b.ToPt; // hit (negative) first
    }

    private static bool Precedes(Segment a, Segment b)
    {
        if (a.From != b.From) return a.From > b.From;
        if (a.To != b.To) return a.To > b.To;
        return a.Hit && !b.Hit; // hit first
    }

    // Working representation during canonicalization: destination magnitude
    // and hit flag carried separately so join points compare sign-free.
    private readonly record struct Segment(int From, int To, bool Hit);

    public bool Equals(CanonicalPlay other)
    {
        if (Count != other.Count) return false;
        for (int i = 0; i < Count; i++)
            if (this[i] != other[i]) return false;
        return true;
    }

    public override bool Equals(object? obj) => obj is CanonicalPlay c && Equals(c);

    public override int GetHashCode()
    {
        var hc = new HashCode();
        hc.Add(Count);
        for (int i = 0; i < Count; i++) hc.Add(this[i]);
        return hc.ToHashCode();
    }

    public static bool operator ==(CanonicalPlay left, CanonicalPlay right) => left.Equals(right);
    public static bool operator !=(CanonicalPlay left, CanonicalPlay right) => !left.Equals(right);
}
