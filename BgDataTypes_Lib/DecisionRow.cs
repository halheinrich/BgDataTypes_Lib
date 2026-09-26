using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// A single analysed decision as one flat row, for CSV and JSON export: one
/// row per decision, one column per field, of either kind.
///
/// <para>
/// <b>A projection of the record, built one way.</b> A row is made by
/// <see cref="From"/> from a <see cref="BgDecisionData"/>, and every column is
/// taken from the record — the after-boards from the record's own derivation
/// (<see cref="CheckerPlayDecision.AfterBestBoard"/>), never computed a second
/// way — so a row and its record cannot disagree on anything the two share,
/// <see cref="IDecisionFilterData"/> above all. The constructor is internal:
/// outside this library a row comes from <see cref="From"/> or from JSON.
/// </para>
///
/// <para>
/// <b>It carries the kind</b> (<see cref="Kind"/>), and the other kind's
/// columns are empty, never zero (halheinrich/backgammon#273): a cube row's
/// <see cref="Roll"/> and after-boards are <see langword="null"/> — empty
/// cells in CSV. A standalone position's <see cref="Game"/>,
/// <see cref="MoveNumber"/> and <see cref="IsStandardStart"/> are
/// <see langword="null"/> likewise (halheinrich/backgammon#124).
/// </para>
///
/// <para>
/// <b>Read back whole.</b> A row read from JSON is held, once every column is
/// read, to what a projection of a record guarantees — the kind's columns
/// present and the other kind's empty, the roll two die faces, the rules of
/// <see cref="DecisionRules"/> — and a document breaking any of them is
/// refused with a <see cref="JsonException"/>, on the reflection path and
/// through <see cref="BgDataTypesJsonContext"/> alike. Every column but the
/// nullable ones is <c>required</c>, per the wire rule stated on
/// <see cref="BgDataTypesJsonContext"/>; each nullable column's documentation
/// says what <see langword="null"/> means.
/// </para>
/// </summary>
public sealed class DecisionRow : IDecisionFilterData, IJsonOnDeserialized
{
    /// <summary>The serializer's and <see cref="From"/>'s constructor; there is no other.</summary>
    [JsonConstructor]
    internal DecisionRow()
    {
    }

    /// <summary>
    /// The row of <paramref name="record"/>: every column taken from it, the
    /// shared ones through its <see cref="IDecisionFilterData"/> view, so row
    /// and record agree on all of them by construction.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="record"/> is <see langword="null"/>.</exception>
    public static DecisionRow From(BgDecisionData record)
    {
        ArgumentNullException.ThrowIfNull(record);

        // The kind's own columns: the checker play's roll, best-play depth and
        // equity, and after-boards; the cube decision's depth and no-double
        // equity, with the checker columns empty.
        var (roll, depth, equity, afterBest, afterPlayer) = record.Match(
            static play => (
                (int?)(play.Decision.Dice[0] * 10 + play.Decision.Dice[1]),
                play.Decision.BestPlay.Depth,
                play.Decision.BestPlay.Equity,
                (BoardPosition?)play.AfterBestBoard,
                play.AfterPlayerBoard),
            static cube => (
                (int?)null,
                cube.Decision.Depth,
                cube.Decision.NoDoubleEquity,
                (BoardPosition?)null,
                (BoardPosition?)null));

        return new DecisionRow
        {
            Kind = record.Kind,
            Id = record.Id,
            Xgid = record.Xgid,
            Error = record.FilterError,
            MatchLength = record.MatchLength,
            Player = record.Player,
            IsStandardStart = record.IsStandardStart,
            Roll = roll,
            AnalysisDepth = depth,
            AnalysisMode = record.AnalysisMode,
            AnalysisLevel = record.AnalysisLevel,
            Equity = equity,
            OnRollNeeds = record.OnRollNeeds,
            OpponentNeeds = record.OpponentNeeds,
            IsCrawford = record.IsCrawford,
            IsJacoby = record.IsJacoby,
            Board = record.Board,
            AfterBestBoard = afterBest,
            AfterPlayerBoard = afterPlayer,
        };
    }

    /// <summary>
    /// The decision's kind (<see cref="IDecisionFilterData.Kind"/>), stated as a
    /// column and written first. It decides which kind's columns are present.
    /// </summary>
    [JsonPropertyOrder(-1)]
    public required DecisionKind Kind { get; init; }

    /// <summary>
    /// Stable, persistent identifier for this decision within its source file
    /// (<see cref="BgDecisionData.Id"/>). Serialized to JSON via
    /// <see cref="DecisionIdJsonConverter"/>; excluded from CSV (the column set
    /// is explicit). The one stored place of <see cref="Game"/>,
    /// <see cref="MoveNumber"/> and <see cref="SourceFile"/>.
    /// </summary>
    public required DecisionId Id { get; init; }

    /// <summary>XGID position string (<see cref="BgDecisionData.Xgid"/>); never empty text.</summary>
    public required string Xgid { get; init; }

    /// <summary>
    /// The user's error (≥ 0) — the record's <see cref="IDecisionFilterData.FilterError"/>:
    /// a checker play's equity loss against the best play, a cube decision's
    /// doubling error or, failing that, its take error. <see langword="null"/>
    /// when no user decision is recorded — an empty CSV cell, never 0.
    /// </summary>
    public double? Error { get; init; }

    /// <summary>Match length (0 = unlimited/money).</summary>
    public required int MatchLength { get; init; }

    /// <summary>
    /// True for an unlimited (money) session
    /// (<see cref="IDecisionFilterData.IsMoneyGame"/>). Redeclared concretely
    /// over the interface default so the predicate is visible on the type
    /// itself — <see cref="MatchScore"/> and other concrete-typed consumers
    /// read it here; the type's single spelling of the rule. Derived from
    /// <see cref="MatchLength"/>, so excluded from JSON;
    /// <see cref="MatchLength"/> remains the CSV and JSON wire form.
    /// </summary>
    [JsonIgnore]
    public bool IsMoneyGame => MatchLength == 0;

    /// <summary>
    /// Name of the player who made the decision
    /// (<see cref="IDecisionFilterData.Player"/>); <see langword="null"/> when
    /// the source recorded no name — an empty CSV cell — never empty text.
    /// </summary>
    public string? Player { get; init; }

    /// <summary>
    /// The file the decision came from — its bare name with extension, no
    /// directory: the <see cref="Id"/>'s <see cref="DecisionId.Filename"/>,
    /// as <see cref="BgDecisionData.SourceFile"/> is. Derived, so excluded
    /// from JSON (<see cref="Id"/> is the wire form, and a document still
    /// stating the retired column reads with it ignored); a CSV column.
    /// </summary>
    [JsonIgnore]
    public string SourceFile => Id.Filename;

    /// <summary>
    /// The 1-based number of the game this decision was played in, within
    /// its source file; <see langword="null"/> for a decision in a standalone
    /// position, which belongs to no game (halheinrich/backgammon#124) — an
    /// empty CSV cell, never a stamped 1. Derived from <see cref="Id"/>, as
    /// <see cref="BgDecisionData.Game"/> is, so excluded from JSON;
    /// <see cref="Id"/> is the wire form.
    /// </summary>
    [JsonIgnore]
    public int? Game => Id.GameInFile;

    /// <summary>
    /// The 1-based move number within the game; <see langword="null"/> for a
    /// decision in a standalone position — derived from <see cref="Id"/> as
    /// <see cref="Game"/> is.
    /// </summary>
    [JsonIgnore]
    public int? MoveNumber => Id.MoveInGame;

    /// <summary>
    /// Whether the game started from the canonical opening position
    /// (<see cref="DescriptiveData.IsStandardStart"/>); <see langword="null"/>
    /// for a decision in a standalone position.
    /// </summary>
    public bool? IsStandardStart { get; init; }

    /// <summary>
    /// A checker play's dice as rolled, as a two-digit integer in rolled order
    /// (e.g. 63, 11); <see langword="null"/> for a cube decision — an empty CSV
    /// cell, never 0. <see cref="Dice"/> is its canonical form.
    /// </summary>
    public int? Roll { get; init; }

    /// <summary>
    /// <see cref="Roll"/> in canonical unordered form
    /// (<see cref="IDecisionFilterData.Dice"/>): <see langword="null"/> for a
    /// cube decision, otherwise the roll's two digits canonicalized by
    /// <see cref="DiceRoll"/> ("13" and "31" both yield high 3, low 1).
    /// Derived, so excluded from JSON; <see cref="Roll"/> remains the CSV and
    /// JSON wire form.
    /// </summary>
    [JsonIgnore]
    public DiceRoll? Dice => Roll is int roll ? new DiceRoll(roll / 10, roll % 10) : null;

    /// <summary>
    /// Human-readable analysis depth label, e.g. "3-ply", "Rollout: 1296
    /// trials. 3-ply": a checker play's best candidate's
    /// (<see cref="PlayCandidate.Depth"/>), a cube decision's cube analysis's
    /// (<see cref="CubeDecisionData.Depth"/>). <see langword="null"/> when
    /// the producer recorded no label — an empty CSV cell — never empty text.
    /// </summary>
    public string? AnalysisDepth { get; init; }

    /// <summary>How the analysis behind this decision was produced — the mode
    /// axis of the two-axis depth taxonomy
    /// (<see cref="IDecisionFilterData.AnalysisMode"/>); together with
    /// <see cref="AnalysisLevel"/> it is the taxonomy form of
    /// <see cref="AnalysisDepth"/>, used for depth filtering.
    /// <see cref="BgDataTypes_Lib.AnalysisMode.Unknown"/> when the producer
    /// did not record it. Serializes to JSON; excluded from CSV output
    /// (<see cref="AnalysisDepth"/> remains the CSV depth column).</summary>
    public required AnalysisMode AnalysisMode { get; init; }

    /// <summary>Evaluation level of the analysis behind this decision — the
    /// level axis paired with <see cref="AnalysisMode"/>
    /// (<see cref="IDecisionFilterData.AnalysisLevel"/>).
    /// <see cref="BgDataTypes_Lib.AnalysisLevel.Unknown"/> when the producer
    /// did not record it. Serializes to JSON; excluded from CSV
    /// output.</summary>
    public required AnalysisLevel AnalysisLevel { get; init; }

    /// <summary>
    /// The equity of the analysis's best line: a checker play's best
    /// candidate's (<see cref="PlayCandidate.Equity"/>), a cube decision's
    /// no-double equity (<see cref="CubeDecisionData.NoDoubleEquity"/>).
    /// </summary>
    public required double Equity { get; init; }

    /// <summary>Away score for the player on roll. 0 for money games.</summary>
    public required int OnRollNeeds { get; init; }

    /// <summary>Away score for the opponent. 0 for money games.</summary>
    public required int OpponentNeeds { get; init; }

    /// <summary>True if this is the Crawford game; never for a cube decision (<see cref="DecisionRules.CrawfordMessage"/>).</summary>
    public required bool IsCrawford { get; init; }

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
    /// <para>
    /// A token, so culture-invariant: the away scores are written with the
    /// invariant culture whatever the ambient one, as every number in
    /// <see cref="ToCsvLine"/> is.
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
            ? string.Create(CultureInfo.InvariantCulture, $"{OnRollNeeds}a{OpponentNeeds}aC")
            : string.Create(CultureInfo.InvariantCulture, $"{OnRollNeeds}a{OpponentNeeds}a");

    /// <summary>
    /// The board at the moment of the decision. <b>Frame: the player on
    /// roll's</b>, the decision-maker's: slot 0 is the opponent's bar, 1–24
    /// the points, 25 the on-roll player's bar; positive counts are the
    /// on-roll player's checkers, negative the opponent's (the
    /// <see cref="BoardPosition"/> layout, as <see cref="PositionData.Mop"/>).
    /// Not included in CSV output.
    /// </summary>
    public required BoardPosition Board { get; init; }

    /// <summary>
    /// The board a checker play's best play leaves, in the next mover's frame
    /// — the record's <see cref="CheckerPlayDecision.AfterBestBoard"/>, taken
    /// from it when the row is built. Always present on a checker-play row;
    /// <see langword="null"/> on a cube row. Not included in CSV output.
    /// </summary>
    public BoardPosition? AfterBestBoard { get; init; }

    /// <summary>
    /// The board the user's checker play leaves, in the same frame — the
    /// record's <see cref="CheckerPlayDecision.AfterPlayerBoard"/>.
    /// <see langword="null"/> when the user's play is not among the
    /// candidates, and on a cube row. Not included in CSV output.
    /// </summary>
    public BoardPosition? AfterPlayerBoard { get; init; }

    // -----------------------------------------------------------------------
    //  IDecisionFilterData
    // -----------------------------------------------------------------------

    /// <summary>The user's error (≥ 0), or <see langword="null"/> when none is recorded; <see cref="Error"/>.</summary>
    [JsonIgnore]
    public double? FilterError => Error;

    // -----------------------------------------------------------------------
    //  Read back whole
    // -----------------------------------------------------------------------

    /// <summary>
    /// Holds a row read from JSON to what <see cref="From"/> guarantees; see
    /// the class summary.
    /// </summary>
    /// <exception cref="JsonException">The row breaks one of those guarantees; the message names it.</exception>
    void IJsonOnDeserialized.OnDeserialized()
    {
        string? fault = Kind switch
        {
            // The identifier converter reads a JSON null as null; the member
            // is present, so `required` does not catch it.
            _ when Id is null =>
                "A row states its Id.",
            _ when Xgid is null =>
                "A row states its Xgid.",
            DecisionKind.CheckerPlay when Roll is null =>
                "A checker-play row states its Roll.",
            DecisionKind.CheckerPlay when !IsTwoFaces(Roll.Value) =>
                $"Roll {Roll} is not two die faces.",
            DecisionKind.CheckerPlay when AfterBestBoard is null =>
                "A checker-play row states the board its best play leaves.",
            DecisionKind.Cube when Roll is not null || AfterBestBoard is not null || AfterPlayerBoard is not null =>
                "A cube row's checker-play columns are empty: Roll, AfterBestBoard and AfterPlayerBoard.",
            _ when !StatedText.Holds(Xgid) => StatedText.Message(nameof(Xgid)),
            _ when !StatedText.Holds(Player) => StatedText.Message(nameof(Player)),
            _ when !StatedText.Holds(AnalysisDepth) => StatedText.Message(nameof(AnalysisDepth)),
            _ when !DecisionRules.CrawfordAllows(Kind, IsCrawford) => DecisionRules.CrawfordMessage,
            _ when !DecisionRules.IdAgrees(Id, Kind) => DecisionRules.IdKindMessage,
            _ when !DecisionRules.StartAgrees(Id, IsStandardStart) => DecisionRules.StartMessage,
            _ => null,
        };
        if (fault is not null)
            throw new JsonException(fault);
    }

    private static bool IsTwoFaces(int roll) =>
        roll / 10 is >= 1 and <= 6 && roll % 10 is >= 1 and <= 6;

    // -----------------------------------------------------------------------
    //  CSV support
    // -----------------------------------------------------------------------

    /// <summary>CSV header row matching the column order of <see cref="ToCsvLine"/>.</summary>
    public static string CsvHeader =>
        "Xgid,Error,MatchScore,MatchLength,Player,SourceFile,Game,MoveNumber,Kind,Roll,AnalysisDepth,Equity";

    /// <summary>
    /// Formats this row as a CSV line (no trailing newline). A
    /// <see langword="null"/> column — the other kind's, or a fact that does
    /// not apply — is an empty cell. Every number is written with the
    /// invariant culture, whatever the ambient one: a decimal comma would
    /// split a cell in two, and the file must read the same wherever it was
    /// written.
    /// </summary>
    public string ToCsvLine()
    {
        var invariant = CultureInfo.InvariantCulture;
        return string.Join(",",
            CsvEscape(Xgid),
            Error?.ToString("G6", invariant),
            CsvEscape(MatchScore),
            MatchLength.ToString(invariant),
            CsvEscape(Player),
            CsvEscape(SourceFile),
            Game?.ToString(invariant),
            MoveNumber?.ToString(invariant),
            Kind,
            Roll?.ToString(invariant),
            CsvEscape(AnalysisDepth),
            Equity.ToString("G6", invariant));
    }

    /// <summary><paramref name="value"/> as a CSV cell; <see langword="null"/> (none recorded) is an empty one.</summary>
    private static string CsvEscape(string? value)
    {
        if (value is null)
            return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
