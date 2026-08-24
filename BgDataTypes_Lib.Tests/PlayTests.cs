using System.Linq.Expressions;

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
    public void Create_OneMove_TypelessNew_Compiles()
    {
        // Regression pin for the former CS0121 gotcha: before the
        // fixed-arity overload set, `Play.Create(new(13, 7))` did not
        // compile — a lone target-typed new(…) bound to the params span
        // parameter itself. The overload set plus
        // [OverloadResolutionPriority(-1)] on the span overload resolves it.
        // This test compiling *is* the assertion; the checks below pin the
        // resulting play.
        var p = Play.Create(new(13, 7));

        Assert.Equal(1, p.Count);
        Assert.Equal(new Move(13, 7), p[0]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Create_LiteralCallSite_BindsFixedArityOverload(int arity)
    {
        // Overload resolution is a compile-time property, so pin it with
        // expression trees: they use the same resolution as an ordinary
        // call, and a ref struct parameter cannot appear in one — so had any
        // of these bound to Create(params ReadOnlySpan<Move>), this file
        // would not compile. The parameter-list assertion states the intent
        // that the compile-time behaviour is protecting.
        Move a = new(13, 7), b = new(8, 5), c = new(6, 3), d = new(24, 18);

        Expression<Func<Play>> expr = arity switch
        {
            1 => () => Play.Create(a),
            2 => () => Play.Create(a, b),
            3 => () => Play.Create(a, b, c),
            _ => () => Play.Create(a, b, c, d),
        };

        var bound = ((MethodCallExpression)expr.Body).Method;
        Assert.Equal(arity, bound.GetParameters().Length);
        Assert.All(bound.GetParameters(), q => Assert.Equal(typeof(Move), q.ParameterType));
    }

    [Fact]
    public void Create_EveryArity_MatchesAddBuiltPlay()
    {
        // The fixed-arity overloads and the incremental primitive share one
        // slot-write seam; this pins that they cannot drift. Byte-for-byte
        // on slots and Count, not just canonical equality.
        Move a = new(13, 7), b = new(8, 5), c = new(6, 3), d = new(24, 18);
        Move[] all = [a, b, c, d];

        Play[] created = [Play.Create(a), Play.Create(a, b), Play.Create(a, b, c), Play.Create(a, b, c, d)];

        for (int arity = 1; arity <= 4; arity++)
        {
            var added = new Play();
            for (int i = 0; i < arity; i++)
                added.Add(all[i]);

            var viaSpan = Play.Create(all.AsSpan(0, arity));
            var fixedArity = created[arity - 1];

            Assert.Equal(arity, fixedArity.Count);
            Assert.Equal(added.Count, fixedArity.Count);
            Assert.Equal(added.Count, viaSpan.Count);
            for (int i = 0; i < arity; i++)
            {
                Assert.Equal(added[i], fixedArity[i]);
                Assert.Equal(added[i], viaSpan[i]);
            }
        }
    }

    [Fact]
    public void Create_SpanOverload_StillReachableForExistingSpans()
    {
        // The general-arity door keeps working for callers that already hold
        // a span or array — [OverloadResolutionPriority] deprioritises it for
        // literal argument lists only, it does not hide it.
        Move[] moves = [new(13, 7), new(8, 5), new(6, 3)];

        var fromArray = Play.Create(moves);
        var fromSpan = Play.Create(moves.AsSpan());

        Assert.Equal(3, fromArray.Count);
        Assert.Equal(3, fromSpan.Count);
        Assert.Equal(new Move(6, 3), fromArray[2]);
        Assert.Equal(new Move(6, 3), fromSpan[2]);
    }

    [Fact]
    public void Create_SpanOverload_OverflowContractUnchanged()
    {
        // The overflow guard moved to an out-of-line throw helper; the
        // exception type, the parameter name, and the message must not move
        // with it. Only the span overload can overflow — the fixed-arity set
        // stops at the doubles maximum by construction.
        Move[] five = [new(8, 5), new(8, 5), new(6, 3), new(6, 3), new(5, 2)];

        var ex = Assert.Throws<ArgumentException>("moves", () => Play.Create(five));
        Assert.StartsWith("A play has at most 4 moves, got 5.", ex.Message);
    }

    [Fact]
    public void CollectionExpression_StillRoutesThroughSpanBuilder()
    {
        // [CollectionBuilder] targets the span overload by signature, so the
        // fixed-arity overloads and the resolution priority must leave
        // collection expressions untouched — at every arity, including one.
        Play one = [new(13, 7)];
        Play four = [new(8, 5), new(8, 5), new(6, 3), new(6, 3)];
        Play empty = [];

        Assert.Equal(1, one.Count);
        Assert.Equal(new Move(13, 7), one[0]);
        Assert.Equal(4, four.Count);
        Assert.Equal(0, empty.Count);
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
