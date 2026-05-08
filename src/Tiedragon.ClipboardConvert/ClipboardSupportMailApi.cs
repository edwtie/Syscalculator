#nullable enable

using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace Tiedragon.ClipboardConvert;

/// <summary>
/// Builds support mail content for clipboard diagnostics.
/// </summary>
public static class ClipboardSupportMailApi
{
    private const int ReportPreviewRowLimit = 5;

    public static string BuildSubject(string appName, Func<string, string, string>? text = null)
    {
        var t = text ?? ((_, fallback) => fallback);
        return string.Format(
            CultureInfo.CurrentCulture,
            t("clipboard.debug.mail_subject", "{0} clipboard diagnosis"),
            appName);
    }

    public static string BuildMailToBody(string appName, Func<string, string, string>? text = null)
    {
        var t = text ?? ((_, fallback) => fallback);
        var body = new StringBuilder();
        body.AppendLine(t("clipboard.debug.mail_intro", "Please look at this clipboard diagnosis."));
        body.AppendLine();
        body.AppendLine(t("clipboard.debug.mail_paste_report", "The full report has been copied to the clipboard. Paste it into this e-mail with Ctrl+V."));
        body.AppendLine();
        body.AppendLine(appName);
        body.AppendLine(FormatReportDate(DateTime.Now, t));
        return body.ToString();
    }

    public static string SaveHtmlMailMessage(
        string directory,
        string to,
        string subject,
        string htmlBody,
        string plainBody)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "clipboard-diagnosis-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".eml");

        using var message = new MailMessage
        {
            From = new MailAddress("noreply@tiedragon.local", "Syscalculator"),
            Subject = subject,
            Body = plainBody,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8,
            IsBodyHtml = false
        };
        if (!string.IsNullOrWhiteSpace(to))
            message.To.Add(to.Trim());

        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(plainBody, Encoding.UTF8, "text/plain"));
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(htmlBody, Encoding.UTF8, "text/html"));

        using var client = new SmtpClient
        {
            DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
            PickupDirectoryLocation = directory
        };
        client.Send(message);

        var generated = Directory
            .EnumerateFiles(directory, "*.eml")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
        if (generated is null)
            throw new InvalidOperationException("E-mail file was not created.");

        if (!string.Equals(generated, path, StringComparison.OrdinalIgnoreCase))
        {
            if (File.Exists(path))
                File.Delete(path);
            File.Move(generated, path);
        }

        return path;
    }

    public static string BuildBody(
        ClipboardDebugReport report,
        string clipboardText,
        string appName,
        Func<string, string, string>? text = null,
        ClipboardConversionSummary? conversionSummary = null,
        string? beforeText = null,
        string? afterText = null,
        string? usedRuleText = null,
        string? usedRuleMathMl = null)
    {
        var t = text ?? ((_, fallback) => fallback);
        var formats = report.Formats.Length == 0
            ? t("clipboard.debug.no_formats", "No clipboard formats found.")
            : string.Join(Environment.NewLine, report.Formats.Select(format => "- " + format));

        var body = new StringBuilder();
        body.AppendLine(t("clipboard.debug.mail_intro", "Please look at this clipboard diagnosis."));
        body.AppendLine();
        body.AppendLine(appName);
        body.AppendLine(FormatReportDate(DateTime.Now, t));
        body.AppendLine();
        body.AppendLine(t("clipboard.debug.summary", "Summary"));
        body.AppendLine("--------");
        body.AppendLine($"{t("clipboard.debug.owner", "Owner")}: {FormatOwner(report.Owner, t)}");
        body.AppendLine($"{t("clipboard.debug.text_length", "Text length")}: {report.TextLength.ToString(CultureInfo.CurrentCulture)}");
        body.AppendLine($"{t("clipboard.debug.rows", "Rows")}: {report.LineCount.ToString(CultureInfo.CurrentCulture)}");
        body.AppendLine($"{t("clipboard.debug.columns", "Columns")}: {report.ColumnCount.ToString(CultureInfo.CurrentCulture)}");
        body.AppendLine($"{t("clipboard.debug.same_last_seen", "Same as last seen")}: {FormatBool(report.SameAsLastSeen, t)}");
        body.AppendLine($"{t("clipboard.debug.same_converted", "Same as conversion")}: {FormatBool(report.SameAsLastConverted, t)}");
        if (!string.IsNullOrWhiteSpace(usedRuleText))
            body.AppendLine($"{t("clipboard.debug.used_rule", "Used rule")}: {usedRuleText}");
        if (!string.IsNullOrWhiteSpace(usedRuleMathMl))
        {
            body.AppendLine();
            body.AppendLine(t("clipboard.debug.mathml", "MathML"));
            body.AppendLine("------");
            body.AppendLine(usedRuleMathMl);
        }
        if (conversionSummary is { } summary)
        {
            body.AppendLine();
            body.AppendLine(t("clipboard.debug.conversion", "Conversion"));
            body.AppendLine("----------");
            body.AppendLine($"{t("clipboard.debug.conversion_rows", "Converted rows")}: {summary.RowCount.ToString(CultureInfo.CurrentCulture)}");
            body.AppendLine($"{t("clipboard.debug.conversion_cells", "Cells")}: {summary.CellCount.ToString(CultureInfo.CurrentCulture)}");
            body.AppendLine($"{t("clipboard.debug.conversion_success", "Converted")}: {summary.ConvertedCellCount.ToString(CultureInfo.CurrentCulture)}");
            body.AppendLine($"{t("clipboard.debug.conversion_errors", "Errors")}: {summary.ErrorCellCount.ToString(CultureInfo.CurrentCulture)}");
        }
        body.AppendLine();
        body.AppendLine(t("clipboard.debug.formats", "Formats"));
        body.AppendLine("-------");
        body.AppendLine(formats);
        body.AppendLine();
        body.AppendLine(t("clipboard.debug.preview", "Preview"));
        body.AppendLine("-------");
        body.AppendLine(FormatPreviewNote(clipboardText, t));
        body.AppendLine(ClipboardDebugApi.EscapePreview(FirstRows(clipboardText, ReportPreviewRowLimit)));

        if (!string.IsNullOrEmpty(beforeText) || !string.IsNullOrEmpty(afterText))
        {
            body.AppendLine();
            body.AppendLine(t("clipboard.debug.before", "Before"));
            body.AppendLine("------");
            body.AppendLine(ClipboardDebugApi.EscapePreview(FirstRows(beforeText ?? "", ReportPreviewRowLimit)));
            body.AppendLine();
            body.AppendLine(t("clipboard.debug.after", "After"));
            body.AppendLine("-----");
            body.AppendLine(ClipboardDebugApi.EscapePreview(FirstRows(afterText ?? "", ReportPreviewRowLimit)));
        }

        return body.ToString();
    }

    public static string BuildHtmlBody(
        ClipboardDebugReport report,
        string clipboardText,
        string appName,
        Func<string, string, string>? text = null,
        ClipboardConversionSummary? conversionSummary = null,
        string? beforeText = null,
        string? afterText = null,
        string? usedRuleText = null,
        string? usedRuleMathMl = null)
    {
        var t = text ?? ((_, fallback) => fallback);
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html><head><meta charset=\"utf-8\"><style>");
        builder.AppendLine("body{font-family:'Segoe UI',Arial,sans-serif;color:#111827;background:#fff;margin:0;padding:24px;}");
        builder.AppendLine("h1{font-size:22px;margin:0;color:#0f3f8f;} h2{font-size:15px;line-height:1.25;color:#0f3f8f;margin:0 0 16px;}");
        builder.AppendLine(".muted{color:#64748b;font-size:12px;margin-top:4px}.card{border:1px solid #8b98aa;margin:16px 0;padding:14px 16px;}");
        builder.AppendLine("table{border-collapse:collapse;width:100%;font-size:13px;}td,th{border:1px solid #9aa7b8;padding:6px 8px;text-align:left;vertical-align:top;}");
        builder.AppendLine("th{background:#e8eef5;color:#0b2e68}.metrics td{border:0;padding:4px 8px 4px 0}.metrics td:first-child{font-weight:600;color:#374151;width:190px;}");
        builder.AppendLine(".formula{font-size:24px;font-weight:700;margin:8px 0;text-align:center}.mathml{background:#fff;color:#111827;padding:0;text-align:center;font-size:24px;font-weight:700;}");
        builder.AppendLine(".split{border-collapse:collapse;width:100%;table-layout:fixed}.splitcell{border:0!important;padding:0 10px 0 0!important;vertical-align:top}.splitcell.right{padding:0 0 0 10px!important}.gridtitle{display:block;margin:0 0 6px;color:#374151}.formats{margin:0;padding-left:18px}");
        builder.AppendLine("</style></head><body>");
        builder.Append("<h1>").Append(E(t("clipboard.debug.report_title", "Clipboard report"))).AppendLine("</h1>");
        builder.Append("<div class=\"muted\">")
            .Append(E(appName))
            .Append(" &middot; ")
            .Append(E(FormatReportDate(DateTime.Now, t)))
            .AppendLine("</div>");

        builder.AppendLine("<div class=\"card\"><h2>" + E(t("clipboard.debug.summary", "Summary")) + "</h2><table class=\"metrics\">");
        AddMetric(builder, t("clipboard.debug.owner", "Owner"), FormatOwner(report.Owner, t));
        AddMetric(builder, t("clipboard.debug.text_length", "Text length"), report.TextLength.ToString(CultureInfo.CurrentCulture));
        AddMetric(builder, t("clipboard.debug.rows", "Rows"), report.LineCount.ToString(CultureInfo.CurrentCulture));
        AddMetric(builder, t("clipboard.debug.columns", "Columns"), report.ColumnCount.ToString(CultureInfo.CurrentCulture));
        AddMetric(builder, t("clipboard.debug.same_last_seen", "Same as last seen"), FormatBool(report.SameAsLastSeen, t));
        AddMetric(builder, t("clipboard.debug.same_converted", "Same as conversion"), FormatBool(report.SameAsLastConverted, t));
        builder.AppendLine("</table></div>");

        if (!string.IsNullOrWhiteSpace(usedRuleText) || !string.IsNullOrWhiteSpace(usedRuleMathMl))
        {
            builder.AppendLine("<div class=\"card\"><h2>" + E(t("clipboard.debug.formula", "Formula")) + "</h2>");
            if (string.IsNullOrWhiteSpace(usedRuleMathMl) && !string.IsNullOrWhiteSpace(usedRuleText))
                builder.Append("<div class=\"formula\">").Append(E(PrettyFormulaText(usedRuleText))).AppendLine("</div>");
            if (!string.IsNullOrWhiteSpace(usedRuleMathMl))
                builder.Append("<div class=\"mathml\">").Append(usedRuleMathMl).AppendLine("</div>");
            builder.AppendLine("</div>");
        }

        if (conversionSummary is { } summary)
        {
            builder.AppendLine("<div class=\"card\"><h2>" + E(t("clipboard.debug.conversion", "Conversion")) + "</h2><table class=\"metrics\">");
            AddMetric(builder, t("clipboard.debug.conversion_rows", "Converted rows"), summary.RowCount.ToString(CultureInfo.CurrentCulture));
            AddMetric(builder, t("clipboard.debug.conversion_cells", "Cells"), summary.CellCount.ToString(CultureInfo.CurrentCulture));
            AddMetric(builder, t("clipboard.debug.conversion_success", "Converted"), summary.ConvertedCellCount.ToString(CultureInfo.CurrentCulture));
            AddMetric(builder, t("clipboard.debug.conversion_errors", "Errors"), summary.ErrorCellCount.ToString(CultureInfo.CurrentCulture));
            builder.AppendLine("</table></div>");
        }

        if (!string.IsNullOrEmpty(beforeText) || !string.IsNullOrEmpty(afterText))
        {
            builder.AppendLine("<div class=\"card\"><h2>" + E(t("clipboard.debug.before_after", "Before / After")) + "</h2><table class=\"split\"><tr><td class=\"splitcell\">");
            builder.Append("<strong class=\"gridtitle\">").Append(E(t("clipboard.debug.before", "Before"))).AppendLine("</strong>");
            AppendHtmlGrid(builder, beforeText ?? "", 0, 0, ReportPreviewRowLimit);
            builder.AppendLine("</td><td class=\"splitcell right\">");
            builder.Append("<strong class=\"gridtitle\">").Append(E(t("clipboard.debug.after", "After"))).AppendLine("</strong>");
            AppendHtmlGrid(builder, afterText ?? "", 0, 0, ReportPreviewRowLimit);
            builder.AppendLine("</td></tr></table></div>");
        }

        builder.AppendLine("<div class=\"card\"><h2>" + E(t("clipboard.debug.preview", "Preview")) + "</h2>");
        builder.Append("<div class=\"muted\">").Append(E(FormatPreviewNote(clipboardText, t))).AppendLine("</div>");
        AppendHtmlGrid(builder, clipboardText, 0, 0, ReportPreviewRowLimit);
        builder.AppendLine("</div>");

        builder.AppendLine("<div class=\"card\"><h2>" + E(t("clipboard.debug.formats", "Formats")) + "</h2>");
        if (report.Formats.Length == 0)
        {
            builder.Append("<div class=\"muted\">").Append(E(t("clipboard.debug.no_formats", "No clipboard formats found."))).AppendLine("</div>");
        }
        else
        {
            builder.AppendLine("<ul class=\"formats\">");
            foreach (var format in report.Formats)
                builder.Append("<li>").Append(E(format)).AppendLine("</li>");
            builder.AppendLine("</ul>");
        }

        builder.AppendLine("</div></body></html>");
        return builder.ToString();
    }

    public static string BuildClipboardHtmlFragment(string html)
    {
        const string startMarker = "<!--StartFragment-->";
        const string endMarker = "<!--EndFragment-->";
        var fragmentHtml = html.Contains(startMarker, StringComparison.Ordinal)
            ? html
            : html.Replace("<body>", "<body>" + startMarker, StringComparison.OrdinalIgnoreCase)
                  .Replace("</body>", endMarker + "</body>", StringComparison.OrdinalIgnoreCase);
        if (!fragmentHtml.Contains(startMarker, StringComparison.Ordinal) ||
            !fragmentHtml.Contains(endMarker, StringComparison.Ordinal))
        {
            fragmentHtml = "<html><body>" + startMarker + html + endMarker + "</body></html>";
        }

        var header = "Version:0.9\r\nStartHTML:0000000000\r\nEndHTML:0000000000\r\nStartFragment:0000000000\r\nEndFragment:0000000000\r\n";
        var startHtml = Encoding.UTF8.GetByteCount(header);
        var endHtml = startHtml + Encoding.UTF8.GetByteCount(fragmentHtml);
        var startMarkerIndex = fragmentHtml.IndexOf(startMarker, StringComparison.Ordinal);
        var endMarkerIndex = fragmentHtml.IndexOf(endMarker, StringComparison.Ordinal);
        var startFragment = startHtml + Encoding.UTF8.GetByteCount(fragmentHtml[..startMarkerIndex]) + Encoding.UTF8.GetByteCount(startMarker);
        var endFragment = startHtml + Encoding.UTF8.GetByteCount(fragmentHtml[..endMarkerIndex]);

        header =
            "Version:0.9\r\n" +
            $"StartHTML:{startHtml:0000000000}\r\n" +
            $"EndHTML:{endHtml:0000000000}\r\n" +
            $"StartFragment:{startFragment:0000000000}\r\n" +
            $"EndFragment:{endFragment:0000000000}\r\n";
        return header + fragmentHtml;
    }

    public static Uri CreateMailToUri(string to, string subject, string body)
    {
        var safeTo = string.IsNullOrWhiteSpace(to) ? "" : to.Trim();
        var value = "mailto:" + safeTo +
            "?subject=" + Uri.EscapeDataString(subject) +
            "&body=" + Uri.EscapeDataString(body);
        return new Uri(value, UriKind.Absolute);
    }

    private static void AddMetric(StringBuilder builder, string label, string value)
    {
        builder.Append("<tr><td>").Append(E(label)).Append("</td><td>").Append(E(value)).AppendLine("</td></tr>");
    }

    private static void AppendHtmlGrid(StringBuilder builder, string text, int minimumRows, int minimumColumns, int maximumRows)
    {
        var rows = ClipboardGridApi.ParseTable(text);
        var rowCount = rows.Count == 0
            ? Math.Max(1, minimumRows)
            : Math.Max(minimumRows, Math.Min(maximumRows, rows.Count));
        var columnCount = rows.Count == 0
            ? Math.Max(1, minimumColumns)
            : Math.Max(minimumColumns, Math.Min(8, rows.Max(row => row.Count)));

        builder.AppendLine("<table>");
        builder.AppendLine("<tr>");
        for (var column = 0; column < columnCount; column++)
            builder.Append("<th>").Append(E(ClipboardGridApi.ColumnName(column))).AppendLine("</th>");
        builder.AppendLine("</tr>");

        for (var row = 0; row < rowCount; row++)
        {
            builder.AppendLine("<tr>");
            for (var column = 0; column < columnCount; column++)
            {
                var value = row < rows.Count && column < rows[row].Count ? rows[row][column] : "";
                builder.Append("<td>").Append(E(value)).AppendLine("</td>");
            }

            builder.AppendLine("</tr>");
        }

        builder.AppendLine("</table>");
    }

    private static string FirstRows(string text, int maximumRows)
    {
        var rows = ClipboardGridApi.ParseTable(text);
        if (rows.Count == 0)
            return "";

        return string.Join(Environment.NewLine, rows.Take(maximumRows).Select(row => string.Join('\t', row)));
    }

    private static string FormatPreviewNote(string text, Func<string, string, string> t)
    {
        var rowCount = ClipboardGridApi.ParseTable(text).Count;
        if (rowCount <= ReportPreviewRowLimit)
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                t("clipboard.debug.preview_rows_all", "{0} rows shown"),
                rowCount);
        }

        return string.Format(
            CultureInfo.CurrentCulture,
            t("clipboard.debug.preview_rows_limited", "First {0} of {1} rows shown"),
            ReportPreviewRowLimit,
            rowCount);
    }

    private static string PrettyFormulaText(string value)
    {
        return value
            .Replace("*", " * ", StringComparison.Ordinal)
            .Replace("/", " / ", StringComparison.Ordinal)
            .Replace("  ", " ", StringComparison.Ordinal)
            .Trim();
    }

    private static string E(string value) => WebUtility.HtmlEncode(value);

    private static string FormatBool(bool value, Func<string, string, string> text)
    {
        return value ? text("common.yes", "Yes") : text("common.no", "No");
    }

    private static string FormatReportDate(DateTime value, Func<string, string, string> text)
    {
        var cultureName = text("clipboard.debug.report_date_culture", CultureInfo.CurrentCulture.Name);
        var format = text("clipboard.debug.report_date_format", "d MMMM yyyy");
        CultureInfo culture;
        try
        {
            culture = CultureInfo.GetCultureInfo(cultureName);
        }
        catch (CultureNotFoundException)
        {
            culture = CultureInfo.CurrentCulture;
        }

        return value.ToString(format, culture);
    }

    private static string FormatOwner(string owner, Func<string, string, string> text)
    {
        return string.Equals(owner, "none", StringComparison.OrdinalIgnoreCase)
            ? text("clipboard.debug.none", "None")
            : owner;
    }
}
