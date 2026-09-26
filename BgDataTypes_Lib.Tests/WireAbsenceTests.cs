using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Absence across the whole wire graph (halheinrich/backgammon#222), by
/// walking it: every serialized member of every type the documents reach —
/// each decision kind, its categories, a candidate, a move inside a play, and
/// the row of each kind — is removed in turn from a full document, which is
/// then read on the reflection path and through
/// <see cref="BgDataTypesJsonContext"/>, a record as the wire unit
/// <see cref="BgDecisionData"/>. A required member's absence is a
/// <see cref="JsonException"/> on both; a nullable member's absence reads
/// exactly as its explicit <c>null</c> does, on both — as <see langword="null"/>,
/// or refused when <c>null</c> would break a rule of the decision's kind (a
/// checker row without its roll). And every member is exactly one of the two,
/// so none can arrive silently as a default. An explicit <c>null</c> for a
/// member that is not nullable is a <see cref="JsonException"/> too, on both
/// paths, whatever the caller's options say about nullable annotations. The
/// rules are stated on <see cref="BgDataTypesJsonContext"/>.
/// </summary>
/// <remarks>
/// The members come from the context's own metadata, walked alongside the
/// full documents of <see cref="WireGoldenTests"/>, so a member added to any
/// type in the graph is a new case here without an edit. The walk starts at
/// each kind's contract: the base's is its converter, which names no member
/// (<see cref="BgDecisionDataJsonConverter"/>).
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

    /// <summary>A full document: the type its members are walked from, the type it is read as, its JSON.</summary>
    private sealed record Full(Type Walked, Type ReadAs, JsonNode Json);

    private static readonly Dictionary<string, Full> Documents = new()
    {
        ["CheckerPlayDecision"] = new(typeof(CheckerPlayDecision), typeof(BgDecisionData),
            JsonNode.Parse(JsonSerializer.Serialize<BgDecisionData>(WireGoldenTests.FullRecord(), ContextOptions))!),
        ["CubeDecision"] = new(typeof(CubeDecision), typeof(BgDecisionData),
            JsonNode.Parse(JsonSerializer.Serialize<BgDecisionData>(WireGoldenTests.FullCubeRecord(), ContextOptions))!),
        ["DecisionRow"] = new(typeof(DecisionRow), typeof(DecisionRow),
            JsonNode.Parse(JsonSerializer.Serialize(WireGoldenTests.FullRow(), ContextOptions))!),
        ["DecisionRow(Cube)"] = new(typeof(DecisionRow), typeof(DecisionRow),
            JsonNode.Parse(JsonSerializer.Serialize(WireGoldenTests.FullCubeRow(), ContextOptions))!),
    };

    /// <summary>Every member of the graph, found by walking each full document with the context's metadata.</summary>
    private static List<Member> AllMembers()
    {
        var members = new List<Member>();
        foreach (var (name, full) in Documents)
            Walk(name, [], full.Walked, full.Json, members);
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
        // take its members out of every case below. Rewritten for the two
        // decision kinds and their categories.
        var owners = AllMembers().Select(m => m.Owner).ToHashSet();

        Type[] expected =
        [
            typeof(CheckerPlayDecision), typeof(CubeDecision), typeof(PositionData),
            typeof(CheckerPlayDecisionData), typeof(CubeDecisionData), typeof(PlayCandidate),
            typeof(Move), typeof(DescriptiveData), typeof(DecisionRow),
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
    public void AbsentMember_RequiredIsRefused_NullableReadsAsItsNull_OnBothPaths(Member member)
    {
        // Rewritten from AbsentMember_RequiredIsRefused_NullableReadsAsNull_OnBothPaths:
        // a nullable member's absence now reads exactly as its explicit null —
        // which loads as null, or is refused when null would break a rule of
        // the decision's kind (a checker row states its roll).
        var full = Documents[member.Document];
        string absent = Edited(full.Json, member, remove: true);

        foreach (var (path, options) in new[] { ("reflection", ReflectionOptions), ("context", ContextOptions) })
        {
            var ex = Record.Exception(() => JsonSerializer.Deserialize(absent, full.ReadAs, options));
            if (member.Info.IsRequired)
            {
                Assert.True(ex is JsonException,
                    $"{member} absent on the {path} path: {ex?.GetType().Name ?? "loaded"}");
                continue;
            }

            var asNull = Record.Exception(() => JsonSerializer.Deserialize(
                Edited(full.Json, member, remove: false), full.ReadAs, options));
            Assert.True(ex?.GetType() == asNull?.GetType(),
                $"{member} on the {path} path: absent gave {ex?.GetType().Name ?? "a load"}, null gave {asNull?.GetType().Name ?? "a load"}");
            Assert.True(ex is null or JsonException, $"{member} absent on the {path} path: {ex?.GetType().Name}");
            if (ex is null)
            {
                var restored = JsonSerializer.Deserialize(absent, full.ReadAs, options)!;
                var value = member.Info.Get!(Navigate(restored, member.Path));
                Assert.True(value is null, $"{member} absent on the {path} path read as {value}");
            }
        }
    }

    // ── An explicit null for a member that is not nullable ────────

    /// <summary>
    /// Both paths, each with <see cref="JsonSerializerOptions.RespectNullableAnnotations"/>
    /// off (the default) and on: the refusal must not depend on the caller's
    /// options.
    /// </summary>
    private static readonly (string Name, JsonSerializerOptions Options)[] NullOptions =
    [
        ("reflection", ReflectionOptions),
        ("reflection, nullable annotations respected", new JsonSerializerOptions { RespectNullableAnnotations = true }),
        ("context", ContextOptions),
        ("context, nullable annotations respected", new JsonSerializerOptions
        {
            TypeInfoResolver = BgDataTypesJsonContext.Default, RespectNullableAnnotations = true,
        }),
    ];

    [Theory]
    [MemberData(nameof(Members))]
    public void ExplicitNull_ForAMemberThatIsNotNullable_IsRefused_WhateverTheOptions(Member member)
    {
        // Added: an explicit null is refused as a JsonException exactly where
        // null is not the member's value — a value type by the serializer, a
        // reference by the type's own guard — on both paths, with the
        // options' nullable annotations respected or not. A nullable member's
        // null is its "none", pinned above.
        if (IsNullable(member.Info))
            return;

        var full = Documents[member.Document];
        string withNull = Edited(full.Json, member, remove: false);

        foreach (var (path, options) in NullOptions)
        {
            var ex = Record.Exception(() => JsonSerializer.Deserialize(withNull, full.ReadAs, options));
            Assert.True(ex is JsonException,
                $"{member} = null on the {path} path: {ex?.GetType().Name ?? "loaded"}");
        }
    }

    [Fact]
    public void FullDocuments_Load_OnBothPaths()
    {
        // The control for every refusal above: nothing but the removed
        // member stands between each document and a clean read.
        foreach (var full in Documents.Values)
        {
            Assert.NotNull(JsonSerializer.Deserialize(full.Json.ToJsonString(), full.ReadAs, ReflectionOptions));
            Assert.NotNull(JsonSerializer.Deserialize(full.Json.ToJsonString(), full.ReadAs, ContextOptions));
        }
    }

    // ── Navigation ────────────────────────────────────────────────

    /// <summary><paramref name="json"/> with the member removed, or set to <c>null</c>.</summary>
    private static string Edited(JsonNode json, Member member, bool remove)
    {
        var document = json.DeepClone();
        var owner = Navigate(document, member.Path).AsObject();
        Assert.True(owner.ContainsKey(member.Info.Name), $"{member} is not in the full document");
        if (remove)
            owner.Remove(member.Info.Name);
        else
            owner[member.Info.Name] = null;
        return document.ToJsonString();
    }

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
