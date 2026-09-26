using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// A money session, as it stands at the decision: the rules its games are
/// played under — the Jacoby and beaver rules and the cube limit — and the
/// session's score before this game. One of the two kinds of
/// <see cref="Session"/>; it carries a money session's facts and nothing else
/// — no length, no away scores and no Crawford game, which belong to a match.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every fact is stated.</b> Each member is required, so a money session
/// under an unknown rule, or with no limit or score, cannot be built: the
/// producer states what the source file records (XG's match header and game
/// header). The beaver rule, the cube limit and the scores are the facts that
/// only the stored XGID used to carry; they are typed members now, and the
/// record derives its XGID from them (<see cref="BgDecisionData.Xgid"/>).
/// </para>
/// <para>
/// <b>Well-formed by construction.</b> The cube limit is a positive power of
/// two, and each score is at least 0; each init setter refuses a breach with
/// an <see cref="ArgumentOutOfRangeException"/> naming the member, and a
/// document breaking one gets a <see cref="System.Text.Json.JsonException"/>
/// carrying it. A document stating a member this kind does not have — a
/// match's — is refused, never read with the member dropped.
/// </para>
/// </remarks>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class MoneySession : Session
{
    private readonly int? _cubeLimit;
    private readonly int? _onRollScore;
    private readonly int? _opponentScore;

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

    /// <summary>
    /// Whether the beaver rule is in force: a player doubled may redouble at
    /// once while keeping the cube (XG's match header; the XGID's field 8 spells
    /// it, bit 2, for money only).
    /// </summary>
    public required bool IsBeaver { get; init; }

    /// <summary>
    /// The highest value the cube may reach in this session — a positive power
    /// of two (XG's default is 1024, <c>2^10</c>), so a position's
    /// <see cref="PositionData.CubeSize"/> never exceeds it. A money session's
    /// rule: a match has no cube limit, the match length bounding what the
    /// cube can win.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on init when the value is not a positive power of two.</exception>
    public required int CubeLimit
    {
        get => _cubeLimit.GetValueOrDefault();
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

    /// <summary>
    /// The points the player on roll had won in the session before this game —
    /// the game header's score, as XG records it; at least 0. A money
    /// session's standing, the counterpart of a match's away scores.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on init when the value is negative.</exception>
    public required int OnRollScore
    {
        get => _onRollScore.GetValueOrDefault();
        init
        {
            Guard(() => CheckScore(value, nameof(OnRollScore)));
            _onRollScore = value;
        }
    }

    /// <summary>The points the opponent had won in the session before this game; at least 0.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on init when the value is negative.</exception>
    public required int OpponentScore
    {
        get => _opponentScore.GetValueOrDefault();
        init
        {
            Guard(() => CheckScore(value, nameof(OpponentScore)));
            _opponentScore = value;
        }
    }

    /// <inheritdoc/>
    public override bool Equals(Session? other) =>
        other is MoneySession money
        && money.IsJacoby == IsJacoby
        && money.IsBeaver == IsBeaver
        && money.CubeLimit == CubeLimit
        && money.OnRollScore == OnRollScore
        && money.OpponentScore == OpponentScore;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Kind, IsJacoby, IsBeaver, CubeLimit, OnRollScore, OpponentScore);

    /// <summary>
    /// The session for a reader — <c>"money, Jacoby, no beaver, cube limit
    /// 1024, 3-0"</c> — for a test failure or a log; not a wire form.
    /// </summary>
    public override string ToString() => string.Create(
        System.Globalization.CultureInfo.InvariantCulture,
        $"money, {(IsJacoby ? "Jacoby" : "no Jacoby")}, {(IsBeaver ? "beaver" : "no beaver")}, cube limit {CubeLimit}, {OnRollScore}-{OpponentScore}");

    private static void CheckScore(int score, string member)
    {
        if (!SessionRules.ScoreHolds(score))
            throw new ArgumentOutOfRangeException(member, score, SessionRules.ScoreMessage);
    }

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
