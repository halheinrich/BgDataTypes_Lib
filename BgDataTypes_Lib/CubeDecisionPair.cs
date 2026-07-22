namespace BgDataTypes_Lib;

/// <summary>
/// A complete cube decision expressed as its two atomic actions: the doubler's
/// <see cref="CubeAction.Double"/> / <see cref="CubeAction.NoDouble"/> choice
/// and the taker's <see cref="CubeAction.Take"/> / <see cref="CubeAction.Pass"/>
/// choice.
/// </summary>
/// <remarks>
/// <para>
/// Each half is validated on construction, paralleling the half-guards on
/// <see cref="DecisionData.DoublerActionError"/> and
/// <see cref="DecisionData.TakerActionError"/>: <see cref="Doubler"/> must be
/// <see cref="CubeAction.NoDouble"/> or <see cref="CubeAction.Double"/>, and
/// <see cref="Taker"/> must be <see cref="CubeAction.Take"/> or
/// <see cref="CubeAction.Pass"/>. A cross-half value
/// (e.g. <see cref="CubeAction.Take"/> as the doubler action) throws
/// <see cref="ArgumentOutOfRangeException"/>.
/// </para>
/// <para>
/// The valid domain is a closed 2×2: exactly four pairs, each with a named
/// canonical instance — <see cref="NoDoubleTake"/>, <see cref="TooGood"/>,
/// <see cref="DoubleTake"/>, <see cref="DoublePass"/>. The Too-Good
/// classification lives here as <see cref="IsTooGood"/>: (NoDouble, Pass)
/// means the doubler is too good to double. The aggregate verdict type
/// (<c>CubeVerdict</c>) and its scoring semantics remain deferred; see the
/// umbrella <c>INSTRUCTIONS.md</c> Deferred section.
/// </para>
/// <para>
/// <c>default(CubeDecisionPair)</c> is <strong>not meaningful</strong>: the
/// <see langword="default"/> of a <see langword="struct"/> bypasses
/// construction and so escapes the half-guards, yielding
/// <c>(NoDouble, NoDouble)</c> — whose <see cref="Taker"/> is not a valid
/// taker action. This is the standard value-type caveat; construct instances
/// explicitly rather than relying on <see langword="default"/>.
/// </para>
/// </remarks>
/// <param name="Doubler">
/// The doubler's atomic action — <see cref="CubeAction.NoDouble"/> or
/// <see cref="CubeAction.Double"/>.
/// </param>
/// <param name="Taker">
/// The taker's atomic action — <see cref="CubeAction.Take"/> or
/// <see cref="CubeAction.Pass"/>.
/// </param>
public readonly record struct CubeDecisionPair(CubeAction Doubler, CubeAction Taker)
{
    /// <summary>
    /// The doubler's atomic action — always <see cref="CubeAction.NoDouble"/>
    /// or <see cref="CubeAction.Double"/>.
    /// </summary>
    public CubeAction Doubler { get; } =
        Doubler is CubeAction.NoDouble or CubeAction.Double
            ? Doubler
            : throw new ArgumentOutOfRangeException(nameof(Doubler), Doubler,
                "CubeDecisionPair.Doubler requires a doubler-half action (Double or NoDouble).");

    /// <summary>
    /// The taker's atomic action — always <see cref="CubeAction.Take"/> or
    /// <see cref="CubeAction.Pass"/>.
    /// </summary>
    public CubeAction Taker { get; } =
        Taker is CubeAction.Take or CubeAction.Pass
            ? Taker
            : throw new ArgumentOutOfRangeException(nameof(Taker), Taker,
                "CubeDecisionPair.Taker requires a taker-half action (Take or Pass).");

    // -----------------------------------------------------------------------
    //  Canonical instances — the closed 2×2 of valid pairs
    // -----------------------------------------------------------------------

    /// <summary>
    /// The "no double, take" pair —
    /// (<see cref="CubeAction.NoDouble"/>, <see cref="CubeAction.Take"/>):
    /// the doubler should not double, and a double would be taken.
    /// </summary>
    public static CubeDecisionPair NoDoubleTake { get; } =
        new(CubeAction.NoDouble, CubeAction.Take);

    /// <summary>
    /// The "too good to double" pair —
    /// (<see cref="CubeAction.NoDouble"/>, <see cref="CubeAction.Pass"/>):
    /// playing on is worth more than cashing, so the doubler should not
    /// double, and a double would be passed. See <see cref="IsTooGood"/>.
    /// </summary>
    public static CubeDecisionPair TooGood { get; } =
        new(CubeAction.NoDouble, CubeAction.Pass);

    /// <summary>
    /// The "double, take" pair —
    /// (<see cref="CubeAction.Double"/>, <see cref="CubeAction.Take"/>):
    /// the doubler should double, and the taker should take.
    /// </summary>
    public static CubeDecisionPair DoubleTake { get; } =
        new(CubeAction.Double, CubeAction.Take);

    /// <summary>
    /// The "double, pass" pair —
    /// (<see cref="CubeAction.Double"/>, <see cref="CubeAction.Pass"/>):
    /// the doubler should double, and the taker should pass.
    /// </summary>
    public static CubeDecisionPair DoublePass { get; } =
        new(CubeAction.Double, CubeAction.Pass);

    // -----------------------------------------------------------------------
    //  Classification
    // -----------------------------------------------------------------------

    /// <summary>
    /// Whether this pair is the "too good to double" case — equal to
    /// <see cref="TooGood"/>, i.e. (<see cref="CubeAction.NoDouble"/>,
    /// <see cref="CubeAction.Pass"/>): the doubler wins more by playing on
    /// (typically for a gammon) than by doubling and cashing.
    /// </summary>
    /// <remarks>
    /// On the non-meaningful <c>default(CubeDecisionPair)</c> —
    /// (NoDouble, NoDouble) — this returns <see langword="false"/>; the
    /// default-value caveat in the type remarks still applies.
    /// </remarks>
    public bool IsTooGood => this == TooGood;
}
