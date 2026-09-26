using System.Reflection;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The one operation that builds a session from its header
/// (<see cref="Session.Create"/>; the umbrella's review of
/// halheinrich/backgammon#273, 2026-09-26): a session's terms, the game's
/// seat-anchored standing and the seat on roll become the record's
/// on-roll session. Pinned here: the orientation for each seat and each kind,
/// the terms carried as they are, the refusals — terms and a standing of
/// different kinds, an away score past the length, a null, an undefined
/// seat — each naming its parameter, and that code outside the library has
/// no other way to build a session.
/// </summary>
public class SessionOrientationTests
{
    private static readonly MoneyTerms Money = new() { IsJacoby = true, IsBeaver = false, CubeLimit = 64 };
    private static readonly MatchTerms Match7 = new() { Length = 7 };

    // ── The orientation ───────────────────────────────────────────

    [Fact]
    public void AMoneyStanding_IsTurnedToTheSeatOnRoll()
    {
        var standing = new MoneyStanding { Score1 = 3, Score2 = 11 };

        var player1 = Assert.IsType<MoneySession>(Session.Create(Money, standing, Seat.Player1));
        var player2 = Assert.IsType<MoneySession>(Session.Create(Money, standing, Seat.Player2));

        Assert.Equal((3, 11), (player1.OnRollScore, player1.OpponentScore));
        Assert.Equal((11, 3), (player2.OnRollScore, player2.OpponentScore));
        Assert.Same(Money, player1.Terms);
        Assert.Same(Money, player2.Terms);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AMatchStanding_IsTurnedToTheSeatOnRoll_TheCrawfordFlagTheGames(bool isCrawford)
    {
        var standing = new MatchStanding { Away1 = 1, Away2 = 4, IsCrawford = isCrawford };

        var player1 = Assert.IsType<MatchSession>(Session.Create(Match7, standing, Seat.Player1));
        var player2 = Assert.IsType<MatchSession>(Session.Create(Match7, standing, Seat.Player2));

        Assert.Equal((1, 4, isCrawford), (player1.OnRollNeeds, player1.OpponentNeeds, player1.IsCrawford));
        Assert.Equal((4, 1, isCrawford), (player2.OnRollNeeds, player2.OpponentNeeds, player2.IsCrawford));
        Assert.Same(Match7, player1.Terms);
    }

    [Theory]
    [InlineData(2, 5)]
    [InlineData(7, 7)]
    [InlineData(6, 1)]
    public void TheSameGame_SeenFromEitherSeat_IsOneSessionWhenTheStandingIsTurnedToo(int away1, int away2)
    {
        // The orientation is the seat on roll's first: player 2 on roll at
        // (a, b) is the session player 1 on roll at (b, a) is.
        Assert.Equal(
            Session.Create(Match7, new MatchStanding { Away1 = away1, Away2 = away2, IsCrawford = false }, Seat.Player2),
            Session.Create(Match7, new MatchStanding { Away1 = away2, Away2 = away1, IsCrawford = false }, Seat.Player1));
        Assert.Equal(
            Session.Create(Money, new MoneyStanding { Score1 = away1, Score2 = away2 }, Seat.Player2),
            Session.Create(Money, new MoneyStanding { Score1 = away2, Score2 = away1 }, Seat.Player1));
    }

    [Fact]
    public void ACreatedSession_RoundTrips_BothPaths()
    {
        foreach (var session in new[]
                 {
                     Session.Create(Money, new MoneyStanding { Score1 = 2, Score2 = 0 }, Seat.Player2),
                     Session.Create(Match7, new MatchStanding { Away1 = 6, Away2 = 1, IsCrawford = true }, Seat.Player2),
                 })
            foreach (var (_, options) in WirePaths.Both)
                Assert.Equal(session, WirePaths.RoundTrip(session, options));
    }

    // ── The refusals ──────────────────────────────────────────────

    [Fact]
    public void TermsAndAStandingOfDifferentKinds_AreRefused_NamingTheStanding()
    {
        foreach (var (terms, standing) in new (SessionTerms, GameStanding)[]
                 {
                     (Money, new MatchStanding { Away1 = 3, Away2 = 5, IsCrawford = false }),
                     (Match7, new MoneyStanding { Score1 = 0, Score2 = 0 }),
                 })
            foreach (var seat in Enum.GetValues<Seat>())
            {
                var ex = Assert.Throws<ArgumentException>(() => Session.Create(terms, standing, seat));
                Assert.Equal("standing", ex.ParamName);
                Assert.Contains(SessionRules.StandingKindMessage, ex.Message);
            }
    }

    [Theory]
    [InlineData(8, 3, Seat.Player1)]
    [InlineData(8, 3, Seat.Player2)]
    [InlineData(3, 8, Seat.Player1)]
    [InlineData(3, 8, Seat.Player2)]
    public void AnAwayScorePastTheTermsLength_IsRefused_NamingTheStanding(int away1, int away2, Seat onRoll)
    {
        // The standing knows no length; the terms do, so the operation holds
        // the one to the other, whichever seat is on roll.
        var standing = new MatchStanding { Away1 = away1, Away2 = away2, IsCrawford = false };

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Session.Create(Match7, standing, onRoll));
        Assert.Equal("standing", ex.ParamName);
        Assert.Contains(SessionRules.NeedsMessage, ex.Message);

        // At the length itself is the match's start, and builds.
        Assert.IsType<MatchSession>(Session.Create(Match7, new MatchStanding { Away1 = 7, Away2 = 7, IsCrawford = false }, onRoll));
    }

    [Fact]
    public void ANullOrAnUndefinedSeat_IsRefused_NamingItsParameter()
    {
        var standing = new MoneyStanding { Score1 = 0, Score2 = 0 };

        Assert.Equal("terms", Assert.Throws<ArgumentNullException>(() => Session.Create(null!, standing, Seat.Player1)).ParamName);
        Assert.Equal("standing", Assert.Throws<ArgumentNullException>(() => Session.Create(Money, null!, Seat.Player1)).ParamName);

        var undefined = Assert.Throws<ArgumentOutOfRangeException>(() => Session.Create(Money, standing, (Seat)2));
        Assert.Equal("onRoll", undefined.ParamName);
        Assert.Contains(SessionRules.SeatMessage, undefined.Message);
    }

    // ── The one way ───────────────────────────────────────────────

    [Theory]
    [InlineData(typeof(MoneySession))]
    [InlineData(typeof(MatchSession))]
    public void CodeOutsideTheLibrary_HasNoOtherWayToBuildASession(Type kind)
    {
        // No public constructor, and no member a caller can set: a producer
        // cannot orient the scores itself.
        Assert.Empty(kind.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        foreach (var property in kind.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            Assert.True(property.SetMethod is null || !(property.SetMethod.IsPublic || property.SetMethod.IsFamily || property.SetMethod.IsFamilyOrAssembly),
                $"{kind.Name}.{property.Name} can be set from outside the library");

        var create = typeof(Session).GetMethod(nameof(Session.Create))!;
        Assert.True(create.IsPublic && create.IsStatic);
        Assert.Equal([typeof(SessionTerms), typeof(GameStanding), typeof(Seat)], create.GetParameters().Select(p => p.ParameterType));
    }

    [Fact]
    public void ASeat_IsPlayer1OrPlayer2_OnTheWireByName()
    {
        Assert.Equal([Seat.Player1, Seat.Player2], Enum.GetValues<Seat>());
        foreach (var (_, options) in WirePaths.Both)
        {
            Assert.Equal("\"Player2\"", System.Text.Json.JsonSerializer.Serialize(Seat.Player2, options));
            Assert.Throws<System.Text.Json.JsonException>(() => System.Text.Json.JsonSerializer.Deserialize<Seat>("0", options));
        }
    }
}
