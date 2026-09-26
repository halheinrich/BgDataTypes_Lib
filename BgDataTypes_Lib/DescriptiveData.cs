using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// The provenance-and-metadata category of a <see cref="BgDecisionData"/>:
/// who was playing, and the match it belongs to. Producer-supplied from the
/// source file's headers (see <c>ConvertXgToJson_Lib</c>). Every member but
/// the nullable ones is <c>required</c>, per the wire rule stated on
/// <see cref="BgDataTypesJsonContext"/>, and each nullable member's
/// documentation says what <see langword="null"/> means.
///
/// <para>
/// <b>None recorded is <see langword="null"/>.</b> Each text member states
/// text or nothing: empty or white-space text is refused, by code with an
/// <see cref="ArgumentException"/> naming the member and by a document with
/// a <see cref="System.Text.Json.JsonException"/> carrying it (the wire rule
/// on <see cref="BgDataTypesJsonContext"/>), so "none" has one spelling.
/// </para>
///
/// <para>
/// The decision's game and move number are not here, and neither is its
/// source file: the record's <see cref="BgDecisionData.Id"/> carries all
/// three, and the record derives <see cref="BgDecisionData.Game"/>,
/// <see cref="BgDecisionData.MoveNumber"/> and
/// <see cref="BgDecisionData.SourceFile"/> from it — the game and move
/// <see langword="null"/> for a standalone position, never a stamped 1
/// (halheinrich/backgammon#124). A document still stating the retired
/// <c>SourceFile</c> here reads with it ignored. The one game fact that is
/// here, <see cref="IsStandardStart"/>, is likewise <see langword="null"/>
/// for a standalone position.
/// </para>
/// </summary>
public class DescriptiveData
{
    // True while the category is read from a document (see the serializer's
    // constructor below): each rule then refuses as a JsonException.
    private readonly bool _read;
    private readonly string? _onRollName;
    private readonly string? _opponentName;
    private readonly string? _title;
    private readonly string? _event;
    private readonly string? _comment;

    /// <summary>Creates the category; its members are set by the initializer.</summary>
    public DescriptiveData()
    {
    }

    /// <summary>
    /// The serializer's constructor, for a category read from a document: it
    /// marks the category as read before any member is set, so every rule
    /// refuses a breach as a <see cref="System.Text.Json.JsonException"/> (the
    /// wire rule on <see cref="BgDataTypesJsonContext"/>). It takes
    /// <paramref name="matchLength"/> only because a serializer constructor
    /// must bind a member.
    /// </summary>
    [JsonConstructor]
    internal DescriptiveData(int matchLength)
    {
        _read = true;
        MatchLength = matchLength;
    }

    /// <summary>Match length in points. 0 = unlimited / money session.</summary>
    public required int MatchLength { get; init; }

    /// <summary>
    /// Name of the player on roll — the decision-maker this record scores.
    /// Surfaced as <see cref="IDecisionFilterData.Player"/> for filtering.
    /// <see langword="null"/> when the source recorded no name; never empty.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown on init when the value is empty or white space.</exception>
    public string? OnRollName
    {
        get => _onRollName;
        init => _onRollName = Stated(value, nameof(OnRollName));
    }

    /// <summary>Name of the opponent. <see langword="null"/> when the source recorded no name; never empty.</summary>
    /// <exception cref="ArgumentException">Thrown on init when the value is empty or white space.</exception>
    public string? OpponentName
    {
        get => _opponentName;
        init => _opponentName = Stated(value, nameof(OpponentName));
    }

    /// <summary>Save/session title as the source file stored it. <see langword="null"/> when none was recorded; never empty.</summary>
    /// <exception cref="ArgumentException">Thrown on init when the value is empty or white space.</exception>
    public string? Title
    {
        get => _title;
        init => _title = Stated(value, nameof(Title));
    }

    /// <summary>Match date as the source file stored it. <see langword="null"/> when none was recorded.</summary>
    public DateOnly? Date { get; init; }

    /// <summary>Event name (e.g. tournament) as the source file stored it. <see langword="null"/> when none was recorded; never empty.</summary>
    /// <exception cref="ArgumentException">Thrown on init when the value is empty or white space.</exception>
    public string? Event
    {
        get => _event;
        init => _event = Stated(value, nameof(Event));
    }

    /// <summary>
    /// Whether the decision's game started from the canonical opening
    /// position: true if it did, false for a non-standard start (custom
    /// positions, problem setups, Bg960 variants). <see langword="null"/> for a
    /// decision in a standalone position, which belongs to no game, so has no
    /// start (halheinrich/backgammon#124) — never a false standing for "not
    /// applicable". The record holds it to its <see cref="BgDecisionData.Id"/>:
    /// <see langword="null"/> exactly when the Id is an
    /// <see cref="XgpDecisionId"/> (<see cref="DecisionRules.StartMessage"/>).
    /// </summary>
    public bool? IsStandardStart { get; init; }

    /// <summary>XG's per-decision comment text. <see langword="null"/> when none was recorded; never empty.</summary>
    /// <exception cref="ArgumentException">Thrown on init when the value is empty or white space.</exception>
    public string? Comment
    {
        get => _comment;
        init => _comment = Stated(value, nameof(Comment));
    }

    /// <summary>True if the user flagged this decision in XG (the "flag" marker).</summary>
    public required bool Flagged { get; init; }

    /// <summary><paramref name="value"/>, once it keeps the text rule (<see cref="StatedText"/>).</summary>
    private string? Stated(string? value, string member)
    {
        try
        {
            StatedText.Check(value, member);
        }
        catch (ArgumentException fault) when (_read)
        {
            throw DocumentRefusal.Of(fault);
        }
        return value;
    }
}
