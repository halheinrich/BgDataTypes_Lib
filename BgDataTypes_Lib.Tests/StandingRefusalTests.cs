using System.Globalization;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// A game's standing and its session, asked without throwing
/// (halheinrich/backgammon#514, Hal's ruling B of 2026-11-03):
/// <see cref="MatchStanding.TryCreate"/> and
/// <see cref="GameSession.TryCreate"/> hand back the refusal of the rule the
/// values break — the rule's sentence and the values it refuses, on one line —
/// and the throwing doors' messages begin with that same refusal, their
/// exception's type, parameter and actual value as before. Pinned here: each
/// refusal and the values it names; the order the standing's rules are asked
/// in; the throwing doors' agreement; acceptance; and the null arguments.
/// </summary>
public class StandingRefusalTests
{
    private static readonly MoneyTerms Money = new() { IsJacoby = true, IsBeaver = false, CubeLimit = 64 };
    private static readonly MatchTerms Match7 = new() { Length = 7 };

    private static MatchStanding Aways(int away1, int away2, bool isCrawford = false) =>
        new() { Away1 = away1, Away2 = away2, IsCrawford = isCrawford };

    private static MoneyStanding Scores(int score1, int score2) => new() { Score1 = score1, Score2 = score2 };

    /// <summary>The standing's refusal for these values, which must be one.</summary>
    private static string StandingRefusal(int away1, int away2, bool isCrawford)
    {
        Assert.False(MatchStanding.TryCreate(away1, away2, isCrawford, out var standing, out var refusal));
        Assert.Null(standing);
        return refusal;
    }

    /// <summary>The pairing's refusal for these values, which must be one.</summary>
    private static string SessionRefusal(SessionTerms terms, GameStanding standing)
    {
        Assert.False(GameSession.TryCreate(terms, standing, out var session, out var refusal));
        Assert.Null(session);
        return refusal;
    }

    // ── The away-score floor ──────────────────────────────────────

    [Theory]
    [InlineData(0, 5, false, 1, 0)]
    [InlineData(-1, 5, false, 1, -1)]   // Hardigree, game 7
    [InlineData(5, 0, false, 2, 0)]     // Katsios
    [InlineData(5, -3, true, 2, -3)]
    public void AnAwayScoreBelowTheFloor_IsRefused_NamingThePlayerAndTheScore(int away1, int away2, bool isCrawford, int player, int away)
    {
        string refusal = StandingRefusal(away1, away2, isCrawford);

        Assert.Equal(
            string.Create(CultureInfo.InvariantCulture, $"{SessionRules.NeedsMessage} Player {player}'s away score is {away}."),
            refusal);
    }

    [Theory]
    [InlineData(0, 5, "Away1", 0)]
    [InlineData(-1, 5, "Away1", -1)]
    [InlineData(5, 0, "Away2", 0)]
    [InlineData(5, -3, "Away2", -3)]
    public void AnAwayScoreBelowTheFloor_SetInTheConvertersOrder_ThrowsTheRefusal_AsItsTypeParameterAndValueWere(
        int away1, int away2, string member, int away)
    {
        string refusal = StandingRefusal(away1, away2, isCrawford: false);

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new MatchStanding { Away1 = away1, Away2 = away2, IsCrawford = false });

        Assert.StartsWith(refusal, ex.Message, StringComparison.Ordinal);
        Assert.Equal(member, ex.ParamName);
        Assert.Equal(away, Assert.IsType<int>(ex.ActualValue));
    }

    [Fact]
    public void AnAwayScoresRefusal_ReadsTheSameInAnyCulture()
    {
        // A culture's own minus sign would make the value read differently
        // on another machine; the refusal is written invariantly.
        var saved = CultureInfo.CurrentCulture;
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NegativeSign = "~";
        try
        {
            CultureInfo.CurrentCulture = culture;
            Assert.EndsWith("Player 1's away score is -1.", StandingRefusal(-1, 5, isCrawford: false), StringComparison.Ordinal);
            Assert.Equal("-1-away/5-away", SessionRules.FormatAways(-1, 5));
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }

    // ── The Crawford standing ─────────────────────────────────────

    [Theory]
    [InlineData(3, 5)]      // neither player at match point
    [InlineData(5, 3)]
    [InlineData(1, 1)]      // both (Norberg, game 9: a Crawford game at 1-away/1-away)
    public void ACrawfordGameWithoutExactlyOnePlayerAtMatchPoint_IsRefused_NamingBothAwayScores_PlayerOnesFirst(int away1, int away2)
    {
        string refusal = StandingRefusal(away1, away2, isCrawford: true);

        Assert.Equal($"{SessionRules.CrawfordStandingMessage} The standing is {away1}-away/{away2}-away.", refusal);
    }

    [Theory]
    [InlineData(3, 5)]
    [InlineData(5, 3)]
    [InlineData(1, 1)]
    public void ACrawfordRefusal_IsTheSame_WhicheverMemberCompletesIt(int away1, int away2)
    {
        // Set in the converter's order, the flag completes it; set first, the
        // away score set last does. Each names both scores, player 1's first.
        string refusal = StandingRefusal(away1, away2, isCrawford: true);

        foreach (var (member, build) in new (string, Func<MatchStanding>)[]
                 {
                     ("IsCrawford", () => new MatchStanding { Away1 = away1, Away2 = away2, IsCrawford = true }),
                     ("Away2", () => new MatchStanding { IsCrawford = true, Away1 = away1, Away2 = away2 }),
                     ("Away1", () => new MatchStanding { IsCrawford = true, Away2 = away2, Away1 = away1 }),
                 })
        {
            var ex = Assert.Throws<ArgumentException>(build);
            Assert.StartsWith(refusal, ex.Message, StringComparison.Ordinal);
            Assert.Equal(member, ex.ParamName);
        }
    }

    // ── The order the standing's rules are asked in ───────────────

    [Theory]
    [InlineData(0, 0, true, "Player 1's away score is 0.")]     // both below the floor: player 1's first
    [InlineData(5, 0, true, "Player 2's away score is 0.")]     // a floor before the Crawford standing
    [InlineData(0, 3, true, "Player 1's away score is 0.")]
    public void TheStandingsRules_AreAskedInTheConvertersOrder_Away1_Away2_ThenTheCrawfordStanding(
        int away1, int away2, bool isCrawford, string named)
    {
        string refusal = StandingRefusal(away1, away2, isCrawford);
        Assert.EndsWith(named, refusal, StringComparison.Ordinal);

        var ex = Assert.ThrowsAny<ArgumentException>(() => new MatchStanding { Away1 = away1, Away2 = away2, IsCrawford = isCrawford });
        Assert.StartsWith(refusal, ex.Message, StringComparison.Ordinal);
    }

    // ── The length bound ──────────────────────────────────────────

    [Theory]
    [InlineData(17, 19, 19)]    // MTCH2987, game 1
    [InlineData(17, 19, 15)]    // MTCH2987, game 3
    [InlineData(17, 15, 19)]
    [InlineData(1, 2, 1)]       // a one-point match, 2-away
    public void AnAwayScorePastTheLength_IsRefused_NamingTheStandingAndTheLength(int length, int away1, int away2)
    {
        var terms = new MatchTerms { Length = length };
        var standing = Aways(away1, away2);

        string refusal = SessionRefusal(terms, standing);

        Assert.Equal(
            $"{SessionRules.NeedsMessage} The standing is {away1}-away/{away2}-away. The match length is {length}.",
            refusal);

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => GameSession.Create(terms, standing));
        Assert.StartsWith(refusal, ex.Message, StringComparison.Ordinal);
        Assert.Equal("standing", ex.ParamName);
        Assert.Same(standing, ex.ActualValue);
    }

    // ── The kinds ─────────────────────────────────────────────────

    [Fact]
    public void TermsAndAStandingOfDifferentKinds_AreRefused_NamingBothKinds()
    {
        foreach (var (terms, standing, kinds) in new (SessionTerms, GameStanding, string)[]
                 {
                     (Money, Aways(3, 5), "The terms are Money; the standing is Match."),
                     (Match7, Scores(0, 0), "The terms are Match; the standing is Money."),
                 })
        {
            string refusal = SessionRefusal(terms, standing);
            Assert.Equal($"{SessionRules.StandingKindMessage} {kinds}", refusal);

            var ex = Assert.Throws<ArgumentException>(() => GameSession.Create(terms, standing));
            Assert.StartsWith(refusal, ex.Message, StringComparison.Ordinal);
            Assert.Equal("standing", ex.ParamName);
        }
    }

    // ── One line ──────────────────────────────────────────────────

    [Fact]
    public void NoRefusal_HoldsALineBreak()
    {
        string[] refusals =
        [
            StandingRefusal(0, 5, isCrawford: false),
            StandingRefusal(5, -1, isCrawford: false),
            StandingRefusal(3, 5, isCrawford: true),
            SessionRefusal(new MatchTerms { Length = 17 }, Aways(19, 19)),
            SessionRefusal(Money, Aways(3, 5)),
            SessionRefusal(Match7, Scores(0, 0)),
        ];

        Assert.All(refusals, refusal => Assert.DoesNotContain(refusal, c => c is '\n' or '\r'));
    }

    // ── Accepted ──────────────────────────────────────────────────

    [Theory]
    [InlineData(1, 4, true)]
    [InlineData(6, 1, true)]
    [InlineData(1, 1, false)]
    [InlineData(7, 7, false)]
    public void AValidStandingAndSession_AreAccepted_AsTheThrowingDoorsBuildThem(int away1, int away2, bool isCrawford)
    {
        Assert.True(MatchStanding.TryCreate(away1, away2, isCrawford, out var standing, out var standingRefusal));
        Assert.Null(standingRefusal);
        Assert.Equal(Aways(away1, away2, isCrawford), standing);

        Assert.True(GameSession.TryCreate(Match7, standing, out var session, out var sessionRefusal));
        Assert.Null(sessionRefusal);
        Assert.Equal(GameSession.Create(Match7, standing), session);
    }

    [Fact]
    public void AMoneyPair_IsAccepted_AsCreateBuildsIt()
    {
        var standing = Scores(-1, 3);

        Assert.True(GameSession.TryCreate(Money, standing, out var session, out var refusal));
        Assert.Null(refusal);
        Assert.Equal(GameSession.Create(Money, standing), Assert.IsType<MoneyGameSession>(session));
    }

    // ── A null is the caller's fault, not a refusal ───────────────

    [Fact]
    public void ANull_Throws_NamingItsParameter()
    {
        Assert.Equal("terms", Assert.Throws<ArgumentNullException>(() => GameSession.TryCreate(null!, Scores(0, 0), out _, out _)).ParamName);
        Assert.Equal("standing", Assert.Throws<ArgumentNullException>(() => GameSession.TryCreate(Money, null!, out _, out _)).ParamName);
    }
}
