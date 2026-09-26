using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

public class CubeDecisionDataScoringTests
{
    // A cube decision is scored as two independent atomic decisions:
    //
    //   * the doubler's double / no-double decision, which depends on
    //     NoDoubleEquity vs. min(DoubleTakeEquity, 1); and
    //   * the taker's take / pass decision, which depends on
    //     DoubleTakeEquity vs. 1.
    //
    // Each test drives the helper under test directly from the equities its
    // decision depends on. Equities are chosen off the tie boundaries so the
    // expected action is unambiguous.

    private static CubeDecisionData MakeCube(double noDoubleEquity, double doubleTakeEquity)
        => TestRecords.CubeData(
            noDoubleEquity: noDoubleEquity,
            doubleTakeEquity: doubleTakeEquity);

    // ---------------------------------------------------------------------
    //  Doubler decision: BestDoublerAction
    // ---------------------------------------------------------------------

    [Theory]
    // (NoDoubleEquity, DoubleTakeEquity, expected) — Double iff min(E_DT,1) > E_ND.
    [InlineData(0.20, 0.10, CubeAction.NoDouble)]  // min(0.10,1)=0.10 < 0.20
    [InlineData(0.30, 0.60, CubeAction.Double)]    // min(0.60,1)=0.60 > 0.30
    [InlineData(0.50, 1.20, CubeAction.Double)]    // min(1.20,1)=1.00 > 0.50
    [InlineData(1.30, 1.50, CubeAction.NoDouble)]  // min(1.50,1)=1.00 < 1.30
    public void BestDoublerAction_ReturnsExpected(
        double noDoubleEquity, double doubleTakeEquity, CubeAction expected)
    {
        Assert.Equal(expected, MakeCube(noDoubleEquity, doubleTakeEquity).BestDoublerAction);
    }

    // ---------------------------------------------------------------------
    //  Taker decision: BestTakerAction
    // ---------------------------------------------------------------------

    [Theory]
    // (DoubleTakeEquity, expected) — Take iff E_DT < 1. NoDoubleEquity is
    // irrelevant to the taker half, so it is held at a neutral 0.50.
    [InlineData(0.10, CubeAction.Take)]
    [InlineData(0.60, CubeAction.Take)]
    [InlineData(1.20, CubeAction.Pass)]
    [InlineData(1.50, CubeAction.Pass)]
    public void BestTakerAction_ReturnsExpected(
        double doubleTakeEquity, CubeAction expected)
    {
        Assert.Equal(expected, MakeCube(0.50, doubleTakeEquity).BestTakerAction);
    }

    // ---------------------------------------------------------------------
    //  Doubler decision: DoublerActionError
    // ---------------------------------------------------------------------

    [Theory]
    // (NoDoubleEquity, DoubleTakeEquity, action, expectedError).
    // best doubler equity = max(min(E_DT,1), E_ND); error = best - action's equity.
    [InlineData(0.20, 0.10, CubeAction.NoDouble, 0.0)]   // best = NoDouble
    [InlineData(0.20, 0.10, CubeAction.Double,   0.10)]  // 0.20 - min(0.10,1)
    [InlineData(0.30, 0.60, CubeAction.Double,   0.0)]   // best = Double
    [InlineData(0.30, 0.60, CubeAction.NoDouble, 0.30)]  // min(0.60,1) - 0.30
    [InlineData(0.50, 1.20, CubeAction.Double,   0.0)]   // best = Double
    [InlineData(0.50, 1.20, CubeAction.NoDouble, 0.50)]  // min(1.20,1) - 0.50
    [InlineData(1.30, 1.50, CubeAction.NoDouble, 0.0)]   // best = NoDouble
    [InlineData(1.30, 1.50, CubeAction.Double,   0.30)]  // 1.30 - min(1.50,1)
    public void DoublerActionError_ZeroForBest_PositiveForOther(
        double noDoubleEquity, double doubleTakeEquity, CubeAction action, double expectedError)
    {
        Assert.Equal(expectedError,
            MakeCube(noDoubleEquity, doubleTakeEquity).DoublerActionError(action),
            precision: 10);
    }

    // ---------------------------------------------------------------------
    //  Taker decision: TakerActionError
    // ---------------------------------------------------------------------

    [Theory]
    // (DoubleTakeEquity, action, expectedError). Taker equities are the
    // doubler's negated: Take = -E_DT, Pass = -1; best = max(-E_DT, -1).
    [InlineData(0.10, CubeAction.Take, 0.0)]    // best = Take
    [InlineData(0.10, CubeAction.Pass, 0.90)]   // 1 - 0.10
    [InlineData(0.60, CubeAction.Take, 0.0)]    // best = Take
    [InlineData(0.60, CubeAction.Pass, 0.40)]   // 1 - 0.60
    [InlineData(1.20, CubeAction.Pass, 0.0)]    // best = Pass
    [InlineData(1.20, CubeAction.Take, 0.20)]   // 1.20 - 1
    [InlineData(1.50, CubeAction.Pass, 0.0)]    // best = Pass
    [InlineData(1.50, CubeAction.Take, 0.50)]   // 1.50 - 1
    public void TakerActionError_ZeroForBest_PositiveForOther(
        double doubleTakeEquity, CubeAction action, double expectedError)
    {
        Assert.Equal(expectedError,
            MakeCube(0.50, doubleTakeEquity).TakerActionError(action),
            precision: 10);
    }

    // ---------------------------------------------------------------------
    //  A cube decision's members only
    // ---------------------------------------------------------------------

    [Fact]
    public void AtomicHelpers_AreNotMembersOfACheckerPlay()
    {
        // Rewritten from AtomicHelpers_Throw_WhenNotCube: the helpers live on
        // the cube category alone, so calling one on a checker play does not
        // compile — the runtime guard they needed is gone.
        foreach (var name in new[]
                 {
                     nameof(CubeDecisionData.BestDoublerAction), nameof(CubeDecisionData.BestTakerAction),
                     nameof(CubeDecisionData.DoublerActionError), nameof(CubeDecisionData.TakerActionError),
                 })
        {
            Assert.NotEmpty(typeof(CubeDecisionData).GetMember(name));
            Assert.Empty(typeof(CheckerPlayDecisionData).GetMember(name));
            Assert.Empty(typeof(CheckerPlayDecision).GetMember(name));
        }
    }

    // ---------------------------------------------------------------------
    //  Parameter-domain guards on the atomic-action methods
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(CubeAction.Take)]
    [InlineData(CubeAction.Pass)]
    public void DoublerActionError_OnNonDoublerAction_Throws(CubeAction takerAction)
    {
        var d = MakeCube(0.30, 0.60);
        Assert.Throws<ArgumentOutOfRangeException>(() => d.DoublerActionError(takerAction));
    }

    [Theory]
    [InlineData(CubeAction.Double)]
    [InlineData(CubeAction.NoDouble)]
    public void TakerActionError_OnNonTakerAction_Throws(CubeAction doublerAction)
    {
        var d = MakeCube(0.30, 0.60);
        Assert.Throws<ArgumentOutOfRangeException>(() => d.TakerActionError(doublerAction));
    }

    // ---------------------------------------------------------------------
    //  JSON contract — computed cube-scoring properties are NOT serialised
    // ---------------------------------------------------------------------
    //
    //  The computed members are a derivation, not wire. This test pins that
    //  contract: it would fail both if [JsonIgnore] were removed (property
    //  names would appear in the JSON) and if a future change accidentally
    //  re-exposed them via init-only data fields.

    [Fact]
    public void ComputedCubeProperties_AreNotSerialised()
    {
        var d = TestRecords.CubeData(
            noDoubleEquity: 0.30,
            doubleTakeEquity: 0.60);

        string json = JsonSerializer.Serialize(d);

        Assert.DoesNotContain("\"BestDoublerAction\"", json);
        Assert.DoesNotContain("\"BestTakerAction\"",   json);
    }
}
