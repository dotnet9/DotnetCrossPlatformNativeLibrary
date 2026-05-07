using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AvaloniaManagedLibraryTest.Lib;

public static class PrimeDistanceCal
{
    [UnmanagedCallersOnly(EntryPoint = "Cal", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int Cal(int left, int right)
    {
        var first = NextPrime(Math.Abs(left));
        var second = NextPrime(Math.Abs(right));
        return Math.Abs(first - second);

        static int NextPrime(int value)
        {
            if (value <= 2)
            {
                return 2;
            }

            var candidate = value % 2 == 0 ? value + 1 : value;
            while (!IsPrime(candidate))
            {
                candidate += 2;
            }

            return candidate;
        }

        static bool IsPrime(int value)
        {
            if (value < 2)
            {
                return false;
            }

            if (value == 2)
            {
                return true;
            }

            if (value % 2 == 0)
            {
                return false;
            }

            for (var factor = 3; factor <= value / factor; factor += 2)
            {
                if (value % factor == 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
