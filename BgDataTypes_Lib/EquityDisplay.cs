using System.Globalization;

namespace BgDataTypes_Lib;

/// <summary>
/// How an equity figure is shown, and when a loss counts as zero: the one
/// owner of the display precision and of the rule read from it
/// (halheinrich/backgammon#202, folded into halheinrich/backgammon#326 by Hal
/// on 2026-10-01; SPEC-scoring §2a and §3). It shows two kinds of figure at
/// the one precision: an equity loss (<see cref="FormatLoss"/>) and an equity
/// with its sign (<see cref="FormatEquity"/>). An equity loss is any cost or
/// error measured in equity: a cube answer's cost or either of its parts
/// (<see cref="CubeAnswerCost"/>), a checker play's error
/// (<see cref="RankedPlay.Error"/>), a cube action's error
/// (<see cref="CubeDecisionData.DoublerActionError"/>,
/// <see cref="CubeDecisionData.TakerActionError"/>), an error the analyser
/// stored. An equity is a position's or an action's value, such as a play's
/// <see cref="PlayCandidate.Equity"/> or a cube action's
/// <see cref="CubeDecisionData.ActionEquity"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The rule.</b> A figure is shown at four decimals, and a loss counts as
/// zero exactly when it shows as <c>0.0000</c>: a loss that is not negative,
/// when it is below 0.00005. <see cref="CountsAsZero"/> is that sentence: it
/// reads <see cref="FormatLoss"/>'s own text, not a threshold of its own, so
/// what is shown and what is judged cannot disagree, at the boundary or
/// anywhere else. The precision is stated once, in a private format string
/// both displays read, so a loss and an equity beside it are shown alike. A
/// consumer shows every loss through <see cref="FormatLoss"/> and every
/// equity through <see cref="FormatEquity"/>, judges every loss through
/// <see cref="CountsAsZero"/>, and keeps no format string, sign rule or
/// threshold beside them.
/// </para>
/// <para>
/// <b>Verdicts only.</b> The rule decides whether a loss is correct; it
/// changes no stored number. Costs, errors and equities stay exact, as
/// computed from the analyser's numbers ("That's bad Xg data; we merely echo
/// what's there"), and so does which region of SPEC-scoring §3's tables a
/// decision falls in. A sum is judged as a sum: two parts that each count as
/// zero can add up to a cost that does not.
/// </para>
/// <para>
/// <b>Sign.</b> Every loss this library derives is never negative, so a loss
/// is shown bare. An error the analyser stored is echoed as stated, so the
/// loss members take any finite number: a loss that rounds to zero shows as
/// <c>0.0000</c> whatever its sign, negative zero included, and a negative
/// one beyond that shows its sign and does not count as zero. An equity is
/// either sign, so it always shows one: <c>+</c> wherever the figure shows no
/// minus, so an equity that rounds to zero shows as <c>+0.0000</c>, never
/// <c>-0.0000</c>.
/// </para>
/// <para>
/// Until halheinrich/backgammon#326's step 3a this type was
/// <c>EquityLoss</c>, with <c>Format</c> for <see cref="FormatLoss"/>: the
/// name no longer fit once it showed equities too, and it collided with
/// consumer members named for a play's loss.
/// </para>
/// </remarks>
public static class EquityDisplay
{
    /// <summary>
    /// The display: fixed point at four decimals, culture-invariant. The
    /// precision is stated here and nowhere else.
    /// </summary>
    private const string Display = "F4";

    /// <summary>
    /// A figure that rounds to zero from below, as the platform writes it:
    /// <c>-0.0000</c>. Declared ahead of <see cref="ZeroText"/>, whose
    /// initializer reads it through <see cref="FormatLoss"/>.
    /// </summary>
    private static readonly string NegativeZeroText = (-0.0).ToString(Display, CultureInfo.InvariantCulture);

    /// <summary>A zero loss as shown: <c>0.0000</c>.</summary>
    private static readonly string ZeroText = FormatLoss(0.0);

    /// <summary>
    /// <paramref name="loss"/> as it is shown: four decimals, invariant
    /// culture — <c>0.0000</c>, <c>0.0343</c>, <c>1.0267</c>. A loss that
    /// rounds to zero shows as <c>0.0000</c>, never <c>-0.0000</c>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="loss"/> is NaN or infinite: every stored
    /// number is finite, and so is every loss derived from them.
    /// </exception>
    public static string FormatLoss(double loss) =>
        Show(loss, nameof(loss), "An equity loss is a finite number.");

    /// <summary>
    /// <paramref name="equity"/> as it is shown, with its sign: the digits
    /// <see cref="FormatLoss"/> shows for the same number, with <c>+</c> ahead
    /// of any figure that shows no minus — <c>+0.6004</c>, <c>-0.3000</c>,
    /// and <c>+0.0000</c> for an equity that rounds to zero from either side,
    /// never <c>-0.0000</c>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="equity"/> is NaN or infinite: every stored
    /// equity is finite, and so is every equity derived from them.
    /// </exception>
    public static string FormatEquity(double equity)
    {
        string shown = Show(equity, nameof(equity), "An equity is a finite number.");
        return shown.StartsWith('-') ? shown : "+" + shown;
    }

    /// <summary>
    /// Whether <paramref name="loss"/> counts as zero: exactly when it shows
    /// as <c>0.0000</c> (<see cref="FormatLoss"/>), which for a loss that is
    /// not negative is when it is below 0.00005. A cost that counts as zero
    /// is correct.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="loss"/> is NaN or infinite, as
    /// <see cref="FormatLoss"/> refuses it.
    /// </exception>
    public static bool CountsAsZero(double loss) => FormatLoss(loss) == ZeroText;

    /// <summary>
    /// <paramref name="value"/> at the one precision, unsigned where it rounds
    /// to zero: the digits both displays share. A value that is not finite is
    /// refused, naming <paramref name="parameter"/>.
    /// </summary>
    private static string Show(double value, string parameter, string message)
    {
        if (!double.IsFinite(value))
            throw new ArgumentOutOfRangeException(parameter, value, message);
        string text = value.ToString(Display, CultureInfo.InvariantCulture);
        return text == NegativeZeroText ? text[1..] : text;
    }
}
