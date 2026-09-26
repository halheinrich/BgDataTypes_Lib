using System.Globalization;
using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The XGID is derived from the record (halheinrich/backgammon#273, the
/// match-context leg): <see cref="BgDecisionData.Xgid"/> writes eXtreme
/// Gammon's position string from the record's own members, and no record
/// stores one. Pinned here against known XGID strings — literals copied from
/// XG's own output (in the converted corpus) and two the builders' defaults
/// used to store — each record stating its facts independently of the string,
/// so a field the derivation spelled wrongly fails. Nothing here reads
/// <c>TestData/</c>; the converter's corpus test checks real data in its own
/// leg.
/// </summary>
public class XgidDerivationTests
{
    private static PositionData Position(int[] mop, int cubeSize, CubeOwner owner, Session session) =>
        TestRecords.Position(mop: new BoardPosition(mop), cubeSize: cubeSize, cubeOwner: owner, session: session);

    /// <summary>A checker play at <paramref name="position"/>: the builders' pass, valid from every board, since the XGID reads no candidate.</summary>
    private static BgDecisionData Play(PositionData position, int[] dice) => TestRecords.CheckerPlay(
        position: position,
        decision: position.Mop == BoardPosition.Standard
            ? TestRecords.CheckerPlayData(dice: dice)
            : TestRecords.CheckerPlayData(dice: dice, plays: [TestRecords.Candidate(play: [])]));

    private static BgDecisionData Cube(PositionData position) => TestRecords.Cube(position: position);

    /// <summary>XG: a cube decision in a 3-point match at 1-1, the opponent owning a cube of 2.</summary>
    private static BgDecisionData OpponentsCube() =>
        Cube(Position([0, 0, 0, 0, 0, 0, 4, 0, 4, 0, 0, 0, 0, 4, 0, 0, 0, 0, 0, 2, 0, 0, 0, -6, -5, 1], 2, CubeOwner.Opponent,
            TestRecords.MatchSession(length: 3, onRollNeeds: 2, opponentNeeds: 2)));

    /// <summary>XG: a money cube decision without Jacoby, the player on roll owning a cube of 4.</summary>
    private static BgDecisionData MoneyCube() =>
        Cube(Position([0, 0, 3, 1, -2, -2, 3, 0, 5, 3, 0, 0, 0, 0, 0, 0, 0, -2, 0, -4, -3, -1, -1, 0, 0, 0], 4, CubeOwner.OnRoll,
            TestRecords.MoneySession(isJacoby: false)));

    public static TheoryData<string, BgDecisionData, string> Known => new()
    {
        {
            // The builders' opening 3-1 at 0-0 in a 7-point match: the XGID
            // TestRecords used to store.
            "match: the opening",
            TestRecords.CheckerPlay(),
            "XGID=-b----E-C---eE---c-e----B-:0:0:1:31:0:0:0:7:10"
        },
        {
            // The builders' race double/take: the other XGID TestRecords stored.
            "match: a cube decision",
            TestRecords.Cube(),
            "XGID=-BBCCCB------------ddcbaa-:0:0:1:00:0:0:0:7:10"
        },
        {
            // XG: the Crawford game of a 5-point match, the player on roll at
            // match point (4 won, needing 1), the opponent 2 won.
            "match: the Crawford game",
            Play(Position([0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, -1, 0], 1, CubeOwner.Centered,
                TestRecords.MatchSession(length: 5, onRollNeeds: 1, opponentNeeds: 3, isCrawford: true)), [6, 2]),
            "XGID=------A----------------aa-:0:0:1:62:4:2:1:5:10"
        },
        {
            // XG: the player on roll with a checker on the bar.
            "match: the opponent's cube",
            OpponentsCube(),
            "XGID=------D-D----D-----B---feA:1:-1:1:00:1:1:0:3:10"
        },
        {
            // XG: both bars occupied, a 5-point match at 2-0.
            "match: both bars",
            Play(Position([-1, 0, 0, 0, 1, -1, 4, 0, 3, 0, 0, 0, -3, 4, 0, 0, 0, -3, 0, -3, -2, 0, 2, -2, 0, 1], 1, CubeOwner.Centered,
                TestRecords.MatchSession(length: 5, onRollNeeds: 3, opponentNeeds: 5)), [6, 3]),
            "XGID=a---AaD-C---cD---c-cb-Bb-A:0:0:1:63:2:0:0:5:10"
        },
        {
            // XG: money under Jacoby, the session at 1-21, the player on roll
            // owning a cube of 2.
            "money: Jacoby, the session's scores",
            Play(Position([0, 0, 0, 0, 0, 0, 4, 1, 3, -2, 0, 2, 0, 2, 0, 0, -2, -2, 0, -3, -4, -2, 0, 0, 3, 0], 2, CubeOwner.OnRoll,
                TestRecords.MoneySession(isJacoby: true, isBeaver: false, cubeLimit: 1024, onRollScore: 1, opponentScore: 21)), [4, 4]),
            "XGID=------DACb-B-B--bb-cdb--C-:1:1:1:44:1:21:1:0:10"
        },
        {
            "money: no Jacoby, a cube of 4",
            MoneyCube(),
            "XGID=--CAbbC-EC-------b-dcaa---:2:1:1:00:0:0:0:0:10"
        },
        {
            // The same position under the beaver and Jacoby rules with a cube
            // limit of 64: field 8 is 1 + 2, field 10 the limit's exponent.
            "money: beaver, Jacoby, a cube limit",
            Cube(Position([0, 0, 3, 1, -2, -2, 3, 0, 5, 3, 0, 0, 0, 0, 0, 0, 0, -2, 0, -4, -3, -1, -1, 0, 0, 0], 4, CubeOwner.OnRoll,
                TestRecords.MoneySession(isJacoby: true, isBeaver: true, cubeLimit: 64, onRollScore: 7, opponentScore: 0))),
            "XGID=--CAbbC-EC-------b-dcaa---:2:1:1:00:7:0:3:0:6"
        },
    };

    [Theory]
    [MemberData(nameof(Known))]
    public void TheDerivedXgid_IsTheKnownString(string what, BgDecisionData record, string expected)
    {
        Assert.True(expected == record.Xgid, $"{what}: expected {expected}, derived {record.Xgid}");
    }

    [Theory]
    [MemberData(nameof(Known))]
    public void TheDerivation_SurvivesTheRecordsRoundTrip_BothPaths(string what, BgDecisionData record, string expected)
    {
        // The XGID is not on the wire; the record read back derives it again
        // from the members that are.
        foreach (var (path, options) in WirePaths.Both)
        {
            string json = JsonSerializer.Serialize(record, options);
            Assert.DoesNotContain("\"Xgid\"", json);
            Assert.True(expected == JsonSerializer.Deserialize<BgDecisionData>(json, options)!.Xgid, $"{what} on the {path} path");
        }
    }

    // ── Field by field ────────────────────────────────────────────

    private static string[] Fields(BgDecisionData record)
    {
        Assert.StartsWith("XGID=", record.Xgid);
        return record.Xgid["XGID=".Length..].Split(':');
    }

    [Fact]
    public void TheRoll_IsWrittenHighDieFirst_WhateverOrderItWasRolledIn()
    {
        // XG writes the high die first (every XGID in the corpus does); a
        // record's rolled order is its own fact, not the XGID's.
        Assert.Equal("31", Fields(TestRecords.CheckerPlay(decision: TestRecords.CheckerPlayData(dice: [1, 3])))[4]);
        Assert.Equal("31", Fields(TestRecords.CheckerPlay(decision: TestRecords.CheckerPlayData(dice: [3, 1])))[4]);
        Assert.Equal("00", Fields(TestRecords.Cube())[4]);
    }

    [Theory]
    [InlineData(CubeOwner.OnRoll, "1")]
    [InlineData(CubeOwner.Centered, "0")]
    [InlineData(CubeOwner.Opponent, "-1")]
    public void TheCubesOwner_IsSpelledFromThePlayerOnRollsSide(CubeOwner owner, string field)
    {
        var record = TestRecords.Cube(position: TestRecords.Position(
            mop: new BoardPosition([0, 2, 2, 3, 3, 3, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -4, -4, -3, -2, -1, -1, 0]),
            cubeSize: 8, cubeOwner: owner));

        Assert.Equal(["3", field, "1"], Fields(record)[1..4]);
    }

    [Fact]
    public void AMatchsScores_AreItsLengthLessEachAwayScore_ThePlayerOnRollsFirst()
    {
        var record = TestRecords.CheckerPlay(position: TestRecords.Position(
            session: TestRecords.MatchSession(length: 11, onRollNeeds: 4, opponentNeeds: 9)));

        Assert.Equal(["7", "2", "0", "11", "10"], Fields(record)[5..]);
    }

    [Theory]
    [InlineData(false, false, "0")]
    [InlineData(true, false, "1")]
    [InlineData(false, true, "2")]
    [InlineData(true, true, "3")]
    public void AMoneySessionsRules_AreJacobyPlusTwiceBeaver_WithMoneysLengthOfZero(bool jacoby, bool beaver, string field)
    {
        var record = TestRecords.CheckerPlay(position: TestRecords.Position(
            session: TestRecords.MoneySession(isJacoby: jacoby, isBeaver: beaver, cubeLimit: 256, onRollScore: 12, opponentScore: 30)));

        Assert.Equal(["12", "30", field, "0", "8"], Fields(record)[5..]);
    }

    [Fact]
    public void AMatchsMaxCube_IsXgsDefault_ForAMatchHasNoCubeLimit()
    {
        // A match has no cube limit; XG writes 10 (2^10) in every match XGID.
        Assert.Equal("10", Fields(TestRecords.CheckerPlay())[9]);
        Assert.Equal(10, XgidEncoder.MatchMaxCubeField);
        Assert.Null(typeof(MatchSession).GetProperty("CubeLimit"));
    }

    [Fact]
    public void ThePosition_SpellsEachCountAndBothBars()
    {
        // Fifteen on one point and on the other side's: the letter range's ends.
        var record = TestRecords.CheckerPlay(position: TestRecords.Position(
            mop: new BoardPosition([-1, 15, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -14, 0])));

        Assert.Equal("aO----------------------n-", Fields(record)[0]);
    }

    [Theory]
    [InlineData("sv-SE")]   // writes its minus sign as U+2212 under ICU
    [InlineData("de-DE")]
    [InlineData("ar-SA")]
    public void TheXgid_IsCultureInvariant(string cultureName)
    {
        // The opponent's -1 cube position would change its sign under a
        // culture's minus; every number is written invariant.
        var record = OpponentsCube();
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(cultureName);
            Assert.Equal("XGID=------D-D----D-----B---feA:1:-1:1:00:1:1:0:3:10", record.Xgid);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    // ── Derived, never stored ─────────────────────────────────────

    [Fact]
    public void AStatedXgid_IsIgnored_AndTheDerivationStands_BothPaths()
    {
        // A document still stating an XGID — even one contradicting the
        // record — reads with it ignored, as every derived member's is.
        var record = TestRecords.CheckerPlay();
        var document = WirePaths.Document<BgDecisionData>(record);
        document["Xgid"] = "XGID=-b----E-C---eE---c-e----B-:0:0:1:31:0:0:1:0:10";

        foreach (var (_, options) in WirePaths.Both)
            Assert.Equal(record.Xgid, JsonSerializer.Deserialize<BgDecisionData>(document.ToJsonString(), options)!.Xgid);
    }

    [Fact]
    public void TheRowsXgid_IsTheRecords()
    {
        foreach (BgDecisionData record in new[] { TestRecords.CheckerPlay(), MoneyCube() })
            foreach (var ranking in Enum.GetValues<PlayRanking>())
                Assert.Equal(record.Xgid, DecisionRow.From(record, ranking).Xgid);
    }

    [Fact]
    public void TheXgid_CannotBeStated()
    {
        var xgid = typeof(BgDecisionData).GetProperty(nameof(BgDecisionData.Xgid))!;

        Assert.Null(xgid.SetMethod);
        Assert.True(xgid.IsDefined(typeof(System.Text.Json.Serialization.JsonIgnoreAttribute), inherit: false));
    }
}
