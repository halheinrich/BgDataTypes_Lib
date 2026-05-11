using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CubeOwner { OnRoll, Opponent, Centered }