using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AvaloniaDynamicLibraryTest.Models;
using AvaloniaDynamicLibraryTest.Services;
using CodeWF.Log.Core;
using ReactiveUI;

namespace AvaloniaDynamicLibraryTest.ViewModels;

public sealed class LibraryGenerationViewModel : ViewModelBase
{
    private readonly IDynamicLibraryCompiler _compiler;
    private readonly IGeneratedLibraryPathProvider _pathProvider;
    private AlgorithmOption? _selectedAlgorithm;
    private string _sourceCode = string.Empty;
    private bool _isGenerating;

    public LibraryGenerationViewModel(
        IDynamicLibraryCompiler compiler,
        IGeneratedLibraryPathProvider pathProvider)
    {
        _compiler = compiler;
        _pathProvider = pathProvider;

        Algorithms = new ObservableCollection<AlgorithmOption>(CreateAlgorithms());
        GenerateCommand = ReactiveCommand.CreateFromTask(
            GenerateAsync,
            this.WhenAnyValue(x => x.IsGenerating).Select(isGenerating => !isGenerating));
        GenerateCommand.ThrownExceptions.Subscribe(ex => Logger.Error("生成 C# 文件时发生异常。", ex));

        SelectedAlgorithm = Algorithms.FirstOrDefault();
    }

    public ObservableCollection<AlgorithmOption> Algorithms { get; }

    public ReactiveCommand<Unit, Unit> GenerateCommand { get; }

    public string InterfaceDefinition { get; } =
        """
        public interface ICal
        {
            int Cal(int left, int right);
        }
        """;

    public string OutputDirectory => _pathProvider.LibraryDirectory;

    public AlgorithmOption? SelectedAlgorithm
    {
        get => _selectedAlgorithm;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedAlgorithm, value);
            if (value is not null)
            {
                SourceCode = value.SourceCode;
            }
        }
    }

    public string SourceCode
    {
        get => _sourceCode;
        set => this.RaiseAndSetIfChanged(ref _sourceCode, value);
    }

    public bool IsGenerating
    {
        get => _isGenerating;
        set => this.RaiseAndSetIfChanged(ref _isGenerating, value);
    }

    private async Task GenerateAsync()
    {
        if (string.IsNullOrWhiteSpace(SourceCode))
        {
            Logger.Warn("源码为空，已取消生成。");
            return;
        }

        IsGenerating = true;
        try
        {
            Logger.Info("开始生成 C# 文件。");
            var result = await _compiler.CompileAsync(SourceCode);
            foreach (var diagnostic in result.Diagnostics)
            {
                Logger.Warn(diagnostic);
            }

            if (!result.Succeeded)
            {
                Logger.Error("C# 文件生成失败。");
                return;
            }

            Logger.Info($"C# 文件生成成功：{result.OutputPath}");
            if (!string.IsNullOrWhiteSpace(result.SourcePath) && result.SourcePath != result.OutputPath)
            {
                Logger.Debug($"源码快照：{result.SourcePath}");
            }
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private static AlgorithmOption[] CreateAlgorithms()
    {
        return
        [
            new AlgorithmOption("加法", "AddCal", CreateSource("AddCal", "return left + right;")),
            new AlgorithmOption("减法", "SubtractCal", CreateSource("SubtractCal", "return left - right;")),
            new AlgorithmOption("乘法", "MultiplyCal", CreateSource("MultiplyCal", "return left * right;")),
            new AlgorithmOption("最大公约数", "GreatestCommonDivisorCal",
                """
                left = Math.Abs(left);
                right = Math.Abs(right);
                while (right != 0)
                {
                    var next = left % right;
                    left = right;
                    right = next;
                }

                return left;
                """),
            new AlgorithmOption("位混合", "BitMixerCal",
                """
                unchecked
                {
                    var value = left;
                    value = (value * 397) ^ right;
                    value ^= value << 13;
                    value ^= value >> 17;
                    value ^= value << 5;
                    return value;
                }
                """)
        ];
    }

    private static string CreateSource(string className, string methodBody)
    {
        return $$"""
        using System;

        namespace GeneratedDynamicLibrary;

        public interface ICal
        {
            int Cal(int left, int right);
        }

        public sealed class {{className}} : ICal
        {
            public int Cal(int left, int right)
            {
        {{Indent(methodBody, 8)}}
            }
        }
        """;
    }

    private static string Indent(string text, int spaces)
    {
        var prefix = new string(' ', spaces);
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        return string.Join(Environment.NewLine, lines.Select(line => prefix + line));
    }
}
