using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

public class RaceTests
{
    [Fact]
    public void StandardStart_IsNotARace()
    {
        // Highest on-roll = 24, lowest opponent = 1. 24 < 1 is false.
        Assert.False(BoardState.Standard().IsRace);
    }

    [Fact]
    public void Nackgammon_IsNotARace()
    {
        Assert.False(BoardState.Nackgammon().IsRace);
    }

    [Fact]
    public void EmptyBoard_IsRace()
    {
        // Both sides borne off — vacuous race.
        Assert.True(new BoardState().IsRace);
    }

    [Fact]
    public void OnlyOnRollCheckers_IsRace()
    {
        // Opponent fully borne off; on-roll alone — vacuous.
        var mop = new int[26];
        mop[6] = 5;
        mop[3] = 10;
        Assert.True(BoardState.FromMop(mop).IsRace);
    }

    [Fact]
    public void OnlyOpponentCheckers_IsRace()
    {
        var mop = new int[26];
        mop[20] = -5;
        mop[22] = -10;
        Assert.True(BoardState.FromMop(mop).IsRace);
    }

    [Fact]
    public void PureRacePosition_IsRace()
    {
        // On-roll in their home board (1-6); opponent in theirs (19-24).
        // max_onroll = 6, min_opp = 19. 6 < 19 → race.
        var mop = new int[26];
        mop[1] = 3; mop[3] = 5; mop[6] = 7;
        mop[19] = -4; mop[22] = -6; mop[24] = -5;

        Assert.True(BoardState.FromMop(mop).IsRace);
    }

    [Fact]
    public void TouchingPositions_IsNotARace()
    {
        // On-roll at 13, opponent at 13 (impossible in real play, but the
        // predicate is structural). max_onroll = 13, min_opp = 13 → not <.
        var mop = new int[26];
        mop[13] = 1;
        // Opponent at 13 would be -1, but that means a hit — instead use
        // adjacent points: on-roll at 13, opp at 12. Then max_onroll = 13,
        // min_opp = 12 → 13 < 12 is false → not a race.
        mop[12] = -3;
        mop[6] = 5;
        mop[19] = -5;

        Assert.False(BoardState.FromMop(mop).IsRace);
    }

    [Fact]
    public void OnRollOnBar_IsNotARace()
    {
        // On-roll bar (Points[25] > 0) → max_onroll = 25; any opponent on
        // the playing surface gives min_opp ≤ 24 → not a race.
        var mop = new int[26];
        mop[25] = 1;
        mop[6] = 4;
        mop[19] = -5;
        Assert.False(BoardState.FromMop(mop).IsRace);
    }

    [Fact]
    public void OpponentOnBar_IsNotARace()
    {
        // Opponent bar (Points[0] < 0) → min_opp = 0; any on-roll checker
        // gives max_onroll ≥ 1 → not a race.
        var mop = new int[26];
        mop[0] = -1;
        mop[6] = 5;
        mop[19] = -5;
        Assert.False(BoardState.FromMop(mop).IsRace);
    }

    [Fact]
    public void BoundaryOffByOne_IsRace()
    {
        // max_onroll = 12, min_opp = 13 — strictly less, so race.
        var mop = new int[26];
        mop[6] = 7; mop[12] = 8;
        mop[13] = -8; mop[19] = -7;

        Assert.True(BoardState.FromMop(mop).IsRace);
    }
}
