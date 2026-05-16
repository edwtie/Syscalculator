# NOD Legacy Groups

The old `broncode.zip` contains the original `.nod` grouping used by the classic installer.

These directories are release groups:

```text
Distance/
euro/
Mass/
Pressure/
Temperature/
Volume/
```

The modern `Converters` directory keeps this grouping:

```text
Converters/Distance/
Converters/euro/
Converters/Mass/
Converters/Pressure/
Converters/Temperature/
Converters/Volume/
```

The `euro` group has been extended beyond the old zip. The legacy zip contains:

```text
ATS
BEF
DEM
ESP
FIM
FRF
GRD
IEP
ITL
LUF
NLG
NLG-FRF
PTE
```

Syscalculator 2.0 also includes newer euro-related country/currency files:

```text
BGN
CYP
EEK
HRK
LTL
LVL
MTL
SIT
SKK
```

These newer files were not present in the old `broncode.zip`, which made those converters impossible to find in the legacy release set. Syscalculator 2.0 keeps them in `Converters/euro/` so the catalog can discover and show them with the rest of the euro converters.

Syscalculator 2.0 adds new development/demo groups:

```text
Converters/Math/
Converters/Text/
```

The old zip also contains two root `.nod` files:

```text
21.nod
21w.nod
```

They are historical examples. Their functionality is already represented by grouped files:

```text
21.nod  -> Converters/euro/BEF.nod
21w.nod -> Converters/Temperature/graden Fahrenheit.nod
```

So they are not added as separate release converters.
