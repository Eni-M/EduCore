using System.Net;
using System.Text;

namespace EduCore.Services.Email;

// One piece of an email's body. A message is written once as a list of these and rendered to
// both HTML and plain text, so the two can never drift apart the way two hand-written copies do.
public abstract record EmailBlock
{
    public sealed record Paragraph(string Text) : EmailBlock;

    // A value worth pulling out of the prose. Spaced is for strings read character by
    // character and retyped -- a one-time code; an email address is read as a word and the
    // letter-spacing only makes it harder.
    public sealed record Code(string Value, bool Spaced = true) : EmailBlock;

    public sealed record Button(string Label, string Url) : EmailBlock;

    public sealed record Bullets(IReadOnlyList<string> Items) : EmailBlock;

    // Quieter than a paragraph. For the "if this wasn't you" line at the end of a message.
    public sealed record Note(string Text) : EmailBlock;
}

// Renders the school system's own look into an email.
//
// Email is not the web: layout is tables because Outlook still uses Word to render, every style
// is inline because a good number of clients strip <style>, and there are no webfonts because
// most clients ignore them. The identity therefore has to be carried by the things that do
// survive -- the blue and yellow, the chunky border, the pill button, the logo mark -- which is
// exactly what the landing page leads with.
public static class EmailLayout
{
    private const string Blue = "#244aa5";
    private const string BlueDeep = "#16336f";
    private const string Yellow = "#ffc93c";
    private const string YellowSoft = "#fff4d6";
    private const string Ink = "#131a2e";
    private const string Muted = "#55607a";
    private const string Tint = "#f4f7fd";
    private const string Line = "#d5dcec";

    private const string Font =
        "-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,'Helvetica Neue',Arial,sans-serif";

    public static EmailBody Render(string heading, IReadOnlyList<EmailBlock> blocks, string appUrl) =>
        new(PlainText(heading, blocks), Html(heading, blocks, appUrl));

    // For messages whose body is already assembled as text -- the schedule and guardian digests
    // build theirs line by line from query results. A blank line starts a new paragraph, and a
    // run of lines within one becomes a list, which is what those bodies already look like.
    // The digests would read better still with a purpose-built table; this gets them the same
    // envelope as everything else without rewriting how they are generated.
    public static EmailBody FromText(string heading, string text, string appUrl)
    {
        var blocks = new List<EmailBlock>();

        foreach (var chunk in text.Replace("\r\n", "\n")
                     .Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            var lines = chunk.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.TrimEnd())
                .Where(l => l.Length > 0)
                .ToList();

            if (lines.Count == 0)
                continue;

            if (lines.Count == 1)
                blocks.Add(new EmailBlock.Paragraph(lines[0]));
            else
                blocks.Add(new EmailBlock.Bullets(lines.Select(l => l.TrimStart('-', ' ')).ToList()));
        }

        return Render(heading, blocks, appUrl);
    }

    // ---- plain text ------------------------------------------------------

    private static string PlainText(string heading, IReadOnlyList<EmailBlock> blocks)
    {
        var sb = new StringBuilder();
        sb.AppendLine(heading);
        sb.AppendLine(new string('=', heading.Length));
        sb.AppendLine();

        foreach (var block in blocks)
        {
            switch (block)
            {
                case EmailBlock.Paragraph p:
                    sb.AppendLine(p.Text);
                    break;
                case EmailBlock.Code c:
                    sb.AppendLine($"    {c.Value}");
                    break;
                case EmailBlock.Button b:
                    sb.AppendLine($"{b.Label}: {b.Url}");
                    break;
                case EmailBlock.Bullets l:
                    foreach (var item in l.Items) sb.AppendLine($"  - {item}");
                    break;
                case EmailBlock.Note n:
                    sb.AppendLine(n.Text);
                    break;
            }

            sb.AppendLine();
        }

        sb.AppendLine("--");
        sb.AppendLine("EduCore");

        return sb.ToString().TrimEnd() + Environment.NewLine;
    }

    // ---- html ------------------------------------------------------------

    private static string Html(string heading, IReadOnlyList<EmailBlock> blocks, string appUrl)
    {
        var body = new StringBuilder();

        foreach (var block in blocks)
            body.Append(RenderBlock(block));

        // The preheader is the grey line a client shows next to the subject in the inbox list.
        // Left unset it fills with whatever markup comes first, which reads as noise.
        var preheader = blocks.OfType<EmailBlock.Paragraph>().FirstOrDefault()?.Text ?? heading;

        return $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width,initial-scale=1">
        <meta name="color-scheme" content="light">
        <title>{E(heading)}</title>
        </head>
        <body style="margin:0;padding:0;background:{Tint};font-family:{Font};">
        <div style="display:none;max-height:0;overflow:hidden;opacity:0;">{E(Trim(preheader))}</div>
        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background:{Tint};">
        <tr><td align="center" style="padding:32px 16px;">

        <table role="presentation" width="600" cellpadding="0" cellspacing="0" border="0" style="width:100%;max-width:600px;">

        <tr><td style="padding:0 4px 18px;">
          <table role="presentation" cellpadding="0" cellspacing="0" border="0"><tr>
            <td style="background:{Blue};border:2px solid {Ink};border-radius:12px 12px 12px 0;width:34px;height:34px;text-align:center;vertical-align:middle;color:#ffffff;font-family:{Font};font-size:17px;font-weight:800;">E</td>
            <td style="padding-left:10px;font-family:{Font};font-size:21px;font-weight:800;color:{Ink};">EduCore</td>
          </tr></table>
        </td></tr>

        <tr><td style="background:#ffffff;border:2px solid {Ink};border-radius:20px;padding:36px 34px;">
          <h1 style="margin:0 0 20px;font-family:{Font};font-size:26px;line-height:1.25;font-weight:800;color:{Ink};">{E(heading)}</h1>
          {body}
        </td></tr>

        <tr><td style="padding:20px 8px 0;font-family:{Font};font-size:12px;line-height:1.6;color:{Muted};">
          You are receiving this because your school uses EduCore.<br>
          <a href="{E(appUrl)}" style="color:{Blue};">{E(appUrl)}</a>
        </td></tr>

        </table>
        </td></tr>
        </table>
        </body>
        </html>
        """;
    }

    private static string RenderBlock(EmailBlock block) => block switch
    {
        EmailBlock.Paragraph p =>
            $"""<p style="margin:0 0 16px;font-family:{Font};font-size:16px;line-height:1.6;color:{Ink};">{E(p.Text)}</p>""",

        // Letter-spaced and oversized: this is read aloud and retyped, not skimmed.
        EmailBlock.Code c =>
            $"""
             <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="margin:0 0 18px;">
             <tr><td style="background:{YellowSoft};border:2px solid {Ink};border-radius:14px;padding:{(c.Spaced ? "16px 26px" : "14px 22px")};font-family:{Font};font-size:{(c.Spaced ? "30px" : "18px")};font-weight:{(c.Spaced ? "800" : "700")};letter-spacing:{(c.Spaced ? "6px" : "0")};color:{Ink};word-break:break-all;">{E(c.Value)}</td></tr>
             </table>
             """,

        // A table cell rather than a styled <a>: the cell is what gives Outlook a clickable
        // background, and the padding lives on the cell so it survives there too.
        EmailBlock.Button b =>
            $"""
             <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="margin:6px 0 20px;">
             <tr><td style="background:{Blue};border:2px solid {Ink};border-radius:9999px;">
               <a href="{E(b.Url)}" style="display:inline-block;padding:14px 28px;font-family:{Font};font-size:16px;font-weight:700;color:#ffffff;text-decoration:none;">{E(b.Label)} &rarr;</a>
             </td></tr>
             </table>
             """,

        EmailBlock.Bullets l =>
            $"""<ul style="margin:0 0 18px;padding-left:22px;font-family:{Font};font-size:16px;line-height:1.6;color:{Ink};">{
                string.Concat(l.Items.Select(i => $"<li style=\"margin:0 0 6px;\">{E(i)}</li>"))}</ul>""",

        EmailBlock.Note n =>
            $"""
             <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="margin:18px 0 0;">
             <tr><td style="border-top:2px solid {Line};padding-top:16px;font-family:{Font};font-size:14px;line-height:1.6;color:{Muted};">{E(n.Text)}</td></tr>
             </table>
             """,

        _ => string.Empty
    };

    // Names, email addresses and school names all reach these templates from user input, so
    // every interpolated value is escaped. An unescaped apostrophe is cosmetic; unescaped markup
    // would let somebody write their name as a link and send it out under our From address.
    private static string E(string value) => WebUtility.HtmlEncode(value);

    private static string Trim(string value) =>
        value.Length <= 140 ? value : value[..137] + "...";
}
