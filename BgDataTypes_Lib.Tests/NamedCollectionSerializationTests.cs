using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The wire contract of <see cref="NamedCollectionJsonConverter{TValue, TSelf}"/>
/// through the test specialization: the envelope pinned byte-for-byte, the
/// strict-envelope rejections, the tolerant entry body, and the trio. The
/// sample payload's body is <c>{"Label":…,"Weight":…,"Tags":[…]}</c>,
/// pinned in <see cref="IJsonDocumentTests"/>.
/// </summary>
public class NamedCollectionSerializationTests
{
    private const string EmptyBody = """{"Label":"","Weight":0,"Tags":[]}""";
    private const string RichBody = """{"Label":"blitz","Weight":7,"Tags":["a","b"]}""";

    private static SamplePayload Rich() => new()
    {
        Label = "blitz",
        Weight = 7,
        Tags = ["a", "b"],
    };

    // A minimal valid document; tests splice mutations in via replacement.
    private const string Valid =
        """{"schemaVersion":1,"samples":[{"name":"Blitz","payload":{"Label":"","Weight":0,"Tags":[]}}]}""";

    // -----------------------------------------------------------------------
    //  The envelope, pinned against hand-written samples
    // -----------------------------------------------------------------------

    [Fact]
    public void Write_PinsTheEnvelopeByteForByte()
    {
        var collection = SampleCollection.Empty.With("Blitz", Rich());

        Assert.Equal(
            """{"schemaVersion":1,"samples":[{"name":"Blitz","payload":{"Label":"blitz","Weight":7,"Tags":["a","b"]}}]}""",
            collection.ToJson());
    }

    [Fact]
    public void Write_PinsTheEmptyCollectionShape()
    {
        Assert.Equal("""{"schemaVersion":1,"samples":[]}""", SampleCollection.Empty.ToJson());
    }

    [Fact]
    public void Write_OrdersEntriesCanonically_RegardlessOfAddOrder()
    {
        var collection = SampleCollection.Empty
            .With("delta", new SamplePayload())
            .With("Alpha", new SamplePayload());

        Assert.Equal(
            $$"""{"schemaVersion":1,"samples":[{"name":"Alpha","payload":{{EmptyBody}}},{"name":"delta","payload":{{EmptyBody}}}]}""",
            collection.ToJson());
    }

    [Fact]
    public void Write_IsImmuneToConsumerNamingPolicy()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseUpper };
        var collection = SampleCollection.Empty.With("Blitz", new SamplePayload());

        var json = JsonSerializer.Serialize(collection, options);

        Assert.Contains("\"schemaVersion\":1", json);
        Assert.Contains("\"samples\":[", json);
        Assert.Contains("\"name\":\"Blitz\"", json);
        Assert.Contains("\"Label\":", json);   // payload body immune too
    }

    [Fact]
    public void Write_ThroughTheReflectionPath_IsByteIdenticalToToJson()
    {
        // The attribute-bound converter is the wire form under any resolver.
        var collection = SampleCollection.Empty.With("Blitz", Rich());

        Assert.Equal(collection.ToJson(), JsonSerializer.Serialize(collection));
    }

    [Fact]
    public void RoundTrip_ThroughASourceGeneratedContext_UsesTheClosedConverter()
    {
        // Constraint 2 of the leg, proven in-assembly: the context's generated
        // metadata for SampleCollection instantiates SampleCollectionJsonConverter
        // itself — no reflection, no converter factory — and that path is the
        // canonical one byte for byte.
        var options = new JsonSerializerOptions { TypeInfoResolver = SampleJsonContext.Default };
        var collection = SampleCollection.Empty.With("Blitz", Rich());

        var json = JsonSerializer.Serialize(collection, options);
        var restored = JsonSerializer.Deserialize<SampleCollection>(json, options);

        Assert.Equal(collection.ToJson(), json);
        Assert.Equal(["Blitz"], restored!.Names);
        Assert.Equal(RichBody, restored.Get("Blitz").ToJson());
    }

    [Fact]
    public void Converter_CarriesTheSpecializationsWireNames()
    {
        var converter = new SampleCollectionJsonConverter();

        Assert.Equal("samples", converter.EntriesPropertyName);
        Assert.Equal("payload", converter.ValuePropertyName);
    }

    // -----------------------------------------------------------------------
    //  Round trips
    // -----------------------------------------------------------------------

    [Fact]
    public void RoundTrip_PreservesEverything()
    {
        var collection = SampleCollection.Empty
            .With("Blitz", Rich())
            .With("calm", new SamplePayload());

        var restored = SampleCollection.FromJson(collection.ToJson());

        Assert.Equal(["Blitz", "calm"], restored.Names);
        Assert.Equal(RichBody, restored.Get("Blitz").ToJson());
        Assert.Equal(EmptyBody, restored.Get("calm").ToJson());
    }

    [Fact]
    public void RoundTrip_PreservesTheEmptyCollection()
    {
        var restored = SampleCollection.FromJson(SampleCollection.Empty.ToJson());

        Assert.Equal(0, restored.Count);
    }

    // -----------------------------------------------------------------------
    //  Fail-loud envelope reads
    // -----------------------------------------------------------------------

    [Fact]
    public void Read_AcceptsUnsortedEntries_AndRecanonicalizes()
    {
        // Order is presentation, not semantics: a hand-reordered file is not
        // corruption. The read re-canonicalizes.
        var json =
            $$"""{"schemaVersion":1,"samples":[{"name":"delta","payload":{{EmptyBody}}},{"name":"Alpha","payload":{{EmptyBody}}}]}""";

        Assert.Equal(["Alpha", "delta"], SampleCollection.FromJson(json).Names);
    }

    [Fact]
    public void Read_AcceptsPropertiesInAnyOrder()
    {
        // Envelope property order is likewise not semantics — at the top
        // level and inside an entry.
        var json =
            $$"""{"samples":[{"payload":{{RichBody}},"name":"Blitz"}],"schemaVersion":1}""";

        var restored = SampleCollection.FromJson(json);

        Assert.Equal(["Blitz"], restored.Names);
        Assert.Equal(RichBody, restored.Get("Blitz").ToJson());
    }

    [Fact]
    public void Read_RejectsNewerSchemaVersion_WithDistinguishedMessage()
    {
        var json = Valid.Replace("\"schemaVersion\":1", "\"schemaVersion\":2");

        var ex = Assert.Throws<JsonException>(() => SampleCollection.FromJson(json));

        Assert.Contains("newer", ex.Message);
        Assert.Contains("SampleCollection", ex.Message);
    }

    [Fact]
    public void Read_RejectsOlderSchemaVersion()
    {
        var json = Valid.Replace("\"schemaVersion\":1", "\"schemaVersion\":0");

        var ex = Assert.Throws<JsonException>(() => SampleCollection.FromJson(json));

        Assert.Contains("unsupported", ex.Message);
        Assert.DoesNotContain("newer", ex.Message);
    }

    [Theory]
    [InlineData("\"1\"")]
    [InlineData("1.5")]
    [InlineData("null")]
    public void Read_RejectsNonIntegerSchemaVersion(string token)
    {
        var json = Valid.Replace("\"schemaVersion\":1", $"\"schemaVersion\":{token}");

        var ex = Assert.Throws<JsonException>(() => SampleCollection.FromJson(json));

        Assert.Contains("schemaVersion", ex.Message);
    }

    [Fact]
    public void Read_RejectsMissingSchemaVersion()
    {
        var json = Valid.Replace("\"schemaVersion\":1,", "");

        var ex = Assert.Throws<JsonException>(() => SampleCollection.FromJson(json));

        Assert.Contains("schemaVersion", ex.Message);
    }

    [Fact]
    public void Read_RejectsMissingEntries()
    {
        var ex = Assert.Throws<JsonException>(
            () => SampleCollection.FromJson("""{"schemaVersion":1}"""));

        Assert.Contains("samples", ex.Message);
    }

    [Fact]
    public void Read_RejectsEntriesThatAreNotAnArray()
    {
        var ex = Assert.Throws<JsonException>(
            () => SampleCollection.FromJson("""{"schemaVersion":1,"samples":{}}"""));

        Assert.Contains("samples", ex.Message);
    }

    [Fact]
    public void Read_RejectsAnEntryThatIsNotAnObject()
    {
        var ex = Assert.Throws<JsonException>(
            () => SampleCollection.FromJson("""{"schemaVersion":1,"samples":[5]}"""));

        Assert.Contains("samples", ex.Message);
    }

    [Fact]
    public void Read_RejectsUnknownTopLevelProperty_NamingTheSpecialization()
    {
        var json = Valid.Replace("\"schemaVersion\":1", "\"schemaVersion\":1,\"extra\":1");

        var ex = Assert.Throws<JsonException>(() => SampleCollection.FromJson(json));

        Assert.Contains("extra", ex.Message);
        Assert.Contains("SampleCollection", ex.Message);
    }

    [Fact]
    public void Read_RejectsTheGenericSpellingAsUnknown()
    {
        // The wire vocabulary is the specialization's, not the generic's: a
        // document spelled with some other specialization's names is not
        // this document.
        var json = Valid.Replace("\"samples\":", "\"entries\":");

        var ex = Assert.Throws<JsonException>(() => SampleCollection.FromJson(json));

        Assert.Contains("entries", ex.Message);
    }

    [Fact]
    public void Read_RejectsUnknownEntryProperty()
    {
        var json = Valid.Replace("\"name\":\"Blitz\"", "\"name\":\"Blitz\",\"extra\":1");

        var ex = Assert.Throws<JsonException>(() => SampleCollection.FromJson(json));

        Assert.Contains("extra", ex.Message);
    }

    [Fact]
    public void Read_RejectsMissingName()
    {
        var json = $$"""{"schemaVersion":1,"samples":[{"payload":{{EmptyBody}}}]}""";

        var ex = Assert.Throws<JsonException>(() => SampleCollection.FromJson(json));

        Assert.Contains("name", ex.Message);
    }

    [Fact]
    public void Read_RejectsNonStringName()
    {
        var json = Valid.Replace("\"name\":\"Blitz\"", "\"name\":5");

        var ex = Assert.Throws<JsonException>(() => SampleCollection.FromJson(json));

        Assert.Contains("name", ex.Message);
    }

    [Fact]
    public void Read_RejectsMissingValue_NamingTheEntry()
    {
        var ex = Assert.Throws<JsonException>(() => SampleCollection.FromJson(
            """{"schemaVersion":1,"samples":[{"name":"Blitz"}]}"""));

        Assert.Contains("payload", ex.Message);
        Assert.Contains("Blitz", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" Blitz")]
    [InlineData("Blitz ")]
    public void Read_RejectsBlankOrUntrimmedName(string name)
    {
        // The same single-sourced rule as With: the wire enforces exactly the
        // in-memory name invariants.
        var json = Valid.Replace("\"name\":\"Blitz\"", $"\"name\":\"{name}\"");

        Assert.Throws<JsonException>(() => SampleCollection.FromJson(json));
    }

    [Theory]
    [InlineData("Blitz")]   // exact duplicate
    [InlineData("blitz")]   // case-variant — same name per the name rule
    public void Read_RejectsDuplicateNames(string secondName)
    {
        var json =
            $$"""{"schemaVersion":1,"samples":[{"name":"Blitz","payload":{{EmptyBody}}},{"name":"{{secondName}}","payload":{{EmptyBody}}}]}""";

        var ex = Assert.Throws<JsonException>(() => SampleCollection.FromJson(json));

        Assert.Contains("Duplicate", ex.Message);
    }

    [Theory]
    [InlineData("5")]
    [InlineData("[]")]
    [InlineData("\"text\"")]
    public void Read_RejectsANonObjectRoot(string json)
    {
        Assert.Throws<JsonException>(() => SampleCollection.FromJson(json));
    }

    // -----------------------------------------------------------------------
    //  Entry bodies — the payload's parser rules; corruption fails the file
    // -----------------------------------------------------------------------

    [Fact]
    public void Read_ToleratesUnknownPayloadMembers_AsThePayloadDoes()
    {
        // The strict-envelope/tolerant-payload split: whatever the payload's
        // own parser ignores, the envelope ignores — a retired member must
        // never brick a saved collection.
        var json =
            """{"schemaVersion":1,"samples":[{"name":"Blitz","payload":{"Label":"x","Retired":[1,2]}}]}""";

        var restored = SampleCollection.FromJson(json);

        Assert.Equal("x", restored.Get("Blitz").Label);
    }

    [Theory]
    [InlineData("""{"Weight":"seven"}""")]   // contract violation inside the body
    [InlineData("null")]                     // null body
    [InlineData("5")]                        // not an object
    [InlineData("[]")]                       // not an object
    public void Read_BadPayloadBody_FailsTheWholeFile_NamingTheEntry(string body)
    {
        // Corruption, not evolution: a silently-reset entry would stand in for
        // a saved one, which is worse than a loud error.
        var json = $$"""{"schemaVersion":1,"samples":[{"name":"Blitz","payload":{{body}}}]}""";

        var ex = Assert.Throws<JsonException>(() => SampleCollection.FromJson(json));

        Assert.Contains("Blitz", ex.Message);
        Assert.Contains("payload", ex.Message);
    }

    [Fact]
    public void Read_NormalizesEachBody_StoringThePayloadsCanonicalForm()
    {
        // The stored form is what the wire will carry: member order and
        // spacing in the file do not survive into the document.
        var json =
            """{"schemaVersion":1,"samples":[{"name":"Blitz","payload":{ "Tags": ["a","b"], "Weight": 7, "Label": "blitz" }}]}""";

        var restored = SampleCollection.FromJson(json);

        Assert.Equal(RichBody, restored.Get("Blitz").ToJson());
    }

    // -----------------------------------------------------------------------
    //  FromJson / TryFromJson — the collection's own trio
    // -----------------------------------------------------------------------

    [Fact]
    public void FromJson_RejectsNullToken()
    {
        Assert.Throws<ArgumentException>(() => SampleCollection.FromJson("null"));
    }

    [Fact]
    public void FromJson_RejectsNullString()
    {
        Assert.Throws<ArgumentNullException>(() => SampleCollection.FromJson(null!));
    }

    [Fact]
    public void TryFromJson_RestoresAValidCollection()
    {
        var collection = SampleCollection.Empty.With("Blitz", Rich());

        var result = SampleCollection.TryFromJson(collection.ToJson(), out var restored);

        Assert.True(result);
        Assert.Equal(["Blitz"], restored.Names);
        Assert.Equal(RichBody, restored.Get("Blitz").ToJson());
    }

    [Theory]
    [InlineData(null)]                      // file never written
    [InlineData("null")]                    // literal null token
    [InlineData("not json")]                // malformed
    [InlineData("{\"schemaVersion\":2}")]   // contract violation
    public void TryFromJson_FallsBackToEmpty(string? json)
    {
        var result = SampleCollection.TryFromJson(json, out var collection);

        Assert.False(result);
        Assert.Same(SampleCollection.Empty, collection);
    }

    [Fact]
    public void Trio_IsReachableThroughTheInterface()
    {
        // The collection is itself an IJsonDocument: a generic store can
        // treat a named collection and a bare payload alike.
        var collection = SampleCollection.Empty.With("Blitz", Rich());

        var restored = Restore<SampleCollection>(collection.ToJson());

        Assert.Equal(collection.ToJson(), restored.ToJson());
    }

    private static T Restore<T>(string json) where T : IJsonDocument<T> => T.FromJson(json);

    // -----------------------------------------------------------------------
    //  The closed converter's own guards
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(null, "payload", "entriesPropertyName")]
    [InlineData("samples", null, "valuePropertyName")]
    public void Converter_RejectsANullWireName(string? entries, string? value, string expectedParam)
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new GuardProbe(entries!, value!));

        Assert.Equal(expectedParam, ex.ParamName);
    }

    [Theory]
    [InlineData("", "payload", "entriesPropertyName")]
    [InlineData(" samples", "payload", "entriesPropertyName")]
    [InlineData("schemaVersion", "payload", "entriesPropertyName")]   // collides with the envelope
    [InlineData("samples", "", "valuePropertyName")]
    [InlineData("samples", "payload ", "valuePropertyName")]
    [InlineData("samples", "name", "valuePropertyName")]              // collides with the entry envelope
    public void Converter_RejectsABlankUntrimmedOrCollidingWireName(string entries, string value, string expectedParam)
    {
        var ex = Assert.Throws<ArgumentException>(() => new GuardProbe(entries, value));

        Assert.Equal(expectedParam, ex.ParamName);
    }

    /// <summary>A throwaway closure of the converter to reach its guarded constructor.</summary>
    private sealed class GuardProbe(string entries, string value)
        : NamedCollectionJsonConverter<SamplePayload, SampleCollection>(entries, value);
}
