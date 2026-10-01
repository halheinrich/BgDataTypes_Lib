namespace BgDataTypes_Lib;

/// <summary>
/// A cube answer's projections, single-sourced (SPEC-scoring §3, amended
/// 2026-09-30 on halheinrich/backgammon#326): its doubling action, its
/// response, and whether it commits to that response, with their inverse,
/// <see cref="Of"/>. Each projection is total over the four answers, and every
/// member refuses a value outside its domain with an
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
    /// a doubler-half and a taker-half action is exactly one answer —
    /// (<see cref="CubeAction.NoDouble"/>, <see cref="CubeAction.Take"/>) is
    /// <see cref="CubeAnswer.NoDouble"/>, (<see cref="CubeAction.Double"/>,
    /// <see cref="CubeAction.Take"/>) <see cref="CubeAnswer.DoubleTake"/>,
    /// (<see cref="CubeAction.Double"/>, <see cref="CubeAction.Pass"/>)
    /// <see cref="CubeAnswer.DoublePass"/>, and
    /// (<see cref="CubeAction.NoDouble"/>, <see cref="CubeAction.Pass"/>)
    /// <see cref="CubeAnswer.NoDoublePass"/>.
    /// </summary>
    /// <remarks>
    /// The one way two actions become an answer: the truth is read off the
    /// two best actions this way (<see cref="CubeDecisionData.BestAnswer"/>),
    /// and a consumer holding two recorded actions forms the answer this way
    /// too, then reads its label through <see cref="CubeDecision.ClaimOf"/> —
    /// so a recorded no double with a pass reads Too good only where gammons
    /// are possible. It replaces the action pair, <c>CubeDecisionPair</c>,
    /// retired as a second statement of the same four-valued domain
    /// (halheinrich/backgammon#326). Which recorded halves are trustworthy is
    /// the caller's to decide before it asks.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="doubler"/> is not
    /// <see cref="CubeAction.NoDouble"/> or <see cref="CubeAction.Double"/>,
    /// or <paramref name="taker"/> is not <see cref="CubeAction.Take"/> or
    /// <see cref="CubeAction.Pass"/>; the doubler is checked first.
    /// </exception>
    public static CubeAnswer Of(CubeAction doubler, CubeAction taker) => (doubler, taker) switch
    {
        (CubeAction.NoDouble, CubeAction.Take) => CubeAnswer.NoDouble,
        (CubeAction.Double, CubeAction.Take) => CubeAnswer.DoubleTake,
        (CubeAction.Double, CubeAction.Pass) => CubeAnswer.DoublePass,
        (CubeAction.NoDouble, CubeAction.Pass) => CubeAnswer.NoDoublePass,
        (CubeAction.NoDouble or CubeAction.Double, _) => throw new ArgumentOutOfRangeException(
            nameof(taker), taker, "A cube answer's taker action is Take or Pass."),
        _ => throw new ArgumentOutOfRangeException(
            nameof(doubler), doubler, "A cube answer's doubler action is NoDouble or Double."),
    };

    private static ArgumentOutOfRangeException Undefined(CubeAnswer answer) =>
        new(nameof(answer), answer, "A cube answer is one of the four CubeAnswer members.");
}
