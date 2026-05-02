# Detachable tool windows

De NOD Editor ondersteunt nu een detachable simulator preview.

## Gedrag

```text
simulator zit standaard vast in editor
dubbelklik op simulator-kop = losmaken
slepen aan simulator-kop/toolbar = losmaken
Weergave > Simulator losmaken
Weergave > Simulator terugdocken
X sluiten in floating window = terugdocken maar verborgen
```

## Techniek

```text
FloatingToolForm.cs
UserControl-achtig hergebruik van bestaande GroupBox
Dock/undock door Control uit Panel te halen en in Form te plaatsen
```

## Belangrijk

De simulator wordt niet opnieuw gemaakt. Hetzelfde control wordt verplaatst tussen:

```text
_previewSplit.Panel2
FloatingToolForm
```
