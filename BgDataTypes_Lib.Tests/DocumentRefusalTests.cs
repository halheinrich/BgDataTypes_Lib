using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// A construction rule a document breaks is a <see cref="JsonException"/>,
/// whatever type the document is read as — a record as
/// <see cref="BgDecisionData"/> or as its own kind, a category on its own —
/// on both read paths, carrying the init guard's own exception; code that
/// breaks the same rule still gets the guard's exception. The wire rule is
/// stated on <see cref="BgDataTypesJsonContext"/>. Added for the umbrella's
/// verdict on the records leg of halheinrich/backgammon#273, which closed the
/// converter's stated limit: a kind read directly used to surface the guard's
/// <see cref="ArgumentException"/>.
/// </summary>
public class DocumentRefusalTests
{
    /// <summary>
    /// Asserts <paramref name="json"/> read as <paramref name="readAs"/> is
    /// refused with a <see cref="JsonException"/> on both paths — never the
    /// guard's own exception — carrying one that is an <paramref name="inner"/>
    /// when one is named. Where a rule spans two members, which guard fires
    /// depends on the order they are set in, so a case names the base type
    /// both guards share.
    /// </summary>
    private static void AssertRefused(string because, Type readAs, string json, Type? inner)
    {
        foreach (var (path, options) in WirePaths.Both)
        {
            var ex = Record.Exception(() => JsonSerializer.Deserialize(json, readAs, options));
            Assert.True(ex is JsonException,
                $"{because}, read as {readAs.Name} on the {path} path: expected a JsonException, got {ex?.GetType().Name ?? "no exception"}: {ex?.Message}");
            if (inner is not null && ex is not null)
                Assert.True(inner.IsInstanceOfType(ex.InnerException),
                    $"{because}, read as {readAs.Name} on the {path} path: expected an inner {inner.Name}, got {ex.InnerException?.GetType().Name ?? "none"}");
        }
    }

    private static JsonObject DocumentOf(BgDecisionData record) => WirePaths.Document<BgDecisionData>(record);

    // ── A record's rules, read as the record and as its own kind ──

    public static IEnumerable<(string Because, BgDecisionData Valid, Action<JsonObject> Break, Type Inner)> RecordBreaches()
    {
        yield return ("a cube decision in the Crawford game", TestRecords.Cube(),
            d => d["Position"]!["IsCrawford"] = true, typeof(ArgumentException));
        yield return ("an identifier naming the other kind", TestRecords.Cube(),
            d => d["Id"] = "match.xg:g1:m2:play", typeof(ArgumentException));
        yield return ("a standalone position stating its start", TestRecords.Cube(),
            d => d["Id"] = "p.xgp", typeof(ArgumentException));
        yield return ("a decision in a game stating no start", TestRecords.CheckerPlay(),
            d => d["Descriptive"]!.AsObject().Remove("IsStandardStart"), typeof(ArgumentException));
        yield return ("a candidate invalid from the position", TestRecords.CheckerPlay(),
            d => d["Decision"]!["Plays"]![1]!["Play"] = JsonNode.Parse("""[{"FrPt":24,"ToPt":12}]"""),
            typeof(ArgumentException));
        yield return ("a cube category's half-guard, inside a record", TestRecords.Cube(),
            d => d["Decision"]!["UserDoublerAction"] = "Take", typeof(ArgumentOutOfRangeException));
        yield return ("a checker category's roll of three dice, inside a record", TestRecords.CheckerPlay(),
            d => d["Decision"]!["Dice"] = JsonNode.Parse("[3,1,2]"), typeof(ArgumentException));
        yield return ("a checker category's user play past its candidates, inside a record", TestRecords.CheckerPlay(),
            d => d["Decision"]!["UserPlayIndex"] = 3, typeof(ArgumentException));
    }

    [Fact]
    public void ARecordBreakingARule_IsRefusedAsAJsonException_AsTheBaseAndAsItsKind_BothPaths()
    {
        foreach (var (because, valid, breach, inner) in RecordBreaches())
        {
            var document = DocumentOf(valid);
            breach(document);
            string json = document.ToJsonString();

            AssertRefused(because, typeof(BgDecisionData), json, inner);
            AssertRefused(because, valid.GetType(), json, inner);
        }
    }

    // ── A category's rules, read as the category ─────────────────

    public static IEnumerable<(string Because, Type ReadAs, object Valid, Action<JsonObject> Break, Type Inner)> CategoryBreaches()
    {
        yield return ("a roll of three dice", typeof(CheckerPlayDecisionData), TestRecords.CheckerPlayData(),
            d => d["Dice"] = JsonNode.Parse("[3,1,2]"), typeof(ArgumentException));
        yield return ("a die face of 7", typeof(CheckerPlayDecisionData), TestRecords.CheckerPlayData(),
            d => d["Dice"] = JsonNode.Parse("[7,1]"), typeof(ArgumentOutOfRangeException));
        yield return ("no candidates", typeof(CheckerPlayDecisionData), TestRecords.CheckerPlayData(),
            d => d["Plays"] = new JsonArray(), typeof(ArgumentException));
        yield return ("a null candidate", typeof(CheckerPlayDecisionData), TestRecords.CheckerPlayData(),
            d => d["Plays"]![0] = null, typeof(ArgumentException));
        yield return ("a user play and an unlisted play's error both stated", typeof(CheckerPlayDecisionData), TestRecords.CheckerPlayData(),
            d => d["UnlistedPlayError"] = 0.1, typeof(ArgumentException));
        yield return ("a doubler action and its unstated-action error both stated", typeof(CubeDecisionData), TestRecords.CubeData(),
            d => d["UnstatedDoublerActionError"] = 0.1, typeof(ArgumentException));
        yield return ("a taker action and its unstated-action error both stated", typeof(CubeDecisionData), TestRecords.CubeData(),
            d => d["UnstatedTakerActionError"] = 0.1, typeof(ArgumentException));
        yield return ("a negative user play", typeof(CheckerPlayDecisionData), TestRecords.CheckerPlayData(),
            d => d["UserPlayIndex"] = -1, typeof(ArgumentOutOfRangeException));
        yield return ("a doubler half holding a taker action", typeof(CubeDecisionData), TestRecords.CubeData(),
            d => d["UserDoublerAction"] = "Take", typeof(ArgumentOutOfRangeException));
        yield return ("a taker half holding a doubler action", typeof(CubeDecisionData), TestRecords.CubeData(),
            d => d["UserTakerAction"] = "Double", typeof(ArgumentOutOfRangeException));
    }

    [Fact]
    public void ACategoryBreakingARule_IsRefusedAsAJsonException_ReadOnItsOwn_BothPaths()
    {
        foreach (var (because, readAs, valid, breach, inner) in CategoryBreaches())
        {
            var document = JsonNode.Parse(JsonSerializer.Serialize(valid, readAs, WirePaths.Context))!.AsObject();
            breach(document);

            AssertRefused(because, readAs, document.ToJsonString(), inner);
        }
    }

    [Fact]
    public void TheSameBreach_FromCode_IsStillTheGuardsOwnException()
    {
        // The document's refusal is the guard's exception rewrapped; code
        // building the same value gets the guard's exception itself.
        Assert.Throws<ArgumentException>(() => TestRecords.Cube(position: TestRecords.Position(isCrawford: true)));
        Assert.Throws<ArgumentOutOfRangeException>(() => TestRecords.CubeData(userDoublerAction: CubeAction.Take));
        Assert.Throws<ArgumentOutOfRangeException>(() => TestRecords.CheckerPlayData(dice: [7, 1]));
        Assert.Throws<ArgumentException>(() => TestRecords.CheckerPlay(
            decision: TestRecords.CheckerPlayData(plays: [TestRecords.Candidate(play: [new(24, 12)])])));
    }

    [Fact]
    public void ACandidatesEquityThatIsNotANumber_IsRefusedAsAJsonException_WhereTheOptionsLetOneIn()
    {
        // A NaN reaches the candidate's guard only where the caller's options
        // read named floating-point literals; the default refuses the token
        // itself. Either way it is a JsonException, and where it reaches the
        // guard it carries the guard's exception.
        var document = JsonNode.Parse(JsonSerializer.Serialize(TestRecords.Candidate(), WirePaths.Context))!.AsObject();
        document["Equity"] = "NaN";
        string json = document.ToJsonString();

        foreach (var resolver in new System.Text.Json.Serialization.Metadata.IJsonTypeInfoResolver[]
                 { new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver(), BgDataTypesJsonContext.Default })
        {
            var lenient = new JsonSerializerOptions
            {
                TypeInfoResolver = resolver,
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            };
            var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PlayCandidate>(json, lenient));
            Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PlayCandidate>(
                json, new JsonSerializerOptions { TypeInfoResolver = resolver }));
        }
    }

    // ── A play's own rule ────────────────────────────────────────

    [Fact]
    public void APlayOfFiveMoves_IsRefusedAsAJsonException_OnItsOwnAndInsideARecord_BothPaths()
    {
        // Play.Add's InvalidOperationException used to escape its converter.
        const string five = """[{"FrPt":6,"ToPt":5},{"FrPt":6,"ToPt":5},{"FrPt":6,"ToPt":5},{"FrPt":6,"ToPt":5},{"FrPt":6,"ToPt":5}]""";
        AssertRefused("a play of five moves", typeof(Play), five, inner: null);

        var document = DocumentOf(TestRecords.CheckerPlay());
        document["Decision"]!["Plays"]![1]!["Play"] = JsonNode.Parse(five);
        AssertRefused("a candidate of five moves", typeof(BgDecisionData), document.ToJsonString(), inner: null);
        AssertRefused("a candidate of five moves", typeof(CheckerPlayDecision), document.ToJsonString(), inner: null);
    }

    // ── How a type knows it is being read ────────────────────────

    [Theory]
    [InlineData(typeof(CheckerPlayDecision))]
    [InlineData(typeof(CubeDecision))]
    [InlineData(typeof(CheckerPlayDecisionData))]
    [InlineData(typeof(CubeDecisionData))]
    [InlineData(typeof(PlayCandidate))]
    [InlineData(typeof(DescriptiveData))]
    public void TheSerializersConstructor_IsInternal_AndCodesIsPublicAndParameterless(Type type)
    {
        // The read mode is set only by the constructor the serializer uses,
        // which code outside the library cannot call.
        var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var serializers = constructors.Where(c => c.IsDefined(typeof(JsonConstructorAttribute))).ToArray();

        var serializer = Assert.Single(serializers);
        Assert.True(serializer.IsAssembly, $"{type.Name}'s serializer constructor is not internal");
        Assert.Single(serializer.GetParameters());

        var code = Assert.Single(constructors, c => c.IsPublic);
        Assert.Empty(code.GetParameters());
    }
}
