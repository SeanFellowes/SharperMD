using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using SharperMD.Models;

namespace SharperMD.Services;

/// <summary>
/// Service for managing application themes and detecting Windows theme settings
/// </summary>
public class ThemeService
{
    public event EventHandler<bool>? ThemeChanged;

    private bool _isDarkTheme;
    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        private set
        {
            if (_isDarkTheme != value)
            {
                _isDarkTheme = value;
                ThemeChanged?.Invoke(this, value);
            }
        }
    }

    public ThemeService()
    {
        // Listen for Windows theme changes
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General)
        {
            // Windows theme may have changed
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var settings = AppSettings.Load();
                if (settings.Theme == ThemeMode.System)
                {
                    IsDarkTheme = IsWindowsInDarkMode();
                    ApplyTheme(IsDarkTheme);
                }
            });
        }
    }

    /// <summary>
    /// Detect if Windows is using dark mode
    /// </summary>
    public static bool IsWindowsInDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

            if (key?.GetValue("AppsUseLightTheme") is int value)
            {
                return value == 0; // 0 = dark mode, 1 = light mode
            }
        }
        catch
        {
            // If we can't read the registry, default to dark
        }

        return true; // Default to dark mode
    }

    /// <summary>
    /// Apply the specified theme to the application
    /// </summary>
    public void ApplyTheme(bool isDark)
    {
        IsDarkTheme = isDark;

        var app = Application.Current;
        if (app == null) return;

        // Update resource dictionary with theme colors
        var resources = app.Resources;

        if (isDark)
        {
            // Dark theme colors
            resources["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            resources["WindowForegroundBrush"] = new SolidColorBrush(Color.FromRgb(241, 241, 241));
            resources["EditorBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(37, 37, 38));
            resources["EditorForegroundBrush"] = new SolidColorBrush(Color.FromRgb(212, 212, 212));
            resources["ToolbarBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 48));
            resources["ToolbarBorderBrush"] = new SolidColorBrush(Color.FromRgb(63, 63, 70));
            resources["MenuBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 48));
            resources["MenuForegroundBrush"] = new SolidColorBrush(Color.FromRgb(241, 241, 241));
            resources["ButtonBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(63, 63, 70));
            resources["ButtonHoverBrush"] = new SolidColorBrush(Color.FromRgb(78, 78, 85));
            resources["ButtonPressedBrush"] = new SolidColorBrush(Color.FromRgb(90, 90, 96));
            resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(0, 122, 204));
            resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(63, 63, 70));
            resources["SplitterBrush"] = new SolidColorBrush(Color.FromRgb(63, 63, 70));
            resources["StatusBarBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0, 122, 204));
            resources["StatusBarForegroundBrush"] = new SolidColorBrush(Colors.White);
            resources["WelcomeBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(37, 37, 38));
            resources["WelcomeCardBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 48));
        }
        else
        {
            // Light theme colors (paper-like for the editor)
            resources["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(243, 243, 243));
            resources["WindowForegroundBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            resources["EditorBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            resources["EditorForegroundBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            resources["ToolbarBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(238, 238, 242));
            resources["ToolbarBorderBrush"] = new SolidColorBrush(Color.FromRgb(204, 206, 219));
            resources["MenuBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(246, 246, 246));
            resources["MenuForegroundBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            resources["ButtonBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(221, 221, 221));
            resources["ButtonHoverBrush"] = new SolidColorBrush(Color.FromRgb(201, 222, 245));
            resources["ButtonPressedBrush"] = new SolidColorBrush(Color.FromRgb(180, 200, 220));
            resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(0, 122, 204));
            resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(204, 206, 219));
            resources["SplitterBrush"] = new SolidColorBrush(Color.FromRgb(204, 206, 219));
            resources["StatusBarBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0, 122, 204));
            resources["StatusBarForegroundBrush"] = new SolidColorBrush(Colors.White);
            resources["WelcomeBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            resources["WelcomeCardBrush"] = new SolidColorBrush(Color.FromRgb(246, 246, 246));
        }
    }

    /// <summary>
    /// Initialize theme based on settings
    /// </summary>
    public void Initialize(ThemeMode mode)
    {
        bool isDark = mode switch
        {
            ThemeMode.Light => false,
            ThemeMode.Dark => true,
            ThemeMode.System => IsWindowsInDarkMode(),
            _ => true
        };

        ApplyTheme(isDark);
    }

    /// <summary>
    /// Get CSS for the preview based on current theme
    /// </summary>
    public string GetPreviewCss()
    {
        return IsDarkTheme ? GetDarkCss() : GetLightCss();
    }

    private static string GetLightCss() => @"
:root {
    --color-bg: #ffffff;
    --color-fg: #24292e;
    --color-border: #e1e4e8;
    --color-code-bg: #f6f8fa;
    --color-blockquote: #6a737d;
    --color-link: #0366d6;
    --color-header: #24292e;
    --color-table-border: #dfe2e5;
    --color-table-row: #f6f8fa;
}

* {
    box-sizing: border-box;
}

body {
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Noto Sans', Helvetica, Arial, sans-serif;
    font-size: 16px;
    line-height: 1.6;
    color: var(--color-fg);
    background-color: var(--color-bg);
    margin: 0;
    padding: 16px 24px;
    max-width: 100%;
}

.markdown-body {
    max-width: 100%;
    margin: 0;
}

h1, h2, h3, h4, h5, h6 {
    color: var(--color-header);
    margin-top: 24px;
    margin-bottom: 16px;
    font-weight: 600;
    line-height: 1.25;
}

h1 { font-size: 2em; border-bottom: 1px solid var(--color-border); padding-bottom: 0.3em; }
h2 { font-size: 1.5em; border-bottom: 1px solid var(--color-border); padding-bottom: 0.3em; }
h3 { font-size: 1.25em; }
h4 { font-size: 1em; }
h5 { font-size: 0.875em; }
h6 { font-size: 0.85em; color: var(--color-blockquote); }

p { margin-top: 0; margin-bottom: 16px; }

a {
    color: var(--color-link);
    text-decoration: none;
}
a:hover { text-decoration: underline; }

code {
    font-family: 'Cascadia Code', 'Fira Code', Consolas, 'Courier New', monospace;
    font-size: 85%;
    background-color: var(--color-code-bg);
    padding: 0.2em 0.4em;
    border-radius: 6px;
}

pre {
    background-color: var(--color-code-bg);
    border-radius: 6px;
    padding: 16px;
    overflow: auto;
    font-size: 85%;
    line-height: 1.45;
    margin-bottom: 16px;
}

pre code {
    background-color: transparent;
    padding: 0;
    border-radius: 0;
    font-size: 100%;
}

blockquote {
    margin: 0 0 16px 0;
    padding: 0 1em;
    color: var(--color-blockquote);
    border-left: 0.25em solid var(--color-border);
}

ul, ol {
    margin-top: 0;
    margin-bottom: 16px;
    padding-left: 2em;
}

li + li { margin-top: 0.25em; }

table {
    border-collapse: collapse;
    width: 100%;
    margin-bottom: 16px;
}

table th, table td {
    border: 1px solid var(--color-table-border);
    padding: 6px 13px;
}

table th {
    font-weight: 600;
    background-color: var(--color-table-row);
}

table tr:nth-child(2n) {
    background-color: var(--color-table-row);
}

hr {
    border: 0;
    border-top: 1px solid var(--color-border);
    margin: 24px 0;
}

img {
    max-width: 100%;
    height: auto;
}

.task-list-item {
    list-style-type: none;
    margin-left: -1.5em;
}

.task-list-item input {
    margin-right: 0.5em;
}

/* Math styling */
.math { overflow-x: auto; }

/* Footnotes */
.footnotes {
    font-size: 0.9em;
    border-top: 1px solid var(--color-border);
    margin-top: 24px;
    padding-top: 16px;
}
";

    private static string GetDarkCss() => @"
:root {
    --color-bg: #0d1117;
    --color-fg: #c9d1d9;
    --color-border: #30363d;
    --color-code-bg: #161b22;
    --color-blockquote: #8b949e;
    --color-link: #58a6ff;
    --color-header: #c9d1d9;
    --color-table-border: #30363d;
    --color-table-row: #161b22;
}

* {
    box-sizing: border-box;
}

body {
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Noto Sans', Helvetica, Arial, sans-serif;
    font-size: 16px;
    line-height: 1.6;
    color: var(--color-fg);
    background-color: var(--color-bg);
    margin: 0;
    padding: 16px 24px;
    max-width: 100%;
}

.markdown-body {
    max-width: 100%;
    margin: 0;
}

h1, h2, h3, h4, h5, h6 {
    color: var(--color-header);
    margin-top: 24px;
    margin-bottom: 16px;
    font-weight: 600;
    line-height: 1.25;
}

h1 { font-size: 2em; border-bottom: 1px solid var(--color-border); padding-bottom: 0.3em; }
h2 { font-size: 1.5em; border-bottom: 1px solid var(--color-border); padding-bottom: 0.3em; }
h3 { font-size: 1.25em; }
h4 { font-size: 1em; }
h5 { font-size: 0.875em; }
h6 { font-size: 0.85em; color: var(--color-blockquote); }

p { margin-top: 0; margin-bottom: 16px; }

a {
    color: var(--color-link);
    text-decoration: none;
}
a:hover { text-decoration: underline; }

code {
    font-family: 'Cascadia Code', 'Fira Code', Consolas, 'Courier New', monospace;
    font-size: 85%;
    background-color: var(--color-code-bg);
    padding: 0.2em 0.4em;
    border-radius: 6px;
}

pre {
    background-color: var(--color-code-bg);
    border-radius: 6px;
    padding: 16px;
    overflow: auto;
    font-size: 85%;
    line-height: 1.45;
    margin-bottom: 16px;
}

pre code {
    background-color: transparent;
    padding: 0;
    border-radius: 0;
    font-size: 100%;
}

blockquote {
    margin: 0 0 16px 0;
    padding: 0 1em;
    color: var(--color-blockquote);
    border-left: 0.25em solid var(--color-border);
}

ul, ol {
    margin-top: 0;
    margin-bottom: 16px;
    padding-left: 2em;
}

li + li { margin-top: 0.25em; }

table {
    border-collapse: collapse;
    width: 100%;
    margin-bottom: 16px;
}

table th, table td {
    border: 1px solid var(--color-table-border);
    padding: 6px 13px;
}

table th {
    font-weight: 600;
    background-color: var(--color-table-row);
}

table tr:nth-child(2n) {
    background-color: var(--color-table-row);
}

hr {
    border: 0;
    border-top: 1px solid var(--color-border);
    margin: 24px 0;
}

img {
    max-width: 100%;
    height: auto;
}

.task-list-item {
    list-style-type: none;
    margin-left: -1.5em;
}

.task-list-item input {
    margin-right: 0.5em;
}

/* Math styling */
.math { overflow-x: auto; }

/* Footnotes */
.footnotes {
    font-size: 0.9em;
    border-top: 1px solid var(--color-border);
    margin-top: 24px;
    padding-top: 16px;
}
";
}
