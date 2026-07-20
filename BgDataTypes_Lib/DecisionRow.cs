using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// A single analysed checker-play or cube decision, ready for CSV/JSON export.
/// </summary>
public sealed class DecisionRow : IDecisionFilterData
{
    /// <summary>
    /// Stable, persistent identifier for this decision within its source file.
    /// Producer-supplied at the build site (see <c>ConvertXgToJson_Lib</c>) —
    /// required so that uninitialized cases surface at construction rather than
    /// later as silent null reads. Serialized to JSON via
    /// <see cref="DecisionIdJsonConverter"/>; excluded from CSV (the column set
    /// is explicit and the ID derives from existing CSV columns at read time
    /// if a consumer wants it).
    /// </summary>
    public required DecisionId Id { get; init; }

    /// <summary>XGID position string.</summary>
    public string Xgid { get; init; } = string.Empty;

    /// <summary>Absolute error (positive = worse than best).</summary>
    public double Error { get; init; }

    /// <summary>Match length (0 = unlimited/money).</summary>
    public int MatchLength { get; init; }

    /// <summary>
    /// True for an unlimited (money) session
    /// (<see cref="IDecisionFilterData.IsMoneyGame"/>). Redeclared concretely
    /// over the interface default so the predicate is visible on the type
    /// itself — <see cref="MatchScore"/> and other concrete-typed consumers
    /// read it here; the type's single spelling of the rule. Derived from
    /// <see cref="MatchLength"/>, so excluded from JSON like
    /// <see cref="IsCube"/>; <see cref="MatchLength"/> remains the CSV and
    /// JSON wire form.
    /// </summary>
    [JsonIgnore]
    public bool IsMoneyGame => MatchLength == 0;

    /// <summary>Name of the player who made the decision.</summary>
    public string Player { get; init; } = string.Empty;

    /// <summary>Originating file name including extension (e.g. "match.xg", "session.xgp"). No directory.</summary>
    public string? SourceFile { get; init; }

    /// <summary>Game number within the match (1-based).</summary>
    public int Game { get; init; }

    /// <summary>Move number within the game (1-based).</summary>
    public int MoveNumber { get; init; }

    /// <summary>True if the game started from the canonical opening position.</summary>
    public bool IsStandardStart { get; init; }

    /// <summary>Dice roll as a two-digit integer, e.g. 63, 11. 0 for cube decisions.</summary>
    public int Roll { get; init; }

    /// <summary>
    /// <see cref="Roll"/> in canonical unordered form
    /// (<see cref="IDecisionFilterData.Dice"/>): null when <see cref="Roll"/>
    /// is 0 (a cube decision), otherwise the roll's two digits canonicalized
    /// by <see cref="DiceRoll"/> ("13" and "31" both yield high 3, low 1). A
    /// malformed <see cref="Roll"/> whose digits are not both die faces
    /// (e.g. 70) throws <see cref="ArgumentOutOfRangeException"/> — corrupt
    /// data fails loud rather than silently filtering wrong. Derived, so
    /// excluded from JSON like <see cref="IsCube"/>; <see cref="Roll"/>
    /// remains the CSV and JSON wire form.
    /// </summary>
    [JsonIgnore]
    public DiceRoll? Dice => Roll == 0 ? null : new DiceRoll(Roll / 10, Roll % 10);

    /// <summary>Human-readable analysis depth label, e.g. "3-ply", "Rollout: 1296 trials. 3-ply".</summary>
    public string AnalysisDepth { get; init; } = string.Empty;

    /// <summary>How the analysis behind this decision was produced — the mode
    /// axis of the two-axis depth taxonomy
    /// (<see cref="IDecisionFilterData.AnalysisMode"/>); together with
    /// <see cref="AnalysisLevel"/> it is the taxonomy form of
    /// <see cref="AnalysisDepth"/>, used for depth filtering.
    /// Producer-stamped; <see cref="BgDataTypes_Lib.AnalysisMode.Unknown"/>
    /// when not set (including JSON written before the two-axis pair
    /// existed). Serializes to JSON; excluded from CSV output
    /// (<see cref="AnalysisDepth"/> remains the CSV depth column).</summary>
    public AnalysisMode AnalysisMode { get; init; }

    /// <summary>Evaluation level of the analysis behind this decision — the
    /// level axis paired with <see cref="AnalysisMode"/>
    /// (<see cref="IDecisionFilterData.AnalysisLevel"/>); for rollout-family
    /// modes, the inner level of the row's decision kind. Producer-stamped;
    /// <see cref="BgDataTypes_Lib.AnalysisLevel.Unknown"/> when not set.
    /// Serializes to JSON; excluded from CSV output.</summary>
    public AnalysisLevel AnalysisLevel { get; init; }

    /// <summary>Best equity value from the analysis.</summary>
    public double Equity { get; init; }

    /// <summary>True if this is a cube decision (Roll == 0); false if a checker play.</summary>
    [JsonIgnore]
    public bool IsCube => Roll == 0;

    /// <summary>Away score for the player on roll. 0 for money games.</summary>
    public int OnRollNeeds { get; init; }

    /// <summary>Away score for the opponent. 0 for money games.</summary>
    public int OpponentNeeds { get; init; }

    /// <summary>True if this is the Crawford game.</summary>
    public bool IsCrawford { get; init; }

    /// <summary>
    /// Match score string derived from <see cref="OnRollNeeds"/>, <see cref="OpponentNeeds"/>,
    /// <see cref="IsCrawford"/>, and <see cref="IsMoneyGame"/>. Used for CSV output only.
    /// </summary>
    [JsonIgnore]
    public string MatchScore => IsMoneyGame
        ? "money"
        : IsCrawford
            ? $"{OnRollNeeds}a{OpponentNeeds}aC"
            : $"{OnRollNeeds}a{OpponentNeeds}a";

    /// <summary>
    /// Checker counts normalized to the player on roll.
    /// board[0]    = opponent's bar (never positive)
    /// board[1-24] = points 1-24 from player on roll's perspective
    /// board[25]   = player on roll's bar (never negative)
    /// Positive values = player on roll's checkers; negative = opponent's.
    /// Not included in CSV output.
    /// </summary>
    public IReadOnlyList<int> Board { get; init; } = [];

    /// <summary>
    /// Board after the best play, with POV flipped — opponent is now on roll.
    /// Same 26-element layout as <see cref="Board"/>; decision-maker's checkers
    /// are negative here, opponent's are positive. Empty for cube decisions.
    /// Not included in CSV output.
    /// </summary>
    public IReadOnlyList<int> AfterBestBoard { get; init; } = [];

    /// <summary>
    /// Board after the player's actual play, same layout and sign convention
    /// as <see cref="AfterBestBoard"/>. Empty for cube decisions.
    /// Not included in CSV output.
    /// </summary>
    public IReadOnlyList<int> AfterPlayerBoard { get; init; } = [];

    // -----------------------------------------------------------------------
    //  IDecisionFilterData
    // -----------------------------------------------------------------------

    /// <summary>
    /// Equity loss for this decision (≥ 0). Maps from <see cref="Error"/>.
    /// Never null on <see cref="DecisionRow"/> — <see cref="Error"/> is always recorded.
    /// </summary>
    [JsonIgnore]
    public double? FilterError => Error;

    // -----------------------------------------------------------------------
    //  CSV support
    // -----------------------------------------------------------------------

    /// <summary>CSV header row matching the column order of <see cref="ToCsvLine"/>.</summary>
    public static string CsvHeader =>
        "Xgid,Error,MatchScore,MatchLength,Player,SourceFile,Game,MoveNumber,Roll,AnalysisDepth,Equity";

    /// <summary>Formats this row as a CSV line (no trailing newline).</summary>
    public string ToCsvLine()
    {
        return string.Join(",",
            CsvEscape(Xgid),
            Error.ToString("G6"),
            CsvEscape(MatchScore),
            MatchLength,
            CsvEscape(Player),
            CsvEscape(SourceFile ?? string.Empty),
            Game,
            MoveNumber,
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