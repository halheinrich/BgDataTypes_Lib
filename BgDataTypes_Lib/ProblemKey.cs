using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// Content identity of a backgammon decision problem — the key under which
/// lifetime stats and position dedupe recognise "the same problem" across
/// files (SPEC-stats-identity.md; halheinrich/backgammon#95). Sibling to
/// <see cref="DecisionId"/>, which remains the file-navigation identity:
/// <see cref="DecisionId"/> answers "where did this record come from",
/// <see cref="ProblemKey"/> answers "which problem is this".
///
/// <para>
/// A fact participates iff it can change the correct answer — and only then:
/// the on-roll-relative board (both bars), the away-scores pair
/// (<c>0</c>/<c>0</c> = money game), the Crawford flag, the cube state
/// (size and owner — for both decision kinds), the Jacoby rule
/// (<see cref="PositionData.IsJacoby"/> — <b>money keys only</b>: with a
/// centered cube it voids undoubled gammons and shifts the doubling window,
/// and it is meaningless off money), and, for checker plays only, the dice
/// in canonical unordered form. The decision kind rides on the dice:
/// a play key carries them, a cube key carries none. Beaver and max-cube,
/// match length, raw scores, seat/turn, and all provenance are excluded by
/// ratified ruling — this key therefore collapses strictly more than the
/// XGID string does
/// (same-away positions from different match lengths unify; mirror-turn
/// duplicates unify), which is the intended consequence, not a defect.
/// </para>
///
/// <para>
/// <b>Canonical string form — a pinned wire contract.</b> The grammar below
/// keys the stats document; it is derived from the decomposed facts and
/// deliberately neither derived from nor resembling the raw XGID string
/// (XGID is display and provenance only). Grammar:
/// <code>
/// key    := board '/' score '/' cube [ '/' dice ]
/// board  := 26 comma-separated signed decimal integers, Mop order
///           ([0] opponent bar &lt;= 0, [1..24] points, [25] on-roll bar &gt;= 0;
///           positive = on-roll player's checkers)
/// score  := match | money
/// match  := onRollAway 'a' opponentAway [ 'cr' ]   ; both away scores &gt; 0
/// money  := '0a0' jacoby                           ; money game
/// jacoby := 'j'                                    ; Jacoby rule in force
///         | 'nj'                                   ; Jacoby rule not in force
/// cube   := size owner                             ; owner: c=centered,
///                                                  ;   o=on-roll, p=opponent
/// dice   := two digits, high face first ("31")     ; present iff checker play
/// </code>
/// Example play key (standard start, 7-away/7-away, centered cube, roll 3-1):
/// <c>0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/7a7/1c/31</c>;
/// the same position for money under Jacoby ends
/// <c>…/0a0j/1c/31</c>.
/// The form is culture-invariant and admits exactly one spelling per value,
/// so ordinal string equality coincides with key equality — equality,
/// hashing and ordering are all defined over the canonical string.
/// </para>
///
/// <para>
/// <b>The Jacoby suffix is money-only, by ruling.</b> It rides the score
/// field of a money key (<c>0a0</c>) and nothing else; every match key is
/// byte-identical to what the v2 grammar emitted, because a slot that is
/// definitionally meaningless off money would contradict §1's "and only
/// then". Both of the fact's values carry a token (<c>j</c> / <c>nj</c>)
/// rather than presence-encoding one of them the way <c>cr</c> encodes
/// Crawford: the absent spelling is the v2 money key, and admitting it
/// would give the money value two spellings — one of them silently wrong,
/// since a v2 money key says nothing about Jacoby. So a v2 money key does
/// not parse under this grammar, deliberately: the stats document's schema
/// version (v3) is what retires it, and the read side surfaces the retired
/// file rather than reinterpreting it (SPEC-stats-identity.md §3).
/// </para>
///
/// <para>
/// <b>Strict parse.</b> <see cref="Parse(string, IFormatProvider?)"/> /
/// <c>TryParse</c> accept only the exact canonical spelling: low-first dice,
/// leading zeros, explicit <c>'+'</c>, whitespace, or any field-shape
/// deviation are rejected. This deliberately diverges from
/// <see cref="DiceRoll"/>'s lenient, canonicalizing parse — that serves
/// human input; this is a wire format, where two spellings of one key would
/// let a stats document split a problem's tallies. Enforcement is
/// structural: the parser re-formats the parsed facts and requires ordinal
/// equality with the input, so only canonical spellings survive.
/// </para>
///
/// <para>
/// <b>Construction and the no-key rung.</b> There are exactly two ways to
/// obtain a key: <see cref="TryDerive"/> (the single producer-side factory
/// from a decision record's facts) and <c>Parse</c>/<c>TryParse</c> (the
/// wire read-back). Consumers never assemble a key by hand — there is no
/// public constructor, and the type is a sealed class rather than a record
/// precisely so no <c>with</c>-expression hatch exists. Derivation that
/// would guess is forbidden: where the record's facts are malformed,
/// degenerate, or inconsistent (see <see cref="TryDerive"/>), there is no
/// key — <see cref="TryDerive"/> returns <see langword="false"/> rather
/// than throwing, and the same fact validation guards the parse door.
/// </para>
///
/// <para>
/// <b>Real-board posture.</b> Fact validation requires a physically possible
/// position: at most 15 checkers per side, per-point counts within ±15, each
/// bar holding only its own side's checkers, and at least one checker on the
/// board. <see cref="BoardState.FromMop"/> deliberately tolerates
/// pseudoboards — that is an affordance of a general board utility;
/// <see cref="ProblemKey"/> identifies real analysed decisions, whose
/// producer-stamped boards always satisfy these bounds, so a violation is
/// corruption and corruption gets no key.
/// </para>
///
/// <para>
/// There is no version token inside the key: the containing stats
/// document's schema version pins the grammar, and a fact entering identity
/// bumps that version rather than the key's shape. The Jacoby suffix is the
/// mechanism's first exercise — the fact entered by amendment on 2026-08-20
/// (halheinrich/backgammon#120), taking the document from v2 to v3
/// (SPEC-stats-identity.md §1, §2, §3).
/// </para>
///
/// <para>
/// Ordering (<see cref="CompareTo(ProblemKey?)"/>) is ordinal over the
/// canonical string — semantically arbitrary but stable, provided so
/// document writers can emit keys in a deterministic, diffable order.
/// JSON round-trips as the canonical string via the bundled
/// <see cref="ProblemKeyJsonConverter"/> (type-level attribute — no
/// consumer-side registration), including use as a dictionary key.
/// </para>
/// </summary>
[JsonConverter(typeof(ProblemKeyJsonConverter))]
public sealed class ProblemKey :
    IEquatable<ProblemKey>,
    IComparable,
    IComparable<ProblemKey>,
    IParsable<ProblemKey>,
    ISpanParsable<ProblemKey>
{
    /// <summary>
    /// The canonical string — the single source of identity: equality,
    /// hashing, ordering and <see cref="ToString"/> all read this.
    /// </summary>
    private readonly string _canonical;

    /// <summary>
    /// The decision kind this key identifies: <see langword="true"/> for a
    /// cube decision (no dice field), <see langword="false"/> for a checker
    /// play (dice field present). The only decomposed fact the type exposes —
    /// the key is an identity, not a position record; the record's
    /// <see cref="PositionData"/> remains the fact source.
    /// </summary>
    public bool IsCubeDecision { get; }

    private ProblemKey(string canonical, bool isCubeDecision)
    {
        _canonical = canonical;
        IsCubeDecision = isCubeDecision;
    }

    // -----------------------------------------------------------------------
    //  Derivation — the single producer-side factory
    // -----------------------------------------------------------------------

    /// <summary>
    /// Attempts to derive the content key from a decision record's facts —
    /// the single derivation site in the ecosystem; consumers never assemble
    /// a key by hand.
    /// </summary>
    /// <param name="data">The decision record to derive from.</param>
    /// <param name="key">
    /// On success, the derived key; on failure, <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="false"/> — no key, per the ratified no-key rung —
    /// when the facts are malformed, degenerate, or inconsistent: a missing
    /// or non-26-element board; an empty board; a per-point count outside
    /// ±15; more than 15 checkers on either side; a checker on the wrong
    /// bar; a negative away score; exactly one away score zero (money is
    /// <c>0</c>/<c>0</c> only — a single 0-away side means the match is
    /// over); a Crawford flag in a money game or with neither side 1-away;
    /// a money record whose <see cref="PositionData.IsJacoby"/> is
    /// <see langword="null"/> (the fact the money grammar spells is not
    /// supplied — guessing "off" is exactly what this rung forbids); a cube
    /// size that is not a positive power of two; an undefined
    /// <see cref="CubeOwner"/> value; or a checker play whose dice are
    /// unstamped or outside 1–6. Otherwise <see langword="true"/>.
    /// A <see cref="PositionData.IsJacoby"/> stamp on a <em>match</em>
    /// record is not a rejection rung: the fact is meaningless off money,
    /// so it is ignored and the match key is unaffected.
    /// Never throws on bad facts (degrade, never block).
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="data"/> is <see langword="null"/> — a
    /// missing record is a caller bug, not malformed facts.
    /// </exception>
    public static bool TryDerive(
        BgDecisionData data,
        [NotNullWhen(true)] out ProblemKey? key)
    {
        ArgumentNullException.ThrowIfNull(data);
        key = null;

        // Category members are declared non-nullable, but lenient JSON input
        // can null them through init — a malformed record, so no key.
        if (data.Position is not { } position || data.Decision is not { } decision)
            return false;

        DiceRoll? dice = null;
        if (!decision.IsCube)
        {
            // Read the raw stored faces, not BgDecisionData.Dice — that
            // derivation throws on malformed faces, and derivation that
            // would guess (or blow up) is exactly what this rung forbids.
            var faces = decision.Dice;
            if (faces is null || faces.Count != 2
                || faces[0] is < 1 or > 6 || faces[1] is < 1 or > 6)
                return false;
            dice = new DiceRoll(faces[0], faces[1]);
        }

        if (!AreValidFacts(
                position.Mop,
                position.OnRollNeeds, position.OpponentNeeds, position.IsCrawford,
                position.CubeSize, position.CubeOwner, position.IsJacoby))
            return false;

        key = new ProblemKey(
            FormatCanonical(
                position.Mop,
                position.OnRollNeeds, position.OpponentNeeds, position.IsCrawford,
                position.CubeSize, position.CubeOwner, position.IsJacoby, dice),
            isCubeDecision: decision.IsCube);
        return true;
    }

    // -----------------------------------------------------------------------
    //  Fact validation — shared by TryDerive and the parse door
    // -----------------------------------------------------------------------

    /// <summary>
    /// The no-key rung's fact validation (dice are validated separately at
    /// each door, before a <see cref="DiceRoll"/> can exist). See
    /// <see cref="TryDerive"/> for the full rejection list.
    /// </summary>
    private static bool AreValidFacts(
        IReadOnlyList<int>? board,
        int onRollAway, int opponentAway, bool isCrawford,
        int cubeSize, CubeOwner cubeOwner, bool? isJacoby)
    {
        // Board: real-board posture (see the type remarks).
        if (board is null || board.Count != 26)
            return false;
        int onRollTotal = 0, opponentTotal = 0;
        for (int i = 0; i < 26; i++)
        {
            int v = board[i];
            if (v is < -15 or > 15)
                return false;
            if (v > 0) onRollTotal += v;
            else opponentTotal -= v;
        }
        if (onRollTotal == 0 && opponentTotal == 0)
            return false;                              // empty board
        if (onRollTotal > 15 || opponentTotal > 15)
            return false;                              // per-side totals
        if (board[0] > 0 || board[25] < 0)
            return false;                              // wrong-bar checkers

        // Away scores: money is 0a0 only; one 0-away side = match over.
        if (onRollAway < 0 || opponentAway < 0)
            return false;
        if ((onRollAway == 0) != (opponentAway == 0))
            return false;

        // Crawford: barred in money; requires a 1-away side.
        if (isCrawford && (onRollAway == 0 || (onRollAway != 1 && opponentAway != 1)))
            return false;

        // Jacoby: the money grammar spells it, so a money record must carry
        // it — an absent fact is the no-key rung, never a guessed "off".
        // Off money the question does not arise: a stamped value there is
        // ignored (not rejected) — the in-tree producer stamps money records
        // only, but a meaningless member must never cost a match key.
        if (onRollAway == 0 && isJacoby is null)
            return false;

        // Cube: positive power of two, defined owner.
        if (cubeSize < 1 || (cubeSize & (cubeSize - 1)) != 0)
            return false;
        if (cubeOwner is not (CubeOwner.OnRoll or CubeOwner.Opponent or CubeOwner.Centered))
            return false;

        return true;
    }

    // -----------------------------------------------------------------------
    //  Canonical formatting
    // -----------------------------------------------------------------------

    /// <summary>
    /// Canonical score-field token for the Crawford game. Presence-encoded:
    /// emitted only for a Crawford key, absent otherwise.
    /// </summary>
    private const string CrawfordToken = "cr";

    /// <summary>
    /// Canonical money-key suffix for "Jacoby rule in force". Unlike
    /// <see cref="CrawfordToken"/> the Jacoby fact is <em>not</em>
    /// presence-encoded: both of its values carry a token
    /// (<see cref="JacobyOffToken"/> spells the other), because absence is
    /// the v2 money spelling, which the v3 grammar deliberately does not
    /// admit.
    /// </summary>
    private const string JacobyOnToken = "j";

    /// <summary>
    /// Canonical money-key suffix for "Jacoby rule not in force" — see
    /// <see cref="JacobyOnToken"/>.
    /// </summary>
    private const string JacobyOffToken = "nj";

    /// <summary>
    /// Formats validated facts as the canonical string. The single emitter:
    /// derivation builds the key's string here, and the parser re-formats
    /// through here to enforce the one-spelling-per-value contract.
    /// All numeric formatting is explicitly invariant.
    /// </summary>
    private static string FormatCanonical(
        IReadOnlyList<int> board,
        int onRollAway, int opponentAway, bool isCrawford,
        int cubeSize, CubeOwner cubeOwner, bool? isJacoby, DiceRoll? dice)
    {
        var sb = new StringBuilder(96);

        for (int i = 0; i < 26; i++)
        {
            if (i > 0)
                sb.Append(',');
            sb.Append(board[i].ToString(CultureInfo.InvariantCulture));
        }

        sb.Append('/')
          .Append(onRollAway.ToString(CultureInfo.InvariantCulture))
          .Append('a')
          .Append(opponentAway.ToString(CultureInfo.InvariantCulture));
        if (onRollAway == 0)
        {
            // Money (0a0): the ruled money-only Jacoby suffix. Crawford is
            // barred here, and AreValidFacts has already guaranteed the fact
            // is present, so the value read is safe.
            sb.Append(isJacoby!.Value ? JacobyOnToken : JacobyOffToken);
        }
        else if (isCrawford)
        {
            sb.Append(CrawfordToken);
        }

        sb.Append('/')
          .Append(cubeSize.ToString(CultureInfo.InvariantCulture))
          .Append(OwnerLetter(cubeOwner));

        if (dice is { } roll)
            sb.Append('/').Append(roll.ToString());

        return sb.ToString();
    }

    /// <summary>Canonical owner letter: c=centered, o=on-roll, p=opponent.</summary>
    private static char OwnerLetter(CubeOwner owner) => owner switch
    {
        CubeOwner.Centered => 'c',
        CubeOwner.OnRoll   => 'o',
        CubeOwner.Opponent => 'p',
        // Unreachable behind AreValidFacts; kept as a guard for future enum growth.
        _ => throw new ArgumentOutOfRangeException(nameof(owner), owner,
            "Undefined CubeOwner value."),
    };

    private static bool TryOwnerFromLetter(char letter, out CubeOwner owner)
    {
        switch (letter)
        {
            case 'c': owner = CubeOwner.Centered; return true;
            case 'o': owner = CubeOwner.OnRoll;   return true;
            case 'p': owner = CubeOwner.Opponent; return true;
            default:  owner = default;            return false;
        }
    }

    // -----------------------------------------------------------------------
    //  Parse / TryParse — span overloads are the primary implementation;
    //  string overloads delegate to them for parity.
    // -----------------------------------------------------------------------

    /// <summary>Parses the canonical string form of a <see cref="ProblemKey"/>.</summary>
    /// <param name="s">The canonical string form (see <see cref="ProblemKey"/>).</param>
    /// <param name="provider">Ignored; the canonical form is culture-invariant.</param>
    /// <returns>The parsed key.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="s"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="s"/> is not the exact canonical form —
    /// including any string whose decomposed facts the no-key rung rejects
    /// (see <see cref="TryDerive"/>).
    /// </exception>
    public static ProblemKey Parse(string s, IFormatProvider? provider = null)
    {
        ArgumentNullException.ThrowIfNull(s);
        return Parse(s.AsSpan(), provider);
    }

    /// <summary>Attempts to parse the canonical string form of a <see cref="ProblemKey"/>.</summary>
    /// <param name="s">The canonical string form, or <see langword="null"/>.</param>
    /// <param name="provider">Ignored; the canonical form is culture-invariant.</param>
    /// <param name="result">
    /// On success, the parsed key; on failure, <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> on success; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out ProblemKey result)
    {
        if (s is null)
        {
            result = null;
            return false;
        }
        return TryParse(s.AsSpan(), provider, out result);
    }

    /// <inheritdoc cref="Parse(string, IFormatProvider)"/>
    public static ProblemKey Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null)
    {
        if (!TryParse(s, provider, out var result))
            throw new FormatException($"Invalid ProblemKey canonical form: '{s}'.");
        return result;
    }

    /// <inheritdoc cref="TryParse(string, IFormatProvider, out ProblemKey)"/>
    public static bool TryParse(
        ReadOnlySpan<char> s,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out ProblemKey result)
    {
        result = null;

        // ---- Field split: board '/' score '/' cube [ '/' dice ] ----
        int firstSlash = s.IndexOf('/');
        if (firstSlash < 0)
            return false;
        var boardSpan = s[..firstSlash];
        var rest = s[(firstSlash + 1)..];

        int secondSlash = rest.IndexOf('/');
        if (secondSlash < 0)
            return false;
        var scoreSpan = rest[..secondSlash];
        rest = rest[(secondSlash + 1)..];

        ReadOnlySpan<char> cubeSpan, diceSpan;
        int thirdSlash = rest.IndexOf('/');
        if (thirdSlash < 0)
        {
            cubeSpan = rest;
            diceSpan = default;
        }
        else
        {
            cubeSpan = rest[..thirdSlash];
            diceSpan = rest[(thirdSlash + 1)..];
            // A fifth field invalidates the input.
            if (diceSpan.IndexOf('/') >= 0)
                return false;
        }
        bool hasDice = thirdSlash >= 0;

        // ---- Board: exactly 26 comma-separated signed integers ----
        var board = new int[26];
        int index = 0;
        var remaining = boardSpan;
        while (true)
        {
            int comma = remaining.IndexOf(',');
            var token = comma < 0 ? remaining : remaining[..comma];
            if (index >= 26 || !TryParseInvariantInt(token, allowSign: true, out board[index]))
                return false;
            index++;
            if (comma < 0)
                break;
            remaining = remaining[(comma + 1)..];
        }
        if (index != 26)
            return false;

        // ---- Score: onRollAway 'a' opponentAway [ 'cr' | 'j' | 'nj' ] ----
        int aIndex = scoreSpan.IndexOf('a');
        if (aIndex < 0)
            return false;
        var opponentSpan = scoreSpan[(aIndex + 1)..];

        // Jacoby suffix (money grammar). "nj" is tested first: it ends in the
        // same letter as "j", so the longer token has to win. A suffix on a
        // match key tokenizes here but cannot survive — the re-format identity
        // check below rejects it, since match keys never emit one.
        bool? isJacoby = null;
        if (opponentSpan.EndsWith(JacobyOffToken))
        {
            isJacoby = false;
            opponentSpan = opponentSpan[..^JacobyOffToken.Length];
        }
        else if (opponentSpan.EndsWith(JacobyOnToken))
        {
            isJacoby = true;
            opponentSpan = opponentSpan[..^JacobyOnToken.Length];
        }

        bool isCrawford = opponentSpan.EndsWith(CrawfordToken);
        if (isCrawford)
            opponentSpan = opponentSpan[..^CrawfordToken.Length];
        if (!TryParseInvariantInt(scoreSpan[..aIndex], allowSign: false, out int onRollAway)
            || !TryParseInvariantInt(opponentSpan, allowSign: false, out int opponentAway))
            return false;

        // ---- Cube: size digits + owner letter ----
        if (cubeSpan.Length < 2 || !TryOwnerFromLetter(cubeSpan[^1], out var cubeOwner)
            || !TryParseInvariantInt(cubeSpan[..^1], allowSign: false, out int cubeSize))
            return false;

        // ---- Dice: canonical DiceRoll token, present iff checker play ----
        DiceRoll? dice = null;
        if (hasDice)
        {
            if (!DiceRoll.TryParse(diceSpan, provider: null, out var roll))
                return false;
            dice = roll;
        }

        // ---- The same fact validation as TryDerive guards the parse door ----
        if (!AreValidFacts(
                board, onRollAway, opponentAway, isCrawford, cubeSize, cubeOwner, isJacoby))
            return false;

        // ---- One spelling per value: re-format and require ordinal identity.
        // This is the structural enforcement of the strict-parse contract —
        // "03", "+2", low-first dice, or any other variant spelling re-formats
        // differently and is rejected here.
        string canonical = FormatCanonical(
            board, onRollAway, opponentAway, isCrawford, cubeSize, cubeOwner, isJacoby, dice);
        if (!s.SequenceEqual(canonical))
            return false;

        result = new ProblemKey(canonical, isCubeDecision: !hasDice);
        return true;
    }

    /// <summary>
    /// Invariant integer parse for canonical tokens. <c>NumberStyles.Integer</c>
    /// would admit leading/trailing whitespace; these styles admit none.
    /// Variant spellings that still parse (an explicit <c>'+'</c>, leading
    /// zeros) are eliminated by the re-format identity check.
    /// </summary>
    private static bool TryParseInvariantInt(
        ReadOnlySpan<char> token, bool allowSign, out int value)
        => int.TryParse(
            token,
            allowSign ? NumberStyles.AllowLeadingSign : NumberStyles.None,
            CultureInfo.InvariantCulture,
            out value);

    // -----------------------------------------------------------------------
    //  Identity — equality, hashing and ordering over the canonical string
    // -----------------------------------------------------------------------

    /// <summary>The canonical string form (see the type remarks for the grammar).</summary>
    public override string ToString() => _canonical;

    /// <summary>
    /// Value equality: ordinal equality of the canonical strings — the
    /// string is the identity.
    /// </summary>
    public bool Equals(ProblemKey? other)
        => other is not null
           && string.Equals(_canonical, other._canonical, StringComparison.Ordinal);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ProblemKey other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(_canonical);

    /// <summary>Equality per <see cref="Equals(ProblemKey?)"/>; two nulls are equal.</summary>
    public static bool operator ==(ProblemKey? left, ProblemKey? right)
        => left is null ? right is null : left.Equals(right);

    /// <summary>Inequality per <see cref="operator ==(ProblemKey?, ProblemKey?)"/>.</summary>
    public static bool operator !=(ProblemKey? left, ProblemKey? right) => !(left == right);

    /// <summary>
    /// Ordinal comparison of the canonical strings — semantically arbitrary
    /// but stable, for deterministic (diffable) document writes. Any
    /// <see cref="ProblemKey"/> compares greater than <see langword="null"/>.
    /// </summary>
    /// <param name="other">The key to compare against.</param>
    /// <returns>Negative, zero, or positive per the standard contract.</returns>
    public int CompareTo(ProblemKey? other)
        => other is null ? 1 : string.CompareOrdinal(_canonical, other._canonical);

    int IComparable.CompareTo(object? obj) => obj switch
    {
        null => 1,
        ProblemKey other => CompareTo(other),
        _ => throw new ArgumentException(
            $"Object must be of type {nameof(ProblemKey)}.", nameof(obj)),
    };
}
