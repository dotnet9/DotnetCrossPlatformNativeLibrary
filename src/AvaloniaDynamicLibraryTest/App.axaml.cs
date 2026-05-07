using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using AvaloniaDynamicLibraryTest.RegionAdapters;
using AvaloniaDynamicLibraryTest.Services;
using AvaloniaDynamicLibraryTest.ViewModels;
using AvaloniaDynamicLibraryTest.Views;
using CodeWF.Log.Core;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Regions;

namespace AvaloniaDynamicLibraryTest;

public partial class App : PrismApplication
{
    private bool _isExiting;

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

    protected override void ConfigureRegionAdapterMappings(RegionAdapterMappings regionAdapterMappings)
    {
        base.ConfigureRegionAdapterMappings(regionAdapterMappings);
        regionAdapterMappings.RegisterMapping(typeof(TabControl), Container.Resolve<TabControlRegionAdapter>());
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<IGeneratedLibraryPathProvider, GeneratedLibraryPathProvider>();
        containerRegistry.RegisterSingleton<IDynamicLibraryCompiler, CSharpDynamicLibraryCompiler>();
        containerRegistry.RegisterSingleton<IDynamicLibraryInvoker, CSharpDynamicLibraryInvoker>();
        containerRegistry.Register<LibraryGenerationViewModel>();
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
            desktop.Exit += (_, _) => _ = Logger.FlushAsync();
            StartExitWatcher(desktop);
        }

        Logger.Info("Avalonia 动态库测试程序已启动。");
    }

    private void StartExitWatcher(IClassicDesktopStyleApplicationLifetime desktop)
    {
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
        Environment.Exit(0);
    }

    private static void ConfigureLogger()
    {
        Logger.LogDir = Path.Combine(AppContext.BaseDirectory, "Logs");
        Logger.Level = LogType.Debug;
        Logger.EnableConsoleOutput = true;
        Logger.TimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
    }
}
