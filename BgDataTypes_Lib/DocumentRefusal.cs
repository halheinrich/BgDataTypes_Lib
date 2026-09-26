using System.Text.Json;

namespace BgDataTypes_Lib;

/// <summary>
/// The refusal of a construction rule a document breaks — the one spelling of
/// it. Code that breaks a record's rule gets the init guard's own
/// <see cref="ArgumentException"/>; a document that breaks it gets this
/// <see cref="JsonException"/>, carrying the guard's exception as its inner
/// exception and its message as its own. How a type knows which of the two is
/// building it is the wire rule stated on <see cref="BgDataTypesJsonContext"/>.
/// </summary>
internal static class DocumentRefusal
{
    /// <summary>The <see cref="JsonException"/> that <paramref name="fault"/> is when a document breaks the rule.</summary>
    internal static JsonException Of(ArgumentException fault) => new(fault.Message, fault);
}
