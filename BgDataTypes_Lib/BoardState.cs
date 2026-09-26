using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace BgDataTypes_Lib;

/// <summary>
/// A mutable backgammon board: the working copy that play applies to.
///
/// <para>
/// <see cref="Points"/> is the 26-count layout of <see cref="BoardPosition"/>:
///   <c>Points[25]</c> = on-roll player's bar,
///   <c>Points[1..24]</c> = playing surface,
///   <c>Points[0]</c> = opponent's bar.
/// Positive values = on-roll player's checkers; negative = opponent's.
/// On-roll moves from high indices toward low (25 → 1, bearing off past 1).
/// </para>
///
/// <para>
/// <b>Callers read it; only its own members write it</b>
/// (halheinrich/backgammon#281). <see cref="Points"/> is a read-only span and
/// <see cref="HighPointOccupied"/> has a private setter, so a write from
/// outside this library does not compile. A board comes into being only by
/// construction — from a position value
/// (<see cref="BoardState(BoardPosition)"/>, and the starting positions
/// <see cref="Standard"/>, <see cref="Nackgammon"/> and <see cref="Bg960"/>
/// built on it), from raw counts (<see cref="FromMop"/>, which validates
/// them), or from another board (<see cref="Copy"/>, <see cref="FlippedCopy"/>)
/// — and then changes only through the raw pair
/// <see cref="ApplyMove(Move)"/> / <see cref="UndoMove(Move)"/>, through
/// <see cref="ApplyPlay(Play)"/> / <see cref="TryApplyPlay(Play)"/>, and
/// through <see cref="SetPosition"/>, which replaces the whole board with a
/// position value. So the board is always a well-formed position
/// (<see cref="BoardPosition"/>'s invariant): every way in is one, the play
/// rule keeps it one, a reset sets it to one, and the raw pair keeps it one
/// under its stated preconditions. The class is sealed: a
/// type that guards an invariant is not open to subclasses.
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
/// <see cref="ToPosition"/> takes the board as a <see cref="BoardPosition"/>
/// value, which is how two boards are compared.
/// </para>
///
/// <para>
/// Play identity is a question about positions, so it is asked of the starting
/// position: <see cref="IsSamePlay"/> (whose remarks state the contract and the
/// rule) and the list match <see cref="IndexOfSamePlay"/>.
/// </para>
/// </summary>
public sealed class BoardState
{
    private readonly int[] _points = new int[26];

    /// <summary>
    /// The 26 counts, read-only (layout in the type summary). Exposed for
    /// hot-path reads: a span over the board's own storage, so reading
    /// allocates nothing and a write does not compile. It reads the board as
    /// it stands; <see cref="ToPosition"/> takes a value that later changes do
    /// not reach.
    /// </summary>
    public ReadOnlySpan<int> Points => _points;

    /// <summary>
    /// Highest point (1–25) with an on-roll checker, 0 if none.
    /// Maintained incrementally by <see cref="ApplyMove(Move)"/> /
    /// <see cref="UndoMove(Move)"/>, and recomputed whenever the board is
    /// built or flipped. Only this type writes it.
    /// </summary>
    public int HighPointOccupied { get; private set; }

    /// <summary>
    /// A board holding <paramref name="position"/> — the construction from a
    /// position value, which every other way in goes through.
    /// </summary>
    /// <param name="position">The position to start from; well-formed by its own invariant.</param>
    public BoardState(BoardPosition position) => SetPosition(position);

    /// <summary>
    /// Replace the whole board with <paramref name="position"/> and recompute
    /// <see cref="HighPointOccupied"/> — the reset, for a caller that reuses
    /// one board across positions rather than building one per position (the
    /// move generator's interop does). Validated by its argument: a
    /// <see cref="BoardPosition"/> is well-formed by its own invariant, so the
    /// board stays one. Allocation-free.
    /// </summary>
    /// <remarks>
    /// The one write of a whole position: construction, the flip behind
    /// <see cref="ApplyPlay(Play)"/> and <see cref="FlippedCopy"/>, and
    /// committing a play all go through it. <see cref="ToPosition"/> is its
    /// read.
    /// </remarks>
    /// <param name="position">The position the board holds from now on.</param>
    public void SetPosition(BoardPosition position)
    {
        position.CopyTo(_points);
        RecalcHighPoint();
    }

    /// <summary>A board at the standard start, <see cref="BoardPosition.Standard"/>.</summary>
    public static BoardState Standard() => new(BoardPosition.Standard);

    /// <summary>A board at the Nackgammon start, <see cref="BoardPosition.Nackgammon"/>.</summary>
    public static BoardState Nackgammon() => new(BoardPosition.Nackgammon);

    /// <summary>
    /// A board at a random Bg960 start, <see cref="BoardPosition.Bg960"/>
    /// (whose summary states the constraints).
    /// </summary>
    /// <param name="seed">Optional RNG seed for reproducibility. Null = random.</param>
    /// <exception cref="InvalidOperationException">No valid position found in 1000 attempts.</exception>
    public static BoardState Bg960(int? seed = null) => new(BoardPosition.Bg960(seed));

    /// <summary>The copy constructor behind <see cref="Copy"/>.</summary>
    private BoardState(BoardState source)
    {
        Array.Copy(source._points, _points, _points.Length);
        HighPointOccupied = source.HighPointOccupied;
    }

    /// <summary>
    /// Build a board from 26 counts in the <see cref="Points"/> layout — the
    /// door from raw outside data, such as a count array in a test or a
    /// parser. The counts must form a well-formed position
    /// (<see cref="BoardPosition"/>'s invariant); partial boards are fine,
    /// since borne-off checkers are not tracked. Equivalent to
    /// <c>new BoardState(new BoardPosition(mop))</c>, and validated by that
    /// one rule.
    /// </summary>
    /// <param name="mop">Exactly 26 counts forming a well-formed position.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="mop"/> does not hold exactly 26 counts, or they do not
    /// form a well-formed position; the message names the fault.
    /// </exception>
    public static BoardState FromMop(ReadOnlySpan<int> mop)
    {
        if (!BoardPosition.TryCreate(mop, out var position, out var fault))
            throw new ArgumentException(fault, nameof(mop));
        return new BoardState(position);
    }

    /// <summary>
    /// The board as it stands, as a <see cref="BoardPosition"/> value — a
    /// snapshot: later changes to this board do not reach it. Allocation-free.
    /// </summary>
    public BoardPosition ToPosition() => BoardPosition.FromWellFormed(_points);

    /// <summary>
    /// Recompute <see cref="HighPointOccupied"/> from scratch, after a whole
    /// position is written (<see cref="SetPosition"/>).
    /// </summary>
    private void RecalcHighPoint()
    {
        HighPointOccupied = 0;
        for (int i = 25; i >= 1; i--)
        {
            if (_points[i] > 0) { HighPointOccupied = i; return; }
        }
    }

    /// <summary>Deep copy.</summary>
    public BoardState Copy() => new(this);

    /// <summary>
    /// Return a new <see cref="BoardState"/> re-expressed from the opponent's
    /// perspective by <see cref="BoardPosition.Flipped"/>'s rule; this instance
    /// is untouched. The points mirror (point <c>i</c> ↔ point <c>25 - i</c>),
    /// the bars swap (<c>[0]</c> ↔ <c>[25]</c>), positive values become the
    /// previous opponent's checkers, and <see cref="PipCount"/> /
    /// <see cref="OpponentPipCount"/> swap. The transform is an involution:
    /// <c>FlippedCopy().FlippedCopy()</c> reproduces the original position.
    /// <see cref="HighPointOccupied"/> is recomputed for the new frame.
    ///
    /// <para>
    /// Use this to <em>query</em> a position from the other player's frame (e.g.
    /// cube-response evaluation) without advancing state; for a value, not a
    /// board, <c>ToPosition().Flipped()</c> does the same without allocating.
    /// To advance past a turn boundary, use <see cref="ApplyPlay(Play)"/>,
    /// which applies moves and flips atomically in place.
    /// </para>
    /// </summary>
    public BoardState FlippedCopy()
    {
        var copy = Copy();
        copy.Flip();
        return copy;
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
        Debug.Assert(_points[move.FrPt] > 0,
            "ApplyMove: the source point holds none of the mover's checkers.");
        Debug.Assert(move.ToPt < 0 ? _points[-move.ToPt] == -1 : move.ToPt == 0 || _points[move.ToPt] >= 0,
            "ApplyMove: the hit mark disagrees with the landing point.");

        _points[move.FrPt]--;
        if (move.ToPt > 0)
        {
            _points[move.ToPt]++;
        }
        else if (move.ToPt < 0)
        {
            int dest = -move.ToPt;
            _points[dest] = 1;
            _points[0]--;
        }
        // ToPt == 0: bear off, checker disappears.

        if (move.FrPt == HighPointOccupied && _points[move.FrPt] == 0)
        {
            HighPointOccupied = 0;
            for (int i = move.FrPt - 1; i >= 1; i--)
            {
                if (_points[i] > 0) { HighPointOccupied = i; break; }
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
        Debug.Assert(move.ToPt < 0 ? _points[-move.ToPt] == 1 : move.ToPt == 0 || _points[move.ToPt] > 0,
            "UndoMove: the landing point is not as the move left it.");

        if (move.ToPt > 0)
        {
            _points[move.ToPt]--;
        }
        else if (move.ToPt < 0)
        {
            int dest = -move.ToPt;
            _points[dest] = -1;
            _points[0]++;
        }

        _points[move.FrPt]++;
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
            ThrowInvalidPlay(fault, culprit, nameof(play), "The play", "this position");
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
    /// and, when it is valid, sets the board to it seen from the other side
    /// (the value's flip) in one write. On a fault nothing is written — the
    /// rule computes into scratch space, never into <see cref="Points"/>.
    /// The reached board is well-formed because this one is and the rule
    /// keeps it so, which is what lets the value skip its outside-data check.
    /// </summary>
    private PlayFault Advance(in Play play, out Move culprit)
    {
        Span<int> reached = stackalloc int[26];
        var fault = Reach(_points, in play, reached, out culprit);
        if (fault == PlayFault.None)
            SetPosition(BoardPosition.FromWellFormed(reached).Flipped());
        return fault;
    }

    /// <summary>
    /// The board <paramref name="play"/> leaves from <paramref name="start"/>:
    /// the position it reaches by the play rule stated on
    /// <see cref="IsSamePlay"/>, seen from the other side — exactly the board
    /// <see cref="ApplyPlay"/> leaves, as a value, in the next mover's frame.
    /// No board is built and nothing is allocated, so a caller holding only a
    /// position (a decision record) derives through the one rule at no cost
    /// per call.
    /// </summary>
    /// <param name="start">The position the play is made from, in the mover's frame.</param>
    /// <param name="play">The play.</param>
    /// <param name="subject">What the refusal calls the play, e.g. <c>"Candidate 2's play"</c>.</param>
    /// <param name="paramName">The member the refusal names.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="play"/> is invalid from <paramref name="start"/>; the
    /// message names the fault and the offending move, as
    /// <see cref="ApplyPlay"/>'s does.
    /// </exception>
    internal static BoardPosition PositionAfter(
        BoardPosition start, in Play play, string subject, string paramName)
    {
        Span<int> counts = stackalloc int[26];
        Span<int> reached = stackalloc int[26];
        start.CopyTo(counts);
        var fault = Reach(counts, in play, reached, out var culprit);
        if (fault != PlayFault.None)
            ThrowInvalidPlay(fault, culprit, paramName, subject, "its position");
        return BoardPosition.FromWellFormed(reached).Flipped();
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
    /// board, so no board-less comparison exists: <see cref="Play"/> has no
    /// equality, and its notation (<see cref="Play.ToNotation"/>) is display,
    /// not identity.
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
    /// Positions compare as <see cref="BoardPosition"/> values, the one
    /// definition of "the same position": all 26 counts, both bars included.
    /// Borne-off checkers need no comparison, since both plays leave from
    /// this one position.
    /// </para>
    /// <para>
    /// <b>This is not a legality check.</b> The dice, forced moves, entering
    /// from the bar before any other checker moves, and the bear-off
    /// conditions stay with the move generator (BgMoveGen): a play no roll
    /// could produce can still be valid here. What the rule guarantees is that
    /// a valid play reaches a well-formed position (<see cref="BoardPosition"/>'s
    /// invariant), and the same one however the play is written.
    /// </para>
    /// <para>
    /// <see cref="ApplyPlay"/> and <see cref="TryApplyPlay"/> apply this same
    /// rule, so applying a play never disagrees with its identity;
    /// <see cref="IndexOfSamePlay"/> is the list match built on it. This
    /// position is only read.
    /// </para>
    /// <para>
    /// <b>Stored plays are held to it too.</b> Every candidate of a
    /// <see cref="CheckerPlayDecision"/> is valid from the decision's own
    /// position: a record holding a play that cannot be played from its
    /// position is corrupt (Hal's ruling on invalid plays, applied to stored
    /// candidates, halheinrich/backgammon#273). The record cannot be built
    /// with one — its construction refuses it with an
    /// <see cref="ArgumentException"/> naming the candidate and the fault,
    /// and a document holding one is a
    /// <see cref="System.Text.Json.JsonException"/> read as a
    /// <see cref="BgDecisionData"/>, on both serialization paths — so the
    /// after-boards the record derives through this rule
    /// (<see cref="CheckerPlayDecision.AfterBestBoard"/>) can never fail to
    /// exist. This is the one statement of that invariant.
    /// </para>
    /// </remarks>
    public bool IsSamePlay(Play first, Play second)
    {
        Span<int> scratch = stackalloc int[26];
        return TryReachPosition(in first, scratch, out var target)
            && ReachesTarget(in second, in target, scratch);
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

        Span<int> scratch = stackalloc int[26];
        if (!TryReachPosition(in play, scratch, out var target))
            return -1;
        for (int i = 0; i < plays.Count; i++)
        {
            var candidate = plays[i];
            if (ReachesTarget(in candidate, in target, scratch))
                return i;
        }
        return -1;
    }

    /// <summary>
    /// The identity comparison, single-sourced for <see cref="IsSamePlay"/>
    /// and <see cref="IndexOfSamePlay"/>: whether <paramref name="play"/> is
    /// valid from this position and reaches <paramref name="target"/>, a
    /// position a valid play reaches from here — compared by
    /// <see cref="BoardPosition"/>'s equality, never here.
    /// <paramref name="scratch"/> is working space.
    /// </summary>
    private bool ReachesTarget(in Play play, in BoardPosition target, Span<int> scratch) =>
        TryReachPosition(in play, scratch, out var reached) && reached == target;

    /// <summary>
    /// The position <paramref name="play"/> reaches from this one by the play
    /// rule, as a value in the mover's frame (unflipped), or false when the
    /// play is invalid. <paramref name="scratch"/> is working space. The
    /// reached board is well-formed because this one is and the rule keeps
    /// it so, which is what lets the value skip its outside-data check.
    /// </summary>
    private bool TryReachPosition(in Play play, Span<int> scratch, out BoardPosition reached)
    {
        if (Reach(_points, in play, scratch, out _) != PlayFault.None)
        {
            reached = default;
            return false;
        }
        reached = BoardPosition.FromWellFormed(scratch);
        return true;
    }

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
    /// The refusal of an invalid play, out of line and single-sourced for
    /// <see cref="ApplyPlay"/> and <see cref="PositionAfter"/>: the message
    /// names the fault and the offending move, for a caller holding a play it
    /// believed valid. <paramref name="subject"/> and <paramref name="from"/>
    /// say which play and which position, in the caller's words.
    /// </summary>
    [DoesNotReturn]
    private static void ThrowInvalidPlay(
        PlayFault fault, Move culprit, string paramName, string subject, string from)
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
            $"{subject} is invalid from {from}: {reason}. See BoardState.IsSamePlay for the rule.",
            paramName);
    }

    /// <summary>
    /// Flip perspective in place, by <see cref="BoardPosition.Flipped"/> —
    /// the one statement of the rule (points mirror, bars swap, signs
    /// invert); this board states none of its own. Implementation mechanics
    /// for <see cref="FlippedCopy"/> — never exposed publicly. Callers should
    /// always reason in on-roll POV: <see cref="ApplyPlay(Play)"/> and
    /// <see cref="TryApplyPlay(Play)"/> flip atomically with the move
    /// application (setting the board to the reached position's flip in one
    /// write), and <see cref="FlippedCopy"/> flips a copy for other-frame
    /// queries.
    ///
    /// Borne-off counts are not tracked on <see cref="BoardState"/> (checkers
    /// simply leave the board), so nothing else needs swapping.
    /// </summary>
    private void Flip() => SetPosition(ToPosition().Flipped());

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
                int n = _points[i];
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
                int n = _points[i];
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
                int n = _points[i];
                if (n > 0) { if (i > maxOnRoll) maxOnRoll = i; }
                else if (n < 0) { if (i < minOpponent) minOpponent = i; }
            }
            return maxOnRoll < minOpponent;
        }
    }
}
