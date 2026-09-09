using System.Globalization;

namespace EduCore.Localization;

// The languages the platform answers in. English is the source language: every message is
// written in English at the throw site, and the catalogue translates it on the way out.
public static class Language
{
    public const string English = "en";
    public const string Albanian = "sq";

    // Serbian in Latin script. The tag matters -- "sr" alone resolves to Cyrillic on some
    // platforms, and the catalogue only carries Latin.
    public const string Serbian = "sr-Latn";

    public static readonly string[] Supported = [English, Albanian, Serbian];

    public static readonly CultureInfo[] SupportedCultures =
        [.. Supported.Select(CultureInfo.GetCultureInfo)];

    // Matches a request's culture to one we actually carry. "sq-AL" and "sq" both mean Albanian;
    // "sr", "sr-Latn-RS" and "sr-Latn" all mean the Latin catalogue.
    public static string Normalise(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
            return English;

        var name = culture.Trim();

        if (name.StartsWith("sq", StringComparison.OrdinalIgnoreCase))
            return Albanian;

        if (name.StartsWith("sr", StringComparison.OrdinalIgnoreCase))
            return Serbian;

        return English;
    }
}
