using System.Globalization;
using System.Text;

namespace BgDataTypes_Lib;

/// <summary>
/// The canonical chain form of a <see cref="Play"/> — the play's display form:
/// which chains its notation shows, and on which chain each hit mark (the
/// <c>*</c>) goes. Produced by <see cref="Play.ToCanonical"/>.
///
/// <para>
/// <b>Internal, with <see cref="PlayChain"/></b> (halheinrich/backgammon#273):
/// a consumer spells a play with <see cref="Play.ToNotation"/> and compares
/// plays with <see cref="BoardState.IsSamePlay"/>, and needs nothing of the
/// chains in between. So the form can change without breaking a consumer.
/// </para>
///
/// <para>
/// <b>This is the display rule, not identity.</b> Whether two plays are the
/// same play is <see cref="BoardState.IsSamePlay"/>, asked of the position
/// they are played from; two encodings of one play can display differently
/// (a multi-die move pairs sources with destinations as written). So
/// <see cref="CanonicalPlay"/>, like <see cref="Play"/>, has no equality:
/// <c>==</c> and <c>!=</c> are not defined, and <see cref="Equals(object)"/>
/// and <see cref="GetHashCode"/> throw (halheinrich/backgammon#273, ruling A).
/// Read a form through <see cref="Count"/> and the indexer.
/// </para>
///
/// <para>
/// Consecutive single-die hops of one checker collapse into a single
/// <see cref="PlayChain"/> recording only the source and final landing point:
/// {(13,10),(10,8)} and {(13,8)} both canonicalize to the one chain 13/8, so
/// differently-decomposed entries display alike, in any move order.
/// </para>
///
/// <para>
/// A hit belongs to its point. A point holds at most one opposing blot, so it
/// is hit at most once, and among the moves or chains landing on one point,
/// which one records the hit does not change the display.
/// Canonicalization lifts the marks off the moves into the play's set of hit
/// points, builds chains from the unmarked moves, and places each point's
/// mark on exactly one chain, its <i>carrier</i>: the first, in canonical
/// order, of the chains ending there. 8/3* 7/3 and 8/3 7/3* therefore both
/// canonicalize to {8/3*, 7/3}. Two marks on one point display as the one
/// hit.
/// </para>
///
/// <para>
/// Hits are never merged away. The single hit-visibility rule
/// (<see cref="MayJoinAt"/>): a join at a shared point P consumes the segment
/// ending there, so it is allowed unless P is hit and that segment is P's
/// carrier. The carrier thus keeps P as its endpoint, where the mark is
/// visible. A lone trajectory with an intermediate hit is split at the hit
/// into two chains: 13/10*/8 canonicalizes to {13/10*, 10/8}, while 13/10/8*
/// collapses to the one chain 13/8*. When several chains land on the hit
/// point, only the carrier stops there and the others may continue through
/// it: {(15,9),(12,9),(9,6)} with the hit on 9 canonicalizes to
/// {15/9*, 12/6}, whichever move landing on 9 recorded the hit.
/// </para>
///
/// <para>
/// Canonical order: chains are sorted by FrPt descending, then |ToPt|
/// descending. Because a point's mark goes to the first chain ending there,
/// a marked chain precedes its unmarked duplicates. The form is
/// deterministic for any multiset of moves, and depends only on the unmarked
/// moves and the set of hit points.
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
/// <b>The notation is rendered by <see cref="ToString"/></b>, the one
/// formatter, and reached publicly through <see cref="Play.ToNotation"/>. A
/// form's canonical text is its <see cref="ToString"/>, as for
/// <see cref="DiceRoll"/> and <see cref="ProblemKey"/>, and the rendering
/// lives here because it leans on this type's order: duplicate chains
/// (doubles moving two checkers identically) are repeated entries, kept
/// adjacent by the canonical order, and the notation groups them.
/// </para>
/// </summary>
internal readonly struct CanonicalPlay
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

        // Hit marks belong to points, not to legs: a point holds at most one
        // opposing blot, so it is hit at most once, and which leg landing
        // there carries the mark is not part of the play's identity. Lift the
        // marks off the legs into the set of hit points; chains are built from
        // the unmarked legs, and each point's mark is placed back on its
        // carrier (MayJoinAt) once the chains are final. Everything below is
        // therefore a function of the unmarked legs and the hit-point set
        // alone, so encodings that differ only in attribution canonicalize
        // identically by construction.
        Span<Segment> legs = stackalloc Segment[n];
        Span<int> hitBuffer = stackalloc int[n];
        int hitCount = 0;
        for (int i = 0; i < n; i++)
        {
            var move = play[i];
            int to = Math.Abs(move.ToPt);
            legs[i] = new Segment(move.FrPt, to);
            if (move.ToPt < 0 && !hitBuffer[..hitCount].Contains(to))
                hitBuffer[hitCount++] = to;
        }
        ReadOnlySpan<int> hitPoints = hitBuffer[..hitCount];

        // Deterministic processing order — the canonical order (Precedes).
        // Chain-building matches greedily, so the same multiset of legs must
        // always be walked in the same order or two encodings of one play
        // could canonicalize differently (e.g. {(13,11),(11,9),(11,8)}: the
        // leg reaching 11 first grabs whichever continuation it meets first).
        // FrPt-descending also visits each checker's legs in journey order,
        // since every legal move decreases the point number.
        SortCanonically(legs);

        Span<Segment> chains = stackalloc Segment[n];
        int chainCount = 0;

        for (int i = 0; i < n; i++)
        {
            var leg = legs[i];
            var (from, to) = leg;

            int matchIdx = -1;
            bool isForward = false;

            if (from is >= 1 and <= 24)
            {
                // Forward: an existing chain ends where this leg starts. That
                // chain is the segment ending at the join, so the rule is
                // asked of it.
                for (int j = 0; j < chainCount; j++)
                {
                    if (chains[j].To == from && MayJoinAt(chains[j], j, chains[..chainCount], hitPoints))
                    {
                        matchIdx = j;
                        isForward = true;
                        break;
                    }
                }
            }

            if (matchIdx < 0 && to is >= 1 and <= 24
                && MayJoinAt(leg, -1, chains[..chainCount], hitPoints))
            {
                // Backward: this leg ends where an existing chain starts. The
                // leg is the segment ending at the join, so the rule is asked
                // of it; it is not a chain yet, so it has no index to exclude.
                // Unreachable for legal plays once legs are journey-ordered,
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
                    ? new Segment(c.From, to)
                    : new Segment(from, c.To);
            }
            else
            {
                chains[chainCount++] = new Segment(from, to);
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
                if (joinPt is < 1 or > 24 || !MayJoinAt(chains[a], a, chains[..chainCount], hitPoints))
                    continue;

                for (int b = 0; b < chainCount; b++)
                {
                    if (b == a || chains[b].From != joinPt) continue;

                    // a is the segment ending at the join, consumed by it.
                    chains[a] = new Segment(chains[a].From, chains[b].To);
                    chains[b] = chains[chainCount - 1];
                    chainCount--;
                    fused = true;
                    break;
                }
            }
        }

        var final = chains[..chainCount];
        SortCanonically(final);

        // Place each hit point's mark on its carrier: the first chain in
        // canonical order ending there. The join rule never consumed that
        // chain, so it exists.
        Span<PlayChain> result = stackalloc PlayChain[chainCount];
        for (int i = 0; i < chainCount; i++)
        {
            var (from, to) = final[i];
            bool carriesHit = hitPoints.Contains(to) && !AnyEndsAt(final[..i], to);
            result[i] = new PlayChain(from, carriesHit ? -to : to);
        }
        return new CanonicalPlay(result);
    }

    /// <summary>
    /// The single hit-visibility rule. A hit point P's mark is carried by the
    /// chain ending at P that comes first in canonical order — chains
    /// identical to it being interchangeable — so that chain must keep P as
    /// its endpoint. A join at P consumes <paramref name="consumed"/>, the
    /// segment ending there, and turns P into an interior point; it is
    /// therefore allowed unless P is hit and <paramref name="consumed"/> is
    /// P's carrier, that is, unless no other chain ending at P comes before it
    /// or level with it. Forward leg-matching, backward leg-matching, and
    /// chain-to-chain fusing all reduce to this predicate, and the final
    /// placement of each mark applies the same "first in canonical order".
    /// </summary>
    /// <param name="consumed">The segment ending at the join point.</param>
    /// <param name="consumedIdx">
    /// Its index in <paramref name="chains"/>, or -1 when it is a leg not yet
    /// in a chain.
    /// </param>
    /// <param name="chains">The chains built so far.</param>
    /// <param name="hitPoints">The play's set of hit points.</param>
    private static bool MayJoinAt(
        Segment consumed, int consumedIdx, ReadOnlySpan<Segment> chains, ReadOnlySpan<int> hitPoints)
    {
        if (!hitPoints.Contains(consumed.To)) return true;

        for (int i = 0; i < chains.Length; i++)
            if (i != consumedIdx && chains[i].To == consumed.To && !Precedes(consumed, chains[i]))
                return true;
        return false;
    }

    private static bool AnyEndsAt(ReadOnlySpan<Segment> chains, int pt)
    {
        foreach (var c in chains)
            if (c.To == pt) return true;
        return false;
    }

    /// <summary>
    /// Sorts into canonical order — From descending, then To descending —
    /// the one order for both the leg walk and the emitted chains.
    /// </summary>
    private static void SortCanonically(Span<Segment> segments)
    {
        for (int i = 1; i < segments.Length; i++)
            for (int j = i; j > 0 && Precedes(segments[j], segments[j - 1]); j--)
                (segments[j], segments[j - 1]) = (segments[j - 1], segments[j]);
    }

    private static bool Precedes(Segment a, Segment b)
    {
        if (a.From != b.From) return a.From > b.From;
        return a.To > b.To;
    }

    /// <summary>
    /// The play in standard backgammon notation: the one formatter, and the
    /// implementation of <see cref="Play.ToNotation"/>, whose doc comment
    /// states what it writes. The chains are written in canonical order,
    /// separated by single spaces, and a run of adjacent identical chains
    /// once with its count.
    /// </summary>
    /// <returns>The notation, or <see cref="string.Empty"/> for the empty play.</returns>
    public override string ToString()
    {
        if (Count == 0) return string.Empty;

        var text = new StringBuilder();
        int idx = 0;
        while (idx < Count)
        {
            var chain = this[idx];
            int to = Math.Abs(chain.ToPt);
            bool anyHit = chain.ToPt < 0;

            int run = 1;
            while (idx + run < Count
                   && this[idx + run].FrPt == chain.FrPt
                   && Math.Abs(this[idx + run].ToPt) == to)
            {
                if (this[idx + run].ToPt < 0) anyHit = true;
                run++;
            }

            if (text.Length > 0) text.Append(' ');
            text.Append(FromLabel(chain.FrPt)).Append('/').Append(ToLabel(to));
            if (run > 1) text.Append('(').Append(run.ToString(CultureInfo.InvariantCulture)).Append(')');
            if (anyHit) text.Append('*');

            idx += run;
        }

        return text.ToString();
    }

    private static string FromLabel(int pt) => pt == 25 ? "bar" : pt.ToString(CultureInfo.InvariantCulture);

    private static string ToLabel(int pt) => pt == 0 ? "off" : pt.ToString(CultureInfo.InvariantCulture);

    // Working representation during canonicalization: an unmarked leg or
    // chain, destination as a magnitude so join points compare sign-free.
    // Hit marks live apart, as the play's set of hit points.
    private readonly record struct Segment(int From, int To);

    /// <summary>
    /// Not supported: <see cref="CanonicalPlay"/> is the display form, not
    /// identity, and has no equality (see the type summary), so this throws.
    /// It is overridden only so that every comparison the runtime routes here
    /// fails loudly, instead of falling back to the default field-wise struct
    /// equality.
    /// </summary>
    /// <exception cref="NotSupportedException">Always.</exception>
    public override bool Equals(object? obj) => throw NoEquality();

    /// <summary>
    /// Not supported, as for <see cref="Equals(object)"/>: the form has no
    /// equality, so it has no hash either, and this throws.
    /// </summary>
    /// <exception cref="NotSupportedException">Always.</exception>
    public override int GetHashCode() => throw NoEquality();

    private static NotSupportedException NoEquality() => new(
        "CanonicalPlay has no equality: it is a play's display form, not its identity. Whether two "
        + "plays are the same play is BoardState.IsSamePlay, from the position they are played from.");
}
