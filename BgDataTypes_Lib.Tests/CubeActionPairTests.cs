using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

public class CubeActionPairTests
{
    [Theory]
    [InlineData(CubeVerdict.NoDouble,   CubeAction.NoDouble, CubeAction.Take)]
    [InlineData(CubeVerdict.DoubleTake, CubeAction.Double,   CubeAction.Take)]
    [InlineData(CubeVerdict.DoublePass, CubeAction.Double,   CubeAction.Pass)]
    [InlineData(CubeVerdict.TooGood,    CubeAction.NoDouble, CubeAction.Pass)]
    public void FromVerdict_ProducesExpectedPair(
        CubeVerdict verdict, CubeAction expectedDoubler, CubeAction expectedResponder)
    {
        var pair = CubeActionPair.FromVerdict(verdict);
        Assert.Equal(new CubeActionPair(expectedDoubler, expectedResponder), pair);
    }

    [Theory]
    [InlineData(CubeAction.NoDouble, CubeAction.Take, CubeVerdict.NoDouble)]
    [InlineData(CubeAction.Double,   CubeAction.Take, CubeVerdict.DoubleTake)]
    [InlineData(CubeAction.Double,   CubeAction.Pass, CubeVerdict.DoublePass)]
    [InlineData(CubeAction.NoDouble, CubeAction.Pass, CubeVerdict.TooGood)]
    public void ToVerdict_ProducesExpectedVerdict(
        CubeAction doubler, CubeAction responder, CubeVerdict expectedVerdict)
    {
        var verdict = new CubeActionPair(doubler, responder).ToVerdict();
        Assert.Equal(expectedVerdict, verdict);
    }

    [Theory]
    [InlineData(CubeVerdict.NoDouble)]
    [InlineData(CubeVerdict.DoubleTake)]
    [InlineData(CubeVerdict.DoublePass)]
    [InlineData(CubeVerdict.TooGood)]
    public void FromVerdict_ToVerdict_RoundTrips(CubeVerdict verdict)
    {
        var roundTripped = CubeActionPair.FromVerdict(verdict).ToVerdict();
        Assert.Equal(verdict, roundTripped);
    }

    [Theory]
    [InlineData(CubeAction.NoDouble, CubeAction.Take, CubeVerdict.NoDouble)]
    [InlineData(CubeAction.Double,   CubeAction.Take, CubeVerdict.DoubleTake)]
    [InlineData(CubeAction.Double,   CubeAction.Pass, CubeVerdict.DoublePass)]
    [InlineData(CubeAction.NoDouble, CubeAction.Pass, CubeVerdict.TooGood)]
    public void TryToVerdict_OnValidPair_ReturnsTrue(
        CubeAction doubler, CubeAction responder, CubeVerdict expectedVerdict)
    {
        var pair = new CubeActionPair(doubler, responder);
        Assert.True(CubeActionPair.TryToVerdict(pair, out var verdict));
        Assert.Equal(expectedVerdict, verdict);
    }

    // Representative coverage of the twelve invalid pairs: an action repeated
    // on both halves, a responder action where the doubler belongs, and a
    // doubler action where the responder belongs. Exhausting all twelve adds
    // no signal — the inverse derives from FromVerdict, so any pair the
    // forward map does not produce is rejected uniformly.
    [Theory]
    [InlineData(CubeAction.NoDouble, CubeAction.NoDouble)]
    [InlineData(CubeAction.Double,   CubeAction.Double)]
    [InlineData(CubeAction.Take,     CubeAction.Pass)]
    [InlineData(CubeAction.Pass,     CubeAction.Take)]
    [InlineData(CubeAction.Take,     CubeAction.Take)]
    [InlineData(CubeAction.Pass,     CubeAction.Pass)]
    public void TryToVerdict_OnInvalidPair_ReturnsFalse(
        CubeAction doubler, CubeAction responder)
    {
        var pair = new CubeActionPair(doubler, responder);
        Assert.False(CubeActionPair.TryToVerdict(pair, out var verdict));
        Assert.Equal(default, verdict);
    }

    [Theory]
    [InlineData(CubeAction.NoDouble, CubeAction.NoDouble)]
    [InlineData(CubeAction.Double,   CubeAction.Double)]
    [InlineData(CubeAction.Take,     CubeAction.Pass)]
    [InlineData(CubeAction.Pass,     CubeAction.Take)]
    [InlineData(CubeAction.Take,     CubeAction.Take)]
    [InlineData(CubeAction.Pass,     CubeAction.Pass)]
    public void ToVerdict_OnInvalidPair_Throws(
        CubeAction doubler, CubeAction responder)
    {
        var pair = new CubeActionPair(doubler, responder);
        Assert.Throws<ArgumentException>(() => pair.ToVerdict());
    }

    [Fact]
    public void FromVerdict_OnUnknownEnum_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CubeActionPair.FromVerdict((CubeVerdict)42));
    }
}
