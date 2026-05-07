using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AvaloniaManagedLibraryTest.Lib;

public static class FibonacciWindowCal
{
    [UnmanagedCallersOnly(EntryPoint = "Cal", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int Cal(int left, int right)
    {
        var index = Math.Abs(left + right) % 47;
        var previous = 0;
        var current = 1;

        for (var i = 0; i < index; i++)
        {
            var next = previous + current;
            previous = current;
            current = next;
        }

        return previous;
    }
}
