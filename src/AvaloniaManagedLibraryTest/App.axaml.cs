using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using AvaloniaManagedLibraryTest.Services;
using AvaloniaManagedLibraryTest.ViewModels;
using AvaloniaManagedLibraryTest.Views;
using CodeWF.Log.Core;
using Prism.DryIoc;
using Prism.Ioc;

namespace AvaloniaManagedLibraryTest;

public partial class App : PrismApplication
{
    private bool _isExiting;
    private bool _nativeLibrariesReleased;

    public override void Initialize()
    {
        ConfigureLogger();
        AvaloniaXamlLoader.Load(this);
        base.Initialize();
    }

    protected override AvaloniaObject CreateShell()
    {
        return Container.Resolve<MainWindow>();
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<INativeLibraryPathProvider, NativeLibraryPathProvider>();
        containerRegistry.RegisterSingleton<IDynamicLibraryInvoker, NativeLibraryInvoker>();
        containerRegistry.Register<LibraryInvocationViewModel>();
        containerRegistry.Register<MainWindowViewModel>();
        containerRegistry.Register<MainWindow>();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        base.OnFrameworkInitializationCompleted();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
            desktop.Exit += (_, _) =>
            {
                ReleaseNativeLibraries();
                _ = Logger.FlushAsync();
            };
            StartExitWatcher(desktop);
        }

        Logger.Info("Avalonia 本机库测试程序已启动。");
    }

    private void StartExitWatcher(IClassicDesktopStyleApplicationLifetime desktop)
    {
        // 某些窗口关闭路径不会立即触发进程退出，这里补一次可见窗口检查。
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        timer.Tick += (_, _) =>
        {
            if (desktop.MainWindow is null || desktop.MainWindow.IsVisible)
            {
                return;
            }

            if (desktop.Windows.Any(window => window.IsVisible))
            {
                return;
            }

            ExitProcess();
        };

        timer.Start();
    }

    private void ExitProcess()
    {
        if (_isExiting)
        {
            return;
        }

        _isExiting = true;
        ReleaseNativeLibraries();
        Logger.FlushAsync().GetAwaiter().GetResult();
        Environment.Exit(0);
    }

    private void ReleaseNativeLibraries()
    {
        if (_nativeLibrariesReleased)
        {
            return;
        }

        _nativeLibrariesReleased = true;
        try
        {
            Container.Resolve<IDynamicLibraryInvoker>().ReleaseAll();
            Logger.Info("已释放缓存的本机库句柄。");
        }
        catch (Exception ex)
        {
            Logger.Error("释放缓存的本机库句柄失败。", ex);
        }
    }

    private static void ConfigureLogger()
    {
        Logger.LogDir = Path.Combine(AppContext.BaseDirectory, "Logs");
        Logger.Level = LogType.Debug;
        Logger.EnableConsoleOutput = true;
        Logger.TimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
    }
}
