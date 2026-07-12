using System.Net;

namespace Inviter.Api.Infrastructure.Email.Templates;

internal static class EmailTemplateLayout
{
    public const string Background = "#f7faf3";
    public const string Card = "#fefefb";
    public const string Foreground = "#061a14";
    public const string MutedForeground = "#4a645b";
    public const string Primary = "#006049";
    public const string PrimaryForeground = "#f8fbf4";
    public const string Border = "#cfdecf";
    public const string Accent = "#c9e7d0";
    public const string Muted = "#e7f0e5";
    public const string StatusYes = "#c0f3d6";
    public const string StatusYesForeground = "#004325";
    public const string StatusMaybe = "#ffe4af";
    public const string StatusMaybeForeground = "#683d00";
    public const string StatusNo = "#ffddd6";
    public const string StatusNoForeground = "#842a23";

    public const string ParagraphStyle = "margin: 0 0 16px; line-height: 1.55;";
    public const string MetaStyle = "margin: 0 0 8px; line-height: 1.55;";

    private const string SansFont = "Inter, -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif";
    private const string SerifFont = "Fraunces, Georgia, 'Times New Roman', serif";

    public static string Shell(string preheader, string contentHtml, string? imageRow = null)
    {
        var hiddenPreheader = WebUtility.HtmlEncode(preheader);
        var headerRow = imageRow is null
            ? $"""<tr><td style="height: 8px; background: {Accent}; font-size: 0; line-height: 0;">&nbsp;</td></tr>"""
            : imageRow;

        return $"""
<!doctype html>
<html lang="da">
<body style="margin: 0; padding: 0; background: {Background}; color: {Foreground}; font-family: {SansFont}; font-feature-settings: 'ss01', 'cv11'; -webkit-font-smoothing: antialiased;">
  <div style="display: none; max-height: 0; overflow: hidden; opacity: 0; color: transparent;">{hiddenPreheader}</div>
  <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="width: 100%; background: {Background}; border-collapse: collapse;">
    <tr>
      <td align="center" style="padding: 28px 16px 36px;">
        <table role="presentation" cellpadding="0" cellspacing="0" width="100%" style="max-width: 592px; width: 100%; margin: 0 auto; background: {Card}; border: 1px solid {Border}; border-radius: 8px; border-collapse: separate; overflow: hidden;">
          {headerRow}
          <tr>
            <td style="padding: 32px;">
              {contentHtml}
            </td>
          </tr>
        </table>
        <p style="margin: 18px 0 0; color: {MutedForeground}; font-size: 12px; line-height: 1.5;">Invitér nu</p>
      </td>
    </tr>
  </table>
</body>
</html>
""";
    }

    public static string Eyebrow(string text) =>
        $"""<p style="margin: 0 0 10px; color: {Primary}; font-size: 12px; font-weight: 700; letter-spacing: 0.08em; line-height: 1.2; text-transform: uppercase;">{WebUtility.HtmlEncode(text)}</p>""";

    public static string Heading(string text, int size = 28) =>
        $"""<h1 style="margin: 0 0 22px; color: {Foreground}; font-family: {SerifFont}; font-size: {size}px; font-weight: 600; font-variation-settings: 'opsz' 144; letter-spacing: 0; line-height: 1.08;">{WebUtility.HtmlEncode(text)}</h1>""";

    public static string Button(string href, string label) =>
        $"""<a href="{WebUtility.HtmlEncode(href)}" style="display: inline-block; background: {Primary}; color: {PrimaryForeground}; border-radius: 8px; padding: 12px 20px; text-decoration: none; font-weight: 650; line-height: 1.2;">{WebUtility.HtmlEncode(label)}</a>""";

    public static string TextLink(string href, string text) =>
        $"""<a href="{WebUtility.HtmlEncode(href)}" style="color: {Primary}; text-decoration: underline; text-underline-offset: 3px; word-break: break-all;">{WebUtility.HtmlEncode(text)}</a>""";

    public static string Divider() =>
        $"""<hr style="border: none; border-top: 1px solid {Border}; margin: 24px 0;">""";

    public static string Note(string html) =>
        $"""<p style="margin: 0; color: {MutedForeground}; font-size: 13px; line-height: 1.55;">{html}</p>""";

    public static string Pill(string label, string background, string foreground) =>
        $"""<span style="display: inline-block; padding: 3px 10px; background: {background}; color: {foreground}; border-radius: 999px; font-size: 13px; font-weight: 650; line-height: 1.35;">{WebUtility.HtmlEncode(label)}</span>""";
}
