using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AvaloniaManagedLibraryTest.Services;

public sealed class NativeLibraryPathProvider : INativeLibraryPathProvider
{
    private const string LibraryDirectoryName = "Lib";

    public string LibraryDirectory { get; } = ResolveLibraryDirectory();

    private static string ResolveLibraryDirectory()
    {
        var appLocalDirectory = Path.Combine(AppContext.BaseDirectory, LibraryDirectoryName);
        if (Directory.Exists(appLocalDirectory))
        {
            return appLocalDirectory;
        }

        var repoRoot = FindRepositoryRoot(AppContext.BaseDirectory);
        if (repoRoot is not null)
        {
            return Path.Combine(
                repoRoot,
                "publish",
                "avalonia-managed",
                GetCurrentRuntimeIdentifier(),
                "AvaloniaManagedLibraryTest",
                LibraryDirectoryName);
        }

        return appLocalDirectory;
    }

    private static string GetCurrentRuntimeIdentifier()
    {
        var architecture = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm64 => "arm64",
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm => "arm",
            _ => RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()
        };

        if (OperatingSystem.IsWindows())
        {
            return $"win-{architecture}";
        }

        if (OperatingSystem.IsLinux())
        {
            return $"linux-{architecture}";
        }

        if (OperatingSystem.IsMacOS())
        {
            return $"osx-{architecture}";
        }

        return RuntimeInformation.RuntimeIdentifier;
    }

    private static string? FindRepositoryRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Cross.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
