using System.Globalization;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// A match, as it stands at the decision: its terms — its length
/// (<see cref="MatchTerms"/>) — what each player still needs, the player on
/// roll's first, and whether this is the Crawford game. One of the two kinds
/// of <see cref="Session"/>; it carries a match's facts and nothing else — no
/// Jacoby rule, which does not apply in a match, and no cube limit, which a
/// match's length makes needless.
/// </summary>
/// <remarks>
/// <para>
/// <b>Composed, not restated</b> (the umbrella's review of
/// halheinrich/backgammon#273, 2026-09-26): the session holds its header's
/// terms as a member and states only the oriented standing itself. It is
/// built by <see cref="Session.Create"/> from the terms, the game's
/// <see cref="MatchStanding"/> and the seat on roll; code outside this
/// library cannot build one otherwise.
/// </para>
/// <para>
/// <b>Well-formed by construction.</b> The terms hold their own rule (a length
/// of at least 1); each away score is at least 1 and at most the length; and
/// in the Crawford game exactly one player is 1-away — the rules stated once
/// on the internal <see cref="SessionRules"/>. Each init setter checks what it
/// can against the members already set, so whichever member completes a
/// contradiction refuses it, in a JSON document in any property order. A
/// document breaking a rule gets a <see cref="System.Text.Json.JsonException"/>
/// carrying the guard's <see cref="ArgumentException"/>, however the session
/// is read (the wire rule on <see cref="BgDataTypesJsonContext"/>), and one
/// stating a member this kind does not have — a money session's — is refused,
/// never read with the member dropped.
/// </para>
/// </remarks>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class MatchSession : Session
{
    // Null only while construction is still stating the member. The guards
    // read an unset member as "nothing to agree with yet".
    private readonly MatchTerms? _terms;
    private readonly int? _onRollNeeds;
    private readonly int? _opponentNeeds;
    private readonly bool? _isCrawford;

    /// <summary>The constructor <see cref="Session.Create"/> builds a match through; its members are set by the initializer.</summary>
    internal MatchSession() : base(SessionKind.Match, read: false)
    {
    }

    /// <summary>
    /// The serializer's constructor. It binds the document's
    /// <paramref name="terms"/>, the session's first member; why the pattern
    /// exists is stated once, on <see cref="BgDataTypesJsonContext"/> ("The
    /// serializer constructors").
    /// </summary>
    [JsonConstructor]
    internal MatchSession(MatchTerms terms) : base(SessionKind.Match, read: true) => Terms = terms;

    /// <summary>The match's terms: its length, as its header states it.</summary>
    /// <exception cref="ArgumentNullException">Thrown on init when the value is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on init when an away score already set is more than the length.</exception>
    [JsonInclude, JsonRequired, JsonPropertyOrder(-1)]
    public MatchTerms Terms
    {
        get => _terms!;
        internal init
        {
            Guard(() =>
            {
                ArgumentNullException.ThrowIfNull(value, nameof(Terms));
                if (_onRollNeeds is int onRoll && !SessionRules.NeedsHolds(onRoll, value.Length))
                    throw new ArgumentOutOfRangeException(nameof(Terms), value.Length, SessionRules.NeedsMessage);
                if (_opponentNeeds is int opponent && !SessionRules.NeedsHolds(opponent, value.Length))
                    throw new ArgumentOutOfRangeException(nameof(Terms), value.Length, SessionRules.NeedsMessage);
            });
            _terms = value;
        }
    }

    /// <summary>
    /// The points the player on roll still needs to win the match — their
    /// away score (3 means "3-away"): at least 1, at most the terms' length.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on init when the value is below 1 or above the length.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown on init when this is the Crawford game and the value leaves not
    /// exactly one player 1-away.
    /// </exception>
    [JsonInclude, JsonRequired]
    public int OnRollNeeds
    {
        get => _onRollNeeds.GetValueOrDefault();
        internal init
        {
            Guard(() => CheckNeeds(value, _opponentNeeds, nameof(OnRollNeeds)));
            _onRollNeeds = value;
        }
    }

    /// <summary>
    /// The points the opponent still needs to win the match: at least 1, at
    /// most the terms' length.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on init when the value is below 1 or above the length.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown on init when this is the Crawford game and the value leaves not
    /// exactly one player 1-away.
    /// </exception>
    [JsonInclude, JsonRequired]
    public int OpponentNeeds
    {
        get => _opponentNeeds.GetValueOrDefault();
        internal init
        {
            Guard(() => CheckNeeds(value, _onRollNeeds, nameof(OpponentNeeds)));
            _opponentNeeds = value;
        }
    }

    /// <summary>
    /// True when this is the Crawford game: the one game, immediately after a
    /// player first reaches match point, in which doubling is barred. Exactly
    /// one player is 1-away in it.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown on init when the value is <see langword="true"/> and both away
    /// scores are set with not exactly one of them 1.
    /// </exception>
    [JsonInclude, JsonRequired]
    public bool IsCrawford
    {
        get => _isCrawford.GetValueOrDefault();
        internal init
        {
            Guard(() =>
            {
                if (value && _onRollNeeds is int onRoll && _opponentNeeds is int opponent
                    && !SessionRules.CrawfordStandingHolds(onRoll, opponent))
                    throw new ArgumentException(SessionRules.CrawfordStandingMessage, nameof(IsCrawford));
            });
            _isCrawford = value;
        }
    }

    /// <inheritdoc/>
    public override bool Equals(Session? other) =>
        other is MatchSession match
        && match.Terms == Terms
        && match.OnRollNeeds == OnRollNeeds
        && match.OpponentNeeds == OpponentNeeds
        && match.IsCrawford == IsCrawford;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Terms, OnRollNeeds, OpponentNeeds, IsCrawford);

    /// <summary>
    /// The match for a reader — <c>"match to 7, 3-away/5-away"</c>, with
    /// <c>", Crawford"</c> in the Crawford game — for a test failure or a
    /// log; not a wire form.
    /// </summary>
    public override string ToString() => string.Create(
        CultureInfo.InvariantCulture,
        $"{Terms}, {OnRollNeeds}-away/{OpponentNeeds}-away{(IsCrawford ? ", Crawford" : "")}");

    /// <inheritdoc/>
    public override TResult Match<TResult>(Func<MoneySession, TResult> money, Func<MatchSession, TResult> match)
    {
        ArgumentNullException.ThrowIfNull(money);
        ArgumentNullException.ThrowIfNull(match);
        return match(this);
    }

    /// <inheritdoc/>
    public override void Switch(Action<MoneySession> money, Action<MatchSession> match)
    {
        ArgumentNullException.ThrowIfNull(money);
        ArgumentNullException.ThrowIfNull(match);
        match(this);
    }

    /// <summary>
    /// Holds an away score to the length, when the terms are set, and to the
    /// Crawford standing, when the other away score and the Crawford flag are
    /// set.
    /// </summary>
    private void CheckNeeds(int needs, int? otherNeeds, string member)
    {
        if (!SessionRules.NeedsHolds(needs, _terms?.Length))
            throw new ArgumentOutOfRangeException(member, needs, SessionRules.NeedsMessage);
        if (_isCrawford == true && otherNeeds is int other && !SessionRules.CrawfordStandingHolds(needs, other))
            throw new ArgumentException(SessionRules.CrawfordStandingMessage, member);
    }
}
