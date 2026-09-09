using System.Text.Json;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// Serialises a <see cref="NamedCollection{TValue, TSelf}"/> specialization
/// as its versioned persistent wire format. Abstract: it exists to be
/// closed by each specialization's own sealed, public, parameterless
/// converter, which passes the two property names that are that
/// specialization's wire identity and is what the specialization's
/// type-level <c>[JsonConverter]</c> names (see
/// <see cref="INamedCollectionSpecialization{TValue, TSelf}"/> for the
/// whole pattern). Consumers register nothing.
///
/// <para>
/// The envelope is hand-written with fixed property names — the persisted
/// format is a file contract and must not vary with the consumer's options
/// (naming policy etc.). Whitespace is the one thing options still control
/// (<see cref="JsonSerializerOptions.WriteIndented"/> lives on the writer
/// the serializer creates), so byte-stable files additionally need fixed
/// consumer-side options. Writes order entries by name — the document's
/// canonical order — so a given collection always serializes to the same
/// content. Wire shape (schema version
/// <see cref="NamedCollection{TValue, TSelf}.CurrentSchemaVersion"/>), with
/// <c>entries</c> and <c>value</c> standing for the specialization's two
/// names — the saved-filter document spells them <c>filters</c> and
/// <c>config</c>:
/// </para>
///
/// <code>
/// {
///   "schemaVersion": 1,
///   "entries": [
///     { "name": "Blitz mistakes", "value": { ...the payload's canonical JSON... } }
///   ]
/// }
/// </code>
///
/// <para>
/// <b>The envelope is strict and fail-loud.</b> A schema version other than
/// <see cref="NamedCollection{TValue, TSelf}.CurrentSchemaVersion"/> (with
/// a distinguished "newer than this reader supports" message — a version
/// bump is the envelope's only evolution mechanism), a missing required
/// property, an unknown property at the top or entry level, an invalid
/// name (blank or untrimmed — the same single-sourced rule as
/// <see cref="NamedCollection{TValue, TSelf}.With"/>, which reads route
/// through), or a duplicate name per the document's name rule
/// (case-insensitive; checked explicitly here because
/// <see cref="NamedCollection{TValue, TSelf}.With"/> would silently
/// replace) all throw <see cref="JsonException"/>. Entry order is the one
/// envelope liberty: reads accept any order and re-canonicalize, because
/// order is presentation, not semantics.
/// </para>
///
/// <para>
/// <b>Entry bodies are the payload's.</b> Each entry's value body is handed
/// verbatim to the payload's <see cref="IJsonDocument{TSelf}.FromJson"/> —
/// its canonical deserialization seam — so whatever that parser tolerates
/// (unknown or retired members, for the real payloads) is tolerated here,
/// and a member retirement never bricks a saved collection. A body the
/// payload itself rejects (a contract violation, the <c>null</c> token, a
/// non-object) is corruption, not evolution, and fails the whole file with
/// the entry named in the message — a silently-reset entry would stand in
/// for a saved one, which is worse than a loud error.
/// <see cref="NamedCollection{TValue, TSelf}.TryFromJson"/> is the tolerant
/// restore path.
/// </para>
///
/// <para>
/// <b>Closed, public, parameterless — because the source generator needs
/// it to be.</b> The generator emits <c>new TheConverter()</c> into the
/// <em>declaring</em> assembly of every context that names the annotated
/// type, so the converter a <c>[JsonConverter]</c> names must be a
/// constructible closed type: an open generic cannot be named by an
/// attribute, this base has no parameterless constructor by design, and an
/// internal converter fails a downstream context with SYSLIB1220 then
/// SYSLIB1030 (the rule every bundled converter in this library states).
/// A converter factory activating a closed converter at runtime is the
/// reflection path this pattern exists to avoid.
/// </para>
/// </summary>
/// <typeparam name="TValue">The payload type; see <see cref="NamedCollection{TValue, TSelf}"/>.</typeparam>
/// <typeparam name="TSelf">The specialization; see <see cref="NamedCollection{TValue, TSelf}"/>.</typeparam>
public abstract class NamedCollectionJsonConverter<TValue, TSelf> : JsonConverter<TSelf>
    where TValue : IJsonDocument<TValue>
    where TSelf : NamedCollection<TValue, TSelf>, INamedCollectionSpecialization<TValue, TSelf>
{
    private const string SchemaVersionPropertyName = "schemaVersion";
    private const string NamePropertyName = "name";

    /// <summary>
    /// Initializes the converter with the specialization's two wire names.
    /// </summary>
    /// <param name="entriesPropertyName">
    /// The top-level property holding the entries array (<c>filters</c> for
    /// the saved-filter document).
    /// </param>
    /// <param name="valuePropertyName">
    /// The per-entry property holding the payload body (<c>config</c> for
    /// the saved-filter document).
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when either name is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when either name is blank or untrimmed, or collides with the
    /// envelope's own fixed names (<c>schemaVersion</c> at the top level,
    /// <c>name</c> at the entry level).
    /// </exception>
    protected NamedCollectionJsonConverter(string entriesPropertyName, string valuePropertyName)
    {
        ValidatePropertyName(entriesPropertyName, SchemaVersionPropertyName, nameof(entriesPropertyName));
        ValidatePropertyName(valuePropertyName, NamePropertyName, nameof(valuePropertyName));
        EntriesPropertyName = entriesPropertyName;
        ValuePropertyName = valuePropertyName;
    }

    /// <summary>The top-level property holding the entries array.</summary>
    public string EntriesPropertyName { get; }

    /// <summary>The per-entry property holding the payload body.</summary>
    public string ValuePropertyName { get; }

    private static string TypeName => typeof(TSelf).Name;

    /// <inheritdoc/>
    public override TSelf? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected object for {TypeName}, got {reader.TokenType}.");

        int? schemaVersion = null;
        List<(string Name, TValue Value)>? entries = null;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            var property = reader.GetString();
            reader.Read();
            if (property == SchemaVersionPropertyName)
                schemaVersion = ReadSchemaVersion(ref reader);
            else if (property == EntriesPropertyName)
                entries = ReadEntries(ref reader);
            else
                throw new JsonException($"Unknown {TypeName} property '{property}'.");
        }

        if (schemaVersion is null)
            throw new JsonException($"Missing required property '{SchemaVersionPropertyName}'.");
        if (entries is null)
            throw new JsonException($"Missing required property '{EntriesPropertyName}'.");

        var collection = NamedCollection<TValue, TSelf>.Empty;
        var seen = new HashSet<string>(NamedCollection<TValue, TSelf>.NameComparer);
        foreach (var (name, value) in entries)
        {
            if (!seen.Add(name))
                throw new JsonException($"Duplicate entry name '{name}'.");

            try
            {
                // Routes through With so the wire-level name rule has the same
                // single definition as the in-memory one. With re-snapshots the
                // freshly parsed value — a second round-trip per entry, seen
                // and accepted: one construction path, stored form guaranteed
                // canonical, negligible at load-a-pick-list scale.
                collection = collection.With(name, value);
            }
            catch (ArgumentException ex)
            {
                throw new JsonException(ex.Message, ex);
            }
        }

        return collection;
    }

    private static int ReadSchemaVersion(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out int version))
            throw new JsonException(
                $"Expected integer for '{SchemaVersionPropertyName}', got {reader.TokenType}.");

        const int current = NamedCollection<TValue, TSelf>.CurrentSchemaVersion;
        if (version > current)
            throw new JsonException(
                $"{TypeName} document has schema version {version}, newer than the highest " +
                $"version this reader supports ({current}).");
        if (version != current)
            throw new JsonException(
                $"{TypeName} document has unsupported schema version {version}; expected {current}.");

        return version;
    }

    private List<(string Name, TValue Value)> ReadEntries(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException($"Expected array for '{EntriesPropertyName}', got {reader.TokenType}.");

        var entries = new List<(string, TValue)>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            entries.Add(ReadEntry(ref reader));

        return entries;
    }

    private (string Name, TValue Value) ReadEntry(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException(
                $"Expected object for a '{EntriesPropertyName}' element, got {reader.TokenType}.");

        string? name = null;
        JsonDocument? body = null;

        try
        {
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                var property = reader.GetString();
                reader.Read();
                if (property == NamePropertyName)
                {
                    if (reader.TokenType != JsonTokenType.String)
                        throw new JsonException(
                            $"Expected string for '{NamePropertyName}', got {reader.TokenType}.");
                    name = reader.GetString();
                }
                else if (property == ValuePropertyName)
                {
                    // Captured raw and re-parsed below through the payload's
                    // own seam — the tolerant-payload boundary.
                    body = JsonDocument.ParseValue(ref reader);
                }
                else
                {
                    throw new JsonException(
                        $"Unknown '{EntriesPropertyName}' element property '{property}'.");
                }
            }

            if (name is null)
                throw new JsonException(
                    $"A '{EntriesPropertyName}' element is missing required property '{NamePropertyName}'.");
            if (body is null)
                throw new JsonException(
                    $"Entry '{name}' is missing required property '{ValuePropertyName}'.");

            try
            {
                return (name, TValue.FromJson(body.RootElement.GetRawText()));
            }
            catch (Exception ex) when (ex is JsonException or ArgumentException)
            {
                // Fail the file, naming the entry: a body the payload rejects
                // is corruption, not evolution (whatever the payload tolerates
                // was already absorbed by its own parser above).
                throw new JsonException(
                    $"Entry '{name}' has an invalid '{ValuePropertyName}' body: {ex.Message}", ex);
            }
        }
        finally
        {
            body?.Dispose();
        }
    }

    /// <inheritdoc/>
    public override void Write(
        Utf8JsonWriter writer,
        TSelf value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber(SchemaVersionPropertyName, NamedCollection<TValue, TSelf>.CurrentSchemaVersion);
        writer.WriteStartArray(EntriesPropertyName);

        foreach (var (name, entry) in value.CanonicalEntries)   // name-sorted — canonical
        {
            writer.WriteStartObject();
            writer.WriteString(NamePropertyName, name);
            writer.WritePropertyName(ValuePropertyName);

            // Embedded via the payload's canonical writer so its ToJson stays
            // the single definition of a value's wire form.
            using (var body = JsonDocument.Parse(entry.ToJson()))
                body.RootElement.WriteTo(writer);

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void ValidatePropertyName(string name, string reserved, string paramName)
    {
        ArgumentNullException.ThrowIfNull(name, paramName);
        if (string.IsNullOrWhiteSpace(name) || name != name.Trim())
            throw new ArgumentException("Wire property name must be non-blank and trimmed.", paramName);
        if (name == reserved)
            throw new ArgumentException(
                $"Wire property name '{name}' collides with the envelope's fixed property.", paramName);
    }
}
