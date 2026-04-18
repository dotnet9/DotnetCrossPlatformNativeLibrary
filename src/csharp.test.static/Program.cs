using System;
using System.Runtime.InteropServices;

namespace csharp.test.static_;

internal static class NativeLibrary
{
    private const string DllName = "TimeMeaning.dll";

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.UTF8)]
    private static extern IntPtr GetTimeMeaning(int timestampSecond);

    public static string GetTimeMeaningString(int timestampSecond)
    {
        IntPtr ptr = GetTimeMeaning(timestampSecond);
        return Marshal.PtrToStringUTF8(ptr) ?? string.Empty;
    }
}

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== C# 静态调用 (DllImport) 测试 ===\n");

        int[] testTimestamps = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, -1, 100, 1000];

        foreach (int ts in testTimestamps)
        {
            string meaning = NativeLibrary.GetTimeMeaningString(ts);
            Console.WriteLine($"时间戳 {ts,6} -> {meaning}");
        }

        Console.WriteLine("\n测试完成！");
    }
}