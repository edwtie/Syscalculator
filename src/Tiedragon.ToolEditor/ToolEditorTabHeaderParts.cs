#nullable enable

namespace Tiedragon.ToolEditor;

/// <summary>
/// UI parts for one editor tab header.
/// </summary>
public readonly record struct ToolEditorTabHeaderParts(Panel Panel, Label Title, Button CloseButton);
