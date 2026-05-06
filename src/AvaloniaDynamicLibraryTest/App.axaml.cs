using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
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
        containerRegistry.RegisterSingleton<IDynamicLibraryCompiler, CSharpSourceFileGenerator>();
        containerRegistry.RegisterSingleton<IDynamicLibraryInvoker, CSharpSourceFileInvoker>();
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
            desktop.Exit += (_, _) => Logger.FlushAsync().GetAwaiter().GetResult();
        }

        Logger.Info("Avalonia 动态库测试程序已启动。");
    }

    private static void ConfigureLogger()
    {
        Logger.LogDir = Path.Combine(AppContext.BaseDirectory, "Logs");
        Logger.Level = LogType.Debug;
        Logger.EnableConsoleOutput = true;
        Logger.TimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
    }
}
