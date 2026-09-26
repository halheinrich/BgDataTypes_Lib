using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// Stable, persistent identifier for a single decision within an XG-family
/// source file.
///
/// <para>
/// Two concrete shapes:
/// <list type="bullet">
///   <item><see cref="XgpDecisionId"/> — a decision from an <c>.xgp</c> position
///     file is identified by the file's bare filename. The producing iterator
///     guarantees at most one decision per <c>.xgp</c>, which is what makes the
///     bare filename a unique key.</item>
///   <item><see cref="XgDecisionId"/> — a decision inside a multi-game
///     <c>.xg</c> file is identified by the tuple
///     <c>(Filename, Game, MoveNumber, IsCube)</c>.
///     <c>IsCube</c> disambiguates the cube-decision row from the checker-play
///     row that XG emits for the same <c>MoveNumber</c>.</item>
/// </list>
/// </para>
///
/// <para>
/// Canonical string form:
/// <list type="bullet">
///   <item><c>"file.xgp"</c> — bare filename for the <c>.xgp</c> shape.</item>
///   <item><c>"file.xg:g{Game}:m{MoveNumber}:{cube|play}"</c> — colon-separated
///     tuple for the <c>.xg</c> shape.</item>
/// </list>
/// </para>
///
/// <para>
/// <b>Filename invariant.</b> The colon character <c>':'</c> is the canonical
/// separator and is forbidden in <see cref="Filename"/> on both subtypes.
/// Without the guard, the parse dispatcher (which discriminates the two shapes
/// by the presence of <c>':'</c>) would not round-trip an <c>.xgp</c>
/// filename that contained a colon. Both subtype constructors reject such
/// inputs with <see cref="ArgumentException"/>; <see cref="TryParse(string, IFormatProvider, out DecisionId)"/>
/// returns <see langword="false"/> instead. Windows filesystem names cannot
/// contain <c>':'</c> in practice, so the guard is a defensive invariant
/// rather than a real restriction on observable filenames.
/// </para>
///
/// <para>
/// Filename equality is case-sensitive (record-default semantics on
/// <see cref="string"/>). The bare filename (with extension, no directory)
/// is stored, and nowhere else: a record's and a row's <c>SourceFile</c> is
/// derived from it (<see cref="BgDecisionData.SourceFile"/>,
/// <see cref="DecisionRow.SourceFile"/>). No full path is retained anywhere.
/// </para>
///
/// <para>
/// Implements both <see cref="IParsable{TSelf}"/> and
/// <see cref="ISpanParsable{TSelf}"/>; the span overloads are the primary
/// implementation, and the string overloads delegate to them for parity.
/// </para>
///
/// <para>
/// JSON shape: round-trips as the canonical string via
/// <see cref="DecisionIdJsonConverter"/>, bundled via type-level
/// <c>[JsonConverter]</c> attribute — consumers do not need to register the
/// converter on their <see cref="System.Text.Json.JsonSerializerOptions"/>.
/// </para>
/// </summary>
[JsonConverter(typeof(DecisionIdJsonConverter))]
public abstract record DecisionId : IParsable<DecisionId>, ISpanParsable<DecisionId>
{
    /// <summary>
    /// Bare filename (no directory component). Must not contain
    /// the canonical-form separator <c>':'</c> — see the
    /// "Filename invariant" remark on <see cref="DecisionId"/> — and is never
    /// empty or white space: every decision comes from a file, and a record's
    /// <c>SourceFile</c> is this name.
    /// </summary>
    public abstract string Filename { get; init; }

    /// <summary>
    /// Internal seam for derived records to enforce the
    /// <see cref="Filename"/> invariant — text, and no <c>':'</c> — inside
    /// their property initializers. Returns the input unchanged on success;
    /// throws on null, empty or white space, or a colon.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="filename"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="filename"/> is empty or white space, or
    /// contains <c>':'</c>.
    /// </exception>
    private protected static string ValidateFilename(string filename)
    {
        ArgumentNullException.ThrowIfNull(filename);
        if (FilenameFault(filename) is { } fault)
            throw new ArgumentException(fault, nameof(filename));
        return filename;
    }

    /// <summary>
    /// The way <paramref name="filename"/> breaks the invariant, or
    /// <see langword="null"/> when it keeps it — shared by the constructors,
    /// which throw it, and <see cref="TryParse(ReadOnlySpan{char}, IFormatProvider, out DecisionId)"/>,
    /// which returns <see langword="false"/> instead.
    /// </summary>
    private static string? FilenameFault(ReadOnlySpan<char> filename) =>
        filename.IsWhiteSpace()
            ? "DecisionId.Filename is a file's name; empty or white-space text is none."
            : filename.Contains(':')
                ? $"DecisionId.Filename must not contain ':' (got '{filename}')."
                : null;

    // -----------------------------------------------------------------------
    //  Where the decision sits in its file (halheinrich/backgammon#124)
    //
    //  The identifier is the one stored place of a decision's game and move
    //  number: an XgDecisionId carries both, and an XgpDecisionId — a
    //  standalone position, which belongs to no game — carries neither. The
    //  records derive their Game and MoveNumber from here and store no copy,
    //  so a number cannot disagree with the identifier or be stamped on a
    //  standalone position.
    // -----------------------------------------------------------------------

    /// <summary>
    /// The 1-based number of the game the decision was played in, or
    /// <see langword="null"/> for a decision in a standalone position.
    /// </summary>
    internal int? GameInFile => this is XgDecisionId xg ? xg.Game : null;

    /// <summary>
    /// The 1-based number of the decision's move within its game, or
    /// <see langword="null"/> for a decision in a standalone position.
    /// </summary>
    internal int? MoveInGame => this is XgDecisionId xg ? xg.MoveNumber : null;

    // -----------------------------------------------------------------------
    //  Parse / TryParse — span overloads are the primary implementation;
    //  string overloads delegate to them for parity.
    // -----------------------------------------------------------------------

    /// <summary>Parses the canonical string form of a <see cref="DecisionId"/>.</summary>
    /// <param name="s">The canonical string form (see <see cref="DecisionId"/>).</param>
    /// <param name="provider">Ignored; the canonical form is culture-invariant.</param>
    /// <returns>The parsed identifier.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="s"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="s"/> is not a valid canonical form.
    /// </exception>
    public static DecisionId Parse(string s, IFormatProvider? provider = null)
    {
        ArgumentNullException.ThrowIfNull(s);
        return Parse(s.AsSpan(), provider);
    }

    /// <summary>Attempts to parse the canonical string form of a <see cref="DecisionId"/>.</summary>
    /// <param name="s">The canonical string form, or <see langword="null"/>.</param>
    /// <param name="provider">Ignored; the canonical form is culture-invariant.</param>
    /// <param name="result">
    /// On success, the parsed identifier; on failure, <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> on success; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out DecisionId result)
    {
        if (s is null)
        {
            result = null;
            return false;
        }
        return TryParse(s.AsSpan(), provider, out result);
    }

    /// <inheritdoc cref="Parse(string, IFormatProvider)"/>
    public static DecisionId Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null)
    {
        if (!TryParse(s, provider, out var result))
            throw new FormatException($"Invalid DecisionId canonical form: '{s}'.");
        return result;
    }

    /// <inheritdoc cref="TryParse(string, IFormatProvider, out DecisionId)"/>
    public static bool TryParse(
        ReadOnlySpan<char> s,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out DecisionId result)
    {
        result = null;

        if (s.IsEmpty)
            return false;

        int firstColon = s.IndexOf(':');
        if (firstColon < 0)
        {
            // No ':' → Xgp shape: bare filename, which must still be a name.
            if (FilenameFault(s) is not null)
                return false;
            result = new XgpDecisionId(s.ToString());
            return true;
        }

        // Has ':' → Xg shape: <filename>:g<Game>:m<MoveNumber>:<cube|play>
        var filename = s[..firstColon];
        if (FilenameFault(filename) is not null)
            return false;

        var rest = s[(firstColon + 1)..];

        int secondColon = rest.IndexOf(':');
        if (secondColon < 0)
            return false;
        var gamePart = rest[..secondColon];
        rest = rest[(secondColon + 1)..];

        int thirdColon = rest.IndexOf(':');
        if (thirdColon < 0)
            return false;
        var movePart = rest[..thirdColon];
        var kindPart = rest[(thirdColon + 1)..];

        // Trailing junk (a fourth ':') invalidates the input.
        if (kindPart.IndexOf(':') >= 0)
            return false;

        if (!TryParsePrefixedInt(gamePart, 'g', out int game))
            return false;
        if (!TryParsePrefixedInt(movePart, 'm', out int moveNumber))
            return false;

        bool isCube;
        if (kindPart.SequenceEqual("cube"))
            isCube = true;
        else if (kindPart.SequenceEqual("play"))
            isCube = false;
        else
            return false;

        // Filename is the slice before the first ':' — by construction has no
        // ':'. The XgDecisionId ctor's ValidateFilename call therefore cannot
        // throw here, but the construction goes through the public ctor for a
        // single source of truth on the invariant.
        result = new XgDecisionId(filename.ToString(), game, moveNumber, isCube);
        return true;
    }

    private static bool TryParsePrefixedInt(ReadOnlySpan<char> token, char prefix, out int value)
    {
        if (token.Length < 2 || token[0] != prefix)
        {
            value = 0;
            return false;
        }
        return int.TryParse(token[1..], NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }
}

/// <summary>
/// Identifier for a decision in an <c>.xgp</c> position file. Identity is the
/// bare filename; <see cref="DecisionId.ToString"/> returns the filename
/// unchanged.
///
/// <para>
/// <b>Why the bare filename is a unique key.</b> Not because XG writes one
/// decision per <c>.xgp</c> — it does not. XG always writes a cube pane
/// alongside the move pane, and a position saved after the dice were rolled
/// can carry analysis in both. The key holds because the producing iterator's
/// emission policy selects at most one decision per <c>.xgp</c>: the analysed
/// checker play if there is one, otherwise the analysed cube. That is a
/// producer contract this identifier depends on, not a property of the file
/// format.
/// </para>
/// </summary>
public sealed record XgpDecisionId(string Filename) : DecisionId
{
    /// <inheritdoc/>
    public override string Filename { get; init; } = ValidateFilename(Filename);

    /// <inheritdoc/>
    public override string ToString() => Filename;
}

/// <summary>
/// Identifier for a decision inside a multi-game <c>.xg</c> file. Identity is
/// the tuple <c>(Filename, Game, MoveNumber, IsCube)</c>; <c>IsCube</c>
/// distinguishes the cube-decision row from the checker-play row XG emits
/// for the same <see cref="MoveNumber"/>.
/// </summary>
/// <param name="Filename">
/// Bare filename of the source <c>.xg</c> file (no directory). Must not
/// contain <c>':'</c> — see the "Filename invariant" remark on
/// <see cref="DecisionId"/>.
/// </param>
/// <param name="Game">1-based game number within the match.</param>
/// <param name="MoveNumber">1-based move number within the game.</param>
/// <param name="IsCube">
/// <see langword="true"/> for the cube-decision row at <paramref name="MoveNumber"/>;
/// <see langword="false"/> for the checker-play row.
/// </param>
public sealed record XgDecisionId(
    string Filename,
    int Game,
    int MoveNumber,
    bool IsCube) : DecisionId
{
    /// <inheritdoc/>
    public override string Filename { get; init; } = ValidateFilename(Filename);

    /// <summary>
    /// Canonical string form: <c>"{Filename}:g{Game}:m{MoveNumber}:{cube|play}"</c>.
    /// </summary>
    public override string ToString() =>
        $"{Filename}:g{Game}:m{MoveNumber}:{(IsCube ? "cube" : "play")}";
}
