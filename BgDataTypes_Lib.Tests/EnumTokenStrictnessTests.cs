using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Pins the string-token-exact contract of this library's five wire enums
/// (halheinrich/backgammon#164): every reader of a stored or wire token accepts
/// the declared member names and rejects numeric ordinals, so no payload can
/// silently re-couple to member declaration numbering — which AnalysisLevel in
/// particular reserves the right to change. Which bytes the names are stays
/// pinned by each enum's own test file; this file pins which token *kinds* the
/// readers admit.
/// </summary>
public class EnumTokenStrictnessTests
{
    // No explicit converter registration anywhere in this file: each enum
    // bundles its own [JsonConverter(typeof(StrictJsonStringEnumConverter<T>))]
    // attribute, so removing that attribute from a type fails these tests
    // loudly rather than being covered for by an option-level registration.

    /// <summary>
    /// A probe enum with the DEFAULT converter registration — the measurement
    /// that motivates <see cref="StrictJsonStringEnumConverter{TEnum}"/>: the
    /// base converter accepts integer ordinals on read, defined or not.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<DefaultConverterProbe>))]
    private enum DefaultConverterProbe
    {
        Alpha,
        Beta,
    }

    /// <summary>
    /// The measured System.Text.Json default this library tightens away from.
    /// If this pin ever breaks, STJ changed its default and the strict
    /// converter's rationale needs re-measuring.
    /// </summary>
    [Fact]
    public void DefaultConverter_AcceptsOrdinals_TheHazardBeingClosed()
    {
        Assert.Equal(DefaultConverterProbe.Beta, JsonSerializer.Deserialize<DefaultConverterProbe>("1"));
        Assert.Equal((DefaultConverterProbe)99, JsonSerializer.Deserialize<DefaultConverterProbe>("99"));
    }

    /// <summary>
    /// The reader is the inverse of its writer: the declared name deserializes
    /// to its member, while the member's own ordinal — and an undefined one —
    /// are rejected with the <see cref="JsonException"/> every serialization
    /// boundary here funnels malformed payloads into.
    /// </summary>
    private static void AssertStringTokenExact<TEnum>(TEnum member)
        where TEnum : struct, Enum
    {
        Assert.Equal(member, JsonSerializer.Deserialize<TEnum>($"\"{member}\""));

        string ordinal = Convert.ToInt32(member, CultureInfo.InvariantCulture)
            .ToString(CultureInfo.InvariantCulture);
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TEnum>(ordinal));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TEnum>("99"));
    }

    [Fact]
    public void AnalysisLevel_IsStringTokenExact() =>
        AssertStringTokenExact(AnalysisLevel.Ply3Red);

    [Fact]
    public void AnalysisMode_IsStringTokenExact() =>
        AssertStringTokenExact(AnalysisMode.BookRollout);

    [Fact]
    public void CubeAction_IsStringTokenExact() =>
        AssertStringTokenExact(CubeAction.Take);

    [Fact]
    public void CubeClaim_IsStringTokenExact() =>
        AssertStringTokenExact(CubeClaim.TooGood);

    [Fact]
    public void CubeOwner_IsStringTokenExact() =>
        AssertStringTokenExact(CubeOwner.Centered);

    /// <summary>
    /// Every member of every tightened enum round-trips by name, so the
    /// strictness closed here costs no legitimate token. Guards the case where
    /// a future member's name is unreachable through the strict converter.
    /// </summary>
    [Fact]
    public void EveryMember_RoundTripsByName()
    {
        AssertEveryMemberRoundTrips<AnalysisLevel>();
        AssertEveryMemberRoundTrips<AnalysisMode>();
        AssertEveryMemberRoundTrips<CubeAction>();
        AssertEveryMemberRoundTrips<CubeClaim>();
        AssertEveryMemberRoundTrips<CubeOwner>();

        static void AssertEveryMemberRoundTrips<TEnum>()
            where TEnum : struct, Enum
        {
            foreach (TEnum member in Enum.GetValues<TEnum>())
            {
                string json = JsonSerializer.Serialize(member);
                Assert.Equal($"\"{member}\"", json);
                Assert.Equal(member, JsonSerializer.Deserialize<TEnum>(json));
            }
        }
    }

    /// <summary>
    /// The rejection through a real containing-object read, not only the bare
    /// enum: an ordinal token in a decision payload fails the whole read.
    /// </summary>
    [Fact]
    public void ContainingObject_OrdinalEnumToken_ThrowsJsonException()
    {
        Assert.Equal(
            CubeOwner.Opponent,
            JsonSerializer.Deserialize<CubeOwnerHolder>("""{"Owner":"Opponent"}""")!.Owner);

        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<CubeOwnerHolder>("""{"Owner":1}"""));
    }

    private sealed record CubeOwnerHolder(CubeOwner Owner);

    /// <summary>
    /// The limit of what a type-level attribute can enforce, measured rather
    /// than assumed: a converter registered on <see cref="JsonSerializerOptions"/>
    /// outranks the type's own <c>[JsonConverter]</c>, so a consumer that
    /// registers a plain <see cref="JsonStringEnumConverter"/> re-opens ordinal
    /// acceptance for these types through *its* reader. That is why the
    /// halheinrich/backgammon#164 sweep could not stop at this library: each
    /// consumer that builds options must tighten its own registration. Pinned
    /// here because this library's contract prose claims it.
    /// </summary>
    [Fact]
    public void OptionsLevelRegistration_OutranksTypeAttribute_SoConsumersMustTightenToo()
    {
        var loose = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() },
        };

        Assert.Equal(CubeOwner.Opponent, JsonSerializer.Deserialize<CubeOwner>("1", loose));
    }

    /// <summary>
    /// The write side of the same strictness: with integer fallback disabled an
    /// undefined value cannot be serialized at all, where the default converter
    /// would have written its number.
    /// </summary>
    [Fact]
    public void StrictConverter_UndefinedValue_CannotBeWritten() =>
        Assert.Throws<JsonException>(() => JsonSerializer.Serialize((AnalysisLevel)99));
}
