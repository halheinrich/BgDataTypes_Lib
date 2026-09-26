using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The depth of an analysis as typed facts (the umbrella's second verdict on
/// the records leg of halheinrich/backgammon#273): a record stores the mode,
/// the level, the rollout trial count, the book edition and an unrecognized
/// level's raw code, and derives the label and the abbreviation from them —
/// with the producer's grammar, moved here unchanged — as it derives the
/// rank. The facts keep their rules, from code and from a document alike.
/// </summary>
public class DepthFactsTests
{
    /// <summary>
    /// Every shape the producer writes, with the label and abbreviation its
    /// ResolveDepthInfo gave it, now derived here.
    /// </summary>
    public static TheoryData<AnalysisMode, AnalysisLevel, int?, BookEdition?, int?, string?, string?> Shapes => new()
    {
        // An evaluation at each kind of level.
        { AnalysisMode.Evaluation, AnalysisLevel.Ply1, null, null, null, "1-ply", "1-ply" },
        { AnalysisMode.Evaluation, AnalysisLevel.Ply3Red, null, null, null, "3-ply Red", "3-ply Red" },
        { AnalysisMode.Evaluation, AnalysisLevel.Ply7, null, null, null, "7-ply", "7-ply" },
        { AnalysisMode.Evaluation, AnalysisLevel.XgRoller, null, null, null, "XG Roller", "R" },
        { AnalysisMode.Evaluation, AnalysisLevel.XgRollerPlus, null, null, null, "XG Roller+", "R+" },
        { AnalysisMode.Evaluation, AnalysisLevel.XgRollerPlusPlus, null, null, null, "XG Roller++", "R++" },
        // A book hit, bare and enriched.
        { AnalysisMode.BookRollout, AnalysisLevel.Unknown, null, BookEdition.V1, null, "Book V1", "Book" },
        { AnalysisMode.BookRollout, AnalysisLevel.Unknown, null, BookEdition.V2, null, "Book V2", "Book" },
        { AnalysisMode.BookRollout, AnalysisLevel.Ply4, 12960, BookEdition.V2, null, "Book V2: 12960 trials. 4-ply", "B4_12960" },
        { AnalysisMode.BookRollout, AnalysisLevel.Ply3Red, 648, BookEdition.V2, null, "Book V2: 648 trials. 3-ply Red", "B3_648" },
        { AnalysisMode.BookRollout, AnalysisLevel.XgRoller, 1296, BookEdition.V2, null, "Book V2: 1296 trials. XG Roller", "BR_1296" },
        { AnalysisMode.BookRollout, AnalysisLevel.Unknown, 1296, BookEdition.V2, 55, "Book V2: 1296 trials. level-55", "Blevel-55_1296" },
        // An explicit rollout, with and without its context.
        { AnalysisMode.Rollout, AnalysisLevel.Unknown, null, null, null, "Rollout", "Ro" },
        { AnalysisMode.Rollout, AnalysisLevel.Ply3, 1296, null, null, "Rollout: 1296 trials. 3-ply", "3p1296" },
        { AnalysisMode.Rollout, AnalysisLevel.XgRollerPlus, 1296, null, null, "Rollout: 1296 trials. XG Roller+", "R+p1296" },
        { AnalysisMode.Rollout, AnalysisLevel.Unknown, 1296, null, 42, "Rollout: 1296 trials. level-42", "level-42p1296" },
        // An unrecognized level, and none recorded.
        { AnalysisMode.Unknown, AnalysisLevel.Unknown, null, null, 7, "level-7", "level-7" },
        { AnalysisMode.Unknown, AnalysisLevel.Unknown, null, null, -100, "level--100", "level--100" },
        { AnalysisMode.Unknown, AnalysisLevel.Unknown, null, null, null, null, null },
    };

    [Theory]
    [MemberData(nameof(Shapes))]
    public void TheLabelAndAbbreviation_AreDerivedFromTheFacts_OnACandidateAndACube(
        AnalysisMode mode, AnalysisLevel level, int? trials, BookEdition? edition, int? code, string? label, string? abbreviation)
    {
        var candidate = TestRecords.Candidate(
            analysisMode: mode, analysisLevel: level, rolloutTrials: trials, bookEdition: edition, unrecognizedLevelCode: code);
        var cube = TestRecords.CubeData(
            analysisMode: mode, analysisLevel: level, rolloutTrials: trials, bookEdition: edition, unrecognizedLevelCode: code);

        Assert.Equal(label, candidate.Depth);
        Assert.Equal(abbreviation, candidate.DepthAbbreviation);
        Assert.Equal(label, cube.Depth);
        Assert.Equal(abbreviation, cube.DepthAbbreviation);
    }

    [Fact]
    public void EachLevelsLabel_IsItsDescription()
    {
        // The label table and the enum's UI labels say the same thing once:
        // a test holds them together.
        foreach (var level in Enum.GetValues<AnalysisLevel>().Where(l => l != AnalysisLevel.Unknown))
        {
            string description = typeof(AnalysisLevel).GetField(level.ToString())!
                .GetCustomAttribute<DescriptionAttribute>()!.Description;
            Assert.Equal(description, TestRecords.Candidate(analysisLevel: level).Depth);
        }
    }

    // ── The facts' rules ─────────────────────────────────────────

    public static IEnumerable<(string Because, string Member, Func<object> Build)> Breaches()
    {
        yield return ("a trial count of 0", "RolloutTrials", () => TestRecords.Candidate(
            analysisMode: AnalysisMode.Rollout, rolloutTrials: 0));
        yield return ("a trial count on an evaluation", "RolloutTrials", () => TestRecords.Candidate(rolloutTrials: 1296));
        yield return ("an edition on a rollout", "BookEdition", () => TestRecords.Candidate(
            analysisMode: AnalysisMode.Rollout, bookEdition: BookEdition.V2));
        yield return ("a raw code for a recognized level", "UnrecognizedLevelCode", () => TestRecords.Candidate(
            unrecognizedLevelCode: 7));
        yield return ("a trial count on a cube evaluation", "RolloutTrials", () => TestRecords.CubeData(rolloutTrials: 1296));
        yield return ("an edition on a cube evaluation", "BookEdition", () => TestRecords.CubeData(bookEdition: BookEdition.V1));
    }

    [Fact]
    public void AFactThatDoesNotBelong_IsRefusedFromCode_NamingTheLaterMember()
    {
        // The builders state the mode and level first, so the fact's own
        // guard refuses.
        foreach (var (because, member, build) in Breaches())
        {
            var ex = Assert.Throws<ArgumentException>(build);
            Assert.True(member == ex.ParamName, $"{because}: refused naming {ex.ParamName}");
        }
    }

    [Fact]
    public void AFactStatedFirst_IsRefusedByTheModeOrLevelSetSecond()
    {
        var byMode = Assert.Throws<ArgumentException>(() => new PlayCandidate
        {
            Play = [], RolloutTrials = 1296, AnalysisMode = AnalysisMode.Evaluation,
            AnalysisLevel = AnalysisLevel.Ply3, Equity = 0,
        });
        var byLevel = Assert.Throws<ArgumentException>(() => new PlayCandidate
        {
            Play = [], AnalysisMode = AnalysisMode.Unknown, UnrecognizedLevelCode = 7,
            AnalysisLevel = AnalysisLevel.Ply3, Equity = 0,
        });

        Assert.Equal("AnalysisMode", byMode.ParamName);
        Assert.Equal("AnalysisLevel", byLevel.ParamName);
    }

    [Theory]
    [InlineData("RolloutTrials", 1296)]
    [InlineData("RolloutTrials", 0)]
    [InlineData("UnrecognizedLevelCode", 7)]
    public void AFactThatDoesNotBelong_IsRefusedFromADocument_OnACandidateAndACube_BothPaths(string member, int value)
    {
        var candidate = WirePaths.Document(TestRecords.Candidate());
        var cube = WirePaths.Document(TestRecords.CubeData());
        candidate[member] = value;
        cube[member] = value;

        Assert.IsAssignableFrom<ArgumentException>(WirePaths.AssertRefused<PlayCandidate>(candidate.ToJsonString()).InnerException);
        Assert.IsAssignableFrom<ArgumentException>(WirePaths.AssertRefused<CubeDecisionData>(cube.ToJsonString()).InnerException);
    }

    // ── On the wire ──────────────────────────────────────────────

    [Fact]
    public void TheFactsRoundTrip_AndTheTextIsNotWritten_BothPaths()
    {
        var candidate = TestRecords.Candidate(
            analysisMode: AnalysisMode.BookRollout, analysisLevel: AnalysisLevel.Unknown,
            rolloutTrials: 1296, bookEdition: BookEdition.V2, unrecognizedLevelCode: 55);

        foreach (var (_, options) in WirePaths.Both)
        {
            string json = JsonSerializer.Serialize(candidate, options);
            var read = JsonSerializer.Deserialize<PlayCandidate>(json, options)!;

            Assert.Contains("\"RolloutTrials\":1296,\"BookEdition\":\"V2\",\"UnrecognizedLevelCode\":55", json);
            Assert.DoesNotContain("\"Depth\"", json);
            Assert.DoesNotContain("DepthAbbreviation", json);
            Assert.Equal(candidate.Depth, read.Depth);
            Assert.Equal(candidate.DepthAbbreviation, read.DepthAbbreviation);
        }
    }

    [Fact]
    public void AStatedLabelOrAbbreviation_IsIgnored_TheDerivationStands_BothPaths()
    {
        var candidate = WirePaths.Document(TestRecords.Candidate());
        var cube = WirePaths.Document(TestRecords.CubeData());
        foreach (var document in new[] { candidate, cube })
        {
            document["Depth"] = "Rollout: 9 trials. 7-ply";
            document["DepthAbbreviation"] = "7p9";
        }

        foreach (var (_, options) in WirePaths.Both)
        {
            var readCandidate = JsonSerializer.Deserialize<PlayCandidate>(candidate.ToJsonString(), options)!;
            var readCube = JsonSerializer.Deserialize<CubeDecisionData>(cube.ToJsonString(), options)!;
            Assert.Equal(("3-ply", "3-ply"), (readCandidate.Depth, readCandidate.DepthAbbreviation));
            Assert.Equal(("3-ply", "3-ply"), (readCube.Depth, readCube.DepthAbbreviation));
        }
    }
}
