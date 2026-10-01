using System.Globalization;

namespace BgDataTypes_Lib;

/// <summary>
/// How an equity loss is shown, and when it counts as zero: the one owner of
/// both (halheinrich/backgammon#202, folded into halheinrich/backgammon#326
/// by Hal on 2026-10-01; SPEC-scoring §2a and §3). An equity loss is any cost
/// or error measured in equity: a cube answer's cost or either of its parts
/// (<see cref="CubeAnswerCost"/>), a checker play's error
/// (<see cref="RankedPlay.Error"/>), a cube action's error
/// (<see cref="CubeDecisionData.DoublerActionError"/>,
/// <see cref="CubeDecisionData.TakerActionError"/>), an error the analyser
/// stored.
/// </summary>
/// <remarks>
/// <para>
/// <b>The rule.</b> A loss is shown at four decimals, and it counts as zero
/// exactly when it shows as <c>0.0000</c>: a loss that is not negative, when
/// it is below 0.00005.
/// <see cref="CountsAsZero"/> is that sentence: it reads
/// <see cref="Format"/>'s own text, not a threshold of its own, so what is
/// shown and what is judged cannot disagree, at the boundary or anywhere
/// else. A consumer shows every loss through <see cref="Format"/> and judges
/// every one through <see cref="CountsAsZero"/>, and keeps no format string or
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
/// <b>Sign.</b> Every loss this library derives is never negative. An error
/// the analyser stored is echoed as stated, so the two members take any
/// finite number: a loss that rounds to zero shows as <c>0.0000</c> whatever
/// its sign, negative zero included, and a negative one beyond that shows its
/// sign and does not count as zero.
/// </para>
/// </remarks>
public static class EquityLoss
{
    /// <summary>
    /// The display: fixed point at four decimals, culture-invariant. The
    /// precision is stated here and nowhere else.
    /// </summary>
    private const string Display = "F4";

    /// <summary>
    /// A loss that rounds to zero from below, as the platform writes it:
    /// <c>-0.0000</c>. Declared ahead of <see cref="ZeroText"/>, whose
    /// initializer reads it through <see cref="Format"/>.
    /// </summary>
    private static readonly string NegativeZeroText = (-0.0).ToString(Display, CultureInfo.InvariantCulture);

    /// <summary>A zero loss as shown: <c>0.0000</c>.</summary>
    private static readonly string ZeroText = Format(0.0);

    /// <summary>
    /// <paramref name="loss"/> as it is shown: four decimals, invariant
    /// culture — <c>0.0000</c>, <c>0.0343</c>, <c>1.0267</c>. A loss that
    /// rounds to zero shows as <c>0.0000</c>, never <c>-0.0000</c>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="loss"/> is NaN or infinite: every stored
    /// number is finite, and so is every loss derived from them.
    /// </exception>
    public static string Format(double loss)
    {
        if (!double.IsFinite(loss))
            throw new ArgumentOutOfRangeException(nameof(loss), loss, "An equity loss is a finite number.");
        string text = loss.ToString(Display, CultureInfo.InvariantCulture);
        return text == NegativeZeroText ? text[1..] : text;
    }

    /// <summary>
    /// Whether <paramref name="loss"/> counts as zero: exactly when it shows
    /// as <c>0.0000</c> (<see cref="Format"/>), which for a loss that is not
    /// negative is when it is below 0.00005. A cost that counts as zero is
    /// correct.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="loss"/> is NaN or infinite, as
    /// <see cref="Format"/> refuses it.
    /// </exception>
    public static bool CountsAsZero(double loss) => Format(loss) == ZeroText;
}
