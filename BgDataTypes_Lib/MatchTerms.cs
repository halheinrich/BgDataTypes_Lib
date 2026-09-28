using System.Globalization;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// A match's terms, as its header states them: its length. One of the two
/// kinds of <see cref="SessionTerms"/>; no money rule, which a match has not.
/// A producer states the real length — XG's raw sentinel for a money session
/// (99999) is a money session's <see cref="MoneyTerms"/>, never a match.
/// </summary>
/// <remarks>
/// <para>
/// <b>No Max Cube</b> (Hal's ruling of 2026-09-27 on
/// halheinrich/backgammon#273: no cube limit is added to these terms). XG
/// match input states a Max Cube, and legitimate files state varying values,
/// but that does not establish a match-domain rule of its own — whether it is
/// one is halheinrich/backgammon#289 — so the terms state the length alone
/// and the record carries no Max Cube for a match. The cube limit the record
/// does carry is a money session's (<see cref="MoneyTerms.CubeLimit"/>). The
/// XGID derived for a match writes a constant in its Max Cube field (the
/// internal encoder states which, and why).
/// </para>
/// <para>
/// <b>Well-formed by construction.</b> The length is at least 1; its init
/// setter refuses a breach with an <see cref="ArgumentOutOfRangeException"/>
/// naming the member, and a document breaking it gets a
/// <see cref="System.Text.Json.JsonException"/> carrying it. A document
/// stating a member these terms do not have — a money rule — is refused,
/// never read with the member dropped.
/// </para>
/// </remarks>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class MatchTerms : SessionTerms
{
    private readonly int _length;

    /// <summary>Creates match terms; the member is set by the initializer.</summary>
    public MatchTerms() : base(SessionKind.Match)
    {
    }

    /// <summary>
    /// The serializer's constructor. It binds the document's
    /// <paramref name="kind"/>, which must be <see cref="SessionKind.Match"/>;
    /// why the pattern exists is stated once, on
    /// <see cref="BgDataTypesJsonContext"/> ("The serializer constructors").
    /// </summary>
    [JsonConstructor]
    internal MatchTerms(SessionKind kind) : base(SessionKind.Match, kind)
    {
    }

    /// <summary>The match's length: the points a player needs to win it, at least 1.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on init when the value is below 1.</exception>
    public required int Length
    {
        get => _length;
        init
        {
            Guard(() =>
            {
                if (!SessionRules.LengthHolds(value))
                    throw new ArgumentOutOfRangeException(nameof(Length), value, SessionRules.LengthMessage);
            });
            _length = value;
        }
    }

    /// <inheritdoc/>
    public override bool Equals(SessionTerms? other) => other is MatchTerms match && match.Length == Length;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Kind, Length);

    /// <summary>The terms for a reader — <c>"match to 7"</c> — for a test failure or a log; not a wire form.</summary>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"match to {Length}");

    /// <inheritdoc/>
    public override TResult Match<TResult>(Func<MoneyTerms, TResult> money, Func<MatchTerms, TResult> match)
    {
        ArgumentNullException.ThrowIfNull(money);
        ArgumentNullException.ThrowIfNull(match);
        return match(this);
    }

    /// <inheritdoc/>
    public override void Switch(Action<MoneyTerms> money, Action<MatchTerms> match)
    {
        ArgumentNullException.ThrowIfNull(money);
        ArgumentNullException.ThrowIfNull(match);
        match(this);
    }
}
