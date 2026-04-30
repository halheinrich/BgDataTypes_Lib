using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

public class PlayTests
{
    [Fact]
    public void Default_IsEmpty()
    {
        var p = new Play();
        Assert.Equal(0, p.Count);
    }

    [Fact]
    public void Add_IncrementsCount_AndStoresMoves()
    {
        var p = new Play();
        p.Add(new Move(13, 7));
        p.Add(new Move(8, 5));

        Assert.Equal(2, p.Count);
        Assert.Equal(new Move(13, 7), p[0]);
        Assert.Equal(new Move(8, 5), p[1]);
    }

    [Fact]
    public void Add_FillsBuffer_FourMoves()
    {
        var p = new Play();
        p.Add(new Move(8, 5));
        p.Add(new Move(8, 5));
        p.Add(new Move(6, 3));
        p.Add(new Move(6, 3));

        Assert.Equal(4, p.Count);
        Assert.Equal(new Move(8, 5), p[0]);
        Assert.Equal(new Move(6, 3), p[3]);
    }

    [Fact]
    public void Add_BeyondFourMoves_Throws()
    {
        var p = new Play();
        p.Add(new Move(8, 5));
        p.Add(new Move(8, 5));
        p.Add(new Move(6, 3));
        p.Add(new Move(6, 3));

        Assert.Throws<InvalidOperationException>(() => p.Add(new Move(5, 2)));
    }

    [Fact]
    public void Indexer_OutOfRange_Throws()
    {
        var p = new Play();
        p.Add(new Move(13, 7));

        Assert.Throws<IndexOutOfRangeException>(() => p[4]);
        Assert.Throws<IndexOutOfRangeException>(() => p[-1]);
    }

    [Fact]
    public void RemoveLast_DecrementsCount()
    {
        var p = new Play();
        p.Add(new Move(13, 7));
        p.Add(new Move(8, 5));
        p.RemoveLast();

        Assert.Equal(1, p.Count);
        Assert.Equal(new Move(13, 7), p[0]);
    }

    [Fact]
    public void RemoveLast_OnEmpty_Throws()
    {
        var p = new Play();
        Assert.Throws<InvalidOperationException>(() => p.RemoveLast());
    }

    [Fact]
    public void Snapshot_DecouplesFromSource()
    {
        var p = new Play();
        p.Add(new Move(13, 7));
        var snap = p.Snapshot();

        p.Add(new Move(8, 5));

        Assert.Equal(1, snap.Count);
        Assert.Equal(2, p.Count);
    }

    [Fact]
    public void DeduplicationKey_IndependentOfMoveOrder()
    {
        var p1 = new Play();
        p1.Add(new Move(13, 7));
        p1.Add(new Move(8, 5));

        var p2 = new Play();
        p2.Add(new Move(8, 5));
        p2.Add(new Move(13, 7));

        Assert.Equal(p1.DeduplicationKey(), p2.DeduplicationKey());
    }

    [Fact]
    public void DeduplicationKey_TreatsHitAndRegular_AsSame_ForSameLanding()
    {
        // |ToPt| is what enters the key — a hit (negative) and a non-hit
        // (positive) on the same point are the same in dedup terms.
        var hit = new Play();
        hit.Add(new Move(13, -7));

        var noHit = new Play();
        noHit.Add(new Move(13, 7));

        Assert.Equal(hit.DeduplicationKey(), noHit.DeduplicationKey());
    }

    [Fact]
    public void DeduplicationKey_DiffersByFrAndAbsTo()
    {
        var p1 = new Play();
        p1.Add(new Move(13, 7));

        var p2 = new Play();
        p2.Add(new Move(13, 5));

        Assert.NotEqual(p1.DeduplicationKey(), p2.DeduplicationKey());
    }

    [Fact]
    public void DeduplicationKey_EmptyPlay_Sentinel()
    {
        var p = new Play();
        Assert.Equal((-99, -99, -99, -99, -99, -99, -99, -99), p.DeduplicationKey());
    }

    [Fact]
    public void Equals_AndHashCode_AreOrderInvariant()
    {
        var p1 = new Play();
        p1.Add(new Move(13, 7));
        p1.Add(new Move(8, 5));

        var p2 = new Play();
        p2.Add(new Move(8, 5));
        p2.Add(new Move(13, 7));

        Assert.True(p1.Equals(p2));
        Assert.Equal(p1.GetHashCode(), p2.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentPlays_NotEqual()
    {
        var p1 = new Play();
        p1.Add(new Move(13, 7));

        var p2 = new Play();
        p2.Add(new Move(13, 5));

        Assert.False(p1.Equals(p2));
    }
}
