using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

// The two-part cube answer of halheinrich/backgammon#86 (SPEC-scoring §3).
public class CubeClaimPairTests
{
    // ---------------------------------------------------------------------
    //  Construction — every valid cell of the 3×2, including the incoherent
    //  one, is representable (SPEC-scoring §3: "The incoherent cell is
    //  allowed" — the type must represent it, not prevent it)
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(CubeClaim.NoDouble, CubeAction.Take)]
    [InlineData(CubeClaim.NoDouble, CubeAction.Pass)]   // the incoherent cell
    [InlineData(CubeClaim.Double, CubeAction.Take)]
    [InlineData(CubeClaim.Double, CubeAction.Pass)]
    [InlineData(CubeClaim.TooGood, CubeAction.Take)]
    [InlineData(CubeClaim.TooGood, CubeAction.Pass)]
    public void Constructs_EveryCellOfTheThreeByTwo(CubeClaim claim, CubeAction taker)
    {
        var pair = new CubeClaimPair(claim, taker);
        Assert.Equal(claim, pair.Claim);
        Assert.Equal(taker, pair.Taker);
    }

    // ---------------------------------------------------------------------
    //  Half-guards
    // ---------------------------------------------------------------------

    [Fact]
    public void UndefinedClaim_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CubeClaimPair((CubeClaim)99, CubeAction.Take));
    }

    [Theory]
    [InlineData(CubeAction.NoDouble)]
    [InlineData(CubeAction.Double)]
    public void DoublerHalfActionAsTaker_Throws(CubeAction doublerAction)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CubeClaimPair(CubeClaim.Double, doublerAction));
    }

    [Fact]
    public void UndefinedTaker_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CubeClaimPair(CubeClaim.Double, (CubeAction)99));
    }

    // ---------------------------------------------------------------------
    //  Canonical instances
    // ---------------------------------------------------------------------

    public static TheoryData<CubeClaimPair, CubeClaim, CubeAction> CanonicalCells => new()
    {
        { CubeClaimPair.NoDoubleTake, CubeClaim.NoDouble, CubeAction.Take },
        { CubeClaimPair.NoDoublePass, CubeClaim.NoDouble, CubeAction.Pass },
        { CubeClaimPair.DoubleTake, CubeClaim.Double, CubeAction.Take },
        { CubeClaimPair.DoublePass, CubeClaim.Double, CubeAction.Pass },
        { CubeClaimPair.TooGoodTake, CubeClaim.TooGood, CubeAction.Take },
        { CubeClaimPair.TooGoodPass, CubeClaim.TooGood, CubeAction.Pass },
    };

    [Theory]
    [MemberData(nameof(CanonicalCells))]
    public void CanonicalInstances_CarryExpectedHalves(
        CubeClaimPair pair, CubeClaim claim, CubeAction taker)
    {
        Assert.Equal(claim, pair.Claim);
        Assert.Equal(taker, pair.Taker);
    }

    // ---------------------------------------------------------------------
    //  IsIncoherent — exactly one cell of the six
    // ---------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(CanonicalCells))]
    public void IsIncoherent_TrueForExactlyTheNoDoublePassCell(
        CubeClaimPair pair, CubeClaim claim, CubeAction taker)
    {
        Assert.Equal(
            claim == CubeClaim.NoDouble && taker == CubeAction.Pass,
            pair.IsIncoherent);
    }

    // ---------------------------------------------------------------------
    //  Equality — record-struct value semantics over the two halves
    // ---------------------------------------------------------------------

    [Fact]
    public void Equality_IsValueBasedOverBothHalves()
    {
        Assert.Equal(CubeClaimPair.TooGoodTake, new CubeClaimPair(CubeClaim.TooGood, CubeAction.Take));
        Assert.NotEqual(CubeClaimPair.TooGoodTake, CubeClaimPair.TooGoodPass);
        Assert.NotEqual(CubeClaimPair.TooGoodTake, CubeClaimPair.NoDoubleTake);
        Assert.True(CubeClaimPair.NoDoublePass == new CubeClaimPair(CubeClaim.NoDouble, CubeAction.Pass));
    }

    // ---------------------------------------------------------------------
    //  default caveat — the standard value-type escape from the half-guards
    // ---------------------------------------------------------------------

    // default bypasses construction, so the guards never run: it carries
    // (NoDouble, NoDouble) — whose Taker is not a valid taker action. Pinned
    // so the documented caveat stays true; shared with CubeDecisionPair,
    // Play, and DiceRoll.
    [Fact]
    public void Default_IsNonMeaningful_AndNotIncoherent()
    {
        var pair = default(CubeClaimPair);
        Assert.Equal(CubeClaim.NoDouble, pair.Claim);
        Assert.Equal(CubeAction.NoDouble, pair.Taker);
        Assert.False(pair.IsIncoherent);
    }
}
