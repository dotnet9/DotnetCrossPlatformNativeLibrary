using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AvaloniaDynamicLibraryTest.Models;
using AvaloniaDynamicLibraryTest.Services;
using CodeWF.Log.Core;
using ReactiveUI;

namespace AvaloniaDynamicLibraryTest.ViewModels;

public sealed class LibraryInvocationViewModel : ViewModelBase
{
    private readonly IGeneratedLibraryPathProvider _pathProvider;
    private readonly IDynamicLibraryInvoker _invoker;
    private int? _leftValue = 10;
    private int? _rightValue = 5;
    private bool _isInvoking;

    public LibraryInvocationViewModel(
        IGeneratedLibraryPathProvider pathProvider,
        IDynamicLibraryInvoker invoker)
    {
        _pathProvider = pathProvider;
        _invoker = invoker;

        RefreshDirectoryCommand = ReactiveCommand.Create(RefreshLibraries);
        DeleteSelectedCommand = ReactiveCommand.Create(DeleteSelectedLibraries);
        InvokeSelectedCommand = ReactiveCommand.CreateFromTask(
            InvokeSelectedLibrariesAsync,
            this.WhenAnyValue(x => x.IsInvoking).Select(isInvoking => !isInvoking));
        InvokeSelectedCommand.ThrownExceptions.Subscribe(ex => Logger.Error("执行 C# 文件时发生异常。", ex));

        RefreshLibraries();
    }

    public ObservableCollection<GeneratedLibraryItem> Libraries { get; } = new();

    public ObservableCollection<LibraryInvocationResult> InvocationResults { get; } = new();

    public ReactiveCommand<Unit, Unit> RefreshDirectoryCommand { get; }

    public ReactiveCommand<Unit, Unit> DeleteSelectedCommand { get; }

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

    public bool IsInvoking
    {
        get => _isInvoking;
        set => this.RaiseAndSetIfChanged(ref _isInvoking, value);
    }

    private void RefreshLibraries()
    {
        Directory.CreateDirectory(_pathProvider.LibraryDirectory);
        Libraries.Clear();

        foreach (var path in Directory.EnumerateFiles(_pathProvider.LibraryDirectory, "*.cs")
                     .OrderByDescending(File.GetLastWriteTime))
        {
            Libraries.Add(new GeneratedLibraryItem(path));
        }

        Logger.Info($"刷新 C# 文件目录：{_pathProvider.LibraryDirectory}，找到 {Libraries.Count} 个 C# 文件。");
    }

    private void DeleteSelectedLibraries()
    {
        var selectedLibraries = Libraries.Where(x => x.IsSelected).ToArray();
        if (selectedLibraries.Length == 0)
        {
            Logger.Warn("未选择要删除的 C# 文件。");
            return;
        }

        foreach (var item in selectedLibraries)
        {
            try
            {
                DeleteLibraryArtifacts(item.FullPath);
                Libraries.Remove(item);
                Logger.Info($"已删除 C# 文件：{item.FullPath}");
            }
            catch (Exception ex)
            {
                Logger.Error($"删除 C# 文件失败：{item.FullPath}", ex);
            }
        }
    }

    private async Task InvokeSelectedLibrariesAsync()
    {
        var selectedPaths = Libraries
            .Where(x => x.IsSelected)
            .Select(x => x.FullPath)
            .ToArray();

        if (selectedPaths.Length == 0)
        {
            Logger.Warn("未选择要执行的 C# 文件。");
            return;
        }

        if (LeftValue is not { } left || RightValue is not { } right)
        {
            Logger.Warn("请输入两个有效整数。");
            return;
        }

        IsInvoking = true;
        try
        {
            InvocationResults.Clear();
            Logger.Info($"开始执行 {selectedPaths.Length} 个 C# 文件，入参 left={left}, right={right}。");

            var results = await _invoker.InvokeAsync(selectedPaths, left, right);
            foreach (var result in results)
            {
                InvocationResults.Add(result);
                if (result.Succeeded)
                {
                    Logger.Info($"{result.LibraryName} -> {result.TypeName}.Cal({left}, {right}) = {result.Result}");
                }
                else
                {
                    Logger.Error($"{result.LibraryName} 执行失败：{result.Message}");
                }
            }

            Logger.Info("C# 文件执行完成。");
        }
        finally
        {
            IsInvoking = false;
        }
    }

    private static void DeleteLibraryArtifacts(string sourcePath)
    {
        var directory = Path.GetDirectoryName(sourcePath);
        var stem = Path.GetFileNameWithoutExtension(sourcePath);
        if (directory is null || string.IsNullOrWhiteSpace(stem))
        {
            return;
        }

        foreach (var extension in new[] { ".dll", ".pdb", ".cs" })
        {
            var path = Path.Combine(directory, stem + extension);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
