using System.Reflection;
using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Pins <see cref="CubeDecisionData.ActionEquity"/>, each cube action's
/// equity handed out by the producer (halheinrich/backgammon#273, Hal's
/// ruling of 2026-09-27): every action in the player on roll's — the
/// doubler's — perspective, a pass worth the cash, doubling worth the lesser
/// of the double/take equity and the cash, no double and take the stored
/// equities; and that it is the one calculation the scoring reads — each
/// half's error is the gap between the best action's equity and the chosen
/// action's, both from it. Equities are synthesized inline per the TestData
/// rule. The scoring and claim suites pin the best actions, the errors and
/// the claim on their own terms.
/// </summary>
public class CubeDecisionDataActionEquityTests
{
    private static CubeDecisionData MakeCube(double noDoubleEquity, double doubleTakeEquity)
        => TestRecords.CubeData(
            noDoubleEquity: noDoubleEquity,
            doubleTakeEquity: doubleTakeEquity);

    // ---------------------------------------------------------------------
    //  Each action's equity, in the player on roll's perspective
    // ---------------------------------------------------------------------

    [Theory]
    // (NoDoubleEquity, DoubleTakeEquity, action, expected) — the doubler's
    // equity for every action, the taker's included: a take is worth the
    // double/take equity to the doubler, a pass +1, never their negations.
    // A take below the cash: doubling is worth the take.
    [InlineData(0.30, 0.60, CubeAction.NoDouble, 0.30)]
    [InlineData(0.30, 0.60, CubeAction.Double, 0.60)]
    [InlineData(0.30, 0.60, CubeAction.Take, 0.60)]
    [InlineData(0.30, 0.60, CubeAction.Pass, 1.0)]
    // A take above the cash: the opponent passes, so doubling is worth the cash.
    [InlineData(0.50, 1.20, CubeAction.NoDouble, 0.50)]
    [InlineData(0.50, 1.20, CubeAction.Double, 1.0)]
    [InlineData(0.50, 1.20, CubeAction.Take, 1.20)]
    [InlineData(0.50, 1.20, CubeAction.Pass, 1.0)]
    // The doubler behind: the stored equities pass through with their sign.
    [InlineData(-0.40, -0.70, CubeAction.NoDouble, -0.40)]
    [InlineData(-0.40, -0.70, CubeAction.Double, -0.70)]
    [InlineData(-0.40, -0.70, CubeAction.Take, -0.70)]
    [InlineData(-0.40, -0.70, CubeAction.Pass, 1.0)]
    // XG's "Too good to double/Take" position (TooGoodAndTake.xgp): no double
    // +1.1711, double/take +0.6004.
    [InlineData(1.1711, 0.6004, CubeAction.NoDouble, 1.1711)]
    [InlineData(1.1711, 0.6004, CubeAction.Double, 0.6004)]
    [InlineData(1.1711, 0.6004, CubeAction.Take, 0.6004)]
    [InlineData(1.1711, 0.6004, CubeAction.Pass, 1.0)]
    // Too good / pass: playing on beats the cash, which doubling is worth.
    [InlineData(1.30, 1.50, CubeAction.NoDouble, 1.30)]
    [InlineData(1.30, 1.50, CubeAction.Double, 1.0)]
    [InlineData(1.30, 1.50, CubeAction.Take, 1.50)]
    [InlineData(1.30, 1.50, CubeAction.Pass, 1.0)]
    public void ActionEquity_OfEachAction_IsItsEquityForThePlayerOnRoll(
        double noDoubleEquity, double doubleTakeEquity, CubeAction action, double expected)
    {
        Assert.Equal(expected, MakeCube(noDoubleEquity, doubleTakeEquity).ActionEquity(action));
    }

    [Theory]
    // (NoDoubleEquity, DoubleTakeEquity, one action, the other, what both are worth).
    // The take at the cash: taking is worth exactly what passing is.
    [InlineData(0.50, 1.0, CubeAction.Take, CubeAction.Pass, 1.0)]
    // Doubling there is worth the cash, whichever response the opponent picks.
    [InlineData(0.50, 1.0, CubeAction.Double, CubeAction.Pass, 1.0)]
    // Doubling worth exactly what playing on is, below the cash.
    [InlineData(0.60, 0.60, CubeAction.Double, CubeAction.NoDouble, 0.60)]
    // Playing on worth exactly the cash, the Too Good boundary...
    [InlineData(1.0, 1.20, CubeAction.NoDouble, CubeAction.Pass, 1.0)]
    // ...where doubling, answered by a pass, is worth it too.
    [InlineData(1.0, 1.20, CubeAction.NoDouble, CubeAction.Double, 1.0)]
    // Everything at the cash.
    [InlineData(1.0, 1.0, CubeAction.NoDouble, CubeAction.Take, 1.0)]
    public void ActionEquity_AtEachTie_TheTiedActionsAreWorthTheSame(
        double noDoubleEquity, double doubleTakeEquity, CubeAction one, CubeAction other, double expected)
    {
        var cube = MakeCube(noDoubleEquity, doubleTakeEquity);

        Assert.Equal(expected, cube.ActionEquity(one));
        Assert.Equal(expected, cube.ActionEquity(other));
    }

    [Fact]
    public void ActionEquity_OfAnUndefinedAction_Throws()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => MakeCube(0.30, 0.60).ActionEquity((CubeAction)99));
        Assert.Equal("action", ex.ParamName);
    }

    // ---------------------------------------------------------------------
    //  One calculation: the scoring reads the equities handed out
    // ---------------------------------------------------------------------

    private static readonly double[] NoDoubleEquities = [-0.50, 0.0, 0.30, 0.60, 0.999, 1.0, 1.001, 1.1711, 1.30];
    private static readonly double[] DoubleTakeEquities = [-0.70, 0.10, 0.60, 0.6004, 0.999, 1.0, 1.001, 1.50];

    [Fact]
    public void EachError_IsTheGapBetweenTheBestActionsEquityAndTheChosenOnes()
    {
        // Over a grid holding every tie: the doubler's error is the best
        // doubler action's equity less the chosen one's; the taker's, in the
        // taker's perspective (the doubler's negated), the chosen action's
        // equity less the best taker action's. Exactly, since both are read
        // from the one calculation.
        foreach (var nd in NoDoubleEquities)
        foreach (var dt in DoubleTakeEquities)
        {
            var cube = MakeCube(nd, dt);
            foreach (var action in new[] { CubeAction.NoDouble, CubeAction.Double })
                Assert.Equal(
                    cube.ActionEquity(cube.BestDoublerAction) - cube.ActionEquity(action),
                    cube.DoublerActionError(action));
            foreach (var action in new[] { CubeAction.Take, CubeAction.Pass })
                Assert.Equal(
                    cube.ActionEquity(action) - cube.ActionEquity(cube.BestTakerAction),
                    cube.TakerActionError(action));
        }
    }

    [Fact]
    public void AnErrorAtATieOfOppositeZeros_IsPositiveZero()
    {
        // Added with the one calculation: playing on stored as -0 and doubling
        // (taken) worth +0 tie, and NoDouble is best. The gap from -0 to +0
        // would subtract to -0, which a document writes as "-0"; the errors
        // were +0 there before the scoring read ActionEquity, and still are.
        var cube = MakeCube(-0.0, 0.0);

        Assert.Equal(CubeAction.NoDouble, cube.BestDoublerAction);
        foreach (var action in new[] { CubeAction.NoDouble, CubeAction.Double })
        {
            Assert.False(double.IsNegative(cube.DoublerActionError(action)), action.ToString());
            Assert.Equal("0", JsonSerializer.Serialize(cube.DoublerActionError(action)));
        }
    }

    [Fact]
    public void TheBestActions_AreTheBestByTheEquitiesHandedOut()
    {
        // The doubler's best is worth the more of its half's two actions, the
        // taker's best leaves the doubler the less of its half's two, and
        // doubling is worth exactly what the taker's best response leaves.
        foreach (var nd in NoDoubleEquities)
        foreach (var dt in DoubleTakeEquities)
        {
            var cube = MakeCube(nd, dt);
            double noDouble = cube.ActionEquity(CubeAction.NoDouble);
            double @double = cube.ActionEquity(CubeAction.Double);
            double take = cube.ActionEquity(CubeAction.Take);
            double pass = cube.ActionEquity(CubeAction.Pass);

            Assert.Equal(Math.Max(@double, noDouble), cube.ActionEquity(cube.BestDoublerAction));
            Assert.Equal(Math.Min(take, pass), cube.ActionEquity(cube.BestTakerAction));
            Assert.Equal(cube.ActionEquity(cube.BestTakerAction), @double);
        }
    }

    // ---------------------------------------------------------------------
    //  A cube decision's member, derived, and the pass value not on its own
    // ---------------------------------------------------------------------

    [Fact]
    public void ActionEquity_IsNotAMemberOfACheckerPlay()
    {
        Assert.NotEmpty(typeof(CubeDecisionData).GetMember(nameof(CubeDecisionData.ActionEquity)));
        Assert.Empty(typeof(CheckerPlayDecisionData).GetMember(nameof(CubeDecisionData.ActionEquity)));
        Assert.Empty(typeof(CheckerPlayDecision).GetMember(nameof(CubeDecisionData.ActionEquity)));
    }

    [Fact]
    public void ActionEquity_IsDerived_NothingNewOnTheWire()
    {
        // Derived, not stored: neither the category nor the flat row gains a
        // member for it, on either path.
        var record = TestRecords.Cube(decision: MakeCube(0.30, 0.60));
        var row = DecisionRow.From(record, PlayRanking.Equity);

        foreach (var (_, options) in WirePaths.Both)
        {
            Assert.DoesNotContain("ActionEquity", JsonSerializer.Serialize(record.Decision, options));
            Assert.DoesNotContain("ActionEquity", JsonSerializer.Serialize(row, options));
        }
        Assert.Null(typeof(DecisionRow).GetProperty("ActionEquity"));
    }

    [Fact]
    public void ThePassValue_IsNotPublishedOnItsOwn()
    {
        // Hal's ruling of 2026-09-27: a consumer gets the pass's equity as an
        // action's equity, never a bare normalization constant to rebuild the
        // domain calculation from. No public member of the category names the
        // pass.
        var members = typeof(CubeDecisionData).GetMembers(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

        Assert.DoesNotContain(members, member => member.Name.Contains("Pass", StringComparison.Ordinal));
    }
}
