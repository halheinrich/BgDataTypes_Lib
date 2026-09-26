using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Money versus match is one of two types (halheinrich/backgammon#273, Hal's
/// ruling of 2026-09-26): <see cref="MoneySession"/> and
/// <see cref="MatchSession"/>, each composing its kind's terms and stating
/// only the oriented standing beside them (the umbrella's review of
/// 2026-09-26). Pinned here, as <see cref="DecisionKindTests"/> pins the
/// decision kinds: the kind on the wire — stated once, in the terms, read
/// wherever the terms and the kind sit, refused as a
/// <see cref="JsonException"/> on both paths when missing, duplicated,
/// unknown or contradicted, read as a session, as its kind, or inside a
/// record — the separation of the two kinds' facts through the public
/// surface, the closed pair that makes <see cref="Session.Match{TResult}"/>
/// exhaustive, and the session as a value.
/// </summary>
public class SessionKindTests
{
    private static Session[] BothKinds() =>
    [
        TestRecords.MoneySession(isJacoby: false),
        TestRecords.MatchSession(length: 9, onRollNeeds: 1, opponentNeeds: 4, isCrawford: true),
    ];

    /// <summary>A record of each decision kind in <paramref name="session"/>; a cube never in the Crawford game.</summary>
    private static BgDecisionData[] RecordsIn(Session session) => session is MatchSession { IsCrawford: true }
        ? [TestRecords.CheckerPlay(position: TestRecords.Position(session: session))]
        : [TestRecords.CheckerPlay(position: TestRecords.Position(session: session)),
           TestRecords.Cube(position: TestRecords.Position(session: session))];

    private static JsonObject SessionDocument(Session session) => WirePaths.Document(session);

    /// <summary>
    /// <paramref name="session"/>'s document edited by <paramref name="edit"/>,
    /// standing on its own and inside a record's position.
    /// </summary>
    private static (string Alone, string InRecord) Edited(Session session, Action<JsonObject> edit)
    {
        var alone = SessionDocument(session);
        edit(alone);
        var record = WirePaths.Document<BgDecisionData>(TestRecords.CheckerPlay(position: TestRecords.Position(session: session)));
        var inRecord = record["Position"]!["Session"]!.AsObject();
        edit(inRecord);
        return (alone.ToJsonString(), record.ToJsonString());
    }

    /// <summary>The terms object inside a session document, to edit.</summary>
    private static JsonObject TermsOf(JsonObject session) => session["Terms"]!.AsObject();

    private static void AssertRefusedAloneAndInARecord(Session session, Action<JsonObject> edit, string? saying = null)
    {
        var (alone, inRecord) = Edited(session, edit);
        foreach (var (_, options) in WirePaths.Both)
        {
            var asSession = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Session>(alone, options));
            var asRecord = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<BgDecisionData>(inRecord, options));
            if (saying is not null)
            {
                Assert.Contains(saying, asSession.Message);
                Assert.Contains(saying, asRecord.Message);
            }
        }
    }

    // ── Round trips ───────────────────────────────────────────────

    [Fact]
    public void EachKind_RoundTripsAsItsKind_BothPaths()
    {
        foreach (var session in BothKinds())
            foreach (var (_, options) in WirePaths.Both)
            {
                var json = JsonSerializer.Serialize(session, options);
                var restored = JsonSerializer.Deserialize<Session>(json, options)!;

                Assert.Equal(session.GetType(), restored.GetType());
                Assert.Equal(session, restored);
                Assert.Equal(json, JsonSerializer.Serialize(restored, options));
            }
    }

    [Fact]
    public void EachKind_InsideARecord_RoundTripsAsItsKind_BothPaths()
    {
        foreach (var session in BothKinds())
            foreach (var record in RecordsIn(session))
                foreach (var (_, options) in WirePaths.Both)
                {
                    var restored = WirePaths.RoundTrip(record, options);

                    Assert.Equal(session.GetType(), restored.Session.GetType());
                    Assert.Equal(session, restored.Session);
                }
    }

    [Fact]
    public void EachKind_InAConsumersDocument_RoundTripsAsItsKind()
    {
        // A consumer's document embedding records reaches the session kinds
        // through the chained resolvers, as it does the decision kinds.
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = JsonTypeInfoResolver.Combine(ConsumerContext.Default, BgDataTypesJsonContext.Default),
        };
        var document = new ConsumerDocument { Decisions = [.. BothKinds().SelectMany(RecordsIn)] };

        var restored = JsonSerializer.Deserialize<ConsumerDocument>(JsonSerializer.Serialize(document, options), options)!;

        Assert.Equal(document.Decisions.Select(d => d.Session), restored.Decisions.Select(d => d.Session));
    }

    // ── The kind is stated once, in the terms ─────────────────────

    [Fact]
    public void TheKind_IsStatedOnce_InTheTermsWrittenFirst_WhateverTheStaticType_BothPaths()
    {
        // Rewritten: the session's kind is its terms', so the document states
        // it there — first in the terms, which are first in the session — and
        // never again beside them.
        foreach (var session in BothKinds())
            foreach (var (_, options) in WirePaths.Both)
            {
                string expected = $"{{\"Terms\":{{\"Kind\":\"{session.Kind}\",";
                Assert.StartsWith(expected, JsonSerializer.Serialize(session, options));
                Assert.StartsWith(expected, JsonSerializer.Serialize(session, session.GetType(), options));
                Assert.Contains("\"Session\":" + expected,
                    JsonSerializer.Serialize<BgDecisionData>(RecordsIn(session)[0], options));
                Assert.Null(SessionDocument(session)["Kind"]);
            }
    }

    [Fact]
    public void TheTermsAndTheirKind_AreReadWhereverTheySit_BothPaths()
    {
        // JSON objects are unordered: terms stated last, with their kind
        // stated last in them, read as terms and a kind stated first.
        foreach (var session in BothKinds())
        {
            var (alone, inRecord) = Edited(session, document =>
            {
                var terms = TermsOf(document).DeepClone().AsObject();
                var kind = terms["Kind"]!.DeepClone();
                terms.Remove("Kind");
                terms.Add("Kind", kind);
                document.Remove("Terms");
                document.Add("Terms", terms);
            });

            foreach (var (_, options) in WirePaths.Both)
            {
                Assert.Equal(session, JsonSerializer.Deserialize<Session>(alone, options));
                Assert.Equal(session, JsonSerializer.Deserialize<BgDecisionData>(inRecord, options)!.Session);
            }
        }
    }

    public static TheoryData<string, string> BadKinds => new()
    {
        { "missing", "" },
        { "unknown", "\"Chouette\"" },
        { "numeric", "1" },
        { "null", "null" },
        { "an object", "{}" },
        { "differently cased name", "\"match\"" },
    };

    [Theory]
    [MemberData(nameof(BadKinds))]
    public void ABadKind_IsRefused_BothPaths_AloneAndInsideARecord(string name, string kindJson)
    {
        var session = TestRecords.MatchSession();
        Action<JsonObject> edit = document =>
        {
            var terms = TermsOf(document);
            terms.Remove("Kind");
            if (kindJson.Length > 0)
                terms.Insert(0, "Kind", JsonNode.Parse(kindJson));
        };

        if (name == "differently cased name")
        {
            // The strict enum token matches names case-insensitively, as the
            // decision kind's does: the strictness closed is the token's kind.
            var (alone, inRecord) = Edited(session, edit);
            foreach (var (_, options) in WirePaths.Both)
            {
                Assert.IsType<MatchSession>(JsonSerializer.Deserialize<Session>(alone, options));
                Assert.IsType<MatchSession>(JsonSerializer.Deserialize<BgDecisionData>(inRecord, options)!.Session);
            }
            return;
        }

        // A missing kind is refused by the dispatch, which says where a
        // session states it — not by whichever kind's contract a guessed kind
        // would reach, whose complaint would not name the kind.
        AssertRefusedAloneAndInARecord(session, edit,
            name == "missing" ? "A session states its Terms.Kind — Money or Match." : null);
    }

    [Theory]
    [InlineData(null)]          // missing
    [InlineData("null")]
    [InlineData("\"Money\"")]   // a string
    [InlineData("[]")]          // an array
    public void MissingOrMalformedTerms_AreRefused_BothPaths_AloneAndInsideARecord(string? termsJson)
    {
        // Added: the terms carry the session's kind, so a session without an
        // object of terms states no kind — the dispatch refuses it, saying so.
        AssertRefusedAloneAndInARecord(TestRecords.MoneySession(), document =>
            {
                document.Remove("Terms");
                if (termsJson is not null)
                    document.Insert(0, "Terms", JsonNode.Parse(termsJson));
            },
            termsJson is null ? "A session states its Terms.Kind" : "A session's Terms is a JSON object");
    }

    [Fact]
    public void TermsOrTheirKind_StatedTwice_AreRefused_BothPaths()
    {
        foreach (var second in new[] { "Match", "Money" })
        {
            var document = SessionDocument(TestRecords.MatchSession());
            var termsJson = document["Terms"]!.ToJsonString();

            string kindTwice = document.ToJsonString().Replace(termsJson, termsJson[..^1] + $",\"Kind\":\"{second}\"}}");
            Assert.Contains("states its Terms.Kind once", WirePaths.AssertRefused<Session>(kindTwice).Message);

            string termsTwice = document.ToJsonString()[..^1] + $",\"Terms\":{{\"Kind\":\"{second}\",\"Length\":7}}}}";
            Assert.Contains("states its Terms once", WirePaths.AssertRefused<Session>(termsTwice).Message);
        }
    }

    [Fact]
    public void AKindContradictingTheMembers_IsRefused_BothPaths()
    {
        // Each kind's document relabelled as the other: its members are the
        // other kind's, and the stated kind's are missing.
        AssertRefusedAloneAndInARecord(TestRecords.MoneySession(), document => TermsOf(document)["Kind"] = "Match");
        AssertRefusedAloneAndInARecord(TestRecords.MatchSession(), document => TermsOf(document)["Kind"] = "Money");
    }

    [Fact]
    public void ASessionKindStatedBesideTheTerms_IsIgnored_TheTermsDecide_BothPaths()
    {
        // Added: the session's Kind is derived from its terms and is not a
        // wire member of its own; a document stating one reads with it
        // ignored, as every derived member is (the record's Xgid). The terms'
        // kind decides, whatever the ignored one says.
        foreach (var session in BothKinds())
        {
            var (alone, inRecord) = Edited(session, document =>
                document.Insert(0, "Kind", session.Kind == SessionKind.Money ? "Match" : "Money"));

            foreach (var (_, options) in WirePaths.Both)
            {
                Assert.Equal(session, JsonSerializer.Deserialize<Session>(alone, options));
                Assert.Equal(session, JsonSerializer.Deserialize<BgDecisionData>(inRecord, options)!.Session);
            }
        }
    }

    public static TheoryData<string, string> ForeignMembers => new()
    {
        // (kind, a member it does not have beside its terms — the other
        // kind's standing, or a fact of its own terms restated)
        { "Money", "Length" },
        { "Money", "OnRollNeeds" },
        { "Money", "OpponentNeeds" },
        { "Money", "IsCrawford" },
        { "Money", "IsJacoby" },
        { "Money", "IsBeaver" },
        { "Money", "CubeLimit" },
        { "Match", "IsJacoby" },
        { "Match", "IsBeaver" },
        { "Match", "CubeLimit" },
        { "Match", "OnRollScore" },
        { "Match", "OpponentScore" },
        { "Match", "Length" },
    };

    [Theory]
    [MemberData(nameof(ForeignMembers))]
    public void AMemberTheKindDoesNotHave_IsRefused_NotDropped_BothPaths(string kind, string member)
    {
        // A money session cannot state a match fact, nor a match a money
        // fact, and neither restates its terms' facts beside them: a full,
        // valid document of the stated kind carrying one such member is never
        // read with the member dropped.
        Session session = kind == "Money" ? TestRecords.MoneySession() : TestRecords.MatchSession();
        var (alone, inRecord) = Edited(session, document =>
            document[member] = member is "IsJacoby" or "IsCrawford" or "IsBeaver" ? JsonValue.Create(false) : JsonValue.Create(1));

        Assert.Contains(member, WirePaths.AssertRefused<Session>(alone).Message);
        WirePaths.AssertRefused<BgDecisionData>(inRecord);
    }

    [Fact]
    public void AKindReadAsItsOwnType_RequiresItsOwnKind_BothPaths()
    {
        // A kind read directly bypasses the converter, but its terms' own
        // contract still requires the kind — their serializer constructor
        // takes it — and refuses the other one.
        var match = SessionDocument(TestRecords.MatchSession());
        foreach (var (_, options) in WirePaths.Both)
            Assert.IsType<MatchSession>(JsonSerializer.Deserialize<MatchSession>(match.ToJsonString(), options));

        var wrong = match.DeepClone().AsObject();
        TermsOf(wrong)["Kind"] = "Money";
        WirePaths.AssertRefused<MatchSession>(wrong.ToJsonString());

        var missing = match.DeepClone().AsObject();
        TermsOf(missing).Remove("Kind");
        WirePaths.AssertRefused<MatchSession>(missing.ToJsonString());

        var money = SessionDocument(TestRecords.MoneySession());
        TermsOf(money).Remove("Kind");
        WirePaths.AssertRefused<MoneySession>(money.ToJsonString());
    }

    // ── A money session states no match fact, a match no money fact ──

    private static HashSet<string> PublicMembers(Type type) =>
        [.. type.GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m is PropertyInfo or MethodInfo { IsSpecialName: false })
            .Select(m => m.Name)];

    [Fact]
    public void TheKinds_ShareOnlyTheBasesMembers()
    {
        // Rewritten for the composed session: what a match has beyond the
        // base, a money session does not, and the reverse — each its own
        // terms and its own standing.
        var shared = PublicMembers(typeof(Session));
        var money = PublicMembers(typeof(MoneySession)).Except(shared).Order().ToArray();
        var match = PublicMembers(typeof(MatchSession)).Except(shared).Order().ToArray();

        Assert.Equal(["OnRollScore", "OpponentScore", "Terms"], money);
        Assert.Equal(["IsCrawford", "OnRollNeeds", "OpponentNeeds", "Terms"], match);
        Assert.Equal(typeof(MoneyTerms), typeof(MoneySession).GetProperty("Terms")!.PropertyType);
        Assert.Equal(typeof(MatchTerms), typeof(MatchSession).GetProperty("Terms")!.PropertyType);
    }

    [Fact]
    public void TheBase_ExposesNoFactOfOneKind()
    {
        var shared = PublicMembers(typeof(Session));

        foreach (var member in new[] { "Terms", "IsJacoby", "IsBeaver", "CubeLimit", "Length", "OnRollNeeds", "OpponentNeeds", "IsCrawford", "MatchLength", "IsMoneyGame" })
            Assert.DoesNotContain(member, shared);
    }

    // ── A closed pair, matched exhaustively ───────────────────────

    [Fact]
    public void TheBase_IsAbstract_AndNotDerivableOutsideTheLibrary()
    {
        Assert.True(typeof(Session).IsAbstract);
        Assert.All(
            typeof(Session).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance),
            ctor => Assert.True(ctor.IsFamilyAndAssembly || ctor.IsAssembly || ctor.IsPrivate,
                $"{ctor} is reachable from outside the library"));
    }

    [Fact]
    public void TheKinds_AreExactlyTwo_AndSealed()
    {
        var kinds = typeof(Session).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Session)))
            .OrderBy(t => t.Name)
            .ToArray();

        Assert.Equal([typeof(MatchSession), typeof(MoneySession)], kinds);
        Assert.All(kinds, t => Assert.True(t.IsSealed));
        Assert.Equal(Enum.GetValues<SessionKind>().Length, kinds.Length);
    }

    [Fact]
    public void Match_HasOneBranchPerKind()
    {
        var match = typeof(Session).GetMethod(nameof(Session.Match))!;
        var branches = match.GetParameters().Select(p => p.ParameterType.GetGenericArguments()[0]).ToArray();

        Assert.Equal([typeof(MoneySession), typeof(MatchSession)], branches);
    }

    [Fact]
    public void MatchAndSwitch_RunTheBranchOfTheSessionsKind()
    {
        foreach (var session in BothKinds())
        {
            string matched = session.Match(money: _ => "money", match: _ => "match");
            string switched = "";
            session.Switch(money: _ => switched = "money", match: _ => switched = "match");

            string expected = session.Kind == SessionKind.Money ? "money" : "match";
            Assert.Equal(expected, matched);
            Assert.Equal(expected, switched);
        }
    }

    [Fact]
    public void MatchAndSwitch_RefuseANullBranch_EvenTheOneNotTaken()
    {
        foreach (var session in BothKinds())
        {
            Assert.Throws<ArgumentNullException>(() => session.Match<int>(null!, _ => 0));
            Assert.Throws<ArgumentNullException>(() => session.Match<int>(_ => 0, null!));
            Assert.Throws<ArgumentNullException>(() => session.Switch(null!, _ => { }));
            Assert.Throws<ArgumentNullException>(() => session.Switch(_ => { }, null!));
        }
    }

    [Fact]
    public void TheKind_IsTheTermsKind_AndNothingCanSetIt()
    {
        // Rewritten: the kind is fixed by the type, which its terms' kind
        // decides; it has no setter at all, since no document states it on
        // the session.
        foreach (var session in BothKinds())
            Assert.Equal(session.Match(money => money.Terms.Kind, match => match.Terms.Kind), session.Kind);
        Assert.Equal(SessionKind.Money, TestRecords.MoneySession().Kind);
        Assert.Equal(SessionKind.Match, TestRecords.MatchSession().Kind);

        Assert.Null(typeof(Session).GetProperty(nameof(Session.Kind))!.SetMethod);
    }

    // ── A value ───────────────────────────────────────────────────

    [Fact]
    public void Equality_IsTheKindAndEachOfItsFacts()
    {
        Session match = TestRecords.MatchSession(length: 9, onRollNeeds: 3, opponentNeeds: 5);
        Session money = TestRecords.MoneySession(isJacoby: true);

        Assert.Equal(match, TestRecords.MatchSession(length: 9, onRollNeeds: 3, opponentNeeds: 5));
        Assert.True(match == TestRecords.MatchSession(length: 9, onRollNeeds: 3, opponentNeeds: 5));
        Assert.Equal(match.GetHashCode(), TestRecords.MatchSession(length: 9, onRollNeeds: 3, opponentNeeds: 5).GetHashCode());
        Assert.Equal(money, TestRecords.MoneySession(isJacoby: true));
        Assert.Equal(money.GetHashCode(), TestRecords.MoneySession(isJacoby: true).GetHashCode());

        // Each fact on its own tells two sessions apart.
        Assert.NotEqual(match, TestRecords.MatchSession(length: 11, onRollNeeds: 3, opponentNeeds: 5));
        Assert.NotEqual(match, TestRecords.MatchSession(length: 9, onRollNeeds: 4, opponentNeeds: 5));
        Assert.NotEqual(match, TestRecords.MatchSession(length: 9, onRollNeeds: 3, opponentNeeds: 4));
        Assert.NotEqual(TestRecords.MatchSession(length: 9, onRollNeeds: 1, opponentNeeds: 5),
            TestRecords.MatchSession(length: 9, onRollNeeds: 1, opponentNeeds: 5, isCrawford: true));
        Assert.NotEqual(money, TestRecords.MoneySession(isJacoby: false));
        Assert.True(money != TestRecords.MoneySession(isJacoby: false));

        // The kinds are never equal, and null equals only null.
        Assert.False(money.Equals(match));
        Assert.False(match.Equals((Session?)null));
        Assert.False(match.Equals((object?)null));
        Assert.True((Session?)null == null);
        Assert.False(match == null);
    }
}
