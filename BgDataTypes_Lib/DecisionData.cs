using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// The decision category of a <see cref="BgDecisionData"/>: what was analysed
/// and how the user's choice scored. One record describes either a checker
/// play (<see cref="IsCube"/> false — <see cref="Dice"/>, <see cref="Plays"/>
/// and the <c>UserPlay*</c> fields are live) or a cube decision
/// (<see cref="IsCube"/> true — the <c>Cube*</c> depth fields, the equity /
/// probability fields and the <c>UserDouble*</c> / <c>UserTake*</c> fields
/// are live). Fields of the inactive half hold their defaults.
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
public class DecisionData
{
    /// <summary>Always length 2. Ignored when IsCube is true.</summary>
    public IReadOnlyList<int> Dice { get; init; } = new int[2];

    /// <summary>
    /// The analysed candidate plays of a checker-play decision, in
    /// producer-supplied order (not guaranteed equity-sorted).
    /// <see cref="BestPlayIndex"/> and <see cref="UserPlayIndex"/> index into
    /// this list. Empty when <see cref="IsCube"/> is true.
    /// </summary>
    public IReadOnlyList<PlayCandidate> Plays { get; init; } = [];
    /// <summary>Index into Plays identifying the best play. </summary>
    public int BestPlayIndex { get; init; }
    /// <summary>
    /// Equity loss from the user's checker play vs. the best play (≥ 0).
    /// Null when no user play is recorded or IsCube is true.
    /// </summary>
    public double? UserPlayError { get; init; }
    /// <summary>
    /// Index into Plays identifying the user's play. -1 if not applicable —
    /// no user play recorded (analysis-only position) or a cube decision.
    /// The single source of "which candidate did the user play"; there is
    /// deliberately no per-candidate flag to keep consistent with it.
    /// </summary>
    public int UserPlayIndex { get; init; } = -1;

    /// <summary>
    /// Decision-kind discriminator: true for a cube decision, false for a
    /// checker play. Selects which half of this record is live — see the
    /// class summary.
    /// </summary>
    public bool IsCube { get; init; }

    // -----------------------------------------------------------------------
    //  Cube decision equity fields
    // -----------------------------------------------------------------------
    /// <summary>Analysis depth label for a cube decision, e.g. "3-ply",
    /// "Rollout: 1296 trials. 3-ply". Empty when IsCube is false.</summary>
    public string CubeDepth { get; init; } = string.Empty;

    /// <summary>Compact display form of CubeDepth. Empty when IsCube is false.</summary>
    public string CubeDepthAbbreviation { get; init; } = string.Empty;

    /// <summary>Ordinal ranking of CubeDepth; see PlayCandidate.DepthRank
    /// for semantics. Defaults to 0.</summary>
    public int CubeDepthRank { get; init; }

    /// <summary>How the cube analysis's numbers were produced — the mode axis
    /// of the two-axis depth taxonomy; see
    /// <see cref="PlayCandidate.AnalysisMode"/> for semantics.
    /// <see cref="AnalysisMode.Unknown"/> when IsCube is false or when not
    /// stamped (including JSON written before the two-axis pair existed).</summary>
    public AnalysisMode CubeAnalysisMode { get; init; }

    /// <summary>Evaluation level of the cube analysis — the level axis paired
    /// with <see cref="CubeAnalysisMode"/>. For a rollout this is the inner
    /// cube level, which (unlike checker rows) can be a Roller-family level:
    /// the shipped opening-book database contains cube rollout levels of
    /// XG Roller. See <see cref="PlayCandidate.AnalysisLevel"/> for the
    /// checker-row counterpart. <see cref="AnalysisLevel.Unknown"/> when
    /// IsCube is false or when not stamped.</summary>
    public AnalysisLevel CubeAnalysisLevel { get; init; }

    /// <summary>
    /// Cubeful equity of not doubling (doubler's perspective, normalised
    /// cube-equity units — see the class summary). One of the two inputs the
    /// cube-scoring helpers derive from.
    /// </summary>
    public double NoDoubleEquity { get; init; }

    /// <summary>
    /// Cubeful equity of double/take (doubler's perspective, normalised
    /// cube-equity units). The taker's equity is its negation; a value above
    /// 1 means the opponent should pass. The other input of the cube-scoring
    /// helpers.
    /// </summary>
    public double DoubleTakeEquity { get; init; }

    /// <summary>Cubeless equity of the no-double evaluation. Defaults to 0.0.</summary>
    public double CubelessNoDoubleEquity { get; init; }

    /// <summary>Cubeless equity of the double/take evaluation. Defaults to 0.0.</summary>
    public double CubelessDoubleTakeEquity { get; init; }

    // Outcome-probability breakdown of the two cube evaluations, on-roll
    // (doubler's) POV, fractions in [0, 1] surfaced verbatim from XG. Win/Lose
    // are total win/loss probabilities; the gammon and backgammon fields are
    // XG's G/B breakdown figures for the same evaluation.

    /// <summary>Probability the on-roll player wins, from the no-double evaluation. Fraction in [0, 1].</summary>
    public double WinPctAfterNoDouble { get; init; }
    /// <summary>XG's gammon-win figure (the "G" of its W/G/B breakdown) from the no-double evaluation. Fraction in [0, 1].</summary>
    public double GammonPctAfterNoDouble { get; init; }
    /// <summary>XG's backgammon-win figure (the "B" of its W/G/B breakdown) from the no-double evaluation. Fraction in [0, 1].</summary>
    public double BgPctAfterNoDouble { get; init; }
    /// <summary>Probability the on-roll player loses, from the no-double evaluation. Fraction in [0, 1].</summary>
    public double LosePctAfterNoDouble { get; init; }
    /// <summary>XG's gammon-loss figure from the no-double evaluation. Fraction in [0, 1].</summary>
    public double LoseGammonPctAfterNoDouble { get; init; }
    /// <summary>XG's backgammon-loss figure from the no-double evaluation. Fraction in [0, 1].</summary>
    public double LoseBgPctAfterNoDouble { get; init; }

    /// <summary>Probability the on-roll player wins, from the double/take evaluation. Fraction in [0, 1].</summary>
    public double WinPctAfterDoubleTake { get; init; }
    /// <summary>XG's gammon-win figure from the double/take evaluation. Fraction in [0, 1].</summary>
    public double GammonPctAfterDoubleTake { get; init; }
    /// <summary>XG's backgammon-win figure from the double/take evaluation. Fraction in [0, 1].</summary>
    public double BgPctAfterDoubleTake { get; init; }
    /// <summary>Probability the on-roll player loses, from the double/take evaluation. Fraction in [0, 1].</summary>
    public double LosePctAfterDoubleTake { get; init; }
    /// <summary>XG's gammon-loss figure from the double/take evaluation. Fraction in [0, 1].</summary>
    public double LoseGammonPctAfterDoubleTake { get; init; }
    /// <summary>XG's backgammon-loss figure from the double/take evaluation. Fraction in [0, 1].</summary>
    public double LoseBgPctAfterDoubleTake { get; init; }

    /// <summary>
    /// XG-producer-specific cube statistic, surfaced verbatim: XG's reported
    /// probability that an opponent error would justify the double (shown in
    /// its cube-analysis pane). Fraction in [0, 1]. This library assigns it
    /// no further semantics.
    /// </summary>
    public double ProbOfOpponentErrorJustifyingDouble { get; init; }
    /// <summary>
    /// Equity loss from the user's doubling decision vs. the correct cube action (≥ 0).
    /// Null when no cube decision is recorded or IsCube is false.
    /// </summary>
    public double? UserDoubleError { get; init; }

    /// <summary>
    /// Equity loss from the user's take/drop decision vs. the correct response (≥ 0).
    /// Null when no cube decision is recorded or IsCube is false.
    /// </summary>
    public double? UserTakeError { get; init; }

    // -----------------------------------------------------------------------
    //  Played cube actions
    // -----------------------------------------------------------------------
    //
    //  The record of what was actually played, carried explicitly: the played
    //  action cannot be recovered from UserDoubleError / UserTakeError alone,
    //  because a zero error does not identify the action when the two cube
    //  equities tie. Each half is guarded to its own action domain, mirroring
    //  CubeDecisionPair's half-guards. Cross-half consistency (a recorded
    //  taker response implies the doubler doubled) is a producer contract,
    //  not guarded here — init-only halves are set independently.

    private readonly CubeAction? _userDoublerAction;
    private readonly CubeAction? _userTakerAction;

    /// <summary>
    /// The doubler action the player on roll actually played —
    /// <see cref="CubeAction.NoDouble"/> or <see cref="CubeAction.Double"/>.
    /// Null when the played action is not recorded — which is retroactively
    /// true of all JSON written before this field existed — or when
    /// <see cref="IsCube"/> is false.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown on init when the value is not <see cref="CubeAction.NoDouble"/>,
    /// <see cref="CubeAction.Double"/> or null.
    /// </exception>
    public CubeAction? UserDoublerAction
    {
        get => _userDoublerAction;
        init => _userDoublerAction =
            value is null or CubeAction.NoDouble or CubeAction.Double
                ? value
                : throw new ArgumentOutOfRangeException(nameof(UserDoublerAction), value,
                    "UserDoublerAction requires a doubler-half action (Double or NoDouble).");
    }

    /// <summary>
    /// The taker action the opponent actually played —
    /// <see cref="CubeAction.Take"/> or <see cref="CubeAction.Pass"/>.
    /// Present only when a double was offered and a response recorded: in an
    /// undoubled game no taker decision exists, so this stays null even when
    /// <see cref="UserDoublerAction"/> is recorded. Null also when the played
    /// actions are not recorded (all JSON written before this field existed)
    /// or when <see cref="IsCube"/> is false.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown on init when the value is not <see cref="CubeAction.Take"/>,
    /// <see cref="CubeAction.Pass"/> or null.
    /// </exception>
    public CubeAction? UserTakerAction
    {
        get => _userTakerAction;
        init => _userTakerAction =
            value is null or CubeAction.Take or CubeAction.Pass
                ? value
                : throw new ArgumentOutOfRangeException(nameof(UserTakerAction), value,
                    "UserTakerAction requires a taker-half action (Take or Pass).");
    }

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
    //  no cross-decision overrides. All four throw InvalidOperationException
    //  when IsCube is false — they are only meaningful on cube decisions, and
    //  silent zero / default returns on play decisions would mask misuse.

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
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="IsCube"/> is <see langword="false"/>.
    /// </exception>
    [JsonIgnore]
    public CubeAction BestDoublerAction
    {
        get
        {
            RequireCube();
            return Math.Min(DoubleTakeEquity, PassEquity) > NoDoubleEquity
                ? CubeAction.Double
                : CubeAction.NoDouble;
        }
    }

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
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="IsCube"/> is <see langword="false"/>.
    /// </exception>
    [JsonIgnore]
    public CubeAction BestTakerAction
    {
        get
        {
            RequireCube();
            return DoubleTakeEquity < PassEquity
                ? CubeAction.Take
                : CubeAction.Pass;
        }
    }

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
    /// record — <see cref="BgDecisionData.CanBeTooGood"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="IsCube"/> is <see langword="false"/>.
    /// </exception>
    [JsonIgnore]
    public CubeClaim BestDoublerClaim
    {
        get
        {
            RequireCube();
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
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="IsCube"/> is <see langword="false"/>.
    /// </exception>
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
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="IsCube"/> is <see langword="false"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="action"/> is not
    /// <see cref="CubeAction.Double"/> or <see cref="CubeAction.NoDouble"/>.
    /// </exception>
    public double DoublerActionError(CubeAction action)
    {
        RequireCube();
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
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="IsCube"/> is <see langword="false"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="action"/> is not
    /// <see cref="CubeAction.Take"/> or <see cref="CubeAction.Pass"/>.
    /// </exception>
    public double TakerActionError(CubeAction action)
    {
        RequireCube();
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

    /// <summary>
    /// The single <see cref="IsCube"/> guard behind every cube-only derived
    /// member — here and on the composite record
    /// (<see cref="BgDecisionData.CanBeTooGood"/>), so a non-cube decision
    /// fails the same way from every door.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="IsCube"/> is <see langword="false"/>.
    /// </exception>
    internal void RequireCube()
    {
        if (!IsCube)
            throw new InvalidOperationException(
                "Cube-decision scoring helpers require IsCube to be true.");
    }
}