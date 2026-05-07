using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AvaloniaManagedLibraryTest.Lib;

public static class WeightedSignalCal
{
    [UnmanagedCallersOnly(EntryPoint = "Cal", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int Cal(int left, int right)
    {
        unchecked
        {
            return (left * 7) + (right * 11);
        }
    }
}
