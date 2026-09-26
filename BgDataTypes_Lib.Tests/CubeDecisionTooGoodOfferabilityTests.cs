using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Pins the offerability fact of SPEC-scoring §3's 2026-09-02 amendment
/// (halheinrich/backgammon#187): <see cref="CubeDecision.CanBeTooGood"/>
/// is <see langword="false"/> exactly for a money position under a known
/// Jacoby rule with the cube centred, and <see langword="true"/> otherwise —
/// the one derivation site consumers read to decide whether the Too Good
/// pair is in the option set. Pinned in both directions as the amendment
/// requires (money-Jacoby-centred → not offered; the same position with the
/// cube turned → offered), plus every rung that must stay
/// <see langword="true"/>: match play and a money session without the Jacoby
/// rule. The unknown rule it once also pinned is not expressible any more: a
/// money session states its rule (halheinrich/backgammon#273). Fixtures are
/// constructed in code per the TestData rule.
/// </summary>
public class CubeDecisionTooGoodOfferabilityTests
{
    // Rewritten: money is the session's kind, stated as a money session with
    // its rule — never a match length of 0 the record derived money from.
    private static CubeDecision Make(
        Session session, CubeOwner cubeOwner,
        double noDoubleEquity = 0.50, double doubleTakeEquity = 0.70)
        => TestRecords.Cube(
            position: TestRecords.Position(
                cubeSize: cubeOwner == CubeOwner.Centered ? 1 : 2,
                cubeOwner: cubeOwner,
                session: session),
            decision: TestRecords.CubeData(
                noDoubleEquity: noDoubleEquity,
                doubleTakeEquity: doubleTakeEquity));

    private static MoneySession Money(bool isJacoby) => TestRecords.MoneySession(isJacoby: isJacoby);

    // ---------------------------------------------------------------------
    //  The one false cell, and its turned-cube twin
    // ---------------------------------------------------------------------

    // Money, Jacoby known in force, cube centred: gammons do not count until
    // the cube turns, so the no-double equity never exceeds the cash and
    // Too Good cannot occur — not offered.
    [Fact]
    public void CanBeTooGood_MoneyJacobyCentred_IsFalse()
    {
        Assert.False(Make(Money(isJacoby: true), CubeOwner.Centered).CanBeTooGood);
    }

    // The same position with the cube turned — a redouble decision, either
    // owner — re-arms gammons, and Too Good is offered again (the Jacoby
    // redouble case SPEC-scoring §3's uniform-availability bullet names).
    [Theory]
    [InlineData(CubeOwner.OnRoll)]
    [InlineData(CubeOwner.Opponent)]
    public void CanBeTooGood_SamePositionWithTheCubeTurned_IsTrue(CubeOwner owner)
    {
        Assert.True(Make(Money(isJacoby: true), owner).CanBeTooGood);
    }

    // ---------------------------------------------------------------------
    //  Every other rung is true
    // ---------------------------------------------------------------------

    // Match play: the Jacoby rule does not apply, and a match cannot state
    // one — the stamp on a match record this pin once tolerated has no member
    // to sit in. Rewritten from CanBeTooGood_MatchPlay_IsTrue(matchLength, isJacoby).
    [Theory]
    [InlineData(7)]
    [InlineData(1)]
    public void CanBeTooGood_MatchPlay_IsTrue(int length)
    {
        var match = TestRecords.MatchSession(length: length, onRollNeeds: length, opponentNeeds: length);

        Assert.True(Make(match, CubeOwner.Centered).CanBeTooGood);
    }

    // Rewritten from CanBeTooGood_MoneyWithUnknownRule_IsTrue: the unknown
    // rule is gone, and with it the near-miss `IsJacoby != false` it caught.
    // What stays load-bearing is that exactly one cell is withheld, which
    // every session and owner pins at once — a spelling that admitted any
    // other cell, or missed this one, fails here.
    [Fact]
    public void CanBeTooGood_IsFalseInExactlyOneCell_OfEverySessionAndOwner()
    {
        Session[] sessions = [Money(isJacoby: true), Money(isJacoby: false), TestRecords.MatchSession()];
        var withheld = new List<string>();
        foreach (var session in sessions)
            foreach (var owner in Enum.GetValues<CubeOwner>())
                if (!Make(session, owner).CanBeTooGood)
                    withheld.Add($"{session.Kind}{(session is MoneySession { Terms.IsJacoby: true } ? "J" : "")}/{owner}");

        Assert.Equal(["MoneyJ/Centered"], withheld);
    }

    // Money without Jacoby: gammons count from the start, Too Good occurs.
    [Fact]
    public void CanBeTooGood_MoneyWithoutJacoby_IsTrue()
    {
        Assert.True(Make(Money(isJacoby: false), CubeOwner.Centered).CanBeTooGood);
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
        var record = Make(Money(isJacoby: true), CubeOwner.Centered,
            noDoubleEquity: 1.30, doubleTakeEquity: 1.50);

        Assert.Equal(CubeClaimPair.TooGoodPass, record.Decision.BestClaimPair);
        Assert.False(record.CanBeTooGood);
    }

    // ---------------------------------------------------------------------
    //  A cube decision's member only, and serialization posture
    // ---------------------------------------------------------------------

    [Fact]
    public void CanBeTooGood_IsNotAMemberOfACheckerPlay()
    {
        // Rewritten from CanBeTooGood_Throws_WhenNotCube: the question has no
        // meaning on a checker play, and asking it no longer compiles, so
        // there is no guard to throw — nor on the claim it sits beside.
        Assert.NotNull(typeof(CubeDecision).GetProperty(nameof(CubeDecision.CanBeTooGood)));
        Assert.Null(typeof(CheckerPlayDecision).GetProperty(nameof(CubeDecision.CanBeTooGood)));
        Assert.Null(typeof(BgDecisionData).GetProperty(nameof(CubeDecision.CanBeTooGood)));
        Assert.Null(typeof(CheckerPlayDecisionData).GetProperty(nameof(CubeDecisionData.BestClaimPair)));
    }

    // A derivation, not wire (halheinrich/backgammon#14).
    [Fact]
    public void CanBeTooGood_IsNotSerialised_BothPaths()
    {
        // Rewritten from CanBeTooGood_IsNotSerialised: no checker-play half
        // to guard any more; both paths now.
        foreach (var (_, options) in WirePaths.Both)
        {
            string json = JsonSerializer.Serialize<BgDecisionData>(Make(Money(isJacoby: true), CubeOwner.Centered), options);
            Assert.DoesNotContain("CanBeTooGood", json, StringComparison.OrdinalIgnoreCase);
        }
    }
}
