using System.Globalization;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// A money session, as it stands at the decision: its terms — the Jacoby and
/// beaver rules and the cube limit (<see cref="MoneyTerms"/>) — and the
/// session's score before this game, the player on roll's first. One of the
/// two kinds of <see cref="Session"/>; it carries a money session's facts and
/// nothing else — no length, no away scores and no Crawford game, which belong
/// to a match.
/// </summary>
/// <remarks>
/// <para>
/// <b>Composed, not restated</b> (the umbrella's review of
/// halheinrich/backgammon#273, 2026-09-26): the session holds its header's
/// terms as a member and states only the oriented standing itself, the
/// scores. It is built by <see cref="Session.Create"/> from the terms, the
/// game's <see cref="MoneyStanding"/> and the seat on roll; code outside this
/// library cannot build one otherwise. The beaver rule, the cube limit and
/// the scores are the facts that only the stored XGID used to carry; the
/// record derives its XGID from them (<see cref="BgDecisionData.Xgid"/>).
/// </para>
/// <para>
/// <b>Well-formed by construction.</b> The terms hold their own rules; each
/// score is at least 0. A document breaking a rule gets a
/// <see cref="System.Text.Json.JsonException"/> carrying the guard's
/// <see cref="ArgumentException"/>, and one stating a member this kind does not
/// have — a match's — is refused, never read with the member dropped.
/// </para>
/// </remarks>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class MoneySession : Session
{
    // Null only while construction is still stating the member.
    private readonly MoneyTerms? _terms;
    private readonly int? _onRollScore;
    private readonly int? _opponentScore;

    /// <summary>The constructor <see cref="Session.Create"/> builds a money session through; its members are set by the initializer.</summary>
    internal MoneySession() : base(SessionKind.Money, read: false)
    {
    }

    /// <summary>
    /// The serializer's constructor. It binds the document's
    /// <paramref name="terms"/>, the session's first member; why the pattern
    /// exists is stated once, on <see cref="BgDataTypesJsonContext"/> ("The
    /// serializer constructors").
    /// </summary>
    [JsonConstructor]
    internal MoneySession(MoneyTerms terms) : base(SessionKind.Money, read: true) => Terms = terms;

    /// <summary>
    /// The session's terms: the Jacoby and beaver rules and the cube limit,
    /// as its header states them. Every money session states them: there is
    /// no unknown rule.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown on init when the value is <see langword="null"/>.</exception>
    [JsonInclude, JsonRequired, JsonPropertyOrder(-1)]
    public MoneyTerms Terms
    {
        get => _terms!;
        internal init
        {
            Guard(() => ArgumentNullException.ThrowIfNull(value, nameof(Terms)));
            _terms = value;
        }
    }

    /// <summary>
    /// The points the player on roll had won in the session before this game —
    /// the game header's score, as XG records it; at least 0. A money
    /// session's standing, the counterpart of a match's away scores.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on init when the value is negative.</exception>
    [JsonInclude, JsonRequired]
    public int OnRollScore
    {
        get => _onRollScore.GetValueOrDefault();
        internal init
        {
            Guard(() => CheckScore(value, nameof(OnRollScore)));
            _onRollScore = value;
        }
    }

    /// <summary>The points the opponent had won in the session before this game; at least 0.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on init when the value is negative.</exception>
    [JsonInclude, JsonRequired]
    public int OpponentScore
    {
        get => _opponentScore.GetValueOrDefault();
        internal init
        {
            Guard(() => CheckScore(value, nameof(OpponentScore)));
            _opponentScore = value;
        }
    }

    /// <inheritdoc/>
    public override bool Equals(Session? other) =>
        other is MoneySession money
        && money.Terms == Terms
        && money.OnRollScore == OnRollScore
        && money.OpponentScore == OpponentScore;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Terms, OnRollScore, OpponentScore);

    /// <summary>
    /// The session for a reader — <c>"money, Jacoby, no beaver, cube limit
    /// 1024, 3-0"</c> — for a test failure or a log; not a wire form.
    /// </summary>
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Terms}, {OnRollScore}-{OpponentScore}");

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
