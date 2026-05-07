using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AvaloniaManagedLibraryTest.Lib;

public static class GreatestCommonDivisorCal
{
    [UnmanagedCallersOnly(EntryPoint = "Cal", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int Cal(int left, int right)
    {
        left = Math.Abs(left);
        right = Math.Abs(right);

        while (right != 0)
        {
            var next = left % right;
            left = right;
            right = next;
        }

        return left;
    }
}
