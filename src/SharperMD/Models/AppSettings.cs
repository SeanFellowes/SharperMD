using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharperMD.Models;

/// <summary>
/// Application settings persisted to disk
/// </summary>
public class AppSettings
{
    private static readonly string SettingsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SharperMD");

    private static readonly string SettingsFile = Path.Combine(SettingsFolder, "settings.json");

    [JsonPropertyName("theme")]
    public ThemeMode Theme { get; set; } = ThemeMode.System;

    [JsonPropertyName("editorFontSize")]
    public double EditorFontSize { get; set; } = 14;

    [JsonPropertyName("previewFontSize")]
    public double PreviewFontSize { get; set; } = 16;

    [JsonPropertyName("editorFontFamily")]
    public string EditorFontFamily { get; set; } = "Cascadia Mono, Consolas, Courier New";

    [JsonPropertyName("showWelcomeScreen")]
    public bool ShowWelcomeScreen { get; set; } = true;

    [JsonPropertyName("scrollSyncEnabled")]
    public bool ScrollSyncEnabled { get; set; } = true;

    [JsonPropertyName("autoSaveEnabled")]
    public bool AutoSaveEnabled { get; set; } = true;

    [JsonPropertyName("autoSaveIntervalSeconds")]
    public int AutoSaveIntervalSeconds { get; set; } = 30;

    [JsonPropertyName("wordWrap")]
    public bool WordWrap { get; set; } = true;

    [JsonPropertyName("showLineNumbers")]
    public bool ShowLineNumbers { get; set; } = true;

    [JsonPropertyName("recentFiles")]
    public List<RecentFile> RecentFiles { get; set; } = new();

    [JsonPropertyName("windowWidth")]
    public double WindowWidth { get; set; } = 1400;

    [JsonPropertyName("windowHeight")]
    public double WindowHeight { get; set; } = 900;

    [JsonPropertyName("windowLeft")]
    public double WindowLeft { get; set; } = 100;

    [JsonPropertyName("windowTop")]
    public double WindowTop { get; set; } = 100;

    [JsonPropertyName("windowMaximized")]
    public bool WindowMaximized { get; set; } = false;

    [JsonPropertyName("editorWidthRatio")]
    public double EditorWidthRatio { get; set; } = 0.5;

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            // If settings are corrupted, return defaults
        }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsFolder);

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(SettingsFile, json);
        }
        catch
        {
            // Silently fail if we can't save settings
        }
    }

    public void AddRecentFile(string filePath)
    {
        // Remove if already exists
        RecentFiles.RemoveAll(r => r.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));

        // Add to front
        RecentFiles.Insert(0, new RecentFile
        {
            FilePath = filePath,
            LastOpened = DateTime.Now
        });

        // Keep only last 10
        if (RecentFiles.Count > 10)
        {
            RecentFiles = RecentFiles.Take(10).ToList();
        }

        Save();
    }

    public void RemoveRecentFile(string filePath)
    {
        RecentFiles.RemoveAll(r => r.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
        Save();
    }
}

public enum ThemeMode
{
    System,
    Light,
    Dark
}

public class RecentFile
{
    [JsonPropertyName("filePath")]
    public string FilePath { get; set; } = string.Empty;

    [JsonPropertyName("lastOpened")]
    public DateTime LastOpened { get; set; }

    [JsonIgnore]
    public string FileName => Path.GetFileName(FilePath);

    [JsonIgnore]
    public string Directory => Path.GetDirectoryName(FilePath) ?? string.Empty;

    [JsonIgnore]
    public bool Exists => File.Exists(FilePath);
}
