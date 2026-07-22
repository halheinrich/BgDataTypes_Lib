using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

public class CubeDecisionPairTests
{
    // ---------------------------------------------------------------------
    //  Construction + member access — every valid half combination
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(CubeAction.NoDouble, CubeAction.Take)]
    [InlineData(CubeAction.NoDouble, CubeAction.Pass)]
    [InlineData(CubeAction.Double,   CubeAction.Take)]
    [InlineData(CubeAction.Double,   CubeAction.Pass)]
    public void Construct_ValidHalves_ExposesMembers(CubeAction doubler, CubeAction taker)
    {
        var pair = new CubeDecisionPair(doubler, taker);

        Assert.Equal(doubler, pair.Doubler);
        Assert.Equal(taker, pair.Taker);
    }

    // ---------------------------------------------------------------------
    //  Deconstruction — positional form yields both halves
    // ---------------------------------------------------------------------

    [Fact]
    public void Deconstruct_YieldsBothHalves()
    {
        var pair = new CubeDecisionPair(CubeAction.Double, CubeAction.Pass);

        var (doubler, taker) = pair;

        Assert.Equal(CubeAction.Double, doubler);
        Assert.Equal(CubeAction.Pass, taker);
    }

    // ---------------------------------------------------------------------
    //  Value equality — record-struct structural semantics
    // ---------------------------------------------------------------------

    [Fact]
    public void Equality_SameHalves_AreEqual()
    {
        var a = new CubeDecisionPair(CubeAction.Double, CubeAction.Take);
        var b = new CubeDecisionPair(CubeAction.Double, CubeAction.Take);

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Theory]
    // Differ in one half or both — never equal.
    [InlineData(CubeAction.Double,   CubeAction.Take, CubeAction.NoDouble, CubeAction.Take)]
    [InlineData(CubeAction.Double,   CubeAction.Take, CubeAction.Double,   CubeAction.Pass)]
    [InlineData(CubeAction.NoDouble, CubeAction.Pass, CubeAction.Double,   CubeAction.Take)]
    public void Equality_DifferentHalves_AreNotEqual(
        CubeAction aDoubler, CubeAction aTaker, CubeAction bDoubler, CubeAction bTaker)
    {
        var a = new CubeDecisionPair(aDoubler, aTaker);
        var b = new CubeDecisionPair(bDoubler, bTaker);

        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }

    // ---------------------------------------------------------------------
    //  Half-guards — a cross-half value throws on construction
    // ---------------------------------------------------------------------

    [Theory]
    // The doubler half rejects taker-only actions.
    [InlineData(CubeAction.Take)]
    [InlineData(CubeAction.Pass)]
    public void Construct_NonDoublerDoubler_Throws(CubeAction invalidDoubler)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CubeDecisionPair(invalidDoubler, CubeAction.Take));
    }

    [Theory]
    // The taker half rejects doubler-only actions.
    [InlineData(CubeAction.Double)]
    [InlineData(CubeAction.NoDouble)]
    public void Construct_NonTakerTaker_Throws(CubeAction invalidTaker)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CubeDecisionPair(CubeAction.Double, invalidTaker));
    }

    // ---------------------------------------------------------------------
    //  Canonical instances — the closed 2×2 of valid pairs
    // ---------------------------------------------------------------------

    public static TheoryData<CubeDecisionPair, CubeAction, CubeAction> CanonicalInstances => new()
    {
        { CubeDecisionPair.NoDoubleTake, CubeAction.NoDouble, CubeAction.Take },
        { CubeDecisionPair.TooGood,      CubeAction.NoDouble, CubeAction.Pass },
        { CubeDecisionPair.DoubleTake,   CubeAction.Double,   CubeAction.Take },
        { CubeDecisionPair.DoublePass,   CubeAction.Double,   CubeAction.Pass },
    };

    [Theory]
    [MemberData(nameof(CanonicalInstances))]
    public void CanonicalInstance_HasExpectedHalves(
        CubeDecisionPair instance, CubeAction doubler, CubeAction taker)
    {
        Assert.Equal(doubler, instance.Doubler);
        Assert.Equal(taker, instance.Taker);
    }

    // ---------------------------------------------------------------------
    //  Too-Good classification — true for (NoDouble, Pass) only
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(CubeAction.NoDouble, CubeAction.Take, false)]
    [InlineData(CubeAction.NoDouble, CubeAction.Pass, true)]
    [InlineData(CubeAction.Double,   CubeAction.Take, false)]
    [InlineData(CubeAction.Double,   CubeAction.Pass, false)]
    public void IsTooGood_TrueForNoDoublePassOnly(
        CubeAction doubler, CubeAction taker, bool expected)
    {
        var pair = new CubeDecisionPair(doubler, taker);

        Assert.Equal(expected, pair.IsTooGood);
    }

    [Fact]
    // Locks the documented caveat: the non-meaningful default —
    // (NoDouble, NoDouble) — is not classified as too good.
    public void IsTooGood_DefaultValue_IsFalse()
    {
        Assert.False(default(CubeDecisionPair).IsTooGood);
    }
}
