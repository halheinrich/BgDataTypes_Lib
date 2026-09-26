using System.Text.Json;
using System.Text.Json.Nodes;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// One spelling for "none recorded": <see langword="null"/>, for text as for
/// numbers (the umbrella's verdict on the records leg of
/// halheinrich/backgammon#273). A text member states text or, where it may,
/// nothing — empty or white-space text is neither, and is refused: by code
/// with an <see cref="ArgumentException"/> naming the member, by a document
/// with a <see cref="JsonException"/> on both paths, whatever it is read as.
/// A member that is never none (the XGID, the id's file name) is refused
/// empty the same way. The depth rank has no number for "not recorded": it is
/// <see langword="null"/> (DerivedValuesTests pins the grid). The analysis
/// enums keep their <c>Unknown</c> member, which a producer states.
/// </summary>
public class NoneRecordedTests
{
    public static TheoryData<string> Blanks => ["", " ", "\t\n"];

    // ── From code ─────────────────────────────────────────────────

    public static IEnumerable<(string Member, Action<string> Build)> TextMembers()
    {
        yield return ("OnRollName", text => TestRecords.Descriptive(onRollName: text));
        yield return ("OpponentName", text => TestRecords.Descriptive(opponentName: text));
        yield return ("Title", text => TestRecords.Descriptive(title: text));
        yield return ("Event", text => TestRecords.Descriptive(@event: text));
        yield return ("Comment", text => TestRecords.Descriptive(comment: text));
        // The record's XGID left this list when it became derived
        // (halheinrich/backgammon#273): code cannot state one, empty or not;
        // the row's column refuses empty text on read (DecisionRowSerializationTests).
        yield return ("filename", text => new XgpDecisionId(text));
        yield return ("filename", text => new XgDecisionId(text, 1, 1, IsCube: false));
    }

    [Theory]
    [MemberData(nameof(Blanks))]
    public void BlankText_IsRefusedFromCode_NamingTheMember(string blank)
    {
        foreach (var (member, build) in TextMembers())
        {
            var ex = Assert.Throws<ArgumentException>(() => build(blank));
            Assert.Equal(member, ex.ParamName);
        }
    }

    [Fact]
    public void Null_IsNoneRecorded_ForEveryNullableTextMember()
    {
        var descriptive = TestRecords.Descriptive(onRollName: null, opponentName: null, title: null, @event: null, comment: null);
        // The depth label and abbreviation are derived now, from typed facts:
        // no depth recorded (the mode and level unknown) derives no text.
        var candidate = TestRecords.Candidate(analysisMode: AnalysisMode.Unknown, analysisLevel: AnalysisLevel.Unknown);
        var cube = TestRecords.CubeData(analysisMode: AnalysisMode.Unknown, analysisLevel: AnalysisLevel.Unknown);

        Assert.Null(descriptive.OnRollName);
        Assert.Null(descriptive.OpponentName);
        Assert.Null(descriptive.Comment);
        Assert.Null(candidate.Depth);
        Assert.Null(cube.DepthAbbreviation);
        Assert.Null(TestRecords.CheckerPlay(descriptive: descriptive).Player);
    }

    // ── From a document ───────────────────────────────────────────

    /// <summary>A record document with the member at <paramref name="path"/> set to <paramref name="value"/>.</summary>
    private static string RecordWith(BgDecisionData record, string[] path, JsonNode? value)
    {
        var document = WirePaths.Document<BgDecisionData>(record);
        JsonNode owner = document;
        foreach (var step in path[..^1])
            owner = int.TryParse(step, out int i) ? owner[i]! : owner[step]!;
        owner[path[^1]] = value;
        return document.ToJsonString();
    }

    public static IEnumerable<(BgDecisionData Record, string[] Path)> RecordTextMembers()
    {
        var play = TestRecords.CheckerPlay(descriptive: TestRecords.Descriptive(title: "t", @event: "e", comment: "c"));
        foreach (var member in new[] { "OnRollName", "OpponentName", "Title", "Event", "Comment" })
            yield return (play, ["Descriptive", member]);
        // The record's XGID left: it is derived, not stated (halheinrich/backgammon#273).
        yield return (play, ["Id"]);
    }

    [Theory]
    [MemberData(nameof(Blanks))]
    public void BlankText_IsRefusedFromADocument_AsTheRecordAndAsItsKind_BothPaths(string blank)
    {
        foreach (var (record, path) in RecordTextMembers())
        {
            string json = RecordWith(record, path, blank);
            foreach (var readAs in new[] { typeof(BgDecisionData), record.GetType() })
                foreach (var (name, options) in WirePaths.Both)
                {
                    var ex = Record.Exception(() => JsonSerializer.Deserialize(json, readAs, options));
                    Assert.True(ex is JsonException,
                        $"{string.Join('.', path)} = '{blank}', read as {readAs.Name} on the {name} path: {ex?.GetType().Name ?? "loaded"}");
                }
        }
    }

    [Fact]
    public void NullText_IsNoneRecorded_InADocument_AndRoundTrips_BothPaths()
    {
        foreach (var (record, path) in RecordTextMembers())
        {
            if (path[^1] is "Id")
                continue;   // never none, so null spells nothing for them
            string json = RecordWith(record, path, null);
            foreach (var (name, options) in WirePaths.Both)
            {
                var read = JsonSerializer.Deserialize<BgDecisionData>(json, options)!;
                Assert.Equal(json, JsonSerializer.Serialize(read, options));
            }
        }
    }

    [Fact]
    public void TheIdsFilename_IsNeverBlank_InItsCanonicalForm()
    {
        Assert.False(DecisionId.TryParse(" ", provider: null, out _));
        Assert.False(DecisionId.TryParse(" :g1:m1:play", provider: null, out _));
        Assert.Throws<FormatException>(() => DecisionId.Parse("\t"));
        Assert.True(DecisionId.TryParse("a b.xgp", provider: null, out var spaced));
        Assert.Equal("a b.xgp", spaced!.Filename);
    }

    // ── The row ───────────────────────────────────────────────────

    [Theory]
    [InlineData("Xgid")]
    [InlineData("Player")]
    [InlineData("AnalysisDepth")]
    public void ARowWithBlankText_IsRefused_BothPaths(string column)
    {
        var document = WirePaths.Document(TestRecords.Row());
        document[column] = " ";

        var ex = WirePaths.AssertRefused<DecisionRow>(document.ToJsonString());
        Assert.Contains(column, ex.Message);
    }

    [Fact]
    public void ARowsNoneRecorded_IsNull_AndAnEmptyCsvCell()
    {
        var record = TestRecords.CheckerPlay(
            descriptive: TestRecords.Descriptive(onRollName: null),
            decision: TestRecords.CheckerPlayData(plays: [
                TestRecords.Candidate(analysisMode: AnalysisMode.Unknown, analysisLevel: AnalysisLevel.Unknown),
                TestRecords.Candidate(play: [new(13, 10), new(6, 5)], equity: -0.1)]));
        // Under equity the best is the candidate whose depth is not recorded.
        var row = DecisionRow.From(record, PlayRanking.Equity);

        Assert.Null(row.Player);
        Assert.Null(row.AnalysisDepth);
        Assert.Null(((IDecisionFilterData)row).Player);
        // Header: Xgid,Error,MatchScore,MatchLength,Player,SourceFile,…,Roll,AnalysisDepth,Equity,Ranking
        var cells = row.ToCsvLine().Split(',');
        Assert.Equal(string.Empty, cells[4]);
        Assert.Equal(string.Empty, cells[10]);
        Assert.DoesNotContain("null", row.ToCsvLine());
    }
}
