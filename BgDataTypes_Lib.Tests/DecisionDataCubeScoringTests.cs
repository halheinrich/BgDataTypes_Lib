using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

public class DecisionDataCubeScoringTests
{
    // Representative regime fixtures — one for each of the four BestVerdict
    // outcomes. Equities chosen so the regime boundaries are unambiguous
    // (no ties), so the tests pin behaviour in the interior of each regime
    // rather than on the tie-breaking edges.
    private const double NoDoubleE_ND   = 0.20;
    private const double NoDoubleE_DT   = 0.10;
    private const double DoubleTakeE_ND = 0.30;
    private const double DoubleTakeE_DT = 0.60;
    private const double DoublePassE_ND = 0.50;
    private const double DoublePassE_DT = 1.20;
    private const double TooGoodE_ND    = 1.30;
    private const double TooGoodE_DT    = 1.50;

    private static DecisionData MakeCube(double noDoubleEquity, double doubleTakeEquity)
        => new()
        {
            IsCube = true,
            NoDoubleEquity = noDoubleEquity,
            DoubleTakeEquity = doubleTakeEquity
        };

    private static DecisionData Regime(CubeVerdict regime) => regime switch
    {
        CubeVerdict.NoDouble   => MakeCube(NoDoubleE_ND,   NoDoubleE_DT),
        CubeVerdict.DoubleTake => MakeCube(DoubleTakeE_ND, DoubleTakeE_DT),
        CubeVerdict.DoublePass => MakeCube(DoublePassE_ND, DoublePassE_DT),
        CubeVerdict.TooGood    => MakeCube(TooGoodE_ND,    TooGoodE_DT),
        _ => throw new ArgumentOutOfRangeException(nameof(regime), regime, null)
    };

    // ---------------------------------------------------------------------
    //  BestVerdict / BestDoublerAction / BestResponderAction
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(CubeVerdict.NoDouble)]
    [InlineData(CubeVerdict.DoubleTake)]
    [InlineData(CubeVerdict.DoublePass)]
    [InlineData(CubeVerdict.TooGood)]
    public void BestVerdict_ReturnsExpected_PerRegime(CubeVerdict regime)
    {
        Assert.Equal(regime, Regime(regime).BestVerdict);
    }

    [Theory]
    [InlineData(CubeVerdict.NoDouble,   CubeAction.NoDouble)]
    [InlineData(CubeVerdict.DoubleTake, CubeAction.Double)]
    [InlineData(CubeVerdict.DoublePass, CubeAction.Double)]
    [InlineData(CubeVerdict.TooGood,    CubeAction.NoDouble)]
    public void BestDoublerAction_ReturnsExpected_PerRegime(
        CubeVerdict regime, CubeAction expected)
    {
        Assert.Equal(expected, Regime(regime).BestDoublerAction);
    }

    [Theory]
    [InlineData(CubeVerdict.NoDouble,   CubeAction.Take)]
    [InlineData(CubeVerdict.DoubleTake, CubeAction.Take)]
    [InlineData(CubeVerdict.DoublePass, CubeAction.Pass)]
    [InlineData(CubeVerdict.TooGood,    CubeAction.Pass)]
    public void BestResponderAction_ReturnsExpected_PerRegime(
        CubeVerdict regime, CubeAction expected)
    {
        Assert.Equal(expected, Regime(regime).BestResponderAction);
    }

    // ---------------------------------------------------------------------
    //  Atomic-action errors
    // ---------------------------------------------------------------------

    [Theory]
    // (regime, action, expectedError)
    // NoDouble regime: best = NoDouble; Double loses E_ND - min(E_DT, 1) = 0.20 - 0.10
    [InlineData(CubeVerdict.NoDouble,   CubeAction.NoDouble, 0.0)]
    [InlineData(CubeVerdict.NoDouble,   CubeAction.Double,   0.10)]
    // DoubleTake regime: best = Double; NoDouble loses min(E_DT,1) - E_ND = 0.60 - 0.30
    [InlineData(CubeVerdict.DoubleTake, CubeAction.Double,   0.0)]
    [InlineData(CubeVerdict.DoubleTake, CubeAction.NoDouble, 0.30)]
    // DoublePass regime: best = Double; NoDouble loses min(E_DT,1) - E_ND = 1 - 0.50
    [InlineData(CubeVerdict.DoublePass, CubeAction.Double,   0.0)]
    [InlineData(CubeVerdict.DoublePass, CubeAction.NoDouble, 0.50)]
    // TooGood regime: best = NoDouble; Double loses E_ND - min(E_DT,1) = 1.30 - 1
    [InlineData(CubeVerdict.TooGood,    CubeAction.NoDouble, 0.0)]
    [InlineData(CubeVerdict.TooGood,    CubeAction.Double,   0.30)]
    public void DoublerActionError_ZeroForBest_PositiveForOther(
        CubeVerdict regime, CubeAction action, double expectedError)
    {
        Assert.Equal(expectedError, Regime(regime).DoublerActionError(action), precision: 10);
    }

    [Theory]
    // NoDouble regime: best = Take; Pass loses 1 - E_DT = 1 - 0.10
    [InlineData(CubeVerdict.NoDouble,   CubeAction.Take, 0.0)]
    [InlineData(CubeVerdict.NoDouble,   CubeAction.Pass, 0.90)]
    // DoubleTake regime: best = Take; Pass loses 1 - E_DT = 1 - 0.60
    [InlineData(CubeVerdict.DoubleTake, CubeAction.Take, 0.0)]
    [InlineData(CubeVerdict.DoubleTake, CubeAction.Pass, 0.40)]
    // DoublePass regime: best = Pass; Take loses E_DT - 1 = 1.20 - 1
    [InlineData(CubeVerdict.DoublePass, CubeAction.Pass, 0.0)]
    [InlineData(CubeVerdict.DoublePass, CubeAction.Take, 0.20)]
    // TooGood regime: best = Pass; Take loses E_DT - 1 = 1.50 - 1
    [InlineData(CubeVerdict.TooGood,    CubeAction.Pass, 0.0)]
    [InlineData(CubeVerdict.TooGood,    CubeAction.Take, 0.50)]
    public void ResponderActionError_ZeroForBest_PositiveForOther(
        CubeVerdict regime, CubeAction action, double expectedError)
    {
        Assert.Equal(expectedError, Regime(regime).ResponderActionError(action), precision: 10);
    }

    // ---------------------------------------------------------------------
    //  VerdictEquityLoss = DDE + TDE (sum identity, all regime × verdict)
    // ---------------------------------------------------------------------

    [Fact]
    public void VerdictEquityLoss_EqualsSumOfDdeAndTde()
    {
        foreach (CubeVerdict regime in Enum.GetValues<CubeVerdict>())
        {
            var d = Regime(regime);
            foreach (CubeVerdict v in Enum.GetValues<CubeVerdict>())
            {
                Assert.Equal(
                    d.DoublerDecisionEquityLoss(v) + d.TakeDecisionEquityLoss(v),
                    d.VerdictEquityLoss(v),
                    precision: 12);
            }
        }
    }

    // ---------------------------------------------------------------------
    //  Natural cases (no NoDouble↔TooGood override) — per-verdict equity loss
    // ---------------------------------------------------------------------

    [Theory]
    // DoubleTake regime (E_ND=0.30, E_DT=0.60): best = DoubleTake
    [InlineData(CubeVerdict.DoubleTake, CubeVerdict.DoubleTake, 0.0)]   // played best
    [InlineData(CubeVerdict.DoubleTake, CubeVerdict.NoDouble,   0.30)]  // DDE=0.30, TDE=0
    [InlineData(CubeVerdict.DoubleTake, CubeVerdict.DoublePass, 0.40)]  // DDE=0,    TDE=0.40
    [InlineData(CubeVerdict.DoubleTake, CubeVerdict.TooGood,    0.70)]  // DDE=0.30, TDE=0.40 (NoDouble↔TooGood not the BestVerdict here → no override)
    // DoublePass regime (E_ND=0.50, E_DT=1.20): best = DoublePass
    [InlineData(CubeVerdict.DoublePass, CubeVerdict.DoublePass, 0.0)]
    [InlineData(CubeVerdict.DoublePass, CubeVerdict.DoubleTake, 0.20)]  // DDE=0,    TDE=0.20
    [InlineData(CubeVerdict.DoublePass, CubeVerdict.NoDouble,   0.70)]  // DDE=0.50, TDE=0.20
    [InlineData(CubeVerdict.DoublePass, CubeVerdict.TooGood,    0.50)]  // DDE=0.50, TDE=0
    public void VerdictEquityLoss_NaturalCases_MatchExpected(
        CubeVerdict regime, CubeVerdict verdict, double expectedLoss)
    {
        Assert.Equal(expectedLoss, Regime(regime).VerdictEquityLoss(verdict), precision: 10);
    }

    // ---------------------------------------------------------------------
    //  NoDouble↔TooGood strategic-confusion override (the two named cases)
    // ---------------------------------------------------------------------

    [Fact]
    public void NoDouble_when_TooGood_correct_equityLoss_is_2x_take_error()
    {
        // TooGood regime: E_ND = 1.30, E_DT = 1.50 → BestVerdict = TooGood.
        // User played NoDouble — atomic doubler action (NoDouble) matches
        // BestDoublerAction, so atomic-baseline DDE = 0. The strategic-
        // confusion override bumps DDE from 0 to TDE so the total reflects
        // both halves of the misjudgement. TDE = ResponderActionError(Take)
        // = E_DT - 1 = 0.50.
        var d = Regime(CubeVerdict.TooGood);

        double dde = d.DoublerDecisionEquityLoss(CubeVerdict.NoDouble);
        double tde = d.TakeDecisionEquityLoss(CubeVerdict.NoDouble);
        double total = d.VerdictEquityLoss(CubeVerdict.NoDouble);

        Assert.Equal(0.50, dde, precision: 10);
        Assert.Equal(0.50, tde, precision: 10);
        Assert.Equal(2.0 * (TooGoodE_DT - 1.0), total, precision: 10);
        Assert.Equal(1.0, total, precision: 10);
    }

    [Fact]
    public void TooGood_when_NoDouble_correct_equityLoss_is_2x_pass_error()
    {
        // NoDouble regime: E_ND = 0.20, E_DT = 0.10 → BestVerdict = NoDouble.
        // User played TooGood — atomic doubler action (NoDouble) matches
        // BestDoublerAction, so atomic-baseline DDE = 0. The strategic-
        // confusion override bumps DDE from 0 to TDE. TDE =
        // ResponderActionError(Pass) = 1 - E_DT = 0.90.
        var d = Regime(CubeVerdict.NoDouble);

        double dde = d.DoublerDecisionEquityLoss(CubeVerdict.TooGood);
        double tde = d.TakeDecisionEquityLoss(CubeVerdict.TooGood);
        double total = d.VerdictEquityLoss(CubeVerdict.TooGood);

        Assert.Equal(0.90, dde, precision: 10);
        Assert.Equal(0.90, tde, precision: 10);
        Assert.Equal(2.0 * (1.0 - NoDoubleE_DT), total, precision: 10);
        Assert.Equal(1.8, total, precision: 10);
    }

    // ---------------------------------------------------------------------
    //  Override does NOT fire on the DoubleTake↔DoublePass pair
    // ---------------------------------------------------------------------

    [Fact]
    public void DoubleTake_when_DoublePass_correct_uses_atomic_baseline_DDE()
    {
        // DoublePass regime: best = DoublePass. User played DoubleTake —
        // atomic doubler matches (both Double), but DT↔DP is explicitly NOT
        // a strategic-confusion override. Expected: DDE = 0 (atomic),
        // TDE = ResponderActionError(Take) = E_DT - 1 = 0.20.
        var d = Regime(CubeVerdict.DoublePass);

        Assert.Equal(0.0,  d.DoublerDecisionEquityLoss(CubeVerdict.DoubleTake), precision: 10);
        Assert.Equal(0.20, d.TakeDecisionEquityLoss(CubeVerdict.DoubleTake),    precision: 10);
        Assert.Equal(0.20, d.VerdictEquityLoss(CubeVerdict.DoubleTake),         precision: 10);
    }

    // ---------------------------------------------------------------------
    //  IsCube guard — every helper throws when called on a play decision
    // ---------------------------------------------------------------------

    [Fact]
    public void AllHelpers_Throw_WhenNotCube()
    {
        var play = new DecisionData();   // IsCube defaults to false

        Assert.Throws<InvalidOperationException>(() => _ = play.BestVerdict);
        Assert.Throws<InvalidOperationException>(() => _ = play.BestDoublerAction);
        Assert.Throws<InvalidOperationException>(() => _ = play.BestResponderAction);
        Assert.Throws<InvalidOperationException>(() => play.DoublerActionError(CubeAction.Double));
        Assert.Throws<InvalidOperationException>(() => play.ResponderActionError(CubeAction.Take));
        Assert.Throws<InvalidOperationException>(() => play.DoublerDecisionEquityLoss(CubeVerdict.NoDouble));
        Assert.Throws<InvalidOperationException>(() => play.TakeDecisionEquityLoss(CubeVerdict.NoDouble));
        Assert.Throws<InvalidOperationException>(() => play.VerdictEquityLoss(CubeVerdict.NoDouble));
    }

    // ---------------------------------------------------------------------
    //  Parameter-domain guards on the atomic-action methods
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(CubeAction.Take)]
    [InlineData(CubeAction.Pass)]
    public void DoublerActionError_OnNonDoublerAction_Throws(CubeAction responderAction)
    {
        var d = Regime(CubeVerdict.DoubleTake);
        Assert.Throws<ArgumentOutOfRangeException>(() => d.DoublerActionError(responderAction));
    }

    [Theory]
    [InlineData(CubeAction.Double)]
    [InlineData(CubeAction.NoDouble)]
    public void ResponderActionError_OnNonResponderAction_Throws(CubeAction doublerAction)
    {
        var d = Regime(CubeVerdict.DoubleTake);
        Assert.Throws<ArgumentOutOfRangeException>(() => d.ResponderActionError(doublerAction));
    }

    // ---------------------------------------------------------------------
    //  JSON contract — computed cube-scoring properties are NOT serialised
    // ---------------------------------------------------------------------
    //
    //  Without [JsonIgnore], System.Text.Json would invoke the public
    //  getters on every DecisionData it serialises, including the play
    //  decisions where the getters throw. This test pins that contract:
    //  it would fail both if [JsonIgnore] were removed (property names
    //  would appear in the JSON) and if a future change accidentally
    //  re-exposed them via init-only data fields.

    [Fact]
    public void ComputedCubeProperties_AreNotSerialised()
    {
        var d = new DecisionData
        {
            IsCube = true,
            NoDoubleEquity = 0.30,
            DoubleTakeEquity = 0.60
        };

        string json = JsonSerializer.Serialize(d);

        Assert.DoesNotContain("\"BestVerdict\"",         json);
        Assert.DoesNotContain("\"BestDoublerAction\"",   json);
        Assert.DoesNotContain("\"BestResponderAction\"", json);
    }
}
