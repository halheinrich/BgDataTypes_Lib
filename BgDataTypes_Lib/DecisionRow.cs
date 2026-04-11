using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// A single analysed checker-play or cube decision, ready for CSV/JSON export.
/// </summary>
public sealed class DecisionRow
{
    /// <summary>XGID position string.</summary>
    public string Xgid { get; init; } = string.Empty;

    /// <summary>Absolute error (positive = worse than best).</summary>
    public double Error { get; init; }

    /// <summary>Match score at the time of the decision, e.g. "3a5a" or "money".</summary>
    public string MatchScore { get; init; } = string.Empty;

    /// <summary>Match length (0 = unlimited/money).</summary>
    public int MatchLength { get; init; }

    /// <summary>Name of the player who made the decision.</summary>
    public string Player { get; init; } = string.Empty;

    /// <summary>Match identifier (file name without extension).</summary>
    public string Match { get; init; } = string.Empty;

    /// <summary>Game number within the match (1-based).</summary>
    public int Game { get; init; }

    /// <summary>Move number within the game (1-based).</summary>
    public int MoveNum { get; init; }

    /// <summary>Dice roll as a two-digit integer, e.g. 63, 11. 0 for cube decisions.</summary>
    public int Roll { get; init; }

    /// <summary>Human-readable analysis depth label, e.g. "3-ply", "Rollout: 1296 trials. 3-ply".</summary>
    public string AnalysisDepth { get; init; } = string.Empty;

    /// <summary>Best equity value from the analysis.</summary>
    public double Equity { get; init; }

    /// <summary>True if this is a cube decision (Roll == 0); false if a checker play.</summary>
    [JsonIgnore]
    public bool IsCube => Roll == 0;

    /// <summary>Away score for the player on roll, parsed from <see cref="MatchScore"/>. 0 for money games.</summary>
    [JsonIgnore]
    public int OnRollNeeds => ParseMatchScore().onRoll;

    /// <summary>Away score for the opponent, parsed from <see cref="MatchScore"/>. 0 for money games.</summary>
    [JsonIgnore]
    public int OpponentNeeds => ParseMatchScore().opponent;

    /// <summary>True if this is a Crawford game, parsed from <see cref="MatchScore"/>.</summary>
    [JsonIgnore]
    public bool IsCrawford => ParseMatchScore().crawford;

    private (int onRoll, int opponent, bool crawford) ParseMatchScore()
    {
        if (string.IsNullOrEmpty(MatchScore) || MatchScore == "money")
            return (0, 0, false);

        var parts = MatchScore.Split('a');
        if (parts.Length < 2
            || !int.TryParse(parts[0], out int onRoll)
            || !int.TryParse(parts[1], out int opponent))
            return (0, 0, false);

        bool crawford = parts.Length >= 3
            && parts[2].Trim().Equals("C", StringComparison.OrdinalIgnoreCase);

        return (onRoll, opponent, crawford);
    }
    /// <summary>
    /// Checker counts normalized to the player on roll.
    /// board[0]    = opponent's bar (never positive)
    /// board[1-24] = points 1-24 from player on roll's perspective
    /// board[25]   = player on roll's bar (never negative)
    /// Positive values = player on roll's checkers; negative = opponent's.
    /// Not included in CSV output.
    /// </summary>
    public int[] Board { get; init; } = [];

    // -----------------------------------------------------------------------
    //  CSV support
    // -----------------------------------------------------------------------

    /// <summary>CSV header row matching the column order of <see cref="ToCsvLine"/>.</summary>
    public static string CsvHeader =>
        "Xgid,Error,MatchScore,MatchLength,Player,Match,Game,MoveNum,Roll,AnalysisDepth,Equity";

    /// <summary>Formats this row as a CSV line (no trailing newline).</summary>
    public string ToCsvLine()
    {
        return string.Join(",",
            CsvEscape(Xgid),
            Error.ToString("G6"),
            CsvEscape(MatchScore),
            MatchLength,
            CsvEscape(Player),
            CsvEscape(Match),
            Game,
            MoveNum,
            Roll,
            CsvEscape(AnalysisDepth),
            Equity.ToString("G6"));
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}