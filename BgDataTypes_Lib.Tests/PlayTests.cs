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
        Play p = [new(13, 7)];

        Assert.Throws<IndexOutOfRangeException>(() => p[4]);
        Assert.Throws<IndexOutOfRangeException>(() => p[-1]);
    }

    [Fact]
    public void Create_NoMoves_IsEmptyPlay()
    {
        var p = Play.Create();

        Assert.Equal(0, p.Count);
        Assert.Equal(new Play(), p);
    }

    [Fact]
    public void Create_StoresMovesInOrder()
    {
        var p = Play.Create(new(13, 7), new(8, 5));

        Assert.Equal(2, p.Count);
        Assert.Equal(new Move(13, 7), p[0]);
        Assert.Equal(new Move(8, 5), p[1]);
    }

    [Fact]
    public void Create_FourMoves_FillsBuffer()
    {
        var p = Play.Create(new(8, 5), new(8, 5), new(6, 3), new(6, 3));

        Assert.Equal(4, p.Count);
        Assert.Equal(new Move(8, 5), p[0]);
        Assert.Equal(new Move(6, 3), p[3]);
    }

    [Fact]
    public void Create_FiveMoves_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("moves",
            () => Play.Create(new(8, 5), new(8, 5), new(6, 3), new(6, 3), new(5, 2)));
    }

    [Fact]
    public void Create_MatchesAddBuiltPlay()
    {
        var added = new Play();
        added.Add(new Move(13, 7));
        added.Add(new Move(8, 5));

        var created = Play.Create(new(13, 7), new(8, 5));

        Assert.Equal(added.Count, created.Count);
        Assert.Equal(added[0], created[0]);
        Assert.Equal(added[1], created[1]);
        Assert.True(added == created);
    }

    [Fact]
    public void CollectionExpression_BuildsPlay()
    {
        Play p = [new(13, 7), new(8, 5)];

        Assert.Equal(2, p.Count);
        Assert.Equal(new Move(13, 7), p[0]);
        Assert.Equal(new Move(8, 5), p[1]);
    }

    [Fact]
    public void CollectionExpression_Empty_IsForcedPass()
    {
        Play p = [];

        Assert.Equal(0, p.Count);
        Assert.Equal(new Play(), p);
    }

    [Fact]
    public void Foreach_YieldsMovesInInsertionOrder()
    {
        Play p = [new(13, 7), new(8, 5), new(6, 3)];

        var seen = new List<Move>();
        foreach (var move in p)
            seen.Add(move);

        Assert.Equal([new(13, 7), new(8, 5), new(6, 3)], seen);
    }

    [Fact]
    public void Foreach_EmptyPlay_YieldsNothing()
    {
        Play p = [];

        foreach (var move in p)
            Assert.Fail($"Empty play yielded {move}.");
    }

    [Fact]
    public void Foreach_EnumeratesCopy_SourceMutationInvisible()
    {
        // The enumerator carries its own value-type copy of the play, so a
        // mid-enumeration Add on the source cannot extend the sequence.
        Play p = [new(13, 7)];

        var seen = 0;
        foreach (var _ in p)
        {
            p.Add(new Move(8, 5));
            seen++;
        }

        Assert.Equal(1, seen);
        Assert.Equal(2, p.Count);
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
        Play p1 = [new(13, 7), new(8, 5)];
        Play p2 = [new(8, 5), new(13, 7)];

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
        Play decomposed = [new(13, 10), new(10, 8)];
        Play combined = [new(13, 8)];

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
        Play hitting = [new(13, -10), new(10, 8)];
        Play quiet = [new(13, 8)];

        Assert.True(hitting != quiet);
    }

    [Fact]
    public void Equals_HitAtFinalPoint_MatchesAcrossDecompositions()
    {
        // A hit at the trajectory's final landing point does not block the
        // collapse: 13/10 + 10/8* and the combined 13/8* are the same play.
        Play decomposed = [new(13, 10), new(10, -8)];
        Play combined = [new(13, -8)];

        Assert.True(decomposed == combined);
        Assert.Equal(decomposed.GetHashCode(), combined.GetHashCode());
    }

    [Fact]
    public void Equals_HitVsNonHit_SameTrajectory_NotEqual()
    {
        Play hit = [new(13, -7)];
        Play noHit = [new(13, 7)];

        Assert.True(hit != noHit);
    }

    [Fact]
    public void Equals_DifferentPlays_NotEqual()
    {
        Play p1 = [new(13, 7)];
        Play p2 = [new(13, 5)];

        Assert.False(p1.Equals(p2));
        Assert.True(p1 != p2);
    }

    [Fact]
    public void Equals_EmptyPlays_Equal_AndDistinctFromNonEmpty()
    {
        Play e1 = [];
        Play e2 = [];
        Play p = [new(13, 7)];

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
