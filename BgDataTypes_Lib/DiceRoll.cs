using System.Numerics;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// A backgammon dice roll in canonical unordered form: <see cref="High"/> and
/// <see cref="Low"/>, each a die face 1–6. The constructor accepts the two
/// faces in either order and canonicalizes, so 3-1 and 1-3 are the same value
/// — value equality falls out of the record-struct semantics over the
/// canonical form. Canonicalization is single-sourced here: producers stamp
/// dice in rolled order and consumers never re-canonicalize.
/// </summary>
/// <remarks>
/// <para>
/// The canonical text form is the two-digit token, high face first — "31",
/// "55" — emitted by <see cref="ToString"/> and read back by
/// <see cref="Parse(string, IFormatProvider?)"/> / <c>TryParse</c> (which
/// also accept the low-first spelling, canonicalizing "13" to the same value
/// as "31"). JSON round-trips as that token via the bundled
/// <see cref="DiceRollJsonConverter"/> — no consumer-side converter
/// registration, per this library's serialization contract.
/// </para>
/// <para>
/// Ordering (<see cref="CompareTo(DiceRoll)"/> and the comparison operators)
/// is ascending by <see cref="High"/>, then <see cref="Low"/> —
/// equivalently, ascending canonical token:
/// 11 &lt; 21 &lt; 22 &lt; 31 &lt; … &lt; 66.
/// </para>
/// <para>
/// <c>default(DiceRoll)</c> is <strong>not meaningful</strong>: the
/// <see langword="default"/> of a <see langword="struct"/> bypasses
/// construction and so escapes face validation, yielding faces of 0. This is
/// the standard value-type caveat shared with <see cref="Play"/> and
/// <see cref="CubeDecisionPair"/>; construct instances explicitly rather
/// than relying on <see langword="default"/>. "No roll" is modelled as
/// <c>DiceRoll?</c> null (see <see cref="IDecisionFilterData.Dice"/>), never
/// as <see langword="default"/>.
/// </para>
/// </remarks>
[JsonConverter(typeof(DiceRollJsonConverter))]
public readonly record struct DiceRoll :
    IComparable,
    IComparable<DiceRoll>,
    IComparisonOperators<DiceRoll, DiceRoll, bool>,
    IParsable<DiceRoll>,
    ISpanParsable<DiceRoll>
{
    /// <summary>The higher die face (1–6); equals <see cref="Low"/> on doubles.</summary>
    public int High { get; }

    /// <summary>The lower die face (1–6); equals <see cref="High"/> on doubles.</summary>
    public int Low { get; }

    /// <summary>
    /// Creates a roll from two die faces given in either order,
    /// canonicalizing to <see cref="High"/> / <see cref="Low"/>.
    /// </summary>
    /// <param name="die1">One die face, 1–6.</param>
    /// <param name="die2">The other die face, 1–6.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when either face is outside 1–6.
    /// </exception>
    public DiceRoll(int die1, int die2)
    {
        ValidateFace(die1, nameof(die1));
        ValidateFace(die2, nameof(die2));
        High = Math.Max(die1, die2);
        Low = Math.Min(die1, die2);
    }

    private static void ValidateFace(int face, string paramName)
    {
        if (face is < 1 or > 6)
            throw new ArgumentOutOfRangeException(paramName, face,
                "A die face must be between 1 and 6.");
    }

    /// <summary>True when both dice show the same face.</summary>
    public bool IsDouble => High == Low;

    // -----------------------------------------------------------------------
    //  Enumeration
    // -----------------------------------------------------------------------

    /// <summary>
    /// The 21 distinct dice rolls, in ascending canonical order — the same
    /// order <see cref="CompareTo(DiceRoll)"/> imposes
    /// (11 &lt; 21 &lt; 22 &lt; 31 &lt; … &lt; 66), the six doubles included.
    /// </summary>
    /// <remarks>
    /// This is the single source for the full roll set: consumers that
    /// enumerate every possible roll — a filter grid, a distribution table —
    /// read it here rather than rebuilding the nested face loop. That
    /// enumeration is domain knowledge this type owns.
    /// </remarks>
    public static IReadOnlyList<DiceRoll> All { get; } = BuildAll();

    private static DiceRoll[] BuildAll()
    {
        var all = new DiceRoll[21];
        int i = 0;
        for (int high = 1; high <= 6; high++)
            for (int low = 1; low <= high; low++)
                all[i++] = new DiceRoll(high, low);
        return all;
    }

    /// <summary>Deconstructs into (<see cref="High"/>, <see cref="Low"/>).</summary>
    /// <param name="high">The higher die face.</param>
    /// <param name="low">The lower die face.</param>
    public void Deconstruct(out int high, out int low)
    {
        high = High;
        low = Low;
    }

    /// <summary>
    /// The canonical two-digit token, high face first — "31", "55".
    /// </summary>
    public override string ToString() => $"{High}{Low}";

    // -----------------------------------------------------------------------
    //  Parsing
    // -----------------------------------------------------------------------

    /// <summary>
    /// Parses a two-digit roll token — either spelling, canonicalizing
    /// ("31" and "13" parse to the same value).
    /// </summary>
    /// <param name="s">The token to parse.</param>
    /// <param name="provider">Ignored — the token form is culture-invariant.</param>
    /// <returns>The parsed roll in canonical form.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="s"/> is null.</exception>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="s"/> is not exactly two digits 1–6.
    /// </exception>
    public static DiceRoll Parse(string s, IFormatProvider? provider = null)
    {
        ArgumentNullException.ThrowIfNull(s);
        return Parse(s.AsSpan(), provider);
    }

    /// <inheritdoc cref="Parse(string, IFormatProvider?)"/>
    public static DiceRoll Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null)
        => TryParse(s, provider, out var result)
            ? result
            : throw new FormatException(
                $"Invalid DiceRoll token: '{s}'. Expected two digits 1–6, e.g. \"31\".");

    /// <summary>
    /// Attempts to parse a two-digit roll token — either spelling,
    /// canonicalizing ("31" and "13" parse to the same value).
    /// </summary>
    /// <param name="s">The token to parse.</param>
    /// <param name="provider">Ignored — the token form is culture-invariant.</param>
    /// <param name="result">
    /// The parsed roll in canonical form, or <see langword="default"/> on failure.
    /// </param>
    /// <returns>True when <paramref name="s"/> is exactly two digits 1–6.</returns>
    public static bool TryParse(string? s, IFormatProvider? provider, out DiceRoll result)
        => TryParse(s.AsSpan(), provider, out result);

    /// <inheritdoc cref="TryParse(string?, IFormatProvider?, out DiceRoll)"/>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out DiceRoll result)
    {
        result = default;
        if (s.Length != 2)
            return false;

        int a = s[0] - '0';
        int b = s[1] - '0';
        if (a is < 1 or > 6 || b is < 1 or > 6)
            return false;

        result = new DiceRoll(a, b);
        return true;
    }

    // -----------------------------------------------------------------------
    //  Ordering
    // -----------------------------------------------------------------------

    /// <summary>
    /// Compares by <see cref="High"/>, then <see cref="Low"/> — ascending
    /// canonical-token order (11 &lt; 21 &lt; 22 &lt; 31 &lt; … &lt; 66).
    /// </summary>
    /// <param name="other">The roll to compare against.</param>
    /// <returns>Negative, zero, or positive per the standard contract.</returns>
    public int CompareTo(DiceRoll other)
    {
        int byHigh = High.CompareTo(other.High);
        return byHigh != 0 ? byHigh : Low.CompareTo(other.Low);
    }

    int IComparable.CompareTo(object? obj) => obj switch
    {
        null => 1,
        DiceRoll other => CompareTo(other),
        _ => throw new ArgumentException(
            $"Object must be of type {nameof(DiceRoll)}.", nameof(obj)),
    };

    /// <summary>Orders per <see cref="CompareTo(DiceRoll)"/>.</summary>
    public static bool operator <(DiceRoll left, DiceRoll right) => left.CompareTo(right) < 0;

    /// <summary>Orders per <see cref="CompareTo(DiceRoll)"/>.</summary>
    public static bool operator <=(DiceRoll left, DiceRoll right) => left.CompareTo(right) <= 0;

    /// <summary>Orders per <see cref="CompareTo(DiceRoll)"/>.</summary>
    public static bool operator >(DiceRoll left, DiceRoll right) => left.CompareTo(right) > 0;

    /// <summary>Orders per <see cref="CompareTo(DiceRoll)"/>.</summary>
    public static bool operator >=(DiceRoll left, DiceRoll right) => left.CompareTo(right) >= 0;
}
