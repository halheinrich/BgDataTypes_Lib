using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// The decision category of a <see cref="CubeDecision"/>: the cube analysis,
/// the user's cube errors and played actions, the analysis's facts about each
/// cube action, and the truth among the four cube answers. It carries a cube
/// decision's fields and nothing else — a checker play's are on
/// <see cref="CheckerPlayDecisionData"/>, and no member of either kind stands
/// for "not applicable" (halheinrich/backgammon#273). Every stored member but
/// the nullable ones is <c>required</c>, per the wire rule stated on
/// <see cref="BgDataTypesJsonContext"/>, and each nullable member's
/// documentation says what <see langword="null"/> means. What the stored
/// members determine — the depth label, abbreviation and rank, the loss
/// probabilities, each cube action's equity, the error of each stated action,
/// the best actions and the best answer — is derived and never stored.
///
/// <para>
/// All equities are in normalised cube-equity units from the on-roll
/// (doubler's) perspective, where winning a single game at the current stake
/// is +1 — so an opponent's pass is worth exactly +1. Each cube action's
/// equity, in that perspective, is <see cref="ActionEquity"/>, the one
/// calculation the best actions, the errors, the truth and each answer's cost
/// derive from. All probability fields are fractions in [0, 1] despite the
/// <c>Pct</c> suffix, surfaced verbatim from the producing analyser (XG).
/// </para>
/// <para>
/// What a cube answer costs is computed here, beside the equities, but asked
/// of the record, <see cref="CubeDecision.CostOf"/>: an answer's cost reads
/// whether gammons are possible, a fact of the position and the session that
/// only the record sees.
/// </para>
/// <para>
/// Every stored number — the equities, the probabilities and the analyser's
/// errors — is finite, the rule stated once on the internal
/// <c>FiniteNumber</c>: a NaN or an infinity is refused, by code with an
/// <see cref="ArgumentOutOfRangeException"/> naming the member and by a
/// document with a <see cref="System.Text.Json.JsonException"/> carrying it.
/// The probabilities' range is not checked.
/// </para>
/// <para>
/// The stored members are what the producing analyser stores, and nothing it
/// does not (halheinrich/backgammon#273, Hal's ruling of 2026-09-27). XG's
/// "Pass Justifying Dbl" figure is not among what XG stores, so no member
/// here claims it; deriving and showing it is separate work
/// (halheinrich/backgammon#288).
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
    /// The serializer's constructor. It binds <paramref name="analysisMode"/>, its
    /// first member, only because a serializer constructor must bind one; why
    /// the pattern exists is stated once, on <see cref="BgDataTypesJsonContext"/>
    /// ("The serializer constructors").
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

    // The stored numbers, each finite (the rule stated on the internal
    // FiniteNumber).
    private readonly double _noDoubleEquity;
    private readonly double _doubleTakeEquity;
    private readonly double _cubelessNoDoubleEquity;
    private readonly double _cubelessDoubleTakeEquity;
    private readonly double _winPctAfterNoDouble;
    private readonly double _gammonPctAfterNoDouble;
    private readonly double _bgPctAfterNoDouble;
    private readonly double _loseGammonPctAfterNoDouble;
    private readonly double _loseBgPctAfterNoDouble;
    private readonly double _winPctAfterDoubleTake;
    private readonly double _gammonPctAfterDoubleTake;
    private readonly double _bgPctAfterDoubleTake;
    private readonly double _loseGammonPctAfterDoubleTake;
    private readonly double _loseBgPctAfterDoubleTake;

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
    /// cube actions' equities, the truth and the answers' costs derive from.
    /// </summary>
    public required double NoDoubleEquity
    {
        get => _noDoubleEquity;
        init => _noDoubleEquity = Finite(value, nameof(NoDoubleEquity));
    }

    /// <summary>
    /// Cubeful equity of double/take (doubler's perspective, normalised
    /// cube-equity units). The taker's equity is its negation; a value above
    /// 1 means the opponent should pass. The other input of the cube actions'
    /// equities, the truth and the answers' costs.
    /// </summary>
    public required double DoubleTakeEquity
    {
        get => _doubleTakeEquity;
        init => _doubleTakeEquity = Finite(value, nameof(DoubleTakeEquity));
    }

    /// <summary>Cubeless equity of the no-double evaluation.</summary>
    public required double CubelessNoDoubleEquity
    {
        get => _cubelessNoDoubleEquity;
        init => _cubelessNoDoubleEquity = Finite(value, nameof(CubelessNoDoubleEquity));
    }

    /// <summary>Cubeless equity of the double/take evaluation.</summary>
    public required double CubelessDoubleTakeEquity
    {
        get => _cubelessDoubleTakeEquity;
        init => _cubelessDoubleTakeEquity = Finite(value, nameof(CubelessDoubleTakeEquity));
    }

    // Outcome-probability breakdown of the two cube evaluations, on-roll
    // (doubler's) POV, fractions in [0, 1] surfaced verbatim from XG. Win is
    // the total win probability, and the total loss is derived from it; the
    // gammon and backgammon fields are XG's G/B breakdown figures for the
    // same evaluation, source data.

    /// <summary>Probability the on-roll player wins, from the no-double evaluation. Fraction in [0, 1].</summary>
    public required double WinPctAfterNoDouble
    {
        get => _winPctAfterNoDouble;
        init => _winPctAfterNoDouble = Finite(value, nameof(WinPctAfterNoDouble));
    }
    /// <summary>XG's gammon-win figure (the "G" of its W/G/B breakdown) from the no-double evaluation. Fraction in [0, 1].</summary>
    public required double GammonPctAfterNoDouble
    {
        get => _gammonPctAfterNoDouble;
        init => _gammonPctAfterNoDouble = Finite(value, nameof(GammonPctAfterNoDouble));
    }
    /// <summary>XG's backgammon-win figure (the "B" of its W/G/B breakdown) from the no-double evaluation. Fraction in [0, 1].</summary>
    public required double BgPctAfterNoDouble
    {
        get => _bgPctAfterNoDouble;
        init => _bgPctAfterNoDouble = Finite(value, nameof(BgPctAfterNoDouble));
    }
    /// <summary>
    /// Probability the on-roll player loses, from the no-double evaluation:
    /// <c>1 − </c><see cref="WinPctAfterNoDouble"/>, derived and never stored.
    /// XG's stored figure equals it within 2.4e-7 over the 17,158 cube
    /// decisions of the local corpus (measured by the umbrella, 2026-09-26).
    /// </summary>
    [JsonIgnore]
    public double LosePctAfterNoDouble => 1.0 - WinPctAfterNoDouble;
    /// <summary>XG's gammon-loss figure from the no-double evaluation. Fraction in [0, 1].</summary>
    public required double LoseGammonPctAfterNoDouble
    {
        get => _loseGammonPctAfterNoDouble;
        init => _loseGammonPctAfterNoDouble = Finite(value, nameof(LoseGammonPctAfterNoDouble));
    }
    /// <summary>XG's backgammon-loss figure from the no-double evaluation. Fraction in [0, 1].</summary>
    public required double LoseBgPctAfterNoDouble
    {
        get => _loseBgPctAfterNoDouble;
        init => _loseBgPctAfterNoDouble = Finite(value, nameof(LoseBgPctAfterNoDouble));
    }

    /// <summary>Probability the on-roll player wins, from the double/take evaluation. Fraction in [0, 1].</summary>
    public required double WinPctAfterDoubleTake
    {
        get => _winPctAfterDoubleTake;
        init => _winPctAfterDoubleTake = Finite(value, nameof(WinPctAfterDoubleTake));
    }
    /// <summary>XG's gammon-win figure from the double/take evaluation. Fraction in [0, 1].</summary>
    public required double GammonPctAfterDoubleTake
    {
        get => _gammonPctAfterDoubleTake;
        init => _gammonPctAfterDoubleTake = Finite(value, nameof(GammonPctAfterDoubleTake));
    }
    /// <summary>XG's backgammon-win figure from the double/take evaluation. Fraction in [0, 1].</summary>
    public required double BgPctAfterDoubleTake
    {
        get => _bgPctAfterDoubleTake;
        init => _bgPctAfterDoubleTake = Finite(value, nameof(BgPctAfterDoubleTake));
    }
    /// <summary>
    /// Probability the on-roll player loses, from the double/take evaluation:
    /// <c>1 − </c><see cref="WinPctAfterDoubleTake"/>, derived and never
    /// stored, as <see cref="LosePctAfterNoDouble"/> is.
    /// </summary>
    [JsonIgnore]
    public double LosePctAfterDoubleTake => 1.0 - WinPctAfterDoubleTake;
    /// <summary>XG's gammon-loss figure from the double/take evaluation. Fraction in [0, 1].</summary>
    public required double LoseGammonPctAfterDoubleTake
    {
        get => _loseGammonPctAfterDoubleTake;
        init => _loseGammonPctAfterDoubleTake = Finite(value, nameof(LoseGammonPctAfterDoubleTake));
    }
    /// <summary>XG's backgammon-loss figure from the double/take evaluation. Fraction in [0, 1].</summary>
    public required double LoseBgPctAfterDoubleTake
    {
        get => _loseBgPctAfterDoubleTake;
        init => _loseBgPctAfterDoubleTake = Finite(value, nameof(LoseBgPctAfterDoubleTake));
    }

    // -----------------------------------------------------------------------
    //  Played cube actions
    // -----------------------------------------------------------------------
    //
    //  The record of what was actually played, carried explicitly: it is
    //  source data — a zero error does not identify the action when the two
    //  cube equities tie — and the error of a stated action is derived from
    //  it (UserDoubleError / UserTakeError). Each half is guarded to its own
    //  action domain, the halves CubeAnswerExtensions.Of accepts, and to the
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
    //  states is the analysis's (DoublerActionError / TakerActionError of
    //  that action), derived, never stored. The one error stored is the
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
                FiniteNumber.Check(value, nameof(UnstatedDoublerActionError));
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
                FiniteNumber.Check(value, nameof(UnstatedTakerActionError));
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

    /// <summary><paramref name="value"/>, once it keeps the number rule (<see cref="FiniteNumber"/>).</summary>
    private double Finite(double value, string member)
    {
        try
        {
            FiniteNumber.Check(value, member);
        }
        catch (ArgumentException fault) when (_read)
        {
            throw DocumentRefusal.Of(fault);
        }
        return value;
    }

    private const string UnstatedDoublerMessage =
        "UnstatedDoublerActionError is the error of a doubler action the record does not state; when UserDoublerAction states it, its error is derived from the equities and is not stated.";

    private const string UnstatedTakerMessage =
        "UnstatedTakerActionError is the error of a taker action the record does not state; when UserTakerAction states it, its error is derived from the equities and is not stated.";

    // -----------------------------------------------------------------------
    //  The analysis's cube actions, and the four answers
    // -----------------------------------------------------------------------
    //
    //  The analysis's facts about the cube actions, each half judged on its
    //  own:
    //
    //    * The doubler's double / no-double decision —
    //      BestDoublerAction, DoublerActionError.
    //    * The taker's take / pass decision —
    //      BestTakerAction, TakerActionError.
    //
    //  Above them, the four cube answers of SPEC-scoring §3 (amended
    //  2026-09-30 and 2026-10-01, halheinrich/backgammon#326): the truth,
    //  BestAnswer, and each answer's cost, CostOf, which the record asks with
    //  the gammon fact it alone sees (CubeDecision.CostOf).
    //
    //  All rest on one calculation, ActionEquity: each cube action's equity
    //  from the doubler's side. The pass's normalised value and the rule for
    //  doubling's equity (the taker's best response) are stated there and
    //  nowhere else; the best actions, the errors, the truth and the costs
    //  read them through it. They exist on the cube decision only, so asking
    //  them of a checker play does not compile.

    /// <summary>
    /// The pass's equity for the doubler — always +1 per cube-equity
    /// normalisation. A pass concedes exactly one game at the current stake,
    /// independent of match score or cube value. Read only by
    /// <see cref="ActionEquity"/>, so the value is handed out as the pass's
    /// equity and never on its own (Hal's ruling of 2026-09-27 on
    /// halheinrich/backgammon#273).
    /// </summary>
    private const double PassEquity = 1.0;

    /// <summary>
    /// The equity of <paramref name="action"/> at this cube decision, from the
    /// player on roll's — the doubler's — perspective, whichever half the
    /// action belongs to: normalised cube-equity units, where winning a single
    /// game at the current stake is +1, as every equity on this type is. Higher
    /// is better for the doubler and worse for the taker; the taker's own
    /// equity for an action is this value negated.
    /// </summary>
    /// <remarks>
    /// <para>Each action's equity:</para>
    /// <list type="bullet">
    /// <item><description><see cref="CubeAction.NoDouble"/> — playing on:
    /// <see cref="NoDoubleEquity"/>.</description></item>
    /// <item><description><see cref="CubeAction.Double"/> — the equity of the
    /// taker's best response (<see cref="BestTakerAction"/>): the opponent
    /// answers a double with whichever of take and pass leaves the doubler
    /// less, so doubling is worth the lesser of <see cref="DoubleTakeEquity"/>
    /// and the cash.</description></item>
    /// <item><description><see cref="CubeAction.Take"/> — the double taken:
    /// <see cref="DoubleTakeEquity"/>.</description></item>
    /// <item><description><see cref="CubeAction.Pass"/> — the double passed:
    /// +1, the cash.</description></item>
    /// </list>
    /// <para>
    /// The one calculation of the cube actions' equities
    /// (halheinrich/backgammon#273, Hal's ruling of 2026-09-27): the best
    /// actions, both halves' errors, the truth and each answer's cost are
    /// derived from it, so a consumer showing each action's equity beside its
    /// error shows the analysis's own numbers, consistent with each other, and
    /// restates neither the pass's value nor the rule for doubling's. The
    /// action errors are facts of the analysis: what each action loses
    /// against the best of its half. They are not what a cube answer costs,
    /// which is SPEC-scoring §3's (<see cref="CubeDecision.CostOf"/>): a cost
    /// adds a response the answer commits to, and §3's two ruled conventions
    /// (its "Two conventions" bullet) can make it differ from the action's
    /// error. Derived on each call and never stored: it is not
    /// on the wire, and the flat row carries none.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="action"/> is not a defined <see cref="CubeAction"/>.
    /// </exception>
    public double ActionEquity(CubeAction action) => action switch
    {
        CubeAction.NoDouble => NoDoubleEquity,
        CubeAction.Double => ActionEquity(BestTakerAction),
        CubeAction.Take => DoubleTakeEquity,
        CubeAction.Pass => PassEquity,
        _ => throw new ArgumentOutOfRangeException(nameof(action), action,
            "ActionEquity requires a defined cube action."),
    };

    /// <summary>
    /// The correct atomic doubler action — <see cref="CubeAction.Double"/>
    /// if doubling has higher equity than not doubling against optimal
    /// opponent response, <see cref="CubeAction.NoDouble"/> otherwise.
    /// </summary>
    /// <remarks>
    /// The doubler's atomic decision: whether to offer the cube, the two
    /// actions compared by <see cref="ActionEquity"/>. Tie (doubling worth
    /// exactly what playing on is) favours <see cref="CubeAction.NoDouble"/>.
    /// </remarks>
    [JsonIgnore]
    public CubeAction BestDoublerAction =>
        ActionEquity(CubeAction.Double) > ActionEquity(CubeAction.NoDouble)
            ? CubeAction.Double
            : CubeAction.NoDouble;

    /// <summary>
    /// The correct atomic taker action — <see cref="CubeAction.Take"/>
    /// when taking yields better taker equity than passing,
    /// <see cref="CubeAction.Pass"/> otherwise.
    /// </summary>
    /// <remarks>
    /// The two actions compared by <see cref="ActionEquity"/>, in the
    /// doubler's perspective, so the taker's best is the one that leaves the
    /// doubler less. Tie (<c>DoubleTakeEquity == 1</c>, the take worth exactly
    /// the cash) favours <see cref="CubeAction.Pass"/>.
    /// </remarks>
    [JsonIgnore]
    public CubeAction BestTakerAction =>
        ActionEquity(CubeAction.Take) < ActionEquity(CubeAction.Pass)
            ? CubeAction.Take
            : CubeAction.Pass;

    /// <summary>
    /// The truth among the four cube answers (SPEC-scoring §3, amended
    /// 2026-09-30 on halheinrich/backgammon#326): the answer whose doubling
    /// action is <see cref="BestDoublerAction"/> and whose response is
    /// <see cref="BestTakerAction"/>. So <see cref="CubeAnswer.DoubleTake"/>
    /// or <see cref="CubeAnswer.DoublePass"/> when doubling is best, by the
    /// taker's best response; otherwise <see cref="CubeAnswer.NoDoublePass"/>
    /// when they'd pass, and <see cref="CubeAnswer.NoDouble"/> when they'd
    /// take.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The one derivation of the truth, from the equities alone: whatever the
    /// position, the session or the cube, which decide only what the answer
    /// reads as (<see cref="CubeDecision.ClaimOf"/>) and what each answer
    /// costs. The two halves' tie-breaks stand: not doubling where doubling
    /// and not doubling are worth the same, and the pass where take and pass
    /// are. So at the tie of halheinrich/backgammon#293, a no-double equity
    /// of exactly 1 with a pass, the truth is the fourth answer.
    /// </para>
    /// <para>
    /// The single representative that classification and display read, and
    /// never the test of a correct answer: an answer is correct when its cost
    /// counts as zero (<see cref="CubeDecision.CostOf"/>,
    /// <see cref="EquityDisplay.CountsAsZero"/>), so at an equity tie more than
    /// one answer is correct. The answers whose cost counts as zero are the
    /// record's <see cref="CubeDecision.ZeroCostAnswers"/>, which always hold
    /// this one but are not sized by it.
    /// </para>
    /// </remarks>
    [JsonIgnore]
    public CubeAnswer BestAnswer => CubeAnswerExtensions.Of(BestDoublerAction, BestTakerAction);

    /// <summary>
    /// Equity loss the doubler incurs by choosing <paramref name="action"/>
    /// rather than the optimal doubler action — <c>0</c> if
    /// <paramref name="action"/> matches <see cref="BestDoublerAction"/>,
    /// otherwise the positive equity gap.
    /// </summary>
    /// <remarks>
    /// The gap <c>ActionEquity(BestDoublerAction) − ActionEquity(action)</c>:
    /// both equities are <see cref="ActionEquity"/>'s, the one calculation, so
    /// the error is the difference between the two equities it hands out —
    /// doubling's against optimal opponent response.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="action"/> is not
    /// <see cref="CubeAction.Double"/> or <see cref="CubeAction.NoDouble"/>.
    /// </exception>
    public double DoublerActionError(CubeAction action)
    {
        if (action is not (CubeAction.Double or CubeAction.NoDouble))
            throw new ArgumentOutOfRangeException(nameof(action), action,
                "DoublerActionError requires a doubler-half action (Double or NoDouble).");
        return Gap(ActionEquity(BestDoublerAction), ActionEquity(action));
    }

    /// <summary>
    /// Equity loss the taker incurs by choosing <paramref name="action"/>
    /// rather than the optimal taker action — <c>0</c> if
    /// <paramref name="action"/> matches <see cref="BestTakerAction"/>,
    /// otherwise the positive equity gap (measured from the taker's
    /// perspective).
    /// </summary>
    /// <remarks>
    /// The taker's equities are <see cref="ActionEquity"/>'s negated, so the
    /// gap, taker's best less taker's chosen, is
    /// <c>ActionEquity(action) − ActionEquity(BestTakerAction)</c>: both from
    /// the one calculation, as the doubler's error is.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="action"/> is not
    /// <see cref="CubeAction.Take"/> or <see cref="CubeAction.Pass"/>.
    /// </exception>
    public double TakerActionError(CubeAction action)
    {
        if (action is not (CubeAction.Take or CubeAction.Pass))
            throw new ArgumentOutOfRangeException(nameof(action), action,
                "TakerActionError requires a taker-half action (Take or Pass).");
        return Gap(ActionEquity(action), ActionEquity(BestTakerAction));
    }

    /// <summary>
    /// About the no-double equity of a borderline double/no-double decision:
    /// what No double costs, less this, where gammons are possible and Too
    /// good is right. Hal's convention (SPEC-scoring §3, 2026-09-30,
    /// halheinrich/backgammon#326): No double there has "no action loss, but
    /// there is a serious evaluation error", charged as how far the position
    /// lies beyond the strength No double diagnoses. Hal set 0.6; the local
    /// corpus measured the borderline at about 0.6 overall and about 0.65 in
    /// money (https://github.com/halheinrich/backgammon/issues/326#issuecomment-5921827183).
    /// Read only by <see cref="CostOf"/>, so the convention is stated once.
    /// </summary>
    private const double BorderlineDoubleEquity = 0.6;

    /// <summary>
    /// Whether, by the equities, the fourth answer is right and playing on is
    /// worth more than the cash: they'd pass a double, and the no-double
    /// equity is above the pass's (T ≥ 1, N &gt; 1), strictly, so not at the
    /// tie. Where gammons are possible this is SPEC-scoring §3's "Too good is
    /// right".
    /// </summary>
    private bool PlayingOnBeatsTheCash =>
        BestTakerAction == CubeAction.Pass
        && ActionEquity(CubeAction.NoDouble) > ActionEquity(CubeAction.Pass);

    /// <summary>
    /// What <paramref name="answer"/> costs, in its two parts, given whether
    /// gammons are possible — the record's fact, which is how a consumer asks
    /// it (<see cref="CubeDecision.CostOf"/>, where each part is stated).
    /// Exact: computed from the stored equities as they are, never rounded.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="answer"/> is not one of the four answers.
    /// </exception>
    internal CubeAnswerCost CostOf(CubeAnswer answer, bool gammonsPossible)
    {
        // What not doubling loses: max(0, min(T, 1) − N).
        double noDoubleError = DoublerActionError(CubeAction.NoDouble);
        return answer switch
        {
            // Its implied take is never charged; where gammons are possible
            // and Too good is right, the convention N − 0.6.
            CubeAnswer.NoDouble => new(
                gammonsPossible && PlayingOnBeatsTheCash
                    ? ActionEquity(CubeAction.NoDouble) - BorderlineDoubleEquity
                    : noDoubleError,
                0.0),
            CubeAnswer.DoubleTake => new(
                DoublerActionError(CubeAction.Double), TakerActionError(CubeAction.Take)),
            CubeAnswer.DoublePass => new(
                DoublerActionError(CubeAction.Double), TakerActionError(CubeAction.Pass)),
            // It reads Too good exactly where gammons are possible
            // (CubeDecision.ClaimOf). Too good when they'd take: thinking it
            // worth more than the cash is at least 1 − T off, the doubling
            // part, and calling the take a pass exactly 1 − T, the take part.
            CubeAnswer.NoDoublePass => new(
                gammonsPossible && BestTakerAction == CubeAction.Take
                    ? Gap(ActionEquity(CubeAction.Pass), ActionEquity(CubeAction.Take))
                    : noDoubleError,
                TakerActionError(CubeAction.Pass)),
            _ => throw new ArgumentOutOfRangeException(nameof(answer), answer,
                "A cube answer is one of the four CubeAnswer members."),
        };
    }

    /// <summary>
    /// An error: the gap <paramref name="higher"/> − <paramref name="lower"/>
    /// between two action equities. Never negative, and +0 at a tie whatever
    /// the signs of the tied zeros: <c>-0</c> less <c>+0</c> would otherwise be
    /// <c>-0</c>, which a document writes as <c>-0</c>.
    /// </summary>
    private static double Gap(double higher, double lower) => Math.Max(0.0, higher - lower);
}
