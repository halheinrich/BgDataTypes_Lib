using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Pins the four-valued cube answer of SPEC-scoring §3 (amended 2026-09-30 on
/// halheinrich/backgammon#326): exactly four values, offered in the table's
/// order, and their three projections — the doubling action, the response,
/// and whether the answer commits to it — each total over the four and
/// refusing anything else. What an answer reads as and what it costs are the
/// decision's, pinned in <see cref="CubeDecisionClaimOfTests"/> and
/// <see cref="CubeDecisionCostOfTests"/>.
/// </summary>
public class CubeAnswerTests
{
    private const CubeAnswer Undefined = (CubeAnswer)4;

    // ---------------------------------------------------------------------
    //  Exactly four, in the order they are offered
    // ---------------------------------------------------------------------

    [Fact]
    public void HasExactlyFourMembers_InTheOrderTheyAreOffered()
    {
        Assert.Equal(
            [CubeAnswer.NoDouble, CubeAnswer.DoubleTake, CubeAnswer.DoublePass, CubeAnswer.NoDoublePass],
            Enum.GetValues<CubeAnswer>());
    }

    // The zero value is an answer, so "no answer" is CubeAnswer? null.
    [Fact]
    public void TheDefault_IsNoDouble()
    {
        Assert.Equal(CubeAnswer.NoDouble, default(CubeAnswer));
    }

    // The fourth is named by its meaning, never by one of its two labels.
    [Fact]
    public void NoMember_IsNamedForALabel()
    {
        Assert.DoesNotContain(Enum.GetNames<CubeAnswer>(), name => name.Contains("TooGood", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(CubeAnswer.NoDouble, "\"NoDouble\"")]
    [InlineData(CubeAnswer.DoubleTake, "\"DoubleTake\"")]
    [InlineData(CubeAnswer.DoublePass, "\"DoublePass\"")]
    [InlineData(CubeAnswer.NoDoublePass, "\"NoDoublePass\"")]
    public void Serializes_AsItsName(CubeAnswer answer, string expectedJson)
    {
        Assert.Equal(expectedJson, JsonSerializer.Serialize(answer));
        Assert.Equal(answer, JsonSerializer.Deserialize<CubeAnswer>(expectedJson));
    }

    // ---------------------------------------------------------------------
    //  The projections, for all four
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(CubeAnswer.NoDouble, CubeAction.NoDouble, CubeAction.Take, false)]
    [InlineData(CubeAnswer.DoubleTake, CubeAction.Double, CubeAction.Take, true)]
    [InlineData(CubeAnswer.DoublePass, CubeAction.Double, CubeAction.Pass, true)]
    [InlineData(CubeAnswer.NoDoublePass, CubeAction.NoDouble, CubeAction.Pass, true)]
    public void EachAnswer_ProjectsItsDoublingActionResponseAndCommitment(
        CubeAnswer answer, CubeAction doubler, CubeAction taker, bool commits)
    {
        Assert.Equal(doubler, answer.DoublerAction());
        Assert.Equal(taker, answer.TakerAction());
        Assert.Equal(commits, answer.CommitsToResponse());
    }

    // Each projection stays in its own half, as CubeDecisionData's action
    // errors require of what they are given.
    [Fact]
    public void TheProjections_StayInTheirHalves()
    {
        foreach (var answer in Enum.GetValues<CubeAnswer>())
        {
            Assert.Contains(answer.DoublerAction(), new[] { CubeAction.NoDouble, CubeAction.Double });
            Assert.Contains(answer.TakerAction(), new[] { CubeAction.Take, CubeAction.Pass });
        }
    }

    // The two projections tell the four apart: no two answers share both, so
    // the answer is exactly its pair of actions, and each pair of a
    // doubler-half and a taker-half action is exactly one answer.
    [Fact]
    public void TheProjections_AreABijectionWithThePairsOfActions()
    {
        var pairs = Enum.GetValues<CubeAnswer>().Select(a => (a.DoublerAction(), a.TakerAction())).ToList();

        Assert.Equal(4, pairs.Distinct().Count());
        foreach (var answer in Enum.GetValues<CubeAnswer>())
            Assert.Equal(answer, CubeAnswerExtensions.Of(answer.DoublerAction(), answer.TakerAction()));
    }

    // Only No double leaves its response uncommitted: the one answer whose
    // implied take is never charged.
    [Fact]
    public void OnlyNoDouble_CommitsToNoResponse()
    {
        Assert.Equal(
            [CubeAnswer.NoDouble],
            Enum.GetValues<CubeAnswer>().Where(a => !a.CommitsToResponse()));
    }

    // ---------------------------------------------------------------------
    //  Nothing else is an answer
    // ---------------------------------------------------------------------

    [Fact]
    public void EachProjection_RefusesAValueOutsideTheFour()
    {
        Assert.Equal("answer", Assert.Throws<ArgumentOutOfRangeException>(() => Undefined.DoublerAction()).ParamName);
        Assert.Equal("answer", Assert.Throws<ArgumentOutOfRangeException>(() => Undefined.TakerAction()).ParamName);
        Assert.Equal("answer", Assert.Throws<ArgumentOutOfRangeException>(() => Undefined.CommitsToResponse()).ParamName);
    }

    // ---------------------------------------------------------------------
    //  The inverse, Of: the public way two actions become an answer
    //  (halheinrich/backgammon#326, retiring CubeDecisionPair)
    // ---------------------------------------------------------------------

    // Its contract, pair by pair, stated independently of the projections.
    [Theory]
    [InlineData(CubeAction.NoDouble, CubeAction.Take, CubeAnswer.NoDouble)]
    [InlineData(CubeAction.Double, CubeAction.Take, CubeAnswer.DoubleTake)]
    [InlineData(CubeAction.Double, CubeAction.Pass, CubeAnswer.DoublePass)]
    [InlineData(CubeAction.NoDouble, CubeAction.Pass, CubeAnswer.NoDoublePass)]
    public void TheInverse_MapsEachPairOfActionsToItsAnswer(CubeAction doubler, CubeAction taker, CubeAnswer expected)
    {
        Assert.Equal(expected, CubeAnswerExtensions.Of(doubler, taker));
    }

    // A refusal names the action outside its half and carries its value; the
    // doubler is checked first.
    [Theory]
    [InlineData(CubeAction.Take, CubeAction.Take, "doubler")]
    [InlineData(CubeAction.Pass, CubeAction.Pass, "doubler")]
    [InlineData(CubeAction.Take, (CubeAction)9, "doubler")]
    [InlineData((CubeAction)9, CubeAction.Take, "doubler")]
    [InlineData(CubeAction.NoDouble, CubeAction.NoDouble, "taker")]
    [InlineData(CubeAction.Double, CubeAction.Double, "taker")]
    [InlineData(CubeAction.Double, (CubeAction)9, "taker")]
    public void TheInverse_RefusesAnActionOutsideItsHalf(CubeAction doubler, CubeAction taker, string parameter)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CubeAnswerExtensions.Of(doubler, taker));
        Assert.Equal(parameter, ex.ParamName);
        Assert.Equal(parameter == "doubler" ? doubler : taker, ex.ActualValue);
    }

    // A consumer holding two recorded actions forms the answer here, so the
    // inverse is public; the action pair it replaced is gone.
    [Fact]
    public void TheInverse_IsPublic_AndTheActionPairIsRetired()
    {
        var of = typeof(CubeAnswerExtensions).GetMethod(nameof(CubeAnswerExtensions.Of));

        Assert.NotNull(of);
        Assert.True(of.IsPublic && of.IsStatic);
        Assert.Null(typeof(CubeAnswer).Assembly.GetType("BgDataTypes_Lib.CubeDecisionPair"));
    }

    // A recorded no double with a pass reads Too good only where gammons are
    // possible: its label is the decision's, never the actions'.
    [Fact]
    public void TwoRecordedActions_ReadTheirLabelFromTheDecision()
    {
        var answer = CubeAnswerExtensions.Of(CubeAction.NoDouble, CubeAction.Pass);

        Assert.Equal(CubeClaim.TooGood, CubeAt.GammonsPossible(0.512, 0.634).ClaimOf(answer));
        Assert.Equal(CubeClaim.NoDouble, CubeAt.GammonsNotPossible(0.512, 0.634).ClaimOf(answer));
    }
}
