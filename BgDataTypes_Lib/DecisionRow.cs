using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// A single analysed checker-play or cube decision, ready for CSV/JSON export.
///
/// <para>
/// <b>The Crawford rule binds the row</b> (halheinrich/backgammon#201).
/// Doubling is prohibited in the Crawford game, so a cube decision
/// (<see cref="Roll"/> of 0) flagged <see cref="IsCrawford"/> cannot exist,
/// and the row cannot be constructed: the <see cref="Roll"/> and
/// <see cref="IsCrawford"/> init setters each check the other, so whichever
/// is set second throws <see cref="ArgumentException"/> naming itself —
/// from an object initializer in either member order and from a JSON
/// document in either property order alike. Cube is the row's
/// <em>default</em> kind, which is why <see cref="Roll"/> is
/// <c>required</c> and why its guard distinguishes a not-yet-stated roll
/// from a stated 0 — see <see cref="Roll"/>. The same rule on the composite
/// record is <see cref="BgDecisionData"/>; <see cref="CrawfordRule"/> is
/// the one spelling of both.
/// </para>
/// </summary>
public sealed class DecisionRow : IDecisionFilterData
{
    // Null until Roll is stated — Roll's doc comment owns the why.
    private readonly int? _roll;
    private readonly bool _isCrawford;

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

    /// <summary>
    /// Dice roll as a two-digit integer, e.g. 63, 11. 0 for cube decisions.
    /// The row's decision-kind discriminator (<see cref="IsCube"/> reads it),
    /// and <c>required</c> because the kind must be stated, never defaulted:
    /// with 0 — a cube — as the default, a row that set
    /// <see cref="IsCrawford"/> and omitted the roll would be a Crawford cube
    /// by accident, and no guard could tell "not yet stated" from "cube".
    /// Every construction therefore names it, and a JSON document without
    /// it is refused (<see cref="System.Text.Json.JsonException"/>) rather
    /// than read as a cube.
    /// </summary>
    /// <remarks>
    /// The one place the sentinel is explained. The property is backed by a
    /// nullable field whose null means "not yet stated", and the
    /// <see cref="IsCrawford"/> guard reads that field rather than this
    /// property: in the legal initializer order
    /// <c>{ IsCrawford = true, Roll = 31 }</c> the <see cref="IsCrawford"/>
    /// setter runs while the roll is still unstated, and a plain
    /// <see langword="int"/> field would hand it a 0 — a cube — and throw
    /// on a legal Crawford play. <c>required</c> guarantees the null never
    /// survives construction, so this setter always runs, sees every stated
    /// 0, and carries the guard for that order; the two guards are
    /// order-independent only together with the sentinel.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown on init when the value is 0 and the already-set
    /// <see cref="IsCrawford"/> is <see langword="true"/> — the Crawford
    /// rule, see the class summary.
    /// </exception>
    public required int Roll
    {
        get => _roll ?? 0;
        init
        {
            CrawfordRule.ThrowIfCrawfordCube(_isCrawford, value == 0, nameof(Roll));
            _roll = value;
        }
    }

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
    /// <exception cref="ArgumentException">
    /// Thrown on init when the value is <see langword="true"/> and
    /// <see cref="Roll"/> has already been stated as 0 — the Crawford rule,
    /// see the class summary. A roll not yet stated does not trip it: the
    /// <see cref="Roll"/> setter, which <c>required</c> guarantees will run,
    /// carries the guard for that order.
    /// </exception>
    public bool IsCrawford
    {
        get => _isCrawford;
        init
        {
            CrawfordRule.ThrowIfCrawfordCube(value, _roll == 0, nameof(IsCrawford));
            _isCrawford = value;
        }
    }

    /// <summary>
    /// Whether the Jacoby rule was in force
    /// (<see cref="IDecisionFilterData.IsJacoby"/>), in the tri-state contract
    /// <see cref="PositionData.IsJacoby"/> owns and states. Stored rather than
    /// derived — the CSV shape carries the fact, so the row must too:
    /// <see cref="MatchScore"/> spells it as a suffix on the money token, the
    /// way <see cref="IsCrawford"/> spells itself as the <c>C</c> suffix on a
    /// match score.
    /// </summary>
    public bool? IsJacoby { get; init; }

    /// <summary>
    /// Match score string derived from <see cref="OnRollNeeds"/>, <see cref="OpponentNeeds"/>,
    /// <see cref="IsCrawford"/>, <see cref="IsMoneyGame"/>, and <see cref="IsJacoby"/>.
    /// Used for CSV output only.
    /// <para>
    /// A money row spells the Jacoby rule as a suffix on the money token —
    /// <c>moneyJ</c> or <c>moneyNJ</c> — the same in-grammar shape Crawford's
    /// <c>C</c> suffix uses on a match score. When the rule is unknown
    /// (<see cref="IsJacoby"/> <see langword="null"/> on a money row) the
    /// token is the bare <c>money</c>: it states what is known and withholds
    /// what is not, and it is neither of the two rule-bearing tokens, which is
    /// exactly the filter-layer contract
    /// (<see cref="IDecisionFilterData.IsJacoby"/>). Read back through a
    /// filter surface it fails loud rather than quietly matching, <c>money</c>
    /// being the retired token there.
    /// </para>
    /// </summary>
    [JsonIgnore]
    public string MatchScore => IsMoneyGame
        ? IsJacoby switch
        {
            true  => "moneyJ",
            false => "moneyNJ",
            null  => "money"
        }
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