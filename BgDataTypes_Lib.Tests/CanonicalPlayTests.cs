using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

public class CanonicalPlayTests
{
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
        // the intermediate touch-down point is not part of the play's identity.
        var viaTen = Play.Create(new(13, 10), new(10, 8)).ToCanonical();
        var viaEleven = Play.Create(new(13, 11), new(11, 8)).ToCanonical();

        Assert.Equal(viaTen, viaEleven);
        Assert.Equal(viaTen.GetHashCode(), viaEleven.GetHashCode());
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
        Assert.NotEqual(both, intermediateOnly);
        Assert.NotEqual(both, endpointOnly);
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

        Assert.Equal(direct, decomposed);
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
        // "(2)" grouping is a display concern, not an identity one.
        var interleaved = Play.Create(
            new(13, 11), new(11, 9), new(13, 11), new(11, 9)).ToCanonical();
        var grouped = Play.Create(
            new(13, 11), new(13, 11), new(11, 9), new(11, 9)).ToCanonical();

        Assert.Equal(2, interleaved.Count);
        Assert.Equal(new PlayChain(13, 9), interleaved[0]);
        Assert.Equal(new PlayChain(13, 9), interleaved[1]);
        Assert.Equal(interleaved, grouped);
    }

    [Fact]
    public void DuplicateChainCount_IsPartOfIdentity()
    {
        var twoCheckers = Play.Create(
            new(13, 11), new(11, 9), new(13, 11), new(11, 9)).ToCanonical();
        var oneChecker = Play.Create(new(13, 11), new(11, 9)).ToCanonical();

        Assert.NotEqual(twoCheckers, oneChecker);
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
    public void Equality_Operators_AndHashCode()
    {
        var a = Play.Create(new(13, 10), new(10, 8)).ToCanonical();
        var b = Play.Create(new Move(13, 8)).ToCanonical();
        var c = Play.Create(new Move(13, -8)).ToCanonical();

        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.True(a != c);
        Assert.True(a.Equals((object)b));
        Assert.False(a.Equals(null));
    }
}
