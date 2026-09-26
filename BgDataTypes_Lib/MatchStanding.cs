using System.Globalization;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// Where the players of a match stand as a game begins: what each still
/// needs to win it, and whether this is the Crawford game. One of the two
/// kinds of <see cref="GameStanding"/>; no money score, which a match has not.
/// Seen from the player on roll, the same facts are a record's
/// <see cref="MatchSession"/>.
/// </summary>
/// <remarks>
/// <b>Well-formed by construction</b>, by a match's rules (the internal
/// <see cref="SessionRules"/>): each away score is at least 1 — a player
/// 0-away has won, and 0-away each was money's stand-in — and in the Crawford
/// game exactly one player is 1-away. The length is the match's terms'
/// (<see cref="MatchTerms"/>), not the game's, so no upper bound is checked
/// here. Each guard is order-independent, naming the member that completed
/// the contradiction; a document breaking one gets a
/// <see cref="System.Text.Json.JsonException"/> carrying it, whatever order
/// it states the members in. A document stating a member this standing does
/// not have — a money score — is refused, never read with the member dropped.
/// </remarks>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class MatchStanding : GameStanding
{
    // Null only while construction is still stating the member.
    private readonly int? _away1;
    private readonly int? _away2;
    private readonly bool? _isCrawford;

    /// <summary>Creates a match standing; the members are set by the initializer.</summary>
    public MatchStanding() : base(SessionKind.Match)
    {
    }

    /// <summary>
    /// The serializer's constructor. It binds the document's
    /// <paramref name="kind"/>, which must be <see cref="SessionKind.Match"/>;
    /// why the pattern exists is stated once, on
    /// <see cref="BgDataTypesJsonContext"/> ("The serializer constructors").
    /// </summary>
    [JsonConstructor]
    internal MatchStanding(SessionKind kind) : base(SessionKind.Match, kind)
    {
    }

    /// <summary>The points player 1 still needs to win the match; at least 1.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on init when the value is below 1.</exception>
    /// <exception cref="ArgumentException">Thrown on init when this is the Crawford game and the value leaves not exactly one player 1-away.</exception>
    public required int Away1
    {
        get => _away1.GetValueOrDefault();
        init
        {
            Guard(() => CheckAway(value, _away2, nameof(Away1)));
            _away1 = value;
        }
    }

    /// <summary>The points player 2 still needs to win the match; at least 1.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on init when the value is below 1.</exception>
    /// <exception cref="ArgumentException">Thrown on init when this is the Crawford game and the value leaves not exactly one player 1-away.</exception>
    public required int Away2
    {
        get => _away2.GetValueOrDefault();
        init
        {
            Guard(() => CheckAway(value, _away1, nameof(Away2)));
            _away2 = value;
        }
    }

    /// <summary>True when this game is the Crawford game: exactly one player is 1-away in it.</summary>
    /// <exception cref="ArgumentException">Thrown on init when the value is <see langword="true"/> and both away scores are set with not exactly one of them 1.</exception>
    public required bool IsCrawford
    {
        get => _isCrawford.GetValueOrDefault();
        init
        {
            Guard(() =>
            {
                if (value && _away1 is int away1 && _away2 is int away2 && !SessionRules.CrawfordStandingHolds(away1, away2))
                    throw new ArgumentException(SessionRules.CrawfordStandingMessage, nameof(IsCrawford));
            });
            _isCrawford = value;
        }
    }

    /// <inheritdoc/>
    public override bool Equals(GameStanding? other) =>
        other is MatchStanding match && match.Away1 == Away1 && match.Away2 == Away2 && match.IsCrawford == IsCrawford;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Kind, Away1, Away2, IsCrawford);

    /// <summary>
    /// The standing for a reader — <c>"match, 3-away/5-away, Crawford"</c>,
    /// player 1's first — for a test failure or a log; not a wire form.
    /// </summary>
    public override string ToString() => string.Create(
        CultureInfo.InvariantCulture,
        $"match, {Away1}-away/{Away2}-away{(IsCrawford ? ", Crawford" : "")}");

    /// <inheritdoc/>
    public override TResult Match<TResult>(Func<MoneyStanding, TResult> money, Func<MatchStanding, TResult> match)
    {
        ArgumentNullException.ThrowIfNull(money);
        ArgumentNullException.ThrowIfNull(match);
        return match(this);
    }

    /// <inheritdoc/>
    public override void Switch(Action<MoneyStanding> money, Action<MatchStanding> match)
    {
        ArgumentNullException.ThrowIfNull(money);
        ArgumentNullException.ThrowIfNull(match);
        match(this);
    }

    /// <summary>Holds an away score to its floor, and to the Crawford standing when the other and the flag are set.</summary>
    private void CheckAway(int away, int? other, string member)
    {
        if (!SessionRules.NeedsHolds(away, length: null))
            throw new ArgumentOutOfRangeException(member, away, SessionRules.NeedsMessage);
        if (_isCrawford == true && other is int known && !SessionRules.CrawfordStandingHolds(away, known))
            throw new ArgumentException(SessionRules.CrawfordStandingMessage, member);
    }
}
