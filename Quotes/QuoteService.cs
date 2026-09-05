using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace Quotes;

public sealed record QuoteContent(string Html, string PlainText);

public static partial class QuoteService
{
    private static readonly string[] SupportedExtensions = [".txt", ".html", ".htm"];

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

        var encodedText = WebUtility.HtmlEncode(content);
        var encodedFontFamily = WebUtility.HtmlEncode(defaultFontFamily);
        var fontSizeValue = defaultFontSize.ToString("0.##", CultureInfo.InvariantCulture);

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
