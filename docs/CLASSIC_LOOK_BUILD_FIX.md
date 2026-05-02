# Classic look build fix

Fixed build errors in `MainForm.cs`:

```text
CS0103: _liveConvertCheckBox does not exist
```

Cause:

```text
The classic polish added a Config menu item for Live convert,
but this branch did not yet define the checkbox field.
```

Fix:

```text
Added _liveConvertCheckBox field
Added Live convert checkbox to options panel
```

Also reduced editor warnings:

```text
removed unused _simIntro field
added nullable suppression to NodEditorForm.cs for prototype UI code
```
