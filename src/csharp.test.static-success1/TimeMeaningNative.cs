using System.Runtime.InteropServices;

namespace csharp.test.static_;

internal static class TimeMeaningNative
{
#if PLATFORM_WIN_X64 || PLATFORM_WIN_X86
const string DLL = "Lib/TimeMeaning.dll";
#elif PLATFORM_LINUX_X64 || PLATFORM_LINUX_ARM64
const string DLL = "Lib/libTimeMeaning.so";
#else
    const string DLL = "Lib/TimeMeaning.dll";
#endif
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern IntPtr GetTimeMeaning(int timestampSecond);

    public static string GetTimeMeaningString(int timestampSecond)
    {
        var ptr = GetTimeMeaning(timestampSecond);
        return Marshal.PtrToStringUTF8(ptr) ?? string.Empty;
    }
}