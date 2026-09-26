using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// A header's money-versus-match facts on the wire: <see cref="SessionTerms"/>
/// (<see cref="IMatchInfo.Terms"/>) and <see cref="GameStanding"/>
/// (<see cref="IGameInfo.Standing"/>) are kinded documents, read through the
/// one dispatch as a record and a session are (halheinrich/backgammon#273,
/// the umbrella's review of 2026-09-26). A producer serializes its header
/// types — the converter's are in its own context — so an abstract member
/// with no contract would be written with its facts dropped and could not be
/// read back. Pinned here, on both paths: the round trip, as the base and as
/// the kind; the kind explicit, first, read wherever it sits; every refusal a
/// <see cref="JsonException"/> — a missing, unknown, duplicated or
/// contradicted kind, a member of the other kind, a missing member; and a
/// producer's header document through the chained resolvers. The kinds' rules,
/// broken in a document, are in <see cref="DocumentRefusalTests"/>.
/// </summary>
public class HeaderWireTests
{
    private static SessionTerms[] BothTerms() =>
    [
        new MoneyTerms { IsJacoby = false, IsBeaver = true, CubeLimit = 64 },
        new MatchTerms { Length = 9 },
    ];

    private static GameStanding[] BothStandings() =>
    [
        new MoneyStanding { Score1 = 3, Score2 = 11 },
        new MatchStanding { Away1 = 1, Away2 = 4, IsCrawford = true },
    ];

    /// <summary>Every value of both families, with the family's base type to read it as.</summary>
    private static IEnumerable<(object Value, Type Base)> Both() =>
        BothTerms().Select(t => ((object)t, typeof(SessionTerms)))
            .Concat(BothStandings().Select(s => ((object)s, typeof(GameStanding))));

    private static SessionKind KindOf(object value) => value switch
    {
        SessionTerms terms => terms.Kind,
        GameStanding standing => standing.Kind,
        _ => throw new ArgumentException("Not a header value.", nameof(value)),
    };

    private static JsonObject DocumentOf(object value, Type type) =>
        JsonNode.Parse(JsonSerializer.Serialize(value, type, WirePaths.Context))!.AsObject();

    private static void AssertRefused(Type readAs, string json, string? saying = null)
    {
        foreach (var (path, options) in WirePaths.Both)
        {
            var ex = Record.Exception(() => JsonSerializer.Deserialize(json, readAs, options));
            Assert.True(ex is JsonException, $"read as {readAs.Name} on the {path} path: expected a JsonException, got {ex?.GetType().Name ?? "no exception"}");
            if (saying is not null)
                Assert.Contains(saying, ex!.Message);
        }
    }

    // ── Round trips ───────────────────────────────────────────────

    [Fact]
    public void EachKind_RoundTripsAsItsKind_AsTheBaseAndAsItself_BothPaths()
    {
        foreach (var (value, @base) in Both())
            foreach (var type in new[] { @base, value.GetType() })
                foreach (var (_, options) in WirePaths.Both)
                {
                    var json = JsonSerializer.Serialize(value, type, options);
                    var restored = JsonSerializer.Deserialize(json, type, options)!;

                    Assert.Equal(value.GetType(), restored.GetType());
                    Assert.Equal(value, restored);
                    Assert.Equal(json, JsonSerializer.Serialize(restored, type, options));
                }
    }

    [Fact]
    public void AProducersHeader_RoundTripsItsTermsAndStanding_ThroughTheChainedResolvers()
    {
        // The converter's header types implement both contracts and are
        // serialized by its own context; chained after this one, the terms and
        // the standing reach their kinds' contracts as a record's kinds do,
        // byte-identically to the reflection path.
        var chained = new JsonSerializerOptions
        {
            TypeInfoResolver = JsonTypeInfoResolver.Combine(ConsumerContext.Default, BgDataTypesJsonContext.Default),
        };
        foreach (var terms in BothTerms())
            foreach (var standing in BothStandings().Where(s => s.Kind == terms.Kind))
            {
                var header = new ConsumerHeader { Player1 = "Mochy", Player2 = "Falafel", Terms = terms, IsStandardStart = true, Standing = standing };

                var json = JsonSerializer.Serialize(header, chained);
                Assert.Equal(JsonSerializer.Serialize(header, WirePaths.Reflection), json);

                var restored = JsonSerializer.Deserialize<ConsumerHeader>(json, chained)!;
                Assert.Equal(terms, restored.Terms);
                Assert.Equal(standing, restored.Standing);
            }
    }

    [Fact]
    public void AProducersHeader_RoundTrips_UnderItsNamingPolicy()
    {
        // The converter writes its header types camelCase; the dispatch finds
        // the kind as the kinds' contracts name it, under the policy.
        var camel = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        foreach (var terms in BothTerms())
            foreach (var standing in BothStandings().Where(s => s.Kind == terms.Kind))
            {
                var header = new ConsumerHeader { Player1 = "Mochy", Player2 = "Falafel", Terms = terms, IsStandardStart = true, Standing = standing };

                var json = JsonSerializer.Serialize(header, camel);
                Assert.Contains($"\"terms\":{{\"kind\":\"{terms.Kind}\",", json);
                Assert.Contains($"\"standing\":{{\"kind\":\"{standing.Kind}\",", json);

                var restored = JsonSerializer.Deserialize<ConsumerHeader>(json, camel)!;
                Assert.Equal(terms, restored.Terms);
                Assert.Equal(standing, restored.Standing);
            }
    }

    // ── The kind is explicit ──────────────────────────────────────

    [Fact]
    public void TheKind_IsWrittenFirst_WhateverTheStaticType_BothPaths()
    {
        foreach (var (value, @base) in Both())
            foreach (var (_, options) in WirePaths.Both)
            {
                string expected = $"{{\"Kind\":\"{KindOf(value)}\",";
                Assert.StartsWith(expected, JsonSerializer.Serialize(value, @base, options));
                Assert.StartsWith(expected, JsonSerializer.Serialize(value, value.GetType(), options));
            }
    }

    [Fact]
    public void TheKind_IsReadWhereverItSits_BothPaths()
    {
        foreach (var (value, @base) in Both())
        {
            var document = DocumentOf(value, @base);
            var kind = document["Kind"]!.DeepClone();
            document.Remove("Kind");
            document.Add("Kind", kind);

            foreach (var (_, options) in WirePaths.Both)
                Assert.Equal(value, JsonSerializer.Deserialize(document.ToJsonString(), @base, options));
        }
    }

    public static TheoryData<string, string> BadKinds => new()
    {
        { "missing", "" },
        { "unknown", "\"Chouette\"" },
        { "numeric", "1" },
        { "null", "null" },
        { "an object", "{}" },
    };

    [Theory]
    [MemberData(nameof(BadKinds))]
    public void ABadKind_IsRefused_BothPaths(string name, string kindJson)
    {
        foreach (var (value, @base) in Both())
        {
            var document = DocumentOf(value, @base);
            document.Remove("Kind");
            if (kindJson.Length > 0)
                document.Insert(0, "Kind", JsonNode.Parse(kindJson));

            // A missing kind is the dispatch's refusal, which says what the
            // document states — not a guessed kind's contract's complaint.
            string? saying = name != "missing" ? null
                : @base == typeof(SessionTerms) ? "A set of session terms states its Kind — Money or Match."
                : "A game standing states its Kind — Money or Match.";
            AssertRefused(@base, document.ToJsonString(), saying);
        }
    }

    [Fact]
    public void AKindStatedTwice_IsRefused_BothPaths()
    {
        foreach (var (value, @base) in Both())
            foreach (var second in new[] { "Money", "Match" })
            {
                var json = DocumentOf(value, @base).ToJsonString()[..^1] + $",\"Kind\":\"{second}\"}}";
                AssertRefused(@base, json, "once");
            }
    }

    [Fact]
    public void AKindContradictingTheMembers_IsRefused_BothPaths()
    {
        // Each kind's document relabelled as the other: its members are the
        // other kind's, and the stated kind's are missing.
        foreach (var (value, @base) in Both())
        {
            var document = DocumentOf(value, @base);
            document["Kind"] = KindOf(value) == SessionKind.Money ? "Match" : "Money";
            AssertRefused(@base, document.ToJsonString());
        }
    }

    public static TheoryData<string, string> ForeignMembers => new()
    {
        // (family and kind, the other kind's member)
        { "MoneyTerms", "Length" },
        { "MatchTerms", "IsJacoby" },
        { "MatchTerms", "IsBeaver" },
        { "MatchTerms", "CubeLimit" },
        { "MoneyStanding", "Away1" },
        { "MoneyStanding", "Away2" },
        { "MoneyStanding", "IsCrawford" },
        { "MatchStanding", "Score1" },
        { "MatchStanding", "Score2" },
    };

    [Theory]
    [MemberData(nameof(ForeignMembers))]
    public void AMemberOfTheOtherKind_IsRefused_NotDropped_BothPaths(string kind, string member)
    {
        var (value, @base) = Both().Single(v => v.Value.GetType().Name == kind);
        var document = DocumentOf(value, @base);
        document[member] = member is "IsJacoby" or "IsBeaver" or "IsCrawford" ? JsonValue.Create(false) : JsonValue.Create(1);

        AssertRefused(@base, document.ToJsonString(), member);
        AssertRefused(value.GetType(), document.ToJsonString(), member);
    }

    [Fact]
    public void EachMember_IsRequired_BothPaths()
    {
        // No member of either family reads as a default when absent: every
        // fact is stated, as a session's are.
        foreach (var (value, @base) in Both())
            foreach (var member in DocumentOf(value, @base).Select(p => p.Key).Where(k => k != "Kind"))
            {
                var document = DocumentOf(value, @base);
                document.Remove(member);
                AssertRefused(@base, document.ToJsonString());
                AssertRefused(value.GetType(), document.ToJsonString());
            }
    }

    [Fact]
    public void AKindReadAsItsOwnType_RequiresItsOwnKind_BothPaths()
    {
        // A kind read directly bypasses the converter, but its own contract
        // still requires the kind — its serializer constructor takes it — and
        // refuses the other one.
        foreach (var (value, @base) in Both())
        {
            var document = DocumentOf(value, @base);
            foreach (var (_, options) in WirePaths.Both)
                Assert.IsType(value.GetType(), JsonSerializer.Deserialize(document.ToJsonString(), value.GetType(), options));

            var wrong = document.DeepClone().AsObject();
            wrong["Kind"] = KindOf(value) == SessionKind.Money ? "Match" : "Money";
            AssertRefused(value.GetType(), wrong.ToJsonString());

            var missing = document.DeepClone().AsObject();
            missing.Remove("Kind");
            AssertRefused(value.GetType(), missing.ToJsonString());
        }
    }

    [Theory]
    [InlineData(3, 5)]      // neither player at match point
    [InlineData(1, 1)]      // both
    public void ACrawfordStandingWithoutOnePlayerAtMatchPoint_IsRefused_InEitherMemberOrder_BothPaths(int away1, int away2)
    {
        // The rule spans three members; whichever completes the breach
        // refuses it, and a document gets the refusal as a JsonException
        // whatever order it states them in.
        foreach (var flagFirst in new[] { true, false })
        {
            string json = flagFirst
                ? $$"""{"Kind":"Match","IsCrawford":true,"Away1":{{away1}},"Away2":{{away2}}}"""
                : $$"""{"Kind":"Match","Away1":{{away1}},"Away2":{{away2}},"IsCrawford":true}""";

            AssertRefused(typeof(GameStanding), json, "Crawford");
            AssertRefused(typeof(MatchStanding), json, "Crawford");
        }
    }
}

/// <summary>
/// A stand-in producer header: the shape of the converter's match and game
/// header types, which implement the skip-early contracts and are serialized
/// by the producer's own context, chained after this library's.
/// </summary>
public sealed class ConsumerHeader : IMatchInfo, IGameInfo
{
    /// <inheritdoc/>
    public required string Player1 { get; init; }

    /// <inheritdoc/>
    public required string Player2 { get; init; }

    /// <inheritdoc/>
    public required SessionTerms Terms { get; init; }

    /// <inheritdoc/>
    public required bool IsStandardStart { get; init; }

    /// <inheritdoc/>
    public required GameStanding Standing { get; init; }
}
