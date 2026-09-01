namespace BgDataTypes_Lib;

/// <summary>
/// The claim-to-action collapse, single-sourced (SPEC-scoring §3;
/// halheinrich/backgammon#86): both no-double claims perform the identical
/// board action, and this is the one place that says so. Consumers needing
/// the board action behind a claim (e.g. feeding
/// <see cref="DecisionData.DoublerActionError"/> with a claimed answer's
/// action) call <see cref="ToCubeAction"/> rather than re-encoding the
/// mapping. The reverse mapping is deliberately absent — a claim is
/// underdetermined by the action alone; the only action-and-equities-to-claim
/// door is <see cref="DecisionData.BestDoublerClaim"/>.
/// </summary>
public static class CubeClaimExtensions
{
    /// <summary>
    /// The doubler board action a claim resolves to:
    /// <see cref="CubeClaim.Double"/> maps to <see cref="CubeAction.Double"/>;
    /// <see cref="CubeClaim.NoDouble"/> and <see cref="CubeClaim.TooGood"/>
    /// both map to <see cref="CubeAction.NoDouble"/> — the claim layer's
    /// defining collapse.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="claim"/> is not a defined
    /// <see cref="CubeClaim"/> member.
    /// </exception>
    public static CubeAction ToCubeAction(this CubeClaim claim) => claim switch
    {
        CubeClaim.NoDouble or CubeClaim.TooGood => CubeAction.NoDouble,
        CubeClaim.Double => CubeAction.Double,
        _ => throw new ArgumentOutOfRangeException(nameof(claim), claim,
            "ToCubeAction requires a defined CubeClaim member.")
    };
}
