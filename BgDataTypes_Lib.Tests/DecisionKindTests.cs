using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// A decision is one of two types (halheinrich/backgammon#273, Hal's ruling
/// of 2026-09-25): <see cref="CheckerPlayDecision"/> and
/// <see cref="CubeDecision"/>, each carrying only its own members. Pinned
/// here: the kinds on the wire (explicit, first, read wherever it sits,
/// refused as a <see cref="JsonException"/> on both paths when missing,
/// duplicated, unknown or contradicted), the separation of the two kinds'
/// members through the public surface — reading one kind's member off the
/// other does not compile, which reflection states — and the closed pair
/// that makes <see cref="BgDecisionData.Match{TResult}"/> exhaustive.
/// </summary>
public class DecisionKindTests
{
    private static BgDecisionData[] BothKinds() => [TestRecords.CheckerPlay(), TestRecords.Cube()];

    /// <summary>
    /// A collection of records is a consumer's document, so its metadata is
    /// the consumer's context, chained with this library's — the composition
    /// pattern (see BgDataTypesJsonContextTests' consumer stand-in).
    /// </summary>
    private static readonly (string Name, JsonSerializerOptions Options)[] DocumentPaths =
    [
        ("reflection", WirePaths.Reflection),
        ("chained", new JsonSerializerOptions
        {
            TypeInfoResolver = System.Text.Json.Serialization.Metadata.JsonTypeInfoResolver.Combine(
                ConsumerContext.Default, BgDataTypesJsonContext.Default)
        }),
    ];

    // ── Round trips ───────────────────────────────────────────────

    [Fact]
    public void EachKind_RoundTripsAsItsKind_BothPaths()
    {
        foreach (var record in BothKinds())
            foreach (var (path, options) in WirePaths.Both)
            {
                var json = JsonSerializer.Serialize(record, options);
                var restored = JsonSerializer.Deserialize<BgDecisionData>(json, options)!;

                Assert.Equal(record.GetType(), restored.GetType());
                Assert.Equal(record.Kind, restored.Kind);
                Assert.Equal(json, JsonSerializer.Serialize(restored, options));
            }
    }

    [Fact]
    public void EachKind_InAConsumersDocument_RoundTripsAsItsKind()
    {
        foreach (var (_, options) in DocumentPaths)
        {
            var restored = JsonSerializer.Deserialize<ConsumerDocument>(
                JsonSerializer.Serialize(new ConsumerDocument { Decisions = [.. BothKinds()] }, options), options)!;

            Assert.IsType<CheckerPlayDecision>(restored.Decisions[0]);
            Assert.IsType<CubeDecision>(restored.Decisions[1]);
        }
    }

    // ── The kind is explicit ──────────────────────────────────────

    [Fact]
    public void TheKind_IsWrittenFirst_WhateverTheStaticType_BothPaths()
    {
        foreach (var record in BothKinds())
            foreach (var (_, options) in WirePaths.Both)
            {
                string expected = $"{{\"Kind\":\"{record.Kind}\",";
                Assert.StartsWith(expected, JsonSerializer.Serialize(record, options));
                Assert.StartsWith(expected, JsonSerializer.Serialize(record, record.GetType(), options));
            }
        foreach (var (_, options) in DocumentPaths)
        {
            var json = JsonSerializer.Serialize(new ConsumerDocument { Decisions = [.. BothKinds()] }, options);
            Assert.StartsWith("{\"Decisions\":[{\"Kind\":\"CheckerPlay\",", json);
            Assert.Contains("},{\"Kind\":\"Cube\",", json);
        }
    }

    [Fact]
    public void TheKind_IsReadWhereverItSits_BothPaths()
    {
        // JSON objects are unordered: a kind stated last reads as one stated first.
        foreach (var record in BothKinds())
        {
            var document = WirePaths.Document(record);
            var kind = document["Kind"]!.DeepClone();
            document.Remove("Kind");
            document.Add("Kind", kind);

            foreach (var (_, options) in WirePaths.Both)
                Assert.Equal(record.GetType(),
                    JsonSerializer.Deserialize<BgDecisionData>(document.ToJsonString(), options)!.GetType());
        }
    }

    public static TheoryData<string, string> BadKinds => new()
    {
        { "missing", "" },
        { "unknown", "\"Chequer\"" },
        { "numeric", "1" },
        { "null", "null" },
        { "an object", "{}" },
        { "differently cased name", "\"cube\"" },
    };

    [Theory]
    [MemberData(nameof(BadKinds))]
    public void ABadKind_IsRefused_BothPaths(string name, string kindJson)
    {
        var document = WirePaths.Document<BgDecisionData>(TestRecords.Cube());
        document.Remove("Kind");
        string json = kindJson.Length == 0
            ? document.ToJsonString()
            : "{\"Kind\":" + kindJson + "," + document.ToJsonString()[1..];

        if (name == "differently cased name")
        {
            // The strict enum token matches names case-insensitively
            // (StrictJsonStringEnumConverter's remarks), so "cube" is Cube:
            // the strictness closed is the token's kind, not its case.
            foreach (var (_, options) in WirePaths.Both)
                Assert.IsType<CubeDecision>(JsonSerializer.Deserialize<BgDecisionData>(json, options));
            return;
        }

        WirePaths.AssertRefused<BgDecisionData>(json);
    }

    [Fact]
    public void AKindStatedTwice_IsRefused_BothPaths()
    {
        foreach (var second in new[] { "Cube", "CheckerPlay" })
        {
            var document = WirePaths.Document<BgDecisionData>(TestRecords.Cube());
            string json = document.ToJsonString()[..^1] + $",\"Kind\":\"{second}\"}}";

            var ex = WirePaths.AssertRefused<BgDecisionData>(json);
            Assert.Contains("once", ex.Message);
        }
    }

    // ── A kind that contradicts the document's members ────────────

    [Fact]
    public void AKindContradictingTheMembers_IsRefused_BothPaths()
    {
        // Each kind's document relabelled as the other: its members are the
        // other kind's, and the stated kind's are missing.
        var play = WirePaths.Document<BgDecisionData>(TestRecords.CheckerPlay());
        var cube = WirePaths.Document<BgDecisionData>(TestRecords.Cube());
        play["Kind"] = "Cube";
        cube["Kind"] = "CheckerPlay";

        WirePaths.AssertRefused<BgDecisionData>(play.ToJsonString());
        WirePaths.AssertRefused<BgDecisionData>(cube.ToJsonString());
    }

    public static TheoryData<string, string, string> ForeignMembers => new()
    {
        // (kind, where, the other kind's member)
        { "Cube", "Decision", "Plays" },
        { "Cube", "Decision", "Dice" },
        { "Cube", "Decision", "UserPlayIndex" },
        { "Cube", "", "AfterBestBoard" },
        { "Cube", "", "Outcome" },
        { "CheckerPlay", "Decision", "NoDoubleEquity" },
        { "CheckerPlay", "Decision", "Depth" },
        { "CheckerPlay", "Decision", "UserDoublerAction" },
        { "CheckerPlay", "", "CanBeTooGood" },
        { "CheckerPlay", "Decision", "IsCube" },
    };

    [Theory]
    [MemberData(nameof(ForeignMembers))]
    public void AMemberOfTheOtherKind_IsRefused_NotDropped_BothPaths(string kind, string where, string member)
    {
        // A full, valid document of the stated kind, carrying one member the
        // other kind has: never loaded with the member silently dropped.
        var document = WirePaths.Document<BgDecisionData>(
            kind == "Cube" ? TestRecords.Cube() : TestRecords.CheckerPlay());
        var owner = where.Length == 0 ? document : document[where]!.AsObject();
        owner[member] = 1;

        var ex = WirePaths.AssertRefused<BgDecisionData>(document.ToJsonString());
        Assert.Contains(member, ex.Message);
    }

    [Fact]
    public void AnIdNamingTheOtherKind_IsRefused_AtConstructionAndOnTheWire()
    {
        // An XgDecisionId names its kind (":cube" / ":play"); a record whose
        // id names the other kind contradicts itself.
        var ex = Assert.Throws<ArgumentException>(
            () => TestRecords.Cube(id: new XgDecisionId("m.xg", 1, 2, IsCube: false)));
        Assert.Equal("Id", ex.ParamName);
        Assert.Throws<ArgumentException>(
            () => TestRecords.CheckerPlay(id: new XgDecisionId("m.xg", 1, 1, IsCube: true)));

        var document = WirePaths.Document<BgDecisionData>(TestRecords.Cube());
        document["Id"] = "m.xg:g1:m2:play";
        var wire = WirePaths.AssertRefused<BgDecisionData>(document.ToJsonString());
        Assert.IsType<ArgumentException>(wire.InnerException);

        // An XgpDecisionId names no kind, and fits either.
        Assert.IsType<CubeDecision>(TestRecords.Cube(id: new XgpDecisionId("p.xgp")));
        Assert.IsType<CheckerPlayDecision>(TestRecords.CheckerPlay(id: new XgpDecisionId("p.xgp")));
    }

    // ── A kind read directly by its own type ──────────────────────

    [Fact]
    public void AKindReadAsItsOwnType_RequiresItsOwnKind_BothPaths()
    {
        // The stated limit of the converter: a kind read directly bypasses
        // it, but its own contract still requires the kind and refuses the
        // other one.
        var cube = WirePaths.Document<BgDecisionData>(TestRecords.Cube());
        foreach (var (_, options) in WirePaths.Both)
            Assert.IsType<CubeDecision>(JsonSerializer.Deserialize<CubeDecision>(cube.ToJsonString(), options));

        var wrong = cube.DeepClone().AsObject();
        wrong["Kind"] = "CheckerPlay";
        WirePaths.AssertRefused<CubeDecision>(wrong.ToJsonString());

        var missing = cube.DeepClone().AsObject();
        missing.Remove("Kind");
        WirePaths.AssertRefused<CubeDecision>(missing.ToJsonString());
    }

    [Fact]
    public void AKindReadAsItsOwnType_SurfacesAConstructionRuleAsTheGuardsOwnException()
    {
        // The other half of the stated limit, pinned so a change to it is
        // seen: read as BgDecisionData, a broken construction rule is a
        // JsonException; read by its own type, it is the init guard's.
        var document = WirePaths.Document<BgDecisionData>(TestRecords.Cube());
        document["Position"]!["IsCrawford"] = true;

        Assert.Throws<ArgumentException>(
            () => JsonSerializer.Deserialize<CubeDecision>(document.ToJsonString(), WirePaths.Context));
        WirePaths.AssertRefused<BgDecisionData>(document.ToJsonString());
    }

    // ── Reading one kind's member off the other does not compile ──

    private static HashSet<string> PublicMembers(Type type) =>
        [.. type.GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m is PropertyInfo or MethodInfo { IsSpecialName: false })
            .Select(m => m.Name)];

    [Fact]
    public void TheKinds_ShareOnlyTheBasesMembers()
    {
        // What a checker play has beyond the base, a cube decision does not,
        // and the reverse; so code reading one kind's member off the other
        // cannot compile. (Decision is named on both, typed per kind.)
        var shared = PublicMembers(typeof(BgDecisionData));
        var play = PublicMembers(typeof(CheckerPlayDecision)).Except(shared).ToHashSet();
        var cube = PublicMembers(typeof(CubeDecision)).Except(shared).ToHashSet();

        Assert.Equal(new[] { "AfterBestBoard", "AfterPlayerBoard", "Decision", "Dice" }, play.Order().ToArray());
        Assert.Equal(new[] { "CanBeTooGood", "Decision" }, cube.Order().ToArray());
        Assert.NotEqual(
            typeof(CheckerPlayDecision).GetProperty("Decision")!.PropertyType,
            typeof(CubeDecision).GetProperty("Decision")!.PropertyType);
    }

    [Fact]
    public void TheDecisionCategories_ShareNoMember()
    {
        var play = PublicMembers(typeof(CheckerPlayDecisionData));
        var cube = PublicMembers(typeof(CubeDecisionData));
        var objectMembers = PublicMembers(typeof(object));

        Assert.Empty(play.Intersect(cube).Except(objectMembers));
        Assert.Contains("Plays", play);
        Assert.Contains("NoDoubleEquity", cube);
    }

    [Fact]
    public void TheBase_ExposesNoMemberOfOneKind()
    {
        // The base's surface is what every decision has: the checker play's
        // own filter members are explicit interface implementations, off the
        // base's and the cube's surface, and no cube member is on it.
        var shared = PublicMembers(typeof(BgDecisionData));

        foreach (var member in new[]
                 {
                     "Dice", "AfterBestBoard", "AfterPlayerBoard", "Plays", "UserPlayIndex",
                     "CanBeTooGood", "NoDoubleEquity", "BestClaimPair", "IsCube", "Outcome",
                 })
            Assert.DoesNotContain(member, shared);
    }

    // ── A closed pair, matched exhaustively ───────────────────────

    [Fact]
    public void TheBase_IsAbstract_AndNotDerivableOutsideTheLibrary()
    {
        Assert.True(typeof(BgDecisionData).IsAbstract);
        Assert.All(
            typeof(BgDecisionData).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance),
            ctor => Assert.True(ctor.IsFamilyAndAssembly || ctor.IsAssembly || ctor.IsPrivate,
                $"{ctor} is reachable from outside the library"));
    }

    [Fact]
    public void TheKinds_AreExactlyTwo_AndSealed()
    {
        var kinds = typeof(BgDecisionData).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BgDecisionData)))
            .OrderBy(t => t.Name)
            .ToArray();

        Assert.Equal([typeof(CheckerPlayDecision), typeof(CubeDecision)], kinds);
        Assert.All(kinds, t => Assert.True(t.IsSealed));
        Assert.Equal(Enum.GetValues<DecisionKind>().Length, kinds.Length);
    }

    [Fact]
    public void Match_HasOneBranchPerKind()
    {
        // The exhaustiveness is structural: one branch parameter per kind, so
        // a third kind would break every call at compile time.
        var match = typeof(BgDecisionData).GetMethod(nameof(BgDecisionData.Match))!;
        var branches = match.GetParameters().Select(p => p.ParameterType.GetGenericArguments()[0]).ToArray();

        Assert.Equal([typeof(CheckerPlayDecision), typeof(CubeDecision)], branches);
    }

    [Fact]
    public void MatchAndSwitch_RunTheBranchOfTheRecordsKind()
    {
        foreach (var record in BothKinds())
        {
            string matched = record.Match(checkerPlay: _ => "play", cube: _ => "cube");
            string switched = "";
            record.Switch(checkerPlay: _ => switched = "play", cube: _ => switched = "cube");

            string expected = record.Kind == DecisionKind.Cube ? "cube" : "play";
            Assert.Equal(expected, matched);
            Assert.Equal(expected, switched);
        }
    }

    [Fact]
    public void MatchAndSwitch_RefuseANullBranch_EvenTheOneNotTaken()
    {
        foreach (var record in BothKinds())
        {
            Assert.Throws<ArgumentNullException>(() => record.Match<int>(null!, _ => 0));
            Assert.Throws<ArgumentNullException>(() => record.Match<int>(_ => 0, null!));
            Assert.Throws<ArgumentNullException>(() => record.Switch(null!, _ => { }));
            Assert.Throws<ArgumentNullException>(() => record.Switch(_ => { }, null!));
        }
    }

    [Fact]
    public void TheKind_IsTheTypes_AndCodeCannotSetIt()
    {
        Assert.Equal(DecisionKind.CheckerPlay, TestRecords.CheckerPlay().Kind);
        Assert.Equal(DecisionKind.Cube, TestRecords.Cube().Kind);

        var kind = typeof(BgDecisionData).GetProperty(nameof(BgDecisionData.Kind))!;
        Assert.False(kind.SetMethod!.IsPublic);
        Assert.False(kind.SetMethod.IsFamily);
    }
}
