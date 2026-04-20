using CodeWF.Log.Core;
using TimeMeaningNative;

#if PLATFORM_WIN_X64
var platform = "Windows X64";
#elif PLATFORM_WIN_X86
var platform = "Windows X86";
#elif PLATFORM_LINUX_X64
var platform = "Linux X64";
#elif PLATFORM_LINUX_ARM64
var platform = "Linux ARM64";
#else
var platform = "Unknown";
#endif

Logger.Info($"=== C# 静态调用C++库 (DllImport) 测试 [{platform}]，子工程使用宏条件编译指定C++库路径，允许不同平台的库路径不同 ===\n");

try
{
    int[] testTimestamps = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, -1, 100, 1000];

    foreach (var ts in testTimestamps)
    {
        var meaning = TimeMeaningApi.GetTimeMeaningString(ts);
        Logger.Info($"时间戳 {ts,6} -> {meaning}");
    }

    Logger.Info("\n测试完成！");
}
catch (Exception ex)
{
    Logger.Error($"错误: {ex.Message}");
}

Console.ReadKey();