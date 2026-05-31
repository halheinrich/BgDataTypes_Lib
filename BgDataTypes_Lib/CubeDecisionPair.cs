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
/// The aggregate verdict — whether the pair is correct, and the mapping from
/// pair to verdict — is intentionally absent. It returns later alongside
/// <c>CubeVerdict</c> on a cleaner footing; see the umbrella
/// <c>INSTRUCTIONS.md</c> Deferred section.
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
}
