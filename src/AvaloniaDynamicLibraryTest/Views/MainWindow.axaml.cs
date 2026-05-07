using System;
using Avalonia;
using Avalonia.Controls;
using Ursa.Controls;

namespace AvaloniaDynamicLibraryTest.Views;

public partial class MainWindow : UrsaWindow
{
    private bool _hasOpened;
    private bool _isExiting;

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        _hasOpened = true;
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        ExitProcess();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        ExitProcess();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (_hasOpened && change.Property == IsVisibleProperty && !IsVisible)
        {
            ExitProcess();
        }
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
}
