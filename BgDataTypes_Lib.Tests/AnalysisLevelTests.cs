using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

public class AnalysisLevelTests
{
    // No explicit JsonStringEnumConverter registration: AnalysisLevel bundles
    // its own [JsonConverter(typeof(JsonStringEnumConverter))] attribute. The
    // tests rely on the attribute alone so that removing it from the type
    // would fail this suite loudly (rather than silently passing because an
    // option-level registration covered for it).
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
    };

    [Fact]
    public void HasExactlyElevenMembers()
    {
        Assert.Equal(11, Enum.GetValues<AnalysisLevel>().Length);
    }

    [Fact]
    public void UnknownIsTheZeroValue()
    {
        // Deliberate: unstamped / legacy JSON deserializes to the default,
        // which must read as "level not recorded" — in particular the
        // BookRollout + Unknown graceful-degradation stamp.
        Assert.Equal(0, (int)AnalysisLevel.Unknown);
        Assert.Equal(AnalysisLevel.Unknown, default(AnalysisLevel));
    }

    [Fact]
    public void MembersAreInAscendingRigorOrder()
    {
        // Plies below the XG Roller family; the UI renders level choices in
        // declaration order, so a reorder is a visible UI change and must be
        // deliberate. Ordering is informational, not contractual — filtering
        // uses membership, DepthRank orders.
        AnalysisLevel[] expected =
        [
            AnalysisLevel.Unknown,
            AnalysisLevel.Ply1,
            AnalysisLevel.Ply2,
            AnalysisLevel.Ply3,
            AnalysisLevel.Ply4,
            AnalysisLevel.Ply5,
            AnalysisLevel.Ply6,
            AnalysisLevel.Ply7,
            AnalysisLevel.XgRoller,
            AnalysisLevel.XgRollerPlus,
            AnalysisLevel.XgRollerPlusPlus,
        ];
        Assert.Equal(expected, Enum.GetValues<AnalysisLevel>());
    }

    [Theory]
    [InlineData(AnalysisLevel.Unknown, "\"Unknown\"")]
    [InlineData(AnalysisLevel.Ply1, "\"Ply1\"")]
    [InlineData(AnalysisLevel.Ply3, "\"Ply3\"")]
    [InlineData(AnalysisLevel.Ply7, "\"Ply7\"")]
    [InlineData(AnalysisLevel.XgRoller, "\"XgRoller\"")]
    [InlineData(AnalysisLevel.XgRollerPlusPlus, "\"XgRollerPlusPlus\"")]
    public void Serializes_AsString(AnalysisLevel level, string expectedJson)
    {
        var json = JsonSerializer.Serialize(level, Options);
        Assert.Equal(expectedJson, json);
    }

    [Theory]
    [InlineData(AnalysisLevel.Unknown)]
    [InlineData(AnalysisLevel.Ply1)]
    [InlineData(AnalysisLevel.Ply7)]
    [InlineData(AnalysisLevel.XgRoller)]
    [InlineData(AnalysisLevel.XgRollerPlus)]
    [InlineData(AnalysisLevel.XgRollerPlusPlus)]
    public void RoundTrips_ThroughJson(AnalysisLevel level)
    {
        var json = JsonSerializer.Serialize(level, Options);
        var restored = JsonSerializer.Deserialize<AnalysisLevel>(json, Options);
        Assert.Equal(level, restored);
    }

    [Fact]
    public void EveryMember_HasANonEmptyDescriptionLabel()
    {
        // Display text is owned here; downstream label readers (e.g.
        // XgFilter_Lib's EnumLabel.ToLabel) throw on a member without
        // [Description]. Exhaustive so a future member can't ship unlabeled.
        foreach (var member in Enum.GetValues<AnalysisLevel>())
        {
            var field = typeof(AnalysisLevel).GetField(member.ToString())!;
            var attr = field.GetCustomAttribute<DescriptionAttribute>();
            Assert.NotNull(attr);
            Assert.False(string.IsNullOrWhiteSpace(attr.Description),
                $"{member} has an empty [Description]");
        }
    }

    [Theory]
    [InlineData(AnalysisLevel.Unknown, "Unknown")]
    [InlineData(AnalysisLevel.Ply1, "1-ply")]
    [InlineData(AnalysisLevel.Ply3, "3-ply")]
    [InlineData(AnalysisLevel.Ply7, "7-ply")]
    [InlineData(AnalysisLevel.XgRoller, "XG Roller")]
    [InlineData(AnalysisLevel.XgRollerPlus, "XG Roller+")]
    [InlineData(AnalysisLevel.XgRollerPlusPlus, "XG Roller++")]
    public void DescriptionLabels_MatchDisplayForms(AnalysisLevel level, string expectedLabel)
    {
        var field = typeof(AnalysisLevel).GetField(level.ToString())!;
        var attr = field.GetCustomAttribute<DescriptionAttribute>()!;
        Assert.Equal(expectedLabel, attr.Description);
    }
}
