using System.Reflection;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Pins the claim reading of SPEC-scoring §3 (amended 2026-10-01 on
/// halheinrich/backgammon#326), <see cref="CubeDecision.ClaimOf"/>: what a cube
/// answer reads as at a decision. No double reads No double, the two doubling
/// answers read Double, and the fourth reads Too good where gammons are
/// possible and No double where they are not — which of its two labels
/// applies, Too good or No double / Pass. Through full decisions, since the
/// gammon fact is the record's.
/// </summary>
public class CubeDecisionClaimOfTests
{
    [Theory]
    [InlineData(CubeAnswer.NoDouble, true, CubeClaim.NoDouble)]
    [InlineData(CubeAnswer.DoubleTake, true, CubeClaim.Double)]
    [InlineData(CubeAnswer.DoublePass, true, CubeClaim.Double)]
    [InlineData(CubeAnswer.NoDoublePass, true, CubeClaim.TooGood)]
    [InlineData(CubeAnswer.NoDouble, false, CubeClaim.NoDouble)]
    [InlineData(CubeAnswer.DoubleTake, false, CubeClaim.Double)]
    [InlineData(CubeAnswer.DoublePass, false, CubeClaim.Double)]
    [InlineData(CubeAnswer.NoDoublePass, false, CubeClaim.NoDouble)]
    public void EachAnswer_ReadsItsClaim(CubeAnswer answer, bool gammonsPossible, CubeClaim expected)
    {
        Assert.Equal(expected, CubeAt.Decision(gammonsPossible, 0.512, 0.634).ClaimOf(answer));
    }

    // The fourth answer's label choice follows each gate of the gammon fact:
    // closing any one of them turns its reading from Too good to No double,
    // with its pass — the No double / Pass label.
    [Fact]
    public void TheFourthAnswersLabel_FollowsEachGate()
    {
        var open = TestRecords.Cube();
        CubeDecision[] closed =
        [
            CubeAt.GammonsNotPossible(0.512, 0.634),
            TestRecords.Cube(position: TestRecords.Position(
                mop: CubeAt.Race, cubeSize: 2, cubeOwner: CubeOwner.OnRoll,
                session: TestRecords.MatchSession(onRollNeeds: 2, opponentNeeds: 4))),
            TestRecords.Cube(position: TestRecords.Position(
                mop: CubeAt.Race, session: TestRecords.MoneySession(isJacoby: true))),
        ];

        Assert.Equal(CubeClaim.TooGood, open.ClaimOf(CubeAnswer.NoDoublePass));
        Assert.All(closed, cube => Assert.Equal(CubeClaim.NoDouble, cube.ClaimOf(CubeAnswer.NoDoublePass)));
    }

    // The reading is the answer's and the gammon fact's: the equities decide
    // nothing about it, wherever they place the decision.
    [Theory]
    [InlineData(0.30, 0.60)]   // Double / Take is right
    [InlineData(0.50, 0.40)]   // No double / Take
    [InlineData(0.50, 1.20)]   // Double / Pass
    [InlineData(1.30, 1.50)]   // the fourth, playing on beating the cash
    [InlineData(1.00, 1.20)]   // the tie
    public void TheReading_DoesNotDependOnTheEquities(double noDoubleEquity, double doubleTakeEquity)
    {
        foreach (var answer in Enum.GetValues<CubeAnswer>())
        {
            Assert.Equal(CubeAt.GammonsPossible(0.512, 0.634).ClaimOf(answer),
                CubeAt.GammonsPossible(noDoubleEquity, doubleTakeEquity).ClaimOf(answer));
            Assert.Equal(CubeAt.GammonsNotPossible(0.512, 0.634).ClaimOf(answer),
                CubeAt.GammonsNotPossible(noDoubleEquity, doubleTakeEquity).ClaimOf(answer));
        }
    }

    // The truth's claim is the reading of the truth. At the tie of
    // halheinrich/backgammon#293 (no-double equity exactly 1, with a pass)
    // the truth is the fourth answer, so it reads Too good where gammons are
    // possible and No double / Pass where they are not.
    [Fact]
    public void TheTruthAtTheTie_ReadsByTheGammonFact()
    {
        var possible = CubeAt.GammonsPossible(1.0, 1.20);
        var notPossible = CubeAt.GammonsNotPossible(1.0, 2.0267);

        Assert.Equal(CubeClaim.TooGood, possible.ClaimOf(possible.Decision.BestAnswer));
        Assert.Equal(CubeClaim.NoDouble, notPossible.ClaimOf(notPossible.Decision.BestAnswer));
    }

    [Fact]
    public void ClaimOf_RefusesAValueOutsideTheFour()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => TestRecords.Cube().ClaimOf((CubeAnswer)4));
        Assert.Equal("answer", ex.ParamName);
    }

    // Derived once, and nowhere else: ClaimOf is the one public member of the
    // library handing out a claim, and the category's equities-only claim it
    // replaced is gone.
    [Fact]
    public void ClaimOf_IsTheOneDerivationOfAClaim()
    {
        var handingOutAClaim = typeof(CubeDecision).Assembly.GetExportedTypes()
            .SelectMany(t => t.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(m => m switch
            {
                MethodInfo method => method.ReturnType == typeof(CubeClaim),
                PropertyInfo property => property.PropertyType == typeof(CubeClaim),
                _ => false,
            })
            .Select(m => $"{m.DeclaringType!.Name}.{m.Name}");

        Assert.Equal(["CubeDecision.ClaimOf"], handingOutAClaim);
        Assert.Null(typeof(CubeDecisionData).GetProperty("BestDoublerClaim"));
        Assert.Null(typeof(CubeDecisionData).GetProperty("BestClaimPair"));
    }
}
