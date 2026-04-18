using System.Runtime.InteropServices;

namespace TimeMeaningNative;

public static class TimeMeaningApi
{
#if WIN_X64 || WIN_X86
const string DLL = "Lib/TimeMeaning.dll";
#elif LINUX_X64 || LINUX_ARM64
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