using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

public class AnalysisModeTests
{
    // No explicit JsonStringEnumConverter registration: AnalysisMode bundles
    // its own [JsonConverter(typeof(JsonStringEnumConverter))] attribute. The
    // tests rely on the attribute alone so that removing it from the type
    // would fail this suite loudly (rather than silently passing because an
    // option-level registration covered for it).
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
    };

    [Fact]
    public void HasExactlyFourMembers()
    {
        Assert.Equal(4, Enum.GetValues<AnalysisMode>().Length);
    }

    [Fact]
    public void UnknownIsTheZeroValue()
    {
        // Deliberate: unstamped / legacy JSON (including JSON stamped with the
        // retired flat AnalysisDepthClass) deserializes to the default, which
        // must read as "mode not recorded".
        Assert.Equal(0, (int)AnalysisMode.Unknown);
        Assert.Equal(AnalysisMode.Unknown, default(AnalysisMode));
    }

    [Fact]
    public void MembersAreInDeclarationOrder()
    {
        // The UI renders mode choices in declaration order — a reorder is a
        // visible UI change and must be deliberate.
        AnalysisMode[] expected =
        [
            AnalysisMode.Unknown,
            AnalysisMode.Evaluation,
            AnalysisMode.Rollout,
            AnalysisMode.BookRollout,
        ];
        Assert.Equal(expected, Enum.GetValues<AnalysisMode>());
    }

    [Theory]
    [InlineData(AnalysisMode.Unknown, "\"Unknown\"")]
    [InlineData(AnalysisMode.Evaluation, "\"Evaluation\"")]
    [InlineData(AnalysisMode.Rollout, "\"Rollout\"")]
    [InlineData(AnalysisMode.BookRollout, "\"BookRollout\"")]
    public void Serializes_AsString(AnalysisMode mode, string expectedJson)
    {
        var json = JsonSerializer.Serialize(mode, Options);
        Assert.Equal(expectedJson, json);
    }

    [Theory]
    [InlineData(AnalysisMode.Unknown)]
    [InlineData(AnalysisMode.Evaluation)]
    [InlineData(AnalysisMode.Rollout)]
    [InlineData(AnalysisMode.BookRollout)]
    public void RoundTrips_ThroughJson(AnalysisMode mode)
    {
        var json = JsonSerializer.Serialize(mode, Options);
        var restored = JsonSerializer.Deserialize<AnalysisMode>(json, Options);
        Assert.Equal(mode, restored);
    }

    [Fact]
    public void EveryMember_HasANonEmptyDescriptionLabel()
    {
        // Display text is owned here; downstream label readers (e.g.
        // XgFilter_Lib's EnumLabel.ToLabel) throw on a member without
        // [Description]. Exhaustive so a future member can't ship unlabeled.
        foreach (var member in Enum.GetValues<AnalysisMode>())
        {
            var field = typeof(AnalysisMode).GetField(member.ToString())!;
            var attr = field.GetCustomAttribute<DescriptionAttribute>();
            Assert.NotNull(attr);
            Assert.False(string.IsNullOrWhiteSpace(attr.Description),
                $"{member} has an empty [Description]");
        }
    }

    [Theory]
    [InlineData(AnalysisMode.Unknown, "Unknown")]
    [InlineData(AnalysisMode.Evaluation, "Evaluation")]
    [InlineData(AnalysisMode.Rollout, "Rollout")]
    [InlineData(AnalysisMode.BookRollout, "Book rollout")]
    public void DescriptionLabels_MatchDisplayForms(AnalysisMode mode, string expectedLabel)
    {
        var field = typeof(AnalysisMode).GetField(mode.ToString())!;
        var attr = field.GetCustomAttribute<DescriptionAttribute>()!;
        Assert.Equal(expectedLabel, attr.Description);
    }
}
