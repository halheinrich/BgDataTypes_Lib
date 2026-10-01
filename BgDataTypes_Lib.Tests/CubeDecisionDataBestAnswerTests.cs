using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Pins the truth among the four cube answers (SPEC-scoring §3, amended
/// 2026-09-30 and 2026-10-01 on halheinrich/backgammon#326),
/// <see cref="CubeDecisionData.BestAnswer"/>: Double / Take or Double / Pass
/// when doubling is best, by the taker's best response; otherwise the fourth
/// answer when they'd pass, and No double when they'd take — with the two
/// halves' tie-breaks, not doubling and the pass. In every region of §3's
/// tables, at each boundary, and from the equities alone. These synthetic
/// pins are this library's evidence for the rule, and they gate; it replaced
/// the claim derivation and its pair, whose suite this was.
/// </summary>
public class CubeDecisionDataBestAnswerTests
{
    private static CubeAnswer TruthAt(double noDoubleEquity, double doubleTakeEquity) =>
        TestRecords.CubeData(noDoubleEquity: noDoubleEquity, doubleTakeEquity: doubleTakeEquity).BestAnswer;

    // ---------------------------------------------------------------------
    //  Every region
    // ---------------------------------------------------------------------

    [Theory]
    // (N, T, truth). Double / Take: T < 1, T > N.
    [InlineData(0.30, 0.60, CubeAnswer.DoubleTake)]
    [InlineData(-0.20, 0.10, CubeAnswer.DoubleTake)]
    [InlineData(0.6222, 0.9829, CubeAnswer.DoubleTake)]
    // No double / Take: T < 1, T <= N, with N below 1 or above it — playing
    // on beats being taken whatever it is worth (the 2026-09-02 ruling).
    [InlineData(0.50, 0.40, CubeAnswer.NoDouble)]
    [InlineData(1.1711, 0.6004, CubeAnswer.NoDouble)]
    // Double / Pass: T >= 1, N < 1.
    [InlineData(0.50, 1.20, CubeAnswer.DoublePass)]
    [InlineData(0.9413, 1.1528, CubeAnswer.DoublePass)]
    // The fourth answer, playing on beating the cash: T >= 1, N > 1.
    [InlineData(1.30, 1.50, CubeAnswer.NoDoublePass)]
    [InlineData(1.0793, 1.2989, CubeAnswer.NoDoublePass)]
    // The tie (halheinrich/backgammon#293): T >= 1, N = 1.
    [InlineData(1.0, 1.20, CubeAnswer.NoDoublePass)]
    [InlineData(1.0, 2.0267, CubeAnswer.NoDoublePass)]
    public void TheTruth_InEveryRegion(double noDoubleEquity, double doubleTakeEquity, CubeAnswer expected)
    {
        Assert.Equal(expected, TruthAt(noDoubleEquity, doubleTakeEquity));
    }

    // ---------------------------------------------------------------------
    //  The boundaries, and the tie-breaks that decide them
    // ---------------------------------------------------------------------

    [Theory]
    // T = 1: take and pass are worth the same; the pass is the
    // representative, so a double is Double / Pass and no double the fourth.
    [InlineData(0.50, 1.0, CubeAnswer.DoublePass)]
    [InlineData(1.30, 1.0, CubeAnswer.NoDoublePass)]
    // T = N < 1: doubling and not doubling are worth the same; not doubling
    // is the representative.
    [InlineData(0.60, 0.60, CubeAnswer.NoDouble)]
    [InlineData(-0.40, -0.40, CubeAnswer.NoDouble)]
    // N = 1 with a pass: the tie, where doubling is worth the cash, as not
    // doubling is.
    [InlineData(1.0, 1.0, CubeAnswer.NoDoublePass)]
    [InlineData(1.0, 1.0000001, CubeAnswer.NoDoublePass)]
    // N exactly 1 with a take: playing on beats being taken.
    [InlineData(1.0, 0.60, CubeAnswer.NoDouble)]
    [InlineData(1.0, 0.9999999, CubeAnswer.NoDouble)]
    // Just off the boundaries, each way.
    [InlineData(0.9999999, 1.20, CubeAnswer.DoublePass)]
    [InlineData(1.0000001, 1.20, CubeAnswer.NoDoublePass)]
    [InlineData(0.5999999, 0.60, CubeAnswer.DoubleTake)]
    public void TheTruth_AtTheBoundaries(double noDoubleEquity, double doubleTakeEquity, CubeAnswer expected)
    {
        Assert.Equal(expected, TruthAt(noDoubleEquity, doubleTakeEquity));
    }

    // The truth is the answer whose projections are the two halves' best
    // actions, at every point of a grid holding each boundary.
    [Fact]
    public void TheTruth_ProjectsTheBestActions()
    {
        double[] equities = [-1.0, -0.30, 0.0, 0.40, 0.60, 0.999, 1.0, 1.001, 1.1711, 1.50, 2.0267];

        foreach (var nd in equities)
            foreach (var dt in equities)
            {
                var data = TestRecords.CubeData(noDoubleEquity: nd, doubleTakeEquity: dt);
                Assert.Equal(data.BestDoublerAction, data.BestAnswer.DoublerAction());
                Assert.Equal(data.BestTakerAction, data.BestAnswer.TakerAction());
            }
    }

    // ---------------------------------------------------------------------
    //  From the equities alone
    // ---------------------------------------------------------------------

    // Whether gammons are possible decides what the truth reads as and what
    // each answer costs, never which answer it is.
    [Theory]
    [InlineData(0.30, 0.60)]
    [InlineData(0.50, 0.40)]
    [InlineData(0.50, 1.20)]
    [InlineData(1.30, 1.50)]
    [InlineData(1.0, 1.20)]
    public void TheTruth_IsTheSame_WhetherGammonsArePossibleOrNot(double noDoubleEquity, double doubleTakeEquity)
    {
        Assert.Equal(
            CubeAt.GammonsPossible(noDoubleEquity, doubleTakeEquity).Decision.BestAnswer,
            CubeAt.GammonsNotPossible(noDoubleEquity, doubleTakeEquity).Decision.BestAnswer);
    }

    // ---------------------------------------------------------------------
    //  A cube decision's member only, and serialization posture
    // ---------------------------------------------------------------------

    [Fact]
    public void BestAnswer_IsNotAMemberOfACheckerPlay()
    {
        Assert.NotNull(typeof(CubeDecisionData).GetProperty(nameof(CubeDecisionData.BestAnswer)));
        Assert.Null(typeof(CheckerPlayDecisionData).GetProperty(nameof(CubeDecisionData.BestAnswer)));
        Assert.Null(typeof(CheckerPlayDecision).GetProperty(nameof(CubeDecisionData.BestAnswer)));
    }

    // A derivation, not wire: [JsonIgnore] keeps it off the document, as it
    // does the best actions.
    [Fact]
    public void BestAnswer_IsNotSerialised_BothPaths()
    {
        foreach (var (_, options) in WirePaths.Both)
            Assert.DoesNotContain("BestAnswer", JsonSerializer.Serialize(TestRecords.CubeData(), options));
    }
}
