using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// A cube decision: whether the player on roll doubles, and whether the
/// opponent takes. One of the two kinds of <see cref="BgDecisionData"/>; it
/// carries a cube decision's fields and nothing else — see
/// <see cref="CubeDecisionData"/> for its <see cref="Decision"/> category,
/// which also holds the cube-scoring policy. A cube decision is never made in
/// the Crawford game: its <see cref="BgDecisionData.Position"/> refuses a
/// Crawford position (<see cref="DecisionRules.CrawfordMessage"/>,
/// halheinrich/backgammon#201).
/// </summary>
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
    /// The cube analysis, the user's cube errors and played actions, and the
    /// scoring policy — see <see cref="CubeDecisionData"/>.
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
    /// Whether the Too Good verdict can occur at this position — the
    /// offerability fact of SPEC-scoring §3's 2026-09-02 amendment
    /// (halheinrich/backgammon#187): <see langword="false"/> exactly when the
    /// session is money (<see cref="BgDecisionData.IsMoneyGame"/>), the
    /// Jacoby rule is known to be in force (<see cref="BgDecisionData.IsJacoby"/>
    /// is <see langword="true"/>) and the cube is centred
    /// (<see cref="PositionData.CubeOwner"/> is
    /// <see cref="CubeOwner.Centered"/>); <see langword="true"/> otherwise.
    /// Gammons do not count under Jacoby until the cube turns, so the
    /// no-double equity never exceeds the cash there and the verdict cannot
    /// arise; a turned cube re-arms gammons, and Too Good returns.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The one derivation site of this fact in the ecosystem: a consumer
    /// that offers cube answers reads it to decide whether the Too Good pair
    /// is in the option set, and never re-derives it from the record's
    /// rules fields (the same encapsulation rule as
    /// <see cref="CubeDecisionData.BestDoublerClaim"/>). It lives on the
    /// record, not on <see cref="CubeDecisionData"/>, because only the record
    /// sees money, Jacoby and cube owner together; and only on a cube
    /// decision, the one kind the question has a meaning for. Money is
    /// reached through the record's <see cref="BgDecisionData.IsMoneyGame"/>,
    /// never a <c>MatchLength == 0</c> restated here.
    /// </para>
    /// <para>
    /// An unknown rule is not a known Jacoby rule:
    /// <see cref="BgDecisionData.IsJacoby"/> <see langword="null"/> (a money
    /// record whose rule was never stamped, or any match record) leaves this
    /// <see langword="true"/> — the verdict is withheld only when the
    /// position's own facts rule it out, the same posture
    /// <see cref="IDecisionFilterData.IsJacoby"/> states for the filter layer.
    /// This is a fact about the position, independent of what
    /// <see cref="CubeDecisionData.BestClaimPair"/> derives: the derivation
    /// reads equities only and would still name Too Good if the producer's
    /// numbers said so.
    /// </para>
    /// </remarks>
    [JsonIgnore]
    public bool CanBeTooGood =>
        !(IsMoneyGame
          && IsJacoby == true
          && Position.CubeOwner == CubeOwner.Centered);

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
