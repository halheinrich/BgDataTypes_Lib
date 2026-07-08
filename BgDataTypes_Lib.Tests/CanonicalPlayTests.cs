using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

public class CanonicalPlayTests
{
    private static Play PlayOf(params Move[] moves)
    {
        var p = new Play();
        foreach (var m in moves) p.Add(m);
        return p;
    }

    [Fact]
    public void EmptyPlay_CanonicalizesToDefault()
    {
        var canonical = new Play().ToCanonical();

        Assert.Equal(0, canonical.Count);
        Assert.Equal(default, canonical);
    }

    [Fact]
    public void SingleMove_SingleChain()
    {
        var canonical = PlayOf(new Move(13, 7)).ToCanonical();

        Assert.Equal(1, canonical.Count);
        Assert.Equal(new PlayChain(13, 7), canonical[0]);
    }

    [Fact]
    public void ConsecutiveLegs_CollapseToOneChain()
    {
        var canonical = PlayOf(new Move(13, 10), new Move(10, 8)).ToCanonical();

        Assert.Equal(1, canonical.Count);
        Assert.Equal(new PlayChain(13, 8), canonical[0]);
    }

    [Fact]
    public void OutOfOrderLegs_CollapseToOneChain()
    {
        var canonical = PlayOf(new Move(10, 8), new Move(13, 10)).ToCanonical();

        Assert.Equal(1, canonical.Count);
        Assert.Equal(new PlayChain(13, 8), canonical[0]);
    }

    [Fact]
    public void DifferentDecompositionRoutes_SameCanonicalForm()
    {
        // 13/8 played big die first (via 10) or small die first (via 11):
        // the intermediate touch-down point is not part of the play's identity.
        var viaTen = PlayOf(new Move(13, 10), new Move(10, 8)).ToCanonical();
        var viaEleven = PlayOf(new Move(13, 11), new Move(11, 8)).ToCanonical();

        Assert.Equal(viaTen, viaEleven);
        Assert.Equal(viaTen.GetHashCode(), viaEleven.GetHashCode());
    }

    [Fact]
    public void IntermediateHit_SplitsChain_HitStaysVisible()
    {
        // 13/10*/8 — the hit at 10 must stay visible, so the trajectory
        // splits there and the hit sits at the first chain's endpoint.
        var canonical = PlayOf(new Move(13, -10), new Move(10, 8)).ToCanonical();

        Assert.Equal(2, canonical.Count);
        Assert.Equal(new PlayChain(13, -10), canonical[0]);
        Assert.Equal(new PlayChain(10, 8), canonical[1]);
    }

    [Fact]
    public void EndpointHit_DoesNotBlockCollapse()
    {
        // 13/10 10/8* collapses to 13/8* — the hit is at the final landing
        // point, which stays visible on the merged chain.
        var canonical = PlayOf(new Move(13, 10), new Move(10, -8)).ToCanonical();

        Assert.Equal(1, canonical.Count);
        Assert.Equal(new PlayChain(13, -8), canonical[0]);
    }

    [Fact]
    public void DoubleHit_BothHitsPreserved()
    {
        // 13/10*/8* — hits at both points; nothing may collapse.
        var both = PlayOf(new Move(13, -10), new Move(10, -8)).ToCanonical();

        Assert.Equal(2, both.Count);
        Assert.Equal(new PlayChain(13, -10), both[0]);
        Assert.Equal(new PlayChain(10, -8), both[1]);

        var intermediateOnly = PlayOf(new Move(13, -10), new Move(10, 8)).ToCanonical();
        var endpointOnly = PlayOf(new Move(13, 10), new Move(10, -8)).ToCanonical();
        Assert.NotEqual(both, intermediateOnly);
        Assert.NotEqual(both, endpointOnly);
    }

    [Fact]
    public void BarEntry_Collapses_AcrossEntryPoint()
    {
        var canonical = PlayOf(new Move(25, 20), new Move(20, 15)).ToCanonical();

        Assert.Equal(1, canonical.Count);
        Assert.Equal(new PlayChain(25, 15), canonical[0]);
    }

    [Fact]
    public void BarEntry_HitOnEntryPoint_SplitsChain()
    {
        // bar/20* 20/15 — entering with a hit, then continuing: the hit at 20
        // is intermediate to the trajectory and must stay visible.
        var canonical = PlayOf(new Move(25, -20), new Move(20, 15)).ToCanonical();

        Assert.Equal(2, canonical.Count);
        Assert.Equal(new PlayChain(25, -20), canonical[0]);
        Assert.Equal(new PlayChain(20, 15), canonical[1]);
    }

    [Fact]
    public void BearOff_ChainEndsOff()
    {
        var canonical = PlayOf(new Move(6, 3), new Move(3, 0)).ToCanonical();

        Assert.Equal(1, canonical.Count);
        Assert.Equal(new PlayChain(6, 0), canonical[0]);
    }

    [Fact]
    public void BearOff_DirectAndDecomposed_SameCanonicalForm()
    {
        // 5/off in one hop (overshoot die) and 5/2 2/off both notate as
        // "5/off" — same canonical form.
        var direct = PlayOf(new Move(5, 0)).ToCanonical();
        var decomposed = PlayOf(new Move(5, 2), new Move(2, 0)).ToCanonical();

        Assert.Equal(direct, decomposed);
    }

    [Fact]
    public void Doubles_FourLegChain_CollapsesToOne()
    {
        var canonical = PlayOf(
            new Move(13, 11), new Move(11, 9), new Move(9, 7), new Move(7, 5)).ToCanonical();

        Assert.Equal(1, canonical.Count);
        Assert.Equal(new PlayChain(13, 5), canonical[0]);
    }

    [Fact]
    public void Doubles_TwoCheckersSameRoute_TwoEqualChains()
    {
        // Two checkers each playing 13/11 11/9. Duplicate chains are kept —
        // "(2)" grouping is a display concern, not an identity one.
        var interleaved = PlayOf(
            new Move(13, 11), new Move(11, 9), new Move(13, 11), new Move(11, 9)).ToCanonical();
        var grouped = PlayOf(
            new Move(13, 11), new Move(13, 11), new Move(11, 9), new Move(11, 9)).ToCanonical();

        Assert.Equal(2, interleaved.Count);
        Assert.Equal(new PlayChain(13, 9), interleaved[0]);
        Assert.Equal(new PlayChain(13, 9), interleaved[1]);
        Assert.Equal(interleaved, grouped);
    }

    [Fact]
    public void DuplicateChainCount_IsPartOfIdentity()
    {
        var twoCheckers = PlayOf(
            new Move(13, 11), new Move(11, 9), new Move(13, 11), new Move(11, 9)).ToCanonical();
        var oneChecker = PlayOf(new Move(13, 11), new Move(11, 9)).ToCanonical();

        Assert.NotEqual(twoCheckers, oneChecker);
    }

    [Fact]
    public void Chains_SortedByFromPointDescending()
    {
        var canonical = PlayOf(new Move(6, 3), new Move(13, 10)).ToCanonical();

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
        var canonical = PlayOf(new Move(10, 4), new Move(8, 5), new Move(4, 8)).ToCanonical();

        Assert.Equal(1, canonical.Count);
        Assert.Equal(new PlayChain(10, 5), canonical[0]);
    }

    [Fact]
    public void EncodingDomain_UpwardLeg_ExtendsChainBackward()
    {
        // Encoding-domain determinism pin: the upward leg 5/15 joins the
        // start of the already-built chain 15/10 (backward extension).
        var canonical = PlayOf(new Move(15, 10), new Move(5, 15)).ToCanonical();

        Assert.Equal(1, canonical.Count);
        Assert.Equal(new PlayChain(5, 10), canonical[0]);
    }

    [Fact]
    public void Indexer_OutOfRange_Throws()
    {
        var canonical = PlayOf(new Move(13, 7)).ToCanonical();

        Assert.Throws<IndexOutOfRangeException>(() => canonical[1]);
        Assert.Throws<IndexOutOfRangeException>(() => canonical[-1]);
        Assert.Throws<IndexOutOfRangeException>(() => default(CanonicalPlay)[0]);
    }

    [Fact]
    public void Equality_Operators_AndHashCode()
    {
        var a = PlayOf(new Move(13, 10), new Move(10, 8)).ToCanonical();
        var b = PlayOf(new Move(13, 8)).ToCanonical();
        var c = PlayOf(new Move(13, -8)).ToCanonical();

        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.True(a != c);
        Assert.True(a.Equals((object)b));
        Assert.False(a.Equals(null));
    }
}
