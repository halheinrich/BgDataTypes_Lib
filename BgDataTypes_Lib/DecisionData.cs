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

    /// <summary>Depth class of the cube analysis; see
    /// <see cref="PlayCandidate.DepthClass"/> for semantics.
    /// <see cref="AnalysisDepthClass.Unknown"/> when IsCube is false or
    /// when not stamped (including JSON written before this field existed).</summary>
    public AnalysisDepthClass CubeDepthClass { get; init; }

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

    private void RequireCube()
    {
        if (!IsCube)
            throw new InvalidOperationException(
                "Cube-decision scoring helpers require IsCube to be true.");
    }
}