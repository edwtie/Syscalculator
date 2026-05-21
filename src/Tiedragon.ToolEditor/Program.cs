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
        ApplicationConfiguration.Initialize();
        Application.Run(new ToolEditorForm());
    }
}
