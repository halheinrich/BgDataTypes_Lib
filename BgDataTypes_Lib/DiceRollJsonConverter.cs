using System.Text.Json;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// Serialises <see cref="DiceRoll"/> as its canonical two-digit token ("31",
/// "55" — see the <see cref="DiceRoll"/> remarks). Default property-based
/// serialisation would emit a <c>{"High":…,"Low":…}</c> object with no setter
/// path back; the token is the intended on-wire shape regardless. Bundled via
/// type-level <c>[JsonConverter]</c> attribute on <see cref="DiceRoll"/> so
/// consumers do not need to register the converter on their
/// <see cref="JsonSerializerOptions"/>.
/// </summary>
internal sealed class DiceRollJsonConverter : JsonConverter<DiceRoll>
{
    public override DiceRoll Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException(
                $"Expected string for DiceRoll, got {reader.TokenType}.");

        var s = reader.GetString();
        if (s is null || !DiceRoll.TryParse(s, provider: null, out var result))
            throw new JsonException($"Invalid DiceRoll token: '{s}'.");

        return result;
    }

    public override void Write(
        Utf8JsonWriter writer,
        DiceRoll value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
