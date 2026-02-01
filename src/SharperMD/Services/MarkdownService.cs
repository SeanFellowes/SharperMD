using System.IO;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;

namespace SharperMD.Services;

/// <summary>
/// Service for converting markdown to HTML using Markdig
/// </summary>
public class MarkdownService
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownService()
    {
        // Configure Markdig with all useful extensions
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()      // Tables, footnotes, task lists, etc.
            .UseEmojiAndSmiley()          // Emoji support
            .UseMathematics()             // LaTeX math support
            .UseAutoLinks()               // Auto-link URLs
            .UseTaskLists()               // GitHub-style task lists
            .UsePipeTables()              // Pipe tables
            .UseGridTables()              // Grid tables
            .UseListExtras()              // Extra list features
            .UseDefinitionLists()         // Definition lists
            .UseFootnotes()               // Footnotes
            .UseAutoIdentifiers()         // Auto IDs for headers
            .UseYamlFrontMatter()         // YAML front matter
            .UseMediaLinks()              // Media link extensions
            .UseFigures()                 // Figure captions
            .UseAbbreviations()           // Abbreviations
            .UseGenericAttributes()       // Generic attributes
            .UseSmartyPants()             // Smart typography
            .UseDiagrams()                // Mermaid diagram support
            .Build();
    }

    /// <summary>
    /// Convert markdown to HTML
    /// </summary>
    public string ToHtml(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return string.Empty;

        return Markdig.Markdown.ToHtml(markdown, _pipeline);
    }

    /// <summary>
    /// Parse markdown and return the document AST
    /// </summary>
    public MarkdownDocument Parse(string markdown)
    {
        return Markdig.Markdown.Parse(markdown, _pipeline);
    }

    /// <summary>
    /// Generate a full HTML document with styling for preview
    /// </summary>
    /// <param name="markdown">The markdown content</param>
    /// <param name="css">CSS styles to apply</param>
    /// <param name="isDarkTheme">Whether dark theme is active</param>
    /// <param name="documentBasePath">Optional base path for resolving relative URLs (directory containing the document)</param>
    public string ToFullHtml(string markdown, string css, bool isDarkTheme, string? documentBasePath = null)
    {
        var html = ToHtml(markdown);

        // Resolve relative paths to absolute file:// URLs if a base path is provided
        if (!string.IsNullOrEmpty(documentBasePath))
        {
            html = ResolveRelativePaths(html, documentBasePath);
        }

        var theme = isDarkTheme ? "dark" : "light";

        return $@"<!DOCTYPE html>
<html lang=""en"" data-theme=""{theme}"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
{css}
    </style>
    <link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/styles/{(isDarkTheme ? "github-dark" : "github")}.min.css"">
    <script src=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/highlight.min.js""></script>
    <script src=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/languages/powershell.min.js""></script>
    <script src=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/languages/sql.min.js""></script>
    <script src=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/languages/bash.min.js""></script>
    <script src=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/languages/csharp.min.js""></script>
    <script src=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/languages/json.min.js""></script>
    <script src=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/languages/xml.min.js""></script>
    <script src=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/languages/yaml.min.js""></script>
    <script src=""https://polyfill.io/v3/polyfill.min.js?features=es6""></script>
    <script id=""MathJax-script"" async src=""https://cdn.jsdelivr.net/npm/mathjax@3/es5/tex-mml-chtml.js""></script>
    <!-- Mermaid diagram support -->
    <script src=""https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js""></script>
</head>
<body>
    <div class=""markdown-body"">
{html}
    </div>
    <script>
        // Initialize Mermaid with theme
        mermaid.initialize({{
            startOnLoad: true,
            theme: '{(isDarkTheme ? "dark" : "default")}',
            securityLevel: 'loose',
            flowchart: {{
                useMaxWidth: true,
                htmlLabels: true
            }}
        }});

        hljs.highlightAll();

        // Re-render MathJax if content changes
        if (typeof MathJax !== 'undefined' && MathJax.typesetPromise) {{
            MathJax.typesetPromise();
        }}
    </script>
</body>
</html>";
    }

    /// <summary>
    /// Resolve relative paths in HTML to absolute file:// URLs
    /// </summary>
    private string ResolveRelativePaths(string html, string basePath)
    {
        // Regex to match src="..." and href="..." attributes
        // Captures the attribute name and the path value
        var pattern = @"(src|href)=""([^""]+)""";

        return Regex.Replace(html, pattern, match =>
        {
            var attribute = match.Groups[1].Value;
            var path = match.Groups[2].Value;

            // Skip if already an absolute URL (http, https, data, file, etc.)
            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("file:", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("#")) // Anchor links
            {
                return match.Value;
            }

            try
            {
                // Combine base path with relative path
                var absolutePath = Path.GetFullPath(Path.Combine(basePath, path));

                // Only convert if the file exists
                if (File.Exists(absolutePath))
                {
                    // Convert to file:// URL format
                    var fileUrl = new Uri(absolutePath).AbsoluteUri;
                    return $@"{attribute}=""{fileUrl}""";
                }
            }
            catch
            {
                // If path resolution fails, return original
            }

            return match.Value;
        });
    }

    /// <summary>
    /// Get line number mapping for scroll sync
    /// This maps source line numbers to approximate positions in the output
    /// </summary>
    public Dictionary<int, string> GetLineMapping(string markdown)
    {
        var mapping = new Dictionary<int, string>();
        var document = Parse(markdown);

        foreach (var block in document.Descendants())
        {
            if (block.Line > 0)
            {
                // Create an anchor ID for this line
                mapping[block.Line] = $"line-{block.Line}";
            }
        }

        return mapping;
    }
}
