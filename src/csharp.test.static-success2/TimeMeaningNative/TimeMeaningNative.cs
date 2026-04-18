using System.Runtime.InteropServices;

namespace TimeMeaningNative;

public static class TimeMeaningApi
{
    const string DLL = "Lib/TimeMeaning";
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern IntPtr GetTimeMeaning(int timestampSecond);

    public static string GetTimeMeaningString(int timestampSecond)
    {
        var ptr = GetTimeMeaning(timestampSecond);
        return Marshal.PtrToStringUTF8(ptr) ?? string.Empty;
    }
}