using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using Markdig;

namespace Quotes;

public sealed record QuoteContent(string Html, string PlainText);

public static partial class QuoteService
{
    //private static readonly string[] SupportedExtensions = [".txt", ".html", ".htm", ".md"];
    private static readonly string[] SupportedExtensions = [".md"];
    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseSoftlineBreakAsHardlineBreak() // <-- Enables automatic <br /> rendering
        .Build();

    public static QuoteContent? GetRandomQuote(string folder, string defaultFontFamily, double defaultFontSize)
    {
        var files = GetSupportedQuoteFiles(folder);
        if (files.Length == 0)
            return null;

        var file = DequeueNextQuoteFile(files);
        return BuildQuoteContent(file, defaultFontFamily, defaultFontSize);
    }

    private static string[] GetSupportedQuoteFiles(string folder)
    {
        if (!Directory.Exists(folder))
            return [];

        return Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
            .Where(file => SupportedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .ToArray();
    }

    private static string DequeueNextQuoteFile(string[] files)
    {
        var config = AppConfig.Load();

        config.ShuffleQueue.RemoveAll(path => !files.Contains(path, StringComparer.OrdinalIgnoreCase));

        if (config.ShuffleQueue.Count == 0)
            config.ShuffleQueue.AddRange(files.OrderBy(_ => Random.Shared.Next()));

        var file = config.ShuffleQueue[0];
        config.ShuffleQueue.RemoveAt(0);
        config.Save();

        return file;
    }

    private static QuoteContent BuildQuoteContent(string file, string defaultFontFamily, double defaultFontSize)
    {
        var content = File.ReadAllText(file).Trim();
        var extension = Path.GetExtension(file);

        if (IsHtml(extension))
            return BuildHtmlQuoteContent(content);

        if (IsMarkdown(extension))
            return BuildMarkdownQuoteContent(content, defaultFontFamily, defaultFontSize);

        return BuildTextQuoteContent(content, defaultFontFamily, defaultFontSize);
    }

    private static bool IsHtml(string extension) =>
        extension.Equals(".html", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".htm", StringComparison.OrdinalIgnoreCase);

    private static bool IsMarkdown(string extension) =>
        extension.Equals(".md", StringComparison.OrdinalIgnoreCase);

    private static QuoteContent BuildHtmlQuoteContent(string content)
    {
        var plainText = WebUtility.HtmlDecode(HtmlTagRegex().Replace(content, " ")).Trim();
        return new QuoteContent(content, plainText);
    }

    private static QuoteContent BuildMarkdownQuoteContent(string content, string defaultFontFamily, double defaultFontSize)
    {
        var renderedHtml = Markdown.ToHtml(content, MarkdownPipeline);
        var plainText = Markdown.ToPlainText(content, MarkdownPipeline).Trim();
        var fontStyle = BuildFontStyle(defaultFontFamily, defaultFontSize);

        var mdHtml = $$"""
<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8">
<style>
body { margin: 0; color: #1A1A1A; background: transparent; {{fontStyle}} line-height: 1; }
p { margin: 0 0 0.5em 0; }
p:last-child { margin-bottom: 0; }
blockquote { margin: 0 0 0.5em 1em; padding-left: 0.5em; border-left: 3px solid #ccc; font-style: italic; }
/* Removes the empty space between a paragraph and a list right below it */
p + ol, p + ul { margin-top: 0; }
</style>
</head>
<body>
{{renderedHtml}}
</body>
</html>
""";

        return new QuoteContent(mdHtml, plainText);
    }

    private static QuoteContent BuildTextQuoteContent(string content, string defaultFontFamily, double defaultFontSize)
    {
        var encodedText = WebUtility.HtmlEncode(content);
        var fontStyle = BuildFontStyle(defaultFontFamily, defaultFontSize);

        var html = $$"""
<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8">
<style>
body { margin: 0; color: #1A1A1A; background: transparent; }
pre { white-space: pre-wrap; margin: 0; {{fontStyle}} font-style: italic; line-height: 1.5; color: #1A1A1A; }
</style>
</head>
<body>
<pre>{{encodedText}}</pre>
</body>
</html>
""";

        return new QuoteContent(html, content);
    }

    private static string BuildFontStyle(string defaultFontFamily, double defaultFontSize)
    {
        var encodedFontFamily = WebUtility.HtmlEncode(defaultFontFamily);
        var fontSizeValue = defaultFontSize.ToString("0.##", CultureInfo.InvariantCulture);
        return $"font-family: '{encodedFontFamily}', serif !important; font-size: {fontSizeValue}px !important;";
    }

    [GeneratedRegex("<[^>]+>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegex();
}
