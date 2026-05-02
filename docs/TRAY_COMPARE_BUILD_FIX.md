# Tray Compare172 build fix

Fixed build errors where tray support methods existed but the backing fields were missing.

Added to `MainForm.cs`:

```csharp
private NotifyIcon _notifyIcon = null!;
private ContextMenuStrip _trayMenu = null!;
private bool _minimizeToTray = true;
private bool _allowRealClose;
```

Also cleaned `NodUiMetadata.cs` warning:

```text
CS0219: variable 'inIntro' is assigned but never used
```
