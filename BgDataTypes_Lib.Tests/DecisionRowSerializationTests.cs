using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

public class DecisionRowSerializationTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false
    };

    // -----------------------------------------------------------------------
    //  JSON round-trip
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_RoundTrip_CheckerPlay()
    {
        var original = new DecisionRow
        {
            Id = new XgpDecisionId("test.xgp"),
            Xgid = "XGID=-b----E-C---eE---c-e----B-:0:0:1:63:0:0:3:0:10",
            Error = 0.023,
            MatchLength = 9,
            OnRollNeeds = 3,
            OpponentNeeds = 5,
            IsCrawford = false,
            Player = "Mochy",
            SourceFile = "mochy-falafel.xg",
            Game = 2,
            MoveNumber = 7,
            Roll = 63,
            AnalysisDepth = "3-ply",
            Equity = -0.142,
            Board = [0, 2, 0, 0, 0, 0, -5, 0, -3, 0, 0, 0, 5, 0, 0, 0, 0, -5, 0, -2, 0, 0, 0, 0, 2, 1]
        };

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;

        Assert.Equal(original.Xgid, restored.Xgid);
        Assert.Equal(original.Error, restored.Error);
        Assert.Equal(original.MatchLength, restored.MatchLength);
        Assert.Equal(original.OnRollNeeds, restored.OnRollNeeds);
        Assert.Equal(original.OpponentNeeds, restored.OpponentNeeds);
        Assert.Equal(original.IsCrawford, restored.IsCrawford);
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
        var original = new DecisionRow
        {
            Id = new XgpDecisionId("test.xgp"),
            Roll = 0,
            Equity = 0.312,
            Player = "Falafel",
            SourceFile = "mochy-falafel.xg",
            Game = 1,
            MoveNumber = 3,
            AnalysisDepth = "Rollout: 1296 trials. 3-ply",
            MatchLength = 9,
            OnRollNeeds = 1,
            OpponentNeeds = 1,
            IsCrawford = true
        };

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;

        Assert.Equal(0, restored.Roll);
        Assert.True(restored.IsCube);
        Assert.Equal(original.Equity, restored.Equity);
        Assert.Equal(original.AnalysisDepth, restored.AnalysisDepth);
        Assert.True(restored.IsCrawford);
    }

    [Fact]
    public void DecisionRow_RoundTrip_StringDefaults()
    {
        var original = new DecisionRow { Id = new XgpDecisionId("test.xgp") };
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

        var original = new DecisionRow { Id = new XgpDecisionId("test.xgp"), Board = board };
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;

        Assert.Equal(original.Board, restored.Board);
    }

    [Fact]
    public void DecisionRow_RoundTrip_MatchScoreNotSerialized()
    {
        var original = new DecisionRow { Id = new XgpDecisionId("test.xgp"), MatchLength = 9, OnRollNeeds = 3, OpponentNeeds = 5 };
        var json = JsonSerializer.Serialize(original, Options);

        Assert.DoesNotContain("MatchScore", json);

        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;
        Assert.Equal("3a5a", restored.MatchScore);
    }

    // -----------------------------------------------------------------------
    //  AnalysisDepthClass — JSON only, excluded from CSV
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_AnalysisDepthClass_RoundTrip()
    {
        var original = new DecisionRow
        {
            Id = new XgpDecisionId("test.xgp"),
            AnalysisDepth = "XG Roller++",
            AnalysisDepthClass = AnalysisDepthClass.XgRollerPlusPlus
        };

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;

        // String form via the enum's bundled converter — no options-level registration.
        Assert.Contains("\"AnalysisDepthClass\":\"XgRollerPlusPlus\"", json);
        Assert.Equal(AnalysisDepthClass.XgRollerPlusPlus, restored.AnalysisDepthClass);
    }

    [Fact]
    public void DecisionRow_AnalysisDepthClass_DefaultsToUnknown()
    {
        var row = new DecisionRow { Id = new XgpDecisionId("test.xgp") };
        Assert.Equal(AnalysisDepthClass.Unknown, row.AnalysisDepthClass);
    }

    [Fact]
    public void DecisionRow_AnalysisDepthClass_MissingInLegacyJson_DeserializesToUnknown()
    {
        // JSON written before AnalysisDepthClass existed carries no such field.
        var json = "{\"Id\":\"test.xgp\",\"AnalysisDepth\":\"3-ply\",\"Roll\":63}";
        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;

        Assert.Equal(AnalysisDepthClass.Unknown, restored.AnalysisDepthClass);
        Assert.Equal("3-ply", restored.AnalysisDepth);
    }

    [Fact]
    public void DecisionRow_AnalysisDepthClass_NotInCsvOutput()
    {
        var row = new DecisionRow
        {
            Id = new XgpDecisionId("test.xgp"),
            AnalysisDepth = "3-ply",
            AnalysisDepthClass = AnalysisDepthClass.Ply3
        };

        Assert.DoesNotContain("AnalysisDepthClass", DecisionRow.CsvHeader);
        // Column count unchanged: 11 columns → 10 commas.
        Assert.Equal(10, row.ToCsvLine().Count(c => c == ','));
    }

    [Fact]
    public void DecisionRow_IDecisionFilterData_AnalysisDepthClass()
    {
        IDecisionFilterData row = new DecisionRow
        {
            Id = new XgpDecisionId("test.xgp"),
            AnalysisDepthClass = AnalysisDepthClass.Rollout
        };

        Assert.Equal(AnalysisDepthClass.Rollout, row.AnalysisDepthClass);
    }

    // -----------------------------------------------------------------------
    //  MatchScore reconstruction
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_MatchScore_Money()
    {
        var row = new DecisionRow { Id = new XgpDecisionId("test.xgp"), MatchLength = 0, OnRollNeeds = 0, OpponentNeeds = 0 };
        Assert.Equal("money", row.MatchScore);
    }

    [Fact]
    public void DecisionRow_MatchScore_Standard()
    {
        var row = new DecisionRow { Id = new XgpDecisionId("test.xgp"), MatchLength = 9, OnRollNeeds = 3, OpponentNeeds = 5 };
        Assert.Equal("3a5a", row.MatchScore);
    }

    [Fact]
    public void DecisionRow_MatchScore_Crawford()
    {
        var row = new DecisionRow { Id = new XgpDecisionId("test.xgp"), MatchLength = 9, OnRollNeeds = 1, OpponentNeeds = 1, IsCrawford = true };
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
        var row = new DecisionRow { Id = new XgpDecisionId("test.xgp"), MatchLength = 9, OnRollNeeds = 3, OpponentNeeds = 5, Player = "Last, First" };
        var line = row.ToCsvLine();
        Assert.Contains("\"Last, First\"", line);
    }

    [Fact]
    public void DecisionRow_ToCsvLine_EscapesQuotes()
    {
        var row = new DecisionRow { Id = new XgpDecisionId("test.xgp"), Player = "say \"hello\"" };
        var line = row.ToCsvLine();
        Assert.Contains("\"say \"\"hello\"\"\"", line);
    }

    [Fact]
    public void DecisionRow_ToCsvLine_DoublesFormattedG6()
    {
        var row = new DecisionRow { Id = new XgpDecisionId("test.xgp"), Error = 0.12345678, Equity = -0.98765432 };
        var line = row.ToCsvLine();
        Assert.Contains("0.123457", line);
        Assert.Contains("-0.987654", line);
    }

    [Fact]
    public void DecisionRow_ToCsvLine_BoardNotInCsv()
    {
        var row = new DecisionRow
        {
            Id = new XgpDecisionId("test.xgp"),
            Board = [0, 2, 0, 0, 0, 0, -5, 0, -3, 0, 0, 0, 5, 0, 0, 0, 0, -5, 0, -2, 0, 0, 0, 0, 2, 1]
        };
        var line = row.ToCsvLine();
        Assert.Equal(10, line.Count(c => c == ','));
    }

    // -----------------------------------------------------------------------
    //  SourceFile — field and CSV behaviour
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_SourceFile_DefaultsToNull()
    {
        var row = new DecisionRow { Id = new XgpDecisionId("test.xgp") };
        Assert.Null(row.SourceFile);
    }

    [Fact]
    public void DecisionRow_ToCsvLine_SourceFile_EmptyCellWhenNull()
    {
        var row = new DecisionRow { Id = new XgpDecisionId("test.xgp"), Player = "Mochy", SourceFile = null };
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
        var row = new DecisionRow { Id = new XgpDecisionId("test.xgp"), SourceFile = "mochy-falafel.xg" };
        var line = row.ToCsvLine();
        Assert.Contains(",mochy-falafel.xg,", line);
    }

    [Fact]
    public void DecisionRow_ToCsvLine_SourceFile_FilenameWithSpaces_Unquoted()
    {
        var row = new DecisionRow { Id = new XgpDecisionId("test.xgp"), SourceFile = "Mochy vs Falafel.xgp" };
        var line = row.ToCsvLine();
        // RFC 4180 does not require quoting on spaces; value passes through literally.
        Assert.Contains(",Mochy vs Falafel.xgp,", line);
    }

    [Fact]
    public void DecisionRow_ToCsvLine_SourceFile_FilenameWithComma_Quoted()
    {
        var row = new DecisionRow { Id = new XgpDecisionId("test.xgp"), SourceFile = "file,with,commas.xg" };
        var line = row.ToCsvLine();
        Assert.Contains("\"file,with,commas.xg\"", line);
    }

    [Fact]
    public void DecisionRow_RoundTrip_SourceFile()
    {
        var original = new DecisionRow { Id = new XgpDecisionId("test.xgp"), SourceFile = "mochy-falafel.xg" };
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
        IDecisionFilterData row = new DecisionRow
        {
            Id = new XgpDecisionId("test.xgp"),
            Player = "Mochy",
            Roll = 63,
            MatchLength = 9,
            OnRollNeeds = 3,
            OpponentNeeds = 5,
            Error = 0.023,
            Board = [0, 2, 0, 0, 0, 0, -5, 0, -3, 0, 0, 0, 5, 0, 0, 0, 0, -5, 0, -2, 0, 0, 0, 0, 2, 1]
        };

        Assert.Equal("Mochy", row.Player);
        Assert.False(row.IsCube);
        Assert.Equal(3, row.OnRollNeeds);
        Assert.Equal(5, row.OpponentNeeds);
        Assert.False(row.IsCrawford);
        Assert.Equal(0.023, row.FilterError);
        Assert.Equal(26, row.Board.Count);
    }

    [Fact]
    public void DecisionRow_IDecisionFilterData_CubeDecision()
    {
        IDecisionFilterData row = new DecisionRow
        {
            Id = new XgpDecisionId("test.xgp"),
            Player = "Falafel",
            Roll = 0,
            MatchLength = 9,
            OnRollNeeds = 1,
            OpponentNeeds = 1,
            IsCrawford = true,
            Error = 0.011
        };

        Assert.True(row.IsCube);
        Assert.True(row.IsCrawford);
        Assert.Equal(0.011, row.FilterError);
    }

    [Fact]
    public void DecisionRow_IDecisionFilterData_FilterError_IsNullableDouble()
    {
        IDecisionFilterData row = new DecisionRow { Id = new XgpDecisionId("test.xgp"), Error = 0.045 };
        double? fe = row.FilterError;
        Assert.NotNull(fe);
        Assert.Equal(0.045, fe!.Value);
    }

    // -----------------------------------------------------------------------
    //  After-boards — JSON only, excluded from CSV (matches Board)
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_AfterBoards_DefaultToEmpty()
    {
        var row = new DecisionRow { Id = new XgpDecisionId("test.xgp") };
        Assert.Empty(row.AfterBestBoard);
        Assert.Empty(row.AfterPlayerBoard);
    }

    [Fact]
    public void DecisionRow_RoundTrip_AfterBoards()
    {
        var best = new int[26];
        best[1] = 2; best[6] = -5; best[20] = -2;
        var player = new int[26];
        player[1] = 2; player[6] = -5; player[19] = -2;

        var original = new DecisionRow
        {
            Id = new XgpDecisionId("test.xgp"),
            Xgid = "XGID=-b----E-C---eE---c-e----B-:0:0:1:63:0:0:3:0:10",
            Error = 0.018,
            Roll = 63,
            Player = "Mochy",
            AfterBestBoard = best,
            AfterPlayerBoard = player
        };

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

        var row = new DecisionRow
        {
            Id = new XgpDecisionId("test.xgp"),
            Board = [0, 2, 0, 0, 0, 0, -5, 0, -3, 0, 0, 0, 5, 0, 0, 0, 0, -5, 0, -2, 0, 0, 0, 0, 2, 1],
            AfterBestBoard = best,
            AfterPlayerBoard = best
        };

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

        IDecisionFilterData row = new DecisionRow
        {
            Id = new XgpDecisionId("test.xgp"),
            Roll = 63,
            AfterBestBoard = best,
            AfterPlayerBoard = player
        };

        Assert.False(row.IsCube);
        Assert.Equal(best, row.AfterBestBoard);
        Assert.Equal(player, row.AfterPlayerBoard);
    }

    [Fact]
    public void DecisionRow_IDecisionFilterData_AfterBoards_EmptyByDefault_CubeDecision()
    {
        IDecisionFilterData row = new DecisionRow { Id = new XgpDecisionId("test.xgp"), Roll = 0, Error = 0.025 };

        Assert.True(row.IsCube);
        Assert.Empty(row.AfterBestBoard);
        Assert.Empty(row.AfterPlayerBoard);
    }

    // -----------------------------------------------------------------------
    //  MoveNumber and IsStandardStart
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_MoveNumber_RoundTrip()
    {
        var original = new DecisionRow { Id = new XgpDecisionId("test.xgp"), MoveNumber = 17 };
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;
        Assert.Equal(17, restored.MoveNumber);
    }

    [Fact]
    public void DecisionRow_MoveNumber_DefaultsToZero()
    {
        var row = new DecisionRow { Id = new XgpDecisionId("test.xgp") };
        Assert.Equal(0, row.MoveNumber);
    }

    [Fact]
    public void DecisionRow_IsStandardStart_RoundTrip()
    {
        var original = new DecisionRow { Id = new XgpDecisionId("test.xgp"), IsStandardStart = true };
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;
        Assert.True(restored.IsStandardStart);
    }

    [Fact]
    public void DecisionRow_IsStandardStart_DefaultsToFalse()
    {
        var row = new DecisionRow { Id = new XgpDecisionId("test.xgp") };
        Assert.False(row.IsStandardStart);
    }

    [Fact]
    public void DecisionRow_ToCsvLine_MoveNumberInColumnOrder()
    {
        var row = new DecisionRow
        {
            Id = new XgpDecisionId("test.xgp"),
            Player = "Mochy",
            Game = 2,
            MoveNumber = 17,
            Roll = 63
        };
        var line = row.ToCsvLine();
        // CSV header is: ...Player,SourceFile,Game,MoveNumber,Roll,...
        // SourceFile is null → empty cell. Expect "Mochy,,2,17,63,"
        Assert.Contains(",Mochy,,2,17,63,", line);
    }

    [Fact]
    public void DecisionRow_IDecisionFilterData_MoveNumberAndIsStandardStart()
    {
        IDecisionFilterData row = new DecisionRow
        {
            Id = new XgpDecisionId("test.xgp"),
            MoveNumber = 12,
            IsStandardStart = true
        };

        Assert.Equal(12, row.MoveNumber);
        Assert.True(row.IsStandardStart);
    }

    [Fact]
    public void DecisionRow_IDecisionFilterData_MoveNumberAndIsStandardStart_Defaults()
    {
        IDecisionFilterData row = new DecisionRow { Id = new XgpDecisionId("test.xgp") };

        Assert.Equal(0, row.MoveNumber);
        Assert.False(row.IsStandardStart);
    }

    // -----------------------------------------------------------------------
    //  Id — persistent decision identifier
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_Id_RoundTrip_Xgp()
    {
        var original = new DecisionRow { Id = new XgpDecisionId("match.xgp") };
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;

        Assert.Equal(new XgpDecisionId("match.xgp"), restored.Id);
        // Canonical-string JSON shape (bundled DecisionIdJsonConverter), not a polymorphic object.
        Assert.Contains("\"Id\":\"match.xgp\"", json);
    }

    [Fact]
    public void DecisionRow_Id_RoundTrip_Xg()
    {
        var original = new DecisionRow
        {
            Id = new XgDecisionId("match.xg", Game: 2, MoveNumber: 17, IsCube: false)
        };
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
        var row = new DecisionRow
        {
            Id = new XgDecisionId("match.xg", 2, 17, IsCube: true),
            Player = "Mochy"
        };

        Assert.DoesNotContain("Id", DecisionRow.CsvHeader);
        Assert.DoesNotContain("match.xg:g2:m17:cube", row.ToCsvLine());
    }
}