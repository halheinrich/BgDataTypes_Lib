using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The display rule: which chains a play's notation shows and where its hit
/// marks go. <see cref="CanonicalPlay"/> has no equality (halheinrich/backgammon#273,
/// ruling A), so forms are compared chain by chain through <see cref="Chains"/>;
/// play identity is pinned in <see cref="PlayIdentityTests"/>.
/// </summary>
public class CanonicalPlayTests
{
    /// <summary>A form's chains in canonical order, for chain-by-chain comparison.</summary>
    private static PlayChain[] Chains(CanonicalPlay form)
    {
        var chains = new PlayChain[form.Count];
        for (int i = 0; i < form.Count; i++)
            chains[i] = form[i];
        return chains;
    }

    [Fact]
    public void EmptyPlay_CanonicalizesToDefault()
    {
        var canonical = new Play().ToCanonical();

        Assert.Equal(0, canonical.Count);
        Assert.Equal(Chains(default), Chains(canonical));
    }

    [Fact]
    public void SingleMove_SingleChain()
    {
        var canonical = Play.Create(new Move(13, 7)).ToCanonical();

        Assert.Equal(1, canonical.Count);
        Assert.Equal(new PlayChain(13, 7), canonical[0]);
    }

    [Fact]
    public void ConsecutiveLegs_CollapseToOneChain()
    {
        var canonical = Play.Create(new(13, 10), new(10, 8)).ToCanonical();

        Assert.Equal(1, canonical.Count);
        Assert.Equal(new PlayChain(13, 8), canonical[0]);
    }

    [Fact]
    public void OutOfOrderLegs_CollapseToOneChain()
    {
        var canonical = Play.Create(new(10, 8), new(13, 10)).ToCanonical();

        Assert.Equal(1, canonical.Count);
        Assert.Equal(new PlayChain(13, 8), canonical[0]);
    }

    [Fact]
    public void DifferentDecompositionRoutes_SameCanonicalForm()
    {
        // 13/8 played big die first (via 10) or small die first (via 11):
        // the intermediate touch-down point is not shown.
        var viaTen = Play.Create(new(13, 10), new(10, 8)).ToCanonical();
        var viaEleven = Play.Create(new(13, 11), new(11, 8)).ToCanonical();

        Assert.Equal([new PlayChain(13, 8)], Chains(viaTen));
        Assert.Equal(Chains(viaTen), Chains(viaEleven));
    }

    [Fact]
    public void IntermediateHit_SplitsChain_HitStaysVisible()
    {
        // 13/10*/8 — the hit at 10 must stay visible, so the trajectory
        // splits there and the hit sits at the first chain's endpoint.
        var canonical = Play.Create(new(13, -10), new(10, 8)).ToCanonical();

        Assert.Equal(2, canonical.Count);
        Assert.Equal(new PlayChain(13, -10), canonical[0]);
        Assert.Equal(new PlayChain(10, 8), canonical[1]);
    }

    [Fact]
    public void EndpointHit_DoesNotBlockCollapse()
    {
        // 13/10 10/8* collapses to 13/8* — the hit is at the final landing
        // point, which stays visible on the merged chain.
        var canonical = Play.Create(new(13, 10), new(10, -8)).ToCanonical();

        Assert.Equal(1, canonical.Count);
        Assert.Equal(new PlayChain(13, -8), canonical[0]);
    }

    [Fact]
    public void DoubleHit_BothHitsPreserved()
    {
        // 13/10*/8* — hits at both points; nothing may collapse.
        var both = Play.Create(new(13, -10), new(10, -8)).ToCanonical();

        Assert.Equal(2, both.Count);
        Assert.Equal(new PlayChain(13, -10), both[0]);
        Assert.Equal(new PlayChain(10, -8), both[1]);

        var intermediateOnly = Play.Create(new(13, -10), new(10, 8)).ToCanonical();
        var endpointOnly = Play.Create(new(13, 10), new(10, -8)).ToCanonical();
        Assert.NotEqual(Chains(both), Chains(intermediateOnly));
        Assert.NotEqual(Chains(both), Chains(endpointOnly));
    }

    [Fact]
    public void PointMadeOnBlot_EitherAttribution_MarkOnCarrier()
    {
        // halheinrich/backgammon#273: 5-4 making the 3-point on a blot. One
        // blot, hit once; which checker's move records the hit does not change
        // the display. The mark goes to the carrier — the first chain in
        // canonical order ending on 3 — whichever move recorded it.
        var hitOnEight = Play.Create(new(8, -3), new(7, 3)).ToCanonical();
        var hitOnSeven = Play.Create(new(8, 3), new(7, -3)).ToCanonical();

        Assert.Equal(2, hitOnSeven.Count);
        Assert.Equal(new PlayChain(8, -3), hitOnSeven[0]);
        Assert.Equal(new PlayChain(7, 3), hitOnSeven[1]);
        Assert.Equal(Chains(hitOnEight), Chains(hitOnSeven));
    }

    [Fact]
    public void Doubles_ThreeCheckersPointOnBlot_EveryAttributionSameForm()
    {
        // 2-2 with a blot on 4: 8/4 and two checkers 6/4. Every move landing
        // on 4 may record the hit, in the collapsed encoding (8/4 as one
        // move) and the single-die one (8/6 6/4); all display alike, with the
        // mark on the carrier 8/4.
        Play[] encodings =
        [
            [new(8, -4), new(6, 4), new(6, 4)],
            [new(8, 4), new(6, -4), new(6, 4)],
            [new(6, 4), new(8, 4), new(6, -4)],
            [new(8, 6), new(6, -4), new(6, 4), new(6, 4)],
            [new(6, 4), new(8, 6), new(6, 4), new(6, -4)],
        ];

        foreach (var play in encodings)
        {
            var canonical = play.ToCanonical();

            Assert.Equal(3, canonical.Count);
            Assert.Equal(new PlayChain(8, -4), canonical[0]);
            Assert.Equal(new PlayChain(6, 4), canonical[1]);
            Assert.Equal(new PlayChain(6, 4), canonical[2]);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Doubles_FourCheckersOnOnePoint_EveryAttributionSameForm(int hitter)
    {
        // 2-2, four checkers 6/4 onto a blot. Legal single-die moves onto one
        // point share a source, so the four attributions are one multiset of
        // moves; the pin is that the group keeps exactly one mark, on the
        // first of its identical chains.
        var moves = new Move[] { new(6, 4), new(6, 4), new(6, 4), new(6, 4) };
        moves[hitter] = new Move(6, -4);

        var canonical = Play.Create(moves).ToCanonical();

        Assert.Equal(4, canonical.Count);
        Assert.Equal(new PlayChain(6, -4), canonical[0]);
        for (int i = 1; i < 4; i++)
            Assert.Equal(new PlayChain(6, 4), canonical[i]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void EncodingDomain_FourChainsOnOnePoint_MarkOnCarrierWhicheverRecordedIt(int hitter)
    {
        // Encoding-domain pin (no legal roll lands four checkers on one point
        // from four sources): four distinct chains ending on 5, the hit
        // recorded on each in turn. One point, one mark, on the carrier 9/5.
        var moves = new Move[] { new(9, 5), new(8, 5), new(7, 5), new(6, 5) };
        moves[hitter] = moves[hitter] with { ToPt = -5 };

        var canonical = Play.Create(moves).ToCanonical();

        Assert.Equal(4, canonical.Count);
        Assert.Equal(new PlayChain(9, -5), canonical[0]);
        Assert.Equal(new PlayChain(8, 5), canonical[1]);
        Assert.Equal(new PlayChain(7, 5), canonical[2]);
        Assert.Equal(new PlayChain(6, 5), canonical[3]);
    }

    [Fact]
    public void HitPoint_SharedLanding_OnlyCarrierStops()
    {
        // 3-3 with a blot on 9: checkers from 15 and 12 both reach 9 and one
        // of them continues to 6. The carrier 15/9 keeps 9 as its endpoint;
        // the other chain landing there continues through it. Which move
        // landing on 9 recorded the hit does not change the chains.
        Play[] encodings =
        [
            [new(15, -9), new(12, 9), new(9, 6)],
            [new(15, 9), new(12, -9), new(9, 6)],
            [new(15, 12), new(12, -9), new(12, 9), new(9, 6)],
        ];

        foreach (var play in encodings)
        {
            var canonical = play.ToCanonical();

            Assert.Equal(2, canonical.Count);
            Assert.Equal(new PlayChain(15, -9), canonical[0]);
            Assert.Equal(new PlayChain(12, 6), canonical[1]);
        }
    }

    [Fact]
    public void Doubles_IdenticalChainsOnHitPoint_OneStopsOneContinues()
    {
        // 2-2 with a blot on 6: two checkers 8/6, one continuing to 4. The
        // two chains ending on 6 are identical, so either may continue while
        // the other carries the mark; single-die and collapsed encodings
        // agree on 8/6* 8/4.
        var singleDie = Play.Create(new(8, 6), new(8, -6), new(6, 4)).ToCanonical();
        var collapsed = Play.Create(new(8, -6), new(8, 4)).ToCanonical();

        Assert.Equal(2, singleDie.Count);
        Assert.Equal(new PlayChain(8, -6), singleDie[0]);
        Assert.Equal(new PlayChain(8, 4), singleDie[1]);
        Assert.Equal(Chains(collapsed), Chains(singleDie));
    }

    [Fact]
    public void Doubles_HitsOnTwoPoints_DistinctFromEitherAlone()
    {
        // 2-2, two checkers 8/6/4 over blots. Attribution is per point: a hit
        // on 6 and one on 4 displays differently from a hit on only one of
        // them, and from the quiet play.
        var both = Play.Create(new(8, -6), new(6, -4), new(8, 6), new(6, 4)).ToCanonical();
        var sixOnly = Play.Create(new(8, -6), new(6, 4), new(8, 6), new(6, 4)).ToCanonical();
        var fourOnly = Play.Create(new(8, 6), new(6, -4), new(8, 6), new(6, 4)).ToCanonical();
        var quiet = Play.Create(new(8, 6), new(6, 4), new(8, 6), new(6, 4)).ToCanonical();

        CanonicalPlay[] all = [both, sixOnly, fourOnly, quiet];
        for (int i = 0; i < all.Length; i++)
            for (int j = i + 1; j < all.Length; j++)
                Assert.NotEqual(Chains(all[i]), Chains(all[j]));
    }

    [Fact]
    public void EncodingDomain_TwoMarksOnOnePoint_CountAsTheOneHit()
    {
        // Encoding-domain pin (no legal play hits one point twice — it holds
        // at most one blot): the form shows which points are hit, so a second
        // mark on the same point adds nothing.
        var twoMarks = Play.Create(new(8, -3), new(7, -3)).ToCanonical();
        var oneMark = Play.Create(new(8, -3), new(7, 3)).ToCanonical();

        Assert.Equal(new PlayChain(8, -3), twoMarks[0]);
        Assert.Equal(new PlayChain(7, 3), twoMarks[1]);
        Assert.Equal(Chains(oneMark), Chains(twoMarks));
    }

    [Fact]
    public void BarEntry_Collapses_AcrossEntryPoint()
    {
        var canonical = Play.Create(new(25, 20), new(20, 15)).ToCanonical();

        Assert.Equal(1, canonical.Count);
        Assert.Equal(new PlayChain(25, 15), canonical[0]);
    }

    [Fact]
    public void BarEntry_HitOnEntryPoint_SplitsChain()
    {
        // bar/20* 20/15 — entering with a hit, then continuing: the hit at 20
        // is intermediate to the trajectory and must stay visible.
        var canonical = Play.Create(new(25, -20), new(20, 15)).ToCanonical();

        Assert.Equal(2, canonical.Count);
        Assert.Equal(new PlayChain(25, -20), canonical[0]);
        Assert.Equal(new PlayChain(20, 15), canonical[1]);
    }

    [Fact]
    public void BearOff_ChainEndsOff()
    {
        var canonical = Play.Create(new(6, 3), new(3, 0)).ToCanonical();

        Assert.Equal(1, canonical.Count);
        Assert.Equal(new PlayChain(6, 0), canonical[0]);
    }

    [Fact]
    public void BearOff_DirectAndDecomposed_SameCanonicalForm()
    {
        // 5/off in one hop (overshoot die) and 5/2 2/off both notate as
        // "5/off" — same canonical form.
        var direct = Play.Create(new Move(5, 0)).ToCanonical();
        var decomposed = Play.Create(new(5, 2), new(2, 0)).ToCanonical();

        Assert.Equal(Chains(direct), Chains(decomposed));
    }

    [Fact]
    public void Doubles_FourLegChain_CollapsesToOne()
    {
        var canonical = Play.Create(
            new(13, 11), new(11, 9), new(9, 7), new(7, 5)).ToCanonical();

        Assert.Equal(1, canonical.Count);
        Assert.Equal(new PlayChain(13, 5), canonical[0]);
    }

    [Fact]
    public void Doubles_TwoCheckersSameRoute_TwoEqualChains()
    {
        // Two checkers each playing 13/11 11/9. Duplicate chains are kept —
        // "(2)" grouping is the formatter's rendering of them.
        var interleaved = Play.Create(
            new(13, 11), new(11, 9), new(13, 11), new(11, 9)).ToCanonical();
        var grouped = Play.Create(
            new(13, 11), new(13, 11), new(11, 9), new(11, 9)).ToCanonical();

        Assert.Equal(2, interleaved.Count);
        Assert.Equal(new PlayChain(13, 9), interleaved[0]);
        Assert.Equal(new PlayChain(13, 9), interleaved[1]);
        Assert.Equal(Chains(interleaved), Chains(grouped));
    }

    [Fact]
    public void DuplicateChainCount_IsPartOfTheForm()
    {
        var twoCheckers = Play.Create(
            new(13, 11), new(11, 9), new(13, 11), new(11, 9)).ToCanonical();
        var oneChecker = Play.Create(new(13, 11), new(11, 9)).ToCanonical();

        Assert.NotEqual(Chains(twoCheckers), Chains(oneChecker));
    }

    [Fact]
    public void Chains_SortedByFromPointDescending()
    {
        var canonical = Play.Create(new(6, 3), new(13, 10)).ToCanonical();

        Assert.Equal(2, canonical.Count);
        Assert.Equal(new PlayChain(13, 10), canonical[0]);
        Assert.Equal(new PlayChain(6, 3), canonical[1]);
    }

    [Fact]
    public void EncodingDomain_ZigzagTrajectory_FusesToFixpoint()
    {
        // Encoding-domain determinism pin (legal plays always move downward;
        // this zigzag 10/4/8/5 exercises the chain-fuse fixpoint). The upward
        // leg 4/8 first extends 10/4, leaving 10/8 adjacent to 8/5; the fuse
        // pass joins them.
        var canonical = Play.Create(new(10, 4), new(8, 5), new(4, 8)).ToCanonical();

        Assert.Equal(1, canonical.Count);
        Assert.Equal(new PlayChain(10, 5), canonical[0]);
    }

    [Fact]
    public void EncodingDomain_UpwardLeg_ExtendsChainBackward()
    {
        // Encoding-domain determinism pin: the upward leg 5/15 joins the
        // start of the already-built chain 15/10 (backward extension).
        var canonical = Play.Create(new(15, 10), new(5, 15)).ToCanonical();

        Assert.Equal(1, canonical.Count);
        Assert.Equal(new PlayChain(5, 10), canonical[0]);
    }

    [Fact]
    public void Indexer_OutOfRange_Throws()
    {
        var canonical = Play.Create(new Move(13, 7)).ToCanonical();

        Assert.Throws<IndexOutOfRangeException>(() => canonical[1]);
        Assert.Throws<IndexOutOfRangeException>(() => canonical[-1]);
        Assert.Throws<IndexOutOfRangeException>(() => default(CanonicalPlay)[0]);
    }

    [Fact]
    public void Equality_Retired_NoOperators_AndEveryRouteThrows()
    {
        // Rewritten from Equality_Operators_AndHashCode (halheinrich/backgammon#273,
        // ruling A): the display form has no equality. == and != must be
        // compile errors, so no operator is declared, and every runtime route
        // to Equals or GetHashCode fails loudly.
        var a = Play.Create(new(13, 10), new(10, 8)).ToCanonical();
        var b = Play.Create(new Move(13, 8)).ToCanonical();

        Assert.Null(typeof(CanonicalPlay).GetMethod("op_Equality"));
        Assert.Null(typeof(CanonicalPlay).GetMethod("op_Inequality"));
        Assert.DoesNotContain(typeof(IEquatable<CanonicalPlay>), typeof(CanonicalPlay).GetInterfaces());

        Assert.Throws<NotSupportedException>(() => a.Equals(b));
        Assert.Throws<NotSupportedException>(() => a.Equals(null));
        Assert.Throws<NotSupportedException>(() => a.GetHashCode());
        Assert.Throws<NotSupportedException>(() => EqualityComparer<CanonicalPlay>.Default.Equals(a, b));
        Assert.Throws<NotSupportedException>(() => new HashSet<CanonicalPlay> { a });
    }
}
