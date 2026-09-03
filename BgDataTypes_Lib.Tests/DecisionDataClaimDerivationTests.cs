using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Pins the truth-claim derivation of SPEC-scoring §3
/// (halheinrich/backgammon#86; amended 2026-09-02 by
/// halheinrich/backgammon#187): Too Good ⟺ best doubler action is NoDouble
/// AND NoDoubleEquity &gt; 1 AND best taker action is Pass — implemented as
/// <see cref="DecisionData.BestDoublerClaim"/> beside its action-level
/// siblings, with <see cref="DecisionData.BestClaimPair"/> composing the
/// full derived truth. Equities are synthesized inline per the TestData
/// rule; the real-corpus exercise of the same predicate lives in
/// <see cref="TooGoodCorpusExerciseTests"/>, and the offerability fact that
/// sits beside the claim on the composite record is pinned in
/// <see cref="BgDecisionDataTooGoodOfferabilityTests"/>.
/// </summary>
public class DecisionDataClaimDerivationTests
{
    private static DecisionData MakeCube(double noDoubleEquity, double doubleTakeEquity)
        => new()
        {
            IsCube = true,
            NoDoubleEquity = noDoubleEquity,
            DoubleTakeEquity = doubleTakeEquity
        };

    // ---------------------------------------------------------------------
    //  BestDoublerClaim — every claim value, from equities alone
    // ---------------------------------------------------------------------

    [Theory]
    // (NoDoubleEquity, DoubleTakeEquity, expected).
    // NoDouble: not good enough — playing on beats doubling, but is worth
    // no more than the cashed point.
    [InlineData(0.20, 0.10, CubeClaim.NoDouble)]   // min(0.10,1)=0.10 < 0.20
    [InlineData(0.95, 0.90, CubeClaim.NoDouble)]   // min(0.90,1)=0.90 < 0.95
    // NoDouble by ruling: playing on beats being taken, yet the opponent
    // takes — no pass is involved. XG's "Too good to double/Take" verdict
    // (TooGoodAndTake.xgp: no double +1.1711, double/take +0.6004), the
    // position that decided halheinrich/backgammon#187's amendment.
    [InlineData(1.1711, 0.6004, CubeClaim.NoDouble)]
    // Double: offering the cube is best against optimal response.
    [InlineData(0.30, 0.60, CubeClaim.Double)]     // min(0.60,1)=0.60 > 0.30
    [InlineData(0.50, 1.20, CubeClaim.Double)]     // min(1.20,1)=1.00 > 0.50 — a cash
    // TooGood: no-double is right, playing on beats the cashed point, AND
    // the opponent would pass.
    [InlineData(1.30, 1.50, CubeClaim.TooGood)]    // classic too good / pass
    [InlineData(1.05, 1.00, CubeClaim.TooGood)]    // pass at the taker tie (DoubleTakeEquity == 1)
    public void BestDoublerClaim_ReturnsExpected(
        double noDoubleEquity, double doubleTakeEquity, CubeClaim expected)
    {
        Assert.Equal(expected, MakeCube(noDoubleEquity, doubleTakeEquity).BestDoublerClaim);
    }

    // The Too Good boundary is strict: NoDoubleEquity exactly 1 (playing on
    // worth exactly the cash) is NOT too good — the spec predicate reads
    // "NoDoubleEquity > 1", and the tie keeps the ruled tie-favours-NoDouble
    // posture. Just above 1 the claim flips — but only with the pass.
    [Theory]
    [InlineData(1.0, 0.90, CubeClaim.NoDouble)]         // at the boundary, taker would take
    [InlineData(1.0, 1.50, CubeClaim.NoDouble)]         // at the boundary, taker would pass
    [InlineData(1.000000001, 0.90, CubeClaim.NoDouble)] // just above, but a take — No double by ruling
    [InlineData(1.000000001, 1.50, CubeClaim.TooGood)]  // just above with a pass — too good
    public void BestDoublerClaim_TooGoodBoundary_IsStrictlyAboveOne(
        double noDoubleEquity, double doubleTakeEquity, CubeClaim expected)
    {
        Assert.Equal(expected, MakeCube(noDoubleEquity, doubleTakeEquity).BestDoublerClaim);
    }

    // The pass requirement (SPEC-scoring §3, 2026-09-02: "A position the
    // opponent would take is No double / Take whatever the no-double
    // equity"), pinned over a sweep of no-double equities above 1 against
    // every taker-takes posture.
    [Theory]
    [InlineData(1.001)]
    [InlineData(1.1711)]
    [InlineData(1.50)]
    [InlineData(2.50)]
    public void BestDoublerClaim_WhenOpponentWouldTake_IsNoDoubleWhateverTheNoDoubleEquity(
        double noDoubleEquity)
    {
        double[] takeEquities = [-0.30, 0.10, 0.6004, 0.999];

        foreach (var dt in takeEquities)
        {
            Assert.Equal(CubeClaim.NoDouble, MakeCube(noDoubleEquity, dt).BestDoublerClaim);
        }
    }

    // ---------------------------------------------------------------------
    //  BestClaimPair — the four reachable verdict cells, plus the boundary
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(0.20, 0.10, CubeClaim.NoDouble, CubeAction.Take)]     // No double, take
    [InlineData(1.1711, 0.6004, CubeClaim.NoDouble, CubeAction.Take)] // No double, take — above 1,
                                                                      // but taken (the retired
                                                                      // cell's own position)
    [InlineData(0.30, 0.60, CubeClaim.Double, CubeAction.Take)]       // Double, take
    [InlineData(0.50, 1.20, CubeClaim.Double, CubeAction.Pass)]       // Double, pass
    [InlineData(1.30, 1.50, CubeClaim.TooGood, CubeAction.Pass)]      // Too good, pass
    [InlineData(1.0, 1.20, CubeClaim.NoDouble, CubeAction.Pass)]      // the boundary cell
    public void BestClaimPair_ReachesEveryVerdictCell(
        double noDoubleEquity, double doubleTakeEquity,
        CubeClaim expectedClaim, CubeAction expectedTaker)
    {
        Assert.Equal(
            new CubeClaimPair(expectedClaim, expectedTaker),
            MakeCube(noDoubleEquity, doubleTakeEquity).BestClaimPair);
    }

    // The retired cell: Too Good / Take is unreachable as derived truth
    // since the 2026-09-02 amendment. Structurally, a TooGood claim
    // requires BestTakerAction == Pass, so the composed pair's taker half
    // can never be Take — pinned over a grid that includes every equity
    // the old predicate would have sent to that cell.
    [Fact]
    public void BestClaimPair_NeverDerivesTheRetiredTooGoodTakeCell()
    {
        double[] noDoubleEquities = [-0.50, 0.0, 0.60, 0.999, 1.0, 1.001, 1.1711, 1.30, 1.80, 2.50];
        double[] doubleTakeEquities = [-0.30, 0.10, 0.6004, 0.999, 1.0, 1.001, 1.50];

        foreach (var nd in noDoubleEquities)
        foreach (var dt in doubleTakeEquities)
        {
            Assert.NotEqual(CubeClaimPair.TooGoodTake, MakeCube(nd, dt).BestClaimPair);
        }
    }

    // ---------------------------------------------------------------------
    //  The incoherent cell as derived truth — boundary only
    // ---------------------------------------------------------------------

    // Off the tie boundary, (NoDouble, Pass) is unreachable as truth: a
    // derived NoDouble claim with a derived Pass (DoubleTakeEquity >= 1)
    // means min(DoubleTakeEquity, 1) = 1 — for NoDouble to be best requires
    // NoDoubleEquity >= 1, and for the claim to stay NoDouble rather than
    // TooGood requires NoDoubleEquity <= 1, leaving exactly
    // NoDoubleEquity == 1. This is the halheinrich/backgammon#86
    // unreachability insight restated at the claim layer, pinned over a
    // grid of off-boundary equities; the amended predicate's pass term does
    // not move the boundary.
    [Fact]
    public void BestClaimPair_OffTheBoundary_IsNeverIncoherent()
    {
        double[] noDoubleEquities = [-0.50, 0.0, 0.20, 0.60, 0.95, 0.999, 1.001, 1.20, 1.80];
        double[] doubleTakeEquities = [-0.30, 0.10, 0.60, 0.999, 1.0, 1.001, 1.40];

        foreach (var nd in noDoubleEquities)
        foreach (var dt in doubleTakeEquities)
        {
            Assert.False(MakeCube(nd, dt).BestClaimPair.IsIncoherent,
                $"derived truth for nd={nd}, dt={dt} must not be the incoherent cell");
        }
    }

    // AT the boundary — NoDoubleEquity exactly 1 with DoubleTakeEquity >= 1 —
    // both halves tie, and the ruled tie-breaks (NoDouble; Pass) compose to
    // the incoherent cell as derived truth. Every answer's equity is
    // identical there, so nothing is at stake in scoring; the spec-literal
    // reading, ruled acceptable 2026-09-01 (SPEC-scoring §3's sixth-cell
    // ruling) and unchanged by the 2026-09-02 amendment.
    [Theory]
    [InlineData(1.0, 1.0)]
    [InlineData(1.0, 1.20)]
    public void BestClaimPair_AtTheDoubleTieBoundary_IsTheIncoherentCell(
        double noDoubleEquity, double doubleTakeEquity)
    {
        Assert.Equal(
            CubeClaimPair.NoDoublePass,
            MakeCube(noDoubleEquity, doubleTakeEquity).BestClaimPair);
    }

    // ---------------------------------------------------------------------
    //  Context-freedom — the Jacoby-redouble ruling
    // ---------------------------------------------------------------------

    // Too Good genuinely occurs in money under Jacoby via redoubles — a
    // turned cube re-arms gammons (SPEC-scoring §3, "Uniform availability").
    // The derivation reads equities only, so no rules context can suppress
    // the claim: a full money/Jacoby record in redouble posture derives Too
    // Good from the same three equities as any match position. (Whether the
    // verdict is offered at a position is the separate offerability fact,
    // BgDecisionData.CanBeTooGood, pinned in its own suite.)
    [Fact]
    public void Derivation_IsContextFree_JacobyRedoubleDerivesTooGood()
    {
        var record = new BgDecisionData
        {
            Id = new XgpDecisionId("jacoby-redouble.xgp"),
            Position = new PositionData
            {
                Mop = new int[26],
                OnRollNeeds = 0,            // money session
                OpponentNeeds = 0,
                IsJacoby = true,
                CubeSize = 2,               // cube already turned:
                CubeOwner = CubeOwner.OnRoll // a redouble decision
            },
            Decision = new DecisionData
            {
                IsCube = true,
                NoDoubleEquity = 1.15,      // playing on (gammons re-armed)
                DoubleTakeEquity = 1.30     // beats cashing; opponent passes
            }
        };

        Assert.Equal(CubeClaim.TooGood, record.Decision.BestDoublerClaim);
        Assert.Equal(CubeClaimPair.TooGoodPass, record.Decision.BestClaimPair);
    }

    // ---------------------------------------------------------------------
    //  IsCube guard and serialization posture — sibling parity
    // ---------------------------------------------------------------------

    [Fact]
    public void ClaimDerivation_Throws_WhenNotCube()
    {
        var play = new DecisionData();   // IsCube defaults to false

        Assert.Throws<InvalidOperationException>(() => _ = play.BestDoublerClaim);
        Assert.Throws<InvalidOperationException>(() => _ = play.BestClaimPair);
    }

    // Without [JsonIgnore], System.Text.Json would invoke the throwing
    // getters on every play decision it serialises — the BestDoublerAction
    // precedent, extended to the claim members.
    [Fact]
    public void ClaimDerivation_IsNotSerialised()
    {
        string json = JsonSerializer.Serialize(MakeCube(1.30, 1.50));

        Assert.DoesNotContain("\"BestDoublerClaim\"", json);
        Assert.DoesNotContain("\"BestClaimPair\"", json);
    }
}
