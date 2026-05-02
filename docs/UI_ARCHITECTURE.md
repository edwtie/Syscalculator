# Syscalculator 2.0 UI Architecture

Deze UI-laag is bewust gescheiden van `NodSystem.Core`.

## Projecten

```text
NodSystem.Core
= engine/library

Syscalculator.UI.WinForms
= hoofdscherm, menu, WizardExpress, editor, catalogus, trace viewer
```

## Forms

| Form | Oude VB6-rol | Nieuwe rol |
|---|---|---|
| MainForm | Form1.frm | hoofdcalculator |
| WizardExpressForm | WizardExpress.frm | clipboard/table conversie |
| NodEditorForm | editor.frm/Zeditor.frm | .nod editor + validator |
| CatalogManagerForm | Form3/Form4 | converterlijst beheren |
| TraceViewerForm | nieuw | Calculation Trace bekijken |

## Datastroom

```text
freesyscal.cfg
  ↓
NodCatalogService
  ↓
MainForm ComboBox
  ↓
NodParser
  ↓
NodEngine
  ↓
Resultaat / Trace
```

## Starten

```bash
dotnet run --project src/Syscalculator.UI.WinForms
```

Let op: dit vereist Windows vanwege WinForms.
