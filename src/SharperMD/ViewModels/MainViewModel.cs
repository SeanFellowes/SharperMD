using System.Collections.ObjectModel;
using System.IO;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SharperMD.Models;
using SharperMD.Services;
using Timer = System.Timers.Timer;

namespace SharperMD.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly MarkdownService _markdownService;
    private readonly ThemeService _themeService;
    private readonly Timer _autoSaveTimer;
    private readonly Timer _previewUpdateTimer;

    [ObservableProperty]
    private Document _currentDocument;

    [ObservableProperty]
    private AppSettings _settings;

    /// <summary>
    /// Collection of all open documents (tabs)
    /// </summary>
    public ObservableCollection<Document> OpenDocuments { get; } = new();

    [ObservableProperty]
    private int _selectedTabIndex = -1;

    [ObservableProperty]
    private string _previewHtml = string.Empty;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private bool _showWelcomeScreen = true;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private int _lineCount;

    [ObservableProperty]
    private int _wordCount;

    [ObservableProperty]
    private int _characterCount;

    [ObservableProperty]
    private int _currentLine = 1;

    [ObservableProperty]
    private int _currentColumn = 1;

    [ObservableProperty]
    private double _editorFontSize;

    [ObservableProperty]
    private double _previewFontSize;

    public bool IsDarkTheme => _themeService.IsDarkTheme;

    public string WindowTitle => CurrentDocument != null
        ? $"{CurrentDocument.Title} - SharperMD"
        : "SharperMD";

    public bool HasOpenDocuments => OpenDocuments.Count > 0;

    public List<RecentFile> RecentFiles => Settings.RecentFiles.Where(f => f.Exists).ToList();

    public bool HasRecentFiles => RecentFiles.Any();

    public MainViewModel()
    {
        _markdownService = new MarkdownService();
        _themeService = new ThemeService();
        _settings = AppSettings.Load();
        _currentDocument = Document.CreateNew();

        EditorFontSize = _settings.EditorFontSize;
        PreviewFontSize = _settings.PreviewFontSize;

        // Only show welcome screen if user wants it AND there's no session to restore
        var hasSessionToRestore = _settings.OpenDocumentPaths?.Any(File.Exists) == true;
        ShowWelcomeScreen = _settings.ShowWelcomeScreen && !hasSessionToRestore;

        // Initialize statistics for the default document
        UpdateStatistics(_currentDocument.Content);

        // Initialize theme
        _themeService.Initialize(_settings.Theme);
        _themeService.ThemeChanged += (s, isDark) =>
        {
            OnPropertyChanged(nameof(IsDarkTheme));
            UpdatePreview();
        };

        // Setup auto-save timer
        _autoSaveTimer = new Timer(_settings.AutoSaveIntervalSeconds * 1000);
        _autoSaveTimer.Elapsed += OnAutoSaveTimerElapsed;
        if (_settings.AutoSaveEnabled)
        {
            _autoSaveTimer.Start();
        }

        // Setup preview update timer (debounce)
        _previewUpdateTimer = new Timer(300);
        _previewUpdateTimer.AutoReset = false;
        _previewUpdateTimer.Elapsed += (s, e) =>
        {
            Application.Current?.Dispatcher.Invoke(UpdatePreview);
        };

        // Subscribe to document changes
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(CurrentDocument))
            {
                OnPropertyChanged(nameof(WindowTitle));
                if (CurrentDocument != null)
                {
                    CurrentDocument.PropertyChanged += (ds, de) =>
                    {
                        if (de.PropertyName == nameof(Document.Title) || de.PropertyName == nameof(Document.IsDirty))
                        {
                            OnPropertyChanged(nameof(WindowTitle));
                        }
                    };
                }
            }
        };
    }

    public void Initialize(string? filePath)
    {
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            // Opening a specific file from command line
            OpenFile(filePath);
            ShowWelcomeScreen = false;
            IsEditing = false; // View mode when opened with file
        }
        else
        {
            // Try to restore previous session
            RestoreSession();

            if (OpenDocuments.Count == 0)
            {
                // No session to restore
                if (!_settings.ShowWelcomeScreen)
                {
                    NewDocument();
                    ShowWelcomeScreen = false;
                }
            }
        }
    }

    public void OnContentChanged(string content)
    {
        if (CurrentDocument != null)
        {
            CurrentDocument.Content = content;
            UpdateStatistics(content);

            // Debounce preview update
            _previewUpdateTimer.Stop();
            _previewUpdateTimer.Start();
        }
    }

    public void OnCaretPositionChanged(int line, int column)
    {
        CurrentLine = line;
        CurrentColumn = column;
    }

    private void UpdateStatistics(string content)
    {
        LineCount = content.Split('\n').Length;
        WordCount = string.IsNullOrWhiteSpace(content)
            ? 0
            : content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        CharacterCount = content.Length;
    }

    private void UpdatePreview()
    {
        if (CurrentDocument == null) return;

        var css = _themeService.GetPreviewCss();
        css = css.Replace("font-size: 16px", $"font-size: {PreviewFontSize}px");

        // Get the document's directory for resolving relative image paths
        string? documentBasePath = null;
        if (!string.IsNullOrEmpty(CurrentDocument.FilePath))
        {
            documentBasePath = Path.GetDirectoryName(CurrentDocument.FilePath);
        }

        PreviewHtml = _markdownService.ToFullHtml(CurrentDocument.Content, css, _themeService.IsDarkTheme, documentBasePath);
    }

    private void OnAutoSaveTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            if (!_settings.AutoSaveEnabled) return;

            var savedCount = 0;
            foreach (var doc in OpenDocuments)
            {
                if (doc.IsDirty)
                {
                    try
                    {
                        doc.SaveDraft();
                        savedCount++;
                    }
                    catch
                    {
                        // Continue with other documents
                    }
                }
            }

            if (savedCount > 0)
            {
                StatusText = savedCount == 1
                    ? $"Draft saved at {DateTime.Now:HH:mm:ss}"
                    : $"{savedCount} drafts saved at {DateTime.Now:HH:mm:ss}";
            }
        });
    }

    #region Commands

    [RelayCommand]
    private void NewDocument()
    {
        var newDoc = Document.CreateNew();
        AddDocumentTab(newDoc, replaceEmpty: true);

        ShowWelcomeScreen = false;
        IsEditing = true;
        UpdatePreview();
        UpdateStatistics(CurrentDocument.Content);
        StatusText = "New document created";
    }

    [RelayCommand]
    private void OpenFile(string? filePath = null)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Markdown files (*.md;*.markdown)|*.md;*.markdown|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Title = "Open Markdown File"
            };

            if (dialog.ShowDialog() != true) return;
            filePath = dialog.FileName;
        }

        // Check if file is already open - switch to that tab instead
        var existingIndex = FindOpenDocumentIndex(filePath);
        if (existingIndex >= 0)
        {
            SelectedTabIndex = existingIndex;
            StatusText = $"Switched to: {Path.GetFileName(filePath)}";
            return;
        }

        try
        {
            Document newDoc;

            // Check for draft recovery
            var draftContent = Document.GetDraftContent(filePath);
            if (draftContent != null)
            {
                var result = MessageBox.Show(
                    "A draft version of this file was found (possibly from a crash).\n\n" +
                    "Do you want to recover the draft?\n\n" +
                    "Yes = Load draft\nNo = Load original file\nCancel = Don't open",
                    "Recover Draft?",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel) return;

                if (result == MessageBoxResult.Yes)
                {
                    newDoc = Document.Open(filePath);
                    newDoc.Content = draftContent;
                    StatusText = "Draft recovered - remember to save";
                }
                else
                {
                    newDoc = Document.Open(filePath);
                    newDoc.DeleteDraft();
                    StatusText = $"Opened: {filePath}";
                }
            }
            else
            {
                newDoc = Document.Open(filePath);
                StatusText = $"Opened: {filePath}";
            }

            // Add as new tab (smart replace if current tab is empty)
            AddDocumentTab(newDoc, replaceEmpty: true);

            _settings.AddRecentFile(filePath);
            OnPropertyChanged(nameof(RecentFiles));
            OnPropertyChanged(nameof(HasRecentFiles));
            ShowWelcomeScreen = false;
            IsEditing = false; // View mode when opening
            UpdatePreview();
            UpdateStatistics(CurrentDocument.Content);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Save()
    {
        if (CurrentDocument == null) return;

        if (CurrentDocument.IsNew)
        {
            SaveAs();
            return;
        }

        try
        {
            CurrentDocument.Save();
            CurrentDocument.DeleteDraft();
            StatusText = $"Saved: {CurrentDocument.FilePath}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void SaveAs()
    {
        if (CurrentDocument == null) return;

        var dialog = new SaveFileDialog
        {
            Filter = "Markdown files (*.md)|*.md|All files (*.*)|*.*",
            Title = "Save Markdown File",
            FileName = CurrentDocument.FileName,
            DefaultExt = ".md"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            CurrentDocument.SaveAs(dialog.FileName);
            CurrentDocument.DeleteDraft();
            _settings.AddRecentFile(dialog.FileName);
            OnPropertyChanged(nameof(RecentFiles));
            OnPropertyChanged(nameof(HasRecentFiles));
            StatusText = $"Saved: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Export()
    {
        if (CurrentDocument == null) return;

        var dialog = new SaveFileDialog
        {
            Filter = "HTML files (*.html)|*.html|All files (*.*)|*.*",
            Title = "Export as HTML",
            FileName = Path.GetFileNameWithoutExtension(CurrentDocument.FileName) + ".html",
            DefaultExt = ".html"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            File.WriteAllText(dialog.FileName, PreviewHtml);
            StatusText = $"Exported: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to export:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void StartEditing()
    {
        IsEditing = true;
        StatusText = "Editing mode";
    }

    [RelayCommand]
    private void StopEditing()
    {
        if (!CanCloseCurrentDocument()) return;

        IsEditing = false;
        StatusText = "View mode";
    }

    [RelayCommand]
    private void ToggleEditMode()
    {
        if (IsEditing)
        {
            StopEditing();
        }
        else
        {
            StartEditing();
        }
    }

    [RelayCommand]
    private void Exit()
    {
        if (!CanCloseAllDocuments()) return;

        SaveSessionState();
        SaveWindowState();
        Application.Current.Shutdown();
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        var newMode = _settings.Theme switch
        {
            ThemeMode.System => ThemeMode.Light,
            ThemeMode.Light => ThemeMode.Dark,
            ThemeMode.Dark => ThemeMode.System,
            _ => ThemeMode.System
        };

        _settings.Theme = newMode;
        _settings.Save();
        _themeService.Initialize(newMode);
        StatusText = $"Theme: {newMode}";
    }

    [RelayCommand]
    private void SetTheme(string theme)
    {
        var mode = theme.ToLower() switch
        {
            "light" => ThemeMode.Light,
            "dark" => ThemeMode.Dark,
            _ => ThemeMode.System
        };

        _settings.Theme = mode;
        _settings.Save();
        _themeService.Initialize(mode);
        StatusText = $"Theme: {mode}";
    }

    [RelayCommand]
    private void IncreaseFontSize()
    {
        EditorFontSize = Math.Min(EditorFontSize + 2, 48);
        PreviewFontSize = Math.Min(PreviewFontSize + 2, 48);
        SaveFontSettings();
        UpdatePreview();
    }

    [RelayCommand]
    private void DecreaseFontSize()
    {
        EditorFontSize = Math.Max(EditorFontSize - 2, 8);
        PreviewFontSize = Math.Max(PreviewFontSize - 2, 8);
        SaveFontSettings();
        UpdatePreview();
    }

    [RelayCommand]
    private void ResetFontSize()
    {
        EditorFontSize = 14;
        PreviewFontSize = 16;
        SaveFontSettings();
        UpdatePreview();
        StatusText = "Font size reset";
    }

    private void SaveFontSettings()
    {
        _settings.EditorFontSize = EditorFontSize;
        _settings.PreviewFontSize = PreviewFontSize;
        _settings.Save();
    }

    [RelayCommand]
    private void ToggleWordWrap()
    {
        _settings.WordWrap = !_settings.WordWrap;
        _settings.Save();
        OnPropertyChanged(nameof(Settings));
        StatusText = $"Word wrap: {(_settings.WordWrap ? "On" : "Off")}";
    }

    [RelayCommand]
    private void ToggleLineNumbers()
    {
        _settings.ShowLineNumbers = !_settings.ShowLineNumbers;
        _settings.Save();
        OnPropertyChanged(nameof(Settings));
        StatusText = $"Line numbers: {(_settings.ShowLineNumbers ? "On" : "Off")}";
    }

    [RelayCommand]
    private void ToggleScrollSync()
    {
        _settings.ScrollSyncEnabled = !_settings.ScrollSyncEnabled;
        _settings.Save();
        OnPropertyChanged(nameof(Settings));
        StatusText = $"Scroll sync: {(_settings.ScrollSyncEnabled ? "On" : "Off")}";
    }

    [RelayCommand]
    private void HideWelcomeForever()
    {
        _settings.ShowWelcomeScreen = false;
        _settings.Save();
        ShowWelcomeScreen = false;
        NewDocument();
    }

    [RelayCommand]
    private void ShowAbout()
    {
        MessageBox.Show(
            "SharperMD - Markdown Viewer & Editor\n\n" +
            "Version 1.1.0\n\n" +
            "A beautiful, full-featured markdown editor for Windows.\n\n" +
            "Features:\n" +
            "• Multiple document tabs\n" +
            "• Session restore\n" +
            "• Live preview with syntax highlighting\n" +
            "• Support for tables, code blocks, math (LaTeX)\n" +
            "• Light and dark themes\n" +
            "• Auto-save and crash recovery\n" +
            "• Scroll synchronization\n\n" +
            "Built with:\n" +
            "• .NET 8 / WPF\n" +
            "• Markdig (markdown parsing)\n" +
            "• AvalonEdit (text editor)\n" +
            "• WebView2 (preview rendering)\n\n" +
            "Created by:\n" +
            "• Sean Fellowes - Project Creator & Design\n" +
            "• Claude (Anthropic) - Development",
            "About SharperMD",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    [RelayCommand]
    private void ShowKeyboardShortcuts()
    {
        MessageBox.Show(
            "Keyboard Shortcuts\n\n" +
            "File:\n" +
            "  Ctrl+N     New document\n" +
            "  Ctrl+O     Open file\n" +
            "  Ctrl+S     Save\n" +
            "  Ctrl+Shift+S   Save As\n\n" +
            "Edit:\n" +
            "  Ctrl+Z     Undo\n" +
            "  Ctrl+Y     Redo\n" +
            "  Ctrl+X     Cut\n" +
            "  Ctrl+C     Copy\n" +
            "  Ctrl+V     Paste\n" +
            "  Ctrl+A     Select All\n" +
            "  Ctrl+F     Find\n" +
            "  Ctrl+H     Replace\n\n" +
            "Formatting:\n" +
            "  Ctrl+B     Bold\n" +
            "  Ctrl+I     Italic\n" +
            "  Ctrl+K     Insert Link\n" +
            "  Ctrl+Shift+K   Insert Image\n\n" +
            "View:\n" +
            "  Ctrl++     Increase font size\n" +
            "  Ctrl+-     Decrease font size\n" +
            "  Ctrl+0     Reset font size\n" +
            "  Ctrl+Mouse Wheel   Zoom\n\n" +
            "Tabs:\n" +
            "  Ctrl+Tab   Next tab\n" +
            "  Ctrl+Shift+Tab   Previous tab\n" +
            "  Ctrl+W     Close tab",
            "Keyboard Shortcuts",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    #endregion

    #region Formatting Commands

    [RelayCommand]
    private void InsertBold() => RequestInsertText("**", "**", "bold text");

    [RelayCommand]
    private void InsertItalic() => RequestInsertText("*", "*", "italic text");

    [RelayCommand]
    private void InsertStrikethrough() => RequestInsertText("~~", "~~", "strikethrough");

    [RelayCommand]
    private void InsertCode() => RequestInsertText("`", "`", "code");

    [RelayCommand]
    private void InsertCodeBlock() => RequestInsertText("```\n", "\n```", "code here");

    [RelayCommand]
    private void InsertLink() => RequestInsertText("[", "](url)", "link text");

    [RelayCommand]
    private void InsertImage() => RequestInsertText("![", "](image-url)", "alt text");

    [RelayCommand]
    private void InsertHeading1() => RequestInsertLinePrefix("# ");

    [RelayCommand]
    private void InsertHeading2() => RequestInsertLinePrefix("## ");

    [RelayCommand]
    private void InsertHeading3() => RequestInsertLinePrefix("### ");

    [RelayCommand]
    private void InsertBulletList() => RequestInsertLinePrefix("- ");

    [RelayCommand]
    private void InsertNumberedList() => RequestInsertLinePrefix("1. ");

    [RelayCommand]
    private void InsertTaskList() => RequestInsertLinePrefix("- [ ] ");

    [RelayCommand]
    private void InsertQuote() => RequestInsertLinePrefix("> ");

    [RelayCommand]
    private void InsertHorizontalRule() => RequestInsertText("\n---\n", "", "");

    [RelayCommand]
    private void InsertTable() => RequestInsertText(
        "| Column 1 | Column 2 | Column 3 |\n| --- | --- | --- |\n| ", " | | |\n", "cell");

    public event Action<string, string, string>? InsertTextRequested;
    public event Action<string>? InsertLinePrefixRequested;

    /// <summary>
    /// Event fired when tab selection changes, before switching documents.
    /// Handler should save current editor state (scroll position, caret).
    /// </summary>
    public event Action? TabSwitching;

    /// <summary>
    /// Event fired after tab selection changes.
    /// Handler should restore editor content and state for the new document.
    /// </summary>
    public event Action? TabSwitched;

    private void RequestInsertText(string before, string after, string placeholder)
    {
        InsertTextRequested?.Invoke(before, after, placeholder);
    }

    private void RequestInsertLinePrefix(string prefix)
    {
        InsertLinePrefixRequested?.Invoke(prefix);
    }

    #endregion

    #region Tab Management

    partial void OnSelectedTabIndexChanged(int oldValue, int newValue)
    {
        if (newValue < 0 || newValue >= OpenDocuments.Count)
        {
            CurrentDocument = null!;
            return;
        }

        // Notify that we're about to switch - save current editor state
        TabSwitching?.Invoke();

        // Switch to the new document
        CurrentDocument = OpenDocuments[newValue];
        UpdatePreview();
        UpdateStatistics(CurrentDocument?.Content ?? string.Empty);

        // Notify that switch is complete - restore editor content
        TabSwitched?.Invoke();
    }

    /// <summary>
    /// Checks if a file is already open and returns its index, or -1 if not found
    /// </summary>
    private int FindOpenDocumentIndex(string filePath)
    {
        for (int i = 0; i < OpenDocuments.Count; i++)
        {
            if (OpenDocuments[i].FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Checks if current tab can be replaced (is new, empty, and not dirty)
    /// </summary>
    private bool CanReplaceCurrentTab()
    {
        return CurrentDocument is { IsNew: true, IsDirty: false }
               && string.IsNullOrEmpty(CurrentDocument.Content);
    }

    /// <summary>
    /// Adds a document to the tab collection, optionally replacing an empty tab
    /// </summary>
    private void AddDocumentTab(Document doc, bool replaceEmpty = true)
    {
        if (replaceEmpty && CanReplaceCurrentTab() && OpenDocuments.Count > 0)
        {
            // Replace the current empty tab
            var index = SelectedTabIndex >= 0 ? SelectedTabIndex : 0;
            OpenDocuments[index] = doc;
            CurrentDocument = doc;
            SubscribeToDocumentChanges(doc);
        }
        else
        {
            // Add as new tab
            OpenDocuments.Add(doc);
            SubscribeToDocumentChanges(doc);
            SelectedTabIndex = OpenDocuments.Count - 1;
        }

        OnPropertyChanged(nameof(HasOpenDocuments));
    }

    private void SubscribeToDocumentChanges(Document doc)
    {
        doc.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Document.Title) || e.PropertyName == nameof(Document.IsDirty))
            {
                if (doc == CurrentDocument)
                {
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        };
    }

    [RelayCommand]
    private void CloseTab(Document? doc = null)
    {
        doc ??= CurrentDocument;
        if (doc == null) return;

        if (!CanCloseDocument(doc)) return;

        var index = OpenDocuments.IndexOf(doc);
        if (index < 0) return;

        // Delete any draft for this document
        doc.DeleteDraft();

        OpenDocuments.RemoveAt(index);
        OnPropertyChanged(nameof(HasOpenDocuments));

        if (OpenDocuments.Count == 0)
        {
            // No more tabs - show welcome screen or create new document
            if (_settings.ShowWelcomeScreen)
            {
                ShowWelcomeScreen = true;
                CurrentDocument = null!;
                SelectedTabIndex = -1;
            }
            else
            {
                // Create a new empty document
                var newDoc = Document.CreateNew();
                OpenDocuments.Add(newDoc);
                SubscribeToDocumentChanges(newDoc);
                SelectedTabIndex = 0;
            }
        }
        else
        {
            // Select adjacent tab
            SelectedTabIndex = Math.Min(index, OpenDocuments.Count - 1);
        }
    }

    [RelayCommand]
    private void CloseOtherTabs()
    {
        if (CurrentDocument == null) return;

        var toClose = OpenDocuments.Where(d => d != CurrentDocument).ToList();
        foreach (var doc in toClose)
        {
            if (!CanCloseDocument(doc)) return; // Cancel if any document can't be closed
        }

        foreach (var doc in toClose)
        {
            doc.DeleteDraft();
            OpenDocuments.Remove(doc);
        }

        SelectedTabIndex = 0;
        OnPropertyChanged(nameof(HasOpenDocuments));
    }

    [RelayCommand]
    private void CloseAllTabs()
    {
        if (!CanCloseAllDocuments()) return;

        foreach (var doc in OpenDocuments.ToList())
        {
            doc.DeleteDraft();
        }

        OpenDocuments.Clear();
        OnPropertyChanged(nameof(HasOpenDocuments));

        if (_settings.ShowWelcomeScreen)
        {
            ShowWelcomeScreen = true;
            CurrentDocument = null!;
            SelectedTabIndex = -1;
        }
        else
        {
            var newDoc = Document.CreateNew();
            OpenDocuments.Add(newDoc);
            SubscribeToDocumentChanges(newDoc);
            SelectedTabIndex = 0;
        }
    }

    [RelayCommand]
    private void NextTab()
    {
        if (OpenDocuments.Count <= 1) return;
        SelectedTabIndex = (SelectedTabIndex + 1) % OpenDocuments.Count;
    }

    [RelayCommand]
    private void PreviousTab()
    {
        if (OpenDocuments.Count <= 1) return;
        SelectedTabIndex = (SelectedTabIndex - 1 + OpenDocuments.Count) % OpenDocuments.Count;
    }

    [RelayCommand]
    private void CopyFilePath()
    {
        if (CurrentDocument != null && !string.IsNullOrEmpty(CurrentDocument.FilePath))
        {
            Clipboard.SetText(CurrentDocument.FilePath);
            StatusText = "File path copied to clipboard";
        }
    }

    #endregion

    #region Window State

    public void LoadWindowState(Window window)
    {
        // Get the saved dimensions
        var left = _settings.WindowLeft;
        var top = _settings.WindowTop;
        var width = _settings.WindowWidth;
        var height = _settings.WindowHeight;

        // Validate and adjust position to ensure window is visible on screen
        var (adjustedLeft, adjustedTop, adjustedWidth, adjustedHeight) =
            ValidateWindowPosition(left, top, width, height);

        // Apply the validated position
        window.Left = adjustedLeft;
        window.Top = adjustedTop;
        window.Width = adjustedWidth;
        window.Height = adjustedHeight;

        // Apply maximized state after setting position
        if (_settings.WindowMaximized)
        {
            window.WindowState = WindowState.Maximized;
        }
    }

    private (double left, double top, double width, double height) ValidateWindowPosition(
        double left, double top, double width, double height)
    {
        // Get virtual screen bounds (all monitors combined)
        var virtualLeft = SystemParameters.VirtualScreenLeft;
        var virtualTop = SystemParameters.VirtualScreenTop;
        var virtualWidth = SystemParameters.VirtualScreenWidth;
        var virtualHeight = SystemParameters.VirtualScreenHeight;

        // Get primary work area for defaults
        var workArea = SystemParameters.WorkArea;

        // Ensure minimum dimensions
        width = Math.Max(width, 800);
        height = Math.Max(height, 600);

        // Ensure window doesn't exceed virtual screen size
        width = Math.Min(width, virtualWidth);
        height = Math.Min(height, virtualHeight);

        // Check if window is at least partially visible on any monitor
        var windowRight = left + width;
        var windowBottom = top + height;
        var virtualRight = virtualLeft + virtualWidth;
        var virtualBottom = virtualTop + virtualHeight;

        // Define minimum visible area (at least 100 pixels visible)
        const int minVisible = 100;

        bool isVisible = left < virtualRight - minVisible &&
                        windowRight > virtualLeft + minVisible &&
                        top < virtualBottom - minVisible &&
                        windowBottom > virtualTop + minVisible;

        if (!isVisible)
        {
            // Window is off-screen, center on primary work area
            width = Math.Min(width, workArea.Width * 0.85);
            height = Math.Min(height, workArea.Height * 0.85);
            left = workArea.Left + (workArea.Width - width) / 2;
            top = workArea.Top + (workArea.Height - height) / 2;
        }
        else
        {
            // Ensure window is not too far off the visible area
            if (left < virtualLeft) left = virtualLeft;
            if (top < virtualTop) top = virtualTop;
            if (left + width > virtualRight) left = virtualRight - width;
            if (top + height > virtualBottom) top = virtualBottom - height;
        }

        return (left, top, width, height);
    }

    public void SaveWindowState()
    {
        var window = Application.Current.MainWindow;
        if (window == null) return;

        _settings.WindowMaximized = window.WindowState == WindowState.Maximized;

        // Save the RestoreBounds when maximized, actual bounds when normal
        if (window.WindowState == WindowState.Maximized)
        {
            // RestoreBounds contains the window size before maximizing
            var bounds = window.RestoreBounds;
            if (!bounds.IsEmpty && bounds.Width > 0 && bounds.Height > 0)
            {
                _settings.WindowLeft = bounds.Left;
                _settings.WindowTop = bounds.Top;
                _settings.WindowWidth = bounds.Width;
                _settings.WindowHeight = bounds.Height;
            }
        }
        else if (window.WindowState == WindowState.Normal)
        {
            _settings.WindowLeft = window.Left;
            _settings.WindowTop = window.Top;
            _settings.WindowWidth = window.Width;
            _settings.WindowHeight = window.Height;
        }

        _settings.Save();
    }

    public void SaveSessionState()
    {
        // Save open document paths for session restore
        _settings.OpenDocumentPaths = OpenDocuments
            .Where(d => !d.IsNew && !string.IsNullOrEmpty(d.FilePath))
            .Select(d => d.FilePath)
            .ToList();
        _settings.LastActiveTabIndex = SelectedTabIndex;
        _settings.Save();
    }

    public void RestoreSession()
    {
        if (_settings.OpenDocumentPaths == null || _settings.OpenDocumentPaths.Count == 0)
            return;

        var restoredCount = 0;
        var missingFiles = new List<string>();

        foreach (var filePath in _settings.OpenDocumentPaths)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    var doc = Document.Open(filePath);
                    OpenDocuments.Add(doc);
                    SubscribeToDocumentChanges(doc);
                    restoredCount++;
                }
                catch
                {
                    missingFiles.Add(filePath);
                }
            }
            else
            {
                missingFiles.Add(filePath);
            }
        }

        if (restoredCount > 0)
        {
            OnPropertyChanged(nameof(HasOpenDocuments));

            // Restore the active tab
            var tabIndex = Math.Min(_settings.LastActiveTabIndex, OpenDocuments.Count - 1);
            tabIndex = Math.Max(0, tabIndex);
            SelectedTabIndex = tabIndex;

            ShowWelcomeScreen = false;
            StatusText = restoredCount == 1
                ? "Session restored (1 file)"
                : $"Session restored ({restoredCount} files)";
        }

        // Notify user of missing files
        if (missingFiles.Count > 0)
        {
            var message = missingFiles.Count == 1
                ? $"Could not restore: {Path.GetFileName(missingFiles[0])} (file not found)"
                : $"Could not restore {missingFiles.Count} files (not found)";

            if (restoredCount > 0)
            {
                StatusText = message;
            }
        }
    }

    #endregion

    public bool CanCloseCurrentDocument()
    {
        return CanCloseDocument(CurrentDocument);
    }

    public bool CanCloseDocument(Document? doc)
    {
        if (doc is not { IsDirty: true }) return true;

        var result = MessageBox.Show(
            $"'{doc.FileName}' has unsaved changes.\n\n" +
            "Do you want to save before closing?",
            "Unsaved Changes",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning,
            MessageBoxResult.Cancel);

        return result switch
        {
            MessageBoxResult.Yes => SaveDocument(doc),
            MessageBoxResult.No => DiscardDocumentChanges(doc),
            _ => false
        };
    }

    public bool CanCloseAllDocuments()
    {
        foreach (var doc in OpenDocuments)
        {
            if (!CanCloseDocument(doc)) return false;
        }
        return true;
    }

    private bool SaveDocument(Document doc)
    {
        if (doc.IsNew)
        {
            // Need to do Save As
            var dialog = new SaveFileDialog
            {
                Filter = "Markdown files (*.md)|*.md|All files (*.*)|*.*",
                Title = "Save Markdown File",
                FileName = doc.FileName,
                DefaultExt = ".md"
            };

            if (dialog.ShowDialog() != true) return false;

            try
            {
                doc.SaveAs(dialog.FileName);
                doc.DeleteDraft();
                _settings.AddRecentFile(dialog.FileName);
                OnPropertyChanged(nameof(RecentFiles));
                OnPropertyChanged(nameof(HasRecentFiles));
                return true;
            }
            catch
            {
                return false;
            }
        }

        try
        {
            doc.Save();
            doc.DeleteDraft();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool DiscardDocumentChanges(Document doc)
    {
        doc.DeleteDraft();
        return true;
    }

    private bool SaveAndReturn()
    {
        Save();
        return !CurrentDocument.IsDirty;
    }

    private bool DiscardChanges()
    {
        CurrentDocument.DeleteDraft();
        return true;
    }

    public void Cleanup()
    {
        _autoSaveTimer.Stop();
        _autoSaveTimer.Dispose();
        _previewUpdateTimer.Stop();
        _previewUpdateTimer.Dispose();
    }
}
