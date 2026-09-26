using System.Reflection;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The match-scope contract (<see cref="IMatchInfo"/>) and its terms
/// (<see cref="SessionTerms"/>): money versus match is the terms' kind
/// (halheinrich/backgammon#273), never a match length of 0.
/// </summary>
public class IMatchInfoTests
{
    /// <summary>A minimal implementer declaring only the contract's members.</summary>
    private sealed class FakeMatchInfo : IMatchInfo
    {
        public string Player1 { get; init; } = string.Empty;
        public string Player2 { get; init; } = string.Empty;
        public required SessionTerms Terms { get; init; }
    }

    private static MatchTerms Match(int length) => new() { Length = length };

    private static MoneyTerms Money(bool isJacoby = true, bool isBeaver = false, int cubeLimit = 1024) =>
        new() { IsJacoby = isJacoby, IsBeaver = isBeaver, CubeLimit = cubeLimit };

    [Fact]
    public void IMatchInfo_Members_SurfaceThroughContract()
    {
        // Rewritten: the match length reaches the contract as the match's terms.
        IMatchInfo info = new FakeMatchInfo { Player1 = "Mochy", Player2 = "Falafel", Terms = Match(9) };

        Assert.Equal("Mochy", info.Player1);
        Assert.Equal("Falafel", info.Player2);
        Assert.Equal(9, Assert.IsType<MatchTerms>(info.Terms).Length);
    }

    [Fact]
    public void IMatchInfo_AMoneySession_IsItsTermsKind()
    {
        // Rewritten from IMatchInfo_IsMoneyGame_TrueForUnlimitedSession:
        // money is stated as the terms' kind — with its rules — never read off
        // a match length of 0, which no member can hold.
        IMatchInfo info = new FakeMatchInfo { Player1 = "Mochy", Player2 = "Falafel", Terms = Money(isJacoby: false, isBeaver: true, cubeLimit: 64) };

        var money = Assert.IsType<MoneyTerms>(info.Terms);
        Assert.Equal(SessionKind.Money, info.Terms.Kind);
        Assert.Equal((false, true, 64), (money.IsJacoby, money.IsBeaver, money.CubeLimit));
    }

    [Theory]
    [InlineData(1)]  // shortest possible match
    [InlineData(7)]
    [InlineData(25)]
    public void IMatchInfo_AMatchOfAnyLength_IsAMatch(int length)
    {
        // Rewritten from IMatchInfo_IsMoneyGame_FalseForAnyMatchLength.
        IMatchInfo info = new FakeMatchInfo { Terms = Match(length) };

        Assert.Equal(SessionKind.Match, info.Terms.Kind);
        Assert.Equal("match", info.Terms.Match(money: _ => "money", match: _ => "match"));
    }

    [Fact]
    public void IMatchInfo_HasNoMatchLengthOrMoneyPredicate()
    {
        // The stand-ins are not expressible: no member for a length of 0, and
        // no IsMoneyGame to derive from one.
        var members = typeof(IMatchInfo).GetMembers().Select(m => m.Name).ToArray();

        Assert.DoesNotContain("MatchLength", members);
        Assert.DoesNotContain("IsMoneyGame", members);
    }

    // ── The terms ─────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]         // money's old stand-in length
    [InlineData(-1)]
    public void MatchTerms_ALengthBelowOne_IsRefused(int length)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Match(length));
        Assert.Equal("Length", ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void MoneyTerms_ACubeLimitThatIsNotAPositivePowerOfTwo_IsRefused(int cubeLimit)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Money(cubeLimit: cubeLimit));
        Assert.Equal("CubeLimit", ex.ParamName);
    }

    [Fact]
    public void TheKinds_ShareOnlyTheBasesMembers_AndAreAClosedPair()
    {
        static string[] Own(Type type) => [.. type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name).Except(typeof(SessionTerms).GetProperties().Select(p => p.Name)).Order()];

        Assert.Equal(["CubeLimit", "IsBeaver", "IsJacoby"], Own(typeof(MoneyTerms)));
        Assert.Equal(["Length"], Own(typeof(MatchTerms)));
        Assert.True(typeof(SessionTerms).IsAbstract);
        Assert.All(typeof(SessionTerms).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance),
            ctor => Assert.True(ctor.IsFamilyAndAssembly || ctor.IsAssembly || ctor.IsPrivate));
        Assert.Equal([typeof(MatchTerms), typeof(MoneyTerms)],
            typeof(SessionTerms).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(SessionTerms))).OrderBy(t => t.Name));
        Assert.True(typeof(MoneyTerms).IsSealed && typeof(MatchTerms).IsSealed);
    }

    [Fact]
    public void MatchAndSwitch_RunTheBranchOfTheKind_AndRefuseANullBranch()
    {
        foreach (SessionTerms terms in new SessionTerms[] { Money(), Match(5) })
        {
            string expected = terms.Kind == SessionKind.Money ? "money" : "match";
            string switched = "";
            terms.Switch(money: _ => switched = "money", match: _ => switched = "match");

            Assert.Equal(expected, terms.Match(money: _ => "money", match: _ => "match"));
            Assert.Equal(expected, switched);
            Assert.Throws<ArgumentNullException>(() => terms.Match<int>(null!, _ => 0));
            Assert.Throws<ArgumentNullException>(() => terms.Match<int>(_ => 0, null!));
            Assert.Throws<ArgumentNullException>(() => terms.Switch(null!, _ => { }));
            Assert.Throws<ArgumentNullException>(() => terms.Switch(_ => { }, null!));
        }
    }

    [Fact]
    public void Terms_AreValues()
    {
        Assert.Equal(Match(7), Match(7));
        Assert.True(Match(7) == Match(7));
        Assert.Equal(Match(7).GetHashCode(), Match(7).GetHashCode());
        Assert.NotEqual(Match(7), Match(9));
        Assert.Equal(Money(), Money());
        Assert.NotEqual(Money(), Money(isJacoby: false));
        Assert.NotEqual(Money(), Money(isBeaver: true));
        Assert.NotEqual(Money(), Money(cubeLimit: 64));
        Assert.False(Money().Equals(Match(7)));
        Assert.True(Money() != Match(7));
        Assert.False(Match(7).Equals((object?)null));
    }
}
