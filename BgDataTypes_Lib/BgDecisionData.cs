using System.Text.Json;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// The decision record — one analysed backgammon decision as the ecosystem's
/// JSON wire unit. A decision is one of two types
/// (halheinrich/backgammon#273, Hal's ruling of 2026-09-25):
/// <see cref="CheckerPlayDecision"/> and <see cref="CubeDecision"/>, each
/// carrying only its own fields, so code that reads one kind's field off the
/// other does not compile and no member holds a value standing for "not
/// applicable". This base holds what every decision has — its
/// <see cref="Kind"/>, <see cref="Id"/>, <see cref="Xgid"/>, the
/// <see cref="Position"/> it was made in and the <see cref="Descriptive"/>
/// provenance — and each kind adds its own <c>Decision</c> category.
///
/// <para>
/// <b>A closed pair.</b> The constructor is not reachable outside this
/// library, so these two kinds are the only ones there are, and
/// <see cref="Match{TResult}"/> and <see cref="Switch"/> take one branch per
/// kind: a consumer's match over the kind has no silent fall-through, and a
/// third kind would break every match at compile time. Reading a kind's own
/// members is a pattern match
/// (<c>record is CubeDecision cube</c>) or a <see cref="Match{TResult}"/>.
/// </para>
///
/// <para>
/// <b>On the wire</b> the kind is a real member, <c>"Kind"</c>, written first
/// whatever static type the value is serialized as, and read wherever it sits;
/// <see cref="BgDecisionDataJsonConverter"/> finds it, refuses a document
/// without exactly one known kind, and hands the whole document to that
/// kind's generated contract. Every refusal — a member of the other kind, a
/// missing member, a construction rule broken — is a
/// <see cref="JsonException"/>, whether the record is read as
/// <see cref="BgDecisionData"/> or as its own kind. Every stored member is
/// <c>required</c> or nullable, per the wire rule stated on
/// <see cref="BgDataTypesJsonContext"/>; <see cref="Kind"/> is required
/// through <see cref="JsonRequiredAttribute"/>, since the type states it and
/// code never does.
/// </para>
///
/// <para>
/// <b>The members agree by construction.</b> Each init setter checks its value
/// against the members already set and against the kind, which the type
/// fixes before any member is set, so a record breaking a rule of
/// <see cref="DecisionRules"/> cannot be built — from an object initializer in
/// any member order (an <see cref="ArgumentException"/> naming the member that
/// completed the contradiction) or from JSON (a <see cref="JsonException"/>
/// carrying that exception: each kind's serializer constructor marks the
/// record as read before any member is set, as the wire rule on
/// <see cref="BgDataTypesJsonContext"/> states):
/// </para>
/// <list type="bullet">
/// <item><description>a cube decision is never made in the Crawford game
/// (halheinrich/backgammon#201) — <see cref="Position"/> refuses it;</description></item>
/// <item><description>an <see cref="XgDecisionId"/> names this record's own kind
/// — <see cref="Id"/> refuses the other;</description></item>
/// <item><description>a standalone position states no
/// <see cref="DescriptiveData.IsStandardStart"/>, and a decision in a game
/// does (halheinrich/backgammon#124) — whichever of <see cref="Id"/> and
/// <see cref="Descriptive"/> is set second refuses a mismatch;</description></item>
/// <item><description>a checker play's candidates are all valid from its
/// position — see <see cref="CheckerPlayDecision"/>.</description></item>
/// </list>
/// <para>
/// <see cref="ProblemKey"/>'s grammar still accepts a Crawford cube key,
/// because stats documents written before the Crawford guard hold such keys
/// and must keep loading; those keys are inert rather than orphaned — stats
/// are looked up per pooled problem (<c>BgGame_Lib</c>'s
/// <c>MixedProblemSetSource</c>), and nothing but the document writer's
/// ordering walks the whole document.
/// </para>
///
/// <para>
/// <b>The filter view is built for a ranking.</b> The members every kind
/// has are forwarded publicly and depend on no ranking. The
/// <see cref="IDecisionFilterData"/> view also says which play is best and
/// what the player's error is, which a ranking decides (SPEC-scoring §2a,
/// halheinrich/backgammon#282), so the record is not itself a view:
/// <see cref="ViewFor"/> builds one for a <see cref="PlayRanking"/>. Every
/// derived member is excluded from JSON — the stored members are the wire
/// form (halheinrich/backgammon#14).
/// </para>
/// </summary>
[JsonConverter(typeof(BgDecisionDataJsonConverter))]
public abstract class BgDecisionData
{
    private readonly DecisionKind _kind;

    // True for a record being read from a document: set only by the
    // serializer's constructor, before any member is set, so each rule below
    // refuses a breach as a JsonException rather than the guard's own
    // ArgumentException (DocumentRefusal).
    private readonly bool _read;

    // Null only while construction is still setting the member; `required`
    // guarantees each is set by the time it ends, and each init setter
    // rejects an explicit null (halheinrich/backgammon#221). The guards below
    // read an unset member as "nothing to agree with yet".
    private readonly DecisionId? _id;
    private readonly string? _xgid;
    private readonly PositionData? _position;
    private readonly DescriptiveData? _descriptive;

    /// <summary>
    /// The constructor code builds a record through, reachable only from the
    /// two kinds in this library: the kind is fixed here, before any member is
    /// set, so every init guard can read it.
    /// </summary>
    private protected BgDecisionData(DecisionKind kind) => _kind = kind;

    /// <summary>
    /// The constructor a document is read through, reachable only from each
    /// kind's serializer constructor: the kind is fixed as for code, the
    /// record is marked as read, and the kind the document states is held to
    /// it — all before any other member is set.
    /// </summary>
    /// <exception cref="JsonException"><paramref name="statedKind"/> is not <paramref name="kind"/>.</exception>
    private protected BgDecisionData(DecisionKind kind, DecisionKind statedKind)
    {
        _kind = kind;
        _read = true;
        Kind = statedKind;
    }

    /// <summary>
    /// Whether this record is being read from a document — for a kind's own
    /// rules, which refuse a breach as the base's do.
    /// </summary>
    private protected bool IsRead => _read;

    /// <summary>
    /// The decision's kind — the value form of its type: every
    /// <see cref="CheckerPlayDecision"/> is <see cref="DecisionKind.CheckerPlay"/>
    /// and every <see cref="CubeDecision"/> is <see cref="DecisionKind.Cube"/>.
    /// A real wire member, written first; code never sets it, and a document
    /// may only state the record's own kind.
    /// </summary>
    /// <exception cref="JsonException">
    /// Thrown on read when the document states the other kind — reachable
    /// only from JSON: the serializer passes the stated kind to the kind's
    /// serializer constructor, which sets it here.
    /// </exception>
    [JsonInclude, JsonRequired, JsonPropertyOrder(-5)]
    public DecisionKind Kind
    {
        get => _kind;
        internal init
        {
            if (value != _kind)
                throw new JsonException(
                    $"The document states Kind {value} for a {_kind} decision.");
        }
    }

    /// <summary>
    /// Stable, persistent identifier for this decision within its source file.
    /// Producer-supplied at the build site (see <c>ConvertXgToJson_Lib</c>) —
    /// required so that uninitialized cases surface at construction rather than
    /// later as silent null reads. The one stored place of the decision's game
    /// and move number (<see cref="Game"/>, <see cref="MoveNumber"/>).
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown on init when the value is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown on init when the value is an <see cref="XgDecisionId"/> naming
    /// the other kind, or when <see cref="Descriptive"/> is already set and its
    /// <see cref="DescriptiveData.IsStandardStart"/> disagrees with the value
    /// (<see cref="DecisionRules"/>).
    /// </exception>
    [JsonPropertyOrder(-4)]
    public required DecisionId Id
    {
        get => _id!;
        init
        {
            try
            {
                ArgumentNullException.ThrowIfNull(value, nameof(Id));
                if (!DecisionRules.IdAgrees(value, _kind))
                    throw new ArgumentException(DecisionRules.IdKindMessage, nameof(Id));
                if (_descriptive is not null && !DecisionRules.StartAgrees(value, _descriptive.IsStandardStart))
                    throw new ArgumentException(DecisionRules.StartMessage, nameof(Id));
            }
            catch (ArgumentException fault) when (_read)
            {
                throw DocumentRefusal.Of(fault);
            }
            _id = value;
        }
    }

    /// <summary>
    /// XGID position string. Lives at the top level rather than inside
    /// <see cref="Position"/> because it is a digest of the whole decision
    /// context (position, cube/match state, and the decision itself), not a
    /// property of the minimal derived <see cref="PositionData"/>. Mirrors
    /// <see cref="DecisionRow.Xgid"/>. Source data, not a copy: it carries the
    /// cube limit, the beaver rule and a money game's header scores, which no
    /// other member holds (INSTRUCTIONS.md, "Stored or derived"). Every
    /// decision has one, so it is never null or empty text.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown on init when the value is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown on init when the value is empty or white space.</exception>
    [JsonPropertyOrder(-3)]
    public required string Xgid
    {
        get => _xgid!;
        init
        {
            try
            {
                ArgumentNullException.ThrowIfNull(value, nameof(Xgid));
                StatedText.Check(value, nameof(Xgid));
            }
            catch (ArgumentException fault) when (_read)
            {
                throw DocumentRefusal.Of(fault);
            }
            _xgid = value;
        }
    }

    /// <summary>
    /// Board, score context and cube state at the moment of the decision.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown on init when the value is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown on init when this is a cube decision and the position is
    /// Crawford (<see cref="DecisionRules.CrawfordMessage"/>), or when this is
    /// a checker play whose candidates are already set and one of them is
    /// invalid from the position (see <see cref="CheckerPlayDecision"/>).
    /// </exception>
    [JsonPropertyOrder(-2)]
    public required PositionData Position
    {
        get => _position!;
        init
        {
            try
            {
                ArgumentNullException.ThrowIfNull(value, nameof(Position));
                if (!DecisionRules.CrawfordAllows(_kind, value.IsCrawford))
                    throw new ArgumentException(DecisionRules.CrawfordMessage, nameof(Position));
                PositionStated(value);
            }
            catch (ArgumentException fault) when (_read)
            {
                throw DocumentRefusal.Of(fault);
            }
            _position = value;
        }
    }

    /// <summary>Provenance and metadata: players, source file, the match and its start.</summary>
    /// <exception cref="ArgumentNullException">Thrown on init when the value is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown on init when <see cref="Id"/> is already set and the value's
    /// <see cref="DescriptiveData.IsStandardStart"/> disagrees with it
    /// (<see cref="DecisionRules"/>).
    /// </exception>
    [JsonPropertyOrder(-1)]
    public required DescriptiveData Descriptive
    {
        get => _descriptive!;
        init
        {
            try
            {
                ArgumentNullException.ThrowIfNull(value, nameof(Descriptive));
                if (_id is not null && !DecisionRules.StartAgrees(_id, value.IsStandardStart))
                    throw new ArgumentException(DecisionRules.StartMessage, nameof(Descriptive));
            }
            catch (ArgumentException fault) when (_read)
            {
                throw DocumentRefusal.Of(fault);
            }
            _descriptive = value;
        }
    }

    /// <summary>
    /// The position as stated so far, for a kind whose own members must agree
    /// with it: <see langword="null"/> until <see cref="Position"/> is set.
    /// </summary>
    private protected PositionData? StatedPosition => _position;

    /// <summary>
    /// Called from <see cref="Position"/>'s init with the incoming position,
    /// before it is stored, so a kind can hold its own already-set members to
    /// it and refuse by throwing. The base's own rules have passed.
    /// </summary>
    private protected abstract void PositionStated(PositionData position);

    // -----------------------------------------------------------------------
    //  The kind, exhaustively
    // -----------------------------------------------------------------------

    /// <summary>
    /// The result of the branch for this record's kind — one branch per kind,
    /// so a match has no fall-through and a new kind breaks every call at
    /// compile time.
    /// </summary>
    /// <typeparam name="TResult">The type both branches return.</typeparam>
    /// <param name="checkerPlay">The branch for a <see cref="CheckerPlayDecision"/>.</param>
    /// <param name="cube">The branch for a <see cref="CubeDecision"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when either branch is <see langword="null"/>.</exception>
    public abstract TResult Match<TResult>(
        Func<CheckerPlayDecision, TResult> checkerPlay, Func<CubeDecision, TResult> cube);

    /// <summary>
    /// Runs the branch for this record's kind — the statement form of
    /// <see cref="Match{TResult}"/>, with the same guarantee.
    /// </summary>
    /// <param name="checkerPlay">The branch for a <see cref="CheckerPlayDecision"/>.</param>
    /// <param name="cube">The branch for a <see cref="CubeDecision"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when either branch is <see langword="null"/>.</exception>
    public abstract void Switch(Action<CheckerPlayDecision> checkerPlay, Action<CubeDecision> cube);

    // -----------------------------------------------------------------------
    //  Where the decision sits (halheinrich/backgammon#124)
    // -----------------------------------------------------------------------

    /// <summary>
    /// The 1-based number of the game this decision was played in, within
    /// its source file; <see langword="null"/> for a decision in a standalone
    /// position (an <c>.xgp</c> file), which belongs to no game — not a
    /// stamped 1 (halheinrich/backgammon#124). Derived from
    /// <see cref="Id"/>, the one place it is stored
    /// (<see cref="XgDecisionId.Game"/>), so it cannot disagree with the
    /// identifier.
    /// </summary>
    [JsonIgnore]
    public int? Game => Id.GameInFile;

    /// <summary>
    /// The file the decision came from — its bare name with extension, no
    /// directory (e.g. <c>"match.xg"</c>, <c>"session.xgp"</c>): the
    /// <see cref="Id"/>'s <see cref="DecisionId.Filename"/>, the one place it
    /// is stored, so the two cannot disagree. Every decision has one.
    /// </summary>
    [JsonIgnore]
    public string SourceFile => Id.Filename;

    // -----------------------------------------------------------------------
    //  What every decision has, forwarded — and the filter view, for a ranking
    //
    //  Derived members, not wire data: each forwards into (or derives from)
    //  the stored members, which are the JSON wire form, so each carries
    //  [JsonIgnore] (halheinrich/backgammon#14). None of them depends on a
    //  ranking. The filter view's members that say best or error do, so the
    //  record is not itself an IDecisionFilterData: ViewFor builds the view
    //  for one ranking (SPEC-scoring §2a).
    // -----------------------------------------------------------------------

    /// <inheritdoc cref="IDecisionFilterData.Player"/>
    [JsonIgnore]
    public string? Player => Descriptive.OnRollName;
    /// <inheritdoc cref="IDecisionFilterData.OnRollNeeds"/>
    [JsonIgnore]
    public int OnRollNeeds => Position.OnRollNeeds;
    /// <inheritdoc cref="IDecisionFilterData.OpponentNeeds"/>
    [JsonIgnore]
    public int OpponentNeeds => Position.OpponentNeeds;
    /// <inheritdoc cref="IDecisionFilterData.IsCrawford"/>
    [JsonIgnore]
    public bool IsCrawford => Position.IsCrawford;
    /// <inheritdoc cref="IDecisionFilterData.IsJacoby"/>
    [JsonIgnore]
    public bool? IsJacoby => Position.IsJacoby;
    /// <inheritdoc cref="IDecisionFilterData.MatchLength"/>
    [JsonIgnore]
    public int MatchLength => Descriptive.MatchLength;
    /// <summary>
    /// True for an unlimited (money) session: <see cref="IDecisionFilterData.IsMoneyGame"/>'s
    /// rule, redeclared concretely as <see cref="DecisionRow.IsMoneyGame"/> is,
    /// now that the record reaches the filter view through
    /// <see cref="ViewFor"/> rather than being one.
    /// </summary>
    [JsonIgnore]
    public bool IsMoneyGame => MatchLength == 0;
    /// <inheritdoc cref="IDecisionFilterData.MoveNumber"/>
    /// <remarks>
    /// Derived from <see cref="Id"/>, as <see cref="Game"/> is
    /// (<see cref="XgDecisionId.MoveNumber"/>).
    /// </remarks>
    [JsonIgnore]
    public int? MoveNumber => Id.MoveInGame;
    /// <inheritdoc cref="IDecisionFilterData.IsStandardStart"/>
    [JsonIgnore]
    public bool? IsStandardStart => Descriptive.IsStandardStart;
    /// <inheritdoc cref="IDecisionFilterData.Board"/>
    [JsonIgnore]
    public BoardPosition Board => Position.Mop;

    /// <summary>
    /// This decision as the filter layer reads it, for
    /// <paramref name="ranking"/>: the members that say best or error — the
    /// player's error, the best analysis's mode and level, the board the best
    /// play leaves — are the ranking's, and every other member is the
    /// record's. A cube decision's members do not depend on the ranking. Its
    /// values are derived once, when the view is built; reading them
    /// allocates nothing.
    /// </summary>
    /// <param name="ranking">The ranking; <see cref="PlayRanking.Equity"/> is the default an app without the setting uses.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="ranking"/> is not a defined ranking.</exception>
    public IDecisionFilterData ViewFor(PlayRanking ranking)
    {
        if (!Enum.IsDefined(ranking))
            throw new ArgumentOutOfRangeException(nameof(ranking), ranking, "Not a defined play ranking.");
        return new DecisionView(this, ranking);
    }
}
