namespace BgDataTypes_Lib;

/// <summary>
/// The one statement of the rule for a record's text members: a text member
/// states text, or — where it may — nothing, spelled <see langword="null"/>.
/// Empty or white-space text is neither, so it is refused: "none recorded"
/// has one spelling, and it is <see langword="null"/>, as it is for a
/// number. (An enum keeps its <c>Unknown</c> member, which a producer states.)
/// </summary>
internal static class StatedText
{
    /// <summary>Whether <paramref name="value"/> keeps the rule: <see langword="null"/>, or text that is not all white space.</summary>
    internal static bool Holds(string? value) => value is null || !string.IsNullOrWhiteSpace(value);

    /// <summary>The rule, as the message naming <paramref name="member"/> that refuses a breach of it.</summary>
    internal static string Message(string member) =>
        $"{member} states text, or null where none is recorded; empty or white-space text is neither.";

    /// <summary>Refuses <paramref name="value"/> for <paramref name="member"/> when it breaks the rule.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty or white space.</exception>
    internal static void Check(string? value, string member)
    {
        if (!Holds(value))
            throw new ArgumentException(Message(member), member);
    }
}
