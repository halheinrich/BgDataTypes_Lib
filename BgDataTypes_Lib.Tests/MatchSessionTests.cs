using System.Text.Json;
using System.Text.Json.Nodes;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// A match's own rules (<see cref="MatchSession"/>, halheinrich/backgammon#273):
/// its terms' length of at least 1 (<see cref="MatchTerms"/>, composed as a
/// member, never restated beside it — the umbrella's review of 2026-09-26);
/// each away score at least 1 — a player 0-away has won, and 0-away each was
/// money's stand-in — and at most the length; and in the Crawford game
/// exactly one player 1-away. Code outside this library builds a match
/// through <see cref="Session.Create"/>, whose inputs refuse a breach
/// themselves (<see cref="SessionOrientationTests"/>); the session's own
/// guards hold the library's construction and every document, whichever
/// member completes the contradiction — in any member order (the guard's
/// exception, naming that member) and in any property order (a
/// <see cref="JsonException"/> on both paths, carrying it).
/// </summary>
public class MatchSessionTests
{
    /// <summary>A match built as the library builds one, member by member — to reach the session's own guards in a chosen order.</summary>
    private static MatchTerms Terms(int length) => new() { Length = length };

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

        Assert.Equal((length, onRollNeeds, opponentNeeds, isCrawford), (match.Terms.Length, match.OnRollNeeds, match.OpponentNeeds, match.IsCrawford));
        foreach (var (_, options) in WirePaths.Both)
            Assert.Equal(match, WirePaths.RoundTrip<Session>(match, options));
    }

    [Fact]
    public void TheTerms_AreComposed_NotRestated()
    {
        // Rewritten for the composed session: the length is the terms'
        // member, and the session states only its oriented standing beside it.
        Assert.Equal(typeof(MatchTerms), typeof(MatchSession).GetProperty(nameof(MatchSession.Terms))!.PropertyType);
        Assert.Null(typeof(MatchSession).GetProperty("Length"));

        var terms = Terms(9);
        var match = (MatchSession)Session.Create(terms, new MatchStanding { Away1 = 4, Away2 = 9, IsCrawford = false }, Seat.Player1);
        Assert.Same(terms, match.Terms);
    }

    // ── The length ────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]         // money's old stand-in length
    [InlineData(-1)]
    public void ALengthBelowOne_IsRefused_ByTheTerms(int length)
    {
        // Rewritten: the length is the terms', so the terms refuse it,
        // before any session exists.
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => TestRecords.MatchSession(length: length, onRollNeeds: 1, opponentNeeds: 1));

        Assert.Equal("Length", ex.ParamName);
        Assert.Contains(SessionRules.LengthMessage, ex.Message);
    }

    // ── The away scores ───────────────────────────────────────────

    [Theory]
    [InlineData(0)]         // money's old stand-in away score; a 0-away player has won
    [InlineData(-3)]
    public void AnAwayScoreBelowOne_IsRefused_EitherSide(int needs)
    {
        // Rewritten: from code, the standing refuses it, naming its seat's
        // member (the player on roll in seat 1); in a document, the session
        // refuses it, naming its own.
        var onRoll = Assert.Throws<ArgumentOutOfRangeException>(() => TestRecords.MatchSession(onRollNeeds: needs));
        var opponent = Assert.Throws<ArgumentOutOfRangeException>(() => TestRecords.MatchSession(opponentNeeds: needs));
        Assert.Equal("Away1", onRoll.ParamName);
        Assert.Equal("Away2", opponent.ParamName);
        Assert.Contains(SessionRules.NeedsMessage, onRoll.Message);

        foreach (var member in new[] { "OnRollNeeds", "OpponentNeeds" })
        {
            var document = WirePaths.Document<Session>(TestRecords.MatchSession());
            document[member] = needs;
            var refusal = Assert.IsType<ArgumentOutOfRangeException>(WirePaths.AssertRefused<Session>(document.ToJsonString()).InnerException);
            Assert.Equal(member, refusal.ParamName);
        }
    }

    [Theory]
    [InlineData(6, 5)]      // the player on roll's away score past the length
    [InlineData(5, 6)]      // the opponent's
    public void AnAwayScorePastTheLength_IsRefused_NamingWhicheverMemberCompletesIt(int onRollNeeds, int opponentNeeds)
    {
        // The terms set first: the away score completes the contradiction.
        var needsSecond = Assert.Throws<ArgumentOutOfRangeException>(() => new MatchSession
        {
            Terms = Terms(5), OnRollNeeds = onRollNeeds, OpponentNeeds = opponentNeeds, IsCrawford = false,
        });
        Assert.Equal(onRollNeeds > 5 ? "OnRollNeeds" : "OpponentNeeds", needsSecond.ParamName);

        // The away scores set first: the terms complete it, whichever side
        // is past the length (rewritten from a fact breaching the opponent's
        // side alone, which a mutation check showed left the other unpinned).
        var termsSecond = Assert.Throws<ArgumentOutOfRangeException>(() => new MatchSession
        {
            OnRollNeeds = onRollNeeds, OpponentNeeds = opponentNeeds, IsCrawford = false, Terms = Terms(5),
        });
        Assert.Equal("Terms", termsSecond.ParamName);
        Assert.Contains(SessionRules.NeedsMessage, termsSecond.Message);

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
            Terms = Terms(7), OnRollNeeds = onRollNeeds, OpponentNeeds = opponentNeeds, IsCrawford = true,
        });
        Assert.Equal("IsCrawford", flagLast.ParamName);
        Assert.Contains(SessionRules.CrawfordStandingMessage, flagLast.Message);

        var flagFirst = Assert.Throws<ArgumentException>(() => new MatchSession
        {
            IsCrawford = true, Terms = Terms(7), OnRollNeeds = onRollNeeds, OpponentNeeds = opponentNeeds,
        });
        Assert.Equal("OpponentNeeds", flagFirst.ParamName);

        var flagBetween = Assert.Throws<ArgumentException>(() => new MatchSession
        {
            Terms = Terms(7), OpponentNeeds = opponentNeeds, IsCrawford = true, OnRollNeeds = onRollNeeds,
        });
        Assert.Equal("OnRollNeeds", flagBetween.ParamName);
    }

    // ── From a document, in any property order ────────────────────

    public static TheoryData<string, string, int, Type> Breaches => new()
    {
        // (what, member — "Terms.Length" is the terms' — value, the guard's exception)
        { "length 0", "Terms.Length", 0, typeof(ArgumentOutOfRangeException) },
        { "away 0", "OnRollNeeds", 0, typeof(ArgumentOutOfRangeException) },
        { "away past the length", "OpponentNeeds", 8, typeof(ArgumentOutOfRangeException) },
        { "length below an away score", "Terms.Length", 6, typeof(ArgumentOutOfRangeException) },
    };

    [Theory]
    [MemberData(nameof(Breaches))]
    public void ABreachInADocument_IsRefused_InEitherPropertyOrder_BothPaths(string what, string member, int value, Type guard)
    {
        foreach (bool breachFirst in new[] { true, false })
        {
            var document = WirePaths.Document<Session>(TestRecords.MatchSession());
            string moved = member.StartsWith("Terms.", StringComparison.Ordinal) ? "Terms" : member;
            if (moved == "Terms")
                document["Terms"]!["Length"] = value;
            else
                document[member] = value;

            // First or last of the session's members.
            var node = document[moved]!.DeepClone();
            document.Remove(moved);
            if (breachFirst)
                document.Insert(0, moved, node);
            else
                document.Add(moved, node);

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
                document.Insert(0, "IsCrawford", true);
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
        // A match states its terms — their kind and length — both away scores
        // and its Crawford flag: none is read as a default (the wire rule on
        // BgDataTypesJsonContext).
        foreach (var member in new[] { "Terms", "Terms.Kind", "Terms.Length", "OnRollNeeds", "OpponentNeeds", "IsCrawford" })
        {
            var document = WirePaths.Document<Session>(TestRecords.MatchSession());
            var owner = member.StartsWith("Terms.", StringComparison.Ordinal) ? document["Terms"]!.AsObject() : document;
            owner.Remove(member.Split('.')[^1]);

            WirePaths.AssertRefused<Session>(document.ToJsonString());
            WirePaths.AssertRefused<MatchSession>(document.ToJsonString());
        }
    }
}
