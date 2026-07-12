using System.IO;

namespace Quotes;

public static class QuoteService
{
    public static string? GetRandomQuote(string folder)
    {
        if (!Directory.Exists(folder))
            return null;

        var files = Directory.GetFiles(folder, "*.txt");
        if (files.Length == 0)
            return null;

        var file = files[Random.Shared.Next(files.Length)];
        return File.ReadAllText(file).Trim();
    }
}
