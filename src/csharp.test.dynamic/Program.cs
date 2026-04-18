using CodeWF.Log.Core;
using csharp.test.dynamic;

const string platform =
    "Unknown";

#if WIN_X64
Logger.Info($"=== C# 动态调用 (NativeLibrary) 测试 [Windows X64] ===\n");
#elif WIN_X86
Logger.Info($"=== C# 动态调用 (NativeLibrary) 测试 [Windows X86] ===\n");
#elif LINUX_X64
Logger.Info($"=== C# 动态调用 (NativeLibrary) 测试 [Linux X64] ===\n");
#elif LINUX_ARM64
Logger.Info($"=== C# 动态调用 (NativeLibrary) 测试 [Linux ARM64] ===\n");
#else
Logger.Info($"=== C# 动态调用 (NativeLibrary) 测试 [{platform}] ===\n");
#endif

try
{
    int[] testTimestamps = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, -1, 100, 1000];

    foreach (var ts in testTimestamps)
    {
        var meaning = TimeMeaningNative.GetTimeMeaningString(ts);
        Logger.Info($"时间戳 {ts,6} -> {meaning}");
    }

    Logger.Info("\n测试完成！");
}
catch (Exception ex)
{
    Logger.Error($"错误: {ex.Message}");
}
finally
{
    TimeMeaningNative.Free();
}

Console.ReadKey();