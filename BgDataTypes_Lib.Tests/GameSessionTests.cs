using System.Reflection;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// A game's session (<see cref="GameSession"/>; halheinrich/backgammon#273,
/// Hal's ruling of 2026-09-29): a header's terms and a game's standing, paired
/// by <see cref="GameSession.Create"/> with no seat. Pinned here: a money pair
/// and a match pair accepted, each fact read back as the header states it; the
/// refusals, each naming its parameter; that it and
/// <see cref="Session.Create"/> accept exactly the same pairs, at either seat,
/// and refuse the rest alike; that nothing it takes or hands back claims a
/// player on roll; the closed pair; and the value.
/// </summary>
public class GameSessionTests
{
    private static readonly MoneyTerms Money = new() { IsJacoby = true, IsBeaver = false, CubeLimit = 64 };
    private static readonly MatchTerms Match7 = new() { Length = 7 };

    private static MatchStanding Aways(int away1, int away2, bool isCrawford = false) =>
        new() { Away1 = away1, Away2 = away2, IsCrawford = isCrawford };

    private static MoneyStanding Scores(int score1, int score2) => new() { Score1 = score1, Score2 = score2 };

    // ── Accepted, and read back as the header states it ───────────

    [Fact]
    public void AMoneyPair_IsAccepted_TheTermsAndTheScoresAsTheHeaderStatesThem()
    {
        var standing = Scores(3, 11);

        var game = Assert.IsType<MoneyGameSession>(GameSession.Create(Money, standing));

        Assert.Equal(SessionKind.Money, game.Kind);
        Assert.Same(Money, game.Terms);
        Assert.Same(standing, game.Standing);
        Assert.Equal((3, 11), (game.Standing.Score1, game.Standing.Score2));
    }

    [Theory]
    [InlineData(1, 4, true)]
    [InlineData(4, 1, true)]
    [InlineData(1, 4, false)]
    [InlineData(6, 2, false)]
    [InlineData(7, 7, false)]
    public void AMatchPair_IsAccepted_TheLengthAwayScoresAndCrawfordFlagAsTheHeaderStatesThem(int away1, int away2, bool isCrawford)
    {
        // 7-away each is at the length itself, the match's start.
        var standing = Aways(away1, away2, isCrawford);

        var game = Assert.IsType<MatchGameSession>(GameSession.Create(Match7, standing));

        Assert.Equal(SessionKind.Match, game.Kind);
        Assert.Same(Match7, game.Terms);
        Assert.Same(standing, game.Standing);
        Assert.Equal(
            (7, away1, away2, isCrawford),
            (game.Terms.Length, game.Standing.Away1, game.Standing.Away2, game.Standing.IsCrawford));
    }

    [Fact]
    public void AHeadersTermsAndStanding_ReadThroughOneMatch_WithNoKindCheckOfTheCallersOwn()
    {
        // The caller's shape: the header's terms and the game's standing in,
        // typed as the contracts type them, and one branch per kind out —
        // none that cannot run.
        static string Read(SessionTerms terms, GameStanding standing) =>
            GameSession.Create(terms, standing).Match(
                money: _ => "money",
                match: match => $"{match.Terms.Length}: {match.Standing.Away1}/{match.Standing.Away2}{(match.Standing.IsCrawford ? " C" : "")}");

        Assert.Equal("money", Read(Money, Scores(0, 0)));
        Assert.Equal("7: 1/4 C", Read(Match7, Aways(1, 4, isCrawford: true)));
        Assert.Equal("7: 6/2", Read(Match7, Aways(6, 2)));
    }

    // ── The refusals ──────────────────────────────────────────────

    [Fact]
    public void TermsAndAStandingOfDifferentKinds_AreRefused_NamingTheStanding()
    {
        foreach (var (terms, standing) in new (SessionTerms, GameStanding)[]
                 {
                     (Money, Aways(3, 5)),
                     (Match7, Scores(0, 0)),
                 })
        {
            var ex = Assert.Throws<ArgumentException>(() => GameSession.Create(terms, standing));
            Assert.Equal("standing", ex.ParamName);
            Assert.Contains(SessionRules.StandingKindMessage, ex.Message);
        }
    }

    [Theory]
    [InlineData(8, 3)]
    [InlineData(3, 8)]
    [InlineData(8, 8)]
    public void AnAwayScorePastTheTermsLength_IsRefused_NamingTheStanding(int away1, int away2)
    {
        // The standing knows no length; the terms do, so the pairing holds the
        // one to the other, each seat's away score alike.
        var standing = Aways(away1, away2);

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => GameSession.Create(Match7, standing));
        Assert.Equal("standing", ex.ParamName);
        Assert.Same(standing, ex.ActualValue);
        Assert.Contains(SessionRules.NeedsMessage, ex.Message);
    }

    [Fact]
    public void ANull_IsRefused_NamingItsParameter()
    {
        Assert.Equal("terms", Assert.Throws<ArgumentNullException>(() => GameSession.Create(null!, Scores(0, 0))).ParamName);
        Assert.Equal("standing", Assert.Throws<ArgumentNullException>(() => GameSession.Create(Money, null!)).ParamName);
    }

    // ── One rule: the two operations agree ────────────────────────

    [Fact]
    public void ItAndSessionCreate_AcceptExactlyTheSamePairs_AtEitherSeat_AndRefuseTheRestAlike()
    {
        var seen = new HashSet<string>();
        foreach (var (terms, standing) in EveryPair())
        {
            var paired = OutcomeOf(() => GameSession.Create(terms, standing).Kind);
            seen.Add(paired.Refusal?.Name ?? "accepted");

            foreach (var seat in Enum.GetValues<Seat>())
            {
                var oriented = OutcomeOf(() => Session.Create(terms, standing, seat).Kind);
                Assert.True(paired == oriented,
                    $"{terms} with {standing} at {seat}: GameSession.Create {paired}, Session.Create {oriented}");
            }

            // The pair is looked at before the seat: an undefined seat is
            // refused only once the pair is accepted.
            var undefinedSeat = OutcomeOf(() => Session.Create(terms, standing, (Seat)2).Kind);
            if (paired.Refusal is null)
                Assert.Equal((typeof(ArgumentOutOfRangeException), "onRoll"), (undefinedSeat.Refusal, undefinedSeat.ParamName));
            else
                Assert.Equal(paired, undefinedSeat);
        }

        // The grid crosses each edge of the rule: pairs accepted, and pairs
        // refused for their kinds and for an away score past the length.
        Assert.Equal(["accepted", nameof(ArgumentException), nameof(ArgumentOutOfRangeException)], seen.Order());
    }

    /// <summary>
    /// Every pair of a grid wide enough to cross each edge of the rule: money
    /// terms and matches of 1, 3 and 7 points, each with every money standing
    /// of scores up to 2 and every match standing up to 8-away a side, in the
    /// Crawford game too wherever the standing can be.
    /// </summary>
    private static IEnumerable<(SessionTerms Terms, GameStanding Standing)> EveryPair()
    {
        SessionTerms[] terms = [Money, new MatchTerms { Length = 1 }, new MatchTerms { Length = 3 }, Match7];
        var standings = new List<GameStanding>();
        for (int score1 = 0; score1 <= 2; score1++)
            for (int score2 = 0; score2 <= 2; score2++)
                standings.Add(Scores(score1, score2));
        for (int away1 = 1; away1 <= 8; away1++)
            for (int away2 = 1; away2 <= 8; away2++)
            {
                standings.Add(Aways(away1, away2));
                if (SessionRules.CrawfordStandingHolds(away1, away2))
                    standings.Add(Aways(away1, away2, isCrawford: true));
            }

        return from t in terms from s in standings select (t, s);
    }

    /// <summary>What an operation did: the kind it built, or how it refused.</summary>
    private readonly record struct Outcome(SessionKind? Kind, Type? Refusal, string? ParamName, string? Message = null);

    private static Outcome OutcomeOf(Func<SessionKind> build)
    {
        try
        {
            return new Outcome(build(), null, null);
        }
        catch (ArgumentException ex)
        {
            return new Outcome(null, ex.GetType(), ex.ParamName, ex.Message);
        }
    }

    // ── No seat, and none claimed ─────────────────────────────────

    [Fact]
    public void NothingItTakesOrHandsBack_ClaimsAPlayerOnRoll()
    {
        var create = typeof(GameSession).GetMethod(nameof(GameSession.Create))!;
        Assert.True(create.IsPublic && create.IsStatic);
        Assert.Equal([typeof(SessionTerms), typeof(GameStanding)], create.GetParameters().Select(p => p.ParameterType));

        foreach (var type in new[] { typeof(GameSession), typeof(MoneyGameSession), typeof(MatchGameSession) })
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.NotEqual(typeof(Seat), property.PropertyType);
                Assert.DoesNotContain("OnRoll", property.Name, StringComparison.Ordinal);
                Assert.DoesNotContain("Opponent", property.Name, StringComparison.Ordinal);
            }
    }

    [Fact]
    public void EachKind_ComposesItsKindsTermsAndStanding_AndRestatesNoFact()
    {
        static string[] Own(Type type) => [.. type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name).Except(typeof(GameSession).GetProperties().Select(p => p.Name)).Order()];

        Assert.Equal(["Kind"], typeof(GameSession).GetProperties().Select(p => p.Name));
        Assert.Equal(["Standing", "Terms"], Own(typeof(MoneyGameSession)));
        Assert.Equal(["Standing", "Terms"], Own(typeof(MatchGameSession)));
        Assert.Equal(typeof(MoneyTerms), typeof(MoneyGameSession).GetProperty("Terms")!.PropertyType);
        Assert.Equal(typeof(MoneyStanding), typeof(MoneyGameSession).GetProperty("Standing")!.PropertyType);
        Assert.Equal(typeof(MatchTerms), typeof(MatchGameSession).GetProperty("Terms")!.PropertyType);
        Assert.Equal(typeof(MatchStanding), typeof(MatchGameSession).GetProperty("Standing")!.PropertyType);
    }

    [Fact]
    public void TheStanding_StaysSeatAnchored_SoTheSeatsAreNotInterchangeable()
    {
        Assert.NotEqual(GameSession.Create(Match7, Aways(3, 5)), GameSession.Create(Match7, Aways(5, 3)));
        Assert.NotEqual(GameSession.Create(Money, Scores(2, 0)), GameSession.Create(Money, Scores(0, 2)));
    }

    // ── A closed pair, built one way ──────────────────────────────

    [Fact]
    public void CodeOutsideTheLibrary_BuildsAGameSessionThroughCreateAlone()
    {
        Assert.True(typeof(GameSession).IsAbstract);
        foreach (var type in new[] { typeof(GameSession), typeof(MoneyGameSession), typeof(MatchGameSession) })
        {
            Assert.All(
                type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance),
                ctor => Assert.True(ctor.IsFamilyAndAssembly || ctor.IsAssembly || ctor.IsPrivate,
                    $"{type.Name}: {ctor} is reachable from outside the library"));
            Assert.All(type.GetProperties(BindingFlags.Public | BindingFlags.Instance), property => Assert.Null(property.SetMethod));
        }

        var kinds = typeof(GameSession).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(GameSession)))
            .OrderBy(t => t.Name)
            .ToArray();
        Assert.Equal([typeof(MatchGameSession), typeof(MoneyGameSession)], kinds);
        Assert.All(kinds, t => Assert.True(t.IsSealed));
    }

    [Fact]
    public void MatchAndSwitch_RunTheBranchOfTheKind_AndRefuseANullBranch()
    {
        foreach (var game in new[] { GameSession.Create(Money, Scores(0, 0)), GameSession.Create(Match7, Aways(2, 2)) })
        {
            string expected = game.Kind == SessionKind.Money ? "money" : "match";
            string switched = "";
            game.Switch(money: _ => switched = "money", match: _ => switched = "match");

            Assert.Equal(expected, game.Match(money: _ => "money", match: _ => "match"));
            Assert.Equal(expected, switched);
            Assert.Throws<ArgumentNullException>(() => game.Match<int>(null!, _ => 0));
            Assert.Throws<ArgumentNullException>(() => game.Match<int>(_ => 0, null!));
            Assert.Throws<ArgumentNullException>(() => game.Switch(null!, _ => { }));
            Assert.Throws<ArgumentNullException>(() => game.Switch(_ => { }, null!));
        }
    }

    [Fact]
    public void TheKind_IsTheTermsKind_AndNothingCanSetIt()
    {
        foreach (var game in new[] { GameSession.Create(Money, Scores(0, 0)), GameSession.Create(Match7, Aways(2, 2)) })
            Assert.Equal(game.Match(money => money.Terms.Kind, match => match.Terms.Kind), game.Kind);
        Assert.Null(typeof(GameSession).GetProperty(nameof(GameSession.Kind))!.SetMethod);
    }

    [Fact]
    public void ItIsNotAWireType_ItsTermsAndStandingAre()
    {
        // Its wire debut belongs to the first document that embeds it.
        Type[] kinds = [typeof(GameSession), typeof(MoneyGameSession), typeof(MatchGameSession)];
        var roots = typeof(BgDataTypesJsonContext).GetCustomAttributesData()
            .Where(a => a.AttributeType == typeof(JsonSerializableAttribute))
            .Select(a => (Type)a.ConstructorArguments[0].Value!)
            .ToArray();

        Assert.Contains(typeof(SessionTerms), roots);
        Assert.Contains(typeof(GameStanding), roots);
        foreach (var kind in kinds)
        {
            Assert.DoesNotContain(kind, roots);
            Assert.Null(kind.GetCustomAttribute<JsonConverterAttribute>());
            Assert.Null(BgDataTypesJsonContext.Default.GetTypeInfo(kind));
        }
    }

    // ── A value ───────────────────────────────────────────────────

    [Fact]
    public void Equality_IsTheKindTheTermsAndTheStanding()
    {
        // Equal instances, never the same ones: a value.
        GameSession match = GameSession.Create(new MatchTerms { Length = 9 }, Aways(3, 5));
        GameSession money = GameSession.Create(Money, Scores(2, 0));
        GameSession sameMatch = GameSession.Create(new MatchTerms { Length = 9 }, Aways(3, 5));
        GameSession sameMoney = GameSession.Create(new MoneyTerms { IsJacoby = true, IsBeaver = false, CubeLimit = 64 }, Scores(2, 0));

        Assert.Equal(match, sameMatch);
        Assert.True(match == sameMatch);
        Assert.Equal(match.GetHashCode(), sameMatch.GetHashCode());
        Assert.Equal(money, sameMoney);
        Assert.Equal(money.GetHashCode(), sameMoney.GetHashCode());

        // The terms and the standing each tell two apart.
        Assert.NotEqual(match, GameSession.Create(new MatchTerms { Length = 11 }, Aways(3, 5)));
        Assert.NotEqual(match, GameSession.Create(new MatchTerms { Length = 9 }, Aways(3, 4)));
        Assert.NotEqual(GameSession.Create(Match7, Aways(1, 5)), GameSession.Create(Match7, Aways(1, 5, isCrawford: true)));
        Assert.NotEqual(money, GameSession.Create(new MoneyTerms { IsJacoby = false, IsBeaver = false, CubeLimit = 64 }, Scores(2, 0)));
        Assert.NotEqual(money, GameSession.Create(Money, Scores(2, 1)));
        Assert.True(money != GameSession.Create(Money, Scores(2, 1)));

        // The kinds are never equal, and null equals only null.
        Assert.False(money.Equals(match));
        Assert.False(match.Equals((GameSession?)null));
        Assert.False(match.Equals((object?)null));
        Assert.True((GameSession?)null == null);
        Assert.False(match == null);
    }

    [Fact]
    public void ToString_StatesTheSession_Player1First()
    {
        Assert.Equal("money, Jacoby, no beaver, cube limit 64, 3-0", GameSession.Create(Money, Scores(3, 0)).ToString());
        Assert.Equal("match to 7, 1-away/4-away, Crawford", GameSession.Create(Match7, Aways(1, 4, isCrawford: true)).ToString());
        Assert.Equal("match to 7, 6-away/2-away", GameSession.Create(Match7, Aways(6, 2)).ToString());
    }
}
