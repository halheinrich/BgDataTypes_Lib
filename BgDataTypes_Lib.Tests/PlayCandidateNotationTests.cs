using System.Text.Json;
using System.Text.Json.Nodes;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// A candidate's notation is derived from its play and never stored
/// (halheinrich/backgammon#273): <see cref="PlayCandidate.Notation"/> is the
/// play's own <see cref="Play.ToNotation"/>; it is not on the wire, and a
/// document still carrying the retired <c>MoveNotation</c> member reads on
/// both paths with that member ignored.
/// The full-record bytes are pinned in <see cref="WireGoldenTests"/>.
/// </summary>
public class PlayCandidateNotationTests
{
    private static readonly JsonSerializerOptions ReflectionOptions = new();

    private static readonly JsonSerializerOptions ContextOptions = new()
    {
        TypeInfoResolver = BgDataTypesJsonContext.Default
    };

    private static readonly (string Path, JsonSerializerOptions Options)[] BothPaths =
        [("reflection", ReflectionOptions), ("context", ContextOptions)];

    /// <summary>
    /// The tester's position of halheinrich/backgammon#273, in the mover's
    /// frame: the mover has 8(2) and 7(1), the opponent a blot on the 3-point.
    /// Dice 5-4.
    /// </summary>
    private static readonly BoardPosition TesterMop = new(
        [-1, 0, 2, -1, 2, 2, 4, 1, 2, -1, 0, 1, -2, 0, 0, 0, -2, 0, 0, -2, -2, 1, -2, 0, -2, 0]);

    // ── The derived notation ──────────────────────────────────────

    public static TheoryData<string, Play, string> Cases => new()
    {
        { "hit", [new(24, -18), new(13, 9)], "24/18* 13/9" },
        { "bear-off", [new(6, 0), new(4, 1), new(1, 0)], "6/off 4/off" },
        { "bar entry", [new(25, 22), new(22, 16)], "bar/16" },
        { "bar entry with a hit", [new(25, -22), new(13, 9)], "bar/22* 13/9" },
        { "doubles group", [new(8, 5), new(8, 5), new(6, 3), new(6, 3)], "8/5(2) 6/3(2)" },
        { "doubles group with a hit", [new(6, -2), new(6, 2), new(6, 2), new(6, 2)], "6/2(4)*" },
        { "pass", [], "" },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Notation_IsThePlaysNotation(string name, Play play, string expected)
    {
        var candidate = TestRecords.Candidate(play: play);

        Assert.True(expected == candidate.Notation, $"{name}: '{candidate.Notation}'");
        Assert.Equal(play.ToNotation(), candidate.Notation);
    }

    [Fact]
    public void Notation_TesterCase_EitherAttribution_ReadsAsTheCarrierRuleWritesIt()
    {
        // 5-4 making the 3-point on the blot. The converted match stored the
        // mark on the 7-point checker and the text "8/3* 7/3"; the generator
        // marks the 8-point checker. One blot, hit once: both candidates read
        // alike, the mark on the carrier 8/3 (07b1395's carrier rule), and the
        // text can no longer disagree with the play it describes.
        var record = TestRecords.CheckerPlay(
            position: TestRecords.Position(mop: TesterMop),
            decision: TestRecords.CheckerPlayData(
                dice: [5, 4],
                plays:
                [
                    TestRecords.Candidate(play: [new(7, -3), new(8, 3)]),
                    TestRecords.Candidate(play: [new(8, -3), new(7, 3)]),
                ]));

        Assert.Equal("8/3* 7/3", record.Decision.Plays[0].Notation);
        Assert.Equal("8/3* 7/3", record.Decision.Plays[1].Notation);

        // And they are the one play from the decision's position.
        Assert.True(new BoardState(TesterMop).IsSamePlay(
            record.Decision.Plays[0].Play, record.Decision.Plays[1].Play));
    }

    // ── The wire ──────────────────────────────────────────────────

    [Fact]
    public void Notation_IsNeverWritten_BothPaths()
    {
        // The candidate's wire members are exactly its stored ones: the
        // derived notation is not written, and neither is the retired name.
        // Rewritten: the depth rank and the equity loss are derived now (from
        // the mode and level, and on the decision), so they left the list; so
        // did the loss probability, 1 − the win probability, and the depth
        // label and abbreviation, derived from the typed depth facts that
        // joined the list.
        var candidate = TestRecords.Candidate(
            play: [new(24, -18), new(13, 9)], winPct: 0.5, winGammonPct: 0.1, winBgPct: 0.01,
            loseGammonPct: 0.1, loseBgPct: 0.01);

        foreach (var (path, options) in BothPaths)
        {
            var json = JsonSerializer.Serialize(candidate, options);
            var names = JsonNode.Parse(json)!.AsObject().Select(p => p.Key).ToArray();

            Assert.True(
                names.SequenceEqual(
                [
                    "Play", "AnalysisMode", "AnalysisLevel",
                    "RolloutTrials", "BookEdition", "UnrecognizedLevelCode",
                    "Equity", "WinPct", "WinGammonPct", "WinBgPct",
                    "LoseGammonPct", "LoseBgPct",
                ]),
                $"{path}: {string.Join(",", names)}");
            Assert.DoesNotContain("Notation", json);
        }
    }

    [Fact]
    public void LegacyMoveNotation_ReadsOnBothPaths_TheRetiredPropertyIgnored()
    {
        // A candidate written before the member was retired still carries
        // it. It reads, the member ignored like any retired property: the
        // notation is the play's, even where the stored text disagreed with
        // the play, as it did in the converted match of
        // halheinrich/backgammon#273. Written back, the member is gone.
        var candidate = TestRecords.Candidate(play: [new(7, -3), new(8, 3)]);

        foreach (var (path, options) in BothPaths)
        {
            var document = JsonNode.Parse(JsonSerializer.Serialize(candidate, options))!.AsObject();
            document.Insert(0, "MoveNotation", "8/3 7/3*");
            var legacy = document.ToJsonString();

            var restored = JsonSerializer.Deserialize<PlayCandidate>(legacy, options)!;

            Assert.True("8/3* 7/3" == restored.Notation, $"{path}: '{restored.Notation}'");
            Assert.True(restored.Play.IsSameEncoding(candidate.Play), path);
            Assert.Equal("3-ply", restored.Depth);
            Assert.Equal(JsonSerializer.Serialize(candidate, options), JsonSerializer.Serialize(restored, options));
        }
    }
}
