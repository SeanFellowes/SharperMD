# SharperMD

A beautiful, full-featured markdown viewer and editor for Windows 10/11, built with C# and WPF.

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)
![Windows](https://img.shields.io/badge/Platform-Windows%2010%2F11-0078D6)
![License](https://img.shields.io/badge/License-MIT-green)

## Features

### Core Functionality
- **Multiple Document Tabs**: Work with multiple markdown files simultaneously
- **Session Restore**: Automatically reopens your last session's tabs on launch
- **View Mode**: Open markdown files for distraction-free reading with beautiful rendering
- **Edit Mode**: Side-by-side editor and live preview for real-time markdown authoring
- **Command-line Support**: Open files directly from the command line or file explorer

### Editor Features
- **Syntax Highlighting**: Markdown-aware syntax highlighting in the editor
- **Line Numbers**: Toggle line numbers on/off
- **Word Wrap**: Configurable word wrap
- **Fixed-width Font**: Uses Cascadia Mono/Consolas for comfortable coding
- **Formatting Toolbar**: Quick access buttons for bold, italic, headers, lists, links, images, code blocks, and more
- **Keyboard Shortcuts**: Standard shortcuts for all common operations

### Markdown Support
- **Headers** (H1-H6)
- **Bold**, *Italic*, ~~Strikethrough~~
- Bullet and numbered lists
- Task lists (checkboxes)
- Tables (pipe and grid)
- Code blocks with syntax highlighting (supports 190+ languages including SQL, PowerShell, C#, Bash)
- Blockquotes
- Horizontal rules
- Links and images (including local images)
- Footnotes
- Definition lists
- Abbreviations
- Emojis
- **Math Support**: LaTeX/MathML via MathJax
- **Mermaid Diagrams**: Flowcharts, sequence diagrams, class diagrams, Gantt charts, and more

### User Experience
- **Light/Dark Theme**: Follows Windows system theme or manual toggle
- **Welcome Screen**: Quick access to new documents, open files, and recent files
- **Recent Files**: Quick access to recently opened documents
- **Drag & Drop**: Drop markdown files directly onto the window
- **Scroll Sync**: Editor and preview scroll together
- **Font Size Control**: Ctrl+Mouse Wheel or menu options to zoom
- **Status Bar**: Word count, line count, character count, cursor position

### Data Safety
- **Auto-Save**: Automatic draft saving every 30 seconds
- **Crash Recovery**: Recover unsaved work after unexpected closure
- **Unsaved Changes Warning**: Prompts before closing with unsaved changes
- **Export to HTML**: Save rendered markdown as standalone HTML

## Screenshots

<!--
HOW TO ADD SCREENSHOTS:
1. Create a "screenshots" folder in the repo root
2. Take screenshots of SharperMD showing:
   - Dark mode viewing a document
   - Light mode with side-by-side editing
   - Mermaid diagram rendering
   - Code block with syntax highlighting
3. Save as PNG files (e.g., dark-mode.png, edit-mode.png)
4. Uncomment and update the image references below

![Dark Mode](screenshots/dark-mode.png)
![Edit Mode](screenshots/edit-mode.png)
![Mermaid Diagrams](screenshots/mermaid.png)
-->

*Screenshots coming soon - see [TEST-MARKDOWN.md](TEST-MARKDOWN.md) for a demonstration of all supported features*

## Requirements

- Windows 10 (version 1809+) or Windows 11
- WebView2 Runtime (auto-installed on Windows 11, may need installation on Windows 10)

**Note:** The installer includes all .NET dependencies, so no separate .NET installation is required.

## Installation

### Option 1: Download Installer (Recommended)
1. Download `SharperMD-Setup-x.x.x.exe` from the [Releases](https://github.com/SeanFellowes/SharperMD/releases) page
2. Run the installer
3. Optionally check "Associate .md files with SharperMD" during installation
4. Launch from Start Menu or Desktop shortcut

### Option 2: Build from Source
```bash
# Clone the repository
git clone https://github.com/SeanFellowes/SharperMD.git
cd SharperMD

# Build the project
dotnet build -c Release

# Run
dotnet run --project src/SharperMD/SharperMD.csproj
```

### Option 3: Build Installer from Source
Prerequisites:
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Inno Setup 6](https://jrsoftware.org/isinfo.php)

```powershell
# Clone and build installer
git clone https://github.com/SeanFellowes/SharperMD.git
cd SharperMD

# Run the build script (PowerShell)
.\build-installer.ps1

# Or use the batch file
.\build-installer.bat
```

The installer will be created at `installer/output/SharperMD-Setup-1.0.0.exe`

## Usage

### Opening Files
```bash
# Open SharperMD directly
SharperMD.exe

# Open a specific markdown file
SharperMD.exe "path/to/your/file.md"
```

### Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+N` | New document |
| `Ctrl+O` | Open file |
| `Ctrl+S` | Save |
| `Ctrl+Shift+S` | Save As |
| `F2` or `Ctrl+E` | Toggle edit mode |
| `Ctrl+B` | Bold |
| `Ctrl+I` | Italic |
| `Ctrl+K` | Insert link |
| `Ctrl+Shift+K` | Insert image |
| `Ctrl++` | Increase font size |
| `Ctrl+-` | Decrease font size |
| `Ctrl+0` | Reset font size |
| `Ctrl+Scroll` | Zoom in/out |
| `Ctrl+Z` | Undo |
| `Ctrl+Y` | Redo |
| `Ctrl+F` | Find |
| `Ctrl+H` | Find & Replace |
| `F3` | Find next |
| `Shift+F3` | Find previous |
| `Ctrl+Tab` | Next tab |
| `Ctrl+Shift+Tab` | Previous tab |
| `Ctrl+W` | Close tab |

### File Association (Optional)
To associate `.md` files with SharperMD:

1. Right-click any `.md` file
2. Select "Open with" → "Choose another app"
3. Browse to `SharperMD.exe`
4. Check "Always use this app to open .md files"

## Technology Stack

- **Framework**: .NET 8.0 / WPF
- **Markdown Parser**: [Markdig](https://github.com/xoofx/markdig) - Extensible, fast, CommonMark compliant
- **Text Editor**: [AvalonEdit](https://github.com/icsharpcode/AvalonEdit) - WPF-based text editor
- **HTML Rendering**: [WebView2](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) - Chromium-based rendering
- **Code Highlighting**: [highlight.js](https://highlightjs.org/) - Syntax highlighting for code blocks
- **Math Rendering**: [MathJax](https://www.mathjax.org/) - LaTeX math support
- **MVVM**: [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) - Modern MVVM toolkit

## Project Structure

```
SharperMD/
├── SharperMD.sln
├── README.md
├── LICENSE
├── CHANGELOG.md
├── installer/                      # Inno Setup installer scripts
│   └── SharperMD.iss
└── src/
    └── SharperMD/
        ├── App.xaml(.cs)           # Application entry point
        ├── MainWindow.xaml(.cs)    # Main window UI
        ├── Models/
        │   ├── AppSettings.cs      # Settings persistence
        │   └── Document.cs         # Document model with dirty tracking
        ├── ViewModels/
        │   └── MainViewModel.cs    # Main view model
        ├── Views/
        │   └── FindReplaceDialog.xaml(.cs)  # Find & Replace dialog
        ├── Services/
        │   ├── MarkdownService.cs  # Markdown to HTML conversion
        │   └── ThemeService.cs     # Theme management
        ├── Converters/
        │   └── BoolToVisibilityConverter.cs
        └── Resources/
            └── MarkdownSyntax.xshd # Syntax highlighting definition
```

## Configuration

Settings are stored in `%APPDATA%\SharperMD\settings.json`:

```json
{
  "theme": "System",
  "editorFontSize": 14,
  "previewFontSize": 16,
  "editorFontFamily": "Cascadia Mono, Consolas, Courier New",
  "showWelcomeScreen": true,
  "scrollSyncEnabled": true,
  "autoSaveEnabled": true,
  "autoSaveIntervalSeconds": 30,
  "wordWrap": true,
  "showLineNumbers": true,
  "recentFiles": []
}
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## Authors

- **Sean Fellowes** - Project Creator & Design
- **Claude (Anthropic)** - Development

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- [Markdig](https://github.com/xoofx/markdig) by Alexandre Mutel
- [AvalonEdit](https://github.com/icsharpcode/AvalonEdit) by AvalonEdit Contributors
- [highlight.js](https://highlightjs.org/)
- [MathJax](https://www.mathjax.org/)
- Icons and design inspiration from VS Code

## Roadmap

- [x] Multiple document tabs (v1.1)
- [ ] Spell checking (v1.2 - multi-language support)
- [ ] Print support
- [ ] PDF export
- [ ] Custom themes
- [ ] Plugin system
- [ ] Vim keybindings option
- [ ] Outline/TOC sidebar
