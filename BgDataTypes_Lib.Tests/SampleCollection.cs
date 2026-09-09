using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The test suite's closed specialization of
/// <see cref="NamedCollection{TValue, TSelf}"/> over
/// <see cref="SamplePayload"/> — the exact shape the saved-filter and mix
/// documents will take downstream (halheinrich/backgammon#190 legs (B) and
/// (C)): a sealed type deriving with itself as <c>TSelf</c>, the two static
/// members implemented explicitly, a private constructor forwarding the
/// opaque entry set, and a type-level <c>[JsonConverter]</c> naming its own
/// closed converter. Declared as a root of <see cref="SampleJsonContext"/>,
/// so the source generator's acceptance of the pattern (the converter it
/// must instantiate from generated code) is proven by this project
/// compiling.
/// </summary>
[JsonConverter(typeof(SampleCollectionJsonConverter))]
public sealed class SampleCollection
    : NamedCollection<SamplePayload, SampleCollection>,
      INamedCollectionSpecialization<SamplePayload, SampleCollection>
{
    private SampleCollection(Entries entries) : base(entries)
    {
    }

    static SampleCollection INamedCollectionSpecialization<SamplePayload, SampleCollection>
        .Create(Entries entries) => new(entries);

    static JsonTypeInfo<SampleCollection> INamedCollectionSpecialization<SamplePayload, SampleCollection>
        .CanonicalTypeInfo => SampleJsonContext.Default.SampleCollection;
}

/// <summary>
/// The closed converter: public and parameterless-constructible, passing
/// the specialization's two wire names. Nothing else.
/// </summary>
public sealed class SampleCollectionJsonConverter
    : NamedCollectionJsonConverter<SamplePayload, SampleCollection>
{
    public SampleCollectionJsonConverter() : base("samples", "payload")
    {
    }
}
