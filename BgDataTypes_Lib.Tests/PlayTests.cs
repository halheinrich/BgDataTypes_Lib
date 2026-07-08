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
    public void Equals_AndHashCode_AreOrderInvariant()
    {
        var p1 = new Play();
        p1.Add(new Move(13, 7));
        p1.Add(new Move(8, 5));

        var p2 = new Play();
        p2.Add(new Move(8, 5));
        p2.Add(new Move(13, 7));

        Assert.True(p1.Equals(p2));
        Assert.True(p1 == p2);
        Assert.Equal(p1.GetHashCode(), p2.GetHashCode());
    }

    [Fact]
    public void Equals_DecomposedEntry_MatchesCombinedEncoding()
    {
        // The quiz-entry repro: a user enters 13/8 as two clicks (13/10, then
        // 10/8); the candidate list stores the collapsed encoding {(13,8)}.
        // Both canonicalize to the single chain 13/8, so they are equal.
        var decomposed = new Play();
        decomposed.Add(new Move(13, 10));
        decomposed.Add(new Move(10, 8));

        var combined = new Play();
        combined.Add(new Move(13, 8));

        Assert.True(decomposed == combined);
        Assert.Equal(decomposed.GetHashCode(), combined.GetHashCode());
    }

    [Fact]
    public void Equals_HitOnIntermediatePoint_DistinctFromNonHitting()
    {
        // Deliberate reversal of the old hit-stripped DeduplicationKey pin
        // (hit and non-hit compared equal). 13/10*/8 and 13/8 are different
        // plays — one sends a blot to the bar — and the stripped key let a
        // hit-less encoding of a hitting play validate and apply without
        // barring the blot (the booked ApplyPlay/IsLegalPlay board-corruption
        // hazard). Equality is now fully hit-sensitive.
        var hitting = new Play();
        hitting.Add(new Move(13, -10));
        hitting.Add(new Move(10, 8));

        var quiet = new Play();
        quiet.Add(new Move(13, 8));

        Assert.True(hitting != quiet);
    }

    [Fact]
    public void Equals_HitAtFinalPoint_MatchesAcrossDecompositions()
    {
        // A hit at the trajectory's final landing point does not block the
        // collapse: 13/10 + 10/8* and the combined 13/8* are the same play.
        var decomposed = new Play();
        decomposed.Add(new Move(13, 10));
        decomposed.Add(new Move(10, -8));

        var combined = new Play();
        combined.Add(new Move(13, -8));

        Assert.True(decomposed == combined);
        Assert.Equal(decomposed.GetHashCode(), combined.GetHashCode());
    }

    [Fact]
    public void Equals_HitVsNonHit_SameTrajectory_NotEqual()
    {
        var hit = new Play();
        hit.Add(new Move(13, -7));

        var noHit = new Play();
        noHit.Add(new Move(13, 7));

        Assert.True(hit != noHit);
    }

    [Fact]
    public void Equals_DifferentPlays_NotEqual()
    {
        var p1 = new Play();
        p1.Add(new Move(13, 7));

        var p2 = new Play();
        p2.Add(new Move(13, 5));

        Assert.False(p1.Equals(p2));
        Assert.True(p1 != p2);
    }

    [Fact]
    public void Equals_EmptyPlays_Equal_AndDistinctFromNonEmpty()
    {
        var e1 = new Play();
        var e2 = new Play();

        var p = new Play();
        p.Add(new Move(13, 7));

        Assert.True(e1 == e2);
        Assert.Equal(e1.GetHashCode(), e2.GetHashCode());
        Assert.True(e1 != p);
    }

    [Fact]
    public void Equals_StaleBufferSlots_DoNotLeakIntoEquality()
    {
        // RemoveLast leaves the popped move in the buffer; equality must see
        // only the first Count moves.
        var trimmed = new Play();
        trimmed.Add(new Move(13, 7));
        trimmed.Add(new Move(8, 5));
        trimmed.RemoveLast();

        var fresh = new Play();
        fresh.Add(new Move(13, 7));

        Assert.True(trimmed == fresh);
        Assert.Equal(trimmed.GetHashCode(), fresh.GetHashCode());
    }
}
