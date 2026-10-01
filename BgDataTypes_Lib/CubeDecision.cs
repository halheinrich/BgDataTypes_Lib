using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// A cube decision: whether the player on roll doubles, and whether the
/// opponent takes. One of the two kinds of <see cref="BgDecisionData"/>; it
/// carries a cube decision's fields and nothing else — see
/// <see cref="CubeDecisionData"/> for its <see cref="Decision"/> category,
/// which also holds the analysis's facts about each cube action and the truth
/// among the four cube answers. A cube decision is never made in the Crawford
/// game: its <see cref="BgDecisionData.Position"/> refuses a Crawford position
/// (<see cref="DecisionRules.CrawfordMessage"/>, halheinrich/backgammon#201).
/// </summary>
/// <remarks>
/// What needs the position and the session beside the analysis is the
/// record's: whether gammons are possible (<see cref="GammonsPossible"/>),
/// and so what each cube answer reads as (<see cref="ClaimOf"/>) and what it
/// costs (<see cref="CostOf"/>), per SPEC-scoring §3 as amended on
/// halheinrich/backgammon#326.
/// </remarks>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CubeDecision : BgDecisionData
{
    // Null only while construction is still setting it (see BgDecisionData).
    private readonly CubeDecisionData? _decision;

    /// <summary>Creates a cube decision; its members are set by the initializer.</summary>
    public CubeDecision() : base(DecisionKind.Cube)
    {
    }

    /// <summary>
    /// The serializer's constructor. It binds the document's
    /// <paramref name="kind"/>, which must be <see cref="DecisionKind.Cube"/>;
    /// why the pattern exists is stated once, on
    /// <see cref="BgDataTypesJsonContext"/> ("The serializer constructors").
    /// </summary>
    [JsonConstructor]
    internal CubeDecision(DecisionKind kind) : base(DecisionKind.Cube, kind)
    {
    }

    /// <summary>
    /// The cube analysis, the user's cube errors and played actions, the
    /// analysis's facts about each cube action, and the truth among the four
    /// cube answers — see <see cref="CubeDecisionData"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown on init when the value is <see langword="null"/>.</exception>
    public required CubeDecisionData Decision
    {
        get => _decision!;
        init
        {
            try
            {
                ArgumentNullException.ThrowIfNull(value, nameof(Decision));
            }
            catch (ArgumentException fault) when (IsRead)
            {
                throw DocumentRefusal.Of(fault);
            }
            _decision = value;
        }
    }

    /// <summary>
    /// Whether gammons are possible at this decision: whether a gammon the
    /// player on roll wins can count (SPEC-scoring §3, amended 2026-10-01 on
    /// halheinrich/backgammon#326). <see langword="false"/> exactly when any
    /// of three holds:
    /// <list type="bullet">
    /// <item><description>the opponent has a checker borne off
    /// (<see cref="BoardPosition.OpponentBorneOffCount"/>), so no gammon can
    /// be won;</description></item>
    /// <item><description>in a match, the cube is at least the points the
    /// player on roll needs (<see cref="PositionData.CubeSize"/> at least
    /// <see cref="MatchSession.OnRollNeeds"/>), so a single game wins the
    /// match;</description></item>
    /// <item><description>in money under the Jacoby rule, the cube is centred
    /// (<see cref="MoneyTerms.IsJacoby"/>,
    /// <see cref="CubeOwner.Centered"/>), so a gammon counts as a single game
    /// until the cube turns.</description></item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Hal: "If the opponent has checkers borne off, gammons are not possible.
    /// Also, gammons are only possible when the size of the cube is less than
    /// the points we need win the match." The one derivation of the fact,
    /// from the position, the session, the score and the cube, all of which
    /// only the record sees together; no consumer re-derives it. The
    /// analyser's gammon chances are not part of it: a position with no
    /// gammon chance whose gates are open still has gammons possible.
    /// </para>
    /// <para>
    /// It decides what the fourth answer reads as (<see cref="ClaimOf"/>) and
    /// whether SPEC-scoring §3's two conventions apply to the costs
    /// (<see cref="CostOf"/>); it decides nothing about the truth, which is
    /// the equities' (<see cref="CubeDecisionData.BestAnswer"/>). It replaces
    /// the Too good offerability fact: all four answers are always offered.
    /// </para>
    /// </remarks>
    [JsonIgnore]
    public bool GammonsPossible =>
        Position.Mop.OpponentBorneOffCount == 0
        && Session.Match(
            money: money => !(money.Terms.IsJacoby && Position.CubeOwner == CubeOwner.Centered),
            match: match => Position.CubeSize < match.OnRollNeeds);

    /// <summary>
    /// What <paramref name="answer"/> reads as at this decision: the
    /// three-way claim of SPEC-scoring §1, now a reading of the answer
    /// (halheinrich/backgammon#326). <see cref="CubeAnswer.NoDouble"/> reads
    /// <see cref="CubeClaim.NoDouble"/>; <see cref="CubeAnswer.DoubleTake"/>
    /// and <see cref="CubeAnswer.DoublePass"/> read
    /// <see cref="CubeClaim.Double"/>; the fourth answer,
    /// <see cref="CubeAnswer.NoDoublePass"/>, reads
    /// <see cref="CubeClaim.TooGood"/> where gammons are possible
    /// (<see cref="GammonsPossible"/>) and <see cref="CubeClaim.NoDouble"/>
    /// where they are not.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For the fourth answer the reading is which of its two labels applies:
    /// Too good where it reads <see cref="CubeClaim.TooGood"/>, and No double
    /// / Pass where it reads <see cref="CubeClaim.NoDouble"/>. That is the
    /// library's choice; the labels' wording, full and short, is the label
    /// home's, in BackgammonDiagram_Lib, which renders the reading and
    /// re-checks no rule.
    /// </para>
    /// <para>
    /// The one derivation of a claim. The truth's claim is the reading of the
    /// truth, <c>ClaimOf(Decision.BestAnswer)</c>: at the tie of
    /// halheinrich/backgammon#293 it reads Too good where gammons are
    /// possible, though playing on is worth only the cash.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="answer"/> is not one of the four answers.
    /// </exception>
    public CubeClaim ClaimOf(CubeAnswer answer) => answer switch
    {
        CubeAnswer.NoDouble => CubeClaim.NoDouble,
        CubeAnswer.DoubleTake or CubeAnswer.DoublePass => CubeClaim.Double,
        CubeAnswer.NoDoublePass => GammonsPossible ? CubeClaim.TooGood : CubeClaim.NoDouble,
        _ => throw new ArgumentOutOfRangeException(nameof(answer), answer,
            "A cube answer is one of the four CubeAnswer members."),
    };

    /// <summary>
    /// What <paramref name="answer"/> costs at this decision, in its two parts
    /// (SPEC-scoring §3, amended 2026-09-30 and 2026-10-01 on
    /// halheinrich/backgammon#326, whose tables it implements exactly): an
    /// answer costs what it loses in equity, except where gammons are
    /// possible (<see cref="GammonsPossible"/>) and a misreading loses none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// N is the no-double equity and T the double/take equity, each the
    /// doubler's, with the cash at +1; "they'd take" is the taker's best
    /// response (<see cref="CubeDecisionData.BestTakerAction"/>). The
    /// <see cref="CubeAnswerCost.DoublingPart"/>:
    /// </para>
    /// <list type="bullet">
    /// <item><description>No double: what not doubling loses,
    /// max(0, min(T, 1) − N); but where gammons are possible and Too good is
    /// right (T ≥ 1, N &gt; 1), N − 0.6, Hal's convention for a misreading
    /// with "no action loss, but … a serious evaluation error".</description></item>
    /// <item><description>Double / Take and Double / Pass: the doubling
    /// action's loss (<see cref="CubeDecisionData.DoublerActionError"/> of
    /// <see cref="CubeAction.Double"/>).</description></item>
    /// <item><description>The fourth answer: where it reads Too good and
    /// they'd take, 1 − T (Hal: "Too good means you think it's worth more than
    /// 1.0; that's at least (1.0 - DT equity)"); otherwise what not doubling
    /// loses.</description></item>
    /// </list>
    /// <para>
    /// The <see cref="CubeAnswerCost.TakePart"/>: the answer's full take/pass
    /// error (<see cref="CubeDecisionData.TakerActionError"/>) when it commits
    /// to its response (<see cref="CubeAnswerExtensions.CommitsToResponse"/>),
    /// and 0 for No double, whose implied take is never charged. So Too good
    /// when they'd take costs 2(1 − T) in all, the least error it must
    /// contain.
    /// </para>
    /// <para>
    /// Exact: computed from the stored equities as they are, never rounded and
    /// never corrected. Whether a cost or a part counts as zero, so is
    /// correct, is <see cref="EquityLoss.CountsAsZero"/>'s to judge; at an
    /// equity tie more than one answer costs nothing. The cost is not the
    /// analysis's action error: see <see cref="CubeDecisionData.ActionEquity"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="answer"/> is not one of the four answers.
    /// </exception>
    public CubeAnswerCost CostOf(CubeAnswer answer) => Decision.CostOf(answer, GammonsPossible);

    /// <inheritdoc/>
    public override TResult Match<TResult>(
        Func<CheckerPlayDecision, TResult> checkerPlay, Func<CubeDecision, TResult> cube)
    {
        ArgumentNullException.ThrowIfNull(checkerPlay);
        ArgumentNullException.ThrowIfNull(cube);
        return cube(this);
    }

    /// <inheritdoc/>
    public override void Switch(Action<CheckerPlayDecision> checkerPlay, Action<CubeDecision> cube)
    {
        ArgumentNullException.ThrowIfNull(checkerPlay);
        ArgumentNullException.ThrowIfNull(cube);
        cube(this);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Nothing of a cube decision's own depends on the position: the Crawford
    /// rule is the base's, which reads the kind.
    /// </remarks>
    private protected override void PositionStated(PositionData position)
    {
    }
}
