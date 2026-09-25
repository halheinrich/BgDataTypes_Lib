using System.Text.Json;
using System.Text.Json.Nodes;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

public class DecisionRowSerializationTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false
    };

    // -----------------------------------------------------------------------
    //  Absence (halheinrich/backgammon#222): the "defaults to" pins below
    //  were rewritten into these two shapes — see
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

    // -----------------------------------------------------------------------
    //  JSON round-trip
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_RoundTrip_CheckerPlay()
    {
        var original = TestRecords.Row(
            id: new XgpDecisionId("test.xgp"),
            xgid: "XGID=-b----E-C---eE---c-e----B-:0:0:1:63:0:0:3:0:10",
            error: 0.023,
            matchLength: 9,
            onRollNeeds: 3,
            opponentNeeds: 5,
            isCrawford: false,
            isJacoby: null,
            player: "Mochy",
            sourceFile: "mochy-falafel.xg",
            game: 2,
            moveNumber: 7,
            roll: 63,
            analysisDepth: "3-ply",
            equity: -0.142,
            board: new BoardPosition([0, 2, 0, 0, 0, 0, -5, 0, -3, 0, 0, 0, 5, 0, 0, 0, 0, -5, 0, -2, 0, 0, 0, 0, 2, 1]));

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;

        Assert.Equal(original.Xgid, restored.Xgid);
        Assert.Equal(original.Error, restored.Error);
        Assert.Equal(original.MatchLength, restored.MatchLength);
        Assert.Equal(original.OnRollNeeds, restored.OnRollNeeds);
        Assert.Equal(original.OpponentNeeds, restored.OpponentNeeds);
        Assert.Equal(original.IsCrawford, restored.IsCrawford);
        Assert.Equal(original.IsJacoby, restored.IsJacoby);
        Assert.Equal(original.Player, restored.Player);
        Assert.Equal(original.SourceFile, restored.SourceFile);
        Assert.Equal(original.Game, restored.Game);
        Assert.Equal(original.MoveNumber, restored.MoveNumber);
        Assert.Equal(original.Roll, restored.Roll);
        Assert.Equal(original.AnalysisDepth, restored.AnalysisDepth);
        Assert.Equal(original.Equity, restored.Equity);
        Assert.Equal(original.Board, restored.Board);
        Assert.False(restored.IsCube);
    }

    [Fact]
    public void DecisionRow_RoundTrip_CubeDecision()
    {
        var original = TestRecords.Row(
            id: new XgpDecisionId("test.xgp"),
            roll: 0,
            equity: 0.312,
            player: "Falafel",
            sourceFile: "mochy-falafel.xg",
            game: 1,
            moveNumber: 3,
            analysisDepth: "Rollout: 1296 trials. 3-ply",
            matchLength: 9,
            onRollNeeds: 1,
            opponentNeeds: 1);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;

        Assert.Equal(0, restored.Roll);
        Assert.True(restored.IsCube);
        Assert.Equal(original.Equity, restored.Equity);
        Assert.Equal(original.AnalysisDepth, restored.AnalysisDepth);
        Assert.False(restored.IsCrawford);
    }

    [Fact]
    public void DecisionRow_RoundTrip_IsCrawford()
    {
        // The flag's true round trip rides a Crawford checker play: a
        // Crawford cube cannot be constructed (DecisionRowCrawfordCubeTests),
        // so the cube round trip above is a post-Crawford 1-away/1-away game.
        var original = TestRecords.Row(
            id: new XgpDecisionId("test.xgp"),
            roll: 52,
            matchLength: 9,
            onRollNeeds: 1,
            opponentNeeds: 3,
            isCrawford: true);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;

        Assert.True(restored.IsCrawford);
        Assert.False(restored.IsCube);
        Assert.Equal("1a3aC", restored.MatchScore);
    }

    [Fact]
    public void DecisionRow_RoundTrip_EmptyStringsAndNullSourceFile()
    {
        // Rewritten from DecisionRow_RoundTrip_StringDefaults: the empty
        // strings are stated values now, and round-trip as such; the
        // nullable SourceFile's null round-trips too.
        var original = TestRecords.Row(
            id: new XgpDecisionId("test.xgp"), roll: 31,
            xgid: "", player: "", analysisDepth: "", sourceFile: null);
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;

        Assert.Equal(string.Empty, restored.Xgid);
        Assert.Equal(string.Empty, restored.Player);
        Assert.Null(restored.SourceFile);
        Assert.Equal(string.Empty, restored.AnalysisDepth);
    }

    [Fact]
    public void DecisionRow_RoundTrip_Board()
    {
        var board = new int[26];
        board[1] = 2; board[6] = -5; board[24] = -2; board[25] = 1;

        var original = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, board: new BoardPosition(board));
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;

        Assert.Equal(original.Board, restored.Board);
    }

    [Fact]
    public void DecisionRow_RoundTrip_MatchScoreNotSerialized()
    {
        var original = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, matchLength: 9, onRollNeeds: 3, opponentNeeds: 5);
        var json = JsonSerializer.Serialize(original, Options);

        Assert.DoesNotContain("MatchScore", json);

        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;
        Assert.Equal("3a5a", restored.MatchScore);
    }

    // -----------------------------------------------------------------------
    //  AnalysisMode / AnalysisLevel — JSON only, excluded from CSV
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_AnalysisModeAndLevel_RoundTrip()
    {
        var original = TestRecords.Row(
            id: new XgpDecisionId("test.xgp"),
            roll: 31,
            analysisDepth: "XG Roller++",
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
        AssertAbsentIsRefused(TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31),
            "AnalysisMode", "AnalysisLevel");
    }

    [Fact]
    public void DecisionRow_LegacyAnalysisDepthClassJson_IsRefused_TheRetiredPropertyStillIgnored()
    {
        // Rewritten from DecisionRow_LegacyAnalysisDepthClassJson_DeserializesToUnknownPair.
        // JSON written before the two-axis pair existed lacks it; the pair is
        // required now (halheinrich/backgammon#222), so such a document is
        // refused rather than read as "depth not recorded". The retired flat
        // "AnalysisDepthClass" property is still ignored beside a full row.
        var legacy = "{\"Id\":\"test.xgp\",\"AnalysisDepth\":\"3-ply\",\"AnalysisDepthClass\":\"Ply3\",\"Roll\":63}";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DecisionRow>(legacy, Options));

        var full = JsonNode.Parse(JsonSerializer.Serialize(
            TestRecords.Row(roll: 63, analysisDepth: "3-ply"), Options))!.AsObject();
        full["AnalysisDepthClass"] = "Ply3";
        var restored = JsonSerializer.Deserialize<DecisionRow>(full.ToJsonString(), Options)!;

        Assert.Equal(AnalysisMode.Unknown, restored.AnalysisMode);
        Assert.Equal(AnalysisLevel.Unknown, restored.AnalysisLevel);
        Assert.Equal("3-ply", restored.AnalysisDepth);
    }

    [Fact]
    public void DecisionRow_AnalysisModeAndLevel_NotInCsvOutput()
    {
        var row = TestRecords.Row(
            id: new XgpDecisionId("test.xgp"),
            roll: 31,
            analysisDepth: "3-ply",
            analysisMode: AnalysisMode.Evaluation,
            analysisLevel: AnalysisLevel.Ply3);

        Assert.DoesNotContain("AnalysisMode", DecisionRow.CsvHeader);
        Assert.DoesNotContain("AnalysisLevel", DecisionRow.CsvHeader);
        // Column count unchanged: 11 columns → 10 commas.
        Assert.Equal(10, row.ToCsvLine().Count(c => c == ','));
    }

    [Fact]
    public void DecisionRow_IDecisionFilterData_AnalysisModeAndLevel()
    {
        IDecisionFilterData row = TestRecords.Row(
            id: new XgpDecisionId("test.xgp"),
            roll: 31,
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
        var row = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, matchLength: 0, onRollNeeds: 0, opponentNeeds: 0);

        Assert.True(row.IsMoneyGame);
        // IsJacoby unset, so the bare (rule-unknown) money token.
        Assert.Equal("money", row.MatchScore);
    }

    [Fact]
    public void DecisionRow_IsMoneyGame_OnePointMatch()
    {
        // The shortest possible match — the boundary case next to money's 0.
        var row = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, matchLength: 1, onRollNeeds: 1, opponentNeeds: 1);

        Assert.False(row.IsMoneyGame);
        Assert.Equal("1a1a", row.MatchScore);
    }

    [Fact]
    public void DecisionRow_IsMoneyGame_StandardMatch()
    {
        var row = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, matchLength: 9, onRollNeeds: 3, opponentNeeds: 5);

        Assert.False(row.IsMoneyGame);
        Assert.Equal("3a5a", row.MatchScore);
    }

    [Fact]
    public void DecisionRow_IsMoneyGame_NotSerialized_MatchLengthRemainsTheWire()
    {
        var original = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, matchLength: 0);
        var json = JsonSerializer.Serialize(original, Options);

        Assert.DoesNotContain("IsMoneyGame", json);
        Assert.Contains("\"MatchLength\":0", json);

        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;
        Assert.True(restored.IsMoneyGame);
    }

    [Fact]
    public void DecisionRow_IsMoneyGame_NotInCsvOutput()
    {
        var row = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, matchLength: 0);

        Assert.DoesNotContain("IsMoneyGame", DecisionRow.CsvHeader);
        // Column count unchanged: 11 columns → 10 commas.
        Assert.Equal(10, row.ToCsvLine().Count(c => c == ','));
    }

    [Fact]
    public void DecisionRow_IDecisionFilterData_IsMoneyGame()
    {
        IDecisionFilterData money = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, matchLength: 0);
        IDecisionFilterData match = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, matchLength: 9);

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
        var row = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, matchLength: 0, onRollNeeds: 0, opponentNeeds: 0);
        Assert.Equal("money", row.MatchScore);
    }

    [Fact]
    public void DecisionRow_MatchScore_Money_Jacoby()
    {
        var row = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, matchLength: 0, onRollNeeds: 0, opponentNeeds: 0, isJacoby: true);
        Assert.Equal("moneyJ", row.MatchScore);
    }

    [Fact]
    public void DecisionRow_MatchScore_Money_NoJacoby()
    {
        var row = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, matchLength: 0, onRollNeeds: 0, opponentNeeds: 0, isJacoby: false);
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
        var row = TestRecords.Row(
            id: new XgpDecisionId("test.xgp"),
            roll: 31,
            matchLength: 9,
            onRollNeeds: 3,
            opponentNeeds: 5,
            isJacoby: isJacoby);
        Assert.Equal("3a5a", row.MatchScore);
    }

    [Fact]
    public void DecisionRow_MatchScore_Standard()
    {
        var row = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, matchLength: 9, onRollNeeds: 3, opponentNeeds: 5);
        Assert.Equal("3a5a", row.MatchScore);
    }

    [Fact]
    public void DecisionRow_MatchScore_Crawford()
    {
        var row = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, matchLength: 9, onRollNeeds: 1, opponentNeeds: 1, isCrawford: true);
        Assert.Equal("1a1aC", row.MatchScore);
    }

    // -----------------------------------------------------------------------
    //  CSV
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_CsvHeader_ContainsExpectedColumns()
    {
        Assert.Equal(
            "Xgid,Error,MatchScore,MatchLength,Player,SourceFile,Game,MoveNumber,Roll,AnalysisDepth,Equity",
            DecisionRow.CsvHeader);
    }

    [Fact]
    public void DecisionRow_ToCsvLine_EscapesCommas()
    {
        var row = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, matchLength: 9, onRollNeeds: 3, opponentNeeds: 5, player: "Last, First");
        var line = row.ToCsvLine();
        Assert.Contains("\"Last, First\"", line);
    }

    [Fact]
    public void DecisionRow_ToCsvLine_EscapesQuotes()
    {
        var row = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, player: "say \"hello\"");
        var line = row.ToCsvLine();
        Assert.Contains("\"say \"\"hello\"\"\"", line);
    }

    [Fact]
    public void DecisionRow_ToCsvLine_DoublesFormattedG6()
    {
        var row = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, error: 0.12345678, equity: -0.98765432);
        var line = row.ToCsvLine();
        Assert.Contains("0.123457", line);
        Assert.Contains("-0.987654", line);
    }

    [Fact]
    public void DecisionRow_ToCsvLine_BoardNotInCsv()
    {
        var row = TestRecords.Row(
            id: new XgpDecisionId("test.xgp"),
            roll: 31,
            board: new BoardPosition([0, 2, 0, 0, 0, 0, -5, 0, -3, 0, 0, 0, 5, 0, 0, 0, 0, -5, 0, -2, 0, 0, 0, 0, 2, 1]));
        var line = row.ToCsvLine();
        Assert.Equal(10, line.Count(c => c == ','));
    }

    // -----------------------------------------------------------------------
    //  SourceFile — field and CSV behaviour
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_SourceFile_AbsentReadsAsNull()
    {
        // Rewritten from DecisionRow_SourceFile_DefaultsToNull: nullable (none
        // recorded), so its absence reads as null.
        var row = ReadWithout(
            TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, sourceFile: "m.xg"), "SourceFile");
        Assert.Null(row.SourceFile);
    }

    [Fact]
    public void DecisionRow_ToCsvLine_SourceFile_EmptyCellWhenNull()
    {
        var row = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, player: "Mochy", sourceFile: null);
        var line = row.ToCsvLine();

        // Header: Xgid,Error,MatchScore,MatchLength,Player,SourceFile,Game,MoveNumber,Roll,AnalysisDepth,Equity
        // SourceFile is column index 5 (0-based). Between Player and Game it must appear
        // as ",," — an empty cell — not the literal "null".
        Assert.Contains(",Mochy,,", line);
        Assert.DoesNotContain("null", line);
    }

    [Fact]
    public void DecisionRow_ToCsvLine_SourceFile_PlainFilename()
    {
        var row = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, sourceFile: "mochy-falafel.xg");
        var line = row.ToCsvLine();
        Assert.Contains(",mochy-falafel.xg,", line);
    }

    [Fact]
    public void DecisionRow_ToCsvLine_SourceFile_FilenameWithSpaces_Unquoted()
    {
        var row = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, sourceFile: "Mochy vs Falafel.xgp");
        var line = row.ToCsvLine();
        // RFC 4180 does not require quoting on spaces; value passes through literally.
        Assert.Contains(",Mochy vs Falafel.xgp,", line);
    }

    [Fact]
    public void DecisionRow_ToCsvLine_SourceFile_FilenameWithComma_Quoted()
    {
        var row = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, sourceFile: "file,with,commas.xg");
        var line = row.ToCsvLine();
        Assert.Contains("\"file,with,commas.xg\"", line);
    }

    [Fact]
    public void DecisionRow_RoundTrip_SourceFile()
    {
        var original = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, sourceFile: "mochy-falafel.xg");
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;
        Assert.Equal("mochy-falafel.xg", restored.SourceFile);
    }

    // -----------------------------------------------------------------------
    //  IDecisionFilterData — DecisionRow
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_IDecisionFilterData_CheckerPlay()
    {
        var board = new BoardPosition([0, 2, 0, 0, 0, 0, -5, 0, -3, 0, 0, 0, 5, 0, 0, 0, 0, -5, 0, -2, 0, 0, 0, 0, 2, 1]);
        IDecisionFilterData row = TestRecords.Row(
            id: new XgpDecisionId("test.xgp"),
            player: "Mochy",
            roll: 63,
            matchLength: 9,
            onRollNeeds: 3,
            opponentNeeds: 5,
            error: 0.023,
            board: board);

        Assert.Equal("Mochy", row.Player);
        Assert.False(row.IsCube);
        Assert.Equal(3, row.OnRollNeeds);
        Assert.Equal(5, row.OpponentNeeds);
        Assert.False(row.IsCrawford);
        Assert.Equal(0.023, row.FilterError);
        // Rewritten from a 26-element count check: the layout is the type's
        // now, so the view forwards the board itself.
        Assert.Equal(board, row.Board);
    }

    [Fact]
    public void DecisionRow_IDecisionFilterData_CubeDecision()
    {
        IDecisionFilterData row = TestRecords.Row(
            id: new XgpDecisionId("test.xgp"),
            player: "Falafel",
            roll: 0,
            matchLength: 9,
            onRollNeeds: 1,
            opponentNeeds: 1,
            error: 0.011);

        Assert.True(row.IsCube);
        Assert.False(row.IsCrawford);
        Assert.Equal(0.011, row.FilterError);
    }

    [Fact]
    public void DecisionRow_IDecisionFilterData_IsCrawford_Forwards()
    {
        // The flag forwards from a record that can exist — a Crawford
        // checker play; the cube case above is post-Crawford by necessity.
        IDecisionFilterData row = TestRecords.Row(
            id: new XgpDecisionId("test.xgp"),
            roll: 52,
            matchLength: 9,
            onRollNeeds: 1,
            opponentNeeds: 3,
            isCrawford: true);

        Assert.False(row.IsCube);
        Assert.True(row.IsCrawford);
    }

    [Fact]
    public void DecisionRow_IDecisionFilterData_FilterError_IsNullableDouble()
    {
        IDecisionFilterData row = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, error: 0.045);
        double? fe = row.FilterError;
        Assert.NotNull(fe);
        Assert.Equal(0.045, fe!.Value);
    }

    // -----------------------------------------------------------------------
    //  Dice — canonical roll derived from Roll
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_Dice_CheckerPlay_DerivedFromRoll()
    {
        var row = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 63);

        Assert.Equal(new DiceRoll(6, 3), row.Dice);
    }

    [Fact]
    public void DecisionRow_Dice_RolledOrderCanonicalized()
    {
        // The XG parser stamps dice in rolled order, so Roll carries both
        // spellings of a 3-1 (31 and 13); Dice canonicalizes high-first.
        var row = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 13);

        Assert.Equal(new DiceRoll(3, 1), row.Dice);
        Assert.Equal(3, row.Dice!.Value.High);
        Assert.Equal(1, row.Dice!.Value.Low);
    }

    [Fact]
    public void DecisionRow_Dice_CubeDecision_IsNull()
    {
        var row = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 0);

        Assert.True(row.IsCube);
        Assert.Null(row.Dice);
    }

    [Theory]
    // Digits outside 1–6, or not two digits at all — corrupt data fails loud.
    [InlineData(70)]
    [InlineData(7)]
    [InlineData(-13)]
    [InlineData(315)]
    public void DecisionRow_Dice_MalformedRoll_Throws(int badRoll)
    {
        var row = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: badRoll);

        Assert.Throws<ArgumentOutOfRangeException>(() => row.Dice);
    }

    [Fact]
    public void DecisionRow_Dice_NotSerialized_RollRemainsTheWire()
    {
        var original = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 63);
        var json = JsonSerializer.Serialize(original, Options);

        Assert.DoesNotContain("\"Dice\"", json);
        Assert.Contains("\"Roll\":63", json);

        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;
        Assert.Equal(new DiceRoll(6, 3), restored.Dice);
    }

    [Fact]
    public void DecisionRow_Dice_NotInCsvOutput()
    {
        var row = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 63);

        Assert.DoesNotContain("Dice", DecisionRow.CsvHeader);
        // Column count unchanged: 11 columns → 10 commas.
        Assert.Equal(10, row.ToCsvLine().Count(c => c == ','));
    }

    [Fact]
    public void DecisionRow_IDecisionFilterData_Dice()
    {
        IDecisionFilterData play = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 52);
        IDecisionFilterData cube = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 0);

        Assert.Equal(new DiceRoll(5, 2), play.Dice);
        Assert.Null(cube.Dice);
    }

    // -----------------------------------------------------------------------
    //  After-boards — JSON only, excluded from CSV (matches Board)
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_AfterBoards_AbsentAreNull()
    {
        // Rewritten from DecisionRow_AfterBoards_DefaultToEmpty: an absent
        // after-board is null, never an empty list (halheinrich/backgammon#15).
        var row = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31);
        Assert.Null(row.AfterBestBoard);
        Assert.Null(row.AfterPlayerBoard);
    }

    [Fact]
    public void DecisionRow_RoundTrip_AfterBoards()
    {
        var best = new int[26];
        best[1] = 2; best[6] = -5; best[20] = -2;
        var player = new int[26];
        player[1] = 2; player[6] = -5; player[19] = -2;

        var original = TestRecords.Row(
            id: new XgpDecisionId("test.xgp"),
            xgid: "XGID=-b----E-C---eE---c-e----B-:0:0:1:63:0:0:3:0:10",
            error: 0.018,
            roll: 63,
            player: "Mochy",
            afterBestBoard: new BoardPosition(best),
            afterPlayerBoard: new BoardPosition(player));

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;

        Assert.Equal(original.AfterBestBoard, restored.AfterBestBoard);
        Assert.Equal(original.AfterPlayerBoard, restored.AfterPlayerBoard);
    }

    [Fact]
    public void DecisionRow_ToCsvLine_AfterBoardsNotInCsv()
    {
        var best = new int[26];
        best[1] = 2; best[6] = -5;

        var row = TestRecords.Row(
            id: new XgpDecisionId("test.xgp"),
            roll: 31,
            board: new BoardPosition([0, 2, 0, 0, 0, 0, -5, 0, -3, 0, 0, 0, 5, 0, 0, 0, 0, -5, 0, -2, 0, 0, 0, 0, 2, 1]),
            afterBestBoard: new BoardPosition(best),
            afterPlayerBoard: new BoardPosition(best));

        var line = row.ToCsvLine();
        // Column count unchanged: 11 columns → 10 commas.
        Assert.Equal(10, line.Count(c => c == ','));
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
        var best = new int[26];
        best[1] = 2; best[20] = -2;
        var player = new int[26];
        player[1] = 2; player[19] = -2;

        IDecisionFilterData row = TestRecords.Row(
            id: new XgpDecisionId("test.xgp"),
            roll: 63,
            afterBestBoard: new BoardPosition(best),
            afterPlayerBoard: new BoardPosition(player));

        Assert.False(row.IsCube);
        Assert.Equal(new BoardPosition(best), row.AfterBestBoard);
        Assert.Equal(new BoardPosition(player), row.AfterPlayerBoard);
    }

    [Fact]
    public void DecisionRow_IDecisionFilterData_AfterBoards_Null_CubeDecision()
    {
        // Rewritten from ..._AfterBoards_EmptyByDefault_CubeDecision: a cube
        // row's after-boards are absent, which is null.
        IDecisionFilterData row = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 0, error: 0.025);

        Assert.True(row.IsCube);
        Assert.Null(row.AfterBestBoard);
        Assert.Null(row.AfterPlayerBoard);
    }

    // -----------------------------------------------------------------------
    //  MoveNumber and IsStandardStart
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_MoveNumber_RoundTrip()
    {
        var original = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, moveNumber: 17);
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;
        Assert.Equal(17, restored.MoveNumber);
    }

    [Fact]
    public void DecisionRow_MoveNumber_AbsentIsRefused()
    {
        // Rewritten from DecisionRow_MoveNumber_DefaultsToZero.
        AssertAbsentIsRefused(TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, moveNumber: 12),
            "MoveNumber");
    }

    [Fact]
    public void DecisionRow_IsStandardStart_RoundTrip()
    {
        var original = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, isStandardStart: true);
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;
        Assert.True(restored.IsStandardStart);
    }

    [Fact]
    public void DecisionRow_IsStandardStart_AbsentIsRefused()
    {
        // Rewritten from DecisionRow_IsStandardStart_DefaultsToFalse.
        AssertAbsentIsRefused(TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, isStandardStart: true),
            "IsStandardStart");
    }

    [Fact]
    public void DecisionRow_ToCsvLine_MoveNumberInColumnOrder()
    {
        var row = TestRecords.Row(
            id: new XgpDecisionId("test.xgp"),
            player: "Mochy",
            game: 2,
            moveNumber: 17,
            roll: 63);
        var line = row.ToCsvLine();
        // CSV header is: ...Player,SourceFile,Game,MoveNumber,Roll,...
        // SourceFile is null → empty cell. Expect "Mochy,,2,17,63,"
        Assert.Contains(",Mochy,,2,17,63,", line);
    }

    [Fact]
    public void DecisionRow_IDecisionFilterData_MoveNumberAndIsStandardStart()
    {
        IDecisionFilterData row = TestRecords.Row(
            id: new XgpDecisionId("test.xgp"),
            roll: 31,
            moveNumber: 12,
            isStandardStart: true);

        Assert.Equal(12, row.MoveNumber);
        Assert.True(row.IsStandardStart);
    }

    [Fact]
    public void DecisionRow_IDecisionFilterData_MoveNumberAndIsStandardStart_ForwardZeroAndFalse()
    {
        // Rewritten from ..._MoveNumberAndIsStandardStart_Defaults: the view
        // forwards the stated zero and false, which are values now.
        IDecisionFilterData row = TestRecords.Row(
            id: new XgpDecisionId("test.xgp"), roll: 31, moveNumber: 0, isStandardStart: false);

        Assert.Equal(0, row.MoveNumber);
        Assert.False(row.IsStandardStart);
    }

    // -----------------------------------------------------------------------
    //  Id — persistent decision identifier
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_Id_RoundTrip_Xgp()
    {
        var original = TestRecords.Row(id: new XgpDecisionId("match.xgp"), roll: 31);
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;

        Assert.Equal(new XgpDecisionId("match.xgp"), restored.Id);
        // Canonical-string JSON shape (bundled DecisionIdJsonConverter), not a polymorphic object.
        Assert.Contains("\"Id\":\"match.xgp\"", json);
    }

    [Fact]
    public void DecisionRow_Id_RoundTrip_Xg()
    {
        var original = TestRecords.Row(
            id: new XgDecisionId("match.xg", Game: 2, MoveNumber: 17, IsCube: false),
            roll: 31);
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
        var row = TestRecords.Row(
            id: new XgDecisionId("match.xg", 2, 17, IsCube: true),
            roll: 0,
            player: "Mochy");

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
        var json = JsonSerializer.Serialize(
            TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, isJacoby: null), Options);
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
        var original = TestRecords.Row(
            id: new XgpDecisionId("test.xgp"),
            roll: 31,
            matchLength: 0,
            isJacoby: isJacoby);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;

        Assert.Equal(isJacoby, restored.IsJacoby);
        Assert.Equal(original.MatchScore, restored.MatchScore);
    }

    [Fact]
    public void DecisionRow_IsJacoby_AbsentFromJson_ReadsAsNotSupplied()
    {
        // A row written before the fact existed still reads — as unknown.
        // Rewritten onto a full money row without the member: every other
        // member is required now.
        var restored = ReadWithout(
            TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, matchLength: 0, isJacoby: true),
            "IsJacoby");

        Assert.Null(restored.IsJacoby);
        Assert.Equal("money", restored.MatchScore);
    }

    [Theory]
    [InlineData(true, "moneyJ")]
    [InlineData(false, "moneyNJ")]
    [InlineData(null, "money")]
    public void DecisionRow_IDecisionFilterData_IsJacoby(bool? isJacoby, string expectedScore)
    {
        IDecisionFilterData data = TestRecords.Row(
            id: new XgpDecisionId("test.xgp"),
            roll: 31,
            matchLength: 0,
            isJacoby: isJacoby);

        Assert.True(data.IsMoneyGame);
        Assert.Equal(isJacoby, data.IsJacoby);
        Assert.Equal(expectedScore, ((DecisionRow)data).MatchScore);
    }

    [Fact]
    public void DecisionRow_IDecisionFilterData_IsJacoby_MatchRow_IsNull()
    {
        IDecisionFilterData data = TestRecords.Row(id: new XgpDecisionId("test.xgp"), roll: 31, matchLength: 9);

        Assert.False(data.IsMoneyGame);
        Assert.Null(data.IsJacoby);
    }

    [Theory]
    [InlineData(true, "moneyJ")]
    [InlineData(false, "moneyNJ")]
    [InlineData(null, "money")]
    public void DecisionRow_ToCsvLine_IsJacoby_RidesTheMatchScoreColumn(bool? isJacoby, string expectedToken)
    {
        var row = TestRecords.Row(
            id: new XgpDecisionId("test.xgp"),
            roll: 31,
            xgid: "XGID=x",
            matchLength: 0,
            isJacoby: isJacoby);
        var line = row.ToCsvLine();

        // Header: Xgid,Error,MatchScore,MatchLength,... — MatchScore is column
        // index 2 (0-based). No column is added; the token carries the fact.
        Assert.Equal(expectedToken, line.Split(',')[2]);
        Assert.DoesNotContain("IsJacoby", DecisionRow.CsvHeader);
        Assert.Equal(10, line.Count(c => c == ','));
        Assert.DoesNotContain("null", line);
    }
}
