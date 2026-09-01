using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Pins the truth-claim derivation of SPEC-scoring §3
/// (halheinrich/backgammon#86): Too Good ⟺ best doubler action is NoDouble
/// AND NoDoubleEquity &gt; 1 — implemented as
/// <see cref="DecisionData.BestDoublerClaim"/> beside its action-level
/// siblings, with <see cref="DecisionData.BestClaimPair"/> composing the
/// full derived truth. Equities are synthesized inline per the TestData
/// rule; the real-corpus exercise of the same predicate lives in
/// <see cref="TooGoodCorpusExerciseTests"/>.
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
    // Double: offering the cube is best against optimal response.
    [InlineData(0.30, 0.60, CubeClaim.Double)]     // min(0.60,1)=0.60 > 0.30
    [InlineData(0.50, 1.20, CubeClaim.Double)]     // min(1.20,1)=1.00 > 0.50 — a cash
    // TooGood: no-double is right AND playing on beats the cashed point.
    [InlineData(1.30, 1.50, CubeClaim.TooGood)]    // classic too good / pass
    [InlineData(1.1711, 0.6004, CubeClaim.TooGood)] // XG Position 3, the motivating case
                                                    // of halheinrich/backgammon#86:
                                                    // too good AND a take
    public void BestDoublerClaim_ReturnsExpected(
        double noDoubleEquity, double doubleTakeEquity, CubeClaim expected)
    {
        Assert.Equal(expected, MakeCube(noDoubleEquity, doubleTakeEquity).BestDoublerClaim);
    }

    // The Too Good boundary is strict: NoDoubleEquity exactly 1 (playing on
    // worth exactly the cash) is NOT too good — the spec predicate reads
    // "NoDoubleEquity > 1", and the tie keeps the ruled tie-favours-NoDouble
    // posture. Just above 1, the claim flips.
    [Theory]
    [InlineData(1.0, 0.90, CubeClaim.NoDouble)]        // at the boundary, taker would take
    [InlineData(1.0, 1.50, CubeClaim.NoDouble)]        // at the boundary, taker would pass
    [InlineData(1.000000001, 0.90, CubeClaim.TooGood)] // just above — too good / take
    [InlineData(1.000000001, 1.50, CubeClaim.TooGood)] // just above — too good / pass
    public void BestDoublerClaim_TooGoodBoundary_IsStrictlyAboveOne(
        double noDoubleEquity, double doubleTakeEquity, CubeClaim expected)
    {
        Assert.Equal(expected, MakeCube(noDoubleEquity, doubleTakeEquity).BestDoublerClaim);
    }

    // ---------------------------------------------------------------------
    //  BestClaimPair — the five verdict cells of SPEC-scoring §3's table
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(0.20, 0.10, CubeClaim.NoDouble, CubeAction.Take)]  // No double, take
    [InlineData(0.30, 0.60, CubeClaim.Double, CubeAction.Take)]    // Double, take
    [InlineData(0.50, 1.20, CubeClaim.Double, CubeAction.Pass)]    // Double, pass
    [InlineData(1.1711, 0.6004, CubeClaim.TooGood, CubeAction.Take)] // Too good, take
    [InlineData(1.30, 1.50, CubeClaim.TooGood, CubeAction.Pass)]   // Too good, pass
    public void BestClaimPair_ReachesEveryVerdictCell(
        double noDoubleEquity, double doubleTakeEquity,
        CubeClaim expectedClaim, CubeAction expectedTaker)
    {
        Assert.Equal(
            new CubeClaimPair(expectedClaim, expectedTaker),
            MakeCube(noDoubleEquity, doubleTakeEquity).BestClaimPair);
    }

    // ---------------------------------------------------------------------
    //  The incoherent cell as derived truth — boundary only
    // ---------------------------------------------------------------------

    // Off the tie boundary, (NoDouble, Pass) is unreachable as truth: a
    // derived NoDouble claim means NoDoubleEquity <= 1, and a derived Pass
    // means DoubleTakeEquity >= 1, so min(DoubleTakeEquity, 1) = 1 — for
    // NoDouble to be best requires NoDoubleEquity >= 1, leaving exactly
    // NoDoubleEquity == 1. This is the halheinrich/backgammon#86
    // unreachability insight restated
    // at the claim layer, pinned over a grid of off-boundary equities.
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
    // identical there, so nothing is at stake in scoring; this pins the
    // spec-literal reading, flagged to the umbrella as a candidate spec
    // sharpening rather than silently rounded away here (see
    // DecisionData.BestClaimPair remarks).
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

    // Too Good is uniformly available because it genuinely occurs in money
    // under Jacoby via redoubles — a turned cube re-arms gammons
    // (SPEC-scoring §3, "Uniform availability"). The derivation reads
    // equities only, so no rules context can suppress the claim: a full
    // money/Jacoby record in redouble posture derives Too Good from the
    // same three equities as any match position.
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
                DoubleTakeEquity = 0.70     // beats cashing; opponent takes
            }
        };

        Assert.Equal(CubeClaim.TooGood, record.Decision.BestDoublerClaim);
        Assert.Equal(CubeClaimPair.TooGoodTake, record.Decision.BestClaimPair);
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
