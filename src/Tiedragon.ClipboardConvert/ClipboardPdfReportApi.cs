#nullable enable

using System.Globalization;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;

namespace Tiedragon.ClipboardConvert;

/// <summary>
/// PDF export for clipboard diagnosis reports.
/// </summary>
public static class ClipboardPdfReportApi
{
    private const int PageWidth = 595;
    private const int PageHeight = 842;
    private const int Margin = 42;
    private const int BottomMargin = 42;
    private const int CardWidth = PageWidth - Margin * 2;
    private const int FormatPreviewRowLimit = 4;
    private const int PreviewRowLimit = 5;

    public static void SaveReportPdf(
        string path,
        ClipboardDebugReport report,
        string clipboardText,
        string appName,
        Func<string, string, string>? text = null,
        ClipboardConversionSummary? conversionSummary = null,
        string? beforeText = null,
        string? afterText = null,
        string? usedRuleText = null,
        string? usedRuleMathMl = null,
        byte[]? formulaJpeg = null,
        byte[]? logoJpeg = null)
    {
        File.WriteAllBytes(path, BuildReportPdf(report, clipboardText, appName, text, conversionSummary, beforeText, afterText, usedRuleText, usedRuleMathMl, formulaJpeg, logoJpeg));
    }

    public static byte[] BuildReportPdf(
        ClipboardDebugReport report,
        string clipboardText,
        string appName,
        Func<string, string, string>? text = null,
        ClipboardConversionSummary? conversionSummary = null,
        string? beforeText = null,
        string? afterText = null,
        string? usedRuleText = null,
        string? usedRuleMathMl = null,
        byte[]? formulaJpeg = null,
        byte[]? logoJpeg = null)
    {
        var t = text ?? ((_, fallback) => fallback);
        var pages = new List<List<PdfCommand>>();
        var commands = new List<PdfCommand>();
        pages.Add(commands);
        var y = AddPageHeader(commands, t, appName, logoJpeg is { Length: > 0 });

        y = AddSummaryCard(commands, y, report, t, usedRuleText);
        if (!string.IsNullOrWhiteSpace(usedRuleMathMl) || !string.IsNullOrWhiteSpace(usedRuleText))
            y = AddFormulaCard(commands, y, usedRuleText ?? "", usedRuleMathMl ?? "", t, formulaJpeg);

        if (conversionSummary is { } summary)
            y = AddConversionCard(commands, y, summary, t);

        if (!string.IsNullOrEmpty(beforeText) || !string.IsNullOrEmpty(afterText))
            y = AddBeforeAfterCard(commands, y, beforeText ?? "", afterText ?? "", t);

        y = AddPreviewCard(commands, y, clipboardText, t);
        if (!TryAddFormatsCard(commands, y, report.Formats, t, FormatPreviewRowLimit))
        {
            var continuation = new List<PdfCommand>();
            pages.Add(continuation);
            var pageY = AddPageHeader(continuation, t, appName, logoJpeg is { Length: > 0 });
            AddFormatsCard(continuation, pageY, report.Formats, t, 32);
        }

        return BuildPdfBytes(pages.Select(BuildPageStream).ToArray(), formulaJpeg, logoJpeg);
    }

    private static int AddSummaryCard(
        List<PdfCommand> commands,
        int y,
        ClipboardDebugReport report,
        Func<string, string, string> t,
        string? usedRuleText)
    {
        var rows = new List<(string Label, string Value)>
        {
            (t("clipboard.debug.owner", "Owner"), FormatOwner(report.Owner, t)),
            (t("clipboard.debug.text_length", "Text length"), report.TextLength.ToString(CultureInfo.CurrentCulture)),
            (t("clipboard.debug.rows", "Rows"), report.LineCount.ToString(CultureInfo.CurrentCulture)),
            (t("clipboard.debug.columns", "Columns"), report.ColumnCount.ToString(CultureInfo.CurrentCulture)),
            (t("clipboard.debug.same_last_seen", "Same as last seen"), FormatBool(report.SameAsLastSeen, t)),
            (t("clipboard.debug.same_converted", "Same as conversion"), FormatBool(report.SameAsLastConverted, t))
        };
        if (!string.IsNullOrWhiteSpace(usedRuleText))
            rows.Add((t("clipboard.debug.used_rule", "Used rule"), Truncate(usedRuleText, 45)));

        return AddMetricCard(commands, y, t("clipboard.debug.summary", "Summary"), rows);
    }

    private static int AddConversionCard(
        List<PdfCommand> commands,
        int y,
        ClipboardConversionSummary summary,
        Func<string, string, string> t)
    {
        var rows = new[]
        {
            (t("clipboard.debug.conversion_rows", "Converted rows"), summary.RowCount.ToString(CultureInfo.CurrentCulture)),
            (t("clipboard.debug.conversion_cells", "Cells"), summary.CellCount.ToString(CultureInfo.CurrentCulture)),
            (t("clipboard.debug.conversion_success", "Converted"), summary.ConvertedCellCount.ToString(CultureInfo.CurrentCulture)),
            (t("clipboard.debug.conversion_errors", "Errors"), summary.ErrorCellCount.ToString(CultureInfo.CurrentCulture))
        };

        return AddMetricCard(commands, y, t("clipboard.debug.conversion", "Conversion"), rows);
    }

    private static int AddFormulaCard(
        List<PdfCommand> commands,
        int y,
        string formulaText,
        string mathMl,
        Func<string, string, string> t,
        byte[]? formulaJpeg)
    {
        var displayFormula = PrettyFormulaText(string.IsNullOrWhiteSpace(formulaText)
            ? StripMathMl(mathMl)
            : formulaText);
        var height = 86;
        AddCard(commands, y, height);
        AddText(commands, Margin + 16, y - 23, t("clipboard.debug.formula", "Formula"), 12, PdfColor.Accent, bold: true);
        if (formulaJpeg is { Length: > 0 })
            commands.Add(PdfCommand.Image("ImFormula", Margin + 55, y - 70, 400, 42));
        else
            AddFormulaVisual(commands, Margin + 120, y - 58, Truncate(displayFormula, 64));

        return y - height - 14;
    }

    private static void AddFormulaVisual(List<PdfCommand> commands, int x, int y, string formula)
    {
        const float size = 22f;
        var currentX = x;
        var tokens = formula.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in tokens)
        {
            if (token == "*")
            {
                AddMultiplySymbol(commands, currentX + 3, y + 2);
                currentX += 19;
                continue;
            }

            AddText(commands, currentX, y, token, size, PdfColor.Title, bold: true);
            currentX += EstimateTextWidth(token, size, bold: true) + 8;
        }
    }

    private static void AddMultiplySymbol(List<PdfCommand> commands, int x, int y)
    {
        commands.Add(PdfCommand.Line(x, y + 4, x + 9, y + 13, PdfColor.Title, 1.8f));
        commands.Add(PdfCommand.Line(x + 9, y + 4, x, y + 13, PdfColor.Title, 1.8f));
    }

    private static int AddMetricCard(List<PdfCommand> commands, int y, string title, IReadOnlyList<(string Label, string Value)> rows)
    {
        var height = 38 + rows.Count * 17;
        AddCard(commands, y, height);
        AddText(commands, Margin + 16, y - 23, title, 12, PdfColor.Accent, bold: true);

        var rowY = y - 45;
        foreach (var row in rows)
        {
            AddText(commands, Margin + 22, rowY, row.Label, 9, PdfColor.Label, bold: true);
            AddText(commands, Margin + 205, rowY, row.Value, 9.5f, PdfColor.Body);
            rowY -= 17;
        }

        return y - height - 14;
    }

    private static int AddPageHeader(List<PdfCommand> commands, Func<string, string, string> t, string appName, bool showLogo)
    {
        var y = 790;
        if (showLogo)
            commands.Add(PdfCommand.Image("ImLogo", 468, 757, 72, 54));
        AddText(commands, 42, y, t("clipboard.debug.report_title", "Clipboard report"), 20, PdfColor.Title, bold: true);
        y -= 22;
        AddText(commands, 42, y, t("clipboard.debug.report_subtitle", "Clean overview of the current clipboard content."), 10, PdfColor.Muted);
        y -= 18;
        AddText(commands, 42, y, appName, 9, PdfColor.Muted);
        AddText(commands, 405, y, FormatReportDate(DateTime.Now, t), 9, PdfColor.Muted);
        return y - 24;
    }

    private static bool TryAddFormatsCard(List<PdfCommand> commands, int y, string[] formats, Func<string, string, string> t, int maxRows)
    {
        var rows = FormatRows(formats, t);
        var visibleCount = Math.Min(maxRows, rows.Length);
        var height = 42 + visibleCount * 13;
        if (y - height < BottomMargin)
            return false;

        AddFormatsCard(commands, y, formats, t, maxRows);
        return true;
    }

    private static int AddFormatsCard(List<PdfCommand> commands, int y, string[] formats, Func<string, string, string> t, int maxRows)
    {
        var rows = FormatRows(formats, t);
        var visibleRows = rows.Take(maxRows).ToArray();
        var height = 42 + visibleRows.Length * 13;

        AddCard(commands, y, height);
        AddText(commands, Margin + 16, y - 23, t("clipboard.debug.formats", "Formats"), 12, PdfColor.Accent, bold: true);

        var rowY = y - 41;
        foreach (var row in visibleRows)
        {
            AddText(commands, Margin + 22, rowY, Truncate(row, 92), 8.2f, PdfColor.Body);
            rowY -= 13;
        }

        if (rows.Length > visibleRows.Length)
            AddText(commands, Margin + 22, Math.Max(BottomMargin, rowY), "...", 8.2f, PdfColor.Muted);

        return y - height - 14;
    }

    private static string[] FormatRows(string[] formats, Func<string, string, string> t)
    {
        return formats.Length == 0
            ? new[] { t("clipboard.debug.no_formats", "No clipboard formats found.") }
            : formats.Select(format => "- " + format).ToArray();
    }

    private static int AddPreviewCard(List<PdfCommand> commands, int y, string clipboardText, Func<string, string, string> t)
    {
        var table = ClipboardGridApi.ParseTable(clipboardText);
        var rows = table.Count == 0 ? 1 : Math.Min(PreviewRowLimit, table.Count);
        var columns = table.Count == 0 ? 1 : Math.Min(6, table.Max(row => row.Count));
        var cellWidth = (CardWidth - 32) / columns;
        var rowHeight = 20;
        var tableTop = y - 60;
        var height = 74 + rows * rowHeight;

        AddCard(commands, y, height);
        AddText(commands, Margin + 16, y - 23, t("clipboard.debug.preview", "Preview"), 12, PdfColor.Accent, bold: true);
        AddText(commands, Margin + 120, y - 23, FormatPreviewNote(table.Count, t), 8.2f, PdfColor.Muted);

        for (var column = 0; column < columns; column++)
        {
            var x = Margin + 16 + column * cellWidth;
            AddRect(commands, x, tableTop, cellWidth, rowHeight, PdfColor.HeaderFill, PdfColor.Grid);
            AddText(commands, x + 6, tableTop + 6, ClipboardGridApi.ColumnName(column), 8.5f, PdfColor.Label, bold: true);
        }

        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                var x = Margin + 16 + column * cellWidth;
                var yy = tableTop - rowHeight - row * rowHeight;
                AddRect(commands, x, yy, cellWidth, rowHeight, PdfColor.White, PdfColor.Grid);
                var value = row < table.Count && column < table[row].Count ? table[row][column] : "";
                AddText(commands, x + 5, yy + 6, Truncate(value, Math.Max(6, cellWidth / 6)), 8.4f, PdfColor.Body);
            }
        }

        return y - height - 14;
    }

    private static int AddBeforeAfterCard(
        List<PdfCommand> commands,
        int y,
        string beforeText,
        string afterText,
        Func<string, string, string> t)
    {
        var beforeRows = ClipboardGridApi.ParseTable(beforeText);
        var afterRows = ClipboardGridApi.ParseTable(afterText);
        var rowCount = Math.Max(1, Math.Min(4, Math.Max(beforeRows.Count, afterRows.Count)));
        var height = 74 + rowCount * 18;
        AddCard(commands, y, height);
        AddText(commands, Margin + 16, y - 23, t("clipboard.debug.before_after", "Before / After"), 12, PdfColor.Accent, bold: true);

        var columnWidth = (CardWidth - 42) / 2;
        var leftX = Margin + 16;
        var rightX = leftX + columnWidth + 10;
        var titleY = y - 47;
        var tableTop = y - 67;
        AddText(commands, leftX, titleY, t("clipboard.debug.before", "Before"), 8.8f, PdfColor.Label, bold: true);
        AddText(commands, rightX, titleY, t("clipboard.debug.after", "After"), 8.8f, PdfColor.Label, bold: true);
        AddMiniTable(commands, leftX, tableTop, columnWidth, beforeRows, rowCount, t("clipboard.debug.before", "Before"));
        AddMiniTable(commands, rightX, tableTop, columnWidth, afterRows, rowCount, t("clipboard.debug.after", "After"));
        return y - height - 14;
    }

    private static void AddMiniTable(
        List<PdfCommand> commands,
        int x,
        int tableTop,
        int width,
        List<List<string>> rows,
        int rowCount,
        string title)
    {
        const int rowHeight = 18;
        var columns = rows.Count == 0 ? 1 : Math.Max(1, Math.Min(3, rows.Max(row => row.Count)));
        var cellWidth = width / columns;

        for (var column = 0; column < columns; column++)
        {
            var cellX = x + column * cellWidth;
            AddRect(commands, cellX, tableTop, cellWidth, rowHeight, PdfColor.HeaderFill, PdfColor.Grid);
            AddText(commands, cellX + 5, tableTop + 5, ClipboardGridApi.ColumnName(column), 8f, PdfColor.Label, bold: true);
        }

        for (var row = 0; row < rowCount; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                var cellX = x + column * cellWidth;
                var cellY = tableTop - rowHeight - row * rowHeight;
                AddRect(commands, cellX, cellY, cellWidth, rowHeight, PdfColor.White, PdfColor.Grid);
                var value = row < rows.Count && column < rows[row].Count ? rows[row][column] : "";
                AddText(commands, cellX + 5, cellY + 5, Truncate(value, Math.Max(6, cellWidth / 6)), 8.2f, PdfColor.Body);
            }
        }
    }

    private static void AddCard(List<PdfCommand> commands, int top, int height)
    {
        AddRect(commands, Margin, top - height, CardWidth, height, PdfColor.White, PdfColor.Border);
    }

    private static void AddRect(List<PdfCommand> commands, int x, int y, int width, int height, PdfColor fill, PdfColor stroke)
    {
        commands.Add(PdfCommand.Rect(x, y, width, height, fill, stroke));
    }

    private static void AddText(List<PdfCommand> commands, int x, int y, string text, float size, PdfColor color, bool bold = false)
    {
        commands.Add(PdfCommand.Text(x, y, ToPdfSafeText(text), size, color, bold));
    }

    private static string BuildPageStream(IEnumerable<PdfCommand> commands)
    {
        var stream = new StringBuilder();
        foreach (var command in commands)
            command.WriteTo(stream);
        return stream.ToString();
    }

    private static byte[] BuildPdfBytes(IReadOnlyList<string> streams, byte[]? formulaJpeg, byte[]? logoJpeg)
    {
        var hasFormulaImage = formulaJpeg is { Length: > 0 };
        var hasLogoImage = logoJpeg is { Length: > 0 };
        var xObjects = new List<string>();
        var firstImageObjectNumber = 5 + streams.Count * 2;
        var nextObjectNumber = firstImageObjectNumber;
        var logoObjectNumber = 0;
        var formulaObjectNumber = 0;
        if (hasLogoImage)
        {
            logoObjectNumber = nextObjectNumber++;
            xObjects.Add($"/ImLogo {logoObjectNumber} 0 R");
        }
        if (hasFormulaImage)
        {
            formulaObjectNumber = nextObjectNumber++;
            xObjects.Add($"/ImFormula {formulaObjectNumber} 0 R");
        }
        var xObjectResources = xObjects.Count == 0 ? "" : " /XObject << " + string.Join(" ", xObjects) + " >>";
        var pageObjectNumbers = Enumerable.Range(0, streams.Count).Select(index => 5 + index * 2).ToArray();
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            $"<< /Type /Pages /Kids [{string.Join(" ", pageObjectNumbers.Select(number => number + " 0 R"))}] /Count {streams.Count} >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>"
        };

        for (var index = 0; index < streams.Count; index++)
        {
            var pageObjectNumber = 5 + index * 2;
            var contentObjectNumber = pageObjectNumber + 1;
            var stream = streams[index];
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth} {PageHeight}] /Resources << /Font << /F1 3 0 R /F2 4 0 R >>{xObjectResources} >> /Contents {contentObjectNumber} 0 R >>");
            objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}\nendstream");
        }

        if (hasLogoImage)
            objects.Add(BuildJpegImageObject(logoJpeg!));
        if (hasFormulaImage)
            objects.Add(BuildJpegImageObject(formulaJpeg!));

        var builder = new StringBuilder();
        var offsets = new List<int> { 0 };
        builder.Append("%PDF-1.4\n% Tiedragon Clipboard Diagnosis\n");

        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.Append(i + 1).Append(" 0 obj\n");
            builder.Append(objects[i]).Append('\n');
            builder.Append("endobj\n");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append("xref\n0 ").Append(objects.Count + 1).Append('\n');
        builder.Append("0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
            builder.Append(offset.ToString("0000000000", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
        builder.Append("trailer\n<< /Size ").Append(objects.Count + 1).Append(" /Root 1 0 R >>\n");
        builder.Append("startxref\n").Append(xrefOffset).Append("\n%%EOF\n");
        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    private static string BuildJpegImageObject(byte[] jpegBytes)
    {
        var width = 1;
        var height = 1;
        try
        {
            using var image = Image.FromStream(new MemoryStream(jpegBytes));
            width = Math.Max(1, image.Width);
            height = Math.Max(1, image.Height);
        }
        catch
        {
            // Keep a valid tiny image object if dimensions cannot be read.
        }

        var hex = Convert.ToHexString(jpegBytes);
        return $"<< /Type /XObject /Subtype /Image /Width {width} /Height {height} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter [/ASCIIHexDecode /DCTDecode] /Length {hex.Length + 2} >>\nstream\n{hex}>\nendstream";
    }

    private static string FormatBool(bool value, Func<string, string, string> text)
    {
        return value ? text("common.yes", "Yes") : text("common.no", "No");
    }

    private static string FormatOwner(string owner, Func<string, string, string> text)
    {
        return string.Equals(owner, "none", StringComparison.OrdinalIgnoreCase)
            ? text("clipboard.debug.none", "None")
            : owner;
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

    private static string FormatPreviewNote(int rowCount, Func<string, string, string> text)
    {
        if (rowCount <= PreviewRowLimit)
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                text("clipboard.debug.preview_rows_all", "{0} rows shown"),
                rowCount);
        }

        return string.Format(
            CultureInfo.CurrentCulture,
            text("clipboard.debug.preview_rows_limited", "First {0} of {1} rows shown"),
            PreviewRowLimit,
            rowCount);
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
            return value;
        return value[..Math.Max(0, maxLength - 3)] + "...";
    }

    private static int EstimateTextWidth(string text, float size, bool bold)
    {
        var factor = bold ? 0.62f : 0.56f;
        return (int)Math.Ceiling(text.Length * size * factor);
    }

    private static string PrettyFormulaText(string value)
    {
        return value
            .Replace("×", "*", StringComparison.Ordinal)
            .Replace("·", "*", StringComparison.Ordinal)
            .Replace("−", "-", StringComparison.Ordinal)
            .Replace("⇒", " => ", StringComparison.Ordinal)
            .Replace("→", " -> ", StringComparison.Ordinal)
            .Replace("*", " * ", StringComparison.Ordinal)
            .Replace("/", " / ", StringComparison.Ordinal)
            .Replace("  ", " ", StringComparison.Ordinal)
            .Trim();
    }

    private static string StripMathMl(string mathMl)
    {
        var text = Regex.Replace(mathMl, "<[^>]+>", " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        return Regex.Replace(text, @"\s+", " ").Trim();
    }

    private static string ToPdfSafeText(string text)
    {
        text = text
            .Replace("×", "*", StringComparison.Ordinal)
            .Replace("·", "*", StringComparison.Ordinal)
            .Replace("−", "-", StringComparison.Ordinal)
            .Replace("⇒", " => ", StringComparison.Ordinal)
            .Replace("→", " -> ", StringComparison.Ordinal)
            .Replace("–", "-", StringComparison.Ordinal)
            .Replace("—", "-", StringComparison.Ordinal)
            .Replace("…", "...", StringComparison.Ordinal)
            .Replace("“", "\"", StringComparison.Ordinal)
            .Replace("”", "\"", StringComparison.Ordinal)
            .Replace("‘", "'", StringComparison.Ordinal)
            .Replace("’", "'", StringComparison.Ordinal)
            .Replace("€", "EUR", StringComparison.Ordinal);
        var builder = new StringBuilder(text.Length);
        foreach (var ch in text)
            builder.Append(ch is >= ' ' and <= '~' ? ch : '?');
        return builder.ToString();
    }

    private static string EscapePdf(string text)
    {
        return text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }

    private readonly record struct PdfColor(decimal R, decimal G, decimal B)
    {
        public static readonly PdfColor Title = new(0.07m, 0.09m, 0.15m);
        public static readonly PdfColor Accent = new(0.02m, 0.19m, 0.44m);
        public static readonly PdfColor Body = new(0.07m, 0.09m, 0.13m);
        public static readonly PdfColor Label = new(0.18m, 0.22m, 0.28m);
        public static readonly PdfColor Muted = new(0.30m, 0.35m, 0.43m);
        public static readonly PdfColor White = new(1m, 1m, 1m);
        public static readonly PdfColor Border = new(0.52m, 0.57m, 0.64m);
        public static readonly PdfColor Grid = new(0.60m, 0.65m, 0.72m);
        public static readonly PdfColor HeaderFill = new(0.90m, 0.93m, 0.96m);

        public string Fill => $"{F(R)} {F(G)} {F(B)} rg";
        public string Stroke => $"{F(R)} {F(G)} {F(B)} RG";

        private static string F(decimal value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private readonly record struct PdfCommand(
        string Kind,
        int X,
        int Y,
        int Width,
        int Height,
        string TextValue,
        float FontSize,
        bool Bold,
        PdfColor Fill,
        PdfColor Stroke,
        float LineWidth = 1f)
    {
        public static PdfCommand Text(int x, int y, string text, float size, PdfColor color, bool bold)
        {
            return new PdfCommand("text", x, y, 0, 0, text, size, bold, color, color);
        }

        public static PdfCommand Rect(int x, int y, int width, int height, PdfColor fill, PdfColor stroke)
        {
            return new PdfCommand("rect", x, y, width, height, "", 0, false, fill, stroke);
        }

        public static PdfCommand Line(int x1, int y1, int x2, int y2, PdfColor stroke, float lineWidth)
        {
            return new PdfCommand("line", x1, y1, x2, y2, "", 0, false, stroke, stroke, lineWidth);
        }

        public static PdfCommand Image(string name, int x, int y, int width, int height)
        {
            return new PdfCommand("image", x, y, width, height, name, 0, false, PdfColor.White, PdfColor.White);
        }

        public void WriteTo(StringBuilder stream)
        {
            if (Kind == "rect")
            {
                stream.AppendLine("q");
                stream.AppendLine(Fill.Fill);
                stream.AppendLine(Stroke.Stroke);
                stream.AppendLine($"{X} {Y} {Width} {Height} re B");
                stream.AppendLine("Q");
                return;
            }

            if (Kind == "line")
            {
                stream.AppendLine("q");
                stream.AppendLine(Stroke.Stroke);
                stream.AppendLine($"{LineWidth.ToString("0.##", CultureInfo.InvariantCulture)} w");
                stream.AppendLine($"{X} {Y} m {Width} {Height} l S");
                stream.AppendLine("Q");
                return;
            }

            if (Kind == "image")
            {
                stream.AppendLine("q");
                stream.AppendLine($"{Width} 0 0 {Height} {X} {Y} cm");
                stream.AppendLine($"/{TextValue} Do");
                stream.AppendLine("Q");
                return;
            }

            stream.AppendLine("BT");
            stream.AppendLine(Fill.Fill);
            stream.AppendLine($"/{(Bold ? "F2" : "F1")} {FontSize.ToString("0.##", CultureInfo.InvariantCulture)} Tf");
            stream.AppendLine($"1 0 0 1 {X} {Y} Tm ({EscapePdf(TextValue)}) Tj");
            stream.AppendLine("ET");
        }
    }
}
