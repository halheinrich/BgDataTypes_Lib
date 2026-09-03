using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Pins the offerability fact of SPEC-scoring §3's 2026-09-02 amendment
/// (halheinrich/backgammon#187): <see cref="BgDecisionData.CanBeTooGood"/>
/// is <see langword="false"/> exactly for a money position under a known
/// Jacoby rule with the cube centred, and <see langword="true"/> otherwise —
/// the one derivation site consumers read to decide whether the Too Good
/// pair is in the option set. Pinned in both directions as the amendment
/// requires (money-Jacoby-centred → not offered; the same position with the
/// cube turned → offered), plus every rung that must stay
/// <see langword="true"/>: match play, an unknown rule, a known non-Jacoby
/// rule. Fixtures are constructed in code per the TestData rule.
/// </summary>
public class BgDecisionDataTooGoodOfferabilityTests
{
    // Id is required on BgDecisionData; CanBeTooGood never reads it.
    private static readonly DecisionId AnyId = new XgpDecisionId("x.xgp");

    // Money is MatchLength == 0 by the contract's single spelling
    // (IDecisionFilterData.IsMoneyGame); the fixture states the length and
    // lets the record derive money from it, never the other way round.
    private static BgDecisionData Make(
        int matchLength, bool? isJacoby, CubeOwner cubeOwner, bool isCube = true,
        double noDoubleEquity = 0.50, double doubleTakeEquity = 0.70)
        => new()
        {
            Id = AnyId,
            Position = new PositionData
            {
                Mop = new int[26],
                IsJacoby = isJacoby,
                CubeSize = cubeOwner == CubeOwner.Centered ? 1 : 2,
                CubeOwner = cubeOwner
            },
            Descriptive = new DescriptiveData { MatchLength = matchLength },
            Decision = new DecisionData
            {
                IsCube = isCube,
                NoDoubleEquity = noDoubleEquity,
                DoubleTakeEquity = doubleTakeEquity
            }
        };

    // ---------------------------------------------------------------------
    //  The one false cell, and its turned-cube twin
    // ---------------------------------------------------------------------

    // Money, Jacoby known in force, cube centred: gammons do not count until
    // the cube turns, so the no-double equity never exceeds the cash and
    // Too Good cannot occur — not offered.
    [Fact]
    public void CanBeTooGood_MoneyJacobyCentred_IsFalse()
    {
        Assert.False(Make(0, true, CubeOwner.Centered).CanBeTooGood);
    }

    // The same position with the cube turned — a redouble decision, either
    // owner — re-arms gammons, and Too Good is offered again (the Jacoby
    // redouble case SPEC-scoring §3's uniform-availability bullet names).
    [Theory]
    [InlineData(CubeOwner.OnRoll)]
    [InlineData(CubeOwner.Opponent)]
    public void CanBeTooGood_SamePositionWithTheCubeTurned_IsTrue(CubeOwner owner)
    {
        Assert.True(Make(0, true, owner).CanBeTooGood);
    }

    // ---------------------------------------------------------------------
    //  Every other rung is true
    // ---------------------------------------------------------------------

    // Match play: the Jacoby rule does not apply, whatever a producer stamped
    // — a non-null IsJacoby on a match record is tolerated (PositionData's
    // contract) and must not turn a match position into the money cell.
    [Theory]
    [InlineData(7, null)]
    [InlineData(7, true)]
    [InlineData(7, false)]
    [InlineData(1, null)]
    public void CanBeTooGood_MatchPlay_IsTrue(int matchLength, bool? isJacoby)
    {
        Assert.True(Make(matchLength, isJacoby, CubeOwner.Centered).CanBeTooGood);
    }

    // An unknown rule is not a known Jacoby rule: a money record whose rule
    // was never stamped keeps Too Good offered. This is the near-miss the
    // filter-layer contract warns about — `IsJacoby != false` would admit
    // the unknown record into the withheld cell, and this pin is what
    // catches that spelling.
    [Fact]
    public void CanBeTooGood_MoneyWithUnknownRule_IsTrue()
    {
        Assert.True(Make(0, null, CubeOwner.Centered).CanBeTooGood);
    }

    // Money without Jacoby: gammons count from the start, Too Good occurs.
    [Fact]
    public void CanBeTooGood_MoneyWithoutJacoby_IsTrue()
    {
        Assert.True(Make(0, false, CubeOwner.Centered).CanBeTooGood);
    }

    // ---------------------------------------------------------------------
    //  Separation from the claim derivation
    // ---------------------------------------------------------------------

    // Offerability reads the rules context only; the claim reads equities
    // only. A money-Jacoby-centred record whose producer's numbers happen to
    // derive Too Good still reports the verdict as not offerable — the two
    // facts are independent, and neither re-derives the other.
    [Fact]
    public void CanBeTooGood_IsIndependentOfWhatTheEquitiesDerive()
    {
        var record = Make(0, true, CubeOwner.Centered,
            noDoubleEquity: 1.30, doubleTakeEquity: 1.50);

        Assert.Equal(CubeClaimPair.TooGoodPass, record.Decision.BestClaimPair);
        Assert.False(record.CanBeTooGood);
    }

    // ---------------------------------------------------------------------
    //  IsCube guard and serialization posture — parity with BestClaimPair
    // ---------------------------------------------------------------------

    [Fact]
    public void CanBeTooGood_Throws_WhenNotCube()
    {
        var play = Make(0, true, CubeOwner.Centered, isCube: false);

        var ex = Assert.Throws<InvalidOperationException>(() => _ = play.CanBeTooGood);
        var sibling = Assert.Throws<InvalidOperationException>(() => _ = play.Decision.BestClaimPair);
        Assert.Equal(sibling.Message, ex.Message);
    }

    // A derivation, not wire: the top level of the JSON stays the six stored
    // members (halheinrich/backgammon#14), and the throwing getter must not
    // run when a checker play is serialised.
    [Fact]
    public void CanBeTooGood_IsNotSerialised()
    {
        string cubeJson = JsonSerializer.Serialize(Make(0, true, CubeOwner.Centered));
        Assert.DoesNotContain("CanBeTooGood", cubeJson, StringComparison.OrdinalIgnoreCase);

        var play = Make(7, null, CubeOwner.Centered, isCube: false);
        string playJson = JsonSerializer.Serialize(play);   // must not throw
        Assert.DoesNotContain("CanBeTooGood", playJson, StringComparison.OrdinalIgnoreCase);
    }
}
