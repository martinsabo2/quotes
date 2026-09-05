using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using Markdig;

namespace Quotes;

public sealed record QuoteContent(string Html, string PlainText);

public static partial class QuoteService
{
    private static readonly string[] SupportedExtensions = [".txt", ".html", ".htm", ".md"];
    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseSoftlineBreakAsHardlineBreak() // <-- Enables automatic <br /> rendering
        .Build();

    public static QuoteContent? GetRandomQuote(string folder, string defaultFontFamily, double defaultFontSize)
    {
        if (!Directory.Exists(folder))
            return null;

        var files = Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
            .Where(file => SupportedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .ToArray();

        if (files.Length == 0)
            return null;

        var config = AppConfig.Load();

        // Remove queued entries that no longer exist on disk
        config.ShuffleQueue.RemoveAll(p => !files.Contains(p, StringComparer.OrdinalIgnoreCase));

        // Refill with a fresh shuffle when the queue is exhausted
        if (config.ShuffleQueue.Count == 0)
            config.ShuffleQueue.AddRange(files.OrderBy(_ => Random.Shared.Next()));

        // Dequeue the next file
        var file = config.ShuffleQueue[0];
        config.ShuffleQueue.RemoveAt(0);
        config.Save();
        var content = File.ReadAllText(file).Trim();
        var extension = Path.GetExtension(file);

        if (extension.Equals(".html", StringComparison.OrdinalIgnoreCase) || extension.Equals(".htm", StringComparison.OrdinalIgnoreCase))
        {
            var plainText = WebUtility.HtmlDecode(HtmlTagRegex().Replace(content, " ")).Trim();
            return new QuoteContent(content, plainText);
        }

        var encodedFontFamily = WebUtility.HtmlEncode(defaultFontFamily);
        var fontSizeValue = defaultFontSize.ToString("0.##", CultureInfo.InvariantCulture);

        if (extension.Equals(".md", StringComparison.OrdinalIgnoreCase))
        {
            var renderedHtml = Markdown.ToHtml(content, MarkdownPipeline);
            var plainText = Markdown.ToPlainText(content, MarkdownPipeline).Trim();

            var mdHtml = $$"""
<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8">
<style>
body { margin: 0; color: #1A1A1A; background: transparent; font-family: '{{encodedFontFamily}}', serif; font-size: {{fontSizeValue}}px; line-height: 1.5; }
p { margin: 0 0 0.5em 0; }
p:last-child { margin-bottom: 0; }
blockquote { margin: 0 0 0.5em 1em; padding-left: 0.5em; border-left: 3px solid #ccc; font-style: italic; }
</style>
</head>
<body>
{{renderedHtml}}
</body>
</html>
""";
            return new QuoteContent(mdHtml, plainText);
        }

        var encodedText = WebUtility.HtmlEncode(content);

        var html = $$"""
<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8">
<style>
body { margin: 0; color: #1A1A1A; background: transparent; }
pre { white-space: pre-wrap; margin: 0; font-family: '{{encodedFontFamily}}', serif !important; font-size: {{fontSizeValue}}px !important; font-style: italic; line-height: 1.5; color: #1A1A1A; }
</style>
</head>
<body>
<pre>{{encodedText}}</pre>
</body>
</html>
""";

        return new QuoteContent(html, content);
    }

    [GeneratedRegex("<[^>]+>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegex();
}
