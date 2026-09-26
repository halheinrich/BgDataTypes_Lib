using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// The decision category of a <see cref="CubeDecision"/>: the cube analysis,
/// the user's cube errors and played actions, and the cube-scoring policy
/// derived from the analysis. It carries a cube decision's fields and nothing
/// else — a checker play's are on <see cref="CheckerPlayDecisionData"/>, and
/// no member of either kind stands for "not applicable"
/// (halheinrich/backgammon#273). Every stored member but the nullable ones is
/// <c>required</c>, per the wire rule stated on
/// <see cref="BgDataTypesJsonContext"/>, and each nullable member's
/// documentation says what <see langword="null"/> means. What the stored
/// members determine — the depth label, abbreviation and rank, the loss
/// probabilities, the error of each stated action, the best actions and
/// claims — is derived and never stored.
///
/// <para>
/// All equities are in normalised cube-equity units from the on-roll
/// (doubler's) perspective, where winning a single game at the current stake
/// is +1 — so an opponent's pass is worth exactly +1 (see
/// <see cref="BestDoublerAction"/>). All probability fields are fractions in
/// [0, 1] despite the <c>Pct</c> suffix, surfaced verbatim from the producing
/// analyser (XG).
/// </para>
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CubeDecisionData
{
    // True while the category is read from a document (see the serializer's
    // constructor below): each rule then refuses as a JsonException.
    private readonly bool _read;

    /// <summary>Creates the category; its members are set by the initializer.</summary>
    public CubeDecisionData()
    {
    }

    /// <summary>
    /// The serializer's constructor, for a category read from a document: it
    /// marks the category as read before any member is set, so every rule
    /// refuses a breach as a <see cref="System.Text.Json.JsonException"/> (the
    /// wire rule on <see cref="BgDataTypesJsonContext"/>). It takes
    /// <paramref name="analysisMode"/> only because a serializer constructor
    /// must bind a member.
    /// </summary>
    [JsonConstructor]
    internal CubeDecisionData(AnalysisMode analysisMode)
    {
        _read = true;
        AnalysisMode = analysisMode;
    }

    // -----------------------------------------------------------------------
    //  The cube analysis: its depth, as typed facts, and what they determine
    // -----------------------------------------------------------------------

    // Null only while construction is still stating them (`required`
    // guarantees the mode and level by the end), so each setter holds the
    // facts stated so far to the taxonomy's rules.
    private readonly AnalysisMode? _mode;
    private readonly AnalysisLevel? _level;
    private readonly int? _rolloutTrials;
    private readonly BookEdition? _bookEdition;
    private readonly int? _unrecognizedLevelCode;

    /// <summary>How the cube analysis's numbers were produced — the mode axis
    /// of the two-axis depth taxonomy; see
    /// <see cref="PlayCandidate.AnalysisMode"/> for semantics.
    /// <see cref="BgDataTypes_Lib.AnalysisMode.Unknown"/> when the producer
    /// did not record it.</summary>
    /// <exception cref="ArgumentException">
    /// Thrown on init when a depth fact already stated does not belong to the mode.
    /// </exception>
    public required AnalysisMode AnalysisMode
    {
        get => _mode.GetValueOrDefault();
        init
        {
            DepthStated(value, _level, _rolloutTrials, _bookEdition, _unrecognizedLevelCode, nameof(AnalysisMode));
            _mode = value;
        }
    }

    /// <summary>Evaluation level of the cube analysis — the level axis paired
    /// with <see cref="AnalysisMode"/>. For a rollout this is the inner cube
    /// level, which (unlike a candidate's) can be a Roller-family level: the
    /// shipped opening-book database contains cube rollout levels of XG
    /// Roller. See <see cref="PlayCandidate.AnalysisLevel"/> for a
    /// candidate's. <see cref="BgDataTypes_Lib.AnalysisLevel.Unknown"/> when
    /// the producer did not record it.</summary>
    /// <exception cref="ArgumentException">
    /// Thrown on init when <see cref="UnrecognizedLevelCode"/> is already
    /// stated and the value is a level this library recognizes.
    /// </exception>
    public required AnalysisLevel AnalysisLevel
    {
        get => _level.GetValueOrDefault();
        init
        {
            DepthStated(_mode, value, _rolloutTrials, _bookEdition, _unrecognizedLevelCode, nameof(AnalysisLevel));
            _level = value;
        }
    }

    /// <summary>
    /// The number of games the rollout behind the cube analysis played; see
    /// <see cref="PlayCandidate.RolloutTrials"/>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown on init when the value is less than 1, or belongs to no rollout.
    /// </exception>
    public int? RolloutTrials
    {
        get => _rolloutTrials;
        init
        {
            DepthStated(_mode, _level, value, _bookEdition, _unrecognizedLevelCode, nameof(RolloutTrials));
            _rolloutTrials = value;
        }
    }

    /// <summary>The opening-book edition of a book hit; see <see cref="PlayCandidate.BookEdition"/>.</summary>
    /// <exception cref="ArgumentException">Thrown on init when the value belongs to no book hit.</exception>
    public BookEdition? BookEdition
    {
        get => _bookEdition;
        init
        {
            DepthStated(_mode, _level, _rolloutTrials, value, _unrecognizedLevelCode, nameof(BookEdition));
            _bookEdition = value;
        }
    }

    /// <summary>
    /// The raw code of a level this library does not recognize; see
    /// <see cref="PlayCandidate.UnrecognizedLevelCode"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown on init when the value is stated for a recognized level.</exception>
    public int? UnrecognizedLevelCode
    {
        get => _unrecognizedLevelCode;
        init
        {
            DepthStated(_mode, _level, _rolloutTrials, _bookEdition, value, nameof(UnrecognizedLevelCode));
            _unrecognizedLevelCode = value;
        }
    }

    /// <summary>
    /// Analysis depth label of the cube analysis, e.g. "3-ply",
    /// "Rollout: 1296 trials. 3-ply"; derived from the typed depth facts and
    /// never stored — see <see cref="PlayCandidate.Depth"/>.
    /// </summary>
    [JsonIgnore]
    public string? Depth => DepthTaxonomy.Label(
        AnalysisMode, AnalysisLevel, RolloutTrials, BookEdition, UnrecognizedLevelCode);

    /// <summary>Compact display form of <see cref="Depth"/>, derived as it is.</summary>
    [JsonIgnore]
    public string? DepthAbbreviation => DepthTaxonomy.Abbreviation(
        AnalysisMode, AnalysisLevel, RolloutTrials, UnrecognizedLevelCode);

    /// <summary>Ordinal ranking of the cube analysis's depth, derived from
    /// <see cref="AnalysisMode"/> and <see cref="AnalysisLevel"/> and never
    /// stored; <see langword="null"/> when the depth is not recorded. See
    /// <see cref="PlayCandidate.DepthRank"/> for semantics.</summary>
    [JsonIgnore]
    public int? DepthRank => DepthTaxonomy.Rank(AnalysisMode, AnalysisLevel);

    /// <summary>
    /// Holds the depth facts stated so far, <paramref name="member"/>'s value
    /// among them, to the taxonomy's rules (<see cref="DepthTaxonomy.Fault"/>).
    /// </summary>
    private void DepthStated(
        AnalysisMode? mode, AnalysisLevel? level, int? trials, BookEdition? edition, int? code, string member)
    {
        try
        {
            if (DepthTaxonomy.Fault(mode, level, trials, edition, code) is { } fault)
                throw new ArgumentException(fault, member);
        }
        catch (ArgumentException fault) when (_read)
        {
            throw DocumentRefusal.Of(fault);
        }
    }

    /// <summary>
    /// Cubeful equity of not doubling (doubler's perspective, normalised
    /// cube-equity units — see the class summary). One of the two inputs the
    /// cube-scoring helpers derive from.
    /// </summary>
    public required double NoDoubleEquity { get; init; }

    /// <summary>
    /// Cubeful equity of double/take (doubler's perspective, normalised
    /// cube-equity units). The taker's equity is its negation; a value above
    /// 1 means the opponent should pass. The other input of the cube-scoring
    /// helpers.
    /// </summary>
    public required double DoubleTakeEquity { get; init; }

    /// <summary>Cubeless equity of the no-double evaluation.</summary>
    public required double CubelessNoDoubleEquity { get; init; }

    /// <summary>Cubeless equity of the double/take evaluation.</summary>
    public required double CubelessDoubleTakeEquity { get; init; }

    // Outcome-probability breakdown of the two cube evaluations, on-roll
    // (doubler's) POV, fractions in [0, 1] surfaced verbatim from XG. Win is
    // the total win probability, and the total loss is derived from it; the
    // gammon and backgammon fields are XG's G/B breakdown figures for the
    // same evaluation, source data.

    /// <summary>Probability the on-roll player wins, from the no-double evaluation. Fraction in [0, 1].</summary>
    public required double WinPctAfterNoDouble { get; init; }
    /// <summary>XG's gammon-win figure (the "G" of its W/G/B breakdown) from the no-double evaluation. Fraction in [0, 1].</summary>
    public required double GammonPctAfterNoDouble { get; init; }
    /// <summary>XG's backgammon-win figure (the "B" of its W/G/B breakdown) from the no-double evaluation. Fraction in [0, 1].</summary>
    public required double BgPctAfterNoDouble { get; init; }
    /// <summary>
    /// Probability the on-roll player loses, from the no-double evaluation:
    /// <c>1 − </c><see cref="WinPctAfterNoDouble"/>, derived and never stored.
    /// XG's stored figure equals it within 2.4e-7 over the 17,158 cube
    /// decisions of the local corpus (measured by the umbrella, 2026-09-26).
    /// </summary>
    [JsonIgnore]
    public double LosePctAfterNoDouble => 1.0 - WinPctAfterNoDouble;
    /// <summary>XG's gammon-loss figure from the no-double evaluation. Fraction in [0, 1].</summary>
    public required double LoseGammonPctAfterNoDouble { get; init; }
    /// <summary>XG's backgammon-loss figure from the no-double evaluation. Fraction in [0, 1].</summary>
    public required double LoseBgPctAfterNoDouble { get; init; }

    /// <summary>Probability the on-roll player wins, from the double/take evaluation. Fraction in [0, 1].</summary>
    public required double WinPctAfterDoubleTake { get; init; }
    /// <summary>XG's gammon-win figure from the double/take evaluation. Fraction in [0, 1].</summary>
    public required double GammonPctAfterDoubleTake { get; init; }
    /// <summary>XG's backgammon-win figure from the double/take evaluation. Fraction in [0, 1].</summary>
    public required double BgPctAfterDoubleTake { get; init; }
    /// <summary>
    /// Probability the on-roll player loses, from the double/take evaluation:
    /// <c>1 − </c><see cref="WinPctAfterDoubleTake"/>, derived and never
    /// stored, as <see cref="LosePctAfterNoDouble"/> is.
    /// </summary>
    [JsonIgnore]
    public double LosePctAfterDoubleTake => 1.0 - WinPctAfterDoubleTake;
    /// <summary>XG's gammon-loss figure from the double/take evaluation. Fraction in [0, 1].</summary>
    public required double LoseGammonPctAfterDoubleTake { get; init; }
    /// <summary>XG's backgammon-loss figure from the double/take evaluation. Fraction in [0, 1].</summary>
    public required double LoseBgPctAfterDoubleTake { get; init; }

    /// <summary>
    /// XG-producer-specific cube statistic, surfaced verbatim: XG's reported
    /// probability that an opponent error would justify the double (shown in
    /// its cube-analysis pane). Fraction in [0, 1]. This library assigns it
    /// no further semantics.
    /// </summary>
    public required double ProbOfOpponentErrorJustifyingDouble { get; init; }

    // -----------------------------------------------------------------------
    //  Played cube actions
    // -----------------------------------------------------------------------
    //
    //  The record of what was actually played, carried explicitly: it is
    //  source data — a zero error does not identify the action when the two
    //  cube equities tie — and the error of a stated action is derived from
    //  it (UserDoubleError / UserTakeError). Each half is guarded to its own
    //  action domain, mirroring CubeDecisionPair's half-guards, and to the
    //  absence of an unstated-action error for the same half. Cross-half
    //  consistency (a recorded taker response implies the doubler doubled)
    //  is a producer contract, not guarded here — init-only halves are set
    //  independently.

    private readonly CubeAction? _userDoublerAction;
    private readonly CubeAction? _userTakerAction;

    /// <summary>
    /// The doubler action the player on roll actually played —
    /// <see cref="CubeAction.NoDouble"/> or <see cref="CubeAction.Double"/>.
    /// Null when the played action is not recorded.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown on init when the value is not <see cref="CubeAction.NoDouble"/>,
    /// <see cref="CubeAction.Double"/> or null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown on init when the value is stated and
    /// <see cref="UnstatedDoublerActionError"/> already is.
    /// </exception>
    public CubeAction? UserDoublerAction
    {
        get => _userDoublerAction;
        init
        {
            try
            {
                if (value is not (null or CubeAction.NoDouble or CubeAction.Double))
                    throw new ArgumentOutOfRangeException(nameof(UserDoublerAction), value,
                        "UserDoublerAction requires a doubler-half action (Double or NoDouble).");
                if (value is not null && _unstatedDoublerActionError is not null)
                    throw new ArgumentException(UnstatedDoublerMessage, nameof(UserDoublerAction));
            }
            catch (ArgumentException fault) when (_read)
            {
                throw DocumentRefusal.Of(fault);
            }
            _userDoublerAction = value;
        }
    }

    /// <summary>
    /// The taker action the opponent actually played —
    /// <see cref="CubeAction.Take"/> or <see cref="CubeAction.Pass"/>.
    /// Present only when a double was offered and a response recorded: in an
    /// undoubled game no taker decision exists, so this stays null even when
    /// <see cref="UserDoublerAction"/> is recorded. Null also when the played
    /// actions are not recorded.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown on init when the value is not <see cref="CubeAction.Take"/>,
    /// <see cref="CubeAction.Pass"/> or null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown on init when the value is stated and
    /// <see cref="UnstatedTakerActionError"/> already is.
    /// </exception>
    public CubeAction? UserTakerAction
    {
        get => _userTakerAction;
        init
        {
            try
            {
                if (value is not (null or CubeAction.Take or CubeAction.Pass))
                    throw new ArgumentOutOfRangeException(nameof(UserTakerAction), value,
                        "UserTakerAction requires a taker-half action (Take or Pass).");
                if (value is not null && _unstatedTakerActionError is not null)
                    throw new ArgumentException(UnstatedTakerMessage, nameof(UserTakerAction));
            }
            catch (ArgumentException fault) when (_read)
            {
                throw DocumentRefusal.Of(fault);
            }
            _userTakerAction = value;
        }
    }

    // -----------------------------------------------------------------------
    //  The user's errors
    //
    //  No stored copy of a derivable value: the error of an action the record
    //  states is the scoring policy's (DoublerActionError / TakerActionError
    //  of that action), derived, never stored. The one error stored is the
    //  one nothing here determines — the analyser's error for a half whose
    //  played action the record does not state. A document still stating
    //  UserDoubleError or UserTakeError (or DepthRank) reads with it ignored
    //  and the derivation stands: the serializer skips a derived member's
    //  JSON even where it refuses a member the category does not have.
    // -----------------------------------------------------------------------

    private readonly double? _unstatedDoublerActionError;
    private readonly double? _unstatedTakerActionError;

    /// <summary>
    /// Equity loss from the user's doubling decision against the correct
    /// doubler action (≥ 0): the <see cref="DoublerActionError"/> of
    /// <see cref="UserDoublerAction"/> when the record states it, otherwise
    /// the analyser's <see cref="UnstatedDoublerActionError"/>.
    /// <see langword="null"/> when neither exists. Derived, never stored.
    /// </summary>
    [JsonIgnore]
    public double? UserDoubleError =>
        UserDoublerAction is CubeAction action ? DoublerActionError(action) : UnstatedDoublerActionError;

    /// <summary>
    /// Equity loss from the user's take/pass decision against the correct
    /// response (≥ 0): the <see cref="TakerActionError"/> of
    /// <see cref="UserTakerAction"/> when the record states it, otherwise the
    /// analyser's <see cref="UnstatedTakerActionError"/>.
    /// <see langword="null"/> when neither exists — in particular when no
    /// double was offered. Derived, never stored.
    /// </summary>
    [JsonIgnore]
    public double? UserTakeError =>
        UserTakerAction is CubeAction action ? TakerActionError(action) : UnstatedTakerActionError;

    /// <summary>
    /// The doubling error the producing analyser recorded for a decision
    /// whose played doubler action the record does not state
    /// (<see cref="UserDoublerAction"/> <see langword="null"/>) — the one
    /// doubling error nothing here determines. <see langword="null"/> when
    /// none was recorded, and always when the action is stated: its error is
    /// then derived (<see cref="UserDoubleError"/>), so stating it here too
    /// would store a copy.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown on init when the value is stated and <see cref="UserDoublerAction"/>
    /// already is.
    /// </exception>
    public double? UnstatedDoublerActionError
    {
        get => _unstatedDoublerActionError;
        init
        {
            try
            {
                if (value is not null && _userDoublerAction is not null)
                    throw new ArgumentException(UnstatedDoublerMessage, nameof(UnstatedDoublerActionError));
            }
            catch (ArgumentException fault) when (_read)
            {
                throw DocumentRefusal.Of(fault);
            }
            _unstatedDoublerActionError = value;
        }
    }

    /// <summary>
    /// The take/pass error the producing analyser recorded for a decision
    /// whose played taker action the record does not state
    /// (<see cref="UserTakerAction"/> <see langword="null"/>), as
    /// <see cref="UnstatedDoublerActionError"/> is for the doubler half.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown on init when the value is stated and <see cref="UserTakerAction"/>
    /// already is.
    /// </exception>
    public double? UnstatedTakerActionError
    {
        get => _unstatedTakerActionError;
        init
        {
            try
            {
                if (value is not null && _userTakerAction is not null)
                    throw new ArgumentException(UnstatedTakerMessage, nameof(UnstatedTakerActionError));
            }
            catch (ArgumentException fault) when (_read)
            {
                throw DocumentRefusal.Of(fault);
            }
            _unstatedTakerActionError = value;
        }
    }

    private const string UnstatedDoublerMessage =
        "UnstatedDoublerActionError is the error of a doubler action the record does not state; when UserDoublerAction states it, its error is derived from the equities and is not stated.";

    private const string UnstatedTakerMessage =
        "UnstatedTakerActionError is the error of a taker action the record does not state; when UserTakerAction states it, its error is derived from the equities and is not stated.";

    // -----------------------------------------------------------------------
    //  Cube-decision scoring helpers
    // -----------------------------------------------------------------------
    //
    //  Single-source policy for judging a cube decision as two independent
    //  atomic decisions, each scored on its own:
    //
    //    * The doubler's double / no-double decision —
    //      BestDoublerAction, DoublerActionError.
    //    * The taker's take / pass decision —
    //      BestTakerAction, TakerActionError.
    //
    //  Pure equity-loss between two cube actions, evaluated separately, with
    //  no cross-decision overrides. They exist on the cube decision only, so
    //  asking them of a checker play does not compile.

    /// <summary>
    /// Equity the doubler earns when the opponent passes a double — always
    /// 1.0 per cube-equity normalisation. A pass forfeits exactly one cube
    /// by definition, independent of match score or cube value.
    /// </summary>
    private const double PassEquity = 1.0;

    /// <summary>
    /// The correct atomic doubler action — <see cref="CubeAction.Double"/>
    /// if doubling has higher equity than not doubling against optimal
    /// opponent response, <see cref="CubeAction.NoDouble"/> otherwise.
    /// </summary>
    /// <remarks>
    /// The doubler's atomic decision: whether to offer the cube. Tie
    /// (<c>min(DoubleTakeEquity, 1) == NoDoubleEquity</c>) favours
    /// <see cref="CubeAction.NoDouble"/>.
    /// </remarks>
    [JsonIgnore]
    public CubeAction BestDoublerAction =>
        Math.Min(DoubleTakeEquity, PassEquity) > NoDoubleEquity
            ? CubeAction.Double
            : CubeAction.NoDouble;

    /// <summary>
    /// The correct atomic taker action — <see cref="CubeAction.Take"/>
    /// when taking yields better taker equity than passing,
    /// <see cref="CubeAction.Pass"/> otherwise.
    /// </summary>
    /// <remarks>
    /// Determined from the doubler's <see cref="DoubleTakeEquity"/>: the
    /// taker's take equity is its negation, and pass equity is
    /// <c>-1</c>. Tie (<c>DoubleTakeEquity == 1</c>) favours
    /// <see cref="CubeAction.Pass"/>.
    /// </remarks>
    [JsonIgnore]
    public CubeAction BestTakerAction =>
        DoubleTakeEquity < PassEquity
            ? CubeAction.Take
            : CubeAction.Pass;

    /// <summary>
    /// The correct doubler <em>claim</em> — <see cref="BestDoublerAction"/>
    /// widened to the three-valued claim layer of SPEC-scoring §3
    /// (halheinrich/backgammon#86; amended 2026-09-02 by
    /// halheinrich/backgammon#187): <see cref="CubeClaim.Double"/> when
    /// doubling is best; otherwise <see cref="CubeClaim.TooGood"/> when
    /// playing on is worth more than the cashed point
    /// (<see cref="NoDoubleEquity"/> strictly above the pass equity 1)
    /// <em>and</em> the opponent would pass a double
    /// (<see cref="BestTakerAction"/> is <see cref="CubeAction.Pass"/>),
    /// else <see cref="CubeClaim.NoDouble"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The one derivation site of the truth claim in the ecosystem, beside
    /// its action-level siblings — consumers never re-derive (SPEC-scoring
    /// §3's encapsulation rule). Implements the ratified predicate verbatim:
    /// Too Good ⟺ best doubler action is NoDouble <b>and</b>
    /// <c>NoDoubleEquity &gt; 1</c> <b>and</b> best taker action is Pass —
    /// the 2026-09-02 amendment's third term: Too Good requires the pass.
    /// </para>
    /// <para>
    /// The rationale, cell by cell of the no-double half:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// <b>Too good / Pass</b> — playing on beats the cash <em>and</em> they
    /// would pass: the roller declines a point the opponent would concede
    /// because the game is worth more played out. The only Too Good cell.
    /// </description></item>
    /// <item><description>
    /// <b>No double / Take, with a no-double equity above 1</b> — playing on
    /// beats being taken, the opponent takes, and no pass is involved: the
    /// roller refrains because a double would be taken and playing on beats
    /// that, not because any cash was declined. A No double <em>by
    /// ruling</em>: XG labels such a position "Too good to double/Take"
    /// (<c>TooGoodAndTake.xgp</c> — no double +1.1711, double/take +0.6004,
    /// the position that decided the amendment), and the quiz cannot teach
    /// a distinction its players do not make.
    /// </description></item>
    /// <item><description>
    /// <b>No double / Take, with a no-double equity at or below 1</b> — not
    /// good enough to double; the ordinary cell.
    /// </description></item>
    /// </list>
    /// <para>
    /// The equity comparison is strict, so at <c>NoDoubleEquity == 1</c>
    /// exactly (playing on worth exactly the cash) the claim stays
    /// <see cref="CubeClaim.NoDouble"/> — the same tie-favours-NoDouble
    /// posture as <see cref="BestDoublerAction"/>; at that boundary with a
    /// pass, <see cref="BestClaimPair"/> composes the incoherent cell as
    /// before. The derivation reads equities only: no match-score, money, or
    /// Jacoby context enters (Too Good occurs in money too, via Jacoby
    /// redoubles). Whether the verdict <em>can</em> occur at a position is a
    /// separate fact of the rules context, derived beside this one on the
    /// record — <see cref="CubeDecision.CanBeTooGood"/>.
    /// </para>
    /// </remarks>
    [JsonIgnore]
    public CubeClaim BestDoublerClaim
    {
        get
        {
            if (BestDoublerAction == CubeAction.Double)
                return CubeClaim.Double;
            return NoDoubleEquity > PassEquity && BestTakerAction == CubeAction.Pass
                ? CubeClaim.TooGood
                : CubeClaim.NoDouble;
        }
    }

    /// <summary>
    /// The derived truth of the whole cube decision as a two-part claim
    /// answer — (<see cref="BestDoublerClaim"/>,
    /// <see cref="BestTakerAction"/>) — the pair a submitted
    /// <see cref="CubeClaimPair"/> is scored against, half by half
    /// (SPEC-scoring §3; halheinrich/backgammon#86). This is the producer
    /// verdict the answer-type classification consumes; consumers never walk
    /// the equities themselves.
    /// </summary>
    /// <remarks>
    /// Off the tie boundaries this lands in one of the four reachable verdict
    /// cells of SPEC-scoring §3 — <see cref="CubeClaimPair.NoDoubleTake"/>,
    /// <see cref="CubeClaimPair.DoubleTake"/>,
    /// <see cref="CubeClaimPair.DoublePass"/>,
    /// <see cref="CubeClaimPair.TooGoodPass"/>; since the 2026-09-02
    /// amendment (halheinrich/backgammon#187) Too Good requires the pass, so
    /// <see cref="CubeClaimPair.TooGoodTake"/> is never derived. At
    /// <c>NoDoubleEquity == 1</c> exactly with
    /// <c>DoubleTakeEquity &gt;= 1</c>, both halves tie and their ruled
    /// tie-breaks (NoDouble; Pass) compose to
    /// <see cref="CubeClaimPair.NoDoublePass"/> — the incoherent cell as
    /// derived truth, on a measure-zero boundary where every answer's equity
    /// is identical. Pinned by test as the spec-literal reading; flagged to
    /// the umbrella as a candidate spec sharpening rather than silently
    /// rounded away here.
    /// </remarks>
    [JsonIgnore]
    public CubeClaimPair BestClaimPair => new(BestDoublerClaim, BestTakerAction);

    /// <summary>
    /// Equity loss the doubler incurs by choosing <paramref name="action"/>
    /// rather than the optimal doubler action — <c>0</c> if
    /// <paramref name="action"/> matches <see cref="BestDoublerAction"/>,
    /// otherwise the positive equity gap.
    /// </summary>
    /// <remarks>
    /// <c>Double</c>'s value is computed against optimal opponent response
    /// (<c>min(DoubleTakeEquity, 1)</c>); <c>NoDouble</c>'s value is
    /// <see cref="NoDoubleEquity"/>.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="action"/> is not
    /// <see cref="CubeAction.Double"/> or <see cref="CubeAction.NoDouble"/>.
    /// </exception>
    public double DoublerActionError(CubeAction action)
    {
        double actionEquity = action switch
        {
            CubeAction.Double   => Math.Min(DoubleTakeEquity, PassEquity),
            CubeAction.NoDouble => NoDoubleEquity,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action,
                "DoublerActionError requires a doubler-half action (Double or NoDouble).")
        };
        double bestEquity = Math.Max(Math.Min(DoubleTakeEquity, PassEquity), NoDoubleEquity);
        return Math.Max(0.0, bestEquity - actionEquity);
    }

    /// <summary>
    /// Equity loss the taker incurs by choosing <paramref name="action"/>
    /// rather than the optimal taker action — <c>0</c> if
    /// <paramref name="action"/> matches <see cref="BestTakerAction"/>,
    /// otherwise the positive equity gap (measured from the taker's
    /// perspective).
    /// </summary>
    /// <remarks>
    /// Taker equities are the doubler's negated: <c>Take</c> ⇒
    /// <c>-DoubleTakeEquity</c>; <c>Pass</c> ⇒ <c>-1</c>.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="action"/> is not
    /// <see cref="CubeAction.Take"/> or <see cref="CubeAction.Pass"/>.
    /// </exception>
    public double TakerActionError(CubeAction action)
    {
        double actionEquity = action switch
        {
            CubeAction.Take => -DoubleTakeEquity,
            CubeAction.Pass => -PassEquity,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action,
                "TakerActionError requires a taker-half action (Take or Pass).")
        };
        double bestEquity = Math.Max(-DoubleTakeEquity, -PassEquity);
        return Math.Max(0.0, bestEquity - actionEquity);
    }
}
