using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// A money session's facts (<see cref="MoneySession"/>,
/// halheinrich/backgammon#273): the Jacoby and beaver rules, the cube limit
/// and the session's scores — the three facts only the stored XGID used to
/// carry are typed members now. Every one is required; the cube limit is a
/// positive power of two and each score at least 0, refused otherwise from
/// code (the guard's exception, naming the member) and from a document (a
/// <see cref="JsonException"/> on both paths, carrying it).
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
                (restored.IsJacoby, restored.IsBeaver, restored.CubeLimit, restored.OnRollScore, restored.OpponentScore));
            Assert.Equal(money, restored);
        }
    }

    [Theory]
    [InlineData("IsJacoby")]
    [InlineData("IsBeaver")]
    [InlineData("CubeLimit")]
    [InlineData("OnRollScore")]
    [InlineData("OpponentScore")]
    public void EveryFact_IsRequired_OnBothPaths(string member)
    {
        // A money session states each: none is read as a default — not the
        // rule "off", not a limit or a score of 0.
        var document = WirePaths.Document<Session>(TestRecords.MoneySession());
        document.Remove(member);

        WirePaths.AssertRefused<Session>(document.ToJsonString());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(64)]
    [InlineData(1024)]
    [InlineData(1 << 30)]
    public void ACubeLimit_IsAPositivePowerOfTwo(int limit)
    {
        Assert.Equal(limit, TestRecords.MoneySession(cubeLimit: limit).CubeLimit);
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
        document["CubeLimit"] = limit;
        Assert.IsType<ArgumentOutOfRangeException>(WirePaths.AssertRefused<Session>(document.ToJsonString()).InnerException);
    }

    [Theory]
    [InlineData("OnRollScore")]
    [InlineData("OpponentScore")]
    public void ANegativeScore_IsRefused_FromCodeAndFromADocument(string member)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => member == "OnRollScore"
            ? TestRecords.MoneySession(onRollScore: -1)
            : TestRecords.MoneySession(opponentScore: -1));
        Assert.Equal(member, ex.ParamName);
        Assert.Contains(SessionRules.ScoreMessage, ex.Message);

        var document = WirePaths.Document<Session>(TestRecords.MoneySession());
        document[member] = -1;
        Assert.IsType<ArgumentOutOfRangeException>(WirePaths.AssertRefused<Session>(document.ToJsonString()).InnerException);
    }

    [Fact]
    public void EachFact_TellsTwoSessionsApart()
    {
        var money = TestRecords.MoneySession();

        Assert.Equal(money, TestRecords.MoneySession());
        Assert.NotEqual(money, TestRecords.MoneySession(isJacoby: false));
        Assert.NotEqual(money, TestRecords.MoneySession(isBeaver: true));
        Assert.NotEqual(money, TestRecords.MoneySession(cubeLimit: 64));
        Assert.NotEqual(money, TestRecords.MoneySession(onRollScore: 1));
        Assert.NotEqual(money, TestRecords.MoneySession(opponentScore: 1));
    }
}
