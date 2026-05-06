using System;
using System.IO;
using ReactiveUI;

namespace AvaloniaDynamicLibraryTest.Models;

public sealed class GeneratedLibraryItem : ReactiveObject
{
    private bool _isSelected;

    public GeneratedLibraryItem(string fullPath)
    {
        FullPath = fullPath;

        var info = new FileInfo(fullPath);
        FileName = info.Name;
        SizeText = FormatSize(info.Length);
        LastWriteTime = info.LastWriteTime;
        LastWriteTimeText = LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public string FileName { get; }

    public string FullPath { get; }

    public DateTime LastWriteTime { get; }

    public string LastWriteTimeText { get; }

    public string SizeText { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1024 * 1024)
        {
            return $"{bytes / 1024d / 1024d:F2} MB";
        }

        if (bytes >= 1024)
        {
            return $"{bytes / 1024d:F1} KB";
        }

        return $"{bytes} B";
    }
}
