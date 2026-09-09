using System.Text.Json.Serialization.Metadata;

namespace BgDataTypes_Lib;

/// <summary>
/// The static half of a <see cref="NamedCollection{TValue, TSelf}"/>
/// specialization: the two things the generic machinery cannot know about
/// the sealed type deriving from it — how to make an instance, and where
/// the serializer finds its metadata. A base class cannot declare
/// <c>static abstract</c> members, so these live on an interface the
/// specialization lists beside the base class; both are best implemented
/// explicitly, keeping them off the specialization's public surface.
///
/// <para>
/// With the wire vocabulary living on the specialization's converter (see
/// <see cref="NamedCollectionJsonConverter{TValue, TSelf}"/>), this is the
/// whole of what a specialization writes:
/// </para>
/// <code>
/// [JsonConverter(typeof(NamedFilterCollectionJsonConverter))]
/// public sealed class NamedFilterCollection
///     : NamedCollection&lt;FilterConfig, NamedFilterCollection&gt;,
///       INamedCollectionSpecialization&lt;FilterConfig, NamedFilterCollection&gt;
/// {
///     private NamedFilterCollection(Entries entries) : base(entries) { }
///
///     static NamedFilterCollection INamedCollectionSpecialization&lt;FilterConfig, NamedFilterCollection&gt;
///         .Create(Entries entries) => new(entries);
///
///     static JsonTypeInfo&lt;NamedFilterCollection&gt; INamedCollectionSpecialization&lt;FilterConfig, NamedFilterCollection&gt;
///         .CanonicalTypeInfo => XgFilterJsonContext.Default.NamedFilterCollection;
/// }
///
/// public sealed class NamedFilterCollectionJsonConverter
///     : NamedCollectionJsonConverter&lt;FilterConfig, NamedFilterCollection&gt;
/// {
///     public NamedFilterCollectionJsonConverter() : base("filters", "config") { }
/// }
/// </code>
/// </summary>
/// <typeparam name="TValue">The payload type; see <see cref="NamedCollection{TValue, TSelf}"/>.</typeparam>
/// <typeparam name="TSelf">The specialization itself; see <see cref="NamedCollection{TValue, TSelf}"/>.</typeparam>
public interface INamedCollectionSpecialization<TValue, TSelf>
    where TValue : IJsonDocument<TValue>
    where TSelf : NamedCollection<TValue, TSelf>, INamedCollectionSpecialization<TValue, TSelf>
{
    /// <summary>
    /// The factory the base class builds every instance through — the
    /// empty collection, every wither result, every read. Implemented as a
    /// one-line forward to the specialization's private constructor. The
    /// <see cref="NamedCollection{TValue, TSelf}.Entries"/> argument is
    /// constructible only by the base class's assembly, so this member is
    /// not a construction hatch: nothing outside the machinery can call it
    /// with anything.
    /// </summary>
    /// <param name="entries">The canonical, snapshotted entry set.</param>
    /// <returns>A new instance over <paramref name="entries"/>.</returns>
    static abstract TSelf Create(NamedCollection<TValue, TSelf>.Entries entries);

    /// <summary>
    /// The single source of truth for how the specialization reaches a
    /// serializer: its metadata off its own repo's source-generated
    /// context, where the specialization is declared as a
    /// <c>[JsonSerializable]</c> root. The base class's trio resolves
    /// through this and never by reflection; the metadata carries no policy
    /// of its own, because the closed converter writes and reads the whole
    /// envelope by hand.
    /// </summary>
    static abstract JsonTypeInfo<TSelf> CanonicalTypeInfo { get; }
}
