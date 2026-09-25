using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace BgDataTypes_Lib;

/// <summary>
/// Mutable backgammon board position.
///
/// <para>
/// <c>Points[0..25]</c>: 26-element array.
///   <c>Points[25]</c> = on-roll player's bar,
///   <c>Points[1..24]</c> = playing surface,
///   <c>Points[0]</c> = opponent's bar.
/// </para>
///
/// <para>
/// Positive values = on-roll player's checkers; negative = opponent's.
/// On-roll moves from high indices toward low (25 → 1, bearing off past 1).
/// Layout matches <c>PositionData.Mop</c> / <c>IDecisionFilterData.Board</c>.
/// </para>
///
/// <para>
/// Designed for apply/undo mutation — no heap allocations during move generation.
/// Mutability is a deliberate exception within BgDataTypes_Lib (everything else
/// is class with init-only properties); see INSTRUCTIONS.md "Mutability exception".
/// </para>
///
/// <para>
/// Hot-path consumers use <see cref="ApplyMove(Move)"/> / <see cref="UndoMove(Move)"/>
/// to recurse through candidate plays; that pair trusts its moves. Non-hot-path
/// consumers should advance state via <see cref="ApplyPlay(Play)"/>, which
/// applies a whole play through the one play rule, refusing an invalid one, and
/// transparently flips perspective so the next call still reasons in on-roll POV.
/// </para>
///
/// <para>
/// Play identity is a question about positions, so it is asked of the starting
/// position: <see cref="IsSamePlay"/> (whose remarks state the contract and the
/// rule) and the list match <see cref="IndexOfSamePlay"/>.
/// </para>
/// </summary>
public class BoardState
{
    /// <summary>
    /// The raw 26-element point array (layout in the type summary). Exposed
    /// for hot-path reads and deliberate bulk writes; direct mutation
    /// bypasses the incremental bookkeeping, so follow it with
    /// <see cref="RecalcHighPoint"/> or <see cref="HighPointOccupied"/>
    /// desyncs.
    /// </summary>
    public readonly int[] Points = new int[26];

    /// <summary>
    /// Highest point (1–25) with an on-roll checker, 0 if none.
    /// Maintained incrementally by <see cref="ApplyMove(Move)"/> /
    /// <see cref="UndoMove(Move)"/>; recomputed by <see cref="RecalcHighPoint"/>
    /// after bulk mutation (e.g., <see cref="FromMop"/>, internal flip).
    /// External code that mutates <see cref="Points"/> directly must call
    /// <see cref="RecalcHighPoint"/> or this field will desync.
    /// </summary>
    public int HighPointOccupied;

    /// <summary>
    /// An empty board — all points zero, <see cref="HighPointOccupied"/> 0.
    /// Use the factories (<see cref="Standard"/>, <see cref="Nackgammon"/>,
    /// <see cref="Bg960"/>, <see cref="FromMop"/>) for populated starts.
    /// </summary>
    public BoardState() { }

    /// <summary>
    /// Recompute <see cref="HighPointOccupied"/> from scratch. Call after
    /// directly mutating <see cref="Points"/>.
    /// </summary>
    public void RecalcHighPoint()
    {
        HighPointOccupied = 0;
        for (int i = 25; i >= 1; i--)
        {
            if (Points[i] > 0) { HighPointOccupied = i; return; }
        }
    }

    /// <summary>Deep copy.</summary>
    public BoardState Copy()
    {
        var copy = new BoardState();
        Array.Copy(Points, copy.Points, 26);
        copy.HighPointOccupied = HighPointOccupied;
        return copy;
    }

    /// <summary>
    /// Return a new <see cref="BoardState"/> re-expressed from the opponent's
    /// perspective; this instance is untouched. Every value is negated and the
    /// array reversed (point <c>i</c> ↔ point <c>25 - i</c>), so the bars swap
    /// (<c>[0]</c> ↔ <c>[25]</c>), positive values become the previous opponent's
    /// checkers, and <see cref="PipCount"/> / <see cref="OpponentPipCount"/> swap.
    /// The transform is an involution: <c>FlippedCopy().FlippedCopy()</c>
    /// reproduces the original position. <see cref="HighPointOccupied"/> is
    /// recomputed for the new frame.
    ///
    /// <para>
    /// Use this to <em>query</em> a position from the other player's frame (e.g.
    /// cube-response evaluation) without advancing state. To advance past a turn
    /// boundary, use <see cref="ApplyPlay(Play)"/>, which applies moves and flips
    /// atomically in place.
    /// </para>
    /// </summary>
    public BoardState FlippedCopy()
    {
        var copy = Copy();
        copy.Flip();
        return copy;
    }

    // ── Mop bridge ────────────────────────────────────────────────

    /// <summary>
    /// Build a BoardState from a 26-element on-roll-relative point array
    /// (the <c>Mop</c> shape used by <c>PositionData.Mop</c> and
    /// <c>IDecisionFilterData.Board</c>). Layout matches <see cref="Points"/> exactly:
    /// <c>[0]</c> = opponent bar, <c>[1..24]</c> = playing surface, <c>[25]</c> = on-roll bar;
    /// positive = on-roll's checkers, negative = opponent's.
    /// <see cref="HighPointOccupied"/> is recomputed from the copied points.
    /// No checker-count or sign validation is performed — pseudoboards
    /// (e.g., cube-decision references) are legitimate inputs.
    /// </summary>
    public static BoardState FromMop(IReadOnlyList<int> mop)
    {
        ArgumentNullException.ThrowIfNull(mop);
        if (mop.Count != 26)
            throw new ArgumentException(
                $"Mop must have exactly 26 elements; got {mop.Count}.", nameof(mop));

        var s = new BoardState();
        for (int i = 0; i < 26; i++)
            s.Points[i] = mop[i];
        s.RecalcHighPoint();
        return s;
    }

    /// <summary>
    /// Return <see cref="Points"/> as a fresh 26-element list — same layout
    /// as <see cref="FromMop"/> accepts. Defensive copy: subsequent mutations
    /// of this BoardState do not affect the returned list, and vice versa.
    /// </summary>
    public IReadOnlyList<int> ToMop()
    {
        var copy = new int[26];
        Array.Copy(Points, copy, 26);
        return copy;
    }

    // ── Standard starting positions ───────────────────────────────

    /// <summary>
    /// Standard backgammon starting position.
    /// Player's checkers: 6-pt(5), 8-pt(3), 13-pt(5), 24-pt(2)
    /// Opponent's checkers: 19-pt(-5), 17-pt(-3), 12-pt(-5), 1-pt(-2)
    /// </summary>
    public static BoardState Standard()
    {
        var s = new BoardState();
        s.Points[6] = 5;
        s.Points[8] = 3;
        s.Points[13] = 5;
        s.Points[24] = 2;
        s.Points[19] = -5;
        s.Points[17] = -3;
        s.Points[12] = -5;
        s.Points[1] = -2;
        s.RecalcHighPoint();
        return s;
    }

    /// <summary>Nackgammon starting position.</summary>
    public static BoardState Nackgammon()
    {
        var s = new BoardState();
        s.Points[6] = 4;
        s.Points[8] = 3;
        s.Points[13] = 4;
        s.Points[23] = 2;
        s.Points[24] = 2;
        s.Points[19] = -4;
        s.Points[17] = -3;
        s.Points[12] = -4;
        s.Points[2] = -2;
        s.Points[1] = -2;
        s.RecalcHighPoint();
        return s;
    }

    // ── Bg960 setup ───────────────────────────────────────────────

    // Quadrant boundaries (1-indexed point indices)
    private static readonly (int from, int to)[] Quadrants =
    [
        (1,  6),   // home board
        (7,  12),  // outer board
        (13, 18),  // opponent outer board
        (19, 24),  // opponent home board
    ];

    // Made-point weights: num_points → weight
    private static readonly (int points, int weight)[] MadePointWeights =
    [
        (2, 1), (3, 3), (4, 10), (5, 10), (6, 5), (7, 2),
    ];

    /// <summary>
    /// Generate a random Bg960 starting position.
    /// Constraints: symmetrical, no blots (≥ 2 per point), one point per quadrant,
    /// no mirror conflicts, pip count ≥ 100, weighted toward 4–5 made points.
    /// </summary>
    /// <param name="seed">Optional RNG seed for reproducibility. Null = random.</param>
    /// <exception cref="InvalidOperationException">No valid position found in 1000 attempts.</exception>
    public static BoardState Bg960(int? seed = null)
    {
        var rng = seed.HasValue ? new Random(seed.Value) : new Random();

        // Precompute sampling distribution
        int maxPoints = 15 / 2;  // min 2 checkers per point → max 7 points
        int totalWeight = 0;
        for (int i = 0; i < MadePointWeights.Length; i++)
            if (MadePointWeights[i].points >= 4 && MadePointWeights[i].points <= maxPoints)
                totalWeight += MadePointWeights[i].weight;

        for (int attempt = 0; attempt < 1000; attempt++)
        {
            int numPoints = SampleNumPoints(rng, totalWeight);
            int[]? points = SelectPoints(rng, numPoints);
            if (points == null) continue;

            int[] checkers = DistributeCheckers(rng, points, 15, 2);

            // Check pip count (1-indexed: point i contributes checkers[i-1] * i)
            int pips = 0;
            for (int i = 0; i < points.Length; i++)
                pips += checkers[i] * points[i];
            if (pips < 100) continue;

            // Build board
            var s = new BoardState();
            for (int i = 0; i < points.Length; i++)
            {
                int pt = points[i];
                int mirror = 25 - pt;   // 1-indexed mirror
                s.Points[pt] = checkers[i];
                s.Points[mirror] = -checkers[i];
            }
            s.RecalcHighPoint();
            return s;
        }

        throw new InvalidOperationException("Bg960: failed to generate valid position in 1000 attempts");
    }

    private static int SampleNumPoints(Random rng, int totalWeight)
    {
        int r = rng.Next(totalWeight);
        int cumulative = 0;
        for (int i = 0; i < MadePointWeights.Length; i++)
        {
            var (points, weight) = MadePointWeights[i];
            if (points < 4 || points > 15 / 2) continue;
            cumulative += weight;
            if (r < cumulative) return points;
        }
        return MadePointWeights[^1].points;
    }

    /// <summary>
    /// Select numPoints distinct points satisfying quadrant coverage and no mirror conflicts.
    /// Returns null if 1000 inner attempts fail.
    /// </summary>
    private static int[]? SelectPoints(Random rng, int numPoints)
    {
        for (int attempt = 0; attempt < 1000; attempt++)
        {
            var blocked = new HashSet<int>();
            var mandatory = new List<int>();
            bool failed = false;

            // One mandatory point per quadrant
            foreach (var (from, to) in Quadrants)
            {
                var candidates = new List<int>();
                for (int p = from; p <= to; p++)
                    if (!blocked.Contains(p)) candidates.Add(p);

                if (candidates.Count == 0) { failed = true; break; }

                int pt = candidates[rng.Next(candidates.Count)];
                mandatory.Add(pt);
                blocked.Add(pt);
                blocked.Add(25 - pt);   // block mirror
            }

            if (failed) continue;

            if (numPoints < mandatory.Count) continue;

            // Fill remaining slots
            int remaining = numPoints - mandatory.Count;
            var available = new List<int>();
            for (int p = 1; p <= 24; p++)
                if (!blocked.Contains(p)) available.Add(p);

            if (remaining > available.Count) continue;

            var extra = new List<int>();
            for (int i = 0; i < remaining; i++)
            {
                if (available.Count == 0) break;
                int idx = rng.Next(available.Count);
                int pt = available[idx];
                extra.Add(pt);
                available.RemoveAt(idx);
                available.Remove(25 - pt);  // remove mirror
            }

            if (extra.Count < remaining) continue;

            mandatory.AddRange(extra);
            mandatory.Sort();
            return mandatory.ToArray();
        }

        return null;
    }

    /// <summary>
    /// Distribute totalCheckers across points with at least minPerPoint each.
    /// Remainder distributed via stars-and-bars (sorted random dividers).
    /// </summary>
    private static int[] DistributeCheckers(Random rng, int[] points, int totalCheckers, int minPerPoint)
    {
        int k = points.Length;
        int remainder = totalCheckers - minPerPoint * k;
        int[] extra = new int[k];

        if (remainder > 0)
        {
            // Stars and bars: k-1 random dividers in [0, remainder]
            int[] dividers = new int[k - 1];
            for (int i = 0; i < dividers.Length; i++)
                dividers[i] = rng.Next(remainder + 1);
            Array.Sort(dividers);

            int prev = 0;
            for (int i = 0; i < k - 1; i++)
            {
                extra[i] = dividers[i] - prev;
                prev = dividers[i];
            }
            extra[k - 1] = remainder - prev;
        }

        int[] result = new int[k];
        for (int i = 0; i < k; i++)
            result[i] = minPerPoint + extra[i];
        return result;
    }

    // ── Apply / Undo (instance, hot-path) ─────────────────────────

    /// <summary>
    /// Apply a single move in place — the raw, trusting half of the hot-path
    /// pair with <see cref="UndoMove(Move)"/>. Maintains <see cref="HighPointOccupied"/>
    /// incrementally — when emptying the highest point, scans down for the new high.
    ///
    /// <para>
    /// <b>Precondition: the move agrees with the board as it stands.</b> Its
    /// source holds one of the mover's checkers, and its hit mark agrees with
    /// its landing point: a marked move lands on a point holding exactly one
    /// opposing checker, an unmarked move on a point holding none. The mark is
    /// trusted, not derived — a marked move sets the landing point to one
    /// mover's checker and bars the blot, an unmarked one adds to the point —
    /// so a move that breaks the precondition corrupts the board. Debug builds
    /// assert it; release builds do not check. No legality validation either;
    /// callers (e.g. BgMoveGen.MoveGenerator) own that. Callers outside the
    /// hot path apply whole plays with <see cref="ApplyPlay(Play)"/>, which checks.
    /// </para>
    /// </summary>
    public void ApplyMove(Move move)
    {
        Debug.Assert(Points[move.FrPt] > 0,
            "ApplyMove: the source point holds none of the mover's checkers.");
        Debug.Assert(move.ToPt < 0 ? Points[-move.ToPt] == -1 : move.ToPt == 0 || Points[move.ToPt] >= 0,
            "ApplyMove: the hit mark disagrees with the landing point.");

        Points[move.FrPt]--;
        if (move.ToPt > 0)
        {
            Points[move.ToPt]++;
        }
        else if (move.ToPt < 0)
        {
            int dest = -move.ToPt;
            Points[dest] = 1;
            Points[0]--;
        }
        // ToPt == 0: bear off, checker disappears.

        if (move.FrPt == HighPointOccupied && Points[move.FrPt] == 0)
        {
            HighPointOccupied = 0;
            for (int i = move.FrPt - 1; i >= 1; i--)
            {
                if (Points[i] > 0) { HighPointOccupied = i; break; }
            }
        }
    }

    /// <summary>
    /// Reverse a previously applied move in place. The move encodes everything
    /// needed (regular / hit / bear-off) — no separate undo log required.
    ///
    /// <para>
    /// <b>Precondition: the board is as the move left it.</b> Its landing point
    /// holds one of the mover's checkers — for a marked move exactly one, the
    /// hitter, because undoing a hit restores the opposing blot there. Undoing
    /// in the reverse order of applying guarantees it; undoing a hit while
    /// another mover's checker shares its point corrupts the board. Debug
    /// builds assert it; release builds do not check.
    /// </para>
    /// </summary>
    public void UndoMove(Move move)
    {
        Debug.Assert(move.ToPt < 0 ? Points[-move.ToPt] == 1 : move.ToPt == 0 || Points[move.ToPt] > 0,
            "UndoMove: the landing point is not as the move left it.");

        if (move.ToPt > 0)
        {
            Points[move.ToPt]--;
        }
        else if (move.ToPt < 0)
        {
            int dest = -move.ToPt;
            Points[dest] = -1;
            Points[0]++;
        }

        Points[move.FrPt]++;
        if (move.FrPt > HighPointOccupied)
            HighPointOccupied = move.FrPt;
    }

    // ── Turn boundary: ApplyPlay (apply-all + flip) ───────────────

    /// <summary>
    /// Apply <paramref name="play"/> and flip perspective so the state is
    /// re-expressed from the next mover's POV. After this call, the previous
    /// opponent is on roll; positive values are now their checkers.
    ///
    /// <para>
    /// The position reached, and which plays are invalid, are the rule stated
    /// on <see cref="IsSamePlay"/>, so applying a play never disagrees with its
    /// identity and never leaves a corrupt board. An invalid play is refused
    /// with <see cref="ArgumentException"/> and the board is left untouched;
    /// <see cref="TryApplyPlay"/> is the non-throwing form. Legality (the dice
    /// and the rest) stays with the move generator (<c>BgMoveGen</c>).
    /// </para>
    ///
    /// <para>
    /// This is the turn-boundary primitive: callers reasoning in on-roll POV
    /// never need to flip directly. Empty plays (<c>play.Count == 0</c>) still
    /// flip — they represent a forced pass.
    /// </para>
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="play"/> is invalid from this position; the message
    /// names the fault. The board is unchanged.
    /// </exception>
    public void ApplyPlay(Play play)
    {
        var fault = Advance(in play, out var culprit);
        if (fault != PlayFault.None)
            ThrowInvalidPlay(fault, culprit, nameof(play));
    }

    /// <summary>
    /// Apply <paramref name="play"/> and flip perspective exactly as
    /// <see cref="ApplyPlay"/> does when the play is valid from this position,
    /// and return true; return false and leave the board untouched when it is
    /// not. Validity is the rule stated on <see cref="IsSamePlay"/>.
    /// </summary>
    public bool TryApplyPlay(Play play) => Advance(in play, out _) == PlayFault.None;

    /// <summary>
    /// The one body of <see cref="ApplyPlay"/> and <see cref="TryApplyPlay"/>:
    /// computes the position <paramref name="play"/> reaches by the play rule
    /// and, when it is valid, commits it and flips. On a fault nothing is
    /// written — the rule computes into scratch space, never into
    /// <see cref="Points"/>.
    /// </summary>
    private PlayFault Advance(in Play play, out Move culprit)
    {
        Span<int> reached = stackalloc int[26];
        var fault = Reach(Points, in play, reached, out culprit);
        if (fault == PlayFault.None)
        {
            reached.CopyTo(Points);
            Flip();
        }
        return fault;
    }

    // ── Play identity: the one rule, from this position ───────────

    /// <summary>
    /// Whether <paramref name="first"/> and <paramref name="second"/> are the
    /// same play from this position — play identity. These remarks are the
    /// one statement of the identity contract and of the rule behind it; the
    /// other members and documents that depend on it point here.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two plays are the same play from a starting position exactly when
    /// both are valid from it and reach the same position</b>
    /// (halheinrich/backgammon#273, halheinrich/backgammon#277). Identity
    /// belongs to positions, not notation, so one rule covers the order the
    /// moves are written in, how a checker's trajectory is decomposed into
    /// hops, how sources pair with destinations (<c>25/10 20/15*</c> and
    /// <c>25/15* 20/10</c> with 5-5), and which checker carries a hit mark
    /// (<c>8/3* 7/3</c> and <c>8/3 7/3*</c>). A <see cref="Play"/> carries no
    /// board, so no board-less comparison exists: neither <see cref="Play"/>
    /// nor <see cref="CanonicalPlay"/> has equality.
    /// </para>
    /// <para>
    /// <b>The position a play reaches.</b> Each move is a hop of one of the
    /// mover's checkers, and its destination is a <em>stated landing
    /// point</em>, whether intermediate to that checker's trajectory or final.
    /// Hits come from the board, and the marks are checked against it: a
    /// stated landing point holding exactly one opposing checker in this
    /// position is hit, sending that checker to the opponent's bar, and some
    /// hop landing there must carry the hit mark. Which hop carries it, and
    /// whether more than one does, does not matter. The play is
    /// <b>invalid</b> when:
    /// </para>
    /// <list type="bullet">
    /// <item><description>a hop carries a mark on a point that does not hold
    /// exactly one opposing checker;</description></item>
    /// <item><description>a hop lands on an opposing blot and no hop landing
    /// there carries a mark;</description></item>
    /// <item><description>a hop lands on a point the opponent holds with two
    /// or more checkers;</description></item>
    /// <item><description>a hop starts from a point holding none of the
    /// mover's checkers, counting the checkers the play's other hops bring
    /// there;</description></item>
    /// <item><description>a hop does not move toward home within the board:
    /// from a point 1–24 or the bar (25) to a lower point, or off
    /// (0).</description></item>
    /// </list>
    /// <para>
    /// Neither validity nor the position reached depends on the order the
    /// moves are written in. An invalid play reaches no position, so it is
    /// never the same play as any play, an identical encoding included.
    /// Positions compare by all 26 counts of <see cref="Points"/>, both bars
    /// included; borne-off checkers need no comparison, since both plays
    /// leave from this one position.
    /// </para>
    /// <para>
    /// <b>This is not a legality check.</b> The dice, forced moves, entering
    /// from the bar before any other checker moves, and the bear-off
    /// conditions stay with the move generator (BgMoveGen): a play no roll
    /// could produce can still be valid here. What the rule guarantees is that
    /// a valid play reaches a well-formed position, and the same one however
    /// the play is written.
    /// </para>
    /// <para>
    /// <see cref="ApplyPlay"/> and <see cref="TryApplyPlay"/> apply this same
    /// rule, so applying a play never disagrees with its identity;
    /// <see cref="IndexOfSamePlay"/> is the list match built on it. This
    /// position is only read.
    /// </para>
    /// </remarks>
    public bool IsSamePlay(Play first, Play second)
    {
        Span<int> target = stackalloc int[26];
        if (Reach(Points, in first, target, out _) != PlayFault.None)
            return false;
        Span<int> scratch = stackalloc int[26];
        return ReachesTarget(in second, target, scratch);
    }

    /// <summary>
    /// The index of the first entry of <paramref name="plays"/> that is the
    /// same play as <paramref name="play"/> from this position, or -1 when none
    /// is — the list match, for finding a play among the legal ones or a
    /// submitted play among a decision's candidates.
    /// </summary>
    /// <remarks>
    /// Identity is <see cref="IsSamePlay"/>'s, so an entry is found whatever
    /// its encoding. An invalid <paramref name="play"/> matches nothing and
    /// yields -1. The list is not validated; each entry is held to the same
    /// rule, so an invalid entry reaches no position, matches nothing, and is
    /// passed over. When several entries are the same play — a list of
    /// distinct plays never holds two — the lowest index among them is
    /// returned. Acting on a match, applying either play reaches the same
    /// position. This position is only read.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="plays"/> is null.</exception>
    public int IndexOfSamePlay(Play play, IReadOnlyList<Play> plays)
    {
        ArgumentNullException.ThrowIfNull(plays);

        Span<int> target = stackalloc int[26];
        if (Reach(Points, in play, target, out _) != PlayFault.None)
            return -1;
        Span<int> scratch = stackalloc int[26];
        for (int i = 0; i < plays.Count; i++)
        {
            var candidate = plays[i];
            if (ReachesTarget(in candidate, target, scratch))
                return i;
        }
        return -1;
    }

    /// <summary>
    /// The identity comparison, single-sourced for <see cref="IsSamePlay"/>
    /// and <see cref="IndexOfSamePlay"/>: whether <paramref name="play"/> is
    /// valid from this position and reaches <paramref name="target"/>, a
    /// position a valid play reaches from here. <paramref name="scratch"/> is
    /// working space.
    /// </summary>
    private bool ReachesTarget(in Play play, ReadOnlySpan<int> target, Span<int> scratch) =>
        Reach(Points, in play, scratch, out _) == PlayFault.None
        && scratch.SequenceEqual(target);

    /// <summary>How a play fails the rule stated on <see cref="IsSamePlay"/>.</summary>
    private enum PlayFault
    {
        /// <summary>The play is valid.</summary>
        None,
        /// <summary>A hop does not move toward home within the board.</summary>
        NotForward,
        /// <summary>A hop lands on a point the opponent holds with two or more checkers.</summary>
        Blocked,
        /// <summary>A hop's mark sits on a point not holding exactly one opposing checker.</summary>
        MarkWithoutBlot,
        /// <summary>An opposing blot is landed on and no hop landing there is marked.</summary>
        BlotUnmarked,
        /// <summary>A hop starts from a point holding none of the mover's checkers.</summary>
        SourceEmpty,
    }

    /// <summary>
    /// The play rule stated on <see cref="IsSamePlay"/> — the one computation
    /// behind identity, the list match and applying a play. Writes the
    /// position <paramref name="play"/> reaches from <paramref name="start"/>
    /// into <paramref name="reached"/>, in the mover's frame (unflipped), and
    /// returns <see cref="PlayFault.None"/>; or returns the first fault found,
    /// with <paramref name="culprit"/> the offending move and
    /// <paramref name="reached"/> unspecified. Allocation-free.
    /// </summary>
    private static PlayFault Reach(
        ReadOnlySpan<int> start, in Play play, Span<int> reached, out Move culprit)
    {
        culprit = default;
        int n = play.Count;
        Span<Move> hops = stackalloc Move[4];
        for (int i = 0; i < n; i++)
            hops[i] = play[i];
        hops = hops[..n];

        // The encoding range first, so no later step indexes off the board
        // (and Math.Abs never meets int.MinValue): from a point 1-24 or the
        // bar, to a lower point or off.
        foreach (var hop in hops)
        {
            if (hop.FrPt is < 1 or > 25 || hop.ToPt is < -24 or > 24
                || Math.Abs(hop.ToPt) >= hop.FrPt)
            {
                culprit = hop;
                return PlayFault.NotForward;
            }
        }

        // Every stated landing point, against the starting position: a made
        // point is closed, and a point is hit exactly when it holds an
        // opposing blot, which some hop landing there must mark.
        foreach (var hop in hops)
        {
            int to = Math.Abs(hop.ToPt);
            if (to == 0) continue;   // borne off: no landing point
            int held = start[to];
            culprit = hop;
            if (held <= -2) return PlayFault.Blocked;
            if (hop.ToPt < 0 && held != -1) return PlayFault.MarkWithoutBlot;
            if (held == -1 && !AnyMarkedLanding(hops, to)) return PlayFault.BlotUnmarked;
        }
        culprit = default;

        // The hits come from the board: each hit point's blot goes to the
        // opponent's bar, once however many hops mark it. Every -1 at a
        // landing point is a hit point now that the marks have checked out.
        start.CopyTo(reached);
        foreach (var hop in hops)
        {
            int to = Math.Abs(hop.ToPt);
            if (to != 0 && reached[to] == -1)
            {
                reached[to] = 0;
                reached[0]--;
            }
        }

        // Then the hops, highest source first. Every hop moves toward home,
        // so any checker arriving at a point comes from a higher one: this
        // order lets a hop start from a point another hop reaches, whatever
        // order the play writes them in, and a hop that still finds no
        // mover's checker at its source has none to move.
        SortBySourceDescending(hops);
        foreach (var hop in hops)
        {
            if (reached[hop.FrPt] <= 0)
            {
                culprit = hop;
                return PlayFault.SourceEmpty;
            }
            reached[hop.FrPt]--;
            int to = Math.Abs(hop.ToPt);
            if (to != 0) reached[to]++;
        }
        return PlayFault.None;
    }

    private static bool AnyMarkedLanding(ReadOnlySpan<Move> hops, int point)
    {
        foreach (var hop in hops)
            if (hop.ToPt == -point) return true;
        return false;
    }

    private static void SortBySourceDescending(Span<Move> hops)
    {
        for (int i = 1; i < hops.Length; i++)
            for (int j = i; j > 0 && hops[j].FrPt > hops[j - 1].FrPt; j--)
                (hops[j], hops[j - 1]) = (hops[j - 1], hops[j]);
    }

    /// <summary>
    /// The <see cref="ApplyPlay"/> refusal, out of line: the message names
    /// the fault and the offending move, for a caller holding a play it
    /// believed valid.
    /// </summary>
    [DoesNotReturn]
    private static void ThrowInvalidPlay(PlayFault fault, Move culprit, string paramName)
    {
        string hop = $"({culprit.FrPt}, {culprit.ToPt})";
        // Only a move past the encoding check has a landing point; an
        // off-board ToPt may be int.MinValue, which Math.Abs cannot take.
        int to = fault == PlayFault.NotForward ? 0 : Math.Abs(culprit.ToPt);
        string reason = fault switch
        {
            PlayFault.NotForward => $"the move {hop} does not move toward home within the board",
            PlayFault.Blocked => $"the move {hop} lands on point {to}, which the opponent holds with two or more checkers",
            PlayFault.MarkWithoutBlot => $"the move {hop} is marked as a hit, but point {to} does not hold exactly one opposing checker",
            PlayFault.BlotUnmarked => $"point {to} holds an opposing blot, but no move landing there is marked as a hit",
            PlayFault.SourceEmpty => $"the move {hop} starts from point {culprit.FrPt}, which holds none of the mover's checkers",
            _ => throw new UnreachableException(),
        };
        throw new ArgumentException(
            $"The play is invalid from this position: {reason}. See BoardState.IsSamePlay for the rule.",
            paramName);
    }

    /// <summary>
    /// Flip perspective: negate every value and reverse the array, so points
    /// 0↔25, 1↔24, …, 12↔13. Implementation mechanics for
    /// <see cref="ApplyPlay(Play)"/> and <see cref="FlippedCopy"/> — never
    /// exposed publicly. Callers should always reason in on-roll POV;
    /// <see cref="ApplyPlay(Play)"/> performs the flip atomically with the
    /// move application, and <see cref="FlippedCopy"/> flips a copy for
    /// other-frame queries.
    ///
    /// Borne-off counts are not tracked on <see cref="BoardState"/> (checkers
    /// simply leave the board), so nothing else needs swapping.
    /// </summary>
    private void Flip()
    {
        for (int i = 0; i < 13; i++)
        {
            int j = 25 - i;
            int a = Points[i];
            int b = Points[j];
            Points[i] = -b;
            Points[j] = -a;
        }
        // Flip changed which point is "high"; recompute from scratch.
        RecalcHighPoint();
    }

    // ── Derived properties ────────────────────────────────────────

    /// <summary>
    /// Pip count for the on-roll player: sum over <c>i ∈ [1..25]</c> of
    /// <c>i × Points[i]</c> for positive entries. Bar checkers contribute 25
    /// pips each; bear-off contributes 0 (checkers off the board are gone).
    ///
    /// <para>
    /// Use <see langword="int"/> arithmetic — products fit comfortably (15 × 25 = 375
    /// max contribution; total ≤ 375). See BgMoveGen pitfall on integer width.
    /// </para>
    ///
    /// <para>
    /// Distinct from <c>PositionData.OnRollPipCount</c>, which carries the
    /// XG-parser-supplied value. This property is a pure derivation from
    /// <see cref="Points"/> and may not match parser output bit-for-bit if XG
    /// ever rounds.
    /// </para>
    /// </summary>
    public int PipCount
    {
        get
        {
            int total = 0;
            for (int i = 1; i <= 25; i++)
            {
                int n = Points[i];
                if (n > 0) total += i * n;
            }
            return total;
        }
    }

    /// <summary>
    /// Pip count for the opponent: sum over <c>i ∈ [0..24]</c> of
    /// <c>(25 - i) × |Points[i]|</c> for negative entries. Opponent's bar
    /// (index 0) contributes 25 pips per checker; opponent moves
    /// low-index → high-index in the on-roll storage frame, so distance to
    /// bear-off from index <c>i</c> is <c>25 - i</c>.
    /// </summary>
    public int OpponentPipCount
    {
        get
        {
            int total = 0;
            for (int i = 0; i <= 24; i++)
            {
                int n = Points[i];
                if (n < 0) total += (25 - i) * (-n);
            }
            return total;
        }
    }

    /// <summary>
    /// True iff the position is a race — no on-roll checker can ever collide
    /// with an opponent checker. Equivalent statements:
    /// <list type="bullet">
    /// <item>Backgammon-natural: opponent's furthest-back checker is past
    /// on-roll's furthest-back checker.</item>
    /// <item>Array-index form: <c>max(i where Points[i] &gt; 0) &lt; min(i where Points[i] &lt; 0)</c>.</item>
    /// </list>
    /// On-roll moves high → low in this frame; opponent moves low → high. A
    /// race exists iff the two ranges no longer overlap. If either side has
    /// no checkers (all borne off), the position is vacuously a race.
    /// </summary>
    public bool IsRace
    {
        get
        {
            int maxOnRoll = int.MinValue;
            int minOpponent = int.MaxValue;
            for (int i = 0; i <= 25; i++)
            {
                int n = Points[i];
                if (n > 0) { if (i > maxOnRoll) maxOnRoll = i; }
                else if (n < 0) { if (i < minOpponent) minOpponent = i; }
            }
            return maxOnRoll < minOpponent;
        }
    }
}
