#nullable enable

namespace Tiedragon.Help;

public static class WebText
{
    public static string JavaScriptString(string value)
    {
        return "\"" + value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n") + "\"";
    }

    public static string DecodeEscapedNewLines(string value, string newLine)
    {
        return value.Replace("\\r\\n", newLine, StringComparison.Ordinal)
            .Replace("\\n", newLine, StringComparison.Ordinal)
            .Replace("\\r", newLine, StringComparison.Ordinal);
    }

    public static string NormalizeNewLines(string value, string newLine)
    {
        return DecodeEscapedNewLines(value, "\n")
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Replace("\n", newLine, StringComparison.Ordinal);
    }
}
