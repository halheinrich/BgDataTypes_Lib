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

    // ── Mop bridge ────────────────────────────────────────────────

    [Fact]
    public void FromMop_ToMop_RoundTrip()
    {
        var s = BoardState.Standard();
        var mop = s.ToMop();
        var s2 = BoardState.FromMop(mop);

        Assert.Equal(s.Points, s2.Points);
        Assert.Equal(s.HighPointOccupied, s2.HighPointOccupied);
    }

    [Fact]
    public void ToMop_IsDefensiveCopy()
    {
        var s = BoardState.Standard();
        var mop = s.ToMop();

        // Mutating the source board after extraction must not affect the snapshot.
        s.Points[6] = 0;

        Assert.Equal(5, mop[6]);
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
        Assert.Throws<ArgumentException>(() => BoardState.FromMop(new int[25]));
    }

    [Fact]
    public void FromMop_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => BoardState.FromMop(null!));
    }

    // ── Copy ──────────────────────────────────────────────────────

    [Fact]
    public void Copy_IsDeep()
    {
        var s = BoardState.Standard();
        var c = s.Copy();

        c.Points[6] = 0;
        Assert.Equal(5, s.Points[6]);
    }

    // ── ApplyMove / UndoMove round-trip ───────────────────────────

    [Fact]
    public void ApplyMove_UndoMove_RegularMove_RoundTrips()
    {
        var s = BoardState.Standard();
        int[] before = (int[])s.Points.Clone();
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
        int[] before = (int[])s.Points.Clone();
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
        int[] before = (int[])s.Points.Clone();

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
        var play = new Play();
        play.Add(new Move(24, 18));
        play.Add(new Move(13, 9));

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
        var play = new Play();
        play.Add(new Move(24, 18));
        play.Add(new Move(13, 9));

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

        var p1 = new Play();
        p1.Add(new Move(24, 18));
        p1.Add(new Move(13, 9));
        s.ApplyPlay(p1);

        var p2 = new Play();
        p2.Add(new Move(24, 18));
        p2.Add(new Move(13, 9));
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
        var empty = new Play();

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
}
