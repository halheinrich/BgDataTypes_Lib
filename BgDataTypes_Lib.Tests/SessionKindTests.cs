using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Money versus match is one of two types (halheinrich/backgammon#273, Hal's
/// ruling of 2026-09-26): <see cref="MoneySession"/> and
/// <see cref="MatchSession"/>, each carrying only its own facts. Pinned here,
/// as <see cref="DecisionKindTests"/> pins the decision kinds: the kind on
/// the wire (explicit, first, read wherever it sits, refused as a
/// <see cref="JsonException"/> on both paths when missing, duplicated,
/// unknown or contradicted — read as a session, as its kind, or inside a
/// record), the separation of the two kinds' facts through the public
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

    private static void AssertRefusedAloneAndInARecord(Session session, Action<JsonObject> edit)
    {
        var (alone, inRecord) = Edited(session, edit);
        WirePaths.AssertRefused<Session>(alone);
        WirePaths.AssertRefused<BgDecisionData>(inRecord);
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

    // ── The kind is explicit ──────────────────────────────────────

    [Fact]
    public void TheKind_IsWrittenFirst_WhateverTheStaticType_BothPaths()
    {
        foreach (var session in BothKinds())
            foreach (var (_, options) in WirePaths.Both)
            {
                string expected = $"{{\"Kind\":\"{session.Kind}\",";
                Assert.StartsWith(expected, JsonSerializer.Serialize(session, options));
                Assert.StartsWith(expected, JsonSerializer.Serialize(session, session.GetType(), options));
                Assert.Contains("\"Session\":" + expected,
                    JsonSerializer.Serialize<BgDecisionData>(RecordsIn(session)[0], options));
            }
    }

    [Fact]
    public void TheKind_IsReadWhereverItSits_BothPaths()
    {
        // JSON objects are unordered: a kind stated last reads as one stated first.
        foreach (var session in BothKinds())
        {
            var (alone, inRecord) = Edited(session, document =>
            {
                var kind = document["Kind"]!.DeepClone();
                document.Remove("Kind");
                document.Add("Kind", kind);
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
            document.Remove("Kind");
            if (kindJson.Length > 0)
                document.Insert(0, "Kind", JsonNode.Parse(kindJson));
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

        AssertRefusedAloneAndInARecord(session, edit);
    }

    [Fact]
    public void AKindStatedTwice_IsRefused_BothPaths()
    {
        foreach (var second in new[] { "Match", "Money" })
        {
            var json = SessionDocument(TestRecords.MatchSession()).ToJsonString()[..^1] + $",\"Kind\":\"{second}\"}}";

            var ex = WirePaths.AssertRefused<Session>(json);
            Assert.Contains("once", ex.Message);
        }
    }

    [Fact]
    public void AKindContradictingTheMembers_IsRefused_BothPaths()
    {
        // Each kind's document relabelled as the other: its members are the
        // other kind's, and the stated kind's are missing.
        AssertRefusedAloneAndInARecord(TestRecords.MoneySession(), document => document["Kind"] = "Match");
        AssertRefusedAloneAndInARecord(TestRecords.MatchSession(), document => document["Kind"] = "Money");
    }

    public static TheoryData<string, string> ForeignMembers => new()
    {
        // (kind, the other kind's member)
        { "Money", "Length" },
        { "Money", "OnRollNeeds" },
        { "Money", "OpponentNeeds" },
        { "Money", "IsCrawford" },
        { "Match", "IsJacoby" },
    };

    [Theory]
    [MemberData(nameof(ForeignMembers))]
    public void AMemberOfTheOtherKind_IsRefused_NotDropped_BothPaths(string kind, string member)
    {
        // A money session cannot state a match fact, nor a match a money
        // fact: a full, valid document of the stated kind carrying one member
        // the other kind has is never read with the member dropped.
        Session session = kind == "Money" ? TestRecords.MoneySession() : TestRecords.MatchSession();
        var (alone, inRecord) = Edited(session, document =>
            document[member] = member is "IsJacoby" or "IsCrawford" ? JsonValue.Create(false) : JsonValue.Create(1));

        Assert.Contains(member, WirePaths.AssertRefused<Session>(alone).Message);
        WirePaths.AssertRefused<BgDecisionData>(inRecord);
    }

    [Fact]
    public void AKindReadAsItsOwnType_RequiresItsOwnKind_BothPaths()
    {
        // A kind read directly bypasses the converter, but its own contract
        // still requires the kind — its serializer constructor takes it — and
        // refuses the other one.
        var match = SessionDocument(TestRecords.MatchSession());
        foreach (var (_, options) in WirePaths.Both)
            Assert.IsType<MatchSession>(JsonSerializer.Deserialize<MatchSession>(match.ToJsonString(), options));

        var wrong = match.DeepClone().AsObject();
        wrong["Kind"] = "Money";
        WirePaths.AssertRefused<MatchSession>(wrong.ToJsonString());

        var missing = match.DeepClone().AsObject();
        missing.Remove("Kind");
        WirePaths.AssertRefused<MatchSession>(missing.ToJsonString());
        WirePaths.AssertRefused<MoneySession>(SessionDocument(TestRecords.MoneySession()).ToJsonString().Replace("\"Kind\":\"Money\",", ""));
    }

    // ── A money session states no match fact, a match no money fact ──

    private static HashSet<string> PublicMembers(Type type) =>
        [.. type.GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m is PropertyInfo or MethodInfo { IsSpecialName: false })
            .Select(m => m.Name)];

    [Fact]
    public void TheKinds_ShareOnlyTheBasesMembers()
    {
        // What a match has beyond the base, a money session does not, and the
        // reverse; so code reading one kind's fact off the other cannot
        // compile.
        var shared = PublicMembers(typeof(Session));
        var money = PublicMembers(typeof(MoneySession)).Except(shared).Order().ToArray();
        var match = PublicMembers(typeof(MatchSession)).Except(shared).Order().ToArray();

        Assert.Equal(["IsJacoby"], money);
        Assert.Equal(["IsCrawford", "Length", "OnRollNeeds", "OpponentNeeds"], match);
    }

    [Fact]
    public void TheBase_ExposesNoFactOfOneKind()
    {
        var shared = PublicMembers(typeof(Session));

        foreach (var member in new[] { "IsJacoby", "Length", "OnRollNeeds", "OpponentNeeds", "IsCrawford", "MatchLength", "IsMoneyGame" })
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
    public void TheKind_IsTheTypes_AndCodeCannotSetIt()
    {
        Assert.Equal(SessionKind.Money, TestRecords.MoneySession().Kind);
        Assert.Equal(SessionKind.Match, TestRecords.MatchSession().Kind);

        var kind = typeof(Session).GetProperty(nameof(Session.Kind))!;
        Assert.False(kind.SetMethod!.IsPublic);
        Assert.False(kind.SetMethod.IsFamily);
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
