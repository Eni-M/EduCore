using System.Globalization;
using System.Text.RegularExpressions;

namespace EduCore.Localization;

public interface IMessageLocalizer
{
    // Translates an English message into the request's language, or returns it unchanged when
    // there is no translation. Never throws: a missing entry is a cosmetic problem, and losing
    // the error itself would be a real one.
    string Translate(string message);

    string Translate(string message, string culture);
}

public class MessageLocalizer : IMessageLocalizer
{
    // English templates compiled once into "match this shape and capture the values" regexes.
    // Built from MessageCatalog.Templates so adding a template needs no change here.
    private static readonly (string Template, Regex Pattern)[] Templates =
    [
        .. MessageCatalog.Templates
            .OrderByDescending(t => t.Length)   // longest first, so a specific template wins
            .Select(t => (t, new Regex(ToPattern(t), RegexOptions.Compiled | RegexOptions.CultureInvariant)))
    ];

    public string Translate(string message) =>
        Translate(message, CultureInfo.CurrentUICulture.Name);

    public string Translate(string message, string culture)
    {
        var language = Language.Normalise(culture);

        if (language == Language.English || string.IsNullOrEmpty(message))
            return message;

        if (!MessageCatalog.Messages.TryGetValue(language, out var catalogue))
            return message;

        if (catalogue.TryGetValue(message, out var exact))
            return exact;

        foreach (var (template, pattern) in Templates)
        {
            var match = pattern.Match(message);

            if (!match.Success || !catalogue.TryGetValue(template, out var translated))
                continue;

            var values = match.Groups.Cast<Group>().Skip(1).Select(g => (object)g.Value).ToArray();

            // The first value of the not-found template is a resource name of ours, not something
            // a user typed, so it is safe -- and necessary -- to translate it as well.
            if (template.StartsWith("{0} not found", StringComparison.Ordinal)
                && values.Length > 0
                && MessageCatalog.Nouns.TryGetValue(language, out var nouns)
                && nouns.TryGetValue((string)values[0], out var noun))
            {
                values[0] = noun;
            }

            return string.Format(CultureInfo.InvariantCulture, translated, values);
        }

        return message;
    }

    // "A city named {0} already exists." becomes ^A\ city\ named\ (.+?)\ already\ exists\.$
    // Everything outside a placeholder is escaped, so punctuation cannot act as a metacharacter.
    private static string ToPattern(string template)
    {
        var pattern = Regex.Replace(
            Regex.Escape(template),
            @"\\\{(\d+)\\?\}",
            "(.+?)");

        return $"^{pattern}$";
    }
}
