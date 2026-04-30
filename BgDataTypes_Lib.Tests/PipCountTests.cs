using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

public class PipCountTests
{
    [Fact]
    public void Standard_PipCount_Is167()
    {
        // 6×5 + 8×3 + 13×5 + 24×2 = 30 + 24 + 65 + 48 = 167
        Assert.Equal(167, BoardState.Standard().PipCount);
    }

    [Fact]
    public void Standard_OpponentPipCount_Is167()
    {
        // (25-19)×5 + (25-17)×3 + (25-12)×5 + (25-1)×2 = 30 + 24 + 65 + 48 = 167
        Assert.Equal(167, BoardState.Standard().OpponentPipCount);
    }

    [Fact]
    public void EmptyBoard_PipCounts_AreZero()
    {
        var s = new BoardState();
        Assert.Equal(0, s.PipCount);
        Assert.Equal(0, s.OpponentPipCount);
    }

    [Fact]
    public void OnRollBarChecker_Contributes25Pips()
    {
        var mop = new int[26];
        mop[25] = 1;
        var s = BoardState.FromMop(mop);

        Assert.Equal(25, s.PipCount);
    }

    [Fact]
    public void OpponentBarChecker_Contributes25Pips()
    {
        var mop = new int[26];
        mop[0] = -1;
        var s = BoardState.FromMop(mop);

        Assert.Equal(25, s.OpponentPipCount);
    }

    [Fact]
    public void StandardOpening_AfterApplyPlay_PipCountsReflectFlippedPOV()
    {
        // Pre-ApplyPlay on-roll played 24/18 13/9 (10 pips off their count: 6 + 4).
        // ApplyPlay flips perspective. Post-flip:
        //   PipCount         = previous opponent's pip count, untouched = 167
        //   OpponentPipCount = previous on-roll's reduced count             = 157
        var s = BoardState.Standard();
        var play = new Play();
        play.Add(new Move(24, 18));
        play.Add(new Move(13, 9));

        s.ApplyPlay(play);

        Assert.Equal(167, s.PipCount);
        Assert.Equal(157, s.OpponentPipCount);
    }

    [Fact]
    public void Nackgammon_PipCounts_AreSymmetric()
    {
        var s = BoardState.Nackgammon();
        Assert.Equal(s.PipCount, s.OpponentPipCount);
    }
}
