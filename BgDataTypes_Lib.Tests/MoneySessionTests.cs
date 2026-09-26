using System.Text.Json;
using System.Text.Json.Nodes;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// A money session's facts (<see cref="MoneySession"/>,
/// halheinrich/backgammon#273): its terms — the Jacoby and beaver rules and
/// the cube limit, composed as a <see cref="MoneyTerms"/> member, never
/// restated beside it (the umbrella's review of 2026-09-26) — and the
/// session's scores, the player on roll's first. The three facts only the
/// stored XGID used to carry are typed members. Every one is required; the
/// cube limit is a positive power of two and each score at least 0, refused
/// otherwise from code (the guard's exception, naming the member) and from a
/// document (a <see cref="JsonException"/> on both paths, carrying it).
/// </summary>
public class MoneySessionTests
{
    [Fact]
    public void EveryFact_RoundTrips_BothPaths()
    {
        var money = TestRecords.MoneySession(isJacoby: false, isBeaver: true, cubeLimit: 32, onRollScore: 4, opponentScore: 17);

        foreach (var (_, options) in WirePaths.Both)
        {
            var restored = Assert.IsType<MoneySession>(WirePaths.RoundTrip<Session>(money, options));
            Assert.Equal((false, true, 32, 4, 17),
                (restored.Terms.IsJacoby, restored.Terms.IsBeaver, restored.Terms.CubeLimit, restored.OnRollScore, restored.OpponentScore));
            Assert.Equal(money, restored);
        }
    }

    [Fact]
    public void TheTerms_AreComposed_NotRestated()
    {
        // Rewritten for the composed session: the rules and the limit are
        // the terms' members, and the session states only its scores beside
        // them.
        Assert.Equal(typeof(MoneyTerms), typeof(MoneySession).GetProperty(nameof(MoneySession.Terms))!.PropertyType);
        foreach (var restated in new[] { "IsJacoby", "IsBeaver", "CubeLimit" })
            Assert.Null(typeof(MoneySession).GetProperty(restated));

        var terms = new MoneyTerms { IsJacoby = false, IsBeaver = true, CubeLimit = 8 };
        var money = (MoneySession)Session.Create(terms, new MoneyStanding { Score1 = 0, Score2 = 0 }, Seat.Player1);
        Assert.Same(terms, money.Terms);
    }

    public static TheoryData<string> Members =>
        ["Terms", "Terms.IsJacoby", "Terms.IsBeaver", "Terms.CubeLimit", "Terms.Kind", "OnRollScore", "OpponentScore"];

    [Theory]
    [MemberData(nameof(Members))]
    public void EveryFact_IsRequired_OnBothPaths(string member)
    {
        // A money session states each: none is read as a default — not the
        // rule "off", not a limit or a score of 0, not the terms themselves.
        var document = WirePaths.Document<Session>(TestRecords.MoneySession());
        var owner = member.StartsWith("Terms.", StringComparison.Ordinal) ? document["Terms"]!.AsObject() : document;
        owner.Remove(member.Split('.')[^1]);

        WirePaths.AssertRefused<Session>(document.ToJsonString());
        WirePaths.AssertRefused<MoneySession>(document.ToJsonString());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(64)]
    [InlineData(1024)]
    [InlineData(1 << 30)]
    public void ACubeLimit_IsAPositivePowerOfTwo(int limit)
    {
        Assert.Equal(limit, TestRecords.MoneySession(cubeLimit: limit).Terms.CubeLimit);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    [InlineData(3)]
    [InlineData(1000)]
    [InlineData(int.MinValue)]
    public void ACubeLimitThatIsNotAPositivePowerOfTwo_IsRefused_FromCodeAndFromADocument(int limit)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => TestRecords.MoneySession(cubeLimit: limit));
        Assert.Equal("CubeLimit", ex.ParamName);
        Assert.Contains(SessionRules.CubeLimitMessage, ex.Message);

        var document = WirePaths.Document<Session>(TestRecords.MoneySession());
        document["Terms"]!["CubeLimit"] = limit;
        Assert.IsType<ArgumentOutOfRangeException>(WirePaths.AssertRefused<Session>(document.ToJsonString()).InnerException);
    }

    [Theory]
    [InlineData("OnRollScore", "Score1")]
    [InlineData("OpponentScore", "Score2")]
    public void ANegativeScore_IsRefused_ByTheStandingInCode_AndByTheSessionInADocument(string member, string seatMember)
    {
        // Rewritten for the composed session: code builds a session from a
        // standing, whose own guard refuses the score (the player on roll in
        // seat 1, so the on-roll score is player 1's); a document states the
        // session's own members, whose guard refuses it.
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => member == "OnRollScore"
            ? TestRecords.MoneySession(onRollScore: -1)
            : TestRecords.MoneySession(opponentScore: -1));
        Assert.Equal(seatMember, ex.ParamName);
        Assert.Contains(SessionRules.ScoreMessage, ex.Message);

        var document = WirePaths.Document<Session>(TestRecords.MoneySession());
        document[member] = -1;
        var refusal = Assert.IsType<ArgumentOutOfRangeException>(WirePaths.AssertRefused<Session>(document.ToJsonString()).InnerException);
        Assert.Equal(member, refusal.ParamName);
    }

    [Fact]
    public void ANullOrNonObjectTerms_IsRefused_BothPaths()
    {
        foreach (JsonNode? terms in new JsonNode?[] { null, JsonValue.Create("Money"), new JsonArray() })
        {
            var document = WirePaths.Document<Session>(TestRecords.MoneySession());
            document["Terms"] = terms;
            WirePaths.AssertRefused<Session>(document.ToJsonString());
            WirePaths.AssertRefused<MoneySession>(document.ToJsonString());
        }
    }

    [Fact]
    public void EachFact_TellsTwoSessionsApart()
    {
        var money = TestRecords.MoneySession();

        Assert.Equal(money, TestRecords.MoneySession());
        Assert.Equal(money.GetHashCode(), TestRecords.MoneySession().GetHashCode());
        Assert.NotEqual(money, TestRecords.MoneySession(isJacoby: false));
        Assert.NotEqual(money, TestRecords.MoneySession(isBeaver: true));
        Assert.NotEqual(money, TestRecords.MoneySession(cubeLimit: 64));
        Assert.NotEqual(money, TestRecords.MoneySession(onRollScore: 1));
        Assert.NotEqual(money, TestRecords.MoneySession(opponentScore: 1));
    }
}
