using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The in-memory contract of <see cref="NamedCollection{TValue, TSelf}"/>,
/// exercised through the test specialization <see cref="SampleCollection"/>:
/// the name rule, the canonical order, the snapshot contract, and the
/// withers returning the specialization.
/// </summary>
public class NamedCollectionTests
{
    private static SamplePayload Rich() => new()
    {
        Label = "blitz",
        Weight = 7,
        Tags = ["a", "b"],
    };

    // -----------------------------------------------------------------------
    //  Empty
    // -----------------------------------------------------------------------

    [Fact]
    public void Empty_HasNoEntries()
    {
        Assert.Equal(0, SampleCollection.Empty.Count);
        Assert.Empty(SampleCollection.Empty.Names);
    }

    [Fact]
    public void Empty_IsOneInstancePerSpecialization()
    {
        Assert.Same(SampleCollection.Empty, SampleCollection.Empty);
        Assert.Same(SampleCollection.Empty, NamedCollection<SamplePayload, SampleCollection>.Empty);
    }

    [Fact]
    public void CurrentSchemaVersion_IsOne_ReachableThroughTheSpecialization()
    {
        Assert.Equal(1, SampleCollection.CurrentSchemaVersion);
    }

    // -----------------------------------------------------------------------
    //  With — add, replace, invariants; withers return the specialization
    // -----------------------------------------------------------------------

    [Fact]
    public void With_AddsAnEntry()
    {
        var collection = SampleCollection.Empty.With("Blitz", Rich());

        Assert.Equal(1, collection.Count);
        Assert.Equal(["Blitz"], collection.Names);
        Assert.True(collection.Contains("Blitz"));
    }

    [Fact]
    public void With_ReturnsANewInstance_LeavingTheOriginalUntouched()
    {
        var original = SampleCollection.Empty.With("Blitz", new SamplePayload());

        var extended = original.With("Calm", new SamplePayload());

        Assert.NotSame(original, extended);
        Assert.Equal(["Blitz"], original.Names);
        Assert.Equal(["Blitz", "Calm"], extended.Names);
    }

    [Fact]
    public void Withers_ReturnTheSpecialization_NotTheBase()
    {
        // Constraint 3 of the leg: a caller of the specialization keeps its
        // static type through With and Without. The declarations below are
        // the compile-level pin; the runtime pins confirm the instances.
        SampleCollection added = SampleCollection.Empty.With("Blitz", new SamplePayload());
        SampleCollection removed = added.Without("Blitz");

        Assert.IsType<SampleCollection>(added);
        Assert.IsType<SampleCollection>(removed);
    }

    [Fact]
    public void Names_AreSortedCaseInsensitively_RegardlessOfAddOrder()
    {
        // Mixed case chosen so an ordinal sort (upper before lower) would
        // yield Alpha, Charlie, bravo, delta — the mutation this pins against.
        var collection = SampleCollection.Empty
            .With("delta", new SamplePayload())
            .With("Alpha", new SamplePayload())
            .With("Charlie", new SamplePayload())
            .With("bravo", new SamplePayload());

        Assert.Equal(["Alpha", "bravo", "Charlie", "delta"], collection.Names);
    }

    [Fact]
    public void With_ExistingNameInDifferentCase_WithAnEqualValue_StillAdoptsTheNewSpelling()
    {
        // The case SetItem would get wrong: an immutable map's SetItem keeps
        // the existing key when the new value is equal to the stored one, so
        // a value-equal payload (QuizMix's shape) re-saved under a new
        // spelling would keep the old spelling. Remove-then-add does not.
        var collection = SampleCollection.Empty
            .With("Blitz", new SamplePayload())
            .With("blitz", new SamplePayload());

        Assert.Equal(["blitz"], collection.Names);
    }

    [Fact]
    public void With_ExistingNameInDifferentCase_Replaces_NewSpellingAndValueWin()
    {
        var collection = SampleCollection.Empty
            .With("Blitz", new SamplePayload())
            .With("blitz", Rich());

        Assert.Equal(1, collection.Count);
        Assert.Equal(["blitz"], collection.Names);   // last write wins for spelling too
        Assert.Equal(Rich().ToJson(), collection.Get("BLITZ").ToJson());
    }

    [Fact]
    public void With_ExistingNameInSameCase_Replaces_ValueWins()
    {
        var collection = SampleCollection.Empty
            .With("Blitz", new SamplePayload())
            .With("Blitz", Rich());

        Assert.Equal(1, collection.Count);
        Assert.Equal(Rich().ToJson(), collection.Get("Blitz").ToJson());
    }

    [Fact]
    public void With_NullName_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => SampleCollection.Empty.With(null!, new SamplePayload()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" Blitz")]
    [InlineData("Blitz ")]
    [InlineData("\tBlitz")]
    public void With_BlankOrUntrimmedName_Throws(string name)
    {
        var ex = Assert.Throws<ArgumentException>(
            () => SampleCollection.Empty.With(name, new SamplePayload()));

        Assert.Equal("name", ex.ParamName);
    }

    [Fact]
    public void With_NullValue_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => SampleCollection.Empty.With("Blitz", null!));
    }

    // -----------------------------------------------------------------------
    //  Without
    // -----------------------------------------------------------------------

    [Fact]
    public void Without_RemovesCaseInsensitively()
    {
        var collection = SampleCollection.Empty.With("Blitz", new SamplePayload());

        var removed = collection.Without("BLITZ");

        Assert.Equal(0, removed.Count);
        Assert.Equal(1, collection.Count);   // original untouched
    }

    [Fact]
    public void Without_MissingName_IsANoOpReturningTheSameInstance()
    {
        var collection = SampleCollection.Empty.With("Blitz", new SamplePayload());

        Assert.Same(collection, collection.Without("nope"));
        Assert.Same(SampleCollection.Empty, SampleCollection.Empty.Without("nope"));
    }

    [Fact]
    public void Without_NullName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => SampleCollection.Empty.Without(null!));
    }

    // -----------------------------------------------------------------------
    //  Lookup — Contains / Get / TryGet
    // -----------------------------------------------------------------------

    [Fact]
    public void Contains_IsCaseInsensitive()
    {
        var collection = SampleCollection.Empty.With("Blitz", new SamplePayload());

        Assert.True(collection.Contains("bLiTz"));
        Assert.False(collection.Contains("other"));
    }

    [Fact]
    public void Contains_NullName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => SampleCollection.Empty.Contains(null!));
    }

    [Fact]
    public void Get_ReturnsAnEquivalentValue_CaseInsensitively()
    {
        var collection = SampleCollection.Empty.With("Blitz", Rich());

        Assert.Equal(Rich().ToJson(), collection.Get("BLITZ").ToJson());
    }

    [Fact]
    public void Get_MissingName_Throws_NamingTheNameAndTheSpecialization()
    {
        var ex = Assert.Throws<KeyNotFoundException>(() => SampleCollection.Empty.Get("nope"));

        Assert.Contains("nope", ex.Message);
        Assert.Contains("SampleCollection", ex.Message);
    }

    [Fact]
    public void Get_NullName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => SampleCollection.Empty.Get(null!));
    }

    [Fact]
    public void TryGet_Hit_YieldsAnEquivalentValue()
    {
        var collection = SampleCollection.Empty.With("Blitz", Rich());

        var found = collection.TryGet("blitz", out var value);

        Assert.True(found);
        Assert.Equal(Rich().ToJson(), value!.ToJson());
    }

    [Fact]
    public void TryGet_Miss_YieldsNoValue()
    {
        var found = SampleCollection.Empty.TryGet("nope", out var value);

        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public void TryGet_NullName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => SampleCollection.Empty.TryGet(null!, out _));
    }

    // -----------------------------------------------------------------------
    //  Snapshot contract — the document stores values, not instances
    // -----------------------------------------------------------------------

    [Fact]
    public void With_SnapshotsOnIngress_LaterMutationOfTheCallersValueDoesNotLeakIn()
    {
        var live = new SamplePayload();
        var collection = SampleCollection.Empty.With("Blitz", live);

        live.Label = "mutated";
        live.Weight = 99;
        live.Tags.Add("mallory");

        var stored = collection.Get("Blitz");
        Assert.Equal("", stored.Label);
        Assert.Equal(0, stored.Weight);
        Assert.Empty(stored.Tags);
    }

    [Fact]
    public void Get_SnapshotsOnEgress_MutatingARetrievedValueDoesNotLeakBack()
    {
        var collection = SampleCollection.Empty.With("Blitz", new SamplePayload());

        var retrieved = collection.Get("Blitz");
        retrieved.Label = "mutated";
        retrieved.Tags.Add("mallory");

        var again = collection.Get("Blitz");
        Assert.Equal("", again.Label);
        Assert.Empty(again.Tags);
    }

    [Fact]
    public void TryGet_SnapshotsOnEgress_MutatingARetrievedValueDoesNotLeakBack()
    {
        var collection = SampleCollection.Empty.With("Blitz", new SamplePayload());

        collection.TryGet("Blitz", out var retrieved);
        retrieved!.Tags.Add("mallory");

        Assert.Empty(collection.Get("Blitz").Tags);
    }

    [Fact]
    public void Get_ReturnsAFreshInstancePerCall()
    {
        var collection = SampleCollection.Empty.With("Blitz", new SamplePayload());

        Assert.NotSame(collection.Get("Blitz"), collection.Get("Blitz"));
    }

    // -----------------------------------------------------------------------
    //  Equality — by reference, deliberately (see the type remarks)
    // -----------------------------------------------------------------------

    [Fact]
    public void Equality_IsByReference()
    {
        var first = SampleCollection.Empty.With("Blitz", new SamplePayload());
        var second = SampleCollection.Empty.With("Blitz", new SamplePayload());

        Assert.NotEqual(first, second);
        Assert.Equal(first.ToJson(), second.ToJson());   // the sameness a consumer compares
    }
}
