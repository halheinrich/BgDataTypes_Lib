using System.Globalization;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// Where the players of a money session stand as a game begins: the points
/// each has won in the session (the game header's scores). One of the two
/// kinds of <see cref="GameStanding"/>; no away scores and no Crawford game,
/// which belong to a match. Seen from the player on roll, the same facts are a
/// record's <see cref="MoneySession.OnRollScore"/> and
/// <see cref="MoneySession.OpponentScore"/>.
/// </summary>
/// <remarks>
/// <b>Well-formed by construction.</b> Each score is at least 0; each init
/// setter refuses a breach with an <see cref="ArgumentOutOfRangeException"/>
/// naming the member, and a document breaking one gets a
/// <see cref="System.Text.Json.JsonException"/> carrying it. A document
/// stating a member this standing does not have — a match's — is refused,
/// never read with the member dropped.
/// </remarks>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class MoneyStanding : GameStanding
{
    private readonly int _score1;
    private readonly int _score2;

    /// <summary>Creates a money standing; the members are set by the initializer.</summary>
    public MoneyStanding() : base(SessionKind.Money)
    {
    }

    /// <summary>
    /// The serializer's constructor. It binds the document's
    /// <paramref name="kind"/>, which must be <see cref="SessionKind.Money"/>;
    /// why the pattern exists is stated once, on
    /// <see cref="BgDataTypesJsonContext"/> ("The serializer constructors").
    /// </summary>
    [JsonConstructor]
    internal MoneyStanding(SessionKind kind) : base(SessionKind.Money, kind)
    {
    }

    /// <summary>The points player 1 has won in the session before this game; at least 0.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on init when the value is negative.</exception>
    public required int Score1
    {
        get => _score1;
        init
        {
            Guard(() => CheckScore(value, nameof(Score1)));
            _score1 = value;
        }
    }

    /// <summary>The points player 2 has won in the session before this game; at least 0.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on init when the value is negative.</exception>
    public required int Score2
    {
        get => _score2;
        init
        {
            Guard(() => CheckScore(value, nameof(Score2)));
            _score2 = value;
        }
    }

    /// <inheritdoc/>
    public override bool Equals(GameStanding? other) =>
        other is MoneyStanding money && money.Score1 == Score1 && money.Score2 == Score2;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Kind, Score1, Score2);

    /// <summary>The standing for a reader — <c>"money, 3-0"</c>, player 1's first — for a test failure or a log; not a wire form.</summary>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"money, {Score1}-{Score2}");

    /// <inheritdoc/>
    public override TResult Match<TResult>(Func<MoneyStanding, TResult> money, Func<MatchStanding, TResult> match)
    {
        ArgumentNullException.ThrowIfNull(money);
        ArgumentNullException.ThrowIfNull(match);
        return money(this);
    }

    /// <inheritdoc/>
    public override void Switch(Action<MoneyStanding> money, Action<MatchStanding> match)
    {
        ArgumentNullException.ThrowIfNull(money);
        ArgumentNullException.ThrowIfNull(match);
        money(this);
    }

    private static void CheckScore(int score, string member)
    {
        if (!SessionRules.ScoreHolds(score))
            throw new ArgumentOutOfRangeException(member, score, SessionRules.ScoreMessage);
    }
}
