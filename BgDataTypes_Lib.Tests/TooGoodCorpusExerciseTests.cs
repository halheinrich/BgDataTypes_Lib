using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// SPEC-scoring §3's acceptance requirement for the Too Good derivation
/// (halheinrich/backgammon#86): "an exercise check against XG-labelled
/// corpus verdicts … before the predicate is trusted — the derivation is
/// believed trivial, and belief is not verification." The synthesized pins
/// in <see cref="DecisionDataClaimDerivationTests"/> prove the predicate
/// against chosen equities; this check proves real converted data actually
/// reaches the Too Good branch — a predicate no real record ever satisfies
/// would pass every synthetic pin while being vacuous in production. Since
/// the 2026-09-02 amendment (halheinrich/backgammon#187: Too Good requires
/// the pass) it also counts that no corpus position derives the retired
/// Too Good / Take pair.
///
/// <para>
/// Local-only by design (the AGENTS.md TestData rule): the corpus under the
/// umbrella <c>TestData/BgDecisionData/</c> is gitignored, changes over
/// time, and may be absent entirely — on an empty or missing corpus the
/// check passes vacuously, which is why it cannot gate and is not the
/// gating pin. Files are read leniently (mixed-era document shapes and
/// property casings; unreadable files skipped) because the target here is
/// the shipped predicate over the stored equities, not the wire read path —
/// that path has its own gate in <see cref="BgDataTypesJsonContextTests"/>.
/// </para>
/// </summary>
public class TooGoodCorpusExerciseTests
{
    private static readonly string CorpusDir = Path.Combine(
        AppContext.BaseDirectory, "TestData", "BgDecisionData");

    // The corpus documents are per-match sample bundles: three arrays of
    // decision records, each embedding a "decision" object carrying the
    // DecisionData fields the predicate reads.
    private static readonly string[] SampleArrays =
        ["playErrorSamples", "doubleErrorSamples", "takeErrorSamples"];

    [Fact]
    public void TooGoodPredicate_IsExercisedByTheLocalCorpus()
    {
        if (!Directory.Exists(CorpusDir))
            return;   // no local corpus — vacuous by design

        int cubeDecisions = 0;
        int tooGood = 0;
        int tooGoodTake = 0;

        foreach (var file in Directory.EnumerateFiles(CorpusDir, "*.json"))
        {
            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(File.ReadAllText(file));
            }
            catch (JsonException)
            {
                continue;   // not a readable corpus document; skip
            }

            using (document)
            {
                foreach (var decision in CubeDecisionsOf(document))
                {
                    cubeDecisions++;

                    // Every corpus cube decision must derive a defined
                    // claim without throwing — the sweep half of the check.
                    var pair = decision.BestClaimPair;
                    Assert.True(Enum.IsDefined(pair.Claim));

                    if (pair.Claim == CubeClaim.TooGood)
                        tooGood++;
                    if (pair == CubeClaimPair.TooGoodTake)
                        tooGoodTake++;
                }
            }
        }

        // The retired cell, counted over real data: since the 2026-09-02
        // amendment (halheinrich/backgammon#187) Too Good requires the pass,
        // so no corpus position may derive Too Good / Take. Vacuous-safe —
        // a zero count on an empty corpus is the assertion holding, not
        // dodging it — which is why it sits ahead of the vacuous return.
        Assert.Equal(0, tooGoodTake);

        if (cubeDecisions == 0)
            return;   // corpus carries no readable cube decisions — vacuous

        // The forcing function (the XgFilter_Lib corpus-oracle pattern): a
        // zero means the predicate is unexercised by real data, so a source
        // file containing a too-good position should be added to the local
        // corpus rather than this check weakened.
        Assert.True(tooGood > 0,
            $"the local corpus holds {cubeDecisions} cube decisions but none derives " +
            "CubeClaim.TooGood — the predicate is unexercised by real data; convert a " +
            "too-good position (e.g. FixtureFiles/TooGoodAndTake.xgp) into the corpus");
    }

    private static IEnumerable<DecisionData> CubeDecisionsOf(JsonDocument document)
    {
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            yield break;

        foreach (var arrayName in SampleArrays)
        {
            if (!TryGetPropertyAnyCase(document.RootElement, arrayName, out var samples)
                || samples.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var sample in samples.EnumerateArray())
            {
                if (sample.ValueKind != JsonValueKind.Object
                    || !TryGetPropertyAnyCase(sample, "decision", out var decision)
                    || decision.ValueKind != JsonValueKind.Object)
                    continue;

                if (!TryGetPropertyAnyCase(decision, "isCube", out var isCube)
                    || isCube.ValueKind != JsonValueKind.True)
                    continue;

                if (!TryGetDouble(decision, "noDoubleEquity", out var noDoubleEquity)
                    || !TryGetDouble(decision, "doubleTakeEquity", out var doubleTakeEquity))
                    continue;

                yield return new DecisionData
                {
                    IsCube = true,
                    NoDoubleEquity = noDoubleEquity,
                    DoubleTakeEquity = doubleTakeEquity
                };
            }
        }
    }

    // Corpus files span writer eras with differing property casings; the
    // predicate's inputs are looked up case-insensitively so no era is
    // silently dropped.
    private static bool TryGetPropertyAnyCase(
        JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool TryGetDouble(JsonElement element, string name, out double value)
    {
        if (TryGetPropertyAnyCase(element, name, out var property)
            && property.ValueKind == JsonValueKind.Number)
        {
            value = property.GetDouble();
            return true;
        }

        value = 0;
        return false;
    }
}
