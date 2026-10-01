using System.Globalization;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Pins the display precision and the zero rule of halheinrich/backgammon#202
/// (folded into halheinrich/backgammon#326, SPEC-scoring §3 as amended
/// 2026-10-01): an equity loss shows at four decimals, and counts as zero
/// exactly when it shows as <c>0.0000</c>, below 0.00005. Just below, at and
/// just above the threshold through the shared formatting, and the two
/// members' agreement everywhere a test can reach.
/// </summary>
public class EquityLossTests
{
    // The double nearest 0.00005 lies just above it, so it shows as 0.0001:
    // "below 0.00005" leaves it out.
    private const double Threshold = 0.00005;

    // ---------------------------------------------------------------------
    //  The display
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(0.0, "0.0000")]
    [InlineData(0.0343, "0.0343")]
    [InlineData(0.7991999999999999, "0.7992")]
    [InlineData(1.0267, "1.0267")]
    [InlineData(0.36065, "0.3607")]
    [InlineData(12.5, "12.5000")]
    public void Format_ShowsFourDecimals(double loss, string expected)
    {
        Assert.Equal(expected, EquityLoss.Format(loss));
    }

    // Invariant whatever the thread's culture: a German culture would write
    // a comma.
    [Fact]
    public void Format_IsCultureInvariant()
    {
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            Assert.Equal("0.1234", EquityLoss.Format(0.1234));
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }

    // A loss that rounds to zero shows unsigned, whatever its sign: a negative
    // zero, or an analyser's stored error a hair below zero. A negative one
    // beyond that shows its sign, as the data states it.
    [Theory]
    [InlineData(-0.0, "0.0000")]
    [InlineData(-0.00001, "0.0000")]
    [InlineData(-0.0000499999, "0.0000")]
    [InlineData(-0.00005, "-0.0001")]
    [InlineData(-0.05, "-0.0500")]
    public void Format_ShowsANegativeAsTheDataStatesIt(double loss, string expected)
    {
        Assert.Equal(expected, EquityLoss.Format(loss));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void BothMembers_RefuseANumberThatIsNotFinite(double loss)
    {
        Assert.Equal("loss", Assert.Throws<ArgumentOutOfRangeException>(() => EquityLoss.Format(loss)).ParamName);
        Assert.Equal("loss", Assert.Throws<ArgumentOutOfRangeException>(() => EquityLoss.CountsAsZero(loss)).ParamName);
    }

    // ---------------------------------------------------------------------
    //  The zero rule, at the threshold
    // ---------------------------------------------------------------------

    [Fact]
    public void JustBelowTheThreshold_ShowsAsZero_AndCountsAsZero()
    {
        foreach (var loss in new[] { 0.0000499999, Math.BitDecrement(Threshold) })
        {
            Assert.Equal("0.0000", EquityLoss.Format(loss));
            Assert.True(EquityLoss.CountsAsZero(loss), $"{loss:R}");
        }
    }

    [Fact]
    public void AtTheThreshold_ShowsAsAPositiveLoss_AndDoesNotCountAsZero()
    {
        Assert.Equal("0.0001", EquityLoss.Format(Threshold));
        Assert.False(EquityLoss.CountsAsZero(Threshold));
    }

    [Fact]
    public void JustAboveTheThreshold_ShowsAsAPositiveLoss_AndDoesNotCountAsZero()
    {
        foreach (var loss in new[] { 0.0000500001, Math.BitIncrement(Threshold) })
        {
            Assert.Equal("0.0001", EquityLoss.Format(loss));
            Assert.False(EquityLoss.CountsAsZero(loss), $"{loss:R}");
        }
    }

    // ---------------------------------------------------------------------
    //  What is shown and what is judged cannot disagree
    // ---------------------------------------------------------------------

    [Fact]
    public void CountsAsZero_IsExactlyShowingAsZero()
    {
        double[] losses =
        [
            0.0, -0.0, double.Epsilon, 1.5e-6, 0.0000015, 0.00001, 0.0000499999,
            Math.BitDecrement(Threshold), Threshold, Math.BitIncrement(Threshold),
            0.0000500001, 0.0001, 0.00015, 0.0343, 1.0, 1e300,
            -0.00001, Math.BitIncrement(-Threshold), -Threshold, -0.05, -1e300,
        ];

        foreach (var loss in losses)
            Assert.True(
                EquityLoss.CountsAsZero(loss) == (EquityLoss.Format(loss) == "0.0000"),
                $"{loss:R} shows as {EquityLoss.Format(loss)}");
    }

    // A sum is judged as a sum: two parts that each count as zero can add up
    // to a cost that does not, which is why a whole answer is judged on its
    // total and each part on its own (SPEC-scoring §3).
    [Fact]
    public void TwoPartsCountingAsZero_CanSumToACostThatDoesNot()
    {
        const double part = 0.00003;

        Assert.True(EquityLoss.CountsAsZero(part));
        Assert.False(EquityLoss.CountsAsZero(part + part));
        Assert.Equal("0.0001", EquityLoss.Format(part + part));
    }

    // The rule decides verdicts only: a cost it judges as zero keeps its
    // exact value (the Double / Pass cost a hair above the cash, pinned in
    // CubeDecisionCostOfTests).
    [Fact]
    public void TheRule_ChangesNoNumber()
    {
        const double raw = 1.0000015 - 1.0;

        Assert.True(EquityLoss.CountsAsZero(raw));
        Assert.NotEqual(0.0, raw);
    }
}
