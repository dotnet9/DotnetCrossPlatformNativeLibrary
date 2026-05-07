using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AvaloniaManagedLibraryTest.Lib;

public static class GridPathCounterCal
{
    [UnmanagedCallersOnly(EntryPoint = "Cal", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int Cal(int left, int right)
    {
        var width = Math.Abs(left) % 12;
        var height = Math.Abs(right) % 12;
        var grid = new int[width + 1, height + 1];

        for (var x = 0; x <= width; x++)
        {
            grid[x, 0] = 1;
        }

        for (var y = 0; y <= height; y++)
        {
            grid[0, y] = 1;
        }

        for (var x = 1; x <= width; x++)
        {
            for (var y = 1; y <= height; y++)
            {
                grid[x, y] = grid[x - 1, y] + grid[x, y - 1];
            }
        }

        return grid[width, height];
    }
}
