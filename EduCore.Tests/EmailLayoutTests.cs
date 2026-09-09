using EduCore.Services.Email;
using Xunit;

namespace EduCore.Tests;

// The HTML body is assembled by string interpolation, and names and email addresses reach it
// straight from user input. That combination is how a mail template becomes an injection point,
// so escaping is the property worth holding down here.
public class EmailLayoutTests
{
    private const string AppUrl = "https://educore.example";

    [Fact]
    public void A_message_renders_both_a_text_and_an_html_part()
    {
        var body = EmailLayout.Render(
            "Your schedule", [new EmailBlock.Paragraph("Two lessons tomorrow.")], AppUrl);

        Assert.Contains("Two lessons tomorrow.", body.Text);
        Assert.Contains("Two lessons tomorrow.", body.Html);
        Assert.StartsWith("<!DOCTYPE html>", body.Html);
        Assert.DoesNotContain("<", body.Text);
    }

    [Fact]
    public void User_supplied_text_is_escaped_in_the_html()
    {
        // Somebody whose name is stored with markup in it must not get that markup rendered --
        // and must certainly not be able to put a link into mail sent from our address.
        var body = EmailLayout.Render(
            "Welcome",
            [new EmailBlock.Paragraph("Hello <script>alert(1)</script> & \"friends\"")],
            AppUrl);

        Assert.DoesNotContain("<script>", body.Html);
        Assert.Contains("&lt;script&gt;", body.Html);
        Assert.Contains("&amp;", body.Html);
    }

    [Fact]
    public void A_heading_is_escaped_too()
    {
        var body = EmailLayout.Render("<b>Term report</b>", [], AppUrl);

        Assert.DoesNotContain("<b>Term report</b>", body.Html);
        Assert.Contains("&lt;b&gt;Term report", body.Html);
    }

    [Fact]
    public void A_button_carries_its_url_into_both_parts()
    {
        var body = EmailLayout.Render(
            "Reset your password",
            [new EmailBlock.Button("Choose a new password", $"{AppUrl}/reset?token=abc123")],
            AppUrl);

        // The text part has to carry the link as a link, since there is no button to click.
        Assert.Contains($"{AppUrl}/reset?token=abc123", body.Text);
        Assert.Contains($"href=\"{AppUrl}/reset?token=abc123\"", body.Html);
    }

    [Fact]
    public void A_code_survives_into_the_text_part()
    {
        // Someone reading in a text-only client still has to be able to find the code.
        var body = EmailLayout.Render("Your sign-in code", [new EmailBlock.Code("482913")], AppUrl);

        Assert.Contains("482913", body.Text);
        Assert.Contains("482913", body.Html);
    }

    [Fact]
    public void An_existing_text_body_becomes_paragraphs_and_lists()
    {
        var text = "Hello Ana,\n\nPeriod 1 09:00 Maths\nPeriod 2 10:00 Physics";

        var body = EmailLayout.FromText("Your schedule for tomorrow", text, AppUrl);

        Assert.Contains("<p", body.Html);
        Assert.Contains("<li", body.Html);
        Assert.Contains("Period 2 10:00 Physics", body.Html);
        Assert.Contains("Period 2 10:00 Physics", body.Text);
    }

    [Fact]
    public void The_html_is_self_contained_with_no_external_stylesheet()
    {
        // Mail clients strip <style> and <link> often enough that anything not inline is a
        // coin flip. If one of these appears, the message has stopped being safe to send.
        var body = EmailLayout.Render(
            "Welcome", [new EmailBlock.Paragraph("Hello."), new EmailBlock.Button("Sign in", AppUrl)], AppUrl);

        Assert.DoesNotContain("<link", body.Html);
        Assert.DoesNotContain("<style", body.Html);
        Assert.Contains("style=\"", body.Html);
    }
}
