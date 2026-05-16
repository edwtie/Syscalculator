# WizardExpress Clipboard Excel Issue

## Status

Open issue.

## Summary

`WizardExpress` in Syscalculator 2.0 follows the old VB6 model:

1. read text from the Windows clipboard
2. convert the values through the NOD engine
3. clear the clipboard
4. write the converted result back

This works correctly in plain text applications and also works in LibreOffice Calc.

The remaining problem is specific to Microsoft Excel / Microsoft Office clipboard behavior.

Important distinction:

- older Excel desktop uses the Windows clipboard plus Excel-specific rich formats
- Microsoft 365 / Office 365 online runs inside the browser and also has its own Office clipboard state
- Office 365 online can keep extra internal paste memory/history that Syscalculator cannot directly update through the Win32 clipboard API

## What was observed

### Source clipboard from Excel

Excel places a rich clipboard object on Windows, for example:

- `EnhancedMetafile`
- `MetaFilePict`
- `System.Drawing.Bitmap`
- `Biff12`
- `Biff8`
- `Biff5`
- `SymbolicLink`

### Clipboard after WizardExpress convert

After conversion, Syscalculator writes a new clipboard object with text/table formats:

- `System.String`
- `UnicodeText`
- `Text`
- `Csv`
- `TSV`
- `TabSeparatedValues`
- `HTML Format`

## Conclusion

The converted data itself is not the problem.

The issue is that Excel originally owns a richer clipboard object than normal text applications.  
When `WizardExpress` clears and replaces the clipboard, the old Excel-specific formats are gone.

For Office 365 online there is an extra limitation: the browser/Office web app can maintain its own clipboard handling on top of the Windows clipboard. Syscalculator can write normal Windows clipboard formats such as text, TSV, CSV and HTML, but it cannot update Office 365 online's internal clipboard memory directly.

That means:

- Notepad works
- LibreOffice Calc works
- older Excel desktop is a rich-clipboard special case
- Office 365 online is a browser/Office-web clipboard special case

## Why this matches the old wizard

The old VB6 `WizardExpress.frm` uses:

```vb
Clipboard.Clear
Clipboard.SetText Convert
ready = Convert
```

So the old design also replaces the clipboard instead of preserving Excel-specific formats.

## Practical result

Current behavior is considered correct for classic `WizardExpress` compatibility:

- clipboard reset first
- converted text/table data written back
- timer compares current clipboard text with last converted text
- Office 365 online may still choose its own internal clipboard/paste state

## Advanced Mode

Advanced mode now separates normal use from inspection:

- `Excel-style data` opens a clean table view with `Input` and `Output` tabs
- tab-separated clipboard text is shown as spreadsheet-like rows and columns
- output can be copied back as plain TSV
- `Clipboard debug` remains available for low-level clipboard formats and owner details

This makes Office/Excel troubleshooting clearer without turning the main WizardExpress window into a technical debug screen.

## Future option

If Excel desktop-perfect paste behavior is required, this needs a separate Excel-specific route, likely with Office/Excel automation or interoperability.

If Office 365 online-perfect paste behavior is required, Excel COM automation is not enough because the target is a browser app. Safer fallback routes are:

- export converted data as `.tsv` / `.csv`
- paste as plain text into the browser after explicitly copying from Syscalculator
- add a WizardExpress "save table" or "copy plain TSV" mode that avoids pretending it can preserve Office's internal clipboard
