using System.Globalization;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Pins the display precision and the zero rule of halheinrich/backgammon#202
/// (folded into halheinrich/backgammon#326, SPEC-scoring §3 as amended
/// 2026-10-01): an equity loss shows at four decimals, and counts as zero
/// exactly when it shows as <c>0.0000</c>, below 0.00005. Just below, at and
/// just above the threshold through the shared formatting, and the two loss
/// members' agreement everywhere a test can reach. Then the signed display of
/// an equity at the same precision (halheinrich/backgammon#326, step 3a): its
/// sign, its zero, and its digits, which are the loss display's.
/// </summary>
public class EquityDisplayTests
{
    // The double nearest 0.00005 lies just above it, so it shows as 0.0001:
    // "below 0.00005" leaves it out.
    private const double Threshold = 0.00005;

    // ---------------------------------------------------------------------
    //  The display of a loss
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(0.0, "0.0000")]
    [InlineData(0.0343, "0.0343")]
    [InlineData(0.7991999999999999, "0.7992")]
    [InlineData(1.0267, "1.0267")]
    [InlineData(0.36065, "0.3607")]
    [InlineData(12.5, "12.5000")]
    public void FormatLoss_ShowsFourDecimals(double loss, string expected)
    {
        Assert.Equal(expected, EquityDisplay.FormatLoss(loss));
    }

    // Invariant whatever the thread's culture: a German culture would write
    // a comma.
    [Fact]
    public void BothDisplays_AreCultureInvariant()
    {
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            Assert.Equal("0.1234", EquityDisplay.FormatLoss(0.1234));
            Assert.Equal("-0.1234", EquityDisplay.FormatEquity(-0.1234));
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
    public void FormatLoss_ShowsANegativeAsTheDataStatesIt(double loss, string expected)
    {
        Assert.Equal(expected, EquityDisplay.FormatLoss(loss));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void BothLossMembers_RefuseANumberThatIsNotFinite(double loss)
    {
        Assert.Equal("loss", Assert.Throws<ArgumentOutOfRangeException>(() => EquityDisplay.FormatLoss(loss)).ParamName);
        Assert.Equal("loss", Assert.Throws<ArgumentOutOfRangeException>(() => EquityDisplay.CountsAsZero(loss)).ParamName);
    }

    // ---------------------------------------------------------------------
    //  The zero rule, at the threshold
    // ---------------------------------------------------------------------

    [Fact]
    public void JustBelowTheThreshold_ShowsAsZero_AndCountsAsZero()
    {
        foreach (var loss in new[] { 0.0000499999, Math.BitDecrement(Threshold) })
        {
            Assert.Equal("0.0000", EquityDisplay.FormatLoss(loss));
            Assert.True(EquityDisplay.CountsAsZero(loss), $"{loss:R}");
        }
    }

    [Fact]
    public void AtTheThreshold_ShowsAsAPositiveLoss_AndDoesNotCountAsZero()
    {
        Assert.Equal("0.0001", EquityDisplay.FormatLoss(Threshold));
        Assert.False(EquityDisplay.CountsAsZero(Threshold));
    }

    [Fact]
    public void JustAboveTheThreshold_ShowsAsAPositiveLoss_AndDoesNotCountAsZero()
    {
        foreach (var loss in new[] { 0.0000500001, Math.BitIncrement(Threshold) })
        {
            Assert.Equal("0.0001", EquityDisplay.FormatLoss(loss));
            Assert.False(EquityDisplay.CountsAsZero(loss), $"{loss:R}");
        }
    }

    // ---------------------------------------------------------------------
    //  What is shown and what is judged cannot disagree
    // ---------------------------------------------------------------------

    // Values on both sides of the threshold and of zero, shared by the
    // agreement checks below.
    private static readonly double[] Reach =
    [
        0.0, -0.0, double.Epsilon, -double.Epsilon, 1.5e-6, 0.0000015, 0.00001, 0.0000499999,
        Math.BitDecrement(Threshold), Threshold, Math.BitIncrement(Threshold),
        0.0000500001, 0.0001, 0.00015, 0.0343, 0.6004, 1.0, 2.0267, 1e300,
        -0.00001, Math.BitIncrement(-Threshold), -Threshold, Math.BitDecrement(-Threshold),
        -0.05, -0.3, -1.0, -1e300,
    ];

    [Fact]
    public void CountsAsZero_IsExactlyShowingAsZero()
    {
        foreach (var loss in Reach)
            Assert.True(
                EquityDisplay.CountsAsZero(loss) == (EquityDisplay.FormatLoss(loss) == "0.0000"),
                $"{loss:R} shows as {EquityDisplay.FormatLoss(loss)}");
    }

    // A sum is judged as a sum: two parts that each count as zero can add up
    // to a cost that does not, which is why a whole answer is judged on its
    // total and each part on its own (SPEC-scoring §3).
    [Fact]
    public void TwoPartsCountingAsZero_CanSumToACostThatDoesNot()
    {
        const double part = 0.00003;

        Assert.True(EquityDisplay.CountsAsZero(part));
        Assert.False(EquityDisplay.CountsAsZero(part + part));
        Assert.Equal("0.0001", EquityDisplay.FormatLoss(part + part));
    }

    // The rule decides verdicts only: a cost it judges as zero keeps its
    // exact value (the Double / Pass cost a hair above the cash, pinned in
    // CubeDecisionCostOfTests).
    [Fact]
    public void TheRule_ChangesNoNumber()
    {
        const double raw = 1.0000015 - 1.0;

        Assert.True(EquityDisplay.CountsAsZero(raw));
        Assert.NotEqual(0.0, raw);
    }

    // ---------------------------------------------------------------------
    //  The display of an equity, with its sign
    // ---------------------------------------------------------------------

    // Every equity shows a sign: + where the figure shows no minus.
    [Theory]
    [InlineData(0.6004, "+0.6004")]
    [InlineData(1.1711, "+1.1711")]
    [InlineData(2.0267, "+2.0267")]
    [InlineData(1.0, "+1.0000")]
    [InlineData(0.00005, "+0.0001")]
    [InlineData(-0.3, "-0.3000")]
    [InlineData(-1.0, "-1.0000")]
    [InlineData(-0.00005, "-0.0001")]
    public void FormatEquity_ShowsItsSign(double equity, string expected)
    {
        Assert.Equal(expected, EquityDisplay.FormatEquity(equity));
    }

    // An equity of zero, a negative zero, and a value just either side of
    // zero that rounds to it all show +0.0000: never -0.0000, never +-0.0000.
    [Fact]
    public void AnEquityRoundingToZero_ShowsPlusZero()
    {
        double[] zeros =
        [
            0.0, -0.0, double.Epsilon, -double.Epsilon, 0.00001, -0.00001, 0.0000499999, -0.0000499999,
            Math.BitDecrement(Threshold), Math.BitIncrement(-Threshold),
        ];

        foreach (var equity in zeros)
            Assert.Equal("+0.0000", EquityDisplay.FormatEquity(equity));
    }

    // The digits are the loss display's for the same number: one precision,
    // one rounding. An equity adds only the sign rule, so its text is the
    // loss text with + ahead of a figure showing no minus, and an equity of
    // either sign shows the digits the loss display gives its magnitude.
    [Fact]
    public void FormatEquity_SharesFormatLossDigits()
    {
        foreach (var value in Reach)
        {
            string loss = EquityDisplay.FormatLoss(value);
            string equity = EquityDisplay.FormatEquity(value);
            Assert.Equal(loss.StartsWith('-') ? loss : "+" + loss, equity);
            Assert.Equal(EquityDisplay.FormatLoss(Math.Abs(value)), equity[1..]);
            Assert.Equal(EquityDisplay.FormatLoss(Math.Abs(value)), EquityDisplay.FormatEquity(-value)[1..]);
        }
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void FormatEquity_RefusesANumberThatIsNotFinite(double equity)
    {
        Assert.Equal("equity", Assert.Throws<ArgumentOutOfRangeException>(() => EquityDisplay.FormatEquity(equity)).ParamName);
    }
}
