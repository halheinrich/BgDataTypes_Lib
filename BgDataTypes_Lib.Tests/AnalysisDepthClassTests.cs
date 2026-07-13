using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

public class AnalysisDepthClassTests
{
    // No explicit JsonStringEnumConverter registration: AnalysisDepthClass
    // bundles its own [JsonConverter(typeof(JsonStringEnumConverter))]
    // attribute. The tests rely on the attribute alone so that removing it
    // from the type would fail this suite loudly (rather than silently
    // passing because an option-level registration covered for it).
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
    };

    [Fact]
    public void HasExactlyThirteenMembers()
    {
        Assert.Equal(13, Enum.GetValues<AnalysisDepthClass>().Length);
    }

    [Fact]
    public void UnknownIsTheZeroValue()
    {
        // Deliberate: unstamped / legacy JSON deserializes to the default,
        // which must read as "depth not recorded".
        Assert.Equal(0, (int)AnalysisDepthClass.Unknown);
        Assert.Equal(AnalysisDepthClass.Unknown, default(AnalysisDepthClass));
    }

    [Fact]
    public void MembersAreInAscendingRigorOrder()
    {
        // Mirrors the producer's rank ordering (Book/unknown 0, N-ply 1-7,
        // XG Roller family 20-22, rollouts 100+). Informational, not
        // contractual — filtering uses membership, DepthRank orders.
        AnalysisDepthClass[] expected =
        [
            AnalysisDepthClass.Unknown,
            AnalysisDepthClass.Book,
            AnalysisDepthClass.Ply1,
            AnalysisDepthClass.Ply2,
            AnalysisDepthClass.Ply3,
            AnalysisDepthClass.Ply4,
            AnalysisDepthClass.Ply5,
            AnalysisDepthClass.Ply6,
            AnalysisDepthClass.Ply7,
            AnalysisDepthClass.XgRoller,
            AnalysisDepthClass.XgRollerPlus,
            AnalysisDepthClass.XgRollerPlusPlus,
            AnalysisDepthClass.Rollout,
        ];
        Assert.Equal(expected, Enum.GetValues<AnalysisDepthClass>());
    }

    [Theory]
    [InlineData(AnalysisDepthClass.Unknown, "\"Unknown\"")]
    [InlineData(AnalysisDepthClass.Book, "\"Book\"")]
    [InlineData(AnalysisDepthClass.Ply3, "\"Ply3\"")]
    [InlineData(AnalysisDepthClass.XgRollerPlusPlus, "\"XgRollerPlusPlus\"")]
    [InlineData(AnalysisDepthClass.Rollout, "\"Rollout\"")]
    public void Serializes_AsString(AnalysisDepthClass depthClass, string expectedJson)
    {
        var json = JsonSerializer.Serialize(depthClass, Options);
        Assert.Equal(expectedJson, json);
    }

    [Theory]
    [InlineData(AnalysisDepthClass.Unknown)]
    [InlineData(AnalysisDepthClass.Book)]
    [InlineData(AnalysisDepthClass.Ply1)]
    [InlineData(AnalysisDepthClass.Ply7)]
    [InlineData(AnalysisDepthClass.XgRoller)]
    [InlineData(AnalysisDepthClass.Rollout)]
    public void RoundTrips_ThroughJson(AnalysisDepthClass depthClass)
    {
        var json = JsonSerializer.Serialize(depthClass, Options);
        var restored = JsonSerializer.Deserialize<AnalysisDepthClass>(json, Options);
        Assert.Equal(depthClass, restored);
    }

    [Fact]
    public void EveryMember_HasANonEmptyDescriptionLabel()
    {
        // Display text is owned here; downstream label readers (e.g.
        // XgFilter_Lib's EnumLabel.ToLabel) throw on a member without
        // [Description]. Exhaustive so a future member can't ship unlabeled.
        foreach (var member in Enum.GetValues<AnalysisDepthClass>())
        {
            var field = typeof(AnalysisDepthClass).GetField(member.ToString())!;
            var attr = field.GetCustomAttribute<DescriptionAttribute>();
            Assert.NotNull(attr);
            Assert.False(string.IsNullOrWhiteSpace(attr.Description),
                $"{member} has an empty [Description]");
        }
    }

    [Theory]
    [InlineData(AnalysisDepthClass.Ply3, "3-ply")]
    [InlineData(AnalysisDepthClass.XgRollerPlus, "XG Roller+")]
    [InlineData(AnalysisDepthClass.XgRollerPlusPlus, "XG Roller++")]
    [InlineData(AnalysisDepthClass.Rollout, "Rollout")]
    public void DescriptionLabels_MatchDisplayForms(AnalysisDepthClass depthClass, string expectedLabel)
    {
        var field = typeof(AnalysisDepthClass).GetField(depthClass.ToString())!;
        var attr = field.GetCustomAttribute<DescriptionAttribute>()!;
        Assert.Equal(expectedLabel, attr.Description);
    }
}
