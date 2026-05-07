using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using AvaloniaManagedLibraryTest.Models;
using AvaloniaManagedLibraryTest.Services;
using CodeWF.Log.Core;
using ReactiveUI;

namespace AvaloniaManagedLibraryTest.ViewModels;

public sealed class LibraryInvocationViewModel : ViewModelBase
{
    private readonly INativeLibraryPathProvider _pathProvider;
    private readonly IDynamicLibraryInvoker _invoker;
    private int? _leftValue = 10;
    private int? _rightValue = 5;
    private bool _keepLibraryReferences;
    private bool _isInvoking;

    public LibraryInvocationViewModel(
        INativeLibraryPathProvider pathProvider,
        IDynamicLibraryInvoker invoker)
    {
        _pathProvider = pathProvider;
        _invoker = invoker;

        RefreshDirectoryCommand = ReactiveCommand.Create(RefreshLibraries);
        InvokeSelectedCommand = ReactiveCommand.CreateFromTask(InvokeSelectedLibrariesAsync);
        InvokeSelectedCommand.ThrownExceptions.Subscribe(ex => Logger.Error("调用本机库失败。", ex));

        RefreshLibraries();
    }

    public ObservableCollection<NativeLibraryItem> Libraries { get; } = new();

    public ObservableCollection<LibraryInvocationResult> InvocationResults { get; } = new();

    public ReactiveCommand<Unit, Unit> RefreshDirectoryCommand { get; }

    public ReactiveCommand<Unit, Unit> InvokeSelectedCommand { get; }

    public string LibraryDirectory => _pathProvider.LibraryDirectory;

    public int? LeftValue
    {
        get => _leftValue;
        set => this.RaiseAndSetIfChanged(ref _leftValue, value);
    }

    public int? RightValue
    {
        get => _rightValue;
        set => this.RaiseAndSetIfChanged(ref _rightValue, value);
    }

    public bool KeepLibraryReferences
    {
        get => _keepLibraryReferences;
        set
        {
            this.RaiseAndSetIfChanged(ref _keepLibraryReferences, value);
            if (!value)
            {
                _invoker.ReleaseAll();
            }
        }
    }

    public bool IsInvoking
    {
        get => _isInvoking;
        set => this.RaiseAndSetIfChanged(ref _isInvoking, value);
    }

    private void RefreshLibraries()
    {
        Directory.CreateDirectory(_pathProvider.LibraryDirectory);
        Libraries.Clear();

        foreach (var path in Directory.EnumerateFiles(_pathProvider.LibraryDirectory)
                     .Where(IsNativeLibrary)
                     .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
        {
            Libraries.Add(new NativeLibraryItem(_pathProvider.LibraryDirectory, path));
        }

        Logger.Info($"已刷新本机库目录：{_pathProvider.LibraryDirectory}，发现 {Libraries.Count} 个库文件。");
    }

    private async Task InvokeSelectedLibrariesAsync()
    {
        if (IsInvoking)
        {
            return;
        }

        var selectedPaths = Libraries
            .Where(x => x.IsSelected)
            .Select(x => x.FullPath)
            .ToArray();

        if (selectedPaths.Length == 0)
        {
            Logger.Warn("请至少选择一个要调用的本机库。");
            return;
        }

        if (LeftValue is not { } left || RightValue is not { } right)
        {
            Logger.Warn("请输入两个有效的整数入参。");
            return;
        }

        IsInvoking = true;
        try
        {
            InvocationResults.Clear();
            Logger.Info(
                $"开始调用 {selectedPaths.Length} 个本机库，左值={left}，右值={right}，保持库引用={(KeepLibraryReferences ? "是" : "否")}。");

            var results = await _invoker.InvokeAsync(selectedPaths, left, right, KeepLibraryReferences);
            foreach (var result in results)
            {
                InvocationResults.Add(result);
                if (result.Succeeded)
                {
                    Logger.Info($"{result.LibraryName} 调用 {result.TypeName}({left}, {right})，结果={result.Result}。");
                }
                else
                {
                    Logger.Error($"{result.LibraryName} 调用失败：{result.Message}");
                }
            }

            Logger.Info("本机库调用完成。");
        }
        finally
        {
            IsInvoking = false;
        }
    }

    private static bool IsNativeLibrary(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() is ".dll" or ".so" or ".dylib";
    }
}
