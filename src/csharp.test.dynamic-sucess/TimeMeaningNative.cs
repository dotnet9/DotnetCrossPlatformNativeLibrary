using System.Runtime.InteropServices;

namespace csharp.test.dynamic;

internal static class TimeMeaningNative
{
    private static readonly string DllName = OperatingSystem.IsWindows()
        ? "TimeMeaning.dll"
        : "libTimeMeaning.so";

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private delegate IntPtr GetTimeMeaningDelegate(int timestampSecond);

    private static GetTimeMeaningDelegate? _getTimeMeaning;
    private static IntPtr _handle;

    static TimeMeaningNative()
    {
        var dllPath = Path.Combine(AppContext.BaseDirectory, "Lib", DllName);
        _handle = NativeLibrary.Load(dllPath);
        var funcPtr = NativeLibrary.GetExport(_handle, "GetTimeMeaning");
        _getTimeMeaning = Marshal.GetDelegateForFunctionPointer<GetTimeMeaningDelegate>(funcPtr);
    }

    public static void Free()
    {
        if (_handle == IntPtr.Zero) return;
        NativeLibrary.Free(_handle);
        _handle = IntPtr.Zero;
    }

    public static string GetTimeMeaningString(int timestampSecond)
    {
        if (_getTimeMeaning == null)
        {
            throw new InvalidOperationException("动态库未正确加载");
        }

        var ptr = _getTimeMeaning(timestampSecond);
        return Marshal.PtrToStringUTF8(ptr) ?? string.Empty;
    }
}