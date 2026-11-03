using System.Diagnostics.CodeAnalysis;
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
/// <para>
/// <b>Two doors to the rules.</b> The initializer throws on a breach;
/// <see cref="TryCreate"/> hands back the refusal instead, for a caller that
/// reports it (halheinrich/backgammon#514). Both carry one refusal, composed
/// once: the rule's sentence and the values it refuses, on one line, the
/// same on every runtime — the exception's message begins with it, the
/// runtime's own tail after.
/// </para>
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

    /// <summary>
    /// The match standing <paramref name="away1"/>, <paramref name="away2"/>
    /// and <paramref name="isCrawford"/> state, or the refusal of the rule
    /// they break — the standing's rules asked without throwing, for a caller
    /// that reports a refusal rather than failing on it. The initializer is
    /// the throwing door to the same rules.
    /// </summary>
    /// <remarks>
    /// The rules are checked in a fixed order — player 1's away score, then
    /// player 2's, then the Crawford standing — the order an initializer
    /// setting <see cref="Away1"/>, <see cref="Away2"/> and
    /// <see cref="IsCrawford"/> in turn meets them, so for those values that
    /// initializer's exception message begins with this refusal. The refusal
    /// is the rule's sentence and then the values it refuses — the player and
    /// their away score, or both away scores, player 1's first — on one line,
    /// the same on every runtime.
    /// </remarks>
    /// <param name="away1">The points player 1 still needs to win the match.</param>
    /// <param name="away2">The points player 2 still needs to win the match.</param>
    /// <param name="isCrawford">Whether this game is the Crawford game.</param>
    /// <param name="standing">The standing when the values hold the rules; otherwise <see langword="null"/>.</param>
    /// <param name="refusal">The refusal of the first rule the values break; <see langword="null"/> when they hold them all.</param>
    /// <returns><see langword="true"/> when the values hold the standing's rules.</returns>
    public static bool TryCreate(
        int away1,
        int away2,
        bool isCrawford,
        [NotNullWhen(true)] out MatchStanding? standing,
        [NotNullWhen(false)] out string? refusal)
    {
        refusal = SessionRules.AwayFloorRefusal(Seat.Player1, away1)
            ?? SessionRules.AwayFloorRefusal(Seat.Player2, away2)
            ?? SessionRules.CrawfordStandingRefusal(isCrawford, away1, away2);
        standing = refusal is null ? new MatchStanding { Away1 = away1, Away2 = away2, IsCrawford = isCrawford } : null;
        return standing is not null;
    }

    /// <summary>The points player 1 still needs to win the match; at least 1.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on init when the value is below 1.</exception>
    /// <exception cref="ArgumentException">Thrown on init when this is the Crawford game and the value leaves not exactly one player 1-away.</exception>
    public required int Away1
    {
        get => _away1.GetValueOrDefault();
        init
        {
            Guard(() => CheckAway(Seat.Player1, value, _away2, nameof(Away1)));
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
            Guard(() => CheckAway(Seat.Player2, value, _away1, nameof(Away2)));
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
                if (_away1 is int away1 && _away2 is int away2
                    && SessionRules.CrawfordStandingRefusal(value, away1, away2) is string refusal)
                    throw new ArgumentException(refusal, nameof(IsCrawford));
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
    public override string ToString() =>
        $"match, {SessionRules.FormatAways(Away1, Away2)}{(IsCrawford ? ", Crawford" : "")}";

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

    /// <summary>Holds <paramref name="player"/>'s away score to its floor, and to the Crawford standing when the other and the flag are set.</summary>
    private void CheckAway(Seat player, int away, int? other, string member)
    {
        if (SessionRules.AwayFloorRefusal(player, away) is string floor)
            throw new ArgumentOutOfRangeException(member, away, floor);
        if (_isCrawford is bool isCrawford && other is int known)
        {
            var (away1, away2) = player == Seat.Player1 ? (away, known) : (known, away);
            if (SessionRules.CrawfordStandingRefusal(isCrawford, away1, away2) is string crawford)
                throw new ArgumentException(crawford, member);
        }
    }
}
