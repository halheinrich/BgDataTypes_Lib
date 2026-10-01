using System.Reflection;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Pins what each cube answer costs, in its two parts
/// (<see cref="CubeDecision.CostOf"/>), against SPEC-scoring §3's two tables
/// (amended 2026-09-30 and 2026-10-01 on halheinrich/backgammon#326): every
/// cell of each, the boundaries between their rows, and the positions the
/// thread ruled on. N is the no-double equity and T the double/take equity,
/// the cash +1.
/// </summary>
/// <remarks>
/// Each cell is asserted three ways: each part exactly, as its own rule
/// computes it from N and T; the total exactly as the sum of the parts; and
/// the total against the table's own formula for the cell, within 1e-12,
/// since the table writes some sums simplified ((T − N) + (1 − T) as 1 − N).
/// Every decision is a full record (<see cref="CubeAt"/>): the gammon fact is
/// the position's.
/// </remarks>
public class CubeDecisionCostOfTests
{
    private const double TableTolerance = 1e-12;

    private static void AssertCost(
        CubeDecision cube, CubeAnswer answer, double doublingPart, double takePart, double tableCell)
    {
        var cost = cube.CostOf(answer);
        Assert.Equal(doublingPart, cost.DoublingPart);
        Assert.Equal(takePart, cost.TakePart);
        Assert.Equal(cost.DoublingPart + cost.TakePart, cost.Total);
        Assert.Equal(tableCell, cost.Total, TableTolerance);
    }

    // =====================================================================
    //  Where gammons are possible: the fourth answer reads Too good
    // =====================================================================

    // Double / Take is right (T < 1, T > N): No double T − N, Double / Take
    // 0, Double / Pass 1 − T, Too good 2(1 − T).
    [Theory]
    [InlineData(0.30, 0.60)]
    [InlineData(0.4915, 0.5878)]
    [InlineData(-0.20, 0.10)]
    public void GammonsPossible_DoubleTakeIsRight(double n, double t)
    {
        var cube = CubeAt.GammonsPossible(n, t);

        AssertCost(cube, CubeAnswer.NoDouble, t - n, 0.0, t - n);
        AssertCost(cube, CubeAnswer.DoubleTake, 0.0, 0.0, 0.0);
        AssertCost(cube, CubeAnswer.DoublePass, 0.0, 1 - t, 1 - t);
        AssertCost(cube, CubeAnswer.NoDoublePass, 1 - t, 1 - t, 2 * (1 - t));
    }

    // No double / Take is right (T < 1, T <= N), the no-double equity below
    // the cash or above it: No double 0, Double / Take N − T, Double / Pass
    // (N − T) + (1 − T), Too good 2(1 − T).
    [Theory]
    [InlineData(0.50, 0.40)]
    [InlineData(0.5344, 0.4356)]
    [InlineData(1.1711, 0.6004)]
    [InlineData(1.4973, 0.20)]
    public void GammonsPossible_NoDoubleTakeIsRight(double n, double t)
    {
        var cube = CubeAt.GammonsPossible(n, t);

        AssertCost(cube, CubeAnswer.NoDouble, 0.0, 0.0, 0.0);
        AssertCost(cube, CubeAnswer.DoubleTake, n - t, 0.0, n - t);
        AssertCost(cube, CubeAnswer.DoublePass, n - t, 1 - t, (n - t) + (1 - t));
        AssertCost(cube, CubeAnswer.NoDoublePass, 1 - t, 1 - t, 2 * (1 - t));
    }

    // Double / Pass is right (T >= 1, N < 1): No double 1 − N, Double / Take
    // T − 1, Double / Pass 0, Too good 1 − N.
    [Theory]
    [InlineData(0.50, 1.20)]
    [InlineData(0.9413, 1.1528)]
    public void GammonsPossible_DoublePassIsRight(double n, double t)
    {
        var cube = CubeAt.GammonsPossible(n, t);

        AssertCost(cube, CubeAnswer.NoDouble, 1 - n, 0.0, 1 - n);
        AssertCost(cube, CubeAnswer.DoubleTake, 0.0, t - 1, t - 1);
        AssertCost(cube, CubeAnswer.DoublePass, 0.0, 0.0, 0.0);
        AssertCost(cube, CubeAnswer.NoDoublePass, 1 - n, 0.0, 1 - n);
    }

    // Too good is right (T >= 1, N > 1): No double N − 0.6, the convention;
    // Double / Take (N − 1) + (T − 1); Double / Pass N − 1; Too good 0.
    [Theory]
    [InlineData(1.30, 1.50)]
    [InlineData(1.0793, 1.2989)]
    public void GammonsPossible_TooGoodIsRight(double n, double t)
    {
        var cube = CubeAt.GammonsPossible(n, t);

        AssertCost(cube, CubeAnswer.NoDouble, n - 0.6, 0.0, n - 0.6);
        AssertCost(cube, CubeAnswer.DoubleTake, n - 1, t - 1, (n - 1) + (t - 1));
        AssertCost(cube, CubeAnswer.DoublePass, n - 1, 0.0, n - 1);
        AssertCost(cube, CubeAnswer.NoDoublePass, 0.0, 0.0, 0.0);
    }

    // The tie (T >= 1, N = 1; halheinrich/backgammon#293): No double 0, not
    // the convention; Double / Take T − 1; Double / Pass 0; Too good 0.
    [Theory]
    [InlineData(1.0, 1.20)]
    [InlineData(1.0, 2.0267)]
    public void GammonsPossible_TheTie(double n, double t)
    {
        var cube = CubeAt.GammonsPossible(n, t);

        AssertCost(cube, CubeAnswer.NoDouble, 0.0, 0.0, 0.0);
        AssertCost(cube, CubeAnswer.DoubleTake, 0.0, t - 1, t - 1);
        AssertCost(cube, CubeAnswer.DoublePass, 0.0, 0.0, 0.0);
        AssertCost(cube, CubeAnswer.NoDoublePass, 0.0, 0.0, 0.0);
    }

    // =====================================================================
    //  Where gammons are not possible: the fourth answer reads No double / Pass
    // =====================================================================

    // Double / Take is right: No double T − N, Double / Take 0, Double / Pass
    // 1 − T, No double / Pass 1 − N, its no-double error and its pass error.
    [Theory]
    [InlineData(0.30, 0.60)]
    [InlineData(0.6222, 0.9829)]
    public void GammonsNotPossible_DoubleTakeIsRight(double n, double t)
    {
        var cube = CubeAt.GammonsNotPossible(n, t);

        AssertCost(cube, CubeAnswer.NoDouble, t - n, 0.0, t - n);
        AssertCost(cube, CubeAnswer.DoubleTake, 0.0, 0.0, 0.0);
        AssertCost(cube, CubeAnswer.DoublePass, 0.0, 1 - t, 1 - t);
        AssertCost(cube, CubeAnswer.NoDoublePass, t - n, 1 - t, 1 - n);
    }

    // No double / Take is right: No double 0, Double / Take N − T, Double /
    // Pass (N − T) + (1 − T), No double / Pass 1 − T.
    [Theory]
    [InlineData(0.50, 0.40)]
    [InlineData(1.1711, 0.6004)]
    public void GammonsNotPossible_NoDoubleTakeIsRight(double n, double t)
    {
        var cube = CubeAt.GammonsNotPossible(n, t);

        AssertCost(cube, CubeAnswer.NoDouble, 0.0, 0.0, 0.0);
        AssertCost(cube, CubeAnswer.DoubleTake, n - t, 0.0, n - t);
        AssertCost(cube, CubeAnswer.DoublePass, n - t, 1 - t, (n - t) + (1 - t));
        AssertCost(cube, CubeAnswer.NoDoublePass, 0.0, 1 - t, 1 - t);
    }

    // Double / Pass is right: No double 1 − N, Double / Take T − 1, Double /
    // Pass 0, No double / Pass 1 − N.
    [Theory]
    [InlineData(0.50, 1.20)]
    [InlineData(0.9413, 1.1528)]
    public void GammonsNotPossible_DoublePassIsRight(double n, double t)
    {
        var cube = CubeAt.GammonsNotPossible(n, t);

        AssertCost(cube, CubeAnswer.NoDouble, 1 - n, 0.0, 1 - n);
        AssertCost(cube, CubeAnswer.DoubleTake, 0.0, t - 1, t - 1);
        AssertCost(cube, CubeAnswer.DoublePass, 0.0, 0.0, 0.0);
        AssertCost(cube, CubeAnswer.NoDoublePass, 1 - n, 0.0, 1 - n);
    }

    // No double / Pass is right (T >= 1, N >= 1, the tie included): No double
    // 0, no convention; Double / Take (N − 1) + (T − 1); Double / Pass N − 1;
    // No double / Pass 0.
    [Theory]
    [InlineData(1.30, 1.50)]
    [InlineData(1.0793, 1.2989)]
    [InlineData(1.0, 1.20)]
    [InlineData(1.0, 2.0267)]
    public void GammonsNotPossible_NoDoublePassIsRight(double n, double t)
    {
        var cube = CubeAt.GammonsNotPossible(n, t);

        AssertCost(cube, CubeAnswer.NoDouble, 0.0, 0.0, 0.0);
        AssertCost(cube, CubeAnswer.DoubleTake, n - 1, t - 1, (n - 1) + (t - 1));
        AssertCost(cube, CubeAnswer.DoublePass, n - 1, 0.0, n - 1);
        AssertCost(cube, CubeAnswer.NoDoublePass, 0.0, 0.0, 0.0);
    }

    // =====================================================================
    //  The boundaries
    // =====================================================================

    // T = 1: take and pass both cost 0, so Double / Take and Double / Pass
    // cost the same, whichever side of the cash N is.
    [Theory]
    [InlineData(true, 0.50)]
    [InlineData(true, 1.30)]
    [InlineData(false, 0.50)]
    [InlineData(false, 1.30)]
    public void TakeAndPassBothCostNothing_AtTheTakePoint(bool gammonsPossible, double n)
    {
        var cube = CubeAt.Decision(gammonsPossible, n, 1.0);

        Assert.Equal(0.0, cube.CostOf(CubeAnswer.DoubleTake).TakePart);
        Assert.Equal(0.0, cube.CostOf(CubeAnswer.DoublePass).TakePart);
        Assert.Equal(0.0, cube.CostOf(CubeAnswer.NoDoublePass).TakePart);
        Assert.Equal(cube.CostOf(CubeAnswer.DoubleTake), cube.CostOf(CubeAnswer.DoublePass));
    }

    // T = N < 1: doubling and not doubling are worth the same, so No double
    // and Double / Take both cost 0.
    [Theory]
    [InlineData(true, 0.60)]
    [InlineData(false, 0.60)]
    [InlineData(true, -0.40)]
    public void NoDoubleAndDoubleTakeBothCostNothing_WhereDoublingTies(bool gammonsPossible, double equity)
    {
        var cube = CubeAt.Decision(gammonsPossible, equity, equity);

        Assert.Equal(0.0, cube.CostOf(CubeAnswer.NoDouble).Total);
        Assert.Equal(0.0, cube.CostOf(CubeAnswer.DoubleTake).Total);
    }

    // N exactly 1 with a take: a No double / Take row on both tables. Too
    // good there costs 2(1 − T), and No double / Pass 1 − T.
    [Fact]
    public void NoDoubleEquityExactlyOne_WithATake()
    {
        const double n = 1.0, t = 0.60;
        var possible = CubeAt.GammonsPossible(n, t);
        var notPossible = CubeAt.GammonsNotPossible(n, t);

        foreach (var cube in new[] { possible, notPossible })
        {
            AssertCost(cube, CubeAnswer.NoDouble, 0.0, 0.0, 0.0);
            AssertCost(cube, CubeAnswer.DoubleTake, n - t, 0.0, n - t);
            AssertCost(cube, CubeAnswer.DoublePass, n - t, 1 - t, (n - t) + (1 - t));
        }
        AssertCost(possible, CubeAnswer.NoDoublePass, 1 - t, 1 - t, 2 * (1 - t));
        AssertCost(notPossible, CubeAnswer.NoDoublePass, 0.0, 1 - t, 1 - t);
    }

    // N = 1 with a pass, the tie: No double, Double / Pass and the fourth
    // answer all cost 0 on both tables — more than one correct answer.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheTie_HasThreeAnswersCostingNothing(bool gammonsPossible)
    {
        var cube = CubeAt.Decision(gammonsPossible, 1.0, 1.20);

        Assert.Equal(
            [CubeAnswer.NoDouble, CubeAnswer.DoublePass, CubeAnswer.NoDoublePass],
            Enum.GetValues<CubeAnswer>().Where(a => cube.CostOf(a).Total == 0.0));
    }

    // The No double convention needs the strict region: just above the tie it
    // charges N − 0.6, and at the tie nothing.
    [Fact]
    public void TheNoDoubleConvention_StartsStrictlyAboveTheTie()
    {
        Assert.Equal(0.0, CubeAt.GammonsPossible(1.0, 1.20).CostOf(CubeAnswer.NoDouble).Total);
        Assert.Equal(1.0000001 - 0.6, CubeAt.GammonsPossible(1.0000001, 1.20).CostOf(CubeAnswer.NoDouble).Total);
    }

    // =====================================================================
    //  The conventions apply only where gammons are possible
    // =====================================================================

    // Each gate closed on its own takes both conventions away: No double
    // where Too good would be right, and the fourth answer when they'd take.
    [Fact]
    public void TheConventions_GoWithEachGate()
    {
        CubeDecision At(PositionData position, double n, double t) => TestRecords.Cube(
            position: position,
            decision: TestRecords.CubeData(noDoubleEquity: n, doubleTakeEquity: t));
        PositionData[] closed =
        [
            TestRecords.Position(mop: CubeAt.OpponentBorneOff),
            TestRecords.Position(mop: CubeAt.Race, cubeSize: 2, cubeOwner: CubeOwner.OnRoll,
                session: TestRecords.MatchSession(onRollNeeds: 2, opponentNeeds: 4)),
            TestRecords.Position(mop: CubeAt.Race, session: TestRecords.MoneySession(isJacoby: true)),
        ];
        var open = TestRecords.Position(mop: CubeAt.Race, cubeSize: 2, cubeOwner: CubeOwner.OnRoll,
            session: TestRecords.MatchSession(onRollNeeds: 3, opponentNeeds: 4));

        Assert.Equal(1.30 - 0.6, At(open, 1.30, 1.50).CostOf(CubeAnswer.NoDouble).Total);
        Assert.Equal(1 - 0.60, At(open, 0.50, 0.60).CostOf(CubeAnswer.NoDoublePass).DoublingPart);
        foreach (var position in closed)
        {
            Assert.Equal(0.0, At(position, 1.30, 1.50).CostOf(CubeAnswer.NoDouble).Total);
            Assert.Equal(0.60 - 0.50, At(position, 0.50, 0.60).CostOf(CubeAnswer.NoDoublePass).DoublingPart);
        }
    }

    // =====================================================================
    //  The positions ruled on halheinrich/backgammon#326
    // =====================================================================

    // TooGoodAndTake.xgp, gammons possible: Too good costs 2(1 − T), 0.7992,
    // and No double 0.
    [Fact]
    public void Pin_TooGoodAndTake()
    {
        var cube = CubeAt.GammonsPossible(1.1711, 0.6004);

        Assert.Equal("0.7992", EquityDisplay.FormatLoss(cube.CostOf(CubeAnswer.NoDoublePass).Total));
        Assert.Equal(2 * (1 - 0.6004), cube.CostOf(CubeAnswer.NoDoublePass).Total, TableTolerance);
        Assert.Equal(0.0, cube.CostOf(CubeAnswer.NoDouble).Total);
    }

    // The alipasa position (N +0.6222, T +0.9829), gammons possible: Too good
    // 2(1 − T), less than No double's T − N, as Hal ruled.
    [Fact]
    public void Pin_Alipasa()
    {
        var cube = CubeAt.GammonsPossible(0.6222, 0.9829);

        Assert.Equal(2 * (1 - 0.9829), cube.CostOf(CubeAnswer.NoDoublePass).Total, TableTolerance);
        Assert.Equal(0.9829 - 0.6222, cube.CostOf(CubeAnswer.NoDouble).Total);
        Assert.Equal("0.0342", EquityDisplay.FormatLoss(cube.CostOf(CubeAnswer.NoDoublePass).Total));
        Assert.Equal("0.3607", EquityDisplay.FormatLoss(cube.CostOf(CubeAnswer.NoDouble).Total));
    }

    // The Shimodaira tie (N +1.0000, T +2.0267), gammons not possible — the
    // opponent has a checker borne off: No double 0, Double / Take T − 1 as
    // XG's numbers give it ("Your 1.0267 is correct"), Double / Pass 0, No
    // double / Pass 0.
    [Fact]
    public void Pin_Shimodaira()
    {
        var cube = CubeAt.GammonsNotPossible(1.0, 2.0267);

        Assert.False(cube.GammonsPossible);
        Assert.Equal(0.0, cube.CostOf(CubeAnswer.NoDouble).Total);
        Assert.Equal(2.0267 - 1, cube.CostOf(CubeAnswer.DoubleTake).Total);
        Assert.Equal("1.0267", EquityDisplay.FormatLoss(cube.CostOf(CubeAnswer.DoubleTake).Total));
        Assert.Equal(0.0, cube.CostOf(CubeAnswer.DoublePass).Total);
        Assert.Equal(0.0, cube.CostOf(CubeAnswer.NoDoublePass).Total);
    }

    // XG's no-double equity a hair above the cash (+1.0000015) with gammons
    // not possible: No double 0 and No double / Pass 0, while Double / Pass
    // keeps its raw cost N − 1, never rounded, which the zero rule judges as
    // zero. The same equities with gammons possible are Too good's, and No
    // double pays the convention: the gammon fact changes the policy, not
    // XG's number.
    [Fact]
    public void Pin_AHairAboveTheCash()
    {
        const double n = 1.0000015, t = 1.30;
        var cube = CubeAt.GammonsNotPossible(n, t);
        var doublePass = cube.CostOf(CubeAnswer.DoublePass);

        Assert.Equal(0.0, cube.CostOf(CubeAnswer.NoDouble).Total);
        Assert.Equal(0.0, cube.CostOf(CubeAnswer.NoDoublePass).Total);
        Assert.Equal(n - 1, doublePass.Total);
        Assert.True(doublePass.Total > 0.0);
        Assert.True(EquityDisplay.CountsAsZero(doublePass.Total));
        Assert.Equal("0.0000", EquityDisplay.FormatLoss(doublePass.Total));

        Assert.Equal(n - 0.6, CubeAt.GammonsPossible(n, t).CostOf(CubeAnswer.NoDouble).Total);
    }

    // =====================================================================
    //  Properties over a grid holding every boundary
    // =====================================================================

    private static readonly double[] Grid =
        [-1.0, -0.30, 0.0, 0.40, 0.60, 0.6004, 0.9829, 0.999, 1.0, 1.0000015, 1.001, 1.1711, 1.50, 2.0267];

    // The parts are never negative, add up to the total, and the take part
    // is 0 exactly where the answer commits to no response; and the truth
    // always costs nothing.
    [Fact]
    public void EveryCost_KeepsItsShape_AndTheTruthIsFree()
    {
        foreach (bool gammonsPossible in new[] { true, false })
            foreach (var n in Grid)
                foreach (var t in Grid)
                {
                    var cube = CubeAt.Decision(gammonsPossible, n, t);
                    foreach (var answer in Enum.GetValues<CubeAnswer>())
                    {
                        var cost = cube.CostOf(answer);
                        string at = $"{answer} at N {n}, T {t}, gammons {(gammonsPossible ? "possible" : "not possible")}";
                        Assert.True(cost.DoublingPart >= 0.0 && cost.TakePart >= 0.0, at);
                        Assert.True(cost.Total == cost.DoublingPart + cost.TakePart, at);
                        if (!answer.CommitsToResponse())
                            Assert.True(cost.TakePart == 0.0, at);
                    }
                    Assert.Equal(0.0, cube.CostOf(cube.Decision.BestAnswer).Total);
                }
    }

    // A doubling answer's parts are the analysis's own action errors; the
    // conventions touch only the two no-double answers' doubling parts.
    [Fact]
    public void TheDoublingAnswers_CostTheirActionErrors()
    {
        foreach (bool gammonsPossible in new[] { true, false })
            foreach (var n in Grid)
                foreach (var t in Grid)
                {
                    var cube = CubeAt.Decision(gammonsPossible, n, t);
                    var data = cube.Decision;
                    Assert.Equal(
                        (data.DoublerActionError(CubeAction.Double), data.TakerActionError(CubeAction.Take)),
                        (cube.CostOf(CubeAnswer.DoubleTake).DoublingPart, cube.CostOf(CubeAnswer.DoubleTake).TakePart));
                    Assert.Equal(
                        (data.DoublerActionError(CubeAction.Double), data.TakerActionError(CubeAction.Pass)),
                        (cube.CostOf(CubeAnswer.DoublePass).DoublingPart, cube.CostOf(CubeAnswer.DoublePass).TakePart));
                    Assert.Equal(data.TakerActionError(CubeAction.Pass), cube.CostOf(CubeAnswer.NoDoublePass).TakePart);
                }
    }

    // =====================================================================
    //  The cost type, and the door
    // =====================================================================

    [Fact]
    public void CostOf_RefusesAValueOutsideTheFour()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => TestRecords.Cube().CostOf((CubeAnswer)4));
        Assert.Equal("answer", ex.ParamName);
    }

    // Only the library builds a cost, and only the record hands one out: the
    // category's calculation needs the gammon fact, which is the record's.
    [Fact]
    public void ACost_IsTheRecordsToHandOut()
    {
        Assert.Empty(typeof(CubeAnswerCost).GetConstructors());
        Assert.Null(typeof(CubeDecisionData).GetMethod(nameof(CubeDecision.CostOf), BindingFlags.Public | BindingFlags.Instance));
        Assert.All(typeof(CubeAnswerCost).GetProperties(), p => Assert.Null(p.SetMethod));
    }

    [Fact]
    public void ACost_IsAValue()
    {
        var cube = CubeAt.GammonsPossible(0.30, 0.60);
        var same = CubeAt.GammonsPossible(0.30, 0.60).CostOf(CubeAnswer.NoDoublePass);
        var cost = cube.CostOf(CubeAnswer.NoDoublePass);
        var other = cube.CostOf(CubeAnswer.DoublePass);

        Assert.NotSame(same, cost);
        Assert.True(cost == same);
        Assert.True(cost.Equals((object)same));
        Assert.Equal(cost.GetHashCode(), same.GetHashCode());
        Assert.True(cost != other);
        Assert.False(cost.Equals(null));
        Assert.True((CubeAnswerCost?)null == null);
        Assert.False(cost == null);
    }
}
