using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace BgDataTypes_Lib;

/// <summary>
/// An immutable collection of named <typeparamref name="TValue"/> documents
/// — the pick list a consumer offers when the user saves and reloads named
/// configurations — generalized from XgFilter_Lib's saved-filter document
/// (halheinrich/backgammon#190 leg (A)). This library does no I/O;
/// consumers load bytes, restore, apply withers, and write back. The name
/// rule, the canonical order, the snapshot contract and the envelope live
/// here once; a <i>specialization</i> — a sealed derived type such as the
/// saved-filter collection — contributes only its identity (see
/// <see cref="INamedCollectionSpecialization{TValue, TSelf}"/> and
/// <see cref="NamedCollectionJsonConverter{TValue, TSelf}"/>).
///
/// <para>
/// <b>The name rule.</b> One comparer
/// (<see cref="StringComparer.OrdinalIgnoreCase"/>) is the single
/// definition of "same name" everywhere it could matter: duplicate
/// rejection, <see cref="Contains"/>, <see cref="Get"/> /
/// <see cref="TryGet"/> lookup, replace-on-<see cref="With"/>,
/// <see cref="Without"/>, and the canonical sort — which the rule makes
/// total, since case-variant duplicates cannot coexist. Display case is
/// preserved as typed; a replacing <see cref="With"/> stores the new
/// spelling (last write wins for name and value alike). Names are
/// validated, never coerced: blank or untrimmed names are rejected, in
/// memory and on the wire.
/// </para>
///
/// <para>
/// <b>One canonical order.</b> Entries sort by name — in <see cref="Names"/>
/// and on the wire — so a given collection always serializes to the same
/// content regardless of the add/remove sequence that built it. Reads
/// accept entries in any order and re-canonicalize: order is presentation,
/// not semantics, so a hand-reordered file is not corruption (the duplicate
/// check still gates).
/// </para>
///
/// <para>
/// <b>Snapshot contract.</b> A payload may be mutable (UI state binds to
/// it), so this document stores each value's <i>serialized form</i>, never
/// the caller's instance: <see cref="With"/> snapshots on the way in via the
/// payload's canonical <see cref="IJsonDocument{TSelf}.ToJson"/> /
/// <see cref="IJsonDocument{TSelf}.FromJson"/> round-trip — which also
/// normalizes, so the stored value is exactly what the wire will carry —
/// and every retrieval hands out a fresh snapshot on the way out. Mutating
/// a value after saving it, or mutating one retrieved from the document,
/// never affects the document; saving an edit back is an explicit
/// <see cref="With"/>.
/// </para>
///
/// <para>
/// <b>Wire format: strict envelope, tolerant payload.</b> JSON via the
/// specialization's closed <see cref="NamedCollectionJsonConverter{TValue, TSelf}"/>
/// (type-level <c>[JsonConverter]</c> on the specialization — consumers
/// register nothing) with schema version <see cref="CurrentSchemaVersion"/>.
/// The envelope (version, structure, names) is fail-loud, with a version
/// bump as its only evolution mechanism; entry bodies delegate to the
/// payload's own <see cref="IJsonDocument{TSelf}.FromJson"/>, whose
/// tolerance is the payload's to define — a retired payload member must
/// never brick a user's saved collection. The collection is itself an
/// <see cref="IJsonDocument{TSelf}"/>: <see cref="ToJson"/> /
/// <see cref="FromJson"/> round-trip, <see cref="TryFromJson"/> restores an
/// absent or corrupt store to <see cref="Empty"/>.
/// </para>
///
/// <para>
/// <b>Reference equality, deliberately.</b> The type wraps a map of
/// snapshots; value equality would need a definition of payload equality
/// the <see cref="IJsonDocument{TSelf}"/> contract does not promise, and
/// no caller compares collections. Instances compare by reference; a
/// consumer that needs "did the document change" compares
/// <see cref="ToJson"/> output.
/// </para>
/// </summary>
/// <typeparam name="TValue">
/// The payload type stored under each name. Any
/// <see cref="IJsonDocument{TSelf}"/> — its trio is the snapshot mechanism
/// and the entry-body seam.
/// </typeparam>
/// <typeparam name="TSelf">
/// The specialization itself: the sealed type deriving from this class
/// <i>with itself as this argument</i>, so <see cref="With"/> and
/// <see cref="Without"/> return the specialization and the trio is typed to
/// it. The self-type is a convention the compiler cannot fully enforce: a
/// type deriving with another specialization as <typeparamref name="TSelf"/>
/// fails at runtime with <see cref="InvalidCastException"/> on first use.
/// </typeparam>
public abstract class NamedCollection<TValue, TSelf> : IJsonDocument<TSelf>
    where TValue : IJsonDocument<TValue>
    where TSelf : NamedCollection<TValue, TSelf>, INamedCollectionSpecialization<TValue, TSelf>
{
    /// <summary>
    /// The schema version every specialization's envelope reads and writes.
    /// Reads reject any other version (fail-loud); see
    /// <see cref="NamedCollectionJsonConverter{TValue, TSelf}"/>.
    /// </summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>
    /// The document's name rule — the single definition of "same name" for
    /// duplicate rejection, lookup, replace, removal, and the canonical sort
    /// (see the type remarks). Internal so the converter's wire-level
    /// duplicate check applies the same rule.
    /// </summary>
    internal static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;

    // Declared before Empty: static initializers run in textual order, and
    // Empty's needs this map. Kept off the nested Entries type so no static
    // initialization cycle between outer and nested can exist.
    private static readonly ImmutableSortedDictionary<string, TValue> EmptyMap =
        ImmutableSortedDictionary.Create<string, TValue>(NameComparer);

    private readonly ImmutableSortedDictionary<string, TValue> _entries;

    /// <summary>The empty collection: no entries. One instance per specialization.</summary>
    public static TSelf Empty { get; } = TSelf.Create(new Entries(EmptyMap));

    /// <summary>
    /// Initializes the collection over an entry set built by this
    /// machinery. A specialization's private constructor forwards here and
    /// nowhere else; there is no other construction path.
    /// </summary>
    /// <param name="entries">The canonical, snapshotted entry set.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entries"/> is <see langword="null"/>.
    /// </exception>
    protected NamedCollection(Entries entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        _entries = entries.Map;
        Names = entries.Map.Keys.ToImmutableArray();
    }

    /// <summary>Number of entries in this collection.</summary>
    public int Count => _entries.Count;

    /// <summary>
    /// The entry names in canonical (name-sorted) order — what a pick list
    /// binds. Display case is as typed at each name's latest
    /// <see cref="With"/>.
    /// </summary>
    public IReadOnlyList<string> Names { get; }

    /// <summary>
    /// Whether an entry with this name exists, per the name rule
    /// (case-insensitive) — e.g. a consumer's save-as overwrite check.
    /// </summary>
    /// <param name="name">The name to look up.</param>
    /// <returns><see langword="true"/> if an entry with this name exists.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    public bool Contains(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _entries.ContainsKey(name);
    }

    /// <summary>
    /// Retrieves the named value as a fresh snapshot — the caller's to bind
    /// and mutate freely; saving an edit back is an explicit
    /// <see cref="With"/>. Every call returns a new instance (see the
    /// snapshot contract in the type remarks). Lookup follows the name rule
    /// (case-insensitive).
    /// </summary>
    /// <param name="name">The name to look up.</param>
    /// <returns>A fresh copy of the stored value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when no entry has this name.
    /// </exception>
    public TValue Get(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (!_entries.TryGetValue(name, out var stored))
            throw new KeyNotFoundException($"{typeof(TSelf).Name} has no entry named '{name}'.");
        return Snapshot(stored);
    }

    /// <summary>
    /// Non-throwing counterpart to <see cref="Get"/>. Yields no value on a
    /// miss — deliberately unlike <see cref="TryFromJson"/>'s always-usable
    /// out: a failed restore has a sensible fallback (the empty collection),
    /// but a lookup miss does not, and a default payload here would silently
    /// stand in for a saved one.
    /// </summary>
    /// <param name="name">The name to look up.</param>
    /// <param name="value">
    /// On return, a fresh snapshot of the stored value, or the type's
    /// default when no entry has this name.
    /// </param>
    /// <returns><see langword="true"/> if an entry with this name exists.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    public bool TryGet(string name, [MaybeNullWhen(false)] out TValue value)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (_entries.TryGetValue(name, out var stored))
        {
            value = Snapshot(stored);
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Returns a new collection with this name bound to a snapshot of
    /// <paramref name="value"/> — added, or replaced if the name already
    /// exists per the name rule. This is the save-as operation, including
    /// save-under-an-existing-name-overwrites; whether to confirm the
    /// overwrite is the consumer's UI concern. On replace, the new spelling
    /// wins along with the new value.
    /// </summary>
    /// <param name="name">
    /// The entry's name: non-blank, with no leading or trailing whitespace
    /// (validated, not trimmed — the caller normalizes user input).
    /// </param>
    /// <param name="value">
    /// The value to save. The document stores a snapshot of its current
    /// state, never this instance — see the type remarks.
    /// </param>
    /// <returns>The new collection; this instance is unchanged.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name"/> or <paramref name="value"/> is
    /// <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is empty or whitespace-only, or
    /// carries leading or trailing whitespace.
    /// </exception>
    public TSelf With(string name, TValue value)
    {
        ValidateName(name);
        ArgumentNullException.ThrowIfNull(value);

        // Remove-then-add rather than SetItem: SetItem retains the existing
        // key's spelling on replace, and the contract is last-write-wins for
        // the display case too.
        return TSelf.Create(new Entries(_entries.Remove(name).Add(name, Snapshot(value))));
    }

    /// <summary>
    /// Returns a collection without the named entry, per the name rule
    /// (case-insensitive). A missing name is a no-op returning this same
    /// instance — deletion is idempotent.
    /// </summary>
    /// <param name="name">The name to remove.</param>
    /// <returns>
    /// The new collection, or this instance when the name was not present.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    public TSelf Without(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var updated = _entries.Remove(name);
        return ReferenceEquals(updated, _entries) ? (TSelf)this : TSelf.Create(new Entries(updated));
    }

    /// <summary>
    /// Serializes this collection to its canonical JSON representation — the
    /// inverse of <see cref="FromJson"/>: the specialization's envelope,
    /// entries in canonical order, each body the payload's own canonical
    /// form. Resolves its metadata from the specialization's context
    /// (<see cref="INamedCollectionSpecialization{TValue, TSelf}.CanonicalTypeInfo"/>),
    /// never by reflection.
    /// </summary>
    /// <returns>A JSON object string carrying every entry of this collection.</returns>
    public string ToJson() => JsonSerializer.Serialize((TSelf)this, TSelf.CanonicalTypeInfo);

    /// <summary>
    /// Deserializes a <typeparamref name="TSelf"/> from its canonical JSON
    /// representation — the inverse of <see cref="ToJson"/>. Fail-loud: see
    /// <see cref="NamedCollectionJsonConverter{TValue, TSelf}"/> for
    /// everything a read rejects.
    /// </summary>
    /// <param name="json">A JSON object string, typically produced by <see cref="ToJson"/>.</param>
    /// <returns>The materialized collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="json"/> is the literal <c>null</c> token, which yields
    /// no collection.
    /// </exception>
    /// <exception cref="JsonException"><paramref name="json"/> is malformed or violates the wire contract.</exception>
    public static TSelf FromJson(string json) => CanonicalJson.Parse(json, TSelf.CanonicalTypeInfo);

    /// <summary>
    /// Non-throwing counterpart to <see cref="FromJson"/>. Absorbs the three
    /// ways a restore can fail — a null <paramref name="json"/> (e.g. a file
    /// that does not exist), the literal <c>null</c> token, or
    /// malformed/contract-violating JSON — and yields <see cref="Empty"/> in
    /// each case. A consumer that wants to preserve a corrupt file for the
    /// user to repair (rather than overwrite it with the empty fallback)
    /// reacts to the <see langword="false"/> return.
    /// </summary>
    /// <param name="json">
    /// The candidate JSON, or null. Typically read straight from a
    /// persistence store whose contents the caller does not control.
    /// </param>
    /// <param name="collection">
    /// On return, always a usable collection: the restored instance on
    /// success, or <see cref="Empty"/> on failure.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="json"/> was successfully
    /// deserialized; <see langword="false"/> otherwise.
    /// </returns>
    public static bool TryFromJson(string? json, out TSelf collection) =>
        CanonicalJson.TryParse(json, TSelf.CanonicalTypeInfo, Empty, out collection);

    /// <summary>
    /// The stored entries in canonical order, for the converter's write
    /// path. Values are the document's private snapshots — internal callers
    /// must neither leak nor mutate them.
    /// </summary>
    internal IEnumerable<KeyValuePair<string, TValue>> CanonicalEntries => _entries;

    /// <summary>
    /// The boundary deep copy: the payload's canonical serialize/deserialize
    /// round-trip, which is already the single, tested definition of its
    /// value — no separate <c>Clone</c> whose deep-copy semantics would need
    /// independent specification. Also normalizes: what is stored is
    /// byte-identical to what the wire will carry.
    /// </summary>
    private static TValue Snapshot(TValue value) => TValue.FromJson(value.ToJson());

    private static void ValidateName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name must be non-blank.", nameof(name));
        if (name != name.Trim())
            throw new ArgumentException(
                $"Name must not carry leading or trailing whitespace (got '{name}').",
                nameof(name));
    }

    /// <summary>
    /// The opaque entry set a specialization's factory receives and its
    /// constructor forwards: canonical, snapshotted, keyed by the name rule.
    /// Constructible only by the collection machinery in this assembly —
    /// which is what closes the name rule and the snapshot contract against
    /// a bypass: no caller can hand a specialization an arbitrary map, and a
    /// specialization cannot build one of its own.
    /// </summary>
    public sealed class Entries
    {
        internal Entries(ImmutableSortedDictionary<string, TValue> map)
        {
            Map = map;
        }

        internal ImmutableSortedDictionary<string, TValue> Map { get; }
    }
}
