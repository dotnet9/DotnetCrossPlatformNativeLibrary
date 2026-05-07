using System;
using Avalonia;
using Avalonia.Controls;
using ReactiveUI.Avalonia;

namespace AvaloniaDynamicLibraryTest;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var exitCode = BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, ShutdownMode.OnMainWindowClose);
        Environment.Exit(exitCode);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI(_ => { })
            .RegisterReactiveUIViewsFromEntryAssembly();
    }
}
