using System.Globalization;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// A money session's terms, as its header states them: the Jacoby and beaver
/// rules and the cube limit. One of the two kinds of <see cref="SessionTerms"/>;
/// no length, which belongs to a match. A record's <see cref="MoneySession"/>
/// holds them as its <see cref="MoneySession.Terms"/>.
/// </summary>
/// <remarks>
/// <b>Well-formed by construction.</b> Every member is required, and the cube
/// limit is a positive power of two; its init setter refuses a breach with an
/// <see cref="ArgumentOutOfRangeException"/> naming the member, and a document
/// breaking it gets a <see cref="System.Text.Json.JsonException"/> carrying
/// it. A document stating a member these terms do not have — a match's — is
/// refused, never read with the member dropped.
/// </remarks>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class MoneyTerms : SessionTerms
{
    private readonly int _cubeLimit;

    /// <summary>Creates money terms; the members are set by the initializer.</summary>
    public MoneyTerms() : base(SessionKind.Money)
    {
    }

    /// <summary>
    /// The serializer's constructor. It binds the document's
    /// <paramref name="kind"/>, which must be <see cref="SessionKind.Money"/>;
    /// why the pattern exists is stated once, on
    /// <see cref="BgDataTypesJsonContext"/> ("The serializer constructors").
    /// </summary>
    [JsonConstructor]
    internal MoneyTerms(SessionKind kind) : base(SessionKind.Money, kind)
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

    /// <summary>
    /// Whether the beaver rule is in force: a player doubled may redouble at
    /// once while keeping the cube (XG's match header; the XGID's field 8
    /// spells it, bit 2, for money only).
    /// </summary>
    public required bool IsBeaver { get; init; }

    /// <summary>
    /// The highest value the cube may reach in this session — a positive power
    /// of two (XG's default is 1024, <c>2^10</c>), so a position's
    /// <see cref="PositionData.CubeSize"/> never exceeds it. A money session's
    /// rule: a match has no cube limit, the match length bounding what the
    /// cube can win (<see cref="MatchTerms"/>).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on init when the value is not a positive power of two.</exception>
    public required int CubeLimit
    {
        get => _cubeLimit;
        init
        {
            Guard(() =>
            {
                if (!SessionRules.CubeValueHolds(value))
                    throw new ArgumentOutOfRangeException(nameof(CubeLimit), value, SessionRules.CubeLimitMessage);
            });
            _cubeLimit = value;
        }
    }

    /// <inheritdoc/>
    public override bool Equals(SessionTerms? other) =>
        other is MoneyTerms money
        && money.IsJacoby == IsJacoby
        && money.IsBeaver == IsBeaver
        && money.CubeLimit == CubeLimit;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Kind, IsJacoby, IsBeaver, CubeLimit);

    /// <summary>
    /// The terms for a reader — <c>"money, Jacoby, no beaver, cube limit
    /// 1024"</c> — for a test failure or a log; not a wire form.
    /// </summary>
    public override string ToString() => string.Create(
        CultureInfo.InvariantCulture,
        $"money, {(IsJacoby ? "Jacoby" : "no Jacoby")}, {(IsBeaver ? "beaver" : "no beaver")}, cube limit {CubeLimit}");

    /// <inheritdoc/>
    public override TResult Match<TResult>(Func<MoneyTerms, TResult> money, Func<MatchTerms, TResult> match)
    {
        ArgumentNullException.ThrowIfNull(money);
        ArgumentNullException.ThrowIfNull(match);
        return money(this);
    }

    /// <inheritdoc/>
    public override void Switch(Action<MoneyTerms> money, Action<MatchTerms> match)
    {
        ArgumentNullException.ThrowIfNull(money);
        ArgumentNullException.ThrowIfNull(match);
        money(this);
    }
}
