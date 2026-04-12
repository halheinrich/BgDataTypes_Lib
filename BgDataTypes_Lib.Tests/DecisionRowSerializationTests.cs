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
            MatchLength = 9,
            OnRollNeeds = 3,
            OpponentNeeds = 5,
            IsCrawford = false,
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
        Assert.Equal(original.MatchLength, restored.MatchLength);
        Assert.Equal(original.OnRollNeeds, restored.OnRollNeeds);
        Assert.Equal(original.OpponentNeeds, restored.OpponentNeeds);
        Assert.Equal(original.IsCrawford, restored.IsCrawford);
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
            Roll = 0,
            Equity = 0.312,
            Player = "Falafel",
            Match = "mochy-falafel",
            Game = 1,
            MoveNum = 3,
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
        var original = new DecisionRow();
        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;

        Assert.Equal(string.Empty, restored.Xgid);
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

    [Fact]
    public void DecisionRow_RoundTrip_MatchScoreNotSerialized()
    {
        var original = new DecisionRow { MatchLength = 9, OnRollNeeds = 3, OpponentNeeds = 5 };
        var json = JsonSerializer.Serialize(original, Options);

        Assert.DoesNotContain("MatchScore", json);

        var restored = JsonSerializer.Deserialize<DecisionRow>(json, Options)!;
        Assert.Equal("3a5a", restored.MatchScore);
    }

    // -----------------------------------------------------------------------
    //  MatchScore reconstruction
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_MatchScore_Money()
    {
        var row = new DecisionRow { MatchLength = 0, OnRollNeeds = 0, OpponentNeeds = 0 };
        Assert.Equal("money", row.MatchScore);
    }

    [Fact]
    public void DecisionRow_MatchScore_Standard()
    {
        var row = new DecisionRow { MatchLength = 9, OnRollNeeds = 3, OpponentNeeds = 5 };
        Assert.Equal("3a5a", row.MatchScore);
    }

    [Fact]
    public void DecisionRow_MatchScore_Crawford()
    {
        var row = new DecisionRow { MatchLength = 9, OnRollNeeds = 1, OpponentNeeds = 1, IsCrawford = true };
        Assert.Equal("1a1aC", row.MatchScore);
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
        var row = new DecisionRow { MatchLength = 9, OnRollNeeds = 3, OpponentNeeds = 5, Player = "Last, First" };
        var line = row.ToCsvLine();
        Assert.Contains("\"Last, First\"", line);
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
        Assert.Equal(10, line.Count(c => c == ','));
    }

    // -----------------------------------------------------------------------
    //  IDecisionFilterData — DecisionRow
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionRow_IDecisionFilterData_CheckerPlay()
    {
        IDecisionFilterData row = new DecisionRow
        {
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
        IDecisionFilterData row = new DecisionRow { Error = 0.045 };
        double? fe = row.FilterError;
        Assert.NotNull(fe);
        Assert.Equal(0.045, fe!.Value);
    }
}