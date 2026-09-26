using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// The decision category of a <see cref="CheckerPlayDecision"/>: the roll,
/// the analysed candidate plays, which of them is best, and which the user
/// played and at what cost. It carries a checker play's fields and nothing
/// else — the cube's are on <see cref="CubeDecisionData"/>, and no member of
/// either kind stands for "not applicable" (halheinrich/backgammon#273).
/// Every member but the nullable ones is <c>required</c>, per the wire rule
/// stated on <see cref="BgDataTypesJsonContext"/>, and each nullable member's
/// documentation says what <see langword="null"/> means.
/// </summary>
/// <remarks>
/// <para>
/// <b>Well-formed by construction.</b> The roll is two faces 1–6; there is
/// at least one candidate; <see cref="BestPlayIndex"/> identifies one; and
/// <see cref="UserPlayIndex"/> identifies one or is <see langword="null"/>.
/// Each init setter checks what it can against the members already set, so
/// whichever of <see cref="Plays"/> and an index is set second refuses a
/// mismatch — in an object initializer in either order and in a JSON
/// document in either property order alike. <see cref="Plays"/> and
/// <see cref="BestPlayIndex"/> are required, so both setters always run and
/// the check is never skipped. The two collections are copied on init, so a
/// caller keeping its own list cannot change them afterwards. Code breaking a
/// rule gets the guard's <see cref="ArgumentException"/>; a document breaking
/// it, read as this type or inside a record, gets a
/// <see cref="System.Text.Json.JsonException"/> carrying it (the wire rule on
/// <see cref="BgDataTypesJsonContext"/>).
/// </para>
/// <para>
/// Whether each candidate is valid from the decision's position needs the
/// position, so the record holds it: see <see cref="CheckerPlayDecision"/>.
/// </para>
/// </remarks>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CheckerPlayDecisionData
{
    // Null only while construction is still stating the member; `required`
    // guarantees each is set by the time it ends.
    private readonly IReadOnlyList<int>? _dice;
    private readonly IReadOnlyList<PlayCandidate>? _plays;
    private readonly int? _bestPlayIndex;
    private readonly int? _userPlayIndex;

    // True while the category is read from a document (see the serializer's
    // constructor below): each rule then refuses as a JsonException.
    private readonly bool _read;

    /// <summary>Creates the category; its members are set by the initializer.</summary>
    public CheckerPlayDecisionData()
    {
    }

    /// <summary>
    /// The serializer's constructor, for a category read from a document: it
    /// marks the category as read before any member is set, so every rule
    /// refuses a breach as a <see cref="System.Text.Json.JsonException"/> (the
    /// wire rule on <see cref="BgDataTypesJsonContext"/>). It takes
    /// <paramref name="dice"/> only because a serializer constructor must bind
    /// a member.
    /// </summary>
    [JsonConstructor]
    internal CheckerPlayDecisionData(IReadOnlyList<int> dice)
    {
        _read = true;
        Dice = dice;
    }

    /// <summary>
    /// The two dice as rolled, in rolled order — the order a diagram draws
    /// them and an <c>.xgp</c> export writes them back. Each face is 1–6.
    /// <see cref="CheckerPlayDecision.Dice"/> is the canonical unordered
    /// form, a <see cref="DiceRoll"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown on init when the value is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown on init when the value does not hold exactly two faces.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on init when a face is outside 1–6.</exception>
    public required IReadOnlyList<int> Dice
    {
        get => _dice!;
        init
        {
            try
            {
                ArgumentNullException.ThrowIfNull(value, nameof(Dice));
                if (value.Count != 2)
                    throw new ArgumentException(
                        $"A checker play's roll is two dice (got {value.Count}).", nameof(Dice));
                foreach (int face in value)
                {
                    if (face is < 1 or > 6)
                        throw new ArgumentOutOfRangeException(nameof(Dice), face, "A die face is 1 to 6.");
                }
            }
            catch (ArgumentException fault) when (_read)
            {
                throw DocumentRefusal.Of(fault);
            }
            _dice = Array.AsReadOnly([value[0], value[1]]);
        }
    }

    /// <summary>
    /// The analysed candidate plays, in producer-supplied order (not
    /// guaranteed equity-sorted); never empty. <see cref="BestPlayIndex"/>
    /// and <see cref="UserPlayIndex"/> index into this list.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown on init when the value is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown on init when the list is empty or holds a <see langword="null"/>
    /// candidate, or when an index already set does not identify a candidate
    /// of it.
    /// </exception>
    public required IReadOnlyList<PlayCandidate> Plays
    {
        get => _plays!;
        init
        {
            PlayCandidate[] plays;
            try
            {
                ArgumentNullException.ThrowIfNull(value, nameof(Plays));
                if (value.Count == 0)
                    throw new ArgumentException(
                        "A checker-play decision has at least one analysed candidate.", nameof(Plays));
                plays = value.ToArray();
                if (Array.Exists(plays, candidate => candidate is null))
                    throw new ArgumentException("A candidate play is null.", nameof(Plays));
                if (_bestPlayIndex is int best && best >= plays.Length)
                    throw new ArgumentException(
                        $"BestPlayIndex {best} identifies no candidate of {plays.Length}.", nameof(Plays));
                if (_userPlayIndex is int user && user >= plays.Length)
                    throw new ArgumentException(
                        $"UserPlayIndex {user} identifies no candidate of {plays.Length}.", nameof(Plays));
            }
            catch (ArgumentException fault) when (_read)
            {
                throw DocumentRefusal.Of(fault);
            }
            _plays = Array.AsReadOnly(plays);
        }
    }

    /// <summary>
    /// Index into <see cref="Plays"/> of the best play — the canonical single
    /// best when several share zero equity loss (see
    /// <see cref="PlayCandidate.EquityLoss"/>). Always identifies a candidate;
    /// <see cref="BestPlay"/> is that candidate.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown on init when the value is negative, or when
    /// <see cref="Plays"/> is already set and holds no candidate at it.
    /// </exception>
    public required int BestPlayIndex
    {
        get => _bestPlayIndex.GetValueOrDefault();
        init
        {
            try
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(BestPlayIndex));
                if (_plays is not null)
                    ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(value, _plays.Count, nameof(BestPlayIndex));
            }
            catch (ArgumentException fault) when (_read)
            {
                throw DocumentRefusal.Of(fault);
            }
            _bestPlayIndex = value;
        }
    }

    /// <summary>
    /// Index into <see cref="Plays"/> of the play the user made, or
    /// <see langword="null"/> when there is none among the candidates: no
    /// user play was recorded (an analysis-only position), or the user's play
    /// was not among the analysed candidates. The single source of "which
    /// candidate did the user play" — there is deliberately no per-candidate
    /// flag to keep consistent with it. <see cref="UserPlay"/> is that
    /// candidate.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown on init when the value is negative — "none" is
    /// <see langword="null"/>, never -1 — or when <see cref="Plays"/> is
    /// already set and holds no candidate at it.
    /// </exception>
    public int? UserPlayIndex
    {
        get => _userPlayIndex;
        init
        {
            try
            {
                if (value is int index)
                {
                    ArgumentOutOfRangeException.ThrowIfNegative(index, nameof(UserPlayIndex));
                    if (_plays is not null)
                        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _plays.Count, nameof(UserPlayIndex));
                }
            }
            catch (ArgumentException fault) when (_read)
            {
                throw DocumentRefusal.Of(fault);
            }
            _userPlayIndex = value;
        }
    }

    /// <summary>
    /// Equity loss of the user's checker play against the best play (≥ 0);
    /// <see langword="null"/> when no user play is recorded.
    /// </summary>
    public double? UserPlayError { get; init; }

    /// <summary>The candidate <see cref="BestPlayIndex"/> identifies.</summary>
    [JsonIgnore]
    public PlayCandidate BestPlay => Plays[BestPlayIndex];

    /// <summary>
    /// The candidate <see cref="UserPlayIndex"/> identifies, or
    /// <see langword="null"/> when the user's play is not among the
    /// candidates.
    /// </summary>
    [JsonIgnore]
    public PlayCandidate? UserPlay => UserPlayIndex is int index ? Plays[index] : null;
}
