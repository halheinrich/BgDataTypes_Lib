namespace BgDataTypes_Lib;

/// <summary>
/// A complete two-part cube answer as SPEC-scoring §3 rules it
/// (halheinrich/backgammon#86): the doubler's three-valued
/// <see cref="CubeClaim"/> and the taker's
/// <see cref="CubeAction.Take"/> / <see cref="CubeAction.Pass"/> response —
/// the correct response <em>if doubled</em>, answered explicitly even when
/// the claim is a no-double. One type serves both roles the spec scores
/// against each other: a user's submitted answer and the derived truth
/// (<see cref="DecisionData.BestClaimPair"/>).
/// </summary>
/// <remarks>
/// <para>
/// The claim-layer counterpart of <see cref="CubeDecisionPair"/>: where that
/// type pairs two board <em>actions</em> (a closed 2×2), this one pairs a
/// claim with a taker action — a closed 3×2 of six cells, each with a named
/// canonical instance. Four cells are the reachable verdicts an analysis can
/// be (<see cref="NoDoubleTake"/>, <see cref="DoubleTake"/>,
/// <see cref="DoublePass"/>, <see cref="TooGoodPass"/>) — the option set
/// consumers offer since the 2026-09-02 amendment
/// (halheinrich/backgammon#187). The other two are representable but not
/// offered: <see cref="TooGoodTake"/>, a verdict retired by that amendment
/// (Too Good requires the pass) and never derived as truth since; and
/// <see cref="NoDoublePass"/>, the incoherent cell — "not good enough to
/// double, yet they'd pass" — derived only on the tie boundary and named by
/// <see cref="IsIncoherent"/> for review surfaces. A data-types library
/// does not hide cells: the closed 3×2 stays whole so that stored answers
/// from any era remain representable. Scoring semantics live with the
/// consuming legs, not here.
/// </para>
/// <para>
/// Each half is validated on construction, paralleling
/// <see cref="CubeDecisionPair"/>'s half-guards: <see cref="Claim"/> must be
/// a defined <see cref="CubeClaim"/> member and <see cref="Taker"/> must be
/// <see cref="CubeAction.Take"/> or <see cref="CubeAction.Pass"/>; anything
/// else throws <see cref="ArgumentOutOfRangeException"/>.
/// </para>
/// <para>
/// <c>default(CubeClaimPair)</c> is <strong>not meaningful</strong>: the
/// <see langword="default"/> of a <see langword="struct"/> bypasses
/// construction and so escapes the half-guards, yielding
/// (<see cref="CubeClaim.NoDouble"/>, <see cref="CubeAction.NoDouble"/>) —
/// whose <see cref="Taker"/> is not a valid taker action. This is the
/// standard value-type caveat shared with <see cref="CubeDecisionPair"/>,
/// <see cref="Play"/> and <see cref="DiceRoll"/>; construct instances
/// explicitly rather than relying on <see langword="default"/>.
/// </para>
/// </remarks>
/// <param name="Claim">
/// The doubler's claim — any defined <see cref="CubeClaim"/> member.
/// </param>
/// <param name="Taker">
/// The taker's response if doubled — <see cref="CubeAction.Take"/> or
/// <see cref="CubeAction.Pass"/>.
/// </param>
public readonly record struct CubeClaimPair(CubeClaim Claim, CubeAction Taker)
{
    /// <summary>
    /// The doubler's claim — always a defined <see cref="CubeClaim"/> member.
    /// </summary>
    public CubeClaim Claim { get; } =
        Claim is CubeClaim.NoDouble or CubeClaim.Double or CubeClaim.TooGood
            ? Claim
            : throw new ArgumentOutOfRangeException(nameof(Claim), Claim,
                "CubeClaimPair.Claim requires a defined CubeClaim member.");

    /// <summary>
    /// The taker's response if doubled — always <see cref="CubeAction.Take"/>
    /// or <see cref="CubeAction.Pass"/>.
    /// </summary>
    public CubeAction Taker { get; } =
        Taker is CubeAction.Take or CubeAction.Pass
            ? Taker
            : throw new ArgumentOutOfRangeException(nameof(Taker), Taker,
                "CubeClaimPair.Taker requires a taker-half action (Take or Pass).");

    // -----------------------------------------------------------------------
    //  Canonical instances — the closed 3×2 of valid pairs
    // -----------------------------------------------------------------------

    /// <summary>
    /// The "no double, take" answer —
    /// (<see cref="CubeClaim.NoDouble"/>, <see cref="CubeAction.Take"/>):
    /// too early to double, and a double would be taken.
    /// </summary>
    public static CubeClaimPair NoDoubleTake { get; } =
        new(CubeClaim.NoDouble, CubeAction.Take);

    /// <summary>
    /// The incoherent "no double, pass" answer —
    /// (<see cref="CubeClaim.NoDouble"/>, <see cref="CubeAction.Pass"/>):
    /// "not good enough to double, yet they'd pass". Selectable, never best
    /// as an answer, and off the boundary never derived as truth — see
    /// <see cref="IsIncoherent"/>.
    /// </summary>
    public static CubeClaimPair NoDoublePass { get; } =
        new(CubeClaim.NoDouble, CubeAction.Pass);

    /// <summary>
    /// The "double, take" answer —
    /// (<see cref="CubeClaim.Double"/>, <see cref="CubeAction.Take"/>):
    /// a correct double the opponent should take.
    /// </summary>
    public static CubeClaimPair DoubleTake { get; } =
        new(CubeClaim.Double, CubeAction.Take);

    /// <summary>
    /// The "double, pass" answer —
    /// (<see cref="CubeClaim.Double"/>, <see cref="CubeAction.Pass"/>):
    /// a correct double the opponent should pass.
    /// </summary>
    public static CubeClaimPair DoublePass { get; } =
        new(CubeClaim.Double, CubeAction.Pass);

    /// <summary>
    /// The "too good, take" answer —
    /// (<see cref="CubeClaim.TooGood"/>, <see cref="CubeAction.Take"/>):
    /// playing on beats cashing, yet the opponent would still take a double —
    /// the cell halheinrich/backgammon#86 originally introduced the claim
    /// layer to represent. <b>Retired as a verdict on 2026-09-02</b>
    /// (halheinrich/backgammon#187): Too Good requires the pass, so
    /// <see cref="DecisionData.BestClaimPair"/> never derives this cell as
    /// truth — such a position is <see cref="NoDoubleTake"/> by ruling — and
    /// consumers do not offer it. Kept representable because a data-types
    /// library does not hide cells of a closed 3×2.
    /// </summary>
    public static CubeClaimPair TooGoodTake { get; } =
        new(CubeClaim.TooGood, CubeAction.Take);

    /// <summary>
    /// The "too good, pass" answer —
    /// (<see cref="CubeClaim.TooGood"/>, <see cref="CubeAction.Pass"/>):
    /// playing on beats cashing, and a double would be passed.
    /// </summary>
    public static CubeClaimPair TooGoodPass { get; } =
        new(CubeClaim.TooGood, CubeAction.Pass);

    // -----------------------------------------------------------------------
    //  Classification
    // -----------------------------------------------------------------------

    /// <summary>
    /// Whether this pair is the incoherent cell — equal to
    /// <see cref="NoDoublePass"/>: claiming the position is not good enough
    /// to double while answering that the opponent would pass. If they would
    /// pass and the position is not too good, cashing beats playing on, so
    /// the claim contradicts the response. Allowed as a user answer by ruling
    /// (SPEC-scoring §3) — choosing it reveals a misunderstanding a review
    /// surface can name, which is what this property exists for.
    /// </summary>
    /// <remarks>
    /// On the non-meaningful <c>default(CubeClaimPair)</c> —
    /// (NoDouble, NoDouble) — this returns <see langword="false"/>; the
    /// default-value caveat in the type remarks still applies.
    /// </remarks>
    public bool IsIncoherent => this == NoDoublePass;
}
