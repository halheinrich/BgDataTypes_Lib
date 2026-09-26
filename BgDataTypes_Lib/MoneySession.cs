using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// A money session, as it stands at the decision: the rules its games are
/// played under. One of the two kinds of <see cref="Session"/>; it carries a
/// money session's facts and nothing else — no length, no away scores and no
/// Crawford game, which belong to a match.
/// </summary>
/// <remarks>
/// A document stating a member this kind does not have — a match's — is
/// refused, never read with the member dropped.
/// </remarks>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class MoneySession : Session
{
    /// <summary>Creates a money session; its members are set by the initializer.</summary>
    public MoneySession() : base(SessionKind.Money)
    {
    }

    /// <summary>
    /// The serializer's constructor. It binds the document's
    /// <paramref name="kind"/>, which must be <see cref="SessionKind.Money"/>;
    /// why the pattern exists is stated once, on
    /// <see cref="BgDataTypesJsonContext"/> ("The serializer constructors").
    /// </summary>
    [JsonConstructor]
    internal MoneySession(SessionKind kind) : base(SessionKind.Money, kind)
    {
    }

    /// <summary>
    /// Whether the Jacoby rule is in force: gammons and backgammons count as a
    /// single point until the cube has been turned. With a centred cube it
    /// voids undoubled gammons outright and shifts the doubling window, so it
    /// can change the correct answer: it takes part in a money decision's
    /// <see cref="ProblemKey"/> (SPEC-stats-identity.md §1, amended
    /// 2026-08-20; halheinrich/backgammon#120) and decides whether the Too
    /// Good verdict can occur (<see cref="CubeDecision.CanBeTooGood"/>). Every
    /// money session states it: there is no unknown rule.
    /// </summary>
    public required bool IsJacoby { get; init; }

    /// <inheritdoc/>
    public override bool Equals(Session? other) =>
        other is MoneySession money
        && money.IsJacoby == IsJacoby;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Kind, IsJacoby);

    /// <summary>
    /// The session for a reader — <c>"money, Jacoby"</c> or <c>"money, no
    /// Jacoby"</c> — for a test failure or a log; not a wire form.
    /// </summary>
    public override string ToString() => IsJacoby ? "money, Jacoby" : "money, no Jacoby";

    /// <inheritdoc/>
    public override TResult Match<TResult>(Func<MoneySession, TResult> money, Func<MatchSession, TResult> match)
    {
        ArgumentNullException.ThrowIfNull(money);
        ArgumentNullException.ThrowIfNull(match);
        return money(this);
    }

    /// <inheritdoc/>
    public override void Switch(Action<MoneySession> money, Action<MatchSession> match)
    {
        ArgumentNullException.ThrowIfNull(money);
        ArgumentNullException.ThrowIfNull(match);
        money(this);
    }
}
