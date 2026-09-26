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
/// (<see cref="CheckerPlayDecision.AfterBoardOfBest"/>), never computed a second
/// way — so a row and its record cannot disagree on anything the two share,
/// <see cref="IDecisionFilterData"/> above all. The constructor is internal:
/// outside this library a row comes from <see cref="From"/> or from JSON.
/// </para>
///
/// <para>
/// <b>Built for one ranking.</b> Which play is best, and so a checker play's
/// error and result, depth, mode, level, equity and best after-board, is a ranking's
/// (SPEC-scoring §2a, halheinrich/backgammon#282). A row is built for one
/// <see cref="PlayRanking"/>, takes those columns from it, and states it
/// (<see cref="Ranking"/>); an export under the other ranking is a second
/// projection of the same records.
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
/// <b>It carries the session's kind the same way</b>
/// (<see cref="SessionKind"/>): one column per fact of each kind, the other
/// kind's empty — a money row's <see cref="MatchLength"/>, away scores and
/// Crawford flag, a match row's <see cref="IsJacoby"/>. The typed
/// <see cref="Session"/> is the columns as the record's kind, excluded from
/// JSON as <see cref="Dice"/> is.
/// </para>
///
/// <para>
/// <b>Read back whole.</b> A row read from JSON is held, once every column is
/// read, to what a projection of a record guarantees — each kind's columns
/// present and the other kind's empty, the roll two die faces, the session's
/// own rules, the rules of <see cref="DecisionRules"/> — and a document
/// breaking any of them is
/// refused with a <see cref="JsonException"/>, on the reflection path and
/// through <see cref="BgDataTypesJsonContext"/> alike. Every column but the
/// nullable ones is <c>required</c>, per the wire rule stated on
/// <see cref="BgDataTypesJsonContext"/>; each nullable column's documentation
/// says what <see langword="null"/> means.
/// </para>
/// </summary>
public sealed class DecisionRow : IDecisionFilterData, IJsonOnDeserialized
{
    // The session columns as the record's kind: set once, by whichever of the
    // two ways a row comes into being — From, with the record's own session,
    // or a read, with the session the columns state (OnDeserialized).
    private Session? _session;

    /// <summary>The serializer's and <see cref="From"/>'s constructor; there is no other.</summary>
    [JsonConstructor]
    internal DecisionRow()
    {
    }

    /// <summary>
    /// The row of <paramref name="record"/> under <paramref name="ranking"/>:
    /// every column taken from it, the shared ones through its
    /// <see cref="IDecisionFilterData"/> view for the ranking
    /// (<see cref="BgDecisionData.ViewFor"/>), so row and record agree on all
    /// of them by construction.
    /// </summary>
    /// <param name="record">The decision.</param>
    /// <param name="ranking">The ranking the row's best and error columns are derived under; <see cref="PlayRanking.Equity"/> is the default an app without the setting uses.</param>
    /// <exception cref="ArgumentNullException"><paramref name="record"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="ranking"/> is not a defined ranking.</exception>
    public static DecisionRow From(BgDecisionData record, PlayRanking ranking)
    {
        ArgumentNullException.ThrowIfNull(record);
        var view = record.ViewFor(ranking);

        // The kind's own columns: the checker play's roll, and its best play's
        // depth and equity under the ranking; the cube decision's depth and
        // no-double equity, with the checker columns empty.
        var (roll, depth, equity) = record.Match(
            play =>
            {
                var best = play.Decision.RankedBy(ranking).Best.Candidate;
                return ((int?)(play.Decision.Dice[0] * 10 + play.Decision.Dice[1]), best.Depth, best.Equity);
            },
            static cube => ((int?)null, cube.Decision.Depth, cube.Decision.NoDoubleEquity));

        // The session's own columns, the other kind's empty.
        var session = record.Session;
        var match = session as MatchSession;
        var money = session as MoneySession;

        var row = new DecisionRow
        {
            Kind = record.Kind,
            Id = record.Id,
            Xgid = record.Xgid,
            Ranking = ranking,
            Result = view.PlayerResult.Kind,
            Error = view.PlayerResult.TryGetError(out double error) ? error : null,
            Player = record.Player,
            IsStandardStart = record.IsStandardStart,
            Roll = roll,
            AnalysisDepth = depth,
            AnalysisMode = view.AnalysisMode,
            AnalysisLevel = view.AnalysisLevel,
            Equity = equity,
            SessionKind = session.Kind,
            MatchLength = match?.Terms.Length,
            OnRollNeeds = match?.OnRollNeeds,
            OpponentNeeds = match?.OpponentNeeds,
            IsCrawford = match?.IsCrawford,
            IsJacoby = money?.Terms.IsJacoby,
            IsBeaver = money?.Terms.IsBeaver,
            CubeLimit = money?.Terms.CubeLimit,
            OnRollScore = money?.OnRollScore,
            OpponentScore = money?.OpponentScore,
            Board = record.Board,
            AfterBestBoard = view.AfterBestBoard,
            AfterPlayerBoard = view.AfterPlayerBoard,
        };
        row._session = session;
        return row;
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

    /// <summary>
    /// The decision's XGID (<see cref="BgDecisionData.Xgid"/>): the record
    /// derives it, and the row carries it as a column, taken from the record
    /// when the row is built — as the after-boards are, since the row holds
    /// no cube column to derive it from. Never empty text.
    /// </summary>
    public required string Xgid { get; init; }

    /// <summary>
    /// The ranking the row was built for (<see cref="PlayRanking"/>): a
    /// checker play's <see cref="Error"/>, <see cref="Result"/>,
    /// <see cref="Equity"/>, <see cref="AnalysisDepth"/>,
    /// <see cref="AnalysisMode"/>, <see cref="AnalysisLevel"/> and
    /// <see cref="AfterBestBoard"/> are that ranking's. A cube row's columns
    /// do not depend on it; it still states the ranking it was exported
    /// under. A CSV column, before <see cref="Result"/>.
    /// </summary>
    public required PlayRanking Ranking { get; init; }

    /// <summary>
    /// The player's error under <see cref="Ranking"/>, stated exactly when
    /// <see cref="Result"/> is <see cref="PlayerResultKind.Scored"/> (never
    /// negative) or <see cref="PlayerResultKind.Unstated"/> — the record's
    /// <see cref="IDecisionFilterData.PlayerResult"/>'s error. Otherwise
    /// <see langword="null"/>, an empty CSV cell, never 0: which of the two
    /// errorless cases it is, <see cref="Result"/> says.
    /// </summary>
    public double? Error { get; init; }

    /// <summary>
    /// Which case the player's result is under <see cref="Ranking"/>
    /// (<see cref="PlayerResultKind"/>): the column that tells a move the
    /// ranking does not score from a decision with no move recorded, which
    /// both leave <see cref="Error"/> empty. A cube row's is never
    /// <see cref="PlayerResultKind.NotScored"/>. The last CSV column.
    /// </summary>
    public required PlayerResultKind Result { get; init; }

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
    /// trials. 3-ply": a checker play's best candidate's under
    /// <see cref="Ranking"/> (<see cref="PlayCandidate.Depth"/>), a cube
    /// decision's cube analysis's
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
    /// candidate's under <see cref="Ranking"/> (<see cref="PlayCandidate.Equity"/>),
    /// a cube decision's no-double equity (<see cref="CubeDecisionData.NoDoubleEquity"/>).
    /// </summary>
    public required double Equity { get; init; }

    /// <summary>
    /// The session's kind (<see cref="Session.Kind"/>), stated as a column: it
    /// decides which kind's session columns are present — a match's
    /// <see cref="MatchLength"/>, <see cref="OnRollNeeds"/>,
    /// <see cref="OpponentNeeds"/> and <see cref="IsCrawford"/>, or a money
    /// session's <see cref="IsJacoby"/>, <see cref="IsBeaver"/>,
    /// <see cref="CubeLimit"/>, <see cref="OnRollScore"/> and
    /// <see cref="OpponentScore"/> — the other kind's being empty.
    /// </summary>
    public required SessionKind SessionKind { get; init; }

    /// <summary>A match's length (<see cref="MatchTerms.Length"/>); <see langword="null"/> for a money row — an empty CSV cell, never 0.</summary>
    public int? MatchLength { get; init; }

    /// <summary>What the player on roll still needs in a match (<see cref="MatchSession.OnRollNeeds"/>); <see langword="null"/> for a money row, never 0.</summary>
    public int? OnRollNeeds { get; init; }

    /// <summary>What the opponent still needs in a match (<see cref="MatchSession.OpponentNeeds"/>); <see langword="null"/> for a money row, never 0.</summary>
    public int? OpponentNeeds { get; init; }

    /// <summary>
    /// Whether this is a match's Crawford game (<see cref="MatchSession.IsCrawford"/>),
    /// never for a cube decision (<see cref="DecisionRules.CrawfordMessage"/>);
    /// <see langword="null"/> for a money row, which has no Crawford game.
    /// </summary>
    public bool? IsCrawford { get; init; }

    /// <summary>
    /// Whether a money session's Jacoby rule was in force
    /// (<see cref="MoneyTerms.IsJacoby"/>); <see langword="null"/> for a match
    /// row, which has no Jacoby rule. Every money row states it.
    /// <see cref="MatchScore"/> spells it as a suffix on the money token, the
    /// way <see cref="IsCrawford"/> spells itself as the <c>C</c> suffix on a
    /// match score.
    /// </summary>
    public bool? IsJacoby { get; init; }

    /// <summary>Whether a money session's beaver rule was in force (<see cref="MoneyTerms.IsBeaver"/>); <see langword="null"/> for a match row.</summary>
    public bool? IsBeaver { get; init; }

    /// <summary>A money session's cube limit (<see cref="MoneyTerms.CubeLimit"/>); <see langword="null"/> for a match row, which has none.</summary>
    public int? CubeLimit { get; init; }

    /// <summary>The points the player on roll had won in a money session before the game (<see cref="MoneySession.OnRollScore"/>); <see langword="null"/> for a match row.</summary>
    public int? OnRollScore { get; init; }

    /// <summary>The points the opponent had won in a money session before the game (<see cref="MoneySession.OpponentScore"/>); <see langword="null"/> for a match row.</summary>
    public int? OpponentScore { get; init; }

    /// <summary>
    /// The session columns as the record's kind (<see cref="IDecisionFilterData.Session"/>):
    /// the record's own session for a row built by <see cref="From"/>, and the
    /// session its columns state for a row read from JSON, which the read
    /// holds to that kind's rules. Excluded from JSON; the columns are the wire
    /// form.
    /// </summary>
    [JsonIgnore]
    public Session Session => _session!;

    /// <summary>
    /// The match score as one token, for CSV output only, spelled from
    /// <see cref="Session"/>: a match's away scores, <c>3a5a</c>, with a
    /// <c>C</c> suffix in the Crawford game (<c>1a4aC</c>); money as
    /// <c>moneyJ</c> or <c>moneyNJ</c> by its Jacoby rule — the same
    /// in-grammar suffix shape. Every money session states its rule, so the
    /// bare <c>money</c> a row whose rule was not stamped once wrote is gone
    /// (halheinrich/backgammon#273).
    /// <para>
    /// A token, so culture-invariant: the away scores are written with the
    /// invariant culture whatever the ambient one, as every number in
    /// <see cref="ToCsvLine"/> is.
    /// </para>
    /// </summary>
    [JsonIgnore]
    public string MatchScore => Session.Match(
        static money => money.Terms.IsJacoby ? "moneyJ" : "moneyNJ",
        static match => match.IsCrawford
            ? string.Create(CultureInfo.InvariantCulture, $"{match.OnRollNeeds}a{match.OpponentNeeds}aC")
            : string.Create(CultureInfo.InvariantCulture, $"{match.OnRollNeeds}a{match.OpponentNeeds}a"));

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
    /// The board a checker play's best play under <see cref="Ranking"/> leaves,
    /// in the next mover's frame — the record's
    /// <see cref="CheckerPlayDecision.AfterBoardOfBest"/>, taken from it when the
    /// row is built. Always present on a checker-play row;
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

    /// <summary>
    /// The player's result, typed: <see cref="Result"/> with
    /// <see cref="Error"/>, which a row read from JSON is held to agree with.
    /// </summary>
    [JsonIgnore]
    public PlayerResult PlayerResult => Result switch
    {
        PlayerResultKind.NotScored => PlayerResult.NotScored,
        PlayerResultKind.Scored => PlayerResult.Scored(Error!.Value),
        PlayerResultKind.Unstated => PlayerResult.Unstated(Error!.Value),
        _ => PlayerResult.NotRecorded,
    };

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
        // The identifier converter reads a JSON null as null; the member is
        // present, so `required` does not catch it.
        if (Id is null)
            throw new JsonException("A row states its Id.");
        if (Xgid is null)
            throw new JsonException("A row states its Xgid.");

        string? fault = Kind switch
        {
            DecisionKind.CheckerPlay when Roll is null =>
                "A checker-play row states its Roll.",
            DecisionKind.CheckerPlay when !IsTwoFaces(Roll.Value) =>
                $"Roll {Roll} is not two die faces.",
            DecisionKind.CheckerPlay when AfterBestBoard is null =>
                "A checker-play row states the board its best play leaves.",
            DecisionKind.Cube when Roll is not null || AfterBestBoard is not null || AfterPlayerBoard is not null =>
                "A cube row's checker-play columns are empty: Roll, AfterBestBoard and AfterPlayerBoard.",
            DecisionKind.Cube when Result == PlayerResultKind.NotScored =>
                "A cube row's result is never NotScored: a ranking scores checker plays only.",
            _ when (Result is PlayerResultKind.Scored or PlayerResultKind.Unstated) != Error.HasValue =>
                $"A row's Error is stated exactly when its Result is Scored or Unstated; the Result is {Result}.",
            _ when Result == PlayerResultKind.Scored && !(double.IsFinite(Error!.Value) && Error.Value >= 0.0) =>
                $"A scored error is a finite number, never negative (got {Error}).",
            _ when !StatedText.Holds(Xgid) => StatedText.Message(nameof(Xgid)),
            _ when !StatedText.Holds(Player) => StatedText.Message(nameof(Player)),
            _ when !StatedText.Holds(AnalysisDepth) => StatedText.Message(nameof(AnalysisDepth)),
            _ => SessionColumnsFault(),
        };
        if (fault is not null)
            throw new JsonException(fault);

        // The columns as the session's kind, held to that kind's rules as a
        // record's session is; then the rules binding the decision to it.
        try
        {
            _session = StatedSession();
        }
        catch (ArgumentException breach)
        {
            throw DocumentRefusal.Of(breach);
        }
        fault = !DecisionRules.CrawfordAllows(Kind, _session) ? DecisionRules.CrawfordMessage
            : !DecisionRules.IdAgrees(Id, Kind) ? DecisionRules.IdKindMessage
            : !DecisionRules.StartAgrees(Id, IsStandardStart) ? DecisionRules.StartMessage
            : null;
        if (fault is not null)
            throw new JsonException(fault);
    }

    /// <summary>
    /// Why the session columns are not one kind's — its own stated, the other
    /// kind's empty — or <see langword="null"/> when they are.
    /// </summary>
    private string? SessionColumnsFault()
    {
        bool anyMatch = MatchLength is not null || OnRollNeeds is not null || OpponentNeeds is not null || IsCrawford is not null;
        bool allMatch = MatchLength is not null && OnRollNeeds is not null && OpponentNeeds is not null && IsCrawford is not null;
        bool anyMoney = IsJacoby is not null || IsBeaver is not null || CubeLimit is not null || OnRollScore is not null || OpponentScore is not null;
        bool allMoney = IsJacoby is not null && IsBeaver is not null && CubeLimit is not null && OnRollScore is not null && OpponentScore is not null;
        return SessionKind switch
        {
            SessionKind.Match when !allMatch || anyMoney =>
                "A match row states its MatchLength, OnRollNeeds, OpponentNeeds and IsCrawford, and no money column: IsJacoby, IsBeaver, CubeLimit, OnRollScore and OpponentScore are empty.",
            SessionKind.Money when !allMoney || anyMatch =>
                "A money row states its IsJacoby, IsBeaver, CubeLimit, OnRollScore and OpponentScore, and no match column: MatchLength, OnRollNeeds, OpponentNeeds and IsCrawford are empty.",
            SessionKind.Match or SessionKind.Money => null,
            _ => $"Unknown session kind {SessionKind}.",
        };
    }

    /// <summary>
    /// The session the columns state, built as a document's session is — its
    /// terms, and the standing already seen from the player on roll, which is
    /// how the row states it (no seat to turn it from, so not through
    /// <see cref="Session.Create"/>) — so its kind's rules refuse a breach with
    /// the guard's own exception.
    /// </summary>
    private Session StatedSession() => SessionKind == SessionKind.Match
        ? new MatchSession
        {
            Terms = new MatchTerms { Length = MatchLength!.Value },
            OnRollNeeds = OnRollNeeds!.Value,
            OpponentNeeds = OpponentNeeds!.Value,
            IsCrawford = IsCrawford!.Value,
        }
        : new MoneySession
        {
            Terms = new MoneyTerms { IsJacoby = IsJacoby!.Value, IsBeaver = IsBeaver!.Value, CubeLimit = CubeLimit!.Value },
            OnRollScore = OnRollScore!.Value,
            OpponentScore = OpponentScore!.Value,
        };

    private static bool IsTwoFaces(int roll) =>
        roll / 10 is >= 1 and <= 6 && roll % 10 is >= 1 and <= 6;

    // -----------------------------------------------------------------------
    //  CSV support
    // -----------------------------------------------------------------------

    /// <summary>CSV header row matching the column order of <see cref="ToCsvLine"/>.</summary>
    public static string CsvHeader =>
        "Xgid,Error,MatchScore,MatchLength,Player,SourceFile,Game,MoveNumber,Kind,Roll,AnalysisDepth,Equity,Ranking,Result";

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
            MatchLength?.ToString(invariant),
            CsvEscape(Player),
            CsvEscape(SourceFile),
            Game?.ToString(invariant),
            MoveNumber?.ToString(invariant),
            Kind,
            Roll?.ToString(invariant),
            CsvEscape(AnalysisDepth),
            Equity.ToString("G6", invariant),
            Ranking,
            Result);
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
