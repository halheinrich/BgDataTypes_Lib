using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// The edition of XG's opening book an analysis came from — one of the typed
/// depth facts (see <see cref="PlayCandidate.BookEdition"/>), stated only for
/// a book hit (<see cref="AnalysisMode.BookRollout"/>). It survives only in
/// the depth label ("Book V1", "Book V2"); the abbreviation of either is
/// "Book".
/// </summary>
/// <remarks>
/// The token is the member's declared name, string-exact in both directions
/// like every enum here (<see cref="StrictJsonStringEnumConverter{TEnum}"/>).
/// The members a record stores are nullable: <see langword="null"/> is "no
/// edition recorded", so this enum has no <c>Unknown</c> member.
/// </remarks>
[JsonConverter(typeof(StrictJsonStringEnumConverter<BookEdition>))]
public enum BookEdition
{
    /// <summary>XG's first opening book.</summary>
    V1,

    /// <summary>XG's second opening book, the one whose rollout parameters a producer can recover.</summary>
    V2,
}
