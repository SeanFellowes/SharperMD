# Changelog

All notable changes to SharperMD will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-02-02

### Added
- **Multiple Document Tabs**: Open and work with multiple markdown files simultaneously
  - Tab bar with themed styling (light/dark)
  - Close button on each tab with hover effect
  - Right-click context menu: Close, Close Others, Close All, Copy Path
  - Keyboard shortcuts: Ctrl+Tab (next), Ctrl+Shift+Tab (previous), Ctrl+W (close)
  - Smart tab behavior: new files replace empty untitled tabs
  - Duplicate detection: opening an already-open file switches to its tab
- **Session Restore**: Automatically reopens all tabs from your last session on launch
  - Persists open document paths and active tab index
  - Gracefully handles missing files (skips with notification)
- **Close Tab** menu item in File menu

### Changed
- Auto-save now iterates all dirty documents across tabs
- Window close now prompts for each unsaved document
- Updated keyboard shortcuts help dialog with tab shortcuts
- Updated About dialog to version 1.1.0

### Technical
- Added `OpenDocuments` collection and `SelectedTabIndex` to MainViewModel
- Added tab-related theme brushes for light/dark modes
- Added per-document editor state tracking (caret, scroll position, selection)
- Added session persistence to AppSettings

---

## [1.0.0] - 2026-02-01

### Added

#### Core Features
- **View Mode**: Beautiful markdown rendering with GitHub-style formatting
- **Edit Mode**: Side-by-side editor with live preview and scroll sync
- **Command-line Support**: Open files directly via `SharperMD.exe "file.md"`

#### Editor
- Markdown syntax highlighting in editor
- Formatting toolbar (bold, italic, headers, lists, links, images, code, quotes)
- Find & Replace dialog with regex support and Replace All
- Line numbers (toggleable)
- Word wrap (toggleable)
- Fixed-width font (Cascadia Mono/Consolas)
- Standard keyboard shortcuts

#### Markdown Rendering
- Headers (H1-H6)
- Bold, italic, strikethrough text
- Ordered and unordered lists
- Task lists (checkboxes)
- Tables with column alignment
- Code blocks with syntax highlighting (20+ languages)
- Inline code
- Blockquotes (including nested)
- Horizontal rules
- Links and images (including local images)
- Footnotes
- Definition lists
- Abbreviations
- Emojis
- LaTeX math equations (via MathJax)
- Mermaid diagrams (flowcharts, sequence, class, state, Gantt, pie charts)

#### User Experience
- Light/Dark theme (follows Windows system theme)
- Welcome screen with New, Open, and Recent Files
- Recent files list (up to 10 files)
- Drag & drop file support
- Font size control (Ctrl+Scroll, menu, or shortcuts)
- Window position and size memory
- Status bar with word count, line count, character count, cursor position

#### Data Safety
- Auto-save drafts every 30 seconds
- Crash recovery for unsaved work
- Unsaved changes warning on close

#### Installation
- Windows installer with optional .md file association
- Installs to Program Files with Start Menu and Desktop shortcuts
- Clean uninstall support

### Technical
- Built with .NET 8.0 and WPF
- Markdig 0.44.0 for markdown parsing
- AvalonEdit for text editing
- WebView2 (Chromium) for HTML rendering
- highlight.js for code syntax highlighting
- MathJax for mathematical equations
- Mermaid for diagram rendering

---

## Future Plans

- Spell checking (v1.2 - multi-language support)
- Print support
- PDF export
- Custom themes
- Outline/TOC sidebar
- Plugin system
- Vim keybindings option
