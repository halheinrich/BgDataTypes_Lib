using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace BgDataTypes_Lib.Benchmarks;

/// <summary>
/// Head-to-head construction cost for every way to build a <see cref="Play"/>
/// whose moves are already in hand: the incremental <see cref="Play.Add"/>
/// spelling, the fixed-arity <c>Play.Create</c> overloads, the general-arity
/// <see cref="Play.Create(ReadOnlySpan{Move})"/>, and a collection
/// expression (which lowers to the span overload via
/// <see cref="System.Runtime.CompilerServices.CollectionBuilderAttribute"/>).
///
/// <para>
/// The property under test is <em>parity</em>: <c>Create</c> is the
/// intent-level surface and reads better than a run of <c>Add</c> calls, so
/// at a literal call site it may only cost what <c>Add</c> costs. It did not
/// always — looping over the <c>params ReadOnlySpan&lt;Move&gt;</c> calling
/// <c>Add</c> left <c>Play.Count</c> unknown per element, the JIT could not
/// fold the slot switch, and BgMoveGen measured 1.19–1.48x in its generator
/// hot paths and reverted the adoption (halheinrich/backgammon#137). These
/// benchmarks are the standing guard on that parity.
/// </para>
///
/// <para>
/// Cases are grouped by move count with the <c>Add</c> spelling as each
/// group's baseline, so the reported <c>Ratio</c> reads directly as the
/// acceptance number. <b>The fixed-arity rows are gated at parity; the span
/// and collection-expression rows are documented, not gated</b> — both
/// materialise an argument buffer in the <em>caller</em> before the call,
/// a cost outside the method and not one <c>Create</c> can optimise away.
/// The <c>Add</c> baselines are themselves regression guards: routing
/// <c>Add</c> through the shared slot-write seam must not cost it its folded
/// codegen (an early attempt did, by 8x).
/// </para>
///
/// <para>
/// <see cref="MemoryDiagnoserAttribute"/> is on because <see cref="Play"/> is
/// a stack-resident value type and every case here must allocate exactly
/// nothing per operation. A construction path that moves the
/// <c>Allocated</c> column is a regression whether or not the clock notices.
/// </para>
///
/// <para>
/// <b>Read only the sibling comparison, never one run against another.</b>
/// This development machine runs eXtremeGammon rollouts in the background;
/// they inflate absolute means by up to 1.8x. Grouping the variants as
/// sibling <c>[Benchmark]</c> methods in one process is what makes the
/// comparison valid — identical load hits every row.
/// </para>
/// </summary>
[MemoryDiagnoser]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class PlayConstructionBenchmarks
{
    /// <summary>
    /// Plays built per benchmark operation. A single construction is a
    /// handful of nanoseconds — below the noise floor of BenchmarkDotNet's
    /// per-invocation overhead — so each operation builds a batch, which is
    /// also the shape the real caller has (a generator filling a result
    /// list).
    /// </summary>
    private const int Reps = 1000;

    // Instance fields, not locals or constants: the moves must be opaque to
    // the JIT, or it would constant-fold whole constructions away and the
    // benchmark would measure nothing.
    private Move _m0, _m1, _m2, _m3;

    // Pre-built arrays for the span overload's documented use case — a
    // caller that already holds the moves in a span or array, and so pays no
    // buffer materialisation of its own.
    private Move[] _a1 = null!, _a2 = null!, _a3 = null!, _a4 = null!;

    // Pre-allocated destination. Storing each play keeps it live (no
    // dead-code elimination) without allocating per operation, so the
    // MemoryDiagnoser reading stays a clean zero.
    private Play[] _sink = null!;

    /// <summary>Fills the move set, the source arrays, and the destination buffer once.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _m0 = new Move(8, 5);
        _m1 = new Move(8, 5);
        _m2 = new Move(6, 3);
        _m3 = new Move(6, 3);
        _a1 = [_m0];
        _a2 = [_m0, _m1];
        _a3 = [_m0, _m1, _m2];
        _a4 = [_m0, _m1, _m2, _m3];
        _sink = new Play[Reps];
    }

    /// <summary>One move via the incremental primitives — the baseline, and a guard that Add's own codegen has not moved.</summary>
    [Benchmark(Baseline = true), BenchmarkCategory("OneMove")]
    public Play[] OneMove_Add()
    {
        for (int i = 0; i < Reps; i++)
        {
            var play = new Play();
            play.Add(_m0);
            _sink[i] = play;
        }
        return _sink;
    }

    /// <summary>One move via the fixed-arity overload — the gated row.</summary>
    [Benchmark, BenchmarkCategory("OneMove")]
    public Play[] OneMove_Create()
    {
        for (int i = 0; i < Reps; i++)
            _sink[i] = Play.Create(_m0);
        return _sink;
    }

    /// <summary>One move via the span overload from an existing array — documented, not gated.</summary>
    [Benchmark, BenchmarkCategory("OneMove")]
    public Play[] OneMove_CreateSpan()
    {
        for (int i = 0; i < Reps; i++)
            _sink[i] = Play.Create(_a1);
        return _sink;
    }

    /// <summary>One move via a collection expression — documented, not gated.</summary>
    [Benchmark, BenchmarkCategory("OneMove")]
    public Play[] OneMove_CollectionExpression()
    {
        for (int i = 0; i < Reps; i++)
            _sink[i] = [_m0];
        return _sink;
    }

    /// <summary>Two moves via the incremental primitives — the baseline, and a guard that Add's own codegen has not moved.</summary>
    [Benchmark(Baseline = true), BenchmarkCategory("TwoMoves")]
    public Play[] TwoMoves_Add()
    {
        for (int i = 0; i < Reps; i++)
        {
            var play = new Play();
            play.Add(_m0);
            play.Add(_m1);
            _sink[i] = play;
        }
        return _sink;
    }

    /// <summary>Two moves via the fixed-arity overload — the gated row.</summary>
    [Benchmark, BenchmarkCategory("TwoMoves")]
    public Play[] TwoMoves_Create()
    {
        for (int i = 0; i < Reps; i++)
            _sink[i] = Play.Create(_m0, _m1);
        return _sink;
    }

    /// <summary>Two moves via the span overload from an existing array — documented, not gated.</summary>
    [Benchmark, BenchmarkCategory("TwoMoves")]
    public Play[] TwoMoves_CreateSpan()
    {
        for (int i = 0; i < Reps; i++)
            _sink[i] = Play.Create(_a2);
        return _sink;
    }

    /// <summary>Two moves via a collection expression — documented, not gated.</summary>
    [Benchmark, BenchmarkCategory("TwoMoves")]
    public Play[] TwoMoves_CollectionExpression()
    {
        for (int i = 0; i < Reps; i++)
            _sink[i] = [_m0, _m1];
        return _sink;
    }

    /// <summary>Three moves via the incremental primitives — the baseline, and a guard that Add's own codegen has not moved.</summary>
    [Benchmark(Baseline = true), BenchmarkCategory("ThreeMoves")]
    public Play[] ThreeMoves_Add()
    {
        for (int i = 0; i < Reps; i++)
        {
            var play = new Play();
            play.Add(_m0);
            play.Add(_m1);
            play.Add(_m2);
            _sink[i] = play;
        }
        return _sink;
    }

    /// <summary>Three moves via the fixed-arity overload — the gated row.</summary>
    [Benchmark, BenchmarkCategory("ThreeMoves")]
    public Play[] ThreeMoves_Create()
    {
        for (int i = 0; i < Reps; i++)
            _sink[i] = Play.Create(_m0, _m1, _m2);
        return _sink;
    }

    /// <summary>Three moves via the span overload from an existing array — documented, not gated.</summary>
    [Benchmark, BenchmarkCategory("ThreeMoves")]
    public Play[] ThreeMoves_CreateSpan()
    {
        for (int i = 0; i < Reps; i++)
            _sink[i] = Play.Create(_a3);
        return _sink;
    }

    /// <summary>Three moves via a collection expression — documented, not gated.</summary>
    [Benchmark, BenchmarkCategory("ThreeMoves")]
    public Play[] ThreeMoves_CollectionExpression()
    {
        for (int i = 0; i < Reps; i++)
            _sink[i] = [_m0, _m1, _m2];
        return _sink;
    }

    /// <summary>Four moves via the incremental primitives — the baseline, and a guard that Add's own codegen has not moved.</summary>
    [Benchmark(Baseline = true), BenchmarkCategory("FourMoves")]
    public Play[] FourMoves_Add()
    {
        for (int i = 0; i < Reps; i++)
        {
            var play = new Play();
            play.Add(_m0);
            play.Add(_m1);
            play.Add(_m2);
            play.Add(_m3);
            _sink[i] = play;
        }
        return _sink;
    }

    /// <summary>Four moves via the fixed-arity overload — the gated row.</summary>
    [Benchmark, BenchmarkCategory("FourMoves")]
    public Play[] FourMoves_Create()
    {
        for (int i = 0; i < Reps; i++)
            _sink[i] = Play.Create(_m0, _m1, _m2, _m3);
        return _sink;
    }

    /// <summary>Four moves via the span overload from an existing array — documented, not gated.</summary>
    [Benchmark, BenchmarkCategory("FourMoves")]
    public Play[] FourMoves_CreateSpan()
    {
        for (int i = 0; i < Reps; i++)
            _sink[i] = Play.Create(_a4);
        return _sink;
    }

    /// <summary>Four moves via a collection expression — documented, not gated.</summary>
    [Benchmark, BenchmarkCategory("FourMoves")]
    public Play[] FourMoves_CollectionExpression()
    {
        for (int i = 0; i < Reps; i++)
            _sink[i] = [_m0, _m1, _m2, _m3];
        return _sink;
    }
}
