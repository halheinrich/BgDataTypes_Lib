using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Absence across the whole wire graph (halheinrich/backgammon#222), by
/// walking it: every serialized member of every type the two document roots
/// reach — the records, their categories, a candidate, a move inside a
/// play — is removed in turn from a full document, which is then read on the
/// reflection path and through <see cref="BgDataTypesJsonContext"/>. A
/// required member's absence is a <see cref="JsonException"/> on both; a
/// nullable member's absence reads as <see langword="null"/> on both. And
/// every member is exactly one of the two, so none can arrive silently as a
/// default. The rule is stated on <see cref="BgDataTypesJsonContext"/>.
/// </summary>
/// <remarks>
/// The members come from the context's own metadata, walked alongside the
/// full documents of <see cref="WireGoldenTests"/>, so a member added to any
/// type in the graph is a new case here without an edit.
/// </remarks>
public class WireAbsenceTests
{
    private static readonly JsonSerializerOptions ReflectionOptions = new();

    private static readonly JsonSerializerOptions ContextOptions = new()
    {
        TypeInfoResolver = BgDataTypesJsonContext.Default
    };

    /// <summary>One serialized member: where its object sits in a document, and its metadata.</summary>
    public sealed record Member(string Document, object[] Path, Type Owner, JsonPropertyInfo Info)
    {
        public override string ToString() =>
            $"{Document}{string.Concat(Path.Select(p => p is int i ? $"[{i}]" : $".{p}"))}.{Info.Name}";
    }

    private static readonly Dictionary<string, (Type Root, JsonNode Json)> Documents = new()
    {
        ["BgDecisionData"] = (typeof(BgDecisionData),
            JsonNode.Parse(JsonSerializer.Serialize(WireGoldenTests.FullRecord(), ContextOptions))!),
        ["DecisionRow"] = (typeof(DecisionRow),
            JsonNode.Parse(JsonSerializer.Serialize(WireGoldenTests.FullRow(), ContextOptions))!),
    };

    /// <summary>Every member of the graph, found by walking each full document with the context's metadata.</summary>
    private static List<Member> AllMembers()
    {
        var members = new List<Member>();
        foreach (var (name, (root, json)) in Documents)
            Walk(name, [], root, json, members);
        return members;
    }

    private static void Walk(string document, object[] path, Type type, JsonNode node, List<Member> members)
    {
        var info = BgDataTypesJsonContext.Default.GetTypeInfo(type)
            ?? throw new InvalidOperationException($"{type} is not in the context.");

        switch (info.Kind)
        {
            case JsonTypeInfoKind.Object:
                // The metadata lists [JsonIgnore]d members too (the derived
                // views); they are not on the wire, so they are not walked.
                foreach (var property in info.Properties.Where(p => !IsIgnored(p)))
                {
                    members.Add(new Member(document, path, type, property));
                    var child = node[property.Name];
                    if (child is not null)
                        Walk(document, [.. path, property.Name], property.PropertyType, child, members);
                }
                break;

            case JsonTypeInfoKind.Enumerable when info.ElementType is { } element && node is JsonArray { Count: > 0 } items:
                Walk(document, [.. path, 0], element, items[0]!, members);
                break;

            // A play is converter-written as an array of moves; the walk
            // follows it into its first move, which the converter reads
            // through the options, rules and all.
            case JsonTypeInfoKind.None when type == typeof(Play) && node is JsonArray { Count: > 0 } moves:
                Walk(document, [.. path, 0], typeof(Move), moves[0]!, members);
                break;
        }
    }

    public static TheoryData<Member> Members => [.. AllMembers()];

    /// <summary>
    /// The CLR property behind a metadata entry. The generated metadata
    /// leaves the attribute provider unset for some [JsonIgnore]d members,
    /// so it is resolved by name — the library sets no naming policy, so a
    /// JSON name is its property's name.
    /// </summary>
    private static PropertyInfo Clr(JsonPropertyInfo property) =>
        property.DeclaringType.GetProperty(property.Name, BindingFlags.Public | BindingFlags.Instance)
        ?? throw new InvalidOperationException($"No property {property.DeclaringType}.{property.Name}.");

    private static bool IsIgnored(JsonPropertyInfo property) =>
        Clr(property).GetCustomAttribute<System.Text.Json.Serialization.JsonIgnoreAttribute>() is
            { Condition: System.Text.Json.Serialization.JsonIgnoreCondition.Always };

    private static bool IsNullable(JsonPropertyInfo property)
    {
        if (property.PropertyType.IsValueType)
            return Nullable.GetUnderlyingType(property.PropertyType) is not null;
        return new NullabilityInfoContext().Create(Clr(property)).ReadState == NullabilityState.Nullable;
    }

    // ── The classification ────────────────────────────────────────

    [Fact]
    public void TheWalk_ReachesEveryTypeOfTheGraph()
    {
        // Guards the walk itself: a type it silently stopped reaching would
        // take its members out of every case below.
        var owners = AllMembers().Select(m => m.Owner).ToHashSet();

        Type[] expected =
        [
            typeof(BgDecisionData), typeof(PositionData), typeof(DecisionData), typeof(PlayCandidate),
            typeof(Move), typeof(DescriptiveData), typeof(PlayOutcomeData), typeof(DecisionRow),
        ];
        Assert.Equal(expected.OrderBy(t => t.Name), owners.OrderBy(t => t.Name));
    }

    [Theory]
    [MemberData(nameof(Members))]
    public void EveryMember_IsRequiredOrNullable_NeverBoth(Member member)
    {
        // The rule's third bullet: no member arrives silently as a default,
        // because none is neither.
        bool required = member.Info.IsRequired;
        bool nullable = IsNullable(member.Info);

        Assert.True(required != nullable,
            $"{member} is {(required ? "required and nullable" : "neither required nor nullable")}");
    }

    [Theory]
    [MemberData(nameof(Members))]
    public void EveryMember_TheReflectionPathAgreesOnWhetherItIsRequired(Member member)
    {
        var reflection = ReflectionOptions.GetTypeInfo(member.Owner).Properties
            .Single(p => p.Name == member.Info.Name);

        Assert.Equal(member.Info.IsRequired, reflection.IsRequired);
    }

    // ── Absence on both paths ─────────────────────────────────────

    [Theory]
    [MemberData(nameof(Members))]
    public void AbsentMember_RequiredIsRefused_NullableReadsAsNull_OnBothPaths(Member member)
    {
        var (root, full) = Documents[member.Document];
        var document = full.DeepClone();
        var owner = Navigate(document, member.Path).AsObject();
        Assert.True(owner.Remove(member.Info.Name), $"{member} is not in the full document");
        string json = document.ToJsonString();

        foreach (var (path, options) in new[] { ("reflection", ReflectionOptions), ("context", ContextOptions) })
        {
            if (member.Info.IsRequired)
            {
                var ex = Record.Exception(() => JsonSerializer.Deserialize(json, root, options));
                Assert.True(ex is JsonException,
                    $"{member} absent on the {path} path: {ex?.GetType().Name ?? "loaded"}");
            }
            else
            {
                var restored = JsonSerializer.Deserialize(json, root, options)!;
                var value = member.Info.Get!(Navigate(restored, member.Path));
                Assert.True(value is null, $"{member} absent on the {path} path read as {value}");
            }
        }
    }

    [Fact]
    public void FullDocuments_Load_OnBothPaths()
    {
        // The control for every refusal above: nothing but the removed
        // member stands between each document and a clean read.
        foreach (var (root, json) in Documents.Values)
        {
            Assert.NotNull(JsonSerializer.Deserialize(json.ToJsonString(), root, ReflectionOptions));
            Assert.NotNull(JsonSerializer.Deserialize(json.ToJsonString(), root, ContextOptions));
        }
    }

    // ── Navigation ────────────────────────────────────────────────

    private static JsonNode Navigate(JsonNode node, object[] path)
    {
        foreach (var step in path)
            node = step is int i ? node[i]! : node[(string)step]!;
        return node;
    }

    private static object Navigate(object value, object[] path)
    {
        foreach (var step in path)
        {
            value = step switch
            {
                int i when value is IList list => list[i]!,
                int i when value is Play play => play[i],
                string name => value.GetType().GetProperty(name)!.GetValue(value)!,
                _ => throw new InvalidOperationException($"Cannot step {step} into {value.GetType()}."),
            };
        }
        return value;
    }
}
