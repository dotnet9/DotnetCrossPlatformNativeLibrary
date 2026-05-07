using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AvaloniaManagedLibraryTest.Lib;

public static class BitAvalancheCal
{
    [UnmanagedCallersOnly(EntryPoint = "Cal", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int Cal(int left, int right)
    {
        unchecked
        {
            var value = left;
            value ^= right + unchecked((int)0x9E3779B9);
            value = (value ^ (value >> 16)) * unchecked((int)0x85EBCA6B);
            value = (value ^ (value >> 13)) * unchecked((int)0xC2B2AE35);
            return value ^ (value >> 16);
        }
    }
}
