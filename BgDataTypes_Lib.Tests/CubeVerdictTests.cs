using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

public class CubeVerdictTests
{
    // No explicit JsonStringEnumConverter registration: CubeVerdict bundles its
    // own [JsonConverter(typeof(JsonStringEnumConverter))] attribute. The tests
    // rely on the attribute alone so that removing it from the type would fail
    // this suite loudly (rather than silently passing because an option-level
    // registration covered for it).
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
    };

    [Fact]
    public void HasExactlyFourMembers()
    {
        Assert.Equal(4, Enum.GetValues<CubeVerdict>().Length);
    }

    [Fact]
    public void MembersAreInExpectedOrder()
    {
        Assert.Equal(0, (int)CubeVerdict.NoDouble);
        Assert.Equal(1, (int)CubeVerdict.DoubleTake);
        Assert.Equal(2, (int)CubeVerdict.DoublePass);
        Assert.Equal(3, (int)CubeVerdict.TooGood);
    }

    [Theory]
    [InlineData(CubeVerdict.NoDouble,   "\"NoDouble\"")]
    [InlineData(CubeVerdict.DoubleTake, "\"DoubleTake\"")]
    [InlineData(CubeVerdict.DoublePass, "\"DoublePass\"")]
    [InlineData(CubeVerdict.TooGood,    "\"TooGood\"")]
    public void Serializes_AsString(CubeVerdict verdict, string expectedJson)
    {
        var json = JsonSerializer.Serialize(verdict, Options);
        Assert.Equal(expectedJson, json);
    }

    [Theory]
    [InlineData(CubeVerdict.NoDouble)]
    [InlineData(CubeVerdict.DoubleTake)]
    [InlineData(CubeVerdict.DoublePass)]
    [InlineData(CubeVerdict.TooGood)]
    public void RoundTrips_ThroughJson(CubeVerdict verdict)
    {
        var json = JsonSerializer.Serialize(verdict, Options);
        var restored = JsonSerializer.Deserialize<CubeVerdict>(json, Options);
        Assert.Equal(verdict, restored);
    }
}
