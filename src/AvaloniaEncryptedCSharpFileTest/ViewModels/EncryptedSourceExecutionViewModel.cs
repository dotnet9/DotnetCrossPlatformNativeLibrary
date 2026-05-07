using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AvaloniaEncryptedCSharpFileTest.Models;
using AvaloniaEncryptedCSharpFileTest.Services;
using CodeWF.Log.Core;
using ReactiveUI;

namespace AvaloniaEncryptedCSharpFileTest.ViewModels;

public sealed class EncryptedSourceExecutionViewModel : ViewModelBase
{
    private readonly IGeneratedSourceFilePathProvider _pathProvider;
    private readonly IEncryptedSourceFileExecutor _executor;
    private int? _leftValue = 10;
    private int? _rightValue = 5;
    private bool _isExecuting;

    public EncryptedSourceExecutionViewModel(
        IGeneratedSourceFilePathProvider pathProvider,
        IEncryptedSourceFileExecutor executor)
    {
        _pathProvider = pathProvider;
        _executor = executor;

        RefreshDirectoryCommand = ReactiveCommand.Create(RefreshSourceFiles);
        DeleteSelectedCommand = ReactiveCommand.Create(DeleteSelectedSourceFiles);
        ExecuteSelectedCommand = ReactiveCommand.CreateFromTask(
            ExecuteSelectedSourceFilesAsync,
            this.WhenAnyValue(x => x.IsExecuting).Select(isExecuting => !isExecuting));
        ExecuteSelectedCommand.ThrownExceptions.Subscribe(ex => Logger.Error("执行加密 C# 文件时发生异常。", ex));

        RefreshSourceFiles();
    }

    public ObservableCollection<GeneratedSourceFileItem> SourceFiles { get; } = new();

    public ObservableCollection<SourceExecutionResult> ExecutionResults { get; } = new();

    public ReactiveCommand<Unit, Unit> RefreshDirectoryCommand { get; }

    public ReactiveCommand<Unit, Unit> DeleteSelectedCommand { get; }

    public ReactiveCommand<Unit, Unit> ExecuteSelectedCommand { get; }

    public string SourceFileDirectory => _pathProvider.SourceFileDirectory;

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

    public bool IsExecuting
    {
        get => _isExecuting;
        set => this.RaiseAndSetIfChanged(ref _isExecuting, value);
    }

    private void RefreshSourceFiles()
    {
        Directory.CreateDirectory(_pathProvider.SourceFileDirectory);
        SourceFiles.Clear();

        foreach (var path in Directory.EnumerateFiles(_pathProvider.SourceFileDirectory)
                     .Where(IsEncryptedSourceFile)
                     .OrderByDescending(File.GetLastWriteTime))
        {
            SourceFiles.Add(new GeneratedSourceFileItem(path));
        }

        Logger.Info($"刷新加密 C# 文件目录：{_pathProvider.SourceFileDirectory}，找到 {SourceFiles.Count} 个文件。");
    }

    private void DeleteSelectedSourceFiles()
    {
        var selectedFiles = SourceFiles.Where(x => x.IsSelected).ToArray();
        if (selectedFiles.Length == 0)
        {
            Logger.Warn("未选择要删除的加密 C# 文件。");
            return;
        }

        foreach (var item in selectedFiles)
        {
            try
            {
                File.Delete(item.FullPath);
                SourceFiles.Remove(item);
                Logger.Info($"已删除加密 C# 文件：{item.FullPath}");
            }
            catch (Exception ex)
            {
                Logger.Error($"删除加密 C# 文件失败：{item.FullPath}", ex);
            }
        }
    }

    private async Task ExecuteSelectedSourceFilesAsync()
    {
        var selectedPaths = SourceFiles
            .Where(x => x.IsSelected)
            .Select(x => x.FullPath)
            .ToArray();

        if (selectedPaths.Length == 0)
        {
            Logger.Warn("未选择要执行的加密 C# 文件。");
            return;
        }

        if (LeftValue is not { } left || RightValue is not { } right)
        {
            Logger.Warn("请输入两个有效整数。");
            return;
        }

        IsExecuting = true;
        try
        {
            ExecutionResults.Clear();
            Logger.Info($"开始执行 {selectedPaths.Length} 个加密 C# 文件，入参 left={left}, right={right}。");

            var results = await _executor.ExecuteAsync(selectedPaths, left, right);
            foreach (var result in results)
            {
                ExecutionResults.Add(result);
                if (result.Succeeded)
                {
                    Logger.Info($"{result.SourceFileName} -> {result.TypeName}.Cal({left}, {right}) = {result.Result}");
                }
                else
                {
                    Logger.Error($"{result.SourceFileName} 执行失败：{result.Message}");
                }
            }

            Logger.Info("加密 C# 文件执行完成。");
        }
        finally
        {
            IsExecuting = false;
        }
    }

    private static bool IsEncryptedSourceFile(string path)
    {
        return path.EndsWith(".cs.enc", StringComparison.OrdinalIgnoreCase);
    }
}
