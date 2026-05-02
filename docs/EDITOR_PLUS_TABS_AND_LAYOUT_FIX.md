# EditorPlus tabs and layout fix

Verbeteringen:

```text
menu nu boven toolbar
toolbar docked onder menu
flexibele layout zonder vaste top-padding
X-sluitknop op tabs
owner-drawn tab headers
```

## Waarom

De eerdere editor werkte functioneel, maar:

```text
toolbar stond boven menu
layout was minder flexibel
tabbladen hadden geen sluitknop
```

## Technisch

Aangepast in `NodEditorForm.cs`:

```text
MenuStrip -> Dock = Top
ToolStrip -> Dock = Top
SplitContainer -> Dock = Fill
TabControl -> OwnerDrawFixed
TabControl.DrawItem
TabControl.MouseDown
GetTabCloseRect(...)
CloseTab(...)
```
