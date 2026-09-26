namespace BgDataTypes_Lib.TestSupport;

/// <summary>
/// How an allocation pin measures a path: what it allocates in its steady
/// state, on the test thread, immune to a one-off allocation the runtime
/// makes there. Every pin that says "allocates nothing" — in this repository
/// and in every consumer's tests — measures through here, so the method is
/// stated once.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why a single measured loop is not enough.</b> A pin used to read
/// <see cref="GC.GetAllocatedBytesForCurrentThread"/> once around one loop,
/// after one warm-up call. Any allocation on the thread inside that window
/// failed it, including one the path did not make: a first call's work the
/// warm-up missed, or one the runtime makes once on the calling thread. One
/// such pin failed once, in a run whose change touched nothing it measures
/// (halheinrich/backgammon#273); a gating test that fails intermittently is a
/// defect in the test.
/// </para>
/// <para>
/// <b>The measurement.</b> The path is first run for a whole window's worth of
/// calls, so every first-call cost falls outside the measurement. Then up to
/// <see cref="Windows"/> windows of <see cref="CallsPerWindow"/> calls each are
/// measured, and the fewest bytes any window allocated is the result — zero as
/// soon as one window allocates nothing, so the probe stops there. A one-off
/// allocation lands in at most one window, so it cannot make every window
/// allocate; a path that allocates on its calls allocates in every window, so
/// it still fails. The probe's own tests (this repository's
/// <c>AllocationProbeTests</c>) pin both halves.
/// </para>
/// <para>
/// <b>Shared, not copied</b> (halheinrich/backgammon#273). A consumer's
/// copy had already drifted from this one — it dropped the early exit — so
/// the probe lives with what consumers' tests share, beside
/// <see cref="TestRecords"/>, and a consumer's test project references it
/// rather than restating it. It depends on nothing but the runtime.
/// </para>
/// </remarks>
public static class AllocationProbe
{
    /// <summary>The calls in the warm-up and in each measured window.</summary>
    public const int CallsPerWindow = 1000;

    /// <summary>The most windows measured; the probe stops at the first that allocates nothing.</summary>
    public const int Windows = 8;

    /// <summary>
    /// The fewest bytes <paramref name="call"/> allocated on this thread in any
    /// measured window of <see cref="CallsPerWindow"/> calls, after a warm-up
    /// of as many calls — see the class remarks.
    /// </summary>
    /// <param name="call">One call of the path; it keeps its result observable (a sink it adds to).</param>
    /// <returns>The path's steady-state allocation per window, in bytes: 0 for a path that allocates nothing.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="call"/> is <see langword="null"/>.</exception>
    public static long SteadyStateBytes(Action call)
    {
        ArgumentNullException.ThrowIfNull(call);

        for (int i = 0; i < CallsPerWindow; i++)
            call();

        long fewest = long.MaxValue;
        for (int window = 0; window < Windows && fewest != 0; window++)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < CallsPerWindow; i++)
                call();
            fewest = Math.Min(fewest, GC.GetAllocatedBytesForCurrentThread() - before);
        }
        return fewest;
    }
}
