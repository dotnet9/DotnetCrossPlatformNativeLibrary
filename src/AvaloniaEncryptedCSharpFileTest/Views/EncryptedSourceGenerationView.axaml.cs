using System;
using System.ComponentModel;
using Avalonia.Controls;
using AvaloniaEdit.Document;
using AvaloniaEdit.Highlighting;
using AvaloniaEncryptedCSharpFileTest.ViewModels;

namespace AvaloniaEncryptedCSharpFileTest.Views;

public partial class EncryptedSourceGenerationView : UserControl
{
    private EncryptedSourceGenerationViewModel? _viewModel;
    private bool _isSyncingEditorText;

    public EncryptedSourceGenerationView()
    {
        InitializeComponent();
        ConfigureEditors();
        AttachViewModel(DataContext as EncryptedSourceGenerationViewModel);
        Loaded += (_, _) => AttachViewModel(DataContext as EncryptedSourceGenerationViewModel);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (SourceEditor is null || InterfaceEditor is null)
        {
            return;
        }

        AttachViewModel(DataContext as EncryptedSourceGenerationViewModel);
    }

    private void ConfigureEditors()
    {
        var csharpHighlighting = HighlightingManager.Instance.GetDefinition("C#");

        InterfaceEditor.Document ??= new TextDocument();
        InterfaceEditor.SyntaxHighlighting = csharpHighlighting;
        InterfaceEditor.Options.ConvertTabsToSpaces = true;
        InterfaceEditor.Options.IndentationSize = 4;

        SourceEditor.Document ??= new TextDocument();
        SourceEditor.SyntaxHighlighting = csharpHighlighting;
        SourceEditor.Options.ConvertTabsToSpaces = true;
        SourceEditor.Options.IndentationSize = 4;
        SourceEditor.PointerPressed += (_, _) => SourceEditor.TextArea.Focus();
        SourceEditor.TextChanged += (_, _) =>
        {
            if (_isSyncingEditorText || _viewModel is null)
            {
                return;
            }

            if (!string.Equals(_viewModel.SourceCode, SourceEditor.Text, StringComparison.Ordinal))
            {
                _viewModel.SourceCode = SourceEditor.Text;
            }
        };
    }

    private void AttachViewModel(EncryptedSourceGenerationViewModel? viewModel)
    {
        if (ReferenceEquals(_viewModel, viewModel))
        {
            return;
        }

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = viewModel;

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            SyncInterfaceText(_viewModel.InterfaceDefinition);
            SyncEditorText(_viewModel.SourceCode);
        }
        else
        {
            SyncInterfaceText(string.Empty);
            SyncEditorText(string.Empty);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EncryptedSourceGenerationViewModel.SourceCode) && _viewModel is not null)
        {
            SyncEditorText(_viewModel.SourceCode);
        }
    }

    private void SyncInterfaceText(string text)
    {
        InterfaceEditor.Document ??= new TextDocument();
        if (string.Equals(InterfaceEditor.Document.Text, text, StringComparison.Ordinal))
        {
            return;
        }

        InterfaceEditor.Document.Text = text;
    }

    private void SyncEditorText(string text)
    {
        SourceEditor.Document ??= new TextDocument();
        if (string.Equals(SourceEditor.Document.Text, text, StringComparison.Ordinal))
        {
            return;
        }

        _isSyncingEditorText = true;
        try
        {
            SourceEditor.Document.Text = text;
        }
        finally
        {
            _isSyncingEditorText = false;
        }
    }
}
