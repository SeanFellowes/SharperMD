# Changelog

All notable changes to SharperMD will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2024-02-01

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

- Print support
- PDF export
- Custom themes
- Outline/TOC sidebar
- Spell checking
- Multiple document tabs
- Plugin system
