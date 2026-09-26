using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

public class BoardStateTests
{
    // ── Factories ─────────────────────────────────────────────────

    [Fact]
    public void Standard_HasCorrectCheckerLayout()
    {
        var s = BoardState.Standard();

        Assert.Equal(5, s.Points[6]);
        Assert.Equal(3, s.Points[8]);
        Assert.Equal(5, s.Points[13]);
        Assert.Equal(2, s.Points[24]);
        Assert.Equal(-5, s.Points[19]);
        Assert.Equal(-3, s.Points[17]);
        Assert.Equal(-5, s.Points[12]);
        Assert.Equal(-2, s.Points[1]);
        Assert.Equal(0, s.Points[0]);
        Assert.Equal(0, s.Points[25]);
    }

    [Fact]
    public void Standard_OnRollAndOpponentEachHave15Checkers()
    {
        var s = BoardState.Standard();

        int onRoll = 0, opp = 0;
        for (int i = 0; i <= 25; i++)
        {
            if (s.Points[i] > 0) onRoll += s.Points[i];
            else if (s.Points[i] < 0) opp -= s.Points[i];
        }

        Assert.Equal(15, onRoll);
        Assert.Equal(15, opp);
    }

    [Fact]
    public void Standard_HighPointOccupiedIs24()
    {
        Assert.Equal(24, BoardState.Standard().HighPointOccupied);
    }

    [Fact]
    public void Nackgammon_HighPointOccupiedIs24()
    {
        var s = BoardState.Nackgammon();

        Assert.Equal(4, s.Points[6]);
        Assert.Equal(2, s.Points[23]);
        Assert.Equal(2, s.Points[24]);
        Assert.Equal(-2, s.Points[2]);
        Assert.Equal(24, s.HighPointOccupied);
    }

    [Fact]
    public void Bg960_SameSeed_ProducesIdenticalBoards()
    {
        var a = BoardState.Bg960(seed: 42);
        var b = BoardState.Bg960(seed: 42);

        Assert.Equal(a.Points, b.Points);
        Assert.Equal(a.HighPointOccupied, b.HighPointOccupied);
    }

    [Fact]
    public void Bg960_PreservesCheckerConservation()
    {
        var s = BoardState.Bg960(seed: 7);

        int onRoll = 0, opp = 0;
        for (int i = 0; i <= 25; i++)
        {
            if (s.Points[i] > 0) onRoll += s.Points[i];
            else if (s.Points[i] < 0) opp -= s.Points[i];
        }

        Assert.Equal(15, onRoll);
        Assert.Equal(15, opp);
    }

    [Fact]
    public void Bg960_PlayerOpponentSymmetry()
    {
        for (int seed = 1; seed <= 50; seed++)
        {
            var s = BoardState.Bg960(seed: seed);
            for (int i = 1; i <= 24; i++)
            {
                int mirror = 25 - i;
                int playerHere = Math.Max(0, s.Points[i]);
                int opponentMirror = Math.Max(0, -s.Points[mirror]);
                Assert.True(
                    playerHere == opponentMirror,
                    $"Seed {seed}: asymmetry at point {i}: player={playerHere}, opponent at mirror {mirror}={opponentMirror}");
            }
        }
    }

    [Fact]
    public void Bg960_AllOccupiedPointsHaveAtLeast2Checkers()
    {
        for (int seed = 1; seed <= 50; seed++)
        {
            var s = BoardState.Bg960(seed: seed);
            for (int i = 1; i <= 24; i++)
            {
                if (s.Points[i] > 0)
                {
                    Assert.True(
                        s.Points[i] >= 2,
                        $"Seed {seed}: blot at point {i} (count={s.Points[i]})");
                }
            }
        }
    }

    [Fact]
    public void Bg960_AllQuadrantsHaveAtLeastOneOccupiedPoint()
    {
        var quadrants = new[]
        {
            (from: 1,  to: 6),
            (from: 7,  to: 12),
            (from: 13, to: 18),
            (from: 19, to: 24),
        };
        for (int seed = 1; seed <= 50; seed++)
        {
            var s = BoardState.Bg960(seed: seed);
            foreach (var (from, to) in quadrants)
            {
                bool hasOccupied = false;
                for (int i = from; i <= to; i++)
                {
                    if (s.Points[i] > 0)
                    {
                        hasOccupied = true;
                        break;
                    }
                }
                Assert.True(
                    hasOccupied,
                    $"Seed {seed}: quadrant {from}-{to} has no player checkers");
            }
        }
    }

    [Fact]
    public void StartingFactories_HoldTheStartingPositionValues()
    {
        // The layouts are defined once, on the value; each factory is a
        // board built from it.
        Assert.Equal(BoardPosition.Standard, BoardState.Standard().ToPosition());
        Assert.Equal(BoardPosition.Nackgammon, BoardState.Nackgammon().ToPosition());
        Assert.Equal(BoardPosition.Bg960(seed: 42), BoardState.Bg960(seed: 42).ToPosition());
    }

    // ── Construction from a position value ────────────────────────

    public static TheoryData<string, int[]> Positions => new()
    {
        { "standard", [0, -2, 0, 0, 0, 0, 5, 0, 3, 0, 0, 0, -5, 5, 0, 0, 0, -3, 0, -5, 0, 0, 0, 0, 2, 0] },
        { "both bars", [-1, 0, 2, -1, 2, 2, 2, 1, 2, -1, 0, 1, -2, 0, 0, 0, -2, 0, 0, -2, -2, 1, -2, 0, -1, 1] },
        { "bearing off", [0, 3, 2, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -4, 0, 0, -2, 0, 0, 0] },
        { "empty", new int[26] },
    };

    [Theory]
    [MemberData(nameof(Positions))]
    public void Constructor_HoldsThePosition(string name, int[] counts)
    {
        var position = new BoardPosition(counts);

        var s = new BoardState(position);

        Assert.Equal(position, s.ToPosition());
        Assert.True(s.Points.SequenceEqual(counts), name);
    }

    [Theory]
    [MemberData(nameof(Positions))]
    public void Constructor_ComputesHighPointOccupied(string name, int[] counts)
    {
        int expected = 0;
        for (int i = 25; i >= 1 && expected == 0; i--)
            if (counts[i] > 0) expected = i;

        Assert.True(expected == new BoardState(new BoardPosition(counts)).HighPointOccupied, name);
    }

    // ── Snapshot ──────────────────────────────────────────────────

    [Fact]
    public void ToPosition_IsASnapshot_LaterChangesDoNotReachIt()
    {
        var s = BoardState.Standard();
        var snapshot = s.ToPosition();

        s.ApplyMove(new Move(13, 7));
        var afterMove = s.ToPosition();
        s.ApplyPlay([new(24, 18)]);

        Assert.Equal(BoardPosition.Standard, snapshot);
        Assert.Equal(5, snapshot[13]);
        Assert.Equal(0, snapshot[7]);
        Assert.Equal(4, afterMove[13]);
        Assert.Equal(1, afterMove[7]);
        Assert.NotEqual(afterMove, s.ToPosition());
    }

    [Fact]
    public void ToPosition_EqualBoards_EqualPositions()
    {
        // Two boards reached by different routes compare through the value.
        var viaPlay = BoardState.Standard();
        viaPlay.ApplyPlay([new(13, 10), new(10, 8)]);
        var viaOneHop = BoardState.Standard();
        viaOneHop.ApplyPlay([new(13, 8)]);

        Assert.Equal(viaOneHop.ToPosition(), viaPlay.ToPosition());
        Assert.Equal(viaOneHop.ToPosition().GetHashCode(), viaPlay.ToPosition().GetHashCode());
    }

    // ── Read-only to callers (halheinrich/backgammon#281) ─────────

    [Fact]
    public void PublicSurface_HasNoWritableState()
    {
        // A write from outside the library does not compile; this pins the
        // shape that makes it so, against a regression that re-exposes a
        // writable array or field.
        var type = typeof(BoardState);
        const System.Reflection.BindingFlags Public =
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.Static;

        Assert.Empty(type.GetFields(Public));
        Assert.All(type.GetProperties(Public), p =>
            Assert.True(p.SetMethod is null || !p.SetMethod.IsPublic, $"{p.Name} has a public setter"));
        Assert.Equal(typeof(ReadOnlySpan<int>), type.GetProperty(nameof(BoardState.Points))!.PropertyType);
        Assert.Null(type.GetConstructor(Type.EmptyTypes));
    }

    [Fact]
    public void Type_IsSealed()
    {
        // A type that guards an invariant is not open to subclasses: a
        // subclass could add state or ways in that the invariant never sees.
        Assert.True(typeof(BoardState).IsSealed);
    }

    [Fact]
    public void HotPath_ApplyUndoReadsAndSnapshot_AllocateNothing()
    {
        var s = BoardState.Standard();
        var move = new Move(13, 9);
        int sink = Churn(s, move);   // warm the paths outside the measured window

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
            sink += Churn(s, move);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
        Assert.Equal(BoardPosition.Standard, s.ToPosition());
        Assert.NotEqual(int.MinValue, sink);
    }

    private static int Churn(BoardState s, Move move)
    {
        s.ApplyMove(move);
        int read = s.Points[9] + s.Points[13] + s.HighPointOccupied;
        var snapshot = s.ToPosition();
        s.UndoMove(move);
        return read + snapshot.GetHashCode();
    }

    // ── Mop bridge ────────────────────────────────────────────────

    [Fact]
    public void FromMop_ToPosition_RoundTrip()
    {
        // Rewritten from FromMop_ToMop_RoundTrip: ToMop gave way to the
        // position snapshot.
        int[] counts = new int[26];
        BoardPosition.Standard.CopyTo(counts);

        var s = BoardState.FromMop(counts);

        Assert.Equal(BoardPosition.Standard, s.ToPosition());
        Assert.Equal(BoardState.Standard().HighPointOccupied, s.HighPointOccupied);
    }

    [Fact]
    public void FromMop_CopiesTheCounts_LaterWritesToTheSourceDoNotReachIt()
    {
        // Rewritten from ToMop_IsDefensiveCopy: the board can no longer be
        // written from outside, so the defensive copy that matters is the
        // one taken on the way in.
        int[] mop = new int[26];
        BoardPosition.Standard.CopyTo(mop);
        var s = BoardState.FromMop(mop);

        mop[6] = 0;

        Assert.Equal(5, s.Points[6]);
    }

    [Fact]
    public void FromMop_RecomputesHighPointOccupied()
    {
        var mop = new int[26];
        mop[5] = 3;
        mop[10] = 2;
        mop[1] = -5;

        var s = BoardState.FromMop(mop);

        Assert.Equal(10, s.HighPointOccupied);
    }

    [Fact]
    public void FromMop_WrongLength_Throws()
    {
        Assert.Throws<ArgumentException>("mop", () => BoardState.FromMop(new int[25]));
    }

    [Fact]
    public void FromMop_NullArray_IsNoCounts_Throws()
    {
        // Rewritten from FromMop_Null_Throws: the counts arrive as a span,
        // and a null array is an empty span — no counts, not 26.
        int[]? none = null;

        Assert.Throws<ArgumentException>("mop", () => BoardState.FromMop(none));
    }

    [Theory]
    [InlineData(0, 1)]      // an on-roll checker on the opponent's bar
    [InlineData(25, -1)]    // an opponent's checker on the on-roll bar
    [InlineData(24, 3)]     // sixteen on-roll checkers
    [InlineData(19, -6)]    // sixteen opponent's checkers
    public void FromMop_MalformedBoard_Throws(int slot, int count)
    {
        // FromMop validates through BoardPosition's one invariant; the
        // pseudoboards it once tolerated are refused.
        int[] mop = [0, -2, 0, 0, 0, 0, 5, 0, 3, 0, 0, 0, -5, 5, 0, 0, 0, -3, 0, -5, 0, 0, 0, 0, 2, 0];
        mop[slot] = count;

        Assert.Throws<ArgumentException>("mop", () => BoardState.FromMop(mop));
    }

    // Four cases ported from BgMoveGen's deleted BoardStateBridgeTests, orphaned
    // when BoardState moved to BgDataTypes_Lib. The other bridge cases from that
    // suite (Standard round-trip, defensive copy, high-point recalc, null /
    // wrong-length throws) are already pinned above and are not duplicated here.

    [Fact]
    public void FromMop_ToPosition_RoundTrip_Bg960Seeded()
    {
        // Rewritten from FromMop_ToMop_RoundTrip_Bg960Seeded.
        var s = BoardState.Bg960(seed: 42);
        int[] counts = [.. s.Points];
        var s2 = BoardState.FromMop(counts);

        Assert.Equal(s.ToPosition(), s2.ToPosition());
        Assert.Equal(s.HighPointOccupied, s2.HighPointOccupied);
    }

    [Fact]
    public void FromMop_ToPosition_RoundTrip_MidGamePosition()
    {
        // Rewritten from FromMop_ToMop_RoundTrip_MidGamePosition, which built
        // its board by raw writes that no longer compile. Hand-built
        // mid-game: both bars occupied, hit-eligible blot, partial bear-off,
        // both signs present at non-trivial counts. The player-on-bar checker
        // at [25] makes 25 the highest occupied point.
        var mop = new int[26];
        mop[0] = -1;    // opponent on bar
        mop[1] = 3;     // player home-board point
        mop[3] = -1;    // opponent blot in player's home
        mop[6] = 4;
        mop[8] = 2;
        mop[12] = -3;
        mop[13] = 2;
        mop[17] = -4;
        mop[19] = -5;
        mop[24] = 1;    // player blot on 24
        mop[25] = 1;    // player on bar

        var s = BoardState.FromMop(mop);
        var s2 = new BoardState(s.ToPosition());

        Assert.Equal(mop, s.Points);
        Assert.Equal(s.ToPosition(), s2.ToPosition());
        Assert.Equal(s.HighPointOccupied, s2.HighPointOccupied);
        Assert.Equal(25, s2.HighPointOccupied);
    }

    [Fact]
    public void FromMop_RecomputesHighPointOccupied_BarOccupied()
    {
        // Bar (index 25) is the highest possible occupied point — recalc must
        // reach it, not stop at the highest playing-surface point.
        var mop = new int[26];
        mop[6] = 5;
        mop[25] = 1;   // player on bar

        var s = BoardState.FromMop(mop);

        Assert.Equal(25, s.HighPointOccupied);
    }

    [Fact]
    public void FromMop_AcceptsTheEmptyBoard()
    {
        // Rewritten from FromMop_AcceptsPseudoboard_AllZero: the empty board
        // is well-formed (no checkers on the board), not a tolerated
        // pseudoboard — FromMop now validates, and this board passes.
        var s = BoardState.FromMop(new int[26]);

        Assert.Equal(0, s.HighPointOccupied);
        Assert.Equal(BoardPosition.Empty, s.ToPosition());
    }

    // ── Copy ──────────────────────────────────────────────────────

    [Fact]
    public void Copy_IsDeep()
    {
        // Rewritten: the copy is changed through the apply path, since a raw
        // write no longer compiles.
        var s = BoardState.FromMop(
            [-1, 0, 2, -1, 2, 2, 2, 1, 2, -1, 0, 1, -2, 0, 0, 0, -2, 0, 0, -2, -2, 1, -2, 0, -1, 0]);
        var c = s.Copy();

        Assert.Equal(s.ToPosition(), c.ToPosition());
        Assert.Equal(21, c.HighPointOccupied);

        c.ApplyMove(new Move(6, 1));

        Assert.Equal(1, c.Points[6]);
        Assert.Equal(2, s.Points[6]);
        Assert.NotEqual(s.ToPosition(), c.ToPosition());
    }

    // ── ApplyMove / UndoMove round-trip ───────────────────────────

    [Fact]
    public void ApplyMove_UndoMove_RegularMove_RoundTrips()
    {
        var s = BoardState.Standard();
        int[] before = [.. s.Points];
        int highBefore = s.HighPointOccupied;

        var move = new Move(13, 9);
        s.ApplyMove(move);
        Assert.Equal(4, s.Points[13]);
        Assert.Equal(1, s.Points[9]);

        s.UndoMove(move);
        Assert.Equal(before, s.Points);
        Assert.Equal(highBefore, s.HighPointOccupied);
    }

    [Fact]
    public void ApplyMove_UndoMove_Hit_RoundTrips()
    {
        // Construct a position where on-roll can hit an opponent blot.
        var mop = new int[26];
        mop[13] = 1;   // on-roll blot we move from
        mop[7] = -1;   // opponent blot — target of the hit
        var s = BoardState.FromMop(mop);
        int[] before = [.. s.Points];
        int highBefore = s.HighPointOccupied;

        var hit = new Move(13, -7);
        s.ApplyMove(hit);
        Assert.Equal(0, s.Points[13]);
        Assert.Equal(1, s.Points[7]);
        Assert.Equal(-1, s.Points[0]);   // hit checker on opponent's bar

        s.UndoMove(hit);
        Assert.Equal(before, s.Points);
        Assert.Equal(highBefore, s.HighPointOccupied);
    }

    [Fact]
    public void ApplyMove_UndoMove_BearOff_RoundTrips()
    {
        // Bear-off-eligible position: HighPointOccupied <= 6.
        var mop = new int[26];
        mop[6] = 1;
        mop[5] = 2;
        var s = BoardState.FromMop(mop);
        Assert.Equal(6, s.HighPointOccupied);
        int[] before = [.. s.Points];

        var bear = new Move(6, 0);
        s.ApplyMove(bear);
        Assert.Equal(0, s.Points[6]);
        Assert.Equal(5, s.HighPointOccupied);

        s.UndoMove(bear);
        Assert.Equal(before, s.Points);
        Assert.Equal(6, s.HighPointOccupied);
    }

    [Fact]
    public void ApplyMove_EmptyingHigh_ScansDownForNewHigh()
    {
        var mop = new int[26];
        mop[13] = 1;
        mop[8] = 3;
        mop[6] = 5;
        var s = BoardState.FromMop(mop);
        Assert.Equal(13, s.HighPointOccupied);

        s.ApplyMove(new Move(13, 9));

        Assert.Equal(9, s.HighPointOccupied);
    }

    [Fact]
    public void UndoMove_AboveHighPoint_RaisesHighPoint()
    {
        var mop = new int[26];
        mop[6] = 5;
        mop[8] = 3;
        var s = BoardState.FromMop(mop);
        Assert.Equal(8, s.HighPointOccupied);

        var move = new Move(13, 8);   // pretend a checker moved 13 → 8 previously
        s.UndoMove(move);

        Assert.Equal(13, s.HighPointOccupied);
        Assert.Equal(1, s.Points[13]);
        Assert.Equal(2, s.Points[8]);
    }

    // ── ApplyPlay end-to-end ──────────────────────────────────────

    /// <summary>
    /// Standard + 24/18 13/9 (a 6-4 split-and-build), then flip.
    /// Pre-flip: on-roll has 6(5), 8(3), 9(1), 13(4), 18(1), 24(1);
    ///           opponent has 1(-2), 12(-5), 17(-3), 19(-5).
    /// Post-flip (Points'[i] = -Points[25-i]):
    ///   on-roll has 6(5), 8(3), 13(5), 24(2);
    ///   opponent has 1(-1), 7(-1), 12(-4), 16(-1), 17(-3), 19(-5).
    /// Checker conservation: 15 each side.
    /// HighPointOccupied = 24.
    /// </summary>
    [Fact]
    public void ApplyPlay_StandardOpening_FlipsAndAppliesAtomically()
    {
        var s = BoardState.Standard();
        Play play = [new(24, 18), new(13, 9)];

        s.ApplyPlay(play);

        Assert.Equal(5, s.Points[6]);
        Assert.Equal(3, s.Points[8]);
        Assert.Equal(5, s.Points[13]);
        Assert.Equal(2, s.Points[24]);
        Assert.Equal(-1, s.Points[1]);
        Assert.Equal(-1, s.Points[7]);
        Assert.Equal(-4, s.Points[12]);
        Assert.Equal(-1, s.Points[16]);
        Assert.Equal(-3, s.Points[17]);
        Assert.Equal(-5, s.Points[19]);
        Assert.Equal(0, s.Points[0]);
        Assert.Equal(0, s.Points[25]);
        Assert.Equal(24, s.HighPointOccupied);

        // All other points clear.
        int[] expectZero = [2, 3, 4, 5, 9, 10, 11, 14, 15, 18, 20, 21, 22, 23];
        foreach (int i in expectZero)
            Assert.Equal(0, s.Points[i]);
    }

    [Fact]
    public void ApplyPlay_PreservesCheckerConservation()
    {
        var s = BoardState.Standard();
        Play play = [new(24, 18), new(13, 9)];

        s.ApplyPlay(play);

        int onRoll = 0, opp = 0;
        for (int i = 0; i <= 25; i++)
        {
            if (s.Points[i] > 0) onRoll += s.Points[i];
            else if (s.Points[i] < 0) opp -= s.Points[i];
        }

        Assert.Equal(15, onRoll);
        Assert.Equal(15, opp);
    }

    [Fact]
    public void ApplyPlay_TwoTurns_ChainCorrectly()
    {
        // Turn 1: on-roll plays 24/18 13/9 → opponent now on roll.
        // Turn 2: new on-roll (was opponent) plays a 6-4 mirror — 24/18 13/9 again
        //         from their POV. After this second flip, perspective returns to
        //         the original mover; checker counts must still total 15/15 and
        //         signs must remain consistent (positive = new on-roll).
        var s = BoardState.Standard();

        Play p1 = [new(24, 18), new(13, 9)];
        s.ApplyPlay(p1);

        Play p2 = [new(24, 18), new(13, 9)];
        s.ApplyPlay(p2);

        int onRoll = 0, opp = 0;
        for (int i = 0; i <= 25; i++)
        {
            if (s.Points[i] > 0) onRoll += s.Points[i];
            else if (s.Points[i] < 0) opp -= s.Points[i];
        }
        Assert.Equal(15, onRoll);
        Assert.Equal(15, opp);

        // After two flips, perspective is back to the original mover. Their
        // unmoved checkers still anchor 6(5), 8(3); they pushed builders to 9
        // and split to 18 (still showing on the board because the second
        // player's mirror moves did not touch those points).
        Assert.Equal(5, s.Points[6]);
        Assert.Equal(3, s.Points[8]);
        Assert.Equal(1, s.Points[9]);
        Assert.Equal(4, s.Points[13]);
        Assert.Equal(1, s.Points[18]);
        Assert.Equal(1, s.Points[24]);
    }

    [Fact]
    public void ApplyPlay_EmptyPlay_FlipsPerspectiveOnly()
    {
        // Forced pass — Play is empty but turn boundary still flips.
        var s = BoardState.Standard();
        Play empty = [];

        s.ApplyPlay(empty);

        // Standard position is symmetric under the flip, so the layout is
        // unchanged — but signs *would* flip on an asymmetric position.
        // Assert the easy invariant: it remains a 15/15 standard layout.
        Assert.Equal(5, s.Points[6]);
        Assert.Equal(3, s.Points[8]);
        Assert.Equal(5, s.Points[13]);
        Assert.Equal(2, s.Points[24]);
        Assert.Equal(-2, s.Points[1]);
        Assert.Equal(-5, s.Points[12]);
        Assert.Equal(-3, s.Points[17]);
        Assert.Equal(-5, s.Points[19]);
        Assert.Equal(24, s.HighPointOccupied);
    }

    // ── FlippedCopy ───────────────────────────────────────────────

    /// <summary>
    /// Asymmetric position with checkers on both bars:
    /// on-roll 6(4), 13(3), 24(1), bar[25](2); opponent 3(-2), 20(-3), bar[0](-1).
    /// Not flip-symmetric (e.g. Points[24] = 1 but -Points[1] = 0), so tests
    /// built on it cannot pass vacuously the way flip-symmetric positions can.
    /// </summary>
    private static BoardState AsymmetricBoard()
    {
        var mop = new int[26];
        mop[25] = 2;
        mop[24] = 1;
        mop[13] = 3;
        mop[6] = 4;
        mop[0] = -1;
        mop[3] = -2;
        mop[20] = -3;
        return BoardState.FromMop(mop);
    }

    [Fact]
    public void FlippedCopy_NegatesAndReversesIncludingBars()
    {
        var s = AsymmetricBoard();

        var f = s.FlippedCopy();

        // Exact transform: negate + reverse, point i ↔ 25 - i (bars 0 ↔ 25).
        for (int i = 0; i <= 25; i++)
        {
            Assert.True(
                f.Points[i] == -s.Points[25 - i],
                $"Point {i}: expected {-s.Points[25 - i]}, got {f.Points[i]}");
        }
    }

    [Fact]
    public void FlippedCopy_IsTheValuesFlip()
    {
        // One flip rule, on the value: the board's flip states none of its own.
        var s = AsymmetricBoard();

        Assert.Equal(s.ToPosition().Flipped(), s.FlippedCopy().ToPosition());
    }

    [Fact]
    public void Flip_RecomputesHighPointOccupied_ForTheNewFrame()
    {
        // The on-roll player's highest checker is on 5 and the opponent's
        // furthest back on 19 (its own 6-point), so the high point is 5
        // before a flip and 6 after it. The other flip fixtures keep the same
        // high point in both frames, so they cannot see a missed recompute.
        var s = BoardState.FromMop(
            [0, 3, 2, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -4, 0, 0, -2, 0, 0, 0]);
        Assert.Equal(5, s.HighPointOccupied);

        Assert.Equal(6, s.FlippedCopy().HighPointOccupied);

        s.ApplyPlay([]);   // a pass: the in-place flip alone
        Assert.Equal(6, s.HighPointOccupied);
    }

    [Fact]
    public void ApplyPlay_FlipsByTheValuesRule()
    {
        // The position the play reaches in the mover's frame, then the value's
        // flip, is what ApplyPlay leaves.
        var s = AsymmetricBoard();
        var reachedInMoversFrame = s.Copy();
        reachedInMoversFrame.ApplyMove(new Move(13, 9));

        s.ApplyPlay([new(13, 9)]);

        Assert.Equal(reachedInMoversFrame.ToPosition().Flipped(), s.ToPosition());
    }

    [Fact]
    public void FlippedCopy_FlipOfFlip_IsIdentity()
    {
        var s = AsymmetricBoard();

        var back = s.FlippedCopy().FlippedCopy();

        Assert.Equal(s.Points, back.Points);
        Assert.Equal(s.HighPointOccupied, back.HighPointOccupied);
    }

    [Fact]
    public void FlippedCopy_StandardStart_IsFlipSymmetric()
    {
        var s = BoardState.Standard();

        var f = s.FlippedCopy();

        Assert.Equal(s.Points, f.Points);
        Assert.Equal(s.HighPointOccupied, f.HighPointOccupied);
    }

    [Fact]
    public void FlippedCopy_LeavesReceiverUntouched_AndCopyIsIndependent()
    {
        var s = AsymmetricBoard();
        int[] before = [.. s.Points];
        int highBefore = s.HighPointOccupied;

        var f = s.FlippedCopy();

        Assert.Equal(before, s.Points);
        Assert.Equal(highBefore, s.HighPointOccupied);

        // Changing the copy must not leak back into the receiver (rewritten:
        // through the apply path, since a raw write no longer compiles).
        f.ApplyMove(new Move(5, 4));
        Assert.Equal(2, f.Points[5]);
        Assert.Equal(before, s.Points);
    }

    [Fact]
    public void FlippedCopy_SwapsPipCounts()
    {
        var s = AsymmetricBoard();
        // Guard against a vacuous pass: the two pip counts must differ.
        Assert.NotEqual(s.PipCount, s.OpponentPipCount);

        var f = s.FlippedCopy();

        Assert.Equal(s.OpponentPipCount, f.PipCount);
        Assert.Equal(s.PipCount, f.OpponentPipCount);
    }

    [Fact]
    public void FlippedCopy_HighPointOccupied_MatchesFreshConstruction()
    {
        var s = AsymmetricBoard();

        var f = s.FlippedCopy();
        var fresh = new BoardState(f.ToPosition());   // rewritten: ToMop gave way to the snapshot

        Assert.Equal(fresh.HighPointOccupied, f.HighPointOccupied);
    }

    [Fact]
    public void FlippedCopy_EmptyBoard_StaysEmpty()
    {
        // Rewritten: the empty board is built from its value, the public
        // empty constructor having gone.
        var f = new BoardState(BoardPosition.Empty).FlippedCopy();

        Assert.Equal(BoardPosition.Empty, f.ToPosition());
        Assert.Equal(0, f.HighPointOccupied);
    }

    [Fact]
    public void FlippedCopy_Bg960_IsFlipSymmetric()
    {
        // Bg960 positions are symmetric by construction (each made point's
        // mirror holds the negated count), so a flipped copy must reproduce
        // the original exactly.
        for (int seed = 1; seed <= 50; seed++)
        {
            var s = BoardState.Bg960(seed: seed);
            var f = s.FlippedCopy();
            Assert.True(
                f.Points.SequenceEqual(s.Points),
                $"Seed {seed}: flipped copy differs from original");
            Assert.Equal(s.HighPointOccupied, f.HighPointOccupied);
        }
    }
}
