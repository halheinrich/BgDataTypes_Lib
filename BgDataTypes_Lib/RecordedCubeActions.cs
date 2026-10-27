using System.Globalization;

namespace BgDataTypes_Lib;

/// <summary>
/// A cube decision's recorded cube actions, as one value: the action each
/// half was played with, where the record states it, and otherwise the
/// analyser's error for that half. The doubler's half is double or no double
/// (<see cref="DoublerAction"/>, <see cref="UnstatedDoublerError"/>); the
/// taker's, only where a double was offered, is take or pass
/// (<see cref="TakerAction"/>, <see cref="UnstatedTakerError"/>). A record
/// holds it as <see cref="CubeDecisionData.Recorded"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The rules.</b> Each half's action is its own half's — the doubler's
/// <see cref="CubeAction.NoDouble"/> or <see cref="CubeAction.Double"/>, the
/// taker's <see cref="CubeAction.Take"/> or <see cref="CubeAction.Pass"/>, the
/// halves <see cref="CubeAnswerExtensions.Of"/> accepts. A half states its
/// action or the analyser's error for it, never both: a stated action's error
/// is derived from the equities (<see cref="CubeDecisionData.UserDoubleError"/>,
/// <see cref="CubeDecisionData.UserTakeError"/>), so stating it too would
/// store a copy. The analyser's error is a finite number (the rule stated on
/// the internal <c>FiniteNumber</c>) carried as stated, sign and all, as
/// <see cref="PlayerResult.Unstated"/> carries it. And <b>a taker half exists
/// only under a stated double</b>: a response or the analyser's take error
/// beside a doubler action that is not <see cref="CubeAction.Double"/> —
/// <see cref="CubeAction.NoDouble"/> or none stated — is invalid data, never a
/// decision (SPEC-scoring §3, Hal's ruling of 2026-10-25 recorded on
/// halheinrich/backgammon#440).
/// </para>
/// <para>
/// <b>Why one value.</b> The last rule spans both halves, and init-only
/// members are set one at a time, in any order, so no member can hold it
/// whatever order code or a document states them in. One constructor sees all
/// four at once, and every way a record gets its recorded actions goes through
/// it — code through this type, a document through the record's serializer
/// constructor — so neither can state a taker half without a stated double
/// (the design review on halheinrich/backgammon#440, agreed by Hal
/// 2026-10-26).
/// </para>
/// <para>
/// <b>Built by shape.</b> The constructor takes the four and refuses any
/// combination the rules forbid; the six factories name the six valid shapes
/// and build through it. The <c>default</c> value is <see cref="None"/>. A
/// value type: reading it allocates nothing.
/// </para>
/// </remarks>
public readonly struct RecordedCubeActions : IEquatable<RecordedCubeActions>
{
    /// <summary>The recorded cube actions with the four stated, once they keep the rules (see the remarks).</summary>
    /// <param name="doublerAction">The doubler's action: <see cref="CubeAction.NoDouble"/>, <see cref="CubeAction.Double"/>, or <see langword="null"/> when not recorded.</param>
    /// <param name="takerAction">The taker's response: <see cref="CubeAction.Take"/>, <see cref="CubeAction.Pass"/>, or <see langword="null"/> when not recorded.</param>
    /// <param name="unstatedDoublerError">The analyser's error for a doubler action not stated, or <see langword="null"/>.</param>
    /// <param name="unstatedTakerError">The analyser's error for a response not stated, or <see langword="null"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// An action is not its half's, or an error is NaN or an infinity.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// A half states both its action and the analyser's error for it, or a
    /// taker half is stated and <paramref name="doublerAction"/> is not
    /// <see cref="CubeAction.Double"/>.
    /// </exception>
    public RecordedCubeActions(
        CubeAction? doublerAction, CubeAction? takerAction, double? unstatedDoublerError, double? unstatedTakerError)
    {
        if (doublerAction is not (null or CubeAction.NoDouble or CubeAction.Double))
            throw new ArgumentOutOfRangeException(nameof(doublerAction), doublerAction,
                "The doubler's action is NoDouble or Double.");
        if (takerAction is not (null or CubeAction.Take or CubeAction.Pass))
            throw new ArgumentOutOfRangeException(nameof(takerAction), takerAction,
                "The taker's action is Take or Pass.");
        FiniteNumber.Check(unstatedDoublerError, nameof(unstatedDoublerError));
        FiniteNumber.Check(unstatedTakerError, nameof(unstatedTakerError));
        if (doublerAction is not null && unstatedDoublerError is not null)
            throw new ArgumentException(UnstatedErrorMessage("doubler"), nameof(unstatedDoublerError));
        if (takerAction is not null && unstatedTakerError is not null)
            throw new ArgumentException(UnstatedErrorMessage("taker"), nameof(unstatedTakerError));
        if (doublerAction is not CubeAction.Double && (takerAction is not null || unstatedTakerError is not null))
            throw new ArgumentException(
                $"A taker half — a response or the analyser's take error — exists only where a double was offered, so it needs a stated Double; the doubler's action is {doublerAction?.ToString() ?? "not stated"}.",
                takerAction is not null ? nameof(takerAction) : nameof(unstatedTakerError));

        DoublerAction = doublerAction;
        TakerAction = takerAction;
        UnstatedDoublerError = unstatedDoublerError;
        UnstatedTakerError = unstatedTakerError;
    }

    private static string UnstatedErrorMessage(string half) =>
        $"The analyser's {half} error stands for a {half} action the record does not state; a stated action's error is derived from the equities, so a half never states both.";

    /// <summary>
    /// The doubler action the player on roll played —
    /// <see cref="CubeAction.NoDouble"/> or <see cref="CubeAction.Double"/>;
    /// <see langword="null"/> when the record does not state it.
    /// </summary>
    public CubeAction? DoublerAction { get; }

    /// <summary>
    /// The opponent's response to the double — <see cref="CubeAction.Take"/>
    /// or <see cref="CubeAction.Pass"/>; <see langword="null"/> when not
    /// recorded, and always unless <see cref="DoublerAction"/> is
    /// <see cref="CubeAction.Double"/>.
    /// </summary>
    public CubeAction? TakerAction { get; }

    /// <summary>
    /// The analyser's error for a doubler action the record does not state;
    /// <see langword="null"/> when none was recorded, and always when
    /// <see cref="DoublerAction"/> is stated.
    /// </summary>
    public double? UnstatedDoublerError { get; }

    /// <summary>
    /// The analyser's error for a response the record does not state;
    /// <see langword="null"/> when none was recorded, always when
    /// <see cref="TakerAction"/> is stated, and always unless
    /// <see cref="DoublerAction"/> is <see cref="CubeAction.Double"/>.
    /// </summary>
    public double? UnstatedTakerError { get; }

    // -----------------------------------------------------------------------
    //  The six valid shapes
    // -----------------------------------------------------------------------

    /// <summary>Nothing recorded: no action stated and no analyser's error. The <c>default</c> value.</summary>
    public static RecordedCubeActions None => new(null, null, null, null);

    /// <summary>The player on roll did not double; no double was offered, so there is no taker half.</summary>
    public static RecordedCubeActions NoDouble => new(CubeAction.NoDouble, null, null, null);

    /// <summary>
    /// The player on roll doubled and the record holds no answer — neither a
    /// response nor the analyser's take error: a double the opponent answered
    /// by resigning (SPEC-filtering §8, Hal's ruling of 2026-10-26).
    /// </summary>
    public static RecordedCubeActions DoubleUnanswered => new(CubeAction.Double, null, null, null);

    /// <summary>The player on roll doubled, and the opponent's response is stated.</summary>
    /// <param name="takerAction">The response: <see cref="CubeAction.Take"/> or <see cref="CubeAction.Pass"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="takerAction"/> is not a response.</exception>
    public static RecordedCubeActions DoubleAnswered(CubeAction takerAction) => new(CubeAction.Double, takerAction, null, null);

    /// <summary>The player on roll doubled; the response is not stated, and the analyser's error for it stands.</summary>
    /// <param name="unstatedTakerError">The analyser's take error, as stated.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="unstatedTakerError"/> is NaN or an infinity.</exception>
    public static RecordedCubeActions DoubleWithUnstatedResponse(double unstatedTakerError) =>
        new(CubeAction.Double, null, null, unstatedTakerError);

    /// <summary>
    /// The doubler's action is not stated, and the analyser's error for it
    /// stands; with no stated double there is no taker half.
    /// </summary>
    /// <param name="unstatedDoublerError">The analyser's doubling error, as stated.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="unstatedDoublerError"/> is NaN or an infinity.</exception>
    public static RecordedCubeActions UnstatedDoubler(double unstatedDoublerError) =>
        new(null, null, unstatedDoublerError, null);

    // -----------------------------------------------------------------------
    //  Equality and diagnostics
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public bool Equals(RecordedCubeActions other) =>
        DoublerAction == other.DoublerAction
        && TakerAction == other.TakerAction
        && Nullable.Equals(UnstatedDoublerError, other.UnstatedDoublerError)
        && Nullable.Equals(UnstatedTakerError, other.UnstatedTakerError);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is RecordedCubeActions other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine(DoublerAction, TakerAction, UnstatedDoublerError, UnstatedTakerError);

    /// <summary>Whether two values record the same four facts.</summary>
    public static bool operator ==(RecordedCubeActions left, RecordedCubeActions right) => left.Equals(right);

    /// <summary>Whether two values differ in any of the four facts.</summary>
    public static bool operator !=(RecordedCubeActions left, RecordedCubeActions right) => !left.Equals(right);

    /// <summary>
    /// The shape, by its factory's name, with its argument where it has one,
    /// e.g. <c>"DoubleAnswered Take"</c>, <c>"UnstatedDoubler 0.042"</c>,
    /// <c>"None"</c>; for diagnostics.
    /// </summary>
    public override string ToString() => this switch
    {
        { DoublerAction: CubeAction.Double, TakerAction: CubeAction response } => $"{nameof(DoubleAnswered)} {response}",
        { DoublerAction: CubeAction.Double, UnstatedTakerError: double error } =>
            string.Create(CultureInfo.InvariantCulture, $"{nameof(DoubleWithUnstatedResponse)} {error}"),
        { DoublerAction: CubeAction.Double } => nameof(DoubleUnanswered),
        { DoublerAction: CubeAction.NoDouble } => nameof(NoDouble),
        { UnstatedDoublerError: double error } =>
            string.Create(CultureInfo.InvariantCulture, $"{nameof(UnstatedDoubler)} {error}"),
        _ => nameof(None),
    };
}
