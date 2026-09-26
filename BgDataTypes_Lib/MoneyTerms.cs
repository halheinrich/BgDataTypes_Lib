using System.Globalization;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// A money session's terms, as its header states them: the Jacoby and beaver
/// rules and the cube limit. One of the two kinds of <see cref="SessionTerms"/>;
/// no length, which belongs to a match. The same rules a record's
/// <see cref="MoneySession"/> carries, before any game is played.
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

    /// <summary>Whether the Jacoby rule is in force (<see cref="MoneySession.IsJacoby"/>).</summary>
    public required bool IsJacoby { get; init; }

    /// <summary>Whether the beaver rule is in force (<see cref="MoneySession.IsBeaver"/>).</summary>
    public required bool IsBeaver { get; init; }

    /// <summary>The highest value the cube may reach — a positive power of two (<see cref="MoneySession.CubeLimit"/>).</summary>
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
