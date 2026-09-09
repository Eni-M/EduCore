using System.Text.RegularExpressions;
using EduCore.Localization;
using Xunit;

namespace EduCore.Tests;

// The catalogue keys on the English message text, which buys us 240 untouched throw sites and
// costs us this: editing an English message silently orphans its translations. These tests are
// what turns that from a silent regression into a failing build.
public class LocalizationTests
{
    // ---- the catalogue agrees with itself ---------------------------------

    [Fact]
    public void Every_message_is_translated_into_both_languages()
    {
        var albanian = MessageCatalog.Messages[Language.Albanian];
        var serbian = MessageCatalog.Messages[Language.Serbian];

        var onlyAlbanian = albanian.Keys.Except(serbian.Keys).OrderBy(k => k).ToList();
        var onlySerbian = serbian.Keys.Except(albanian.Keys).OrderBy(k => k).ToList();

        Assert.True(
            onlyAlbanian.Count == 0 && onlySerbian.Count == 0,
            $"Missing from Serbian: {string.Join(" | ", onlyAlbanian)}\n" +
            $"Missing from Albanian: {string.Join(" | ", onlySerbian)}");
    }

    [Fact]
    public void Every_template_is_translated_and_keeps_its_placeholders()
    {
        foreach (var language in new[] { Language.Albanian, Language.Serbian })
        {
            var catalogue = MessageCatalog.Messages[language];

            foreach (var template in MessageCatalog.Templates)
            {
                Assert.True(catalogue.ContainsKey(template), $"{language} is missing template: {template}");

                var placeholders = Placeholders(template);
                var translated = Placeholders(catalogue[template]);

                Assert.True(
                    placeholders.SetEquals(translated),
                    $"{language} translation of '{template}' uses placeholders " +
                    $"{{{string.Join(",", translated)}}} but the English uses {{{string.Join(",", placeholders)}}}");
            }
        }
    }

    [Fact]
    public void Every_resource_noun_is_translated_into_both_languages()
    {
        var albanian = MessageCatalog.Nouns[Language.Albanian];
        var serbian = MessageCatalog.Nouns[Language.Serbian];

        Assert.Equal(albanian.Keys.OrderBy(k => k), serbian.Keys.OrderBy(k => k));
    }

    [Fact]
    public void No_translation_is_left_as_its_english_source()
    {
        // A copy-pasted entry that was never actually translated is worse than a missing one:
        // the coverage test above passes and the user still reads English.
        foreach (var language in new[] { Language.Albanian, Language.Serbian })
        {
            var untranslated = MessageCatalog.Messages[language]
                .Where(e => e.Key == e.Value)
                .Select(e => e.Key)
                .ToList();

            Assert.True(untranslated.Count == 0,
                $"{language} entries identical to the English source: {string.Join(" | ", untranslated)}");
        }
    }

    // ---- the localizer behaves ---------------------------------------------

    private static readonly MessageLocalizer Localizer = new();

    [Theory]
    [InlineData("en", "You have no role.", "You have no role.")]
    [InlineData("sq", "You have no role.", "Nuk keni asnjë rol.")]
    [InlineData("sr-Latn", "You have no role.", "Nemate nijednu ulogu.")]
    [InlineData("sq-AL", "You have no role.", "Nuk keni asnjë rol.")]
    [InlineData("sr", "You have no role.", "Nemate nijednu ulogu.")]
    public void Literal_messages_translate(string culture, string english, string expected) =>
        Assert.Equal(expected, Localizer.Translate(english, culture));

    [Fact]
    public void An_unknown_message_falls_through_to_english()
    {
        const string unknown = "Something nobody has translated yet.";
        Assert.Equal(unknown, Localizer.Translate(unknown, "sq"));
    }

    [Fact]
    public void Interpolated_messages_match_their_template_and_keep_the_value()
    {
        var albanian = Localizer.Translate("A school named Gjimnazi Sami Frasheri already exists.", "sq");

        Assert.Contains("Gjimnazi Sami Frasheri", albanian);
        Assert.DoesNotContain("already exists", albanian);
    }

    [Fact]
    public void Not_found_translates_the_resource_noun_but_not_the_id()
    {
        var id = Guid.NewGuid();

        var albanian = Localizer.Translate($"Student not found with id: {id}", "sq");
        var serbian = Localizer.Translate($"Student not found with id: {id}", "sr-Latn");

        Assert.Contains("Nxënësi", albanian);
        Assert.Contains("Učenik", serbian);
        Assert.Contains(id.ToString(), albanian);
        Assert.DoesNotContain("Student", albanian);
    }

    [Fact]
    public void A_value_that_looks_like_a_message_is_not_itself_translated()
    {
        // A school could legitimately be named after something in the catalogue. The captured
        // value is user data and must come back exactly as it went in.
        var message = Localizer.Translate("A school named You have no role. already exists.", "sq");

        Assert.Contains("You have no role.", message);
    }

    // ---- the catalogue matches the source ----------------------------------

    [Fact]
    public void Every_message_thrown_in_the_source_has_a_translation()
    {
        // The test project lives inside the application project, so the sources are always
        // reachable. Asserting rather than skipping keeps this from quietly becoming a no-op.
        var root = RepositoryRoot();
        Assert.NotNull(root);

        var thrown = new HashSet<string>();

        var literal = new Regex(
            @"(?:BusinessException|NotFoundException|ForbiddenException|UnauthorizedException|ConflictException)\(""([^""]+)""\)");
        var validation = new Regex(@"WithMessage\(""([^""]+)""\)");

        foreach (var folder in new[] { "Services", "Endpoints", "Security", "Repositories", "Data", "Validation" })
        {
            var path = Path.Combine(root, folder);

            if (!Directory.Exists(path))
                continue;

            foreach (var file in Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(file);

                foreach (Match m in literal.Matches(text))
                    thrown.Add(m.Groups[1].Value);

                foreach (Match m in validation.Matches(text))
                    thrown.Add(m.Groups[1].Value);
            }
        }

        Assert.NotEmpty(thrown);

        var albanian = MessageCatalog.Messages[Language.Albanian];
        var missing = thrown.Where(m => !albanian.ContainsKey(m)).OrderBy(m => m).ToList();

        Assert.True(missing.Count == 0,
            "These messages are thrown in the source but have no entry in MessageCatalog:\n  "
            + string.Join("\n  ", missing));
    }

    private static HashSet<string> Placeholders(string text) =>
        [.. Regex.Matches(text, @"\{(\d+)\}").Select(m => m.Groups[1].Value)];

    // Walks up from the test binary looking for the application project, so the source scan
    // works from the test runner's working directory without hard-coded paths.
    private static string? RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "EduCore.csproj")))
                return dir.FullName;

            dir = dir.Parent;
        }

        return null;
    }
}
