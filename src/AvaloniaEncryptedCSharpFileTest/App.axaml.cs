using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using AvaloniaEncryptedCSharpFileTest.RegionAdapters;
using AvaloniaEncryptedCSharpFileTest.Services;
using AvaloniaEncryptedCSharpFileTest.ViewModels;
using AvaloniaEncryptedCSharpFileTest.Views;
using CodeWF.Log.Core;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Regions;

namespace AvaloniaEncryptedCSharpFileTest;

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
        containerRegistry.RegisterSingleton<IGeneratedSourceFilePathProvider, GeneratedSourceFilePathProvider>();
        containerRegistry.RegisterSingleton<IEncryptedSourceFileGenerator, EncryptedSourceFileGenerator>();
        containerRegistry.RegisterSingleton<IEncryptedSourceFileExecutor, EncryptedSourceFileExecutor>();
        containerRegistry.Register<EncryptedSourceGenerationViewModel>();
        containerRegistry.Register<EncryptedSourceExecutionViewModel>();
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

        Logger.Info("Avalonia 加密 C# 文件测试程序已启动。");
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
