using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// A match, as it stands at the decision: its length, what each player still
/// needs, and whether this is the Crawford game. One of the two kinds of
/// <see cref="Session"/>; it carries a match's facts and nothing else — no
/// Jacoby rule, which does not apply in a match.
/// </summary>
/// <remarks>
/// <para>
/// <b>Well-formed by construction.</b> The length is at least 1; each away
/// score is at least 1 and at most the length; and in the Crawford game exactly
/// one player is 1-away — the rules stated once on the internal
/// <see cref="SessionRules"/>. Each init setter checks what it can against the
/// members already set, so whichever member completes a contradiction refuses
/// it — in an object initializer in any order and in a JSON document in any
/// property order alike. Code breaking a rule gets the guard's
/// <see cref="ArgumentException"/>; a document breaking it gets a
/// <see cref="System.Text.Json.JsonException"/> carrying it, however the
/// session is read (the wire rule on <see cref="BgDataTypesJsonContext"/>).
/// </para>
/// <para>
/// A document stating a member this kind does not have — a money session's —
/// is refused, never read with the member dropped.
/// </para>
/// </remarks>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class MatchSession : Session
{
    // Null only while construction is still stating the member; `required`
    // guarantees each is set by the time it ends. The guards read an unset
    // member as "nothing to agree with yet".
    private readonly int? _length;
    private readonly int? _onRollNeeds;
    private readonly int? _opponentNeeds;
    private readonly bool? _isCrawford;

    /// <summary>Creates a match; its members are set by the initializer.</summary>
    public MatchSession() : base(SessionKind.Match)
    {
    }

    /// <summary>
    /// The serializer's constructor. It binds the document's
    /// <paramref name="kind"/>, which must be <see cref="SessionKind.Match"/>;
    /// why the pattern exists is stated once, on
    /// <see cref="BgDataTypesJsonContext"/> ("The serializer constructors").
    /// </summary>
    [JsonConstructor]
    internal MatchSession(SessionKind kind) : base(SessionKind.Match, kind)
    {
    }

    /// <summary>The match's length: the points a player needs to win it, at least 1.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown on init when the value is below 1, or when an away score already
    /// set is more than it.
    /// </exception>
    public required int Length
    {
        get => _length.GetValueOrDefault();
        init
        {
            Guard(() =>
            {
                if (!SessionRules.LengthHolds(value))
                    throw new ArgumentOutOfRangeException(nameof(Length), value, SessionRules.LengthMessage);
                if (_onRollNeeds is int onRoll && !SessionRules.NeedsHolds(onRoll, value))
                    throw new ArgumentOutOfRangeException(nameof(Length), value, SessionRules.NeedsMessage);
                if (_opponentNeeds is int opponent && !SessionRules.NeedsHolds(opponent, value))
                    throw new ArgumentOutOfRangeException(nameof(Length), value, SessionRules.NeedsMessage);
            });
            _length = value;
        }
    }

    /// <summary>
    /// The points the player on roll still needs to win the match — their
    /// away score (3 means "3-away"): at least 1, at most <see cref="Length"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on init when the value is below 1 or above the length.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown on init when this is the Crawford game and the value leaves not
    /// exactly one player 1-away.
    /// </exception>
    public required int OnRollNeeds
    {
        get => _onRollNeeds.GetValueOrDefault();
        init
        {
            Guard(() => CheckNeeds(value, _opponentNeeds, nameof(OnRollNeeds)));
            _onRollNeeds = value;
        }
    }

    /// <summary>
    /// The points the opponent still needs to win the match: at least 1, at
    /// most <see cref="Length"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on init when the value is below 1 or above the length.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown on init when this is the Crawford game and the value leaves not
    /// exactly one player 1-away.
    /// </exception>
    public required int OpponentNeeds
    {
        get => _opponentNeeds.GetValueOrDefault();
        init
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
    public required bool IsCrawford
    {
        get => _isCrawford.GetValueOrDefault();
        init
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
        && match.Length == Length
        && match.OnRollNeeds == OnRollNeeds
        && match.OpponentNeeds == OpponentNeeds
        && match.IsCrawford == IsCrawford;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Kind, Length, OnRollNeeds, OpponentNeeds, IsCrawford);

    /// <summary>
    /// The match for a reader — <c>"match to 7, 3-away/5-away"</c>, with
    /// <c>", Crawford"</c> in the Crawford game — for a test failure or a
    /// log; not a wire form.
    /// </summary>
    public override string ToString() => string.Create(
        System.Globalization.CultureInfo.InvariantCulture,
        $"match to {Length}, {OnRollNeeds}-away/{OpponentNeeds}-away{(IsCrawford ? ", Crawford" : "")}");

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
    /// Holds an away score to the length, when set, and to the Crawford
    /// standing, when the other away score and the Crawford flag are set.
    /// </summary>
    private void CheckNeeds(int needs, int? otherNeeds, string member)
    {
        if (!SessionRules.NeedsHolds(needs, _length))
            throw new ArgumentOutOfRangeException(member, needs, SessionRules.NeedsMessage);
        if (_isCrawford == true && otherNeeds is int other && !SessionRules.CrawfordStandingHolds(needs, other))
            throw new ArgumentException(SessionRules.CrawfordStandingMessage, member);
    }
}
