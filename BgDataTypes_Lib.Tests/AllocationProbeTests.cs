namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The allocation pins' measurement (<see cref="AllocationProbe"/>, shipped in
/// the test-support project for every repository's pins), pinned on its own,
/// here where it is written: it reads a path that allocates nothing as
/// nothing, a one-off allocation as nothing, and a path that allocates on its
/// calls as what it allocates. The pins are only as good as this, so both
/// halves are deterministic here rather than left to a race the runtime
/// rarely runs.
/// </summary>
public class AllocationProbeTests
{
    // A sink the paths below write, so no allocation can be elided.
    private static object? _kept;

    [Fact]
    public void APathThatAllocatesNothing_MeasuresNothing()
    {
        int sink = 0;

        Assert.Equal(0, AllocationProbe.SteadyStateBytes(() => sink++));
        Assert.True(sink > 0);
    }

    [Fact]
    public void TheProbe_StopsAtTheFirstWindowThatAllocatesNothing_AndOtherwiseMeasuresEveryWindow()
    {
        // Added when the probe moved to the test-support project: the early
        // exit is part of the measurement as stated — a clean window ends it —
        // and the one thing a consumer's copy had dropped. The result alone
        // cannot show it (the fewest bytes over every window is the same
        // number), so the calls the probe makes are counted. A path that
        // allocates on its calls gets the warm-up and every window. A path
        // that allocates nothing gets the warm-up and whole windows up to the
        // first clean one — fewer than every window. Not "exactly one
        // window": a one-off the runtime makes on the thread could dirty the
        // first, which is what the probe exists to absorb.
        const int everyWindow = (1 + AllocationProbe.Windows) * AllocationProbe.CallsPerWindow;

        int allocating = 0;
        AllocationProbe.SteadyStateBytes(() => { allocating++; _kept = new byte[16]; });
        Assert.Equal(everyWindow, allocating);

        int clean = 0;
        AllocationProbe.SteadyStateBytes(() => clean++);
        AssertStoppedAtACleanWindow(clean, atLeast: 2 * AllocationProbe.CallsPerWindow);

        // A one-off in the first measured window: a later one is clean, and
        // the probe stops there.
        int oneOff = 0;
        AllocationProbe.SteadyStateBytes(() =>
        {
            if (++oneOff == AllocationProbe.CallsPerWindow + 1)
                _kept = new byte[4096];
        });
        AssertStoppedAtACleanWindow(oneOff, atLeast: 3 * AllocationProbe.CallsPerWindow);

        static void AssertStoppedAtACleanWindow(int calls, int atLeast)
        {
            Assert.True(calls < everyWindow, $"{calls} calls: the probe measured every window");
            Assert.True(calls >= atLeast, $"{calls} calls: fewer than the warm-up and the windows before a clean one");
            Assert.Equal(0, calls % AllocationProbe.CallsPerWindow);
        }
    }

    [Fact]
    public void ANullPath_IsRefused()
    {
        Assert.Equal("call", Assert.Throws<ArgumentNullException>(() => AllocationProbe.SteadyStateBytes(null!)).ParamName);
    }

    [Fact]
    public void APathThatAllocatesOnEveryCall_IsMeasured()
    {
        long allocated = AllocationProbe.SteadyStateBytes(() => _kept = new byte[16]);

        Assert.True(allocated >= AllocationProbe.CallsPerWindow * 16L, $"measured {allocated}");
    }

    [Fact]
    public void APathThatAllocatesOnEveryCall_IsMeasuredExactly()
    {
        // Ported as it stands from BgMoveGen's copy of the probe's tests
        // (halheinrich/backgammon#273), so the pins are complete where the
        // probe is written: two allocations a call measure exactly twice one.
        long array = AllocationProbe.SteadyStateBytes(() => _kept = new byte[16]);
        long twice = AllocationProbe.SteadyStateBytes(() => { _kept = new byte[16]; _kept = new byte[16]; });

        Assert.True(array >= AllocationProbe.CallsPerWindow * 16L, $"measured {array}");
        Assert.Equal(2 * array, twice);
    }

    [Fact]
    public void APathThatAllocatesOnSomeCalls_IsMeasured()
    {
        // One call in a hundred: still in every window, so still caught.
        int calls = 0;

        long allocated = AllocationProbe.SteadyStateBytes(() =>
        {
            if (++calls % 100 == 0)
                _kept = new byte[16];
        });

        Assert.True(allocated > 0);
    }

    [Theory]
    [InlineData(1)]                                             // the warm-up's first call
    [InlineData(AllocationProbe.CallsPerWindow + 1)]            // the first measured window's first call
    [InlineData(AllocationProbe.CallsPerWindow * 2 - 1)]        // its last
    [InlineData(AllocationProbe.CallsPerWindow * 3 + 500)]      // a later window's
    public void AOneOffAllocation_WhereverItLands_IsNotCounted(int onCall)
    {
        // The shape of the runtime's one-off allocations on the test thread,
        // which the old single-window measurement counted as the path's own.
        int calls = 0;

        long allocated = AllocationProbe.SteadyStateBytes(() =>
        {
            if (++calls == onCall)
                _kept = new byte[4096];
        });

        Assert.Equal(0, allocated);
    }

    [Fact]
    public void AnAllocationInEachOfTheFirstWindows_IsNotCounted_WhileAWindowStaysClean()
    {
        // Several one-offs, one in each of the first windows but the last:
        // the last window is clean, so the path's steady state is nothing.
        int calls = 0;

        long allocated = AllocationProbe.SteadyStateBytes(() =>
        {
            int call = ++calls;
            if (call > AllocationProbe.CallsPerWindow
                && call <= AllocationProbe.CallsPerWindow * AllocationProbe.Windows
                && call % AllocationProbe.CallsPerWindow == 1)
                _kept = new byte[64];
        });

        Assert.Equal(0, allocated);
    }
}
