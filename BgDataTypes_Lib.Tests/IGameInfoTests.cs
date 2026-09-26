using System.Reflection;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The game-scope contract (<see cref="IGameInfo"/>) and its standing
/// (<see cref="GameStanding"/>): money versus match is the standing's kind
/// (halheinrich/backgammon#273), never away scores of 0 beside a Crawford flag
/// that is always false.
/// </summary>
public class IGameInfoTests
{
    /// <summary>Minimal implementer pinning the contract's member shape.</summary>
    private sealed class FakeGameInfo : IGameInfo
    {
        public bool IsStandardStart { get; init; }
        public required GameStanding Standing { get; init; }
    }

    private static MatchStanding Match(int away1, int away2, bool isCrawford = false) =>
        new() { Away1 = away1, Away2 = away2, IsCrawford = isCrawford };

    private static MoneyStanding Money(int score1 = 0, int score2 = 0) => new() { Score1 = score1, Score2 = score2 };

    [Fact]
    public void IGameInfo_Members_SurfaceThroughContract()
    {
        // Rewritten: the away scores and the Crawford flag reach the contract
        // as the match's standing.
        IGameInfo info = new FakeGameInfo { IsStandardStart = true, Standing = Match(3, 5) };

        Assert.True(info.IsStandardStart);
        var match = Assert.IsType<MatchStanding>(info.Standing);
        Assert.Equal((3, 5, false), (match.Away1, match.Away2, match.IsCrawford));
    }

    [Fact]
    public void IGameInfo_CrawfordGame_SurfacesThroughContract()
    {
        IGameInfo info = new FakeGameInfo { IsStandardStart = true, Standing = Match(1, 4, isCrawford: true) };

        Assert.True(Assert.IsType<MatchStanding>(info.Standing).IsCrawford);
    }

    [Fact]
    public void IGameInfo_AMoneyGame_IsItsStandingsKind()
    {
        // Rewritten from IGameInfo_MoneySessionConvention: money was spelled
        // Away1 = Away2 = 0 with IsCrawfordGame false, and a filter read it off
        // the 0s. It is the standing's kind now, with the session's scores.
        IGameInfo info = new FakeGameInfo { IsStandardStart = true, Standing = Money(score1: 3, score2: 11) };

        var money = Assert.IsType<MoneyStanding>(info.Standing);
        Assert.Equal(SessionKind.Money, info.Standing.Kind);
        Assert.Equal((3, 11), (money.Score1, money.Score2));
    }

    [Fact]
    public void IGameInfo_HasNoAwayScoresOrCrawfordFlagOfItsOwn()
    {
        var members = typeof(IGameInfo).GetMembers().Select(m => m.Name).ToArray();

        Assert.DoesNotContain("Away1", members);
        Assert.DoesNotContain("Away2", members);
        Assert.DoesNotContain("IsCrawfordGame", members);
    }

    // ── The standing ──────────────────────────────────────────────

    [Theory]
    [InlineData(0, 5)]      // money's old stand-in away score
    [InlineData(5, 0)]
    [InlineData(-1, 5)]
    public void MatchStanding_AnAwayScoreBelowOne_IsRefused(int away1, int away2)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Match(away1, away2));
        Assert.Equal(away1 < 1 ? "Away1" : "Away2", ex.ParamName);
    }

    [Theory]
    [InlineData(3, 5)]      // neither player at match point
    [InlineData(1, 1)]      // both
    public void MatchStanding_ACrawfordGameWithoutExactlyOnePlayerAtMatchPoint_IsRefused_InAnyOrder(int away1, int away2)
    {
        var flagLast = Assert.Throws<ArgumentException>(() => new MatchStanding { Away1 = away1, Away2 = away2, IsCrawford = true });
        Assert.Equal("IsCrawford", flagLast.ParamName);

        var flagFirst = Assert.Throws<ArgumentException>(() => new MatchStanding { IsCrawford = true, Away1 = away1, Away2 = away2 });
        Assert.Equal("Away2", flagFirst.ParamName);

        Assert.True(Match(1, 4, isCrawford: true).IsCrawford);
        Assert.True(Match(6, 1, isCrawford: true).IsCrawford);
    }

    [Fact]
    public void MoneyStanding_ANegativeScore_IsRefused()
    {
        Assert.Equal("Score1", Assert.Throws<ArgumentOutOfRangeException>(() => Money(score1: -1)).ParamName);
        Assert.Equal("Score2", Assert.Throws<ArgumentOutOfRangeException>(() => Money(score2: -1)).ParamName);
    }

    [Fact]
    public void TheKinds_ShareOnlyTheBasesMembers_AndAreAClosedPair()
    {
        static string[] Own(Type type) => [.. type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name).Except(typeof(GameStanding).GetProperties().Select(p => p.Name)).Order()];

        Assert.Equal(["Score1", "Score2"], Own(typeof(MoneyStanding)));
        Assert.Equal(["Away1", "Away2", "IsCrawford"], Own(typeof(MatchStanding)));
        Assert.True(typeof(GameStanding).IsAbstract);
        Assert.All(typeof(GameStanding).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance),
            ctor => Assert.True(ctor.IsFamilyAndAssembly || ctor.IsAssembly || ctor.IsPrivate));
        Assert.Equal([typeof(MatchStanding), typeof(MoneyStanding)],
            typeof(GameStanding).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(GameStanding))).OrderBy(t => t.Name));
        Assert.True(typeof(MoneyStanding).IsSealed && typeof(MatchStanding).IsSealed);
    }

    [Fact]
    public void MatchAndSwitch_RunTheBranchOfTheKind_AndRefuseANullBranch()
    {
        foreach (GameStanding standing in new GameStanding[] { Money(), Match(2, 2) })
        {
            string expected = standing.Kind == SessionKind.Money ? "money" : "match";
            string switched = "";
            standing.Switch(money: _ => switched = "money", match: _ => switched = "match");

            Assert.Equal(expected, standing.Match(money: _ => "money", match: _ => "match"));
            Assert.Equal(expected, switched);
            Assert.Throws<ArgumentNullException>(() => standing.Match<int>(null!, _ => 0));
            Assert.Throws<ArgumentNullException>(() => standing.Match<int>(_ => 0, null!));
            Assert.Throws<ArgumentNullException>(() => standing.Switch(null!, _ => { }));
            Assert.Throws<ArgumentNullException>(() => standing.Switch(_ => { }, null!));
        }
    }

    [Fact]
    public void Standings_AreValues()
    {
        Assert.Equal(Match(3, 5), Match(3, 5));
        Assert.True(Match(3, 5) == Match(3, 5));
        Assert.Equal(Match(3, 5).GetHashCode(), Match(3, 5).GetHashCode());
        Assert.NotEqual(Match(3, 5), Match(5, 3));
        Assert.NotEqual(Match(1, 5), Match(1, 5, isCrawford: true));
        Assert.Equal(Money(2, 4), Money(2, 4));
        Assert.NotEqual(Money(2, 4), Money(4, 2));
        Assert.False(Money().Equals(Match(1, 1)));
        Assert.True(Money() != Match(1, 1));
        Assert.False(Money().Equals((object?)null));
    }
}
