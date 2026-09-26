using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// A checker-play decision: the dice are rolled and a play is chosen. One of
/// the two kinds of <see cref="BgDecisionData"/>; it carries a checker
/// play's fields and nothing else — see <see cref="CheckerPlayDecisionData"/>
/// for its <see cref="Decision"/> category.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every candidate is valid from the decision's position.</b> A record
/// holding a play that cannot be played from its own position is corrupt
/// (Hal's ruling on invalid plays, halheinrich/backgammon#273, applied to
/// stored candidates), so the record cannot be built with one. The rule is
/// stated once, beside the play rule it relies on, on
/// <see cref="BoardState.IsSamePlay"/>. Whichever of <see cref="BgDecisionData.Position"/>
/// and <see cref="Decision"/> is set second checks every candidate and refuses
/// the first invalid one with an <see cref="ArgumentException"/> naming it and
/// the fault; read from JSON, as <see cref="BgDecisionData"/> or as this type,
/// it is a <see cref="System.Text.Json.JsonException"/> carrying that
/// exception, on both paths.
/// </para>
/// <para>
/// <b>The after-boards are derived, never stored</b> (the arc's rule: no
/// stored copy of a derivable value). Every candidate's board — the board its
/// play leaves, through the one play rule (<see cref="BoardState.ApplyPlay"/>'s)
/// — is computed once while the record is built, in the same pass that
/// checks the candidates, and read thereafter at no cost:
/// <see cref="AfterBoardOf"/> for any candidate, <see cref="AfterPlayerBoard"/>
/// for the user's, and <see cref="AfterBoardOfBest"/> for the best play under a
/// ranking — which play is best is a ranking's (<see cref="PlayRanking"/>).
/// They are not on the wire, and a derivation can never fail on a record
/// that exists.
/// </para>
/// </remarks>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CheckerPlayDecision : BgDecisionData
{
    // Null only while construction is still setting it (see BgDecisionData).
    private readonly CheckerPlayDecisionData? _decision;

    // Derived while the record is built — by whichever of Position and
    // Decision is set second — and never written after construction ends:
    // the board each candidate leaves, in Plays order.
    private BoardPosition[] _afterBoards = [];

    /// <summary>Creates a checker-play decision; its members are set by the initializer.</summary>
    public CheckerPlayDecision() : base(DecisionKind.CheckerPlay)
    {
    }

    /// <summary>
    /// The serializer's constructor. It binds the document's
    /// <paramref name="kind"/>, which must be <see cref="DecisionKind.CheckerPlay"/>;
    /// why the pattern exists is stated once, on
    /// <see cref="BgDataTypesJsonContext"/> ("The serializer constructors").
    /// </summary>
    [JsonConstructor]
    internal CheckerPlayDecision(DecisionKind kind) : base(DecisionKind.CheckerPlay, kind)
    {
    }

    /// <summary>
    /// The roll, the analysed candidates, and which of them was played — see
    /// <see cref="CheckerPlayDecisionData"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown on init when the value is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown on init when <see cref="BgDecisionData.Position"/> is already
    /// set and a candidate is invalid from it (see the class remarks).
    /// </exception>
    public required CheckerPlayDecisionData Decision
    {
        get => _decision!;
        init
        {
            try
            {
                ArgumentNullException.ThrowIfNull(value, nameof(Decision));
                if (StatedPosition is { } position)
                    Derive(position, value, nameof(Decision));
            }
            catch (ArgumentException fault) when (IsRead)
            {
                throw DocumentRefusal.Of(fault);
            }
            _decision = value;
        }
    }

    /// <summary>
    /// The roll in canonical unordered form (<see cref="IDecisionFilterData.Dice"/>):
    /// the two faces of <see cref="CheckerPlayDecisionData.Dice"/>, which hold
    /// the rolled order, canonicalized by <see cref="DiceRoll"/>.
    /// </summary>
    [JsonIgnore]
    public DiceRoll Dice => new(Decision.Dice[0], Decision.Dice[1]);

    /// <summary>
    /// The board the candidate at <paramref name="index"/> leaves.
    /// <b>Frame: the next mover's</b> — the position the play reaches, flipped
    /// as <see cref="BoardState.ApplyPlay"/> leaves it: the opponent is on
    /// roll, so slot 25 is the opponent's bar and their checkers are positive,
    /// while the decision-maker's checkers are negative. Derived once, read at
    /// no cost.
    /// </summary>
    /// <param name="index">A candidate's index into <see cref="CheckerPlayDecisionData.Plays"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> identifies no candidate.</exception>
    public BoardPosition AfterBoardOf(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _afterBoards.Length);
        return _afterBoards[index];
    }

    /// <summary>
    /// The board the best play under <paramref name="ranking"/> leaves
    /// (<see cref="RankedPlays.Best"/>), in the frame of <see cref="AfterBoardOf"/>.
    /// Always exists. No allocation once the ranking has been asked for.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="ranking"/> is not a defined ranking.</exception>
    public BoardPosition AfterBoardOfBest(PlayRanking ranking) =>
        _afterBoards[Decision.RankedBy(ranking).Best.Index];

    /// <summary>
    /// The board the user's play leaves (<see cref="CheckerPlayDecisionData.UserPlay"/>),
    /// in the frame of <see cref="AfterBoardOf"/>; <see langword="null"/>
    /// exactly when the user's play is not among the candidates. No ranking
    /// decides which play the user made.
    /// </summary>
    [JsonIgnore]
    public BoardPosition? AfterPlayerBoard =>
        Decision.UserPlayIndex is int index ? _afterBoards[index] : null;

    /// <inheritdoc/>
    public override TResult Match<TResult>(
        Func<CheckerPlayDecision, TResult> checkerPlay, Func<CubeDecision, TResult> cube)
    {
        ArgumentNullException.ThrowIfNull(checkerPlay);
        ArgumentNullException.ThrowIfNull(cube);
        return checkerPlay(this);
    }

    /// <inheritdoc/>
    public override void Switch(Action<CheckerPlayDecision> checkerPlay, Action<CubeDecision> cube)
    {
        ArgumentNullException.ThrowIfNull(checkerPlay);
        ArgumentNullException.ThrowIfNull(cube);
        checkerPlay(this);
    }

    /// <inheritdoc/>
    private protected override void PositionStated(PositionData position)
    {
        if (_decision is not null)
            Derive(position, _decision, nameof(Position));
    }

    /// <summary>
    /// Holds every candidate to the play rule from <paramref name="position"/>
    /// and derives the board each leaves — one pass, through
    /// <see cref="BoardState.PositionAfter"/>, which refuses an invalid
    /// candidate naming <paramref name="paramName"/>.
    /// </summary>
    private void Derive(PositionData position, CheckerPlayDecisionData decision, string paramName)
    {
        var plays = decision.Plays;
        var boards = new BoardPosition[plays.Count];
        for (int i = 0; i < plays.Count; i++)
            boards[i] = BoardState.PositionAfter(position.Mop, plays[i].Play, $"Candidate {i}'s play", paramName);
        _afterBoards = boards;
    }
}
