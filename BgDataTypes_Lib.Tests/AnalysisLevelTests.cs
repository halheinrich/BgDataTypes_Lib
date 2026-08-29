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
    public void HasExactlyTwelveMembers()
    {
        Assert.Equal(12, Enum.GetValues<AnalysisLevel>().Length);
    }

    [Fact]
    public void UnknownIsTheZeroValue()
    {
        // Deliberate: unstamped / legacy JSON deserializes to the default,
        // which must read as "level not recorded" — in particular the
        // BookRollout + Unknown graceful-degradation stamp. Head-of-list is
        // the zero-value requirement, not a rigor rank: Unknown sits outside
        // the scale (clause (a) of the 2026-08-28 ruling).
        Assert.Equal(0, (int)AnalysisLevel.Unknown);
        Assert.Equal(AnalysisLevel.Unknown, default(AnalysisLevel));
    }

    [Fact]
    public void MembersAreDeclaredInTheContractualAscendingRigorOrder()
    {
        // The order pin. Declaration order is contractual (ruled 2026-08-28
        // on the authority of XG's own analysis-level menu): every member
        // after Unknown ascends in rigor, and the ply and XG Roller families
        // INTERLEAVE — Ply3, XgRoller, Ply4, XgRollerPlus, Ply5 — rather than
        // forming two blocks. Live consumers read this order (the diagram's
        // level floor; the filter-panel and quiz level dropdowns), so a
        // reorder or an out-of-order insertion is a breaking change and must
        // fail here loudly rather than silently mis-rank downstream.
        AnalysisLevel[] expected =
        [
            AnalysisLevel.Unknown,
            AnalysisLevel.Ply1,
            AnalysisLevel.Ply2,
            AnalysisLevel.Ply3Red,
            AnalysisLevel.Ply3,
            AnalysisLevel.XgRoller,
            AnalysisLevel.Ply4,
            AnalysisLevel.XgRollerPlus,
            AnalysisLevel.Ply5,
            AnalysisLevel.Ply6,
            AnalysisLevel.Ply7,
            AnalysisLevel.XgRollerPlusPlus,
        ];
        Assert.Equal(expected, Enum.GetValues<AnalysisLevel>());
    }

    [Theory]
    [InlineData(AnalysisLevel.Unknown, "\"Unknown\"")]
    [InlineData(AnalysisLevel.Ply1, "\"Ply1\"")]
    [InlineData(AnalysisLevel.Ply3Red, "\"Ply3Red\"")]
    [InlineData(AnalysisLevel.Ply3, "\"Ply3\"")]
    [InlineData(AnalysisLevel.Ply7, "\"Ply7\"")]
    [InlineData(AnalysisLevel.XgRoller, "\"XgRoller\"")]
    [InlineData(AnalysisLevel.XgRollerPlusPlus, "\"XgRollerPlusPlus\"")]
    public void Serializes_AsString(AnalysisLevel level, string expectedJson)
    {
        // Name-based, never number-based: this is what makes the ruled
        // reorder and the Ply3Red insertion wire-safe — renumbering moves no
        // wire value.
        var json = JsonSerializer.Serialize(level, Options);
        Assert.Equal(expectedJson, json);
    }

    [Theory]
    [InlineData(AnalysisLevel.Unknown)]
    [InlineData(AnalysisLevel.Ply1)]
    [InlineData(AnalysisLevel.Ply3Red)]
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
    [InlineData(AnalysisLevel.Ply3Red, "3-ply Red")]
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
