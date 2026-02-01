using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Microsoft.Web.WebView2.Core;
using SharperMD.ViewModels;
using SharperMD.Views;
using System.Xml;

namespace SharperMD;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private bool _isUpdatingFromCode;
    private bool _webViewInitialized;
    private bool _previewOnlyWebViewInitialized;
    private FindReplaceDialog? _findReplaceDialog;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        // Set up Find/Replace command bindings
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Find, OnFind));
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Replace, OnReplace));

        // Subscribe to preview HTML changes
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        // Subscribe to text insertion requests
        _viewModel.InsertTextRequested += OnInsertTextRequested;
        _viewModel.InsertLinePrefixRequested += OnInsertLinePrefixRequested;

        // Setup editor
        SetupEditor();
    }

    private void SetupEditor()
    {
        // Load markdown syntax highlighting
        try
        {
            using var stream = GetType().Assembly.GetManifestResourceStream("SharperMD.Resources.MarkdownSyntax.xshd");
            if (stream != null)
            {
                using var reader = new XmlTextReader(stream);
                Editor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            }
        }
        catch
        {
            // If syntax highlighting fails, continue without it
        }

        // Handle text changes
        Editor.TextChanged += (s, e) =>
        {
            if (_isUpdatingFromCode) return;
            _viewModel.OnContentChanged(Editor.Text);
        };

        // Handle caret position changes
        Editor.TextArea.Caret.PositionChanged += (s, e) =>
        {
            _viewModel.OnCaretPositionChanged(
                Editor.TextArea.Caret.Line,
                Editor.TextArea.Caret.Column);
        };

        // Handle scroll for sync
        Editor.TextArea.TextView.ScrollOffsetChanged += (s, e) =>
        {
            if (_viewModel.Settings.ScrollSyncEnabled)
            {
                SyncPreviewScroll();
            }
        };
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Load window state
        _viewModel.LoadWindowState(this);

        // Initialize WebView2
        await InitializeWebViews();

        // Handle command line file
        _viewModel.Initialize(App.StartupFilePath);

        // Load document content into editor
        if (_viewModel.CurrentDocument != null)
        {
            _isUpdatingFromCode = true;
            Editor.Text = _viewModel.CurrentDocument.Content;
            _isUpdatingFromCode = false;
        }
    }

    private async Task InitializeWebViews()
    {
        try
        {
            // Create WebView2 environment with user data folder in AppData
            // This fixes permission issues when installed in Program Files
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SharperMD", "WebView2");

            Directory.CreateDirectory(userDataFolder);

            var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);

            // Initialize main preview WebView
            await PreviewWebView.EnsureCoreWebView2Async(environment);
            _webViewInitialized = true;

            // Initialize preview-only WebView
            await PreviewOnlyWebView.EnsureCoreWebView2Async(environment);
            _previewOnlyWebViewInitialized = true;

            // Configure WebView settings
            ConfigureWebView(PreviewWebView.CoreWebView2);
            ConfigureWebView(PreviewOnlyWebView.CoreWebView2);

            // Initial preview update
            UpdatePreviewContent();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to initialize WebView2. Please ensure WebView2 Runtime is installed.\n\n{ex.Message}",
                "WebView2 Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ConfigureWebView(CoreWebView2 webView)
    {
        webView.Settings.IsZoomControlEnabled = true;
        webView.Settings.IsStatusBarEnabled = false;
        webView.Settings.AreDefaultContextMenusEnabled = true;
        webView.Settings.IsWebMessageEnabled = true;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.PreviewHtml))
        {
            Dispatcher.Invoke(UpdatePreviewContent);
        }
        else if (e.PropertyName == nameof(MainViewModel.CurrentDocument))
        {
            Dispatcher.Invoke(() =>
            {
                _isUpdatingFromCode = true;
                Editor.Text = _viewModel.CurrentDocument?.Content ?? string.Empty;
                _isUpdatingFromCode = false;
            });
        }
    }

    private void UpdatePreviewContent()
    {
        if (string.IsNullOrEmpty(_viewModel.PreviewHtml)) return;

        if (_webViewInitialized && PreviewWebView.CoreWebView2 != null)
        {
            PreviewWebView.NavigateToString(_viewModel.PreviewHtml);
        }

        if (_previewOnlyWebViewInitialized && PreviewOnlyWebView.CoreWebView2 != null)
        {
            PreviewOnlyWebView.NavigateToString(_viewModel.PreviewHtml);
        }
    }

    private async void SyncPreviewScroll()
    {
        if (!_webViewInitialized || PreviewWebView.CoreWebView2 == null) return;

        try
        {
            // Calculate scroll percentage
            var scrollViewer = Editor.TextArea.TextView;
            var totalHeight = scrollViewer.DocumentHeight;
            var viewportTop = scrollViewer.ScrollOffset.Y;
            var scrollPercent = totalHeight > 0 ? viewportTop / totalHeight : 0;

            // Apply scroll to preview
            var script = $"window.scrollTo(0, document.body.scrollHeight * {scrollPercent});";
            await PreviewWebView.CoreWebView2.ExecuteScriptAsync(script);
        }
        catch
        {
            // Ignore scroll sync errors
        }
    }

    private void OnInsertTextRequested(string before, string after, string placeholder)
    {
        var selection = Editor.TextArea.Selection;

        if (!selection.IsEmpty)
        {
            // Wrap selection
            var selectedText = selection.GetText();
            var start = Editor.SelectionStart;
            var length = Editor.SelectionLength;

            Editor.Document.Replace(start, length, before + selectedText + after);
            Editor.Select(start, before.Length + selectedText.Length + after.Length);
        }
        else
        {
            // Insert with placeholder
            var caretOffset = Editor.CaretOffset;
            var insertText = before + placeholder + after;
            Editor.Document.Insert(caretOffset, insertText);
            Editor.Select(caretOffset + before.Length, placeholder.Length);
        }

        Editor.Focus();
    }

    private void OnInsertLinePrefixRequested(string prefix)
    {
        var line = Editor.Document.GetLineByOffset(Editor.CaretOffset);
        var lineStart = line.Offset;

        Editor.Document.Insert(lineStart, prefix);
        Editor.CaretOffset = lineStart + prefix.Length;
        Editor.Focus();
    }

    private void Editor_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Handle Ctrl+Wheel for zoom
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;

            if (e.Delta > 0)
            {
                _viewModel.IncreaseFontSizeCommand.Execute(null);
            }
            else
            {
                _viewModel.DecreaseFontSizeCommand.Execute(null);
            }
        }
    }

    private void Preview_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Handle Ctrl+Wheel for zoom in preview
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;

            if (e.Delta > 0)
            {
                _viewModel.IncreaseFontSizeCommand.Execute(null);
            }
            else
            {
                _viewModel.DecreaseFontSizeCommand.Execute(null);
            }
        }
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (!_viewModel.CanCloseCurrentDocument())
        {
            e.Cancel = true;
            return;
        }

        _viewModel.SaveWindowState();
        _viewModel.Cleanup();
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                var ext = System.IO.Path.GetExtension(files[0]).ToLower();
                if (ext == ".md" || ext == ".markdown" || ext == ".txt")
                {
                    e.Effects = DragDropEffects.Copy;
                    e.Handled = true;
                    return;
                }
            }
        }

        e.Effects = DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                _viewModel.OpenFileCommand.Execute(files[0]);
            }
        }
    }

    private void OnFind(object sender, ExecutedRoutedEventArgs e)
    {
        ShowFindReplaceDialog(focusReplace: false);
    }

    private void OnReplace(object sender, ExecutedRoutedEventArgs e)
    {
        ShowFindReplaceDialog(focusReplace: true);
    }

    private void ShowFindReplaceDialog(bool focusReplace)
    {
        // Ensure we're in edit mode to use Find/Replace
        if (!_viewModel.IsEditing)
        {
            _viewModel.StartEditingCommand.Execute(null);
        }

        if (_findReplaceDialog == null)
        {
            _findReplaceDialog = new FindReplaceDialog(Editor);
            _findReplaceDialog.Owner = this;
            _findReplaceDialog.Closing += (s, e) =>
            {
                // Hide instead of close so we can reuse
                e.Cancel = true;
                _findReplaceDialog.Hide();
            };
        }

        // Pre-populate with selected text
        if (!string.IsNullOrEmpty(Editor.SelectedText) && !Editor.SelectedText.Contains('\n'))
        {
            _findReplaceDialog.SetSearchText(Editor.SelectedText);
        }

        _findReplaceDialog.Show();

        if (focusReplace)
        {
            _findReplaceDialog.FocusReplaceField();
        }
    }
}
