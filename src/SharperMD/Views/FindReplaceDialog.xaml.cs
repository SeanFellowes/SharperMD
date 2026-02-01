using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ICSharpCode.AvalonEdit;

namespace SharperMD.Views;

public partial class FindReplaceDialog : Window
{
    private readonly FindReplaceViewModel _viewModel;

    public FindReplaceDialog(TextEditor editor)
    {
        InitializeComponent();
        _viewModel = new FindReplaceViewModel(editor, this);
        DataContext = _viewModel;

        Loaded += (s, e) =>
        {
            FindTextBox.Focus();
            FindTextBox.SelectAll();
        };
    }

    public void SetSearchText(string text)
    {
        _viewModel.SearchText = text;
    }

    public void FocusReplaceField()
    {
        ReplaceTextBox.Focus();
        ReplaceTextBox.SelectAll();
    }
}

public partial class FindReplaceViewModel : ObservableObject
{
    private readonly TextEditor _editor;
    private readonly Window _dialog;
    private int _lastSearchIndex = -1;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _replaceText = string.Empty;

    [ObservableProperty]
    private bool _matchCase;

    [ObservableProperty]
    private bool _wholeWord;

    [ObservableProperty]
    private bool _useRegex;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public FindReplaceViewModel(TextEditor editor, Window dialog)
    {
        _editor = editor;
        _dialog = dialog;

        // Pre-populate with selected text if any
        if (!string.IsNullOrEmpty(_editor.SelectedText) && !_editor.SelectedText.Contains('\n'))
        {
            SearchText = _editor.SelectedText;
        }
    }

    [RelayCommand]
    private void FindNext()
    {
        if (string.IsNullOrEmpty(SearchText))
        {
            StatusMessage = "Enter text to find";
            return;
        }

        var result = Find(forward: true);
        if (!result)
        {
            StatusMessage = "No matches found";
        }
    }

    [RelayCommand]
    private void FindPrevious()
    {
        if (string.IsNullOrEmpty(SearchText))
        {
            StatusMessage = "Enter text to find";
            return;
        }

        var result = Find(forward: false);
        if (!result)
        {
            StatusMessage = "No matches found";
        }
    }

    private bool Find(bool forward)
    {
        var text = _editor.Text;
        var searchPattern = SearchText;

        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(searchPattern))
            return false;

        try
        {
            var regex = BuildRegex(searchPattern);
            var matches = regex.Matches(text).Cast<Match>().ToList();

            if (matches.Count == 0)
            {
                _lastSearchIndex = -1;
                return false;
            }

            int startPos = _editor.CaretOffset;

            // Find the next/previous match from current position
            Match? targetMatch = null;

            if (forward)
            {
                // Find first match after current position
                targetMatch = matches.FirstOrDefault(m => m.Index > startPos);
                if (targetMatch == null)
                {
                    // Wrap around to beginning
                    targetMatch = matches.First();
                    StatusMessage = $"Wrapped to start ({matches.Count} matches)";
                }
                else
                {
                    StatusMessage = $"Match {matches.IndexOf(targetMatch) + 1} of {matches.Count}";
                }
            }
            else
            {
                // Find last match before current position
                targetMatch = matches.LastOrDefault(m => m.Index < startPos - 1);
                if (targetMatch == null)
                {
                    // Wrap around to end
                    targetMatch = matches.Last();
                    StatusMessage = $"Wrapped to end ({matches.Count} matches)";
                }
                else
                {
                    StatusMessage = $"Match {matches.IndexOf(targetMatch) + 1} of {matches.Count}";
                }
            }

            if (targetMatch != null)
            {
                _editor.Select(targetMatch.Index, targetMatch.Length);
                _editor.ScrollTo(_editor.Document.GetLineByOffset(targetMatch.Index).LineNumber, 0);
                _lastSearchIndex = targetMatch.Index;
                return true;
            }

            return false;
        }
        catch (RegexParseException ex)
        {
            StatusMessage = $"Invalid regex: {ex.Message}";
            return false;
        }
    }

    [RelayCommand]
    private void Replace()
    {
        if (string.IsNullOrEmpty(SearchText))
        {
            StatusMessage = "Enter text to find";
            return;
        }

        // If current selection matches the search pattern, replace it
        if (!string.IsNullOrEmpty(_editor.SelectedText))
        {
            var regex = BuildRegex(SearchText);
            var selectedMatch = regex.Match(_editor.SelectedText);

            if (selectedMatch.Success && selectedMatch.Value == _editor.SelectedText)
            {
                // Replace the current selection
                var replacement = UseRegex
                    ? regex.Replace(_editor.SelectedText, ReplaceText)
                    : ReplaceText;

                var selectionStart = _editor.SelectionStart;
                _editor.Document.Replace(_editor.SelectionStart, _editor.SelectionLength, replacement);
                _editor.CaretOffset = selectionStart + replacement.Length;
                StatusMessage = "Replaced 1 occurrence";
            }
        }

        // Find next match
        FindNext();
    }

    [RelayCommand]
    private void ReplaceAll()
    {
        if (string.IsNullOrEmpty(SearchText))
        {
            StatusMessage = "Enter text to find";
            return;
        }

        try
        {
            var regex = BuildRegex(SearchText);
            var text = _editor.Text;
            var matches = regex.Matches(text);
            var count = matches.Count;

            if (count == 0)
            {
                StatusMessage = "No matches found";
                return;
            }

            var newText = regex.Replace(text, ReplaceText);

            // Preserve caret position as best as possible
            var caretOffset = _editor.CaretOffset;

            _editor.Document.BeginUpdate();
            _editor.Document.Text = newText;
            _editor.Document.EndUpdate();

            // Try to restore caret position
            _editor.CaretOffset = Math.Min(caretOffset, newText.Length);

            StatusMessage = $"Replaced {count} occurrence{(count == 1 ? "" : "s")}";
        }
        catch (RegexParseException ex)
        {
            StatusMessage = $"Invalid regex: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Close()
    {
        _dialog.Hide();
    }

    private Regex BuildRegex(string pattern)
    {
        if (!UseRegex)
        {
            pattern = Regex.Escape(pattern);
        }

        if (WholeWord)
        {
            pattern = $@"\b{pattern}\b";
        }

        var options = RegexOptions.Compiled;
        if (!MatchCase)
        {
            options |= RegexOptions.IgnoreCase;
        }

        return new Regex(pattern, options);
    }
}
