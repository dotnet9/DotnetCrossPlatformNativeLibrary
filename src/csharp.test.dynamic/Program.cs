using System;
using System.Runtime.InteropServices;

namespace csharp.test.dynamic;

internal static class NativeLibrary
{
    private const string DllName = "TimeMeaning.dll";

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.UTF8)]
    private delegate IntPtr GetTimeMeaningDelegate(int timestampSecond);

    private static GetTimeMeaningDelegate? _getTimeMeaning;

    static NativeLibrary()
    {
        string dllPath = Path.Combine(AppContext.BaseDirectory, DllName);
        IntPtr handle = LoadLibrary(dllPath);
        if (handle == IntPtr.Zero)
        {
            throw new DllNotFoundException($"无法加载动态库: {dllPath}");
        }

        IntPtr funcPtr = GetProcAddress(handle, "GetTimeMeaning");
        if (funcPtr == IntPtr.Zero)
        {
            throw new EntryPointNotFoundException("无法找到函数: GetTimeMeaning");
        }

        _getTimeMeaning = Marshal.GetDelegateForFunctionPointer<GetTimeMeaningDelegate>(funcPtr);
    }

    public static string GetTimeMeaningString(int timestampSecond)
    {
        if (_getTimeMeaning == null)
        {
            throw new InvalidOperationException("动态库未正确加载");
        }

        IntPtr ptr = _getTimeMeaning(timestampSecond);
        return Marshal.PtrToStringUTF8(ptr) ?? string.Empty;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
}

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== C# 动态调用 (LoadLibrary) 测试 ===\n");

        try
        {
            int[] testTimestamps = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, -1, 100, 1000];

            foreach (int ts in testTimestamps)
            {
                string meaning = NativeLibrary.GetTimeMeaningString(ts);
                Console.WriteLine($"时间戳 {ts,6} -> {meaning}");
            }

            Console.WriteLine("\n测试完成！");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误: {ex.Message}");
        }
    }
}