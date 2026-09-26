namespace BgDataTypes_Lib;

/// <summary>
/// The one statement of the rule for a record's floating-point members: every
/// stored one — an equity, a probability, an analyser's error — is a finite
/// number, never NaN or an infinity, which no analysis produces and which
/// would leave the derivations here undefined (the rankings' order, each
/// play's error, the cube's scoring). A nullable one may be
/// <see langword="null"/> where none is recorded. It was first the candidate
/// equity's rule alone; the umbrella's fourth-round ruling on the records leg
/// of halheinrich/backgammon#273 extends it to every stored value.
/// </summary>
/// <remarks>
/// Range is deliberately not checked: a probability's bounds are not measured
/// against real data, so no rule states them yet. Code breaking the rule gets
/// the guard's <see cref="ArgumentOutOfRangeException"/>; a document breaking
/// it gets a <see cref="System.Text.Json.JsonException"/> carrying it, on
/// both paths (the wire rule on <see cref="BgDataTypesJsonContext"/>) — where
/// the reader's options let a non-finite number in at all; by default the
/// token itself is refused.
/// </remarks>
internal static class FiniteNumber
{
    /// <summary>Whether <paramref name="value"/> keeps the rule: <see langword="null"/>, or a finite number.</summary>
    internal static bool Holds(double? value) => value is not double number || double.IsFinite(number);

    /// <summary>The rule, as the message naming <paramref name="member"/> that refuses a breach of it.</summary>
    internal static string Message(string member) =>
        $"{member} is a finite number; NaN and the infinities are not.";

    /// <summary>Refuses <paramref name="value"/> for <paramref name="member"/> when it breaks the rule.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is NaN or an infinity.</exception>
    internal static void Check(double? value, string member)
    {
        if (!Holds(value))
            throw new ArgumentOutOfRangeException(member, value, Message(member));
    }
}
