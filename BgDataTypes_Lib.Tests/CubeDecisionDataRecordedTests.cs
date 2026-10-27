using System.Text.Json;
using System.Text.Json.Nodes;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// A cube decision's recorded cube actions as the record holds them
/// (<see cref="CubeDecisionData.Recorded"/>, halheinrich/backgammon#440): the
/// four wire members read through the value, and a document reaches it
/// through the serializer's constructor, which builds it from all four at
/// once — so a document is held to the value's rules whatever order it
/// states them in, on both paths, and a taker half without a stated double is
/// refused as a <see cref="JsonException"/> naming the rule. The value's own
/// rules are pinned in <see cref="RecordedCubeActionsTests"/>.
/// </summary>
public class CubeDecisionDataRecordedTests
{
    /// <summary>The four wire members, doubler first, as the category writes them.</summary>
    private static readonly string[] DoublerFirst =
        ["UserDoublerAction", "UserTakerAction", "UnstatedDoublerActionError", "UnstatedTakerActionError"];

    /// <summary>The four wire members, taker first.</summary>
    private static readonly string[] TakerFirst =
        ["UnstatedTakerActionError", "UserTakerAction", "UnstatedDoublerActionError", "UserDoublerAction"];

    public static TheoryData<string> Orders => [nameof(DoublerFirst), nameof(TakerFirst)];

    /// <summary>
    /// A cube category's document with the four recorded members stated as
    /// <paramref name="stated"/> gives them (a member it does not name is
    /// left out), in <paramref name="order"/>: doubler first, the four last
    /// as the category writes them, or taker first, the four leading the
    /// document — so either order reaches the serializer in turn.
    /// </summary>
    private static string Document(string order, IReadOnlyDictionary<string, JsonNode?> stated)
    {
        var rest = WirePaths.Document(TestRecords.CubeData(userDoublerAction: null, userTakerAction: null));
        foreach (var member in DoublerFirst)
            rest.Remove(member);

        var document = new JsonObject();
        var members = order == nameof(TakerFirst) ? TakerFirst : DoublerFirst;
        if (order == nameof(TakerFirst))
            AddStated(document, members, stated);
        foreach (var (name, value) in rest.ToArray())
        {
            rest.Remove(name);
            document[name] = value;
        }
        if (order == nameof(DoublerFirst))
            AddStated(document, members, stated);
        return document.ToJsonString();
    }

    private static void AddStated(JsonObject document, string[] members, IReadOnlyDictionary<string, JsonNode?> stated)
    {
        foreach (var member in members)
            if (stated.TryGetValue(member, out var value))
                document[member] = value?.DeepClone();
    }

    /// <summary>A cube category built by code with <paramref name="recorded"/> and the builder's equities.</summary>
    private static CubeDecisionData WithRecorded(RecordedCubeActions recorded) => new()
    {
        AnalysisMode = AnalysisMode.Evaluation, AnalysisLevel = AnalysisLevel.Ply3,
        NoDoubleEquity = 0.512, DoubleTakeEquity = 0.634, CubelessNoDoubleEquity = 0.418, CubelessDoubleTakeEquity = 0.418,
        WinPctAfterNoDouble = 0.709, GammonPctAfterNoDouble = 0.012, BgPctAfterNoDouble = 0,
        LoseGammonPctAfterNoDouble = 0.004, LoseBgPctAfterNoDouble = 0,
        WinPctAfterDoubleTake = 0.709, GammonPctAfterDoubleTake = 0.012, BgPctAfterDoubleTake = 0,
        LoseGammonPctAfterDoubleTake = 0.004, LoseBgPctAfterDoubleTake = 0,
        Recorded = recorded,
    };

    public static TheoryData<RecordedCubeActions> Shapes =>
    [
        RecordedCubeActions.None,
        RecordedCubeActions.NoDouble,
        RecordedCubeActions.DoubleUnanswered,
        RecordedCubeActions.DoubleAnswered(CubeAction.Take),
        RecordedCubeActions.DoubleAnswered(CubeAction.Pass),
        RecordedCubeActions.DoubleWithUnstatedResponse(0.013),
        RecordedCubeActions.UnstatedDoubler(0.042),
    ];

    // ── The record holds the value ────────────────────────────────

    [Theory]
    [MemberData(nameof(Shapes))]
    public void ARecord_CarriesItsRecordedActions_AndTheFourWireMembersAgree(RecordedCubeActions recorded)
    {
        var data = WithRecorded(recorded);

        Assert.Equal(recorded, data.Recorded);
        Assert.Equal(recorded.DoublerAction, data.UserDoublerAction);
        Assert.Equal(recorded.TakerAction, data.UserTakerAction);
        Assert.Equal(recorded.UnstatedDoublerError, data.UnstatedDoublerActionError);
        Assert.Equal(recorded.UnstatedTakerError, data.UnstatedTakerActionError);
    }

    [Fact]
    public void ARecordStatingNoRecordedActions_HoldsNone()
    {
        var data = new CubeDecisionData
        {
            AnalysisMode = AnalysisMode.Evaluation, AnalysisLevel = AnalysisLevel.Ply3,
            NoDoubleEquity = 0.5, DoubleTakeEquity = 0.6, CubelessNoDoubleEquity = 0.4, CubelessDoubleTakeEquity = 0.4,
            WinPctAfterNoDouble = 0.7, GammonPctAfterNoDouble = 0, BgPctAfterNoDouble = 0,
            LoseGammonPctAfterNoDouble = 0, LoseBgPctAfterNoDouble = 0,
            WinPctAfterDoubleTake = 0.7, GammonPctAfterDoubleTake = 0, BgPctAfterDoubleTake = 0,
            LoseGammonPctAfterDoubleTake = 0, LoseBgPctAfterDoubleTake = 0,
        };

        Assert.Equal(RecordedCubeActions.None, data.Recorded);
    }

    [Fact]
    public void TheBuilder_RefusesATakerHalfWithoutAStatedDouble_AsTheValueDoes()
    {
        // TestRecords builds the value from its four arguments, so the
        // library is what refuses — a test that drops the doubler and keeps
        // the builder's default Take states a shape no record can hold.
        Assert.Equal("takerAction",
            Assert.Throws<ArgumentException>(() => TestRecords.CubeData(userDoublerAction: null)).ParamName);
        Assert.Equal("takerAction",
            Assert.Throws<ArgumentException>(() => TestRecords.CubeData(userDoublerAction: CubeAction.NoDouble)).ParamName);
    }

    // ── A document reaches the value through the serializer's constructor ──

    [Theory]
    [MemberData(nameof(Shapes))]
    public void ARecordThatSerializes_ReadsBackEqual_BothPaths(RecordedCubeActions recorded)
    {
        var data = WithRecorded(recorded);

        foreach (var (path, options) in WirePaths.Both)
        {
            string json = JsonSerializer.Serialize(data, options);
            var restored = JsonSerializer.Deserialize<CubeDecisionData>(json, options)!;

            Assert.True(recorded == restored.Recorded, $"{recorded} read back on the {path} path as {restored.Recorded}");
            Assert.Equal(json, JsonSerializer.Serialize(restored, options));
        }
    }

    public static TheoryData<string, string> ValidDocumentsInEachOrder()
    {
        var data = new TheoryData<string, string>();
        foreach (var order in new[] { nameof(DoublerFirst), nameof(TakerFirst) })
        {
            data.Add("DoubleAnswered Take", order);
            data.Add("DoubleWithUnstatedResponse 0.013", order);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(ValidDocumentsInEachOrder))]
    public void AStatedDoubleWithItsTakerHalf_ReadsBackEqual_InEitherOrder_BothPaths(string shape, string order)
    {
        var (stated, expected) = shape == "DoubleAnswered Take"
            ? (new Dictionary<string, JsonNode?>
            {
                ["UserDoublerAction"] = "Double", ["UserTakerAction"] = "Take",
                ["UnstatedDoublerActionError"] = null, ["UnstatedTakerActionError"] = null,
            }, RecordedCubeActions.DoubleAnswered(CubeAction.Take))
            : (new Dictionary<string, JsonNode?>
            {
                ["UserDoublerAction"] = "Double", ["UserTakerAction"] = null,
                ["UnstatedDoublerActionError"] = null, ["UnstatedTakerActionError"] = 0.013,
            }, RecordedCubeActions.DoubleWithUnstatedResponse(0.013));
        string json = Document(order, stated);
        string canonical = JsonSerializer.Serialize(WithRecorded(expected), WirePaths.Context);

        foreach (var (path, options) in WirePaths.Both)
        {
            var restored = JsonSerializer.Deserialize<CubeDecisionData>(json, options)!;

            Assert.True(expected == restored.Recorded, $"{shape}, {order}, on the {path} path: read {restored.Recorded}");
            Assert.Equal(canonical, JsonSerializer.Serialize(restored, WirePaths.Context));
        }
    }

    public static TheoryData<string, RecordedCubeActions> NoTakerHalf_DoublerAbsentOrNull => new()
    {
        { "absent", RecordedCubeActions.None },
        { "null", RecordedCubeActions.None },
        { "absent", RecordedCubeActions.UnstatedDoubler(0.05) },
        { "null", RecordedCubeActions.UnstatedDoubler(0.05) },
    };

    [Theory]
    [MemberData(nameof(NoTakerHalf_DoublerAbsentOrNull))]
    public void ADocumentWithNoTakerHalf_AndTheDoublerAbsentOrNull_ReadsAsTheShapeItStates(string doubler, RecordedCubeActions expected)
    {
        var stated = new Dictionary<string, JsonNode?>();
        if (doubler == "null")
            stated["UserDoublerAction"] = null;
        if (expected.UnstatedDoublerError is double error)
            stated["UnstatedDoublerActionError"] = error;

        foreach (var order in new[] { nameof(DoublerFirst), nameof(TakerFirst) })
            foreach (var (path, options) in WirePaths.Both)
            {
                var restored = JsonSerializer.Deserialize<CubeDecisionData>(Document(order, stated), options)!;
                Assert.True(expected == restored.Recorded, $"doubler {doubler}, {order}, on the {path} path: read {restored.Recorded}");
            }
    }

    public static TheoryData<string, string, string> TakerHalvesWithoutADouble()
    {
        var data = new TheoryData<string, string, string>();
        foreach (var taker in new[] { "a response", "an unstated take error" })
            foreach (var doubler in new[] { "absent", "null", "NoDouble" })
                foreach (var order in new[] { nameof(DoublerFirst), nameof(TakerFirst) })
                    data.Add(taker, doubler, order);
        return data;
    }

    [Theory]
    [MemberData(nameof(TakerHalvesWithoutADouble))]
    public void ADocumentStatingATakerHalfWithoutAStatedDouble_IsRefused_InEitherOrder_BothPaths(string taker, string doubler, string order)
    {
        var stated = new Dictionary<string, JsonNode?>();
        if (taker == "a response")
            stated["UserTakerAction"] = "Take";
        else
            stated["UnstatedTakerActionError"] = 0.013;
        if (doubler != "absent")
            stated["UserDoublerAction"] = doubler == "null" ? null : "NoDouble";

        var ex = WirePaths.AssertRefused<CubeDecisionData>(Document(order, stated));

        Assert.Contains("needs a stated Double", ex.Message);
        Assert.IsType<ArgumentException>(ex.InnerException);
    }

    [Fact]
    public void ARecordStatingATakerHalfWithoutAStatedDouble_IsRefused_AsTheBaseAndAsItsKind_BothPaths()
    {
        // The rule holds wherever the category is read: inside a record, as
        // the wire unit or as its own kind.
        var document = WirePaths.Document<BgDecisionData>(TestRecords.Cube());
        document["Decision"]!["UserDoublerAction"] = null;

        Assert.Contains("needs a stated Double", WirePaths.AssertRefused<BgDecisionData>(document.ToJsonString()).Message);
        Assert.Contains("needs a stated Double", WirePaths.AssertRefused<CubeDecision>(document.ToJsonString()).Message);
    }
}
