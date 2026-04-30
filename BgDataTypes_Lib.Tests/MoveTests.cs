using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

public class MoveTests
{
    [Fact]
    public void Regular_Move_StoresFrAndTo()
    {
        var m = new Move(13, 7);
        Assert.Equal(13, m.FrPt);
        Assert.Equal(7, m.ToPt);
    }

    [Fact]
    public void BearOff_Encoded_AsZeroToPt()
    {
        var m = new Move(4, 0);
        Assert.Equal(4, m.FrPt);
        Assert.Equal(0, m.ToPt);
    }

    [Fact]
    public void Hit_Encoded_AsNegativeToPt()
    {
        var m = new Move(13, -12);
        Assert.Equal(13, m.FrPt);
        Assert.Equal(-12, m.ToPt);
    }

    [Fact]
    public void BarEntry_Encoded_AsFrPt25()
    {
        var m = new Move(25, 22);
        Assert.Equal(25, m.FrPt);
        Assert.Equal(22, m.ToPt);
    }

    [Fact]
    public void RecordStruct_ValueEquality()
    {
        Assert.Equal(new Move(8, 5), new Move(8, 5));
        Assert.NotEqual(new Move(8, 5), new Move(8, 3));
        Assert.NotEqual(new Move(13, -12), new Move(13, 12));
    }

    [Fact]
    public void RecordStruct_HashCodeMatches_OnEquality()
    {
        Assert.Equal(new Move(8, 5).GetHashCode(), new Move(8, 5).GetHashCode());
    }
}
