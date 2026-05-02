# Editor polish build fix

Fixed build error:

```text
CS1061: ToolStripItem does not contain a definition for ShortcutKeys
```

Cause:

```csharp
file.DropDownItems.Add(...).ShortcutKeys = ...
```

`DropDownItems.Add(...)` returns `ToolStripItem`, while `ShortcutKeys` is on `ToolStripMenuItem`.

Fix:

```csharp
AddMenuItem(parent, text, handler, shortcutKeys)
```

This helper creates a `ToolStripMenuItem`, sets `ShortcutKeys`, then adds it to the parent menu.
