#nullable enable

namespace Tiedragon.ToolEditor;

// Copyright (c) Tiedragon. All rights reserved.
//
// Standalone entry point for the Tiedragon ToolEditor application.
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        try
        {
            ToolEditorDebugger.Initialize();
            ApplicationConfiguration.Initialize();
            ToolEditorUiThemeSettings.ApplyApplicationColorMode(ToolEditorUiThemeSettings.Load());
            ToolEditorDebugger.Log("Startup mode: Standalone ToolEditor.");
            Application.Run(new ToolEditorForm());
        }
        catch (Exception ex)
        {
            ToolEditorDebugger.ReportException("Fatal ToolEditor exception", ex, showDialog: true);
            throw;
        }
    }

}
