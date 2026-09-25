using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Play identity by resulting position (halheinrich/backgammon#273,
/// halheinrich/backgammon#277): <see cref="BoardState.IsSamePlay"/>, the list
/// match <see cref="BoardState.IndexOfSamePlay"/>, and the one play rule behind
/// them and behind <see cref="BoardState.ApplyPlay"/> /
/// <see cref="BoardState.TryApplyPlay"/>. Every fixture states its position.
/// </summary>
public class PlayIdentityTests
{
    // ── Fixtures ──────────────────────────────────────────────────

    /// <summary>
    /// The tester's position (halheinrich/backgammon#273): Shimodaira-Suzuki,
    /// 2010 Japan Open Final, game 4, move 33, as the decision's Mop. The mover
    /// has 8(2) and 7(1); the opponent has a blot on the 3-point and one checker
    /// on the bar. Dice 5-4.
    /// </summary>
    private static readonly int[] TesterMop =
        [-1, 0, 2, -1, 2, 2, 4, 1, 2, -1, 0, 1, -2, 0, 0, 0, -2, 0, 0, -2, -2, 1, -2, 0, -2, 0];

    private static BoardState TesterPosition() => BoardState.FromMop(TesterMop);

    /// <summary>The tester's best play as stored: 7 → 3 marked, then 8 → 3.</summary>
    private static readonly Play TesterStored = [new(7, -3), new(8, 3)];

    /// <summary>The generator's form: the mark on the highest source.</summary>
    private static readonly Play TesterGenerated = [new(8, -3), new(7, 3)];

    /// <summary>The unmarked landing written first — the order that corrupted the board.</summary>
    private static readonly Play TesterUnmarkedFirst = [new(8, 3), new(7, -3)];

    /// <summary>A position holding only the stated checkers (a pseudoboard is fine here).</summary>
    private static BoardState Position(params (int Point, int Count)[] checkers)
    {
        var mop = new int[26];
        foreach (var (point, count) in checkers)
            mop[point] = count;
        return BoardState.FromMop(mop);
    }

    /// <summary>
    /// The board <paramref name="play"/> reaches from <paramref name="start"/>,
    /// read back in the mover's frame; <paramref name="start"/> is untouched.
    /// </summary>
    private static int[] Reached(BoardState start, Play play)
    {
        var board = start.Copy();
        board.ApplyPlay(play);
        return [.. board.FlippedCopy().Points];
    }

    private static void AssertAllSamePlay(BoardState start, IReadOnlyList<Play> encodings)
    {
        for (int i = 0; i < encodings.Count; i++)
            for (int j = 0; j < encodings.Count; j++)
                Assert.True(start.IsSamePlay(encodings[i], encodings[j]),
                    $"Encodings {i} and {j} should be the same play.");
    }

    // ── The tester's case ─────────────────────────────────────────

    [Fact]
    public void ApplyPlay_TesterCase_UnmarkedLandingWrittenFirst_ReachesTheHittingBoard()
    {
        // The reproduction: before ApplyPlay went through the play rule, the
        // unmarked 8 → 3 cancelled the blot to zero and the marked 7 → 3 then
        // set the point to one checker, losing one of the mover's checkers.
        var board = TesterPosition();

        board.ApplyPlay(TesterUnmarkedFirst);
        var mover = board.FlippedCopy();

        Assert.Equal(2, mover.Points[3]);
        Assert.Equal(1, mover.Points[8]);
        Assert.Equal(0, mover.Points[7]);
        Assert.Equal(-2, mover.Points[0]);
    }

    [Fact]
    public void IsSamePlay_TesterCase_AllThreeOrders_AreTheSamePlay()
    {
        // Also the rewrite of PlayTests.Equals_PointMadeOnBlot_HitAttributionIgnored
        // (its two attributions are the stored and generator's forms): one
        // blot, hit once, one resulting position — one play.
        AssertAllSamePlay(TesterPosition(), [TesterStored, TesterGenerated, TesterUnmarkedFirst]);
    }

    [Theory]
    [InlineData(7, -3, 8, 3)]    // stored
    [InlineData(8, -3, 7, 3)]    // generator's form
    [InlineData(8, 3, 7, -3)]    // unmarked landing first
    public void ApplyPlay_TesterCase_EachOrder_ReachesTheSameBoard(int fr0, int to0, int fr1, int to1)
    {
        Play play = [new(fr0, to0), new(fr1, to1)];

        var reached = Reached(TesterPosition(), play);

        // Two of the mover's checkers on the 3-point, one left on 8, none on
        // 7, and two opponent checkers on the bar; nothing else moves.
        int[] expected = [.. TesterMop];
        expected[3] = 2;
        expected[8] = 1;
        expected[7] = 0;
        expected[0] = -2;
        Assert.Equal(expected, reached);
    }

    // ── Pairing (halheinrich/backgammon#277) ──────────────────────

    [Fact]
    public void IsSamePlay_Pairing_FiveFive_HitAttributedEitherPairing()
    {
        // 5-5, the mover on the bar and on 20, an opposing blot on 15. The
        // multi-die moves pair sources with destinations two ways, and a
        // single-die spelling reaches the same board a third way.
        var start = Position((25, 1), (20, 1), (15, -1));
        Play barToTen = [new(25, 10), new(20, -15)];
        Play barToFifteen = [new(25, -15), new(20, 10)];
        Play singleDie = [new(25, 20), new(20, -15), new(15, 10), new(20, 15)];

        AssertAllSamePlay(start, [barToTen, barToFifteen, singleDie]);
    }

    [Fact]
    public void IsSamePlay_Pairing_FourFour_NoBlotInTheWay()
    {
        var start = Position((13, 1), (9, 1));
        Play thirteenToFive = [new(13, 5), new(9, 1)];
        Play thirteenToOne = [new(13, 1), new(9, 5)];

        Assert.True(start.IsSamePlay(thirteenToFive, thirteenToOne));
    }

    // ── Move order and decomposition ──────────────────────────────

    [Fact]
    public void IsSamePlay_Decomposition_OneHopTwoHopsAndReversedHops()
    {
        // 18 holds no blot (it is empty), so 24/13 may be written as one
        // move, as the checker's two hops, or with the second hop written
        // first — a hop may start where another hop of the play arrives.
        var start = Position((24, 2), (13, 3), (6, 5));
        Play oneHop = [new(24, 13)];
        Play twoHops = [new(24, 18), new(18, 13)];
        Play reversed = [new(18, 13), new(24, 18)];

        AssertAllSamePlay(start, [oneHop, twoHops, reversed]);
    }

    [Fact]
    public void IsSamePlay_MoveOrder_Irrelevant()
    {
        // Rewritten from PlayTests.Equals_AndHashCode_AreOrderInvariant.
        var start = BoardState.Standard();
        Play written = [new(13, 7), new(8, 5)];
        Play reordered = [new(8, 5), new(13, 7)];

        Assert.True(start.IsSamePlay(written, reordered));
    }

    [Fact]
    public void IsSamePlay_DecomposedEntry_MatchesCombinedEncoding()
    {
        // Rewritten from PlayTests.Equals_DecomposedEntry_MatchesCombinedEncoding.
        // The quiz-entry repro: a user enters 13/8 as two clicks (13/10, then
        // 10/8); the candidate list stores the collapsed encoding {(13,8)}.
        // From the opening position, 10 is empty and both reach one board.
        var start = BoardState.Standard();
        Play decomposed = [new(13, 10), new(10, 8)];
        Play combined = [new(13, 8)];

        Assert.True(start.IsSamePlay(decomposed, combined));
    }

    [Fact]
    public void IsSamePlay_HitAtFinalPoint_MatchesAcrossDecompositions()
    {
        // Rewritten from PlayTests.Equals_HitAtFinalPoint_MatchesAcrossDecompositions.
        // A blot on 8, 10 empty: 13/10 10/8* and the combined 13/8* reach one
        // board.
        var start = Position((13, 2), (8, -1), (6, 5));
        Play decomposed = [new(13, 10), new(10, -8)];
        Play combined = [new(13, -8)];

        Assert.True(start.IsSamePlay(decomposed, combined));
    }

    [Fact]
    public void IsSamePlay_BearOff_DirectAndDecomposed()
    {
        // 5/off with one die, or 5/2 2/off: the same checker leaves.
        var start = Position((6, 1), (5, 2), (1, -2));
        Play direct = [new(5, 0), new(6, 3)];
        Play decomposed = [new(5, 2), new(2, 0), new(6, 3)];

        Assert.True(start.IsSamePlay(direct, decomposed));

        // A borne-off checker leaves the board: nothing lands on either bar.
        var reached = Reached(start, direct);
        Assert.Equal(0, reached[0]);
        Assert.Equal(0, reached[25]);
        Assert.Equal(1, reached[5]);
        Assert.Equal(1, reached[3]);
        Assert.Equal(0, reached[6]);
    }

    [Fact]
    public void IsSamePlay_EmptyPlays_Same_AndDistinctFromNonEmpty()
    {
        // Rewritten from PlayTests.Equals_EmptyPlays_Equal_AndDistinctFromNonEmpty.
        var start = BoardState.Standard();
        Play e1 = [];
        Play e2 = [];

        Assert.True(start.IsSamePlay(e1, e2));
        Assert.False(start.IsSamePlay(e1, [new(13, 7)]));
    }

    [Fact]
    public void IsSamePlay_StaleBufferSlots_DoNotLeakIntoIdentity()
    {
        // Rewritten from PlayTests.Equals_StaleBufferSlots_DoNotLeakIntoEquality
        // (its storage half is PlayTests.IsSameEncoding_StaleBufferSlots_AreNotPartOfTheEncoding).
        // RemoveLast leaves the popped move in the buffer; the rule reads only
        // the first Count moves.
        var start = BoardState.Standard();
        var trimmed = new Play();
        trimmed.Add(new Move(13, 7));
        trimmed.Add(new Move(8, 5));
        trimmed.RemoveLast();

        Assert.True(start.IsSamePlay(trimmed, [new(13, 7)]));
        Assert.False(start.IsSamePlay(trimmed, [new(13, 7), new(8, 5)]));
    }

    // ── Controls: different positions, different plays ───────────

    [Fact]
    public void IsSamePlay_HitOnIntermediatePoint_DistinctFromNonHitting()
    {
        // Rewritten from PlayTests.Equals_HitOnIntermediatePoint_DistinctFromNonHitting.
        // A blot on 10: 13/10*/8 hits it on the way, 13/8 passes over it —
        // different boards, so different plays. (The old hit-stripped
        // DeduplicationKey compared them equal and let a hit-less encoding of
        // the hitting play apply without barring the blot.)
        var start = Position((13, 2), (10, -1), (6, 5));
        Play hitting = [new(13, -10), new(10, 8)];
        Play passing = [new(13, 8)];

        Assert.False(start.IsSamePlay(hitting, passing));
        Assert.Equal(-1, Reached(start, hitting)[0]);
        Assert.Equal(0, Reached(start, passing)[0]);
    }

    [Fact]
    public void IsSamePlay_PointMadeOnBlot_VsSameMovesWithoutHit()
    {
        // Rewritten from PlayTests.Equals_PointMadeOnBlot_VsSameMovesWithoutHit_NotEqual.
        // The board decides whether the point is hit, and an encoding that
        // disagrees with it is invalid. Over the tester's blot the quiet
        // encoding is invalid; on an empty 3-point the hitting one is.
        Play hitting = [new(8, -3), new(7, 3)];
        Play quiet = [new(8, 3), new(7, 3)];
        var overBlot = TesterPosition();
        var overEmpty = Position((8, 2), (7, 1), (6, 4));

        Assert.False(overBlot.IsSamePlay(hitting, quiet));
        Assert.True(overBlot.IsSamePlay(hitting, hitting));
        Assert.False(overBlot.IsSamePlay(quiet, quiet));

        Assert.False(overEmpty.IsSamePlay(hitting, quiet));
        Assert.True(overEmpty.IsSamePlay(quiet, quiet));
        Assert.False(overEmpty.IsSamePlay(hitting, hitting));
    }

    [Fact]
    public void IsSamePlay_HitsOnTwoPoints_VsHitOnOne()
    {
        // Rewritten from PlayTests.Equals_HitsOnTwoPoints_VsHitOnOne_NotEqual.
        // 5-4 from the 13-point over blots on 9 and 8: both points are hit,
        // so an encoding marking only one of them is invalid and is not the
        // same play as the one marking both.
        var start = Position((13, 2), (9, -1), (8, -1), (6, 5));
        Play both = [new(13, -9), new(13, -8)];
        Play nineOnly = [new(13, -9), new(13, 8)];
        Play eightOnly = [new(13, 9), new(13, -8)];

        Assert.True(start.IsSamePlay(both, both));
        Assert.False(start.IsSamePlay(both, nineOnly));
        Assert.False(start.IsSamePlay(both, eightOnly));
        Assert.False(start.IsSamePlay(nineOnly, eightOnly));
    }

    [Fact]
    public void IsSamePlay_HitVsNonHit_SameTrajectory()
    {
        // Rewritten from PlayTests.Equals_HitVsNonHit_SameTrajectory_NotEqual.
        // From any one position exactly one of 13/7* and 13/7 is valid, so
        // they are never the same play.
        Play hit = [new(13, -7)];
        Play noHit = [new(13, 7)];
        var blotOnSeven = Position((13, 2), (7, -1), (6, 5));
        var emptySeven = Position((13, 2), (6, 5));

        Assert.False(blotOnSeven.IsSamePlay(hit, noHit));
        Assert.True(blotOnSeven.IsSamePlay(hit, hit));
        Assert.False(emptySeven.IsSamePlay(hit, noHit));
        Assert.True(emptySeven.IsSamePlay(noHit, noHit));
    }

    [Fact]
    public void IsSamePlay_TwoHits_DifferFromOne()
    {
        // 5-3 with blots on 10 and 5: 13/10*/5* hits both, 13/8/5* only one.
        var start = Position((13, 2), (10, -1), (5, -1), (6, 5));
        Play twoHits = [new(13, -10), new(10, -5)];
        Play oneHit = [new(13, 8), new(8, -5)];

        Assert.False(start.IsSamePlay(twoHits, oneHit));
        Assert.Equal(-2, Reached(start, twoHits)[0]);
        Assert.Equal(-1, Reached(start, oneHit)[0]);
    }

    [Fact]
    public void IsSamePlay_DifferentPlays_NotTheSamePlay()
    {
        // Rewritten from PlayTests.Equals_DifferentPlays_NotEqual.
        var start = BoardState.Standard();

        Assert.False(start.IsSamePlay([new(13, 7)], [new(13, 5)]));
    }

    // ── Invalid plays: one case each ──────────────────────────────

    /// <summary>
    /// Each invalid case: its position, the invalid play, and the valid play
    /// it would be mistaken for were its fault not caught.
    /// </summary>
    private static (BoardState Start, Play Invalid, Play Neighbour) InvalidCase(string name) => name switch
    {
        // A mark on an empty point: 13/10*/8 with 10 empty.
        "MarkOnEmptyPoint" => (Position((13, 2), (6, 5)),
            [new(13, -10), new(10, 8)], [new(13, 8)]),
        // A mark on the mover's own point.
        "MarkOnOwnPoint" => (Position((13, 2), (10, 2), (6, 5)),
            [new(13, -10)], [new(13, 10)]),
        // An unmarked landing on a blot: 13/10 over an opposing blot on 10.
        "UnmarkedLandingOnBlot" => (Position((13, 2), (10, -1), (6, 5)),
            [new(13, 10)], [new(13, -10)]),
        // An unmarked intermediate landing on a blot: 13/10/8.
        "UnmarkedIntermediateLandingOnBlot" => (Position((13, 2), (10, -1), (6, 5)),
            [new(13, 10), new(10, 8)], [new(13, 8)]),
        // A hop from a point without one of the mover's checkers.
        "HopFromEmptyPoint" => (Position((13, 2), (6, 5)),
            [new(14, 10)], [new(13, 9)]),
        // A hop from a point the opponent holds.
        "HopFromOpposingPoint" => (Position((13, 2), (14, -2), (6, 5)),
            [new(14, 10)], [new(13, 9)]),
        // A second hop from a point holding one checker.
        "TwoHopsFromOneChecker" => (Position((13, 1), (6, 5)),
            [new(13, 10), new(13, 8)], [new(13, 10)]),
        // A landing on a made point.
        "LandingOnMadePoint" => (Position((13, 2), (12, -2), (6, 5)),
            [new(13, 12)], [new(13, 11)]),
        // A marked landing on a made point.
        "MarkedLandingOnMadePoint" => (Position((13, 2), (12, -2), (6, 5)),
            [new(13, -12)], [new(13, 11)]),
        // A hop away from home.
        "BackwardHop" => (Position((13, 2), (6, 5)),
            [new(6, 9)], [new(6, 3)]),
        // A hop that does not move.
        "StationaryHop" => (Position((13, 2), (6, 5)),
            [new(6, 6)], []),
        // Encodings off the board.
        "SourceOffTheBoard" => (Position((13, 2), (6, 5)),
            [new(26, 20)], [new(13, 7)]),
        "SourceOnOpponentsBar" => (Position((13, 2), (0, -1), (6, 5)),
            [new(0, 0)], [new(6, 0)]),
        "DestinationOffTheBoard" => (Position((13, 2), (6, 5)),
            [new(6, -25)], [new(6, 0)]),
        "DestinationMinValue" => (Position((13, 2), (6, 5)),
            [new(6, int.MinValue)], [new(6, 0)]),
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, null),
    };

    public static TheoryData<string> InvalidCaseNames =>
    [
        "MarkOnEmptyPoint", "MarkOnOwnPoint",
        "UnmarkedLandingOnBlot", "UnmarkedIntermediateLandingOnBlot",
        "HopFromEmptyPoint", "HopFromOpposingPoint", "TwoHopsFromOneChecker",
        "LandingOnMadePoint", "MarkedLandingOnMadePoint",
        "BackwardHop", "StationaryHop",
        "SourceOffTheBoard", "SourceOnOpponentsBar", "DestinationOffTheBoard", "DestinationMinValue",
    ];

    [Theory]
    [MemberData(nameof(InvalidCaseNames))]
    public void InvalidPlay_MatchesNothing(string name)
    {
        var (start, invalid, neighbour) = InvalidCase(name);

        // The neighbour is valid, so a failure below is the invalid play's.
        Assert.True(start.IsSamePlay(neighbour, neighbour));

        Assert.False(start.IsSamePlay(invalid, invalid));
        Assert.False(start.IsSamePlay(invalid, neighbour));
        Assert.False(start.IsSamePlay(neighbour, invalid));
        Assert.Equal(-1, start.IndexOfSamePlay(invalid, [invalid, neighbour]));
    }

    [Theory]
    [MemberData(nameof(InvalidCaseNames))]
    public void InvalidPlay_ApplyPlayRefuses_BoardUnchanged(string name)
    {
        var (start, invalid, _) = InvalidCase(name);
        int[] before = [.. start.Points];
        int highBefore = start.HighPointOccupied;

        Assert.Throws<ArgumentException>("play", () => start.ApplyPlay(invalid));
        Assert.Equal(before, start.Points);
        Assert.Equal(highBefore, start.HighPointOccupied);

        Assert.False(start.TryApplyPlay(invalid));
        Assert.Equal(before, start.Points);
        Assert.Equal(highBefore, start.HighPointOccupied);
    }

    [Fact]
    public void IsSamePlay_LeavesThePositionUntouched()
    {
        var start = TesterPosition();

        start.IsSamePlay(TesterStored, TesterUnmarkedFirst);
        start.IndexOfSamePlay(TesterUnmarkedFirst, [TesterStored]);

        Assert.Equal(TesterMop, start.Points);
    }

    [Fact]
    public void TryApplyPlay_ValidPlay_AppliesAndFlipsAsApplyPlayDoes()
    {
        var viaTry = TesterPosition();
        var viaApply = TesterPosition();

        Assert.True(viaTry.TryApplyPlay(TesterUnmarkedFirst));
        viaApply.ApplyPlay(TesterUnmarkedFirst);

        Assert.Equal(viaApply.Points, viaTry.Points);
        Assert.Equal(viaApply.HighPointOccupied, viaTry.HighPointOccupied);
    }

    // ── Doubles: several checkers on a hit point ──────────────────

    /// <summary>
    /// <paramref name="moves"/> with the landing on <paramref name="point"/>
    /// marked at each index whose bit is set in <paramref name="marks"/>.
    /// </summary>
    private static Play Marked(Move[] moves, int point, int marks)
    {
        var play = new Play();
        int bit = 0;
        foreach (var move in moves)
        {
            bool landsThere = move.ToPt == point;
            play.Add(landsThere && (marks & (1 << bit++)) != 0 ? move with { ToPt = -point } : move);
        }
        return play;
    }

    [Fact]
    public void Doubles_ThreeCheckersOnAHitPoint_EveryAttributionTheSamePlay()
    {
        // 2-2 with a blot on 4: 8/4 and two checkers 6/4, as a multi-die move
        // or all single-die hops, in two orders; every non-empty set of the
        // landings on 4 may carry the mark.
        var start = Position((8, 1), (6, 2), (4, -1), (13, 2));
        Move[][] spellings =
        [
            [new(8, 4), new(6, 4), new(6, 4)],
            [new(8, 6), new(6, 4), new(6, 4), new(6, 4)],
            [new(6, 4), new(6, 4), new(8, 6), new(6, 4)],
        ];

        var encodings = new List<Play>();
        foreach (var moves in spellings)
            for (int marks = 1; marks < 8; marks++)
                encodings.Add(Marked(moves, 4, marks));

        AssertAllSamePlay(start, encodings);
        var reached = Reached(start, encodings[0]);
        Assert.Equal(3, reached[4]);
        Assert.Equal(-1, reached[0]);
    }

    [Fact]
    public void Doubles_FourCheckersOnAHitPoint_EveryAttributionTheSamePlay()
    {
        // 2-2, four checkers 6/4 onto a blot; every non-empty set of marks.
        var start = Position((6, 4), (4, -1), (13, 2));
        Move[] moves = [new(6, 4), new(6, 4), new(6, 4), new(6, 4)];

        var encodings = new List<Play>();
        for (int marks = 1; marks < 16; marks++)
            encodings.Add(Marked(moves, 4, marks));

        AssertAllSamePlay(start, encodings);
        var reached = Reached(start, encodings[0]);
        Assert.Equal(4, reached[4]);
        Assert.Equal(0, reached[6]);
        Assert.Equal(-1, reached[0]);
    }

    [Fact]
    public void Doubles_UnmarkedOnAHitPoint_InvalidInEveryOrder()
    {
        // The same three checkers with no mark at all: the blot is landed on
        // unmarked, so every spelling is invalid.
        var start = Position((8, 1), (6, 2), (4, -1), (13, 2));
        Play quiet = [new(8, 4), new(6, 4), new(6, 4)];

        Assert.False(start.IsSamePlay(quiet, quiet));
        Assert.False(start.TryApplyPlay(quiet));
    }

    // ── The list match ────────────────────────────────────────────

    /// <summary>Candidates in the tester's position, the best play in its stored form.</summary>
    private static readonly Play[] TesterCandidates =
    [
        [new(11, 6), new(7, -3)],
        TesterStored,
        [new(6, 1), new(5, 1)],
        [new(8, 4), new(8, -3)],
    ];

    [Fact]
    public void IndexOfSamePlay_FindsTheListedPlay_WhateverItsNotation()
    {
        var start = TesterPosition();

        Assert.Equal(1, start.IndexOfSamePlay(TesterStored, TesterCandidates));
        Assert.Equal(1, start.IndexOfSamePlay(TesterGenerated, TesterCandidates));
        Assert.Equal(1, start.IndexOfSamePlay(TesterUnmarkedFirst, TesterCandidates));
        Assert.Equal(2, start.IndexOfSamePlay([new(5, 1), new(6, 1)], TesterCandidates));
    }

    [Fact]
    public void IndexOfSamePlay_OffListPlay_FindsNothing()
    {
        var start = TesterPosition();
        Play offList = [new(6, 2), new(6, 1)];

        // Valid, so the miss is the list's, not the play's.
        Assert.True(start.IsSamePlay(offList, offList));
        Assert.Equal(-1, start.IndexOfSamePlay(offList, TesterCandidates));
    }

    [Fact]
    public void IndexOfSamePlay_EmptyList_FindsNothing()
    {
        Assert.Equal(-1, TesterPosition().IndexOfSamePlay(TesterStored, []));
    }

    [Fact]
    public void IndexOfSamePlay_InvalidPlay_FindsNothing_EvenWhenListed()
    {
        // Both landings on the blot unmarked: invalid, though the identical
        // encoding is in the list.
        Play invalid = [new(8, 3), new(7, 3)];

        Assert.Equal(-1, TesterPosition().IndexOfSamePlay(invalid, [invalid, TesterStored]));
    }

    [Fact]
    public void IndexOfSamePlay_InvalidEntry_IsPassedOver()
    {
        // An invalid entry reaches no position, so it matches nothing — not
        // even a valid play it would reach were its fault ignored.
        Play invalid = [new(8, 3), new(7, 3)];
        Play[] list = [invalid, TesterStored];

        Assert.Equal(1, TesterPosition().IndexOfSamePlay(TesterGenerated, list));
    }

    [Fact]
    public void IndexOfSamePlay_TwoEntriesTheSamePlay_LowestIndexWins()
    {
        var start = TesterPosition();
        Play[] list = [[new(6, 1), new(5, 1)], TesterStored, TesterGenerated];
        Play[] reversed = [[new(6, 1), new(5, 1)], TesterGenerated, TesterStored];

        Assert.Equal(1, start.IndexOfSamePlay(TesterUnmarkedFirst, list));
        Assert.Equal(1, start.IndexOfSamePlay(TesterUnmarkedFirst, reversed));
    }

    [Fact]
    public void IndexOfSamePlay_NullList_Throws()
    {
        Assert.Throws<ArgumentNullException>("plays",
            () => TesterPosition().IndexOfSamePlay(TesterStored, null!));
    }
}
