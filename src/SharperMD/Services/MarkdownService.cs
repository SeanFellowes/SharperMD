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
    /// <summary>
    /// Virtual host name used to serve the bundled preview assets (highlight.js, MathJax,
    /// Mermaid) from the local application folder via WebView2's
    /// SetVirtualHostNameToFolderMapping. Keeping these assets local removes the runtime
    /// dependency on third-party CDNs (and the associated supply-chain risk) and lets the
    /// preview render fully offline.
    /// </summary>
    public const string AssetsHost = "sharpermd-assets";

    /// <summary>Base URL for the bundled preview assets served from <see cref="AssetsHost"/>.</summary>
    private const string AssetsBaseUrl = "https://" + AssetsHost;

    // Pinned library versions. The locally bundled assets must be kept in sync with these
    // versions; the CDN fallback (used for HTML export) references the same versions so that
    // exported documents render identically to the in-app preview.
    private const string HighlightJsVersion = "11.9.0";
    private const string MathJaxVersion = "3.2.2";
    private const string MermaidVersion = "10.9.3";

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

        // Normalize Azure DevOps Mermaid syntax before processing
        markdown = NormalizeMermaidSyntax(markdown);

        return Markdig.Markdown.ToHtml(markdown, _pipeline);
    }

    /// <summary>
    /// Normalize Azure DevOps Mermaid syntax (:::mermaid) to standard syntax (```mermaid)
    /// </summary>
    private string NormalizeMermaidSyntax(string markdown)
    {
        // Pattern to match :::mermaid ... ::: blocks
        // RegexOptions.Singleline allows . to match newlines
        var pattern = @":::mermaid\s*\r?\n(.*?)\r?\n:::";
        
        return Regex.Replace(
            markdown, 
            pattern, 
            "```mermaid\n$1\n```",
            RegexOptions.Singleline | RegexOptions.Multiline
        );
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
    /// <param name="useLocalAssets">
    /// When true (the default, used for the in-app preview), the highlight.js/MathJax/Mermaid
    /// assets are referenced from the locally bundled copies served via WebView2's virtual host.
    /// When false (used for HTML export), they are referenced from public CDNs so the exported
    /// standalone file remains portable when opened in an ordinary browser.
    /// </param>
    public string ToFullHtml(string markdown, string css, bool isDarkTheme, string? documentBasePath = null, bool useLocalAssets = true)
    {
        var html = ToHtml(markdown);

        // Resolve relative paths to absolute file:// URLs if a base path is provided
        if (!string.IsNullOrEmpty(documentBasePath))
        {
            html = ResolveRelativePaths(html, documentBasePath);
        }

        var theme = isDarkTheme ? "dark" : "light";
        var assetReferences = BuildAssetReferences(isDarkTheme, useLocalAssets);

        return $@"<!DOCTYPE html>
<html lang=""en"" data-theme=""{theme}"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
{css}
    </style>
{assetReferences}
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
    /// Build the &lt;head&gt; references for the preview assets (highlight.js, MathJax, Mermaid).
    /// Uses the locally bundled assets for the in-app preview, or public CDNs (same pinned
    /// versions) for portable HTML export.
    /// </summary>
    private static string BuildAssetReferences(bool isDarkTheme, bool useLocalAssets)
    {
        var hlTheme = isDarkTheme ? "github-dark" : "github";
        string[] languages = { "powershell", "sql", "bash", "csharp", "json", "xml", "yaml" };

        string hlStylesUrl, hlCoreUrl, hlLangBaseUrl, mathJaxUrl, mermaidUrl;

        if (useLocalAssets)
        {
            hlStylesUrl = $"{AssetsBaseUrl}/highlight/styles/{hlTheme}.min.css";
            hlCoreUrl = $"{AssetsBaseUrl}/highlight/highlight.min.js";
            hlLangBaseUrl = $"{AssetsBaseUrl}/highlight/languages";
            mathJaxUrl = $"{AssetsBaseUrl}/mathjax/tex-mml-chtml.js";
            mermaidUrl = $"{AssetsBaseUrl}/mermaid/mermaid.min.js";
        }
        else
        {
            var hlCdn = $"https://cdnjs.cloudflare.com/ajax/libs/highlight.js/{HighlightJsVersion}";
            hlStylesUrl = $"{hlCdn}/styles/{hlTheme}.min.css";
            hlCoreUrl = $"{hlCdn}/highlight.min.js";
            hlLangBaseUrl = $"{hlCdn}/languages";
            mathJaxUrl = $"https://cdn.jsdelivr.net/npm/mathjax@{MathJaxVersion}/es5/tex-mml-chtml.js";
            mermaidUrl = $"https://cdn.jsdelivr.net/npm/mermaid@{MermaidVersion}/dist/mermaid.min.js";
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($@"    <link rel=""stylesheet"" href=""{hlStylesUrl}"">");
        sb.AppendLine($@"    <script src=""{hlCoreUrl}""></script>");
        foreach (var lang in languages)
        {
            sb.AppendLine($@"    <script src=""{hlLangBaseUrl}/{lang}.min.js""></script>");
        }
        sb.AppendLine($@"    <script id=""MathJax-script"" async src=""{mathJaxUrl}""></script>");
        sb.AppendLine(@"    <!-- Mermaid diagram support -->");
        sb.Append($@"    <script src=""{mermaidUrl}""></script>");
        return sb.ToString();
    }

    /// <summary>
    /// Resolve relative image paths in HTML to base64 data URIs
    /// </summary>
    private string ResolveRelativePaths(string html, string basePath)
    {
        // Regex to match src="..." attributes (primarily for images)
        var srcPattern = @"src=""([^""]+)""";

        return Regex.Replace(html, srcPattern, match =>
        {
            var path = match.Groups[1].Value;

            // Skip if already an absolute URL (http, https, data, file, etc.)
            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
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
                    // Get MIME type based on file extension
                    var extension = Path.GetExtension(absolutePath).ToLowerInvariant();
                    var mimeType = extension switch
                    {
                        ".png" => "image/png",
                        ".jpg" or ".jpeg" => "image/jpeg",
                        ".gif" => "image/gif",
                        ".svg" => "image/svg+xml",
                        ".webp" => "image/webp",
                        ".bmp" => "image/bmp",
                        ".ico" => "image/x-icon",
                        _ => null
                    };

                    // Only convert known image types to data URIs
                    if (mimeType != null)
                    {
                        var bytes = File.ReadAllBytes(absolutePath);
                        var base64 = Convert.ToBase64String(bytes);
                        return $@"src=""data:{mimeType};base64,{base64}""";
                    }
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
