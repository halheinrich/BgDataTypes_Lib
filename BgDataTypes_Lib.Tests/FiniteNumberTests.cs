using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Every stored floating-point value in the records is finite (the umbrella's
/// fourth-round ruling on the records leg of halheinrich/backgammon#273; the
/// rule stated once on the internal FiniteNumber): the candidate's equity and
/// probabilities, the cube's equities and probabilities, and the analyser's
/// stored errors. A NaN or an infinity is refused from code with the guard's
/// <see cref="ArgumentOutOfRangeException"/> naming the member, and from a
/// document — where the reader's options let one in at all — with a
/// <see cref="JsonException"/> carrying it, on both paths. The members are
/// found by reflection, so a stored number added without the rule fails here.
/// </summary>
public class FiniteNumberTests
{
    /// <summary>Every record type, and the stored numbers each holds.</summary>
    private static readonly Dictionary<Type, string[]> StoredNumbers = new()
    {
        [typeof(PlayCandidate)] = ["Equity", "WinPct", "WinGammonPct", "WinBgPct", "LoseGammonPct", "LoseBgPct"],
        [typeof(CubeDecisionData)] =
        [
            "NoDoubleEquity", "DoubleTakeEquity", "CubelessNoDoubleEquity", "CubelessDoubleTakeEquity",
            "WinPctAfterNoDouble", "GammonPctAfterNoDouble", "BgPctAfterNoDouble",
            "LoseGammonPctAfterNoDouble", "LoseBgPctAfterNoDouble",
            "WinPctAfterDoubleTake", "GammonPctAfterDoubleTake", "BgPctAfterDoubleTake",
            "LoseGammonPctAfterDoubleTake", "LoseBgPctAfterDoubleTake",
            "ProbOfOpponentErrorJustifyingDouble", "UnstatedDoublerActionError", "UnstatedTakerActionError",
        ],
        [typeof(CheckerPlayDecisionData)] = ["UnlistedPlayError"],
        [typeof(PositionData)] = [],
        [typeof(DescriptiveData)] = [],
        [typeof(CheckerPlayDecision)] = [],
        [typeof(CubeDecision)] = [],
        [typeof(BgDecisionData)] = [],
    };

    /// <summary>
    /// A valid instance of <paramref name="type"/> on which each of its stored
    /// numbers can be set: the cube's with no action stated, the checker
    /// play's with no candidate the user's, so the analyser's errors belong.
    /// </summary>
    private static object Valid(Type type) =>
        type == typeof(PlayCandidate) ? TestRecords.Candidate()
        : type == typeof(CubeDecisionData) ? TestRecords.CubeData(userDoublerAction: null, userTakerAction: null)
        : type == typeof(CheckerPlayDecisionData) ? TestRecords.CheckerPlayData(userPlayIndex: null)
        : throw new ArgumentOutOfRangeException(nameof(type), type, "No stored numbers.");

    public static TheoryData<string, string, double> Breaches()
    {
        var data = new TheoryData<string, string, double>();
        foreach (var (type, members) in StoredNumbers)
            foreach (var member in members)
                foreach (double value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
                    data.Add(type.Name, member, value);
        return data;
    }

    private static Type TypeNamed(string name) => StoredNumbers.Keys.Single(type => type.Name == name);

    private static bool IsNumber(Type type) => type == typeof(double) || type == typeof(double?);

    [Fact]
    public void TheStoredNumbers_AreExactlyTheListedOnes_OnEveryRecordType()
    {
        // A stored member is one the wire carries: public, settable, not
        // [JsonIgnore]d (a derived number, such as a loss probability or a
        // cube error, is not stored).
        foreach (var (type, members) in StoredNumbers)
        {
            string[] found = [.. type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => IsNumber(p.PropertyType) && p.SetMethod is not null
                    && p.GetCustomAttribute<JsonIgnoreAttribute>() is null)
                .Select(p => p.Name)
                .Order()];
            Assert.Equal(members.Order(), found);
        }
    }

    [Theory]
    [MemberData(nameof(Breaches))]
    public void ANonFiniteNumber_IsRefusedFromCode_NamingTheMember(string typeName, string member, double value)
    {
        var instance = Valid(TypeNamed(typeName));
        var property = instance.GetType().GetProperty(member)!;

        var ex = Assert.Throws<TargetInvocationException>(() => property.SetValue(instance, value));
        var fault = Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
        Assert.Equal(member, fault.ParamName);
    }

    [Theory]
    [MemberData(nameof(Breaches))]
    public void ANonFiniteNumber_IsRefusedFromADocument_AsAJsonException_BothPaths(string typeName, string member, double value)
    {
        var type = TypeNamed(typeName);
        var document = JsonNode.Parse(JsonSerializer.Serialize(Valid(type), type, WirePaths.Context))!.AsObject();
        document[member] = Token(value);

        foreach (var options in Lenient())
        {
            var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(document.ToJsonString(), type, options));
            var fault = Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
            Assert.Equal(member, fault.ParamName);
        }
    }

    [Fact]
    public void ANonFiniteNumber_IsRefusedInsideARecord_EachKind_BothPaths()
    {
        var play = WirePaths.Document<BgDecisionData>(TestRecords.CheckerPlay());
        play["Decision"]!["Plays"]![1]!["WinPct"] = "NaN";
        var cube = WirePaths.Document<BgDecisionData>(TestRecords.Cube());
        cube["Decision"]!["DoubleTakeEquity"] = "Infinity";

        foreach (var options in Lenient())
            foreach (var document in new[] { play, cube })
            {
                var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<BgDecisionData>(document.ToJsonString(), options));
                Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
            }
    }

    [Fact]
    public void AFiniteNumber_AndANullWhereOneMayBe_AreKept()
    {
        // Range is not checked: a probability outside [0, 1] is kept as
        // stated, since no bound is measured against real data yet.
        var candidate = TestRecords.Candidate(equity: -3.0, winPct: 1.5, winGammonPct: null);
        var cube = TestRecords.CubeData(doubleTakeEquity: 2.0, winPctAfterNoDouble: -0.25);

        Assert.Equal((-3.0, 1.5, (double?)null), (candidate.Equity, candidate.WinPct, candidate.WinGammonPct));
        Assert.Equal((2.0, -0.25), (cube.DoubleTakeEquity, cube.WinPctAfterNoDouble));
    }

    /// <summary>The reader's options on each path, letting a named non-finite literal reach the guard.</summary>
    private static IEnumerable<JsonSerializerOptions> Lenient() =>
        new IJsonTypeInfoResolver[] { new DefaultJsonTypeInfoResolver(), BgDataTypesJsonContext.Default }
            .Select(resolver => new JsonSerializerOptions
            {
                TypeInfoResolver = resolver,
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            });

    private static string Token(double value) =>
        double.IsNaN(value) ? "NaN" : value > 0 ? "Infinity" : "-Infinity";
}
