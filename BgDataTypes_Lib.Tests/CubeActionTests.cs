using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

public class CubeActionTests
{
    // No explicit JsonStringEnumConverter registration: CubeAction bundles its
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
        Assert.Equal(4, Enum.GetValues<CubeAction>().Length);
    }

    [Fact]
    public void MembersAreInExpectedOrder()
    {
        Assert.Equal(0, (int)CubeAction.NoDouble);
        Assert.Equal(1, (int)CubeAction.Double);
        Assert.Equal(2, (int)CubeAction.Take);
        Assert.Equal(3, (int)CubeAction.Pass);
    }

    [Theory]
    [InlineData(CubeAction.NoDouble, "\"NoDouble\"")]
    [InlineData(CubeAction.Double, "\"Double\"")]
    [InlineData(CubeAction.Take, "\"Take\"")]
    [InlineData(CubeAction.Pass, "\"Pass\"")]
    public void Serializes_AsString(CubeAction action, string expectedJson)
    {
        var json = JsonSerializer.Serialize(action, Options);
        Assert.Equal(expectedJson, json);
    }

    [Theory]
    [InlineData(CubeAction.NoDouble)]
    [InlineData(CubeAction.Double)]
    [InlineData(CubeAction.Take)]
    [InlineData(CubeAction.Pass)]
    public void RoundTrips_ThroughJson(CubeAction action)
    {
        var json = JsonSerializer.Serialize(action, Options);
        var restored = JsonSerializer.Deserialize<CubeAction>(json, Options);
        Assert.Equal(action, restored);
    }
}
