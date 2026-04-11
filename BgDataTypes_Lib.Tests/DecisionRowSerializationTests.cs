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
            Xgid = "XGID=-b----E-C---eE---c-e----B-:0:0:1:63:0:0:3:0:10",
            Error = 0.023,
            MatchScore = "3a5a",
            MatchLength = 9,
            Player = "Mochy",
            Match = "mochy-falafel",
            Game = 2,
            MoveNum = 7,
            Roll = 63,
            AnalysisDepth = "3-ply",
            Equity = -0.142,
            Board = [0, 2, 0, 0, 0, 0, -5, 0, -3, 0, 0, 0, 5, 0, 0, 0, 0, -5, 0, -2, 0, 0, 0, 0, 2, 1]
        };

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;

        Assert.Equal(original.Xgid, restored.Xgid);
        Assert.Equal(original.Error, restored.Error);
        Assert.Equal(original.MatchScore, restored.MatchScore);
        Assert.Equal(original.MatchLength, restored.MatchLength);
        Assert.Equal(original.Player, restored.Player);
        Assert.Equal(original.Match, restored.Match);
        Assert.Equal(original.Game, restored.Game);
        Assert.Equal(original.MoveNum, restored.MoveNum);
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
            Xgid = "XGID=-b----E-C---eE---c-e----B-:0:0:1:00:0:0:3:0:10",
            Roll = 0,
            Equity = 0.312,
            Player = "Falafel",
            Match = "mochy-falafel",
            Game = 1,
            MoveNum = 3,
            AnalysisDepth = "Rollout: 1296 trials. 3-ply"
        };

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;

        Assert.Equal(0, restored.Roll);
        Assert.True(restored.IsCube);
        Assert.Equal(original.Equity, restored.Equity);
        Assert.Equal(original.AnalysisDepth, restored.AnalysisDepth);
    }

    [Fact]
    public void DecisionRow_RoundTrip_StringDefaults()
    {
        var original = new DecisionRow();

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;

        Assert.Equal(string.Empty, restored.Xgid);
        Assert.Equal(string.Empty, restored.MatchScore);
        Assert.Equal(string.Empty, restored.Player);
        Assert.Equal(string.Empty, restored.Match);
        Assert.Equal(string.Empty, restored.AnalysisDepth);
    }

    [Fact]
    public void DecisionRow_RoundTrip_Board()
    {
        var board = new int[26];
        board[1] = 2; board[6] = -5; board[24] = -2; board[25] = 1;

        var original = new DecisionRow { Board = board };

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;

        Assert.Equal(original.Board, restored.Board);
    }

    // -----------------------------------------------------------------------
    //  CSV
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_CsvHeader_ContainsExpectedColumns()
    {
        Assert.Equal(
            "Xgid,Error,MatchScore,MatchLength,Player,Match,Game,MoveNum,Roll,AnalysisDepth,Equity",
            DecisionRow.CsvHeader);
    }

    [Fact]
    public void DecisionRow_ToCsvLine_EscapesCommas()
    {
        var row = new DecisionRow { MatchScore = "3,5" };
        var line = row.ToCsvLine();
        Assert.Contains("\"3,5\"", line);
    }

    [Fact]
    public void DecisionRow_ToCsvLine_EscapesQuotes()
    {
        var row = new DecisionRow { Player = "say \"hello\"" };
        var line = row.ToCsvLine();
        Assert.Contains("\"say \"\"hello\"\"\"", line);
    }

    [Fact]
    public void DecisionRow_ToCsvLine_DoublesFormattedG6()
    {
        var row = new DecisionRow { Error = 0.12345678, Equity = -0.98765432 };
        var line = row.ToCsvLine();
        Assert.Contains("0.123457", line);
        Assert.Contains("-0.987654", line);
    }

    [Fact]
    public void DecisionRow_ToCsvLine_BoardNotInCsv()
    {
        var row = new DecisionRow
        {
            Board = [0, 2, 0, 0, 0, 0, -5, 0, -3, 0, 0, 0, 5, 0, 0, 0, 0, -5, 0, -2, 0, 0, 0, 0, 2, 1]
        };
        var line = row.ToCsvLine();
        Assert.Equal(10, line.Count(c => c == ','));  // 11 fields = 10 commas
    }
}