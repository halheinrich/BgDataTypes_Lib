using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Play notation, through its one public route, <see cref="Play.ToNotation"/>
/// (halheinrich/backgammon#273). Every case is ported from BgMoveGen's
/// <c>MoveNotationFormatterTests</c> at BgMoveGen aabf5a0, with the same input
/// and the same expected string: the parity proof that the formatter moved
/// here unchanged. Each keeps its name with <c>Format_</c> read as
/// <c>Notation_</c>. Chain-collapse semantics are pinned in
/// <see cref="CanonicalPlayTests"/>; these pin what the rendering owns — the
/// "bar"/"off" labels, the "*" suffix, "(n)" run-grouping with the group's
/// star after the count, and the rendered shape of representative forms.
/// </summary>
public class PlayNotationTests
{
    private static string Notation(Play play) => play.ToNotation();

    // The public route ----------------------------------------------------

    [Fact]
    public void PublicSurface_ToNotationIsTheOneWayToSpellAPlay_TheChainFormIsInternal()
    {
        // halheinrich/backgammon#273: a consumer spells a play with
        // ToNotation and compares plays by position, so nothing public
        // exposes the chain form. The compiler refuses a public member whose
        // signature names an internal type, so the types' visibility is the
        // whole pin for everything but ToCanonical, pinned by name.
        var toNotation = typeof(Play).GetMethod(nameof(Play.ToNotation), Type.EmptyTypes);
        Assert.NotNull(toNotation);
        Assert.True(toNotation.IsPublic);
        Assert.Equal(typeof(string), toNotation.ReturnType);

        // Play.ToString stays the default: an encoding is more than its notation.
        Assert.Equal(typeof(ValueType), typeof(Play).GetMethod(nameof(ToString), Type.EmptyTypes)!.DeclaringType);

        Assert.False(typeof(CanonicalPlay).IsVisible);
        Assert.False(typeof(PlayChain).IsVisible);
        // ToCanonical exists (so the next line cannot pass vacuously) and is not public.
        Assert.NotNull(typeof(Play).GetMethod(nameof(Play.ToCanonical),
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));
        Assert.Null(typeof(Play).GetMethod(nameof(Play.ToCanonical),
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public));
    }

    // Regular moves --------------------------------------------------------

    [Fact]
    public void Notation_TwoDistinctMoves_SortedByFromPtDesc()
    {
        // Input order (13, 8) then (24, 22); output sorted from-pt desc.
        Assert.Equal("24/22 13/8", Notation(Play.Create(new Move(13, 8), new Move(24, 22))));
    }

    [Fact]
    public void Notation_SingleMove_Renders()
    {
        Assert.Equal("13/8", Notation(Play.Create(new Move(13, 8))));
    }

    [Fact]
    public void Notation_SameMoveTwice_GroupsWithCount()
    {
        Assert.Equal("13/11(2)", Notation(Play.Create(new Move(13, 11), new Move(13, 11))));
    }

    // Bar entry ------------------------------------------------------------

    [Fact]
    public void Notation_BarEntry_RendersBarPrefix()
    {
        Assert.Equal("bar/21", Notation(Play.Create(new Move(25, 21))));
    }

    [Fact]
    public void Notation_BarEntryHit_AppendsAsterisk()
    {
        Assert.Equal("bar/22*", Notation(Play.Create(new Move(25, -22))));
    }

    [Fact]
    public void Notation_TwoBarEntries_Grouped()
    {
        Assert.Equal("bar/23(2)", Notation(Play.Create(new Move(25, 23), new Move(25, 23))));
    }

    [Fact]
    public void Notation_TwoBarEntriesDifferentPoints_SortedByToPtDescTiebreak()
    {
        // Tied on from-pt (both bar=25); |to-pt| desc tiebreaker: 22 > 20.
        // Input in reverse order to exercise the sort.
        Assert.Equal("bar/22 bar/20", Notation(Play.Create(new Move(25, 20), new Move(25, 22))));
    }

    // Bear off -------------------------------------------------------------

    [Fact]
    public void Notation_BearOff_RendersOffSuffix()
    {
        Assert.Equal("2/off", Notation(Play.Create(new Move(2, 0))));
    }

    [Fact]
    public void Notation_TwoBearOffs_SeparateWhenDistinct()
    {
        Assert.Equal("6/off 5/off", Notation(Play.Create(new Move(6, 0), new Move(5, 0))));
    }

    [Fact]
    public void Notation_DoublesBearOffSamePoint_Grouped()
    {
        Assert.Equal("5/1 4/off(3)", Notation(Play.Create(
            new Move(5, 1), new Move(4, 0), new Move(4, 0), new Move(4, 0))));
    }

    [Fact]
    public void Notation_BearOffFromPoint1_RendersAs1Off()
    {
        Assert.Equal("6/3 1/off", Notation(Play.Create(new Move(6, 3), new Move(1, 0))));
    }

    // Hits -----------------------------------------------------------------

    [Fact]
    public void Notation_HitOnDestination_AppendsAsterisk()
    {
        Assert.Equal("24/18*", Notation(Play.Create(new Move(24, -18))));
    }

    [Fact]
    public void Notation_TwoSeparateHits_BothMarked()
    {
        Assert.Equal("24/18* 13/9*", Notation(Play.Create(new Move(24, -18), new Move(13, -9))));
    }

    [Fact]
    public void Notation_HitAtIntermediatePreventsForwardChainCollapse()
    {
        // 24/18*/17 — the hit at 18 must stay visible, so the chain does not
        // collapse to "24/17".
        Assert.Equal("24/18* 18/17", Notation(Play.Create(new Move(24, -18), new Move(18, 17))));
    }

    [Fact]
    public void Notation_HitAtFinalPointInChain_PreservesAsterisk()
    {
        // 24→21→15*: the hit is at the chain's endpoint, so the collapse is
        // fine and the "*" lands on the endpoint.
        Assert.Equal("24/15*", Notation(Play.Create(new Move(24, 21), new Move(21, -15))));
    }

    [Fact]
    public void Notation_IntermediateHitInTrajectory_RendersSplitChains()
    {
        // A three-leg trajectory hitting at the middle point: 13/11 11/9* 9/7.
        // Canonicalization splits at the hit (the "*" at 9 must stay visible),
        // so the play renders as two chains — collapsed prefix, then the
        // continuation.
        Assert.Equal("13/9* 9/7", Notation(Play.Create(
            new Move(13, 11), new Move(11, -9), new Move(9, 7))));
    }

    // Doubles --------------------------------------------------------------

    [Fact]
    public void Notation_FullDoubles_RendersAllFour()
    {
        Assert.Equal("24/20 13/9(2) 8/4", Notation(Play.Create(
            new Move(24, 20), new Move(13, 9), new Move(13, 9), new Move(8, 4))));
    }

    [Fact]
    public void Notation_DoublesAllSame_GroupsAsFour()
    {
        Assert.Equal("8/4(4)", Notation(Play.Create(
            new Move(8, 4), new Move(8, 4), new Move(8, 4), new Move(8, 4))));
    }

    [Fact]
    public void Notation_PairMixedHits_GroupsWithAsteriskAfterCount()
    {
        // Two checkers from 6 land on 2; the first hits an opponent blot, the
        // second joins the now-safe point. The group collapses to "6/2(2)"
        // and the "*" follows the count, never "6/2* 6/2".
        Assert.Equal("6/2(2)*", Notation(Play.Create(new Move(6, -2), new Move(6, 2))));
    }

    [Fact]
    public void Notation_QuadOneHit_GroupsWithAsteriskAfterCount()
    {
        // Doubles into the same destination, one hit among the four. Locks
        // the (n)-before-* ordering at higher counts.
        Assert.Equal("6/2(4)*", Notation(Play.Create(
            new Move(6, -2), new Move(6, 2), new Move(6, 2), new Move(6, 2))));
    }

    [Fact]
    public void Notation_PartialRepeat_GroupsOnlyMatching()
    {
        // 8/5 6/3 6/3 — the first leg distinct, the next two identical.
        Assert.Equal("8/5 6/3(2)", Notation(Play.Create(
            new Move(8, 5), new Move(6, 3), new Move(6, 3))));
    }

    // Chain compression ----------------------------------------------------

    [Fact]
    public void Notation_SameCheckerChainInOrder_CollapsesToSingleLeg()
    {
        // 24→21→15 written in time order.
        Assert.Equal("24/15", Notation(Play.Create(new Move(24, 21), new Move(21, 15))));
    }

    [Fact]
    public void Notation_SameCheckerChainOutOfOrder_CollapsesSame()
    {
        // The 21/14 case: XG writes (20, 14) before (21, 20). Order
        // insensitivity is canonicalization's; this pins that the notation
        // inherits it.
        Assert.Equal("21/14", Notation(Play.Create(new Move(20, 14), new Move(21, 20))));
    }

    [Fact]
    public void Notation_BarEntryChain_CompressesToBarSlashFinal()
    {
        Assert.Equal("bar/15", Notation(Play.Create(new Move(25, 21), new Move(21, 15))));
    }

    [Fact]
    public void Notation_ChainThenBearOff_Compresses()
    {
        Assert.Equal("4/off", Notation(Play.Create(new Move(4, 1), new Move(1, 0))));
    }

    [Fact]
    public void Notation_TwoIdenticalChains_Group()
    {
        // Two checkers each chain 24→20→16 — the collapsed chains are
        // identical and group as "(2)".
        Assert.Equal("24/16(2)", Notation(Play.Create(
            new Move(24, 20), new Move(20, 16),
            new Move(24, 20), new Move(20, 16))));
    }

    [Fact]
    public void Notation_MixChainableAndNonChainable_OnlyMatchingLegsCollapse()
    {
        // 24→21→15 chains; 13/8 is independent.
        Assert.Equal("24/15 13/8", Notation(Play.Create(
            new Move(24, 21), new Move(13, 8), new Move(21, 15))));
    }

    // Edge cases -----------------------------------------------------------

    [Fact]
    public void Notation_EmptyPlay_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, Notation(new Play()));
        Assert.Equal(string.Empty, default(CanonicalPlay).ToString());
    }
}
