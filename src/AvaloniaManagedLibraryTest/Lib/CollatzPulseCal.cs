using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AvaloniaManagedLibraryTest.Lib;

public static class CollatzPulseCal
{
    [UnmanagedCallersOnly(EntryPoint = "Cal", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int Cal(int left, int right)
    {
        var value = Math.Abs(left) + Math.Abs(right);
        if (value == 0)
        {
            return 0;
        }

        var steps = 0;
        while (value != 1 && steps < 512)
        {
            value = value % 2 == 0 ? value / 2 : (value * 3) + 1;
            steps++;
        }

        return steps;
    }
}
