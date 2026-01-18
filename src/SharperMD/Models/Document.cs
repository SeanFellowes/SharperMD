using CommunityToolkit.Mvvm.ComponentModel;

namespace SharperMD.Models;

/// <summary>
/// Represents a markdown document with dirty tracking
/// </summary>
public partial class Document : ObservableObject
{
    private string _originalContent = string.Empty;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private bool _isDirty;

    [ObservableProperty]
    private bool _isNew = true;

    [ObservableProperty]
    private DateTime? _lastSaved;

    [ObservableProperty]
    private DateTime? _lastAutoSaved;

    public string FileName => string.IsNullOrEmpty(FilePath) ? "Untitled.md" : Path.GetFileName(FilePath);

    public string Title => IsDirty ? $"*{FileName}" : FileName;

    public string DisplayPath => string.IsNullOrEmpty(FilePath) ? "New Document" : FilePath;

    public Document()
    {
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Content))
            {
                IsDirty = Content != _originalContent;
                OnPropertyChanged(nameof(Title));
            }
            else if (e.PropertyName == nameof(FilePath))
            {
                OnPropertyChanged(nameof(FileName));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(DisplayPath));
            }
            else if (e.PropertyName == nameof(IsDirty))
            {
                OnPropertyChanged(nameof(Title));
            }
        };
    }

    public static Document CreateNew()
    {
        var doc = new Document
        {
            IsNew = true,
            Content = string.Empty
        };
        doc._originalContent = string.Empty;
        return doc;
    }

    public static Document Open(string filePath)
    {
        var content = File.ReadAllText(filePath);
        var doc = new Document
        {
            FilePath = filePath,
            Content = content,
            IsNew = false,
            IsDirty = false
        };
        doc._originalContent = content;
        return doc;
    }

    public void Save()
    {
        if (string.IsNullOrEmpty(FilePath))
            throw new InvalidOperationException("Cannot save document without a file path. Use SaveAs instead.");

        File.WriteAllText(FilePath, Content);
        _originalContent = Content;
        IsDirty = false;
        IsNew = false;
        LastSaved = DateTime.Now;
    }

    public void SaveAs(string filePath)
    {
        FilePath = filePath;
        Save();
    }

    public void MarkAsSaved()
    {
        _originalContent = Content;
        IsDirty = false;
        LastSaved = DateTime.Now;
    }

    public void Revert()
    {
        Content = _originalContent;
        IsDirty = false;
    }

    /// <summary>
    /// Gets the draft file path for auto-save
    /// </summary>
    public string GetDraftPath()
    {
        var draftsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SharperMD", "drafts");

        Directory.CreateDirectory(draftsFolder);

        if (string.IsNullOrEmpty(FilePath))
        {
            // For new documents, use a hash of the content or a timestamp
            return Path.Combine(draftsFolder, $"untitled_{GetHashCode():X8}.md.draft");
        }

        // For existing documents, use a hash of the file path
        var hash = FilePath.GetHashCode();
        return Path.Combine(draftsFolder, $"{Path.GetFileName(FilePath)}_{hash:X8}.draft");
    }

    public void SaveDraft()
    {
        var draftPath = GetDraftPath();
        File.WriteAllText(draftPath, Content);
        LastAutoSaved = DateTime.Now;
    }

    public void DeleteDraft()
    {
        var draftPath = GetDraftPath();
        if (File.Exists(draftPath))
        {
            File.Delete(draftPath);
        }
    }

    public static string? GetDraftContent(string filePath)
    {
        var draftsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SharperMD", "drafts");

        var hash = filePath.GetHashCode();
        var draftPath = Path.Combine(draftsFolder, $"{Path.GetFileName(filePath)}_{hash:X8}.draft");

        if (File.Exists(draftPath))
        {
            return File.ReadAllText(draftPath);
        }

        return null;
    }

    public static bool HasDraft(string filePath)
    {
        return GetDraftContent(filePath) != null;
    }
}
