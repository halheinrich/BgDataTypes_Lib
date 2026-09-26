namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The allocation pins' measurement (<see cref="AllocationProbe"/>), pinned on
/// its own: it reads a path that allocates nothing as nothing, a one-off
/// allocation as nothing, and a path that allocates on its calls as what it
/// allocates. The pins are only as good as this, so both halves are
/// deterministic here rather than left to a race the runtime rarely runs.
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
    public void APathThatAllocatesOnEveryCall_IsMeasured()
    {
        long allocated = AllocationProbe.SteadyStateBytes(() => _kept = new byte[16]);

        Assert.True(allocated >= AllocationProbe.CallsPerWindow * 16L, $"measured {allocated}");
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
