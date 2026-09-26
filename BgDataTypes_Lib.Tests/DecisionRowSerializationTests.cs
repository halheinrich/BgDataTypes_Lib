using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The flat row: built only by projecting a record (<see cref="DecisionRow.From"/>),
/// round-tripped through JSON, and written to CSV. Rewritten for the records
/// leg of halheinrich/backgammon#273: a row is no longer hand-built, so each
/// fixture states the record facts its columns come from, through
/// <see cref="PlayRow"/> and <see cref="CubeRow"/> below.
/// </summary>
public class DecisionRowSerializationTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false
    };

    // -----------------------------------------------------------------------
    //  Fixtures: a row of each kind, from the record facts its columns take
    // -----------------------------------------------------------------------

    /// <summary>
    /// A checker-play row, built as every row is — from a record. Each
    /// argument is named for the column it lands in and states the record
    /// fact that column is taken from: <paramref name="error"/> is the user's
    /// play error, <paramref name="player"/> the player on roll,
    /// <paramref name="dice"/> the roll, and the depth, mode, level and equity
    /// the best candidate's. On the standard start the one candidate plays
    /// 8/5 6/5; on any other <paramref name="board"/> it passes, which is valid
    /// from every position. <paramref name="isStandardStart"/> left
    /// <see langword="null"/> follows the id: stated for a game, none for a
    /// standalone position. With one candidate, every ranking reads alike;
    /// <paramref name="ranking"/> is the one the row states.
    /// </summary>
    private static DecisionRow PlayRow(
        DecisionId? id = null,
        string xgid = "XGID=x",
        double? error = null,
        int matchLength = 7,
        string? player = "Alice",
        bool? isStandardStart = null,
        int[]? dice = null,
        AnalysisMode analysisMode = AnalysisMode.Evaluation,
        AnalysisLevel analysisLevel = AnalysisLevel.Ply3,
        int? rolloutTrials = null,
        double equity = 0.1604,
        int onRollNeeds = 7,
        int opponentNeeds = 7,
        bool isCrawford = false,
        bool? isJacoby = null,
        BoardPosition? board = null,
        int? userPlayIndex = 0,
        PlayRanking ranking = PlayRanking.Equity)
    {
        id ??= new XgDecisionId("match.xg", 1, 1, IsCube: false);
        var mop = board ?? BoardPosition.Standard;
        Play play = mop == BoardPosition.Standard ? [new(8, 5), new(6, 5)] : [];
        return DecisionRow.From(TestRecords.CheckerPlay(
            id: id,
            xgid: xgid,
            position: TestRecords.Position(
                mop: mop, onRollNeeds: onRollNeeds, opponentNeeds: opponentNeeds,
                isCrawford: isCrawford, isJacoby: isJacoby),
            decision: TestRecords.CheckerPlayData(
                dice: dice ?? [3, 1],
                plays: [TestRecords.Candidate(
                    play: play, analysisMode: analysisMode,
                    analysisLevel: analysisLevel, rolloutTrials: rolloutTrials, equity: equity)],
                userPlayIndex: error is null ? userPlayIndex : null,
                unlistedPlayError: error),
            descriptive: TestRecords.Descriptive(
                matchLength: matchLength, onRollName: player,
                isStandardStart: isStandardStart ?? (id is XgpDecisionId ? null : true))), ranking);
    }

    /// <summary>
    /// A cube row, from a record as <see cref="PlayRow"/> is: the depth, mode,
    /// level and equity (the no-double equity) are the cube analysis's. Left
    /// null, <paramref name="error"/> leaves the builder's double and take
    /// stated, whose derived errors are 0; stated, it is the doubling error
    /// of an unstated action — the one cube error a record stores. No column
    /// depends on <paramref name="ranking"/>; the row states it.
    /// </summary>
    private static DecisionRow CubeRow(
        DecisionId? id = null,
        string xgid = "XGID=x",
        double? error = null,
        int matchLength = 7,
        string? player = "Alice",
        bool? isStandardStart = null,
        AnalysisMode analysisMode = AnalysisMode.Evaluation,
        AnalysisLevel analysisLevel = AnalysisLevel.Ply3,
        int? rolloutTrials = null,
        double equity = 0.512,
        int onRollNeeds = 7,
        int opponentNeeds = 7,
        bool? isJacoby = null,
        PlayRanking ranking = PlayRanking.Equity)
    {
        id ??= new XgDecisionId("match.xg", 1, 2, IsCube: true);
        return DecisionRow.From(TestRecords.Cube(
            id: id,
            xgid: xgid,
            position: TestRecords.Position(onRollNeeds: onRollNeeds, opponentNeeds: opponentNeeds, isJacoby: isJacoby),
            decision: error is null
                ? TestRecords.CubeData(
                    analysisMode: analysisMode, analysisLevel: analysisLevel, rolloutTrials: rolloutTrials,
                    noDoubleEquity: equity)
                : TestRecords.CubeData(
                    analysisMode: analysisMode, analysisLevel: analysisLevel, rolloutTrials: rolloutTrials,
                    noDoubleEquity: equity, userDoublerAction: null, userTakerAction: null,
                    unstatedDoublerActionError: error),
            descriptive: TestRecords.Descriptive(
                matchLength: matchLength, onRollName: player,
                isStandardStart: isStandardStart ?? (id is XgpDecisionId ? null : true))), ranking);
    }

    // -----------------------------------------------------------------------
    //  Absence (halheinrich/backgammon#222): see
    //  BgDecisionDataSerializationTests for the pattern; WireAbsenceTests
    //  walks every member on both paths.
    // -----------------------------------------------------------------------

    /// <summary>
    /// The row's document without each of <paramref name="members"/> is
    /// refused; the full document loads (the control).
    /// </summary>
    private static void AssertAbsentIsRefused(DecisionRow full, params string[] members)
    {
        var json = JsonSerializer.Serialize(full, Options);
        Assert.NotNull(JsonSerializer.Deserialize<DecisionRow>(json, Options));
        foreach (var member in members)
        {
            var document = JsonNode.Parse(json)!.AsObject();
            Assert.True(document.Remove(member), $"{member} is not in the document");
            Assert.Throws<JsonException>(
                () => JsonSerializer.Deserialize<DecisionRow>(document.ToJsonString(), Options));
        }
    }

    /// <summary><paramref name="full"/> read back from a document without <paramref name="members"/>.</summary>
    private static DecisionRow ReadWithout(DecisionRow full, params string[] members)
    {
        var document = JsonNode.Parse(JsonSerializer.Serialize(full, Options))!.AsObject();
        foreach (var member in members)
            Assert.True(document.Remove(member), $"{member} is not in the document");
        return JsonSerializer.Deserialize<DecisionRow>(document.ToJsonString(), Options)!;
    }

    private static DecisionRow RoundTrip(DecisionRow row, JsonSerializerOptions? options = null) =>
        JsonSerializer.Deserialize<DecisionRow>(JsonSerializer.Serialize(row, options ?? Options), options ?? Options)!;

    // -----------------------------------------------------------------------
    //  JSON round-trip
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_RoundTrip_CheckerPlay()
    {
        var original = PlayRow(
            id: new XgDecisionId("mochy-falafel.xg", Game: 2, MoveNumber: 7, IsCube: false),
            xgid: "XGID=-b----E-C---eE---c-e----B-:0:0:1:63:0:0:3:0:10",
            error: 0.023,
            matchLength: 9,
            onRollNeeds: 3,
            opponentNeeds: 5,
            isCrawford: false,
            isJacoby: null,
            player: "Mochy",
            dice: [6, 3],
            equity: -0.142,
            board: new BoardPosition([0, 2, 0, 0, 0, 0, -5, 0, -3, 0, 0, 0, 5, 0, 0, 0, 0, -5, 0, -2, 0, 0, 0, 0, 2, 1]));

        var restored = RoundTrip(original);

        Assert.Equal(DecisionKind.CheckerPlay, restored.Kind);
        Assert.Equal(original.Xgid, restored.Xgid);
        Assert.Equal(original.Error, restored.Error);
        Assert.Equal(original.MatchLength, restored.MatchLength);
        Assert.Equal(original.OnRollNeeds, restored.OnRollNeeds);
        Assert.Equal(original.OpponentNeeds, restored.OpponentNeeds);
        Assert.Equal(original.IsCrawford, restored.IsCrawford);
        Assert.Equal(original.IsJacoby, restored.IsJacoby);
        Assert.Equal(original.Player, restored.Player);
        Assert.Equal(original.SourceFile, restored.SourceFile);
        Assert.Equal(2, restored.Game);
        Assert.Equal(7, restored.MoveNumber);
        Assert.Equal(63, restored.Roll);
        Assert.Equal(original.AnalysisDepth, restored.AnalysisDepth);
        Assert.Equal(original.Equity, restored.Equity);
        Assert.Equal(original.Board, restored.Board);
        Assert.Equal(original.AfterBestBoard, restored.AfterBestBoard);
        Assert.Equal(original.Ranking, restored.Ranking);
    }

    [Fact]
    public void DecisionRow_RoundTrip_CubeDecision()
    {
        // Rewritten: the cube row's roll is empty now — null, never 0 — and
        // its kind is stated.
        var original = CubeRow(
            equity: 0.312,
            player: "Falafel",
            analysisMode: AnalysisMode.Rollout,
            analysisLevel: AnalysisLevel.Ply3,
            rolloutTrials: 1296,
            matchLength: 9,
            onRollNeeds: 1,
            opponentNeeds: 1);

        var restored = RoundTrip(original);

        Assert.Equal(DecisionKind.Cube, restored.Kind);
        Assert.Null(restored.Roll);
        Assert.Equal(original.Equity, restored.Equity);
        Assert.Equal(original.AnalysisDepth, restored.AnalysisDepth);
        Assert.False(restored.IsCrawford);
    }

    [Fact]
    public void DecisionRow_RoundTrip_IsCrawford()
    {
        // The flag's true round trip rides a Crawford checker play: a
        // Crawford cube cannot be constructed (DecisionRowCrawfordCubeTests).
        var original = PlayRow(dice: [5, 2], matchLength: 9, onRollNeeds: 1, opponentNeeds: 3, isCrawford: true);

        var restored = RoundTrip(original);

        Assert.True(restored.IsCrawford);
        Assert.Equal(DecisionKind.CheckerPlay, restored.Kind);
        Assert.Equal("1a3aC", restored.MatchScore);
    }

    [Fact]
    public void DecisionRow_RoundTrip_NoneRecorded()
    {
        // Rewritten from DecisionRow_RoundTrip_EmptyStrings (itself from
        // DecisionRow_RoundTrip_EmptyStringsAndNullSourceFile and
        // DecisionRow_RoundTrip_StringDefaults): empty text no longer means
        // "none recorded" — null does, its one spelling — so a player and a
        // depth label that were not recorded round-trip as null, and an empty
        // one cannot be stated. The XGID and the source file are never none.
        var restored = RoundTrip(PlayRow(player: null, analysisMode: AnalysisMode.Unknown, analysisLevel: AnalysisLevel.Unknown));

        Assert.Null(restored.Player);
        Assert.Null(restored.AnalysisDepth);
        Assert.Equal("XGID=x", restored.Xgid);
        Assert.Equal("match.xg", restored.SourceFile);
        Assert.Throws<ArgumentException>(() => PlayRow(player: ""));
        Assert.Throws<ArgumentException>(() => PlayRow(xgid: ""));
    }

    [Fact]
    public void DecisionRow_RoundTrip_Board()
    {
        var board = new int[26];
        board[1] = 2; board[6] = -5; board[24] = -2; board[25] = 1;

        var original = PlayRow(board: new BoardPosition(board));

        Assert.Equal(original.Board, RoundTrip(original).Board);
    }

    [Fact]
    public void DecisionRow_RoundTrip_MatchScoreNotSerialized()
    {
        var original = PlayRow(matchLength: 9, onRollNeeds: 3, opponentNeeds: 5);
        var json = JsonSerializer.Serialize(original, Options);

        Assert.DoesNotContain("MatchScore", json);

        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;
        Assert.Equal("3a5a", restored.MatchScore);
    }

    // -----------------------------------------------------------------------
    //  The projection (halheinrich/backgammon#273, the records leg)
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_From_TakesEveryColumnFromTheRecord_CheckerPlay()
    {
        // Added: the row is the record's, column by column — the shared view
        // through IDecisionFilterData, the kind's own from the kind, and what
        // says best or error from the ranking the row is built for. Where the
        // rankings differ is pinned in PlayRankingTests.
        var record = TestRecords.CheckerPlay(decision: TestRecords.CheckerPlayData(userPlayIndex: 2));

        foreach (var ranking in Enum.GetValues<PlayRanking>())
        {
            var row = DecisionRow.From(record, ranking);
            var ranked = record.Decision.RankedBy(ranking);

            Assert.Equal(ranking, row.Ranking);
            Assert.Equal(record.Kind, row.Kind);
            Assert.Equal(record.Id, row.Id);
            Assert.Equal(record.Xgid, row.Xgid);
            Assert.Equal(ranked.UserPlayError, row.Error);
            Assert.Equal(0.1813, row.Error!.Value, 12);
            Assert.Equal(record.MatchLength, row.MatchLength);
            Assert.Equal(record.Player, row.Player);
            Assert.Equal(record.SourceFile, row.SourceFile);
            Assert.Equal(record.IsStandardStart, row.IsStandardStart);
            Assert.Equal(31, row.Roll);
            Assert.Equal(ranked.Best.Candidate.Depth, row.AnalysisDepth);
            Assert.Equal(ranked.Best.Candidate.AnalysisMode, row.AnalysisMode);
            Assert.Equal(ranked.Best.Candidate.AnalysisLevel, row.AnalysisLevel);
            Assert.Equal(ranked.Best.Candidate.Equity, row.Equity);
            Assert.Equal(record.OnRollNeeds, row.OnRollNeeds);
            Assert.Equal(record.OpponentNeeds, row.OpponentNeeds);
            Assert.Equal(record.IsCrawford, row.IsCrawford);
            Assert.Equal(record.IsJacoby, row.IsJacoby);
            Assert.Equal(record.Board, row.Board);
            Assert.Equal(record.AfterBoardOfBest(ranking), row.AfterBestBoard);
            Assert.Equal(record.AfterPlayerBoard, row.AfterPlayerBoard);
        }
    }

    [Fact]
    public void DecisionRow_From_TakesEveryColumnFromTheRecord_Cube()
    {
        // Added: the cube's own columns, and the checker play's empty.
        var record = TestRecords.Cube(decision: TestRecords.CubeData(
            userDoublerAction: null, userTakerAction: null, unstatedTakerActionError: 0.04));

        foreach (var ranking in Enum.GetValues<PlayRanking>())
        {
            var row = DecisionRow.From(record, ranking);

            Assert.Equal(ranking, row.Ranking);
            Assert.Equal(DecisionKind.Cube, row.Kind);
            Assert.Equal(0.04, row.Error);
            Assert.Equal(record.Decision.Depth, row.AnalysisDepth);
            Assert.Equal(record.Decision.AnalysisMode, row.AnalysisMode);
            Assert.Equal(record.Decision.AnalysisLevel, row.AnalysisLevel);
            Assert.Equal(record.Decision.NoDoubleEquity, row.Equity);
            Assert.Null(row.Roll);
            Assert.Null(row.AfterBestBoard);
            Assert.Null(row.AfterPlayerBoard);
        }
    }

    [Fact]
    public void DecisionRow_From_AgreesWithTheRecord_OnEveryFilterMember_EachKind_EachRanking()
    {
        // Added: the IDecisionFilterData contract, member by member, read off
        // the record's view and off its row, each built for the same ranking
        // — the agreement the converter used to test after building the two
        // separately is structural now.
        foreach (BgDecisionData record in new BgDecisionData[]
                 {
                     TestRecords.CheckerPlay(decision: TestRecords.CheckerPlayData(dice: [1, 3], userPlayIndex: 1)),
                     TestRecords.CheckerPlay(id: new XgpDecisionId("p.xgp"), decision: TestRecords.CheckerPlayData(userPlayIndex: null)),
                     TestRecords.Cube(),
                     TestRecords.Cube(position: TestRecords.Position(onRollNeeds: 0, opponentNeeds: 0, isJacoby: true),
                         descriptive: TestRecords.Descriptive(matchLength: 0)),
                 })
        {
            foreach (var ranking in Enum.GetValues<PlayRanking>())
            {
                var fromRecord = record.ViewFor(ranking);
                IDecisionFilterData fromRow = DecisionRow.From(record, ranking);

                foreach (var property in typeof(IDecisionFilterData).GetProperties())
                    Assert.Equal(property.GetValue(fromRecord), property.GetValue(fromRow));
                Assert.Equal(fromRecord.IsMoneyGame, fromRow.IsMoneyGame);
                Assert.Equal(ranking, fromRow.Ranking);
            }
        }
    }

    [Fact]
    public void DecisionRow_HasNoPublicConstructor()
    {
        // Added: the projection is the one way a row is made outside this
        // library, so its after-boards are never computed a second way.
        Assert.Empty(typeof(DecisionRow).GetConstructors());
    }

    [Fact]
    public void DecisionRow_OtherKindsColumns_AreEmpty_EachKind()
    {
        // Added: a cube row's checker-play columns are empty — null, never
        // zero — in JSON and in CSV; a checker row has no cube-only column.
        var cube = CubeRow(player: "Mochy");
        var json = JsonSerializer.Serialize(cube, Options);

        Assert.Null(cube.Roll);
        Assert.Null(cube.Dice);
        Assert.Null(cube.AfterBestBoard);
        Assert.Null(cube.AfterPlayerBoard);
        Assert.Contains("\"Roll\":null", json);
        Assert.Contains("\"AfterBestBoard\":null", json);
        Assert.Contains(",Cube,,", cube.ToCsvLine());

        var play = PlayRow();
        Assert.NotNull(play.Roll);
        Assert.NotNull(play.AfterBestBoard);
    }

    // -----------------------------------------------------------------------
    //  Read back whole — a row read from JSON is held to what the
    //  projection guarantees, refused with a JsonException on both paths
    // -----------------------------------------------------------------------

    public static TheoryData<string, string> MalformedRows => new()
    {
        // (a fixture's mutation, what it breaks)
        { "cube-roll", "A cube row's checker-play columns are empty" },
        { "cube-afterbest", "A cube row's checker-play columns are empty" },
        { "cube-crawford", "Crawford" },
        { "play-no-roll", "states its Roll" },
        { "play-bad-roll", "not two die faces" },
        { "play-no-afterbest", "the board its best play leaves" },
        { "play-id-names-cube", "identifier names the other kind" },
        { "standalone-with-start", "IsStandardStart is a fact about a game" },
        { "game-without-start", "IsStandardStart is a fact about a game" },
        { "null-id", "states its Id" },
    };

    private static string Malformed(string name)
    {
        var play = WirePaths.Document(PlayRow());
        var cube = WirePaths.Document(CubeRow());
        switch (name)
        {
            case "cube-roll": cube["Roll"] = 31; return cube.ToJsonString();
            case "cube-afterbest": cube["AfterBestBoard"] = play["AfterBestBoard"]!.DeepClone(); return cube.ToJsonString();
            case "cube-crawford": cube["IsCrawford"] = true; return cube.ToJsonString();
            case "play-no-roll": play.Remove("Roll"); return play.ToJsonString();
            case "play-bad-roll": play["Roll"] = 70; return play.ToJsonString();
            case "play-no-afterbest": play["AfterBestBoard"] = null; return play.ToJsonString();
            case "play-id-names-cube": play["Id"] = "match.xg:g1:m1:cube"; return play.ToJsonString();
            case "standalone-with-start": play["Id"] = "p.xgp"; return play.ToJsonString();
            case "game-without-start": play["IsStandardStart"] = null; return play.ToJsonString();
            case "null-id": play["Id"] = null; return play.ToJsonString();
            default: throw new ArgumentOutOfRangeException(nameof(name), name, null);
        }
    }

    [Theory]
    [MemberData(nameof(MalformedRows))]
    public void DecisionRow_Read_BreakingAGuarantee_IsRefused_BothPaths(string mutation, string reason)
    {
        // Added.
        var ex = WirePaths.AssertRefused<DecisionRow>(Malformed(mutation));

        Assert.Contains(reason, ex.Message);
    }

    [Fact]
    public void DecisionRow_Read_WellFormedRows_Load_BothPaths()
    {
        // The control for every refusal above.
        foreach (var row in new[] { PlayRow(), CubeRow(), PlayRow(id: new XgpDecisionId("p.xgp")) })
            foreach (var (_, options) in WirePaths.Both)
                Assert.NotNull(RoundTrip(row, options));
    }

    // -----------------------------------------------------------------------
    //  AnalysisMode / AnalysisLevel — JSON only, excluded from CSV
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_AnalysisModeAndLevel_RoundTrip()
    {
        var original = PlayRow(
            analysisMode: AnalysisMode.Evaluation,
            analysisLevel: AnalysisLevel.XgRollerPlusPlus);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;

        // String form via the enums' bundled converters — no options-level registration.
        Assert.Contains("\"AnalysisMode\":\"Evaluation\"", json);
        Assert.Contains("\"AnalysisLevel\":\"XgRollerPlusPlus\"", json);
        Assert.Equal(AnalysisMode.Evaluation, restored.AnalysisMode);
        Assert.Equal(AnalysisLevel.XgRollerPlusPlus, restored.AnalysisLevel);
    }

    [Fact]
    public void DecisionRow_AnalysisModeAndLevel_AbsentIsRefused()
    {
        // Rewritten from DecisionRow_AnalysisModeAndLevel_DefaultToUnknown.
        AssertAbsentIsRefused(PlayRow(), "AnalysisMode", "AnalysisLevel");
    }

    [Fact]
    public void DecisionRow_LegacyAnalysisDepthClassJson_IsRefused_TheRetiredPropertyStillIgnored()
    {
        // Rewritten from DecisionRow_LegacyAnalysisDepthClassJson_DeserializesToUnknownPair.
        // JSON written before the two-axis pair existed lacks it and is
        // refused. The retired flat "AnalysisDepthClass" property is still
        // ignored beside a full row: every column of either kind is a member
        // of the one flat row, so a member it does not know cannot be another
        // kind's.
        var legacy = "{\"Id\":\"test.xgp\",\"AnalysisDepth\":\"3-ply\",\"AnalysisDepthClass\":\"Ply3\",\"Roll\":63}";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DecisionRow>(legacy, Options));

        var row = PlayRow();
        var full = JsonNode.Parse(JsonSerializer.Serialize(row, Options))!.AsObject();
        full["AnalysisDepthClass"] = "Ply3";
        var restored = JsonSerializer.Deserialize<DecisionRow>(full.ToJsonString(), Options)!;

        Assert.Equal(row.AnalysisMode, restored.AnalysisMode);
        Assert.Equal(row.AnalysisLevel, restored.AnalysisLevel);
        Assert.Equal("3-ply", restored.AnalysisDepth);
    }

    [Fact]
    public void DecisionRow_AnalysisModeAndLevel_NotInCsvOutput()
    {
        var row = PlayRow(analysisMode: AnalysisMode.Evaluation, analysisLevel: AnalysisLevel.Ply3);

        Assert.DoesNotContain("AnalysisMode", DecisionRow.CsvHeader);
        Assert.DoesNotContain("AnalysisLevel", DecisionRow.CsvHeader);
        // Rewritten: 13 columns with the Kind and Ranking columns → 12 commas.
        Assert.Equal(12, row.ToCsvLine().Count(c => c == ','));
    }

    [Fact]
    public void DecisionRow_IDecisionFilterData_AnalysisModeAndLevel()
    {
        IDecisionFilterData row = CubeRow(
            analysisMode: AnalysisMode.BookRollout,
            analysisLevel: AnalysisLevel.XgRoller);

        Assert.Equal(AnalysisMode.BookRollout, row.AnalysisMode);
        Assert.Equal(AnalysisLevel.XgRoller, row.AnalysisLevel);
    }

    // -----------------------------------------------------------------------
    //  IsMoneyGame — derived from MatchLength, feeds MatchScore
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_IsMoneyGame_MoneyRow()
    {
        var row = PlayRow(matchLength: 0, onRollNeeds: 0, opponentNeeds: 0);

        Assert.True(row.IsMoneyGame);
        // IsJacoby unset, so the bare (rule-unknown) money token.
        Assert.Equal("money", row.MatchScore);
    }

    [Fact]
    public void DecisionRow_IsMoneyGame_OnePointMatch()
    {
        // The shortest possible match — the boundary case next to money's 0.
        var row = PlayRow(matchLength: 1, onRollNeeds: 1, opponentNeeds: 1);

        Assert.False(row.IsMoneyGame);
        Assert.Equal("1a1a", row.MatchScore);
    }

    [Fact]
    public void DecisionRow_IsMoneyGame_StandardMatch()
    {
        var row = PlayRow(matchLength: 9, onRollNeeds: 3, opponentNeeds: 5);

        Assert.False(row.IsMoneyGame);
        Assert.Equal("3a5a", row.MatchScore);
    }

    [Fact]
    public void DecisionRow_IsMoneyGame_NotSerialized_MatchLengthRemainsTheWire()
    {
        var original = PlayRow(matchLength: 0);
        var json = JsonSerializer.Serialize(original, Options);

        Assert.DoesNotContain("IsMoneyGame", json);
        Assert.Contains("\"MatchLength\":0", json);

        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;
        Assert.True(restored.IsMoneyGame);
    }

    [Fact]
    public void DecisionRow_IsMoneyGame_NotInCsvOutput()
    {
        var row = PlayRow(matchLength: 0);

        Assert.DoesNotContain("IsMoneyGame", DecisionRow.CsvHeader);
        // Rewritten: 13 columns with the Kind and Ranking columns → 12 commas.
        Assert.Equal(12, row.ToCsvLine().Count(c => c == ','));
    }

    [Fact]
    public void DecisionRow_IDecisionFilterData_IsMoneyGame()
    {
        IDecisionFilterData money = PlayRow(matchLength: 0);
        IDecisionFilterData match = PlayRow(matchLength: 9);

        Assert.True(money.IsMoneyGame);
        Assert.False(match.IsMoneyGame);
    }

    // -----------------------------------------------------------------------
    //  MatchScore reconstruction
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_MatchScore_Money_JacobyUnknown()
    {
        // No IsJacoby stamp — the bare token, which is neither moneyJ nor moneyNJ.
        var row = PlayRow(matchLength: 0, onRollNeeds: 0, opponentNeeds: 0);
        Assert.Equal("money", row.MatchScore);
    }

    [Fact]
    public void DecisionRow_MatchScore_Money_Jacoby()
    {
        var row = PlayRow(matchLength: 0, onRollNeeds: 0, opponentNeeds: 0, isJacoby: true);
        Assert.Equal("moneyJ", row.MatchScore);
    }

    [Fact]
    public void DecisionRow_MatchScore_Money_NoJacoby()
    {
        var row = PlayRow(matchLength: 0, onRollNeeds: 0, opponentNeeds: 0, isJacoby: false);
        Assert.Equal("moneyNJ", row.MatchScore);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    public void DecisionRow_MatchScore_MatchRow_IgnoresIsJacoby(bool? isJacoby)
    {
        // The suffix is money-only: a match score is unchanged by the stamp,
        // stray or otherwise, exactly as PositionData.IsJacoby's contract says.
        var row = PlayRow(matchLength: 9, onRollNeeds: 3, opponentNeeds: 5, isJacoby: isJacoby);
        Assert.Equal("3a5a", row.MatchScore);
    }

    [Fact]
    public void DecisionRow_MatchScore_Standard()
    {
        var row = PlayRow(matchLength: 9, onRollNeeds: 3, opponentNeeds: 5);
        Assert.Equal("3a5a", row.MatchScore);
    }

    [Fact]
    public void DecisionRow_MatchScore_Crawford()
    {
        var row = PlayRow(matchLength: 9, onRollNeeds: 1, opponentNeeds: 1, isCrawford: true);
        Assert.Equal("1a1aC", row.MatchScore);
    }

    // -----------------------------------------------------------------------
    //  CSV
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_CsvHeader_ContainsExpectedColumns()
    {
        // Rewritten: the row carries its kind, so the CSV does — a column of
        // its own beside the roll, never inferred from an empty roll — and
        // the ranking it was built for, last, since the error, depth and
        // equity before it are that ranking's.
        Assert.Equal(
            "Xgid,Error,MatchScore,MatchLength,Player,SourceFile,Game,MoveNumber,Kind,Roll,AnalysisDepth,Equity,Ranking",
            DecisionRow.CsvHeader);
    }

    [Fact]
    public void DecisionRow_ToCsvLine_EndsWithTheRankingsToken_EachKind()
    {
        // Added: the declared name, as on the wire.
        foreach (var ranking in Enum.GetValues<PlayRanking>())
        {
            Assert.EndsWith($",{ranking}", PlayRow(ranking: ranking).ToCsvLine());
            Assert.EndsWith($",{ranking}", CubeRow(ranking: ranking).ToCsvLine());
        }
        Assert.EndsWith(",DepthFirst", PlayRow(ranking: PlayRanking.DepthFirst).ToCsvLine());
    }

    [Fact]
    public void DecisionRow_ToCsvLine_KindAndRoll_EachKind()
    {
        // Added: the kind's token, then the roll — empty for a cube.
        Assert.Contains(",CheckerPlay,63,", PlayRow(dice: [6, 3]).ToCsvLine());
        Assert.Contains(",Cube,,", CubeRow().ToCsvLine());
    }

    [Fact]
    public void DecisionRow_ToCsvLine_EscapesCommas()
    {
        var row = PlayRow(matchLength: 9, onRollNeeds: 3, opponentNeeds: 5, player: "Last, First");
        var line = row.ToCsvLine();
        Assert.Contains("\"Last, First\"", line);
    }

    [Fact]
    public void DecisionRow_ToCsvLine_EscapesQuotes()
    {
        var row = PlayRow(player: "say \"hello\"");
        var line = row.ToCsvLine();
        Assert.Contains("\"say \"\"hello\"\"\"", line);
    }

    [Fact]
    public void DecisionRow_ToCsvLine_DoublesFormattedG6()
    {
        var row = PlayRow(error: 0.12345678, equity: -0.98765432);
        var line = row.ToCsvLine();
        Assert.Contains("0.123457", line);
        Assert.Contains("-0.987654", line);
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("sv-SE")]
    public void DecisionRow_ToCsvLine_IsCultureInvariant(string cultureName)
    {
        // Added: every number in a CSV line is written with the invariant
        // culture, whatever the ambient one. Pinned under two comma-decimal
        // cultures — sv-SE also writes its minus sign as U+2212 under ICU — so
        // a number formatted with the ambient culture would split its cell in
        // two or change its sign.
        var row = PlayRow(
            error: 0.12345678, equity: -0.98765432, dice: [6, 3],
            matchLength: 9, onRollNeeds: 3, opponentNeeds: 5);
        const string expected = "XGID=x,0.123457,3a5a,9,Alice,match.xg,1,1,CheckerPlay,63,3-ply,-0.987654,Equity";

        var original = CultureInfo.CurrentCulture;
        try
        {
            var culture = new CultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            Assert.Equal(",", culture.NumberFormat.NumberDecimalSeparator);  // the pin is live

            Assert.Equal(expected, row.ToCsvLine());
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void DecisionRow_ToCsvLine_NoErrorRecorded_IsAnEmptyCell()
    {
        // Added: "no user decision recorded" is null, an empty cell — the
        // 0 the producer used to write in its place was a stand-in.
        var row = PlayRow(xgid: "XGID=x", error: null, userPlayIndex: null);

        Assert.Null(row.Error);
        Assert.StartsWith("XGID=x,,", row.ToCsvLine());
    }

    [Fact]
    public void DecisionRow_ToCsvLine_BoardNotInCsv()
    {
        var row = PlayRow(board: new BoardPosition([0, 2, 0, 0, 0, 0, -5, 0, -3, 0, 0, 0, 5, 0, 0, 0, 0, -5, 0, -2, 0, 0, 0, 0, 2, 1]));
        var line = row.ToCsvLine();
        // Rewritten: 13 columns with the Kind and Ranking columns → 12 commas.
        Assert.Equal(12, line.Count(c => c == ','));
    }

    // -----------------------------------------------------------------------
    //  SourceFile — field and CSV behaviour
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_RetiredSourceFileColumn_IsIgnored_TheIdsStands()
    {
        // Rewritten from DecisionRow_SourceFile_AbsentReadsAsNull: the source
        // file is the Id's, not a column of its own, so there is nothing to be
        // absent. A document still stating the retired column reads with it
        // ignored — a stated name cannot contradict the Id.
        var document = JsonNode.Parse(JsonSerializer.Serialize(PlayRow(), Options))!.AsObject();
        Assert.False(document.ContainsKey("SourceFile"));
        document["SourceFile"] = "other.xg";

        foreach (var (_, options) in WirePaths.Both)
            Assert.Equal("match.xg", JsonSerializer.Deserialize<DecisionRow>(document.ToJsonString(), options)!.SourceFile);
    }

    [Fact]
    public void DecisionRow_ToCsvLine_SourceFile_IsTheIdsFilename()
    {
        // Rewritten from DecisionRow_ToCsvLine_SourceFile_EmptyCellWhenNull:
        // every decision has a source file — its Id's — so the cell is never
        // empty. Header: Xgid,Error,MatchScore,MatchLength,Player,SourceFile,...
        // (column index 5, between Player and Game).
        Assert.Contains(",Mochy,match.xg,1,1,", PlayRow(player: "Mochy").ToCsvLine());
        Assert.Contains(",Mochy,position.xgp,,,",
            PlayRow(id: new XgpDecisionId("position.xgp"), player: "Mochy").ToCsvLine());
    }

    [Fact]
    public void DecisionRow_ToCsvLine_SourceFile_PlainFilename()
    {
        var line = PlayRow(id: new XgDecisionId("mochy-falafel.xg", 1, 1, IsCube: false)).ToCsvLine();
        Assert.Contains(",mochy-falafel.xg,", line);
    }

    [Fact]
    public void DecisionRow_ToCsvLine_SourceFile_FilenameWithSpaces_Unquoted()
    {
        var line = PlayRow(id: new XgpDecisionId("Mochy vs Falafel.xgp")).ToCsvLine();
        // RFC 4180 does not require quoting on spaces; value passes through literally.
        Assert.Contains(",Mochy vs Falafel.xgp,", line);
    }

    [Fact]
    public void DecisionRow_ToCsvLine_SourceFile_FilenameWithComma_Quoted()
    {
        var line = PlayRow(id: new XgDecisionId("file,with,commas.xg", 1, 1, IsCube: false)).ToCsvLine();
        Assert.Contains("\"file,with,commas.xg\"", line);
    }

    [Fact]
    public void DecisionRow_RoundTrip_SourceFile()
    {
        // Rewritten: the source file rides the Id through the round trip.
        Assert.Equal("mochy-falafel.xg",
            RoundTrip(PlayRow(id: new XgDecisionId("mochy-falafel.xg", 1, 1, IsCube: false))).SourceFile);
    }

    // -----------------------------------------------------------------------
    //  IDecisionFilterData — DecisionRow
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_IDecisionFilterData_CheckerPlay()
    {
        var board = new BoardPosition([0, 2, 0, 0, 0, 0, -5, 0, -3, 0, 0, 0, 5, 0, 0, 0, 0, -5, 0, -2, 0, 0, 0, 0, 2, 1]);
        IDecisionFilterData row = PlayRow(
            player: "Mochy",
            dice: [6, 3],
            matchLength: 9,
            onRollNeeds: 3,
            opponentNeeds: 5,
            error: 0.023,
            board: board);

        Assert.Equal("Mochy", row.Player);
        Assert.Equal(DecisionKind.CheckerPlay, row.Kind);
        Assert.Equal(3, row.OnRollNeeds);
        Assert.Equal(5, row.OpponentNeeds);
        Assert.False(row.IsCrawford);
        Assert.Equal(0.023, row.FilterError);
        Assert.Equal(board, row.Board);
    }

    [Fact]
    public void DecisionRow_IDecisionFilterData_CubeDecision()
    {
        IDecisionFilterData row = CubeRow(
            player: "Falafel",
            matchLength: 9,
            onRollNeeds: 1,
            opponentNeeds: 1,
            error: 0.011);

        Assert.Equal(DecisionKind.Cube, row.Kind);
        Assert.False(row.IsCrawford);
        Assert.Equal(0.011, row.FilterError);
    }

    [Fact]
    public void DecisionRow_IDecisionFilterData_IsCrawford_Forwards()
    {
        // The flag forwards from a record that can exist — a Crawford
        // checker play; the cube case above is post-Crawford by necessity.
        IDecisionFilterData row = PlayRow(dice: [5, 2], matchLength: 9, onRollNeeds: 1, opponentNeeds: 3, isCrawford: true);

        Assert.Equal(DecisionKind.CheckerPlay, row.Kind);
        Assert.True(row.IsCrawford);
    }

    [Fact]
    public void DecisionRow_IDecisionFilterData_FilterError_IsNullableDouble()
    {
        // Rewritten: the error is nullable on the row as on the record —
        // present here, none when no user decision is recorded.
        IDecisionFilterData row = PlayRow(error: 0.045);
        double? fe = row.FilterError;
        Assert.NotNull(fe);
        Assert.Equal(0.045, fe!.Value);
        Assert.Null(((IDecisionFilterData)PlayRow(error: null, userPlayIndex: null)).FilterError);
    }

    // -----------------------------------------------------------------------
    //  Dice — canonical roll derived from Roll
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_Dice_CheckerPlay_DerivedFromRoll()
    {
        Assert.Equal(new DiceRoll(6, 3), PlayRow(dice: [6, 3]).Dice);
    }

    [Fact]
    public void DecisionRow_Dice_RolledOrderCanonicalized()
    {
        // The XG parser stamps dice in rolled order, so Roll carries both
        // spellings of a 3-1 (31 and 13); Dice canonicalizes high-first.
        var row = PlayRow(dice: [1, 3]);

        Assert.Equal(13, row.Roll);
        Assert.Equal(new DiceRoll(3, 1), row.Dice);
        Assert.Equal(3, row.Dice!.Value.High);
        Assert.Equal(1, row.Dice!.Value.Low);
    }

    [Fact]
    public void DecisionRow_Dice_CubeDecision_IsNull()
    {
        var row = CubeRow();

        Assert.Equal(DecisionKind.Cube, row.Kind);
        Assert.Null(row.Dice);
    }

    [Theory]
    // Digits outside 1–6, or not two digits at all — corrupt data fails loud.
    [InlineData(70)]
    [InlineData(7)]
    [InlineData(-13)]
    [InlineData(315)]
    public void DecisionRow_Dice_MalformedRoll_IsRefusedOnRead(int badRoll)
    {
        // Rewritten from DecisionRow_Dice_MalformedRoll_Throws: a projection
        // cannot hold a malformed roll, and a document holding one is refused
        // at read, so the Dice derivation never meets it.
        var document = WirePaths.Document(PlayRow());
        document["Roll"] = badRoll;

        WirePaths.AssertRefused<DecisionRow>(document.ToJsonString());
    }

    [Fact]
    public void DecisionRow_Dice_NotSerialized_RollRemainsTheWire()
    {
        var original = PlayRow(dice: [6, 3]);
        var json = JsonSerializer.Serialize(original, Options);

        Assert.DoesNotContain("\"Dice\"", json);
        Assert.Contains("\"Roll\":63", json);

        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;
        Assert.Equal(new DiceRoll(6, 3), restored.Dice);
    }

    [Fact]
    public void DecisionRow_Dice_NotInCsvOutput()
    {
        var row = PlayRow(dice: [6, 3]);

        Assert.DoesNotContain("Dice", DecisionRow.CsvHeader);
        // Rewritten: 13 columns with the Kind and Ranking columns → 12 commas.
        Assert.Equal(12, row.ToCsvLine().Count(c => c == ','));
    }

    [Fact]
    public void DecisionRow_IDecisionFilterData_Dice()
    {
        IDecisionFilterData play = PlayRow(dice: [5, 2]);
        IDecisionFilterData cube = CubeRow();

        Assert.Equal(new DiceRoll(5, 2), play.Dice);
        Assert.Null(cube.Dice);
    }

    // -----------------------------------------------------------------------
    //  After-boards — the record's derivation, JSON only, excluded from CSV
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_AfterBoards_AbsentAreNull()
    {
        // Rewritten: a cube row's after-boards are empty (null); a checker
        // row's player board is null exactly when the user's play is not
        // among the candidates, and reads null when absent.
        var cube = CubeRow();
        Assert.Null(cube.AfterBestBoard);
        Assert.Null(cube.AfterPlayerBoard);

        Assert.Null(PlayRow(userPlayIndex: null).AfterPlayerBoard);
        Assert.Null(ReadWithout(PlayRow(), "AfterPlayerBoard").AfterPlayerBoard);
    }

    [Fact]
    public void DecisionRow_RoundTrip_AfterBoards()
    {
        // Rewritten: the boards are the record's derivation, taken when the
        // row is built, and round-trip as the row's columns.
        var record = TestRecords.CheckerPlay(decision: TestRecords.CheckerPlayData(userPlayIndex: 1));

        foreach (var ranking in Enum.GetValues<PlayRanking>())
        {
            var original = DecisionRow.From(record, ranking);
            foreach (var (_, options) in WirePaths.Both)
            {
                var restored = RoundTrip(original, options);
                Assert.Equal(record.AfterBoardOfBest(ranking), restored.AfterBestBoard);
                Assert.Equal(record.AfterPlayerBoard, restored.AfterPlayerBoard);
            }
        }
    }

    [Fact]
    public void DecisionRow_ToCsvLine_AfterBoardsNotInCsv()
    {
        var line = PlayRow().ToCsvLine();
        // Rewritten: 13 columns with the Kind and Ranking columns → 12 commas.
        Assert.Equal(12, line.Count(c => c == ','));
    }

    [Fact]
    public void DecisionRow_CsvHeader_DoesNotMentionAfterBoards()
    {
        Assert.DoesNotContain("AfterBestBoard", DecisionRow.CsvHeader);
        Assert.DoesNotContain("AfterPlayerBoard", DecisionRow.CsvHeader);
    }

    [Fact]
    public void DecisionRow_IDecisionFilterData_AfterBoards_CheckerPlay()
    {
        // Rewritten: the row forwards the boards it took from the record.
        var record = TestRecords.CheckerPlay(decision: TestRecords.CheckerPlayData(userPlayIndex: 2));

        foreach (var ranking in Enum.GetValues<PlayRanking>())
        {
            IDecisionFilterData row = DecisionRow.From(record, ranking);

            Assert.Equal(DecisionKind.CheckerPlay, row.Kind);
            Assert.Equal(record.AfterBoardOfBest(ranking), row.AfterBestBoard);
            Assert.Equal(record.AfterPlayerBoard, row.AfterPlayerBoard);
        }
    }

    [Fact]
    public void DecisionRow_IDecisionFilterData_AfterBoards_Null_CubeDecision()
    {
        // Rewritten from ..._AfterBoards_EmptyByDefault_CubeDecision: a cube
        // row's after-boards are absent, which is null.
        IDecisionFilterData row = CubeRow(error: 0.025);

        Assert.Equal(DecisionKind.Cube, row.Kind);
        Assert.Null(row.AfterBestBoard);
        Assert.Null(row.AfterPlayerBoard);
    }

    // -----------------------------------------------------------------------
    //  Game, MoveNumber (derived from the Id, halheinrich/backgammon#124)
    //  and IsStandardStart
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_GameAndMoveNumber_MatchPosition_KeepTheIdsNumbers()
    {
        // Rewritten from DecisionRow_MoveNumber_RoundTrip: the numbers are the
        // Id's, and round-trip with it.
        var restored = RoundTrip(PlayRow(id: new XgDecisionId("m.xg", Game: 2, MoveNumber: 17, IsCube: false)));

        Assert.Equal(2, restored.Game);
        Assert.Equal(17, restored.MoveNumber);
    }

    [Fact]
    public void DecisionRow_GameAndMoveNumber_StandalonePosition_AreNone()
    {
        // Rewritten from DecisionRow_MoveNumber_AbsentIsRefused: a standalone
        // position has no game or move number, and the row stores no copy of
        // either (halheinrich/backgammon#124).
        var original = PlayRow(id: new XgpDecisionId("position.xgp"));
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;

        Assert.Null(restored.Game);
        Assert.Null(restored.MoveNumber);
        Assert.DoesNotContain("\"Game\"", json);
        Assert.DoesNotContain("\"MoveNumber\"", json);
    }

    [Fact]
    public void DecisionRow_IsStandardStart_RoundTrip()
    {
        Assert.True(RoundTrip(PlayRow(isStandardStart: true)).IsStandardStart);
    }

    [Fact]
    public void DecisionRow_IsStandardStart_StandalonePosition_IsNone()
    {
        // Rewritten from DecisionRow_IsStandardStart_AbsentIsRefused: the
        // column is nullable now — none for a standalone position, which has
        // no game to start (halheinrich/backgammon#124) — and a document
        // that drops it from a game's row is refused as that rule, not as an
        // absent member (DecisionRow_Read_BreakingAGuarantee_IsRefused_BothPaths).
        var standalone = RoundTrip(PlayRow(id: new XgpDecisionId("p.xgp")));

        Assert.Null(standalone.IsStandardStart);
    }

    [Fact]
    public void DecisionRow_ToCsvLine_MoveNumberInColumnOrder()
    {
        var row = PlayRow(
            id: new XgDecisionId("m.xg", Game: 2, MoveNumber: 17, IsCube: false),
            player: "Mochy",
            dice: [6, 3]);
        var line = row.ToCsvLine();
        // CSV header is: ...Player,SourceFile,Game,MoveNumber,Kind,Roll,...
        // Rewritten: the source file is the Id's, never an empty cell.
        Assert.Contains(",Mochy,m.xg,2,17,CheckerPlay,63,", line);
    }

    [Fact]
    public void DecisionRow_ToCsvLine_StandalonePosition_GameAndMoveNumberAreEmptyCells()
    {
        var row = PlayRow(id: new XgpDecisionId("position.xgp"), player: "Mochy", dice: [6, 3]);

        // Neither a 1 nor a 0: no game applies (halheinrich/backgammon#124).
        Assert.Contains(",Mochy,position.xgp,,,CheckerPlay,63,", row.ToCsvLine());
    }

    [Fact]
    public void DecisionRow_IDecisionFilterData_MoveNumberAndIsStandardStart()
    {
        IDecisionFilterData row = PlayRow(
            id: new XgDecisionId("m.xg", Game: 1, MoveNumber: 12, IsCube: false),
            isStandardStart: true);

        Assert.Equal(12, row.MoveNumber);
        Assert.True(row.IsStandardStart);
    }

    [Fact]
    public void DecisionRow_IDecisionFilterData_MoveNumber_StandalonePosition_IsNone()
    {
        // Rewritten from ..._MoveNumberAndIsStandardStart_ForwardZeroAndFalse:
        // a standalone position has no move number and no start
        // (halheinrich/backgammon#124).
        IDecisionFilterData row = PlayRow(id: new XgpDecisionId("test.xgp"));

        Assert.Null(row.MoveNumber);
        Assert.Null(row.IsStandardStart);
    }

    // -----------------------------------------------------------------------
    //  Id — persistent decision identifier
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_Id_RoundTrip_Xgp()
    {
        var original = PlayRow(id: new XgpDecisionId("match.xgp"));
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;

        Assert.Equal(new XgpDecisionId("match.xgp"), restored.Id);
        // Canonical-string JSON shape (bundled DecisionIdJsonConverter), not a polymorphic object.
        Assert.Contains("\"Id\":\"match.xgp\"", json);
    }

    [Fact]
    public void DecisionRow_Id_RoundTrip_Xg()
    {
        var original = PlayRow(id: new XgDecisionId("match.xg", Game: 2, MoveNumber: 17, IsCube: false));
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;

        Assert.Equal(
            new XgDecisionId("match.xg", 2, 17, IsCube: false),
            restored.Id);
        Assert.Contains("\"Id\":\"match.xg:g2:m17:play\"", json);
    }

    [Fact]
    public void DecisionRow_Id_NotInCsvOutput()
    {
        var row = CubeRow(id: new XgDecisionId("match.xg", 2, 17, IsCube: true), player: "Mochy");

        Assert.DoesNotContain("Id", DecisionRow.CsvHeader);
        Assert.DoesNotContain("match.xg:g2:m17:cube", row.ToCsvLine());
    }

    // -----------------------------------------------------------------------
    //  IsJacoby — tri-state field, CSV via MatchScore
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_IsJacoby_NullRoundTrips()
    {
        // Rewritten from DecisionRow_IsJacoby_DefaultsToNull: the unknown rule
        // is the null a producer states, and it round-trips (the absent case
        // is DecisionRow_IsJacoby_AbsentFromJson_ReadsAsNotSupplied).
        var json = JsonSerializer.Serialize(PlayRow(isJacoby: null), Options);
        var row = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;

        Assert.Contains("\"IsJacoby\":null", json);
        Assert.Null(row.IsJacoby);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    public void DecisionRow_RoundTrip_IsJacoby(bool? isJacoby)
    {
        var original = PlayRow(matchLength: 0, onRollNeeds: 0, opponentNeeds: 0, isJacoby: isJacoby);

        var restored = RoundTrip(original);

        Assert.Equal(isJacoby, restored.IsJacoby);
        Assert.Equal(original.MatchScore, restored.MatchScore);
    }

    [Fact]
    public void DecisionRow_IsJacoby_AbsentFromJson_ReadsAsNotSupplied()
    {
        // A money row without the fact reads as unknown.
        var restored = ReadWithout(
            PlayRow(matchLength: 0, onRollNeeds: 0, opponentNeeds: 0, isJacoby: true), "IsJacoby");

        Assert.Null(restored.IsJacoby);
        Assert.Equal("money", restored.MatchScore);
    }

    [Theory]
    [InlineData(true, "moneyJ")]
    [InlineData(false, "moneyNJ")]
    [InlineData(null, "money")]
    public void DecisionRow_IDecisionFilterData_IsJacoby(bool? isJacoby, string expectedScore)
    {
        IDecisionFilterData data = PlayRow(matchLength: 0, onRollNeeds: 0, opponentNeeds: 0, isJacoby: isJacoby);

        Assert.True(data.IsMoneyGame);
        Assert.Equal(isJacoby, data.IsJacoby);
        Assert.Equal(expectedScore, ((DecisionRow)data).MatchScore);
    }

    [Fact]
    public void DecisionRow_IDecisionFilterData_IsJacoby_MatchRow_IsNull()
    {
        IDecisionFilterData data = PlayRow(matchLength: 9);

        Assert.False(data.IsMoneyGame);
        Assert.Null(data.IsJacoby);
    }

    [Theory]
    [InlineData(true, "moneyJ")]
    [InlineData(false, "moneyNJ")]
    [InlineData(null, "money")]
    public void DecisionRow_ToCsvLine_IsJacoby_RidesTheMatchScoreColumn(bool? isJacoby, string expectedToken)
    {
        var row = PlayRow(xgid: "XGID=x", matchLength: 0, onRollNeeds: 0, opponentNeeds: 0, isJacoby: isJacoby);
        var line = row.ToCsvLine();

        // Header: Xgid,Error,MatchScore,MatchLength,... — MatchScore is column
        // index 2 (0-based). No column is added for it; the token carries the
        // fact.
        Assert.Equal(expectedToken, line.Split(',')[2]);
        Assert.DoesNotContain("IsJacoby", DecisionRow.CsvHeader);
        Assert.Equal(12, line.Count(c => c == ','));
        Assert.DoesNotContain("null", line);
    }
}
