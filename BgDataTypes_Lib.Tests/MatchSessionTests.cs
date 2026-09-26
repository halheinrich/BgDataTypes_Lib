using System.Text.Json;
using System.Text.Json.Nodes;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// A match's own rules (<see cref="MatchSession"/>, halheinrich/backgammon#273):
/// a length of at least 1; each away score at least 1 — a player 0-away has
/// won, and 0-away each was money's stand-in — and at most the length; and in
/// the Crawford game exactly one player 1-away. Each is refused whichever
/// member completes the contradiction, from an object initializer in any
/// order (the guard's exception, naming that member) and from a document in
/// any property order (a <see cref="JsonException"/> on both paths, carrying
/// it).
/// </summary>
public class MatchSessionTests
{
    // ── What a match may be ───────────────────────────────────────

    public static TheoryData<int, int, int, bool> Standings => new()
    {
        { 1, 1, 1, false },     // a 1-point match: both at match point, never Crawford
        { 7, 7, 7, false },     // the start of a 7-point match
        { 7, 1, 6, true },      // the Crawford game, the player on roll at match point
        { 7, 6, 1, true },      // the Crawford game, the opponent at match point
        { 7, 1, 1, false },     // double match point, after the Crawford game
        { 7, 1, 4, false },     // post-Crawford
        { 25, 25, 1, false },   // a long match
    };

    [Theory]
    [MemberData(nameof(Standings))]
    public void AStandingAMatchCanReach_Builds_AndRoundTrips_BothPaths(int length, int onRollNeeds, int opponentNeeds, bool isCrawford)
    {
        var match = TestRecords.MatchSession(length, onRollNeeds, opponentNeeds, isCrawford);

        Assert.Equal((length, onRollNeeds, opponentNeeds, isCrawford), (match.Length, match.OnRollNeeds, match.OpponentNeeds, match.IsCrawford));
        foreach (var (_, options) in WirePaths.Both)
            Assert.Equal(match, WirePaths.RoundTrip<Session>(match, options));
    }

    // ── The length ────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]         // money's old stand-in length
    [InlineData(-1)]
    public void ALengthBelowOne_IsRefused(int length)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new MatchSession
        {
            Length = length, OnRollNeeds = 1, OpponentNeeds = 1, IsCrawford = false,
        });

        Assert.Equal("Length", ex.ParamName);
        Assert.Contains(SessionRules.LengthMessage, ex.Message);
    }

    // ── The away scores ───────────────────────────────────────────

    [Theory]
    [InlineData(0)]         // money's old stand-in away score; a 0-away player has won
    [InlineData(-3)]
    public void AnAwayScoreBelowOne_IsRefused_EitherSide(int needs)
    {
        var onRoll = Assert.Throws<ArgumentOutOfRangeException>(() => TestRecords.MatchSession(onRollNeeds: needs));
        var opponent = Assert.Throws<ArgumentOutOfRangeException>(() => TestRecords.MatchSession(opponentNeeds: needs));

        Assert.Equal("OnRollNeeds", onRoll.ParamName);
        Assert.Equal("OpponentNeeds", opponent.ParamName);
        Assert.Contains(SessionRules.NeedsMessage, onRoll.Message);
    }

    [Fact]
    public void AnAwayScorePastTheLength_IsRefused_NamingWhicheverMemberCompletesIt()
    {
        // The length set first: the away score completes the contradiction.
        var needsSecond = Assert.Throws<ArgumentOutOfRangeException>(() => new MatchSession
        {
            Length = 5, OnRollNeeds = 6, OpponentNeeds = 5, IsCrawford = false,
        });
        Assert.Equal("OnRollNeeds", needsSecond.ParamName);

        // The away scores set first: the length completes it.
        var lengthSecond = Assert.Throws<ArgumentOutOfRangeException>(() => new MatchSession
        {
            OnRollNeeds = 5, OpponentNeeds = 6, IsCrawford = false, Length = 5,
        });
        Assert.Equal("Length", lengthSecond.ParamName);
        Assert.Contains(SessionRules.NeedsMessage, lengthSecond.Message);

        // At the length itself is the match's start, and builds.
        Assert.Equal(5, TestRecords.MatchSession(length: 5, onRollNeeds: 5, opponentNeeds: 5).OnRollNeeds);
    }

    // ── The Crawford game ─────────────────────────────────────────

    [Theory]
    [InlineData(7, 7)]      // neither player at match point
    [InlineData(3, 2)]
    [InlineData(1, 1)]      // both: the Crawford game was over before, or never was
    public void ACrawfordGameWithoutExactlyOnePlayerAtMatchPoint_IsRefused_InAnyMemberOrder(int onRollNeeds, int opponentNeeds)
    {
        var flagLast = Assert.Throws<ArgumentException>(() => new MatchSession
        {
            Length = 7, OnRollNeeds = onRollNeeds, OpponentNeeds = opponentNeeds, IsCrawford = true,
        });
        Assert.Equal("IsCrawford", flagLast.ParamName);
        Assert.Contains(SessionRules.CrawfordStandingMessage, flagLast.Message);

        var flagFirst = Assert.Throws<ArgumentException>(() => new MatchSession
        {
            IsCrawford = true, Length = 7, OnRollNeeds = onRollNeeds, OpponentNeeds = opponentNeeds,
        });
        Assert.Equal("OpponentNeeds", flagFirst.ParamName);

        var flagBetween = Assert.Throws<ArgumentException>(() => new MatchSession
        {
            Length = 7, OpponentNeeds = opponentNeeds, IsCrawford = true, OnRollNeeds = onRollNeeds,
        });
        Assert.Equal("OnRollNeeds", flagBetween.ParamName);
    }

    // ── From a document, in any property order ────────────────────

    public static TheoryData<string, string, int, Type> Breaches => new()
    {
        // (what, member, value, the guard's exception)
        { "length 0", "Length", 0, typeof(ArgumentOutOfRangeException) },
        { "away 0", "OnRollNeeds", 0, typeof(ArgumentOutOfRangeException) },
        { "away past the length", "OpponentNeeds", 8, typeof(ArgumentOutOfRangeException) },
        { "length below an away score", "Length", 6, typeof(ArgumentOutOfRangeException) },
    };

    [Theory]
    [MemberData(nameof(Breaches))]
    public void ABreachInADocument_IsRefused_InEitherPropertyOrder_BothPaths(string what, string member, int value, Type guard)
    {
        foreach (bool breachFirst in new[] { true, false })
        {
            var document = WirePaths.Document<Session>(TestRecords.MatchSession());
            document.Remove(member);
            if (breachFirst)
                document.Insert(1, member, value);
            else
                document.Add(member, value);

            var ex = WirePaths.AssertRefused<Session>(document.ToJsonString());
            Assert.True(guard.IsInstanceOfType(ex.InnerException), $"{what}, breach first: {breachFirst}: {ex.InnerException?.GetType().Name}");
        }
    }

    [Fact]
    public void ACrawfordGameAtNoMatchPoint_InADocument_IsRefused_InEitherPropertyOrder_BothPaths()
    {
        foreach (bool flagFirst in new[] { true, false })
        {
            var document = WirePaths.Document<Session>(TestRecords.MatchSession());
            document.Remove("IsCrawford");
            if (flagFirst)
                document.Insert(1, "IsCrawford", true);
            else
                document.Add("IsCrawford", true);

            var ex = WirePaths.AssertRefused<Session>(document.ToJsonString());
            Assert.IsType<ArgumentException>(ex.InnerException);
            Assert.Contains("exactly one player is 1-away", ex.Message);
        }
    }

    [Fact]
    public void EveryMemberOfAMatch_IsRequired_OnBothPaths()
    {
        // A match states its length, both away scores and its Crawford flag:
        // none is read as a default (the wire rule on BgDataTypesJsonContext).
        foreach (var member in new[] { "Length", "OnRollNeeds", "OpponentNeeds", "IsCrawford" })
        {
            var document = WirePaths.Document<Session>(TestRecords.MatchSession());
            document.Remove(member);

            WirePaths.AssertRefused<Session>(document.ToJsonString());
        }
    }
}
