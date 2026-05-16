#nullable enable
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Tiedragon.ClipboardConvert;

/// <summary>
/// Reusable API for clipboard read, copy, paste, and delimited text conversion.
/// </summary>
public static class ClipboardConvertApi
{
    /// <summary>
    /// Reads text and debug metadata from the clipboard.
    /// </summary>
    public static ClipboardSnapshot ReadSnapshot()
    {
        try
        {
            var data = Clipboard.GetDataObject();
            if (data is null)
                return ClipboardSnapshot.Empty;

            var formats = data.GetFormats();
            string text;

            if (data.GetDataPresent(DataFormats.UnicodeText))
                text = data.GetData(DataFormats.UnicodeText)?.ToString() ?? "";
            else if (data.GetDataPresent(DataFormats.Text))
                text = data.GetData(DataFormats.Text)?.ToString() ?? "";
            else
                text = Clipboard.ContainsText() ? Clipboard.GetText() : "";

            return new ClipboardSnapshot(text, formats, DescribeClipboardOwner());
        }
        catch
        {
            return ClipboardSnapshot.Empty;
        }
    }

    /// <summary>
    /// Tries to read plain clipboard text.
    /// </summary>
    public static bool TryGetText(out string text)
    {
        var snapshot = ReadSnapshot();
        text = snapshot.Text;
        return !string.IsNullOrWhiteSpace(text);
    }

    /// <summary>
    /// Copies text to the clipboard, including text, TSV, CSV, and HTML table formats when possible.
    /// </summary>
    public static bool SetText(string rawText, IntPtr ownerHandle = default)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return false;

        var text = NormalizeText(rawText);
        try
        {
            if (TryWriteClipboardWithOwner(text, ownerHandle))
                return true;
        }
        catch
        {
        }

        try
        {
            Clipboard.Clear();
            var data = BuildClipboardDataObject(text);
            Clipboard.SetDataObject(data, true, 10, 100);
            if (Clipboard.ContainsText() && Clipboard.GetText() == text)
                return true;
        }
        catch
        {
        }

        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                Clipboard.Clear();
                Clipboard.SetText(text, TextDataFormat.UnicodeText);
                if (Clipboard.ContainsText() && Clipboard.GetText() == text)
                    return true;
            }
            catch
            {
                Thread.Sleep(75);
            }
        }

        return false;
    }

    /// <summary>
    /// Converts newline/tab delimited clipboard text cell by cell.
    /// </summary>
    public static ClipboardTextConversionResult ConvertDelimitedText(
        string sourceText,
        Func<string, int, ClipboardCellConversion> convertCell)
    {
        var normalizedInput = sourceText.Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = normalizedInput.Split('\n').ToList();
        while (lines.Count > 0 && lines[^1].Length == 0)
            lines.RemoveAt(lines.Count - 1);

        var output = new StringBuilder();
        var hadError = false;
        var cellCount = 0;
        var errorCellCount = 0;

        for (var rowIndex = 0; rowIndex < lines.Count; rowIndex++)
        {
            var cells = lines[rowIndex].Split('\t');
            for (var cellIndex = 0; cellIndex < cells.Length; cellIndex++)
            {
                if (cellIndex > 0)
                    output.Append('\t');

                var converted = convertCell(cells[cellIndex], rowIndex);
                hadError |= converted.HadError;
                cellCount++;
                if (converted.HadError)
                    errorCellCount++;
                output.Append(converted.Text);
            }

            if (rowIndex < lines.Count - 1)
                output.Append(Environment.NewLine);
        }

        var summary = new ClipboardConversionSummary(
            lines.Count,
            cellCount,
            cellCount - errorCellCount,
            errorCellCount);
        return new ClipboardTextConversionResult(output.ToString(), hadError, summary);
    }

    /// <summary>
    /// Normalizes clipboard text to the current platform newline convention.
    /// </summary>
    public static string NormalizeText(string text)
    {
        return text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", Environment.NewLine);
    }

    private static DataObject BuildClipboardDataObject(string text)
    {
        var data = new DataObject();
        var csv = BuildCsvText(text);
        var html = BuildClipboardHtml(text);

        data.SetData(DataFormats.UnicodeText, true, text);
        data.SetData(DataFormats.Text, true, text);
        data.SetData(DataFormats.StringFormat, true, text);
        data.SetData("CSV", true, csv);
        data.SetData("Csv", true, csv);
        data.SetData("TSV", true, text);
        data.SetData("TabSeparatedValues", true, text);
        data.SetData(DataFormats.CommaSeparatedValue, true, csv);
        data.SetData(DataFormats.Html, true, html);

        return data;
    }

    private static bool TryWriteClipboardWithOwner(string text, IntPtr ownerHandle)
    {
        var csv = BuildCsvText(text);
        var html = BuildClipboardHtml(text);
        var handles = new List<IntPtr>();
        var opened = false;

        try
        {
            if (!OpenClipboard(ownerHandle))
                return false;

            opened = true;

            if (!EmptyClipboard())
                return false;

            if (!SetClipboardTextFormat(CF_UNICODETEXT, text, Encoding.Unicode, appendNullTerminator: true, handles))
                return false;
            if (!SetClipboardTextFormat(CF_TEXT, text, Encoding.Default, appendNullTerminator: true, handles))
                return false;
            if (!SetClipboardRegisteredFormat("Csv", csv, Encoding.Unicode, handles))
                return false;
            if (!SetClipboardRegisteredFormat("CSV", csv, Encoding.Unicode, handles))
                return false;
            if (!SetClipboardRegisteredFormat("TSV", text, Encoding.Unicode, handles))
                return false;
            if (!SetClipboardRegisteredFormat("TabSeparatedValues", text, Encoding.Unicode, handles))
                return false;
            if (!SetClipboardTextFormat(CF_HTML, html, Encoding.UTF8, appendNullTerminator: false, handles))
                return false;

            return true;
        }
        finally
        {
            if (opened)
                CloseClipboard();

            foreach (var handle in handles)
            {
                if (handle != IntPtr.Zero)
                    GlobalFree(handle);
            }
        }
    }

    private static bool SetClipboardRegisteredFormat(string formatName, string text, Encoding encoding, List<IntPtr> handles)
    {
        var format = RegisterClipboardFormat(formatName);
        return format != 0 && SetClipboardTextFormat(format, text, encoding, appendNullTerminator: true, handles);
    }

    private static bool SetClipboardTextFormat(uint format, string text, Encoding encoding, bool appendNullTerminator, List<IntPtr> handles)
    {
        var bytes = encoding.GetBytes(text);
        var terminatorLength = appendNullTerminator ? encoding.GetByteCount("\0") : 0;
        var size = bytes.Length + terminatorLength;
        var hGlobal = GlobalAlloc(GMEM_MOVEABLE, (nuint)size);
        if (hGlobal == IntPtr.Zero)
            return false;

        var target = GlobalLock(hGlobal);
        if (target == IntPtr.Zero)
        {
            GlobalFree(hGlobal);
            return false;
        }

        try
        {
            Marshal.Copy(bytes, 0, target, bytes.Length);
            for (var i = 0; i < terminatorLength; i++)
                Marshal.WriteByte(target, bytes.Length + i, 0);
        }
        finally
        {
            GlobalUnlock(hGlobal);
        }

        if (SetClipboardData(format, hGlobal) == IntPtr.Zero)
        {
            GlobalFree(hGlobal);
            return false;
        }

        handles.Add(IntPtr.Zero);
        return true;
    }

    private static string BuildCsvText(string text)
    {
        var rows = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var output = new StringBuilder();

        for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            var cells = rows[rowIndex].Split('\t');
            for (var cellIndex = 0; cellIndex < cells.Length; cellIndex++)
            {
                if (cellIndex > 0)
                    output.Append(',');

                output.Append('"');
                output.Append(cells[cellIndex].Replace("\"", "\"\""));
                output.Append('"');
            }

            if (rowIndex < rows.Length - 1)
                output.Append(Environment.NewLine);
        }

        return output.ToString();
    }

    private static string BuildClipboardHtml(string text)
    {
        var rows = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var body = new StringBuilder();
        body.AppendLine("<table>");

        foreach (var row in rows)
        {
            body.AppendLine("<tr>");
            foreach (var cell in row.Split('\t'))
            {
                body.Append("<td>");
                body.Append(System.Net.WebUtility.HtmlEncode(cell));
                body.AppendLine("</td>");
            }
            body.AppendLine("</tr>");
        }

        body.AppendLine("</table>");

        var fragment = body.ToString();
        var htmlBody = "<html><body><!--StartFragment-->" + fragment + "<!--EndFragment--></body></html>";
        const string headerTemplate =
            "Version:0.9\r\n" +
            "StartHTML:{0:0000000000}\r\n" +
            "EndHTML:{1:0000000000}\r\n" +
            "StartFragment:{2:0000000000}\r\n" +
            "EndFragment:{3:0000000000}\r\n";

        var dummyHeader = string.Format(headerTemplate, 0, 0, 0, 0);
        var startHtml = dummyHeader.Length;
        var startFragment = startHtml + htmlBody.IndexOf("<!--StartFragment-->", StringComparison.Ordinal) + "<!--StartFragment-->".Length;
        var endFragment = startHtml + htmlBody.IndexOf("<!--EndFragment-->", StringComparison.Ordinal);
        var endHtml = startHtml + htmlBody.Length;
        var header = string.Format(headerTemplate, startHtml, endHtml, startFragment, endFragment);
        return header + htmlBody;
    }

    private static string DescribeClipboardOwner()
    {
        try
        {
            var owner = GetClipboardOwner();
            if (owner == IntPtr.Zero)
                return "none";

            var title = new StringBuilder(128);
            GetWindowText(owner, title, title.Capacity);
            GetWindowThreadProcessId(owner, out var processId);
            var processName = "";
            try
            {
                processName = Process.GetProcessById((int)processId).ProcessName;
            }
            catch
            {
            }

            var titleText = title.ToString();
            if (!string.IsNullOrWhiteSpace(processName) && !string.IsNullOrWhiteSpace(titleText))
                return $"{processName}: {titleText}";
            if (!string.IsNullOrWhiteSpace(processName))
                return processName;
            return owner.ToString("X");
        }
        catch
        {
            return "unknown";
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetClipboardOwner();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint RegisterClipboardFormat(string lpszFormat);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, nuint dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr hMem);

    private const uint GMEM_MOVEABLE = 0x0002;
    private const uint CF_TEXT = 1;
    private const uint CF_UNICODETEXT = 13;
    private static readonly uint CF_HTML = RegisterClipboardFormat("HTML Format");
}
