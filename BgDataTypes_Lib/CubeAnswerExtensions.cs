namespace BgDataTypes_Lib;

/// <summary>
/// A cube answer's projections, single-sourced (SPEC-scoring §3, amended
/// 2026-09-30 on halheinrich/backgammon#326): its doubling action, its
/// response, and whether it commits to that response. Each is total over the
/// four answers and refuses any other value with an
/// <see cref="ArgumentOutOfRangeException"/>. They hold whatever the decision:
/// what an answer reads as and what it costs are the decision's
/// (<see cref="CubeDecision.ClaimOf"/>, <see cref="CubeDecision.CostOf"/>).
/// </summary>
public static class CubeAnswerExtensions
{
    /// <summary>
    /// The answer's doubling action: <see cref="CubeAction.Double"/> for
    /// <see cref="CubeAnswer.DoubleTake"/> and <see cref="CubeAnswer.DoublePass"/>,
    /// <see cref="CubeAction.NoDouble"/> for <see cref="CubeAnswer.NoDouble"/>
    /// and <see cref="CubeAnswer.NoDoublePass"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="answer"/> is not one of the four answers.
    /// </exception>
    public static CubeAction DoublerAction(this CubeAnswer answer) => answer switch
    {
        CubeAnswer.NoDouble or CubeAnswer.NoDoublePass => CubeAction.NoDouble,
        CubeAnswer.DoubleTake or CubeAnswer.DoublePass => CubeAction.Double,
        _ => throw Undefined(answer),
    };

    /// <summary>
    /// The answer's response, if doubled: <see cref="CubeAction.Pass"/> for
    /// <see cref="CubeAnswer.DoublePass"/> and <see cref="CubeAnswer.NoDoublePass"/>,
    /// <see cref="CubeAction.Take"/> for <see cref="CubeAnswer.DoubleTake"/> and
    /// for <see cref="CubeAnswer.NoDouble"/>, whose take is implied
    /// (<see cref="CommitsToResponse"/>).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="answer"/> is not one of the four answers.
    /// </exception>
    public static CubeAction TakerAction(this CubeAnswer answer) => answer switch
    {
        CubeAnswer.NoDouble or CubeAnswer.DoubleTake => CubeAction.Take,
        CubeAnswer.DoublePass or CubeAnswer.NoDoublePass => CubeAction.Pass,
        _ => throw Undefined(answer),
    };

    /// <summary>
    /// Whether the answer commits to its response, so that its cost charges
    /// it: <see langword="false"/> for <see cref="CubeAnswer.NoDouble"/> alone.
    /// A doubling answer offers the cube, so its response is charged; the
    /// fourth answer states its pass and is charged for it under either
    /// label; No double offers no cube, so the take it implies is never
    /// charged, and its cost's <see cref="CubeAnswerCost.TakePart"/> is 0
    /// (SPEC-scoring §3, 2026-10-01). It is the one statement of which answers
    /// a take/pass tally counts.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="answer"/> is not one of the four answers.
    /// </exception>
    public static bool CommitsToResponse(this CubeAnswer answer) => answer switch
    {
        CubeAnswer.NoDouble => false,
        CubeAnswer.DoubleTake or CubeAnswer.DoublePass or CubeAnswer.NoDoublePass => true,
        _ => throw Undefined(answer),
    };

    /// <summary>
    /// The answer whose two projections are <paramref name="doubler"/> and
    /// <paramref name="taker"/>: the projections' inverse, since each pair of
    /// a doubler-half and a taker-half action is exactly one answer. It is how
    /// the truth is read off the best actions
    /// (<see cref="CubeDecisionData.BestAnswer"/>).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when either action is not of its own half.
    /// </exception>
    internal static CubeAnswer Of(CubeAction doubler, CubeAction taker) => (doubler, taker) switch
    {
        (CubeAction.NoDouble, CubeAction.Take) => CubeAnswer.NoDouble,
        (CubeAction.Double, CubeAction.Take) => CubeAnswer.DoubleTake,
        (CubeAction.Double, CubeAction.Pass) => CubeAnswer.DoublePass,
        (CubeAction.NoDouble, CubeAction.Pass) => CubeAnswer.NoDoublePass,
        _ => throw new ArgumentOutOfRangeException(
            doubler is CubeAction.NoDouble or CubeAction.Double ? nameof(taker) : nameof(doubler),
            "A cube answer pairs a doubler-half action (NoDouble or Double) with a taker-half action (Take or Pass)."),
    };

    private static ArgumentOutOfRangeException Undefined(CubeAnswer answer) =>
        new(nameof(answer), answer, "A cube answer is one of the four CubeAnswer members.");
}
