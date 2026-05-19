# Graph Display And Input Rules

The graph has two separate flows:

```text
backend value -> formatter -> display text
user input text -> parser -> backend value
```

Never use the rounded display text as a backend value.

```text
backend value -> rounded display text -> backend value
```

is forbidden because it loses precision and can move graph lines, markers or ranges.

## Backend Values

Backend values are the source of truth:

- graph point X/Y values
- X min / X max / Y min / Y max values
- Step value
- marker line positions
- zoom and pan view values
- range calculations

Backend values must stay numeric and exact for the chosen data type.

When the formula changes in the node editor, all graph-derived data must be recalculated from backend values:

- calculated graph line
- point table rows
- red point markers
- Home/reference view if the formula changes the graph extent
- nano-scale locator reference if it depends on the Home/reference view

Formula changes must invalidate stale graph data before redraw.

## Display Values

Display values are only for the user interface:

- axis labels
- marker line labels
- point table text
- pointer/status text
- visible text in range/step inputs

Display may be simplified:

```text
backend: 241.000000123
display: 241,0
```

Display may use SI prefixes:

```text
backend: 0.000000010
display: 10 n
```

Display may use extreme units already supported by the graph:

```text
backend: 9460730472580800
display: 1 lj
```

## Input Fields

X min, X max, Y min, Y max and Step are input fields.

The line checkbox controls only the blue range markers and green data/range markers. It does not control the calculated graph line, normal X/Y grid, red Step points or the point table.

- Lines on: blue range markers and green markers are visible.
- Lines off: blue range markers and green markers are hidden.
- Lines off: keyboard input, focus-loss parsing and validation warnings must not run for these fields.
- Lines off: the displayed values may still update from the backend/view for information only.
- Lines off: the calculated graph line and normal X/Y grid must remain visible.
- Lines off: red Step points and the point table may remain visible when calculated rows exist.

They need both display and input behavior:

```text
backend -> formatter -> visible text
typed text -> parser -> backend
```

Rules:

- Programmatic refresh may format the visible text.
- Programmatic refresh must not parse the formatted visible text back into the backend.
- User typing may parse text into a new backend value.
- User typing may support normal decimal input first.
- SI input may be added only if parsing is explicit and tested.
- Existing `Value` must remain the source of truth for graph calculations.

## Mouse Zoom And Pan

Mouse zoom and pan are backend/view operations.

They must not parse display text.

Flow:

```text
mouse wheel / drag -> backend view update -> formatter -> display text
```

Allowed:

- update graph view from mouse input
- redraw graph
- update visible labels through formatter
- update visible range/position display through formatter

Forbidden:

- read formatted display text during mouse zoom
- parse display labels into backend values
- rewrite backend range from rounded display text
- change marker values because a display label was rounded

Parsing is only allowed when the user explicitly edits an input field through keyboard/text entry and commits that edit.

Flow:

```text
keyboard input -> parser -> backend value -> formatter -> display text
```

## Mouse Rectangle Zoom

Right mouse drag may create a red zoom rectangle on the large graph window only.

This feature is not available in the mini preview graph because there is not enough space for precise rectangle selection.

Behavior:

- right mouse down starts the rectangle selection
- dragging updates the red rectangle overlay
- the rectangle keeps the same aspect ratio as the current graph plot area
- the aspect ratio is recalculated from the current plot size after dialog resize/layout changes
- dragging may expand or shrink the rectangle, but its width/height ratio stays locked to the current graph plot size
- releasing the right mouse button zooms the graph view to the selected rectangle
- pressing Escape or releasing with a too-small rectangle cancels the selection
- the rectangle is a temporary overlay and must not change graph data

Rules:

- rectangle zoom updates only the backend/view range
- rectangle zoom must preserve the current graph view aspect ratio
- rectangle zoom must not parse X min, X max, Y min, Y max or Step display text
- after zoom, visible range/viewpoint fields are regenerated from backend/view through the formatter
- if graph lines are off, the viewpoint fields remain read-only while still showing the new view

## Nano Scale View Locator

At nano scale and smaller, the graph may show a one-time view locator to help the user understand where the current zoom position is.

Design intent: prevent the user from getting lost at tiny scales. At nano/pico/femto scale the view can feel like being "very small" inside the graph, so the UI should keep orientation visible without interrupting exploration.

Behavior:

- when entering nano scale for the first time in a graph session, show a small locator/overview cue once
- the locator helps explain the current zoom position and the visible calculated line
- after the first cue, do not keep reopening or distracting the user
- while dragging/panning the graph, a red point or small red rectangle may move with the current view position
- the moving red indicator is a visual locator only

Rules:

- the locator reference/overview data is the same as the Home view data
- pressing Home resets the graph to the same reference view used by the locator
- the locator must follow backend/view pan and zoom state
- the locator must not parse range/step input fields
- the locator must not change graph data or calculated line values
- if there is no calculated line, the locator may still show view position but must not show point/table data
- this feature is for the large graph; the mini preview may show only a compact indicator if there is room

## Keyboard Input

Keyboard input is the only path where display-like text may become a backend value.

Allowed commit moments:

- Enter
- losing focus after the user edited the field, with validation warning if parsing fails
- spinner/button change if it changes the numeric backend value directly

During typing, do not live-update the graph. The user may type partial text such as `-`, `0,`, `10^` or `1e-` without changing the backend value. Parse and update only on an allowed commit moment.

Focus-loss validation behavior:

- if the text is valid, parse it, update the backend value, then regenerate display text
- if the text is invalid, show a warning and keep the old backend value
- after an invalid focus-loss warning, restore the field text to the old formatted backend value or keep focus in the field if the UI pattern supports it
- warnings must be local to the edited field and must not move graph lines or change ranges

Keyboard input examples:

```text
1,05  -> backend 1.05
1.05  -> backend 1.05
0,5   -> backend 0.5
5e-1  -> backend 0.5
5 x 10^-1 -> backend 0.5
0,0000005 -> backend 0.0000005
500 n -> backend 0.000000500
5 x 10^-7 -> backend 0.0000005
10^5  -> backend 100000
10^-5 -> backend 0.00001
1e-5  -> backend 0.00001
10 n  -> backend 0.000000010
500 u -> backend 0.000500
2 k   -> backend 2000
1 lj  -> backend one light-year value when supported by the parser/data type
```

Input fields should show a tooltip with short examples:

```text
Voorbeelden:
1,05
0,5 = 5 x 10⁻¹
0,0000005 = 500 n = 5 x 10⁻⁷
10⁵  (ook: 10^5)
10⁻⁵ (ook: 10^-5)
1e-5
10 n
2 k
```

The tooltip may use superscript display such as `10⁵` and `10⁻⁵` for readability. Parser input should still accept plain text forms such as `10^5` and `10^-5`.

Do not show `0,5` as `500 n`; that would mean `0,0000005`. Use `0,5 = 5 x 10⁻¹` for the half-unit example.

The tooltip is help text only. Showing the tooltip must not parse or change the backend value.

Rules:

- parsing only happens for the field the user edited
- typing by itself must not update the graph or backend value
- parser errors must show a warning and must not change the old backend value
- successful parsing updates backend first
- display text is then regenerated from backend through the formatter
- keyboard parsing must not run during mouse zoom, pan, repaint or graph resampling

## SI Table Help

The visible point table may show SI-formatted values to help users understand the scale.

The point table belongs to the calculated graph line:

- calculated line visible: point table may be visible and filled with calculated X/Y values
- calculated line hidden or unavailable: point table must be empty and hidden
- toggling the line off clears/hides the table display
- toggling the line back on repopulates the table from backend graph points
- table button is enabled only when the table has calculated rows
- table button is disabled when the table is empty/hidden
- table visibility has a separate user preference: show table on/off
- if show table is on and rows become available, the table appears automatically
- if show table is on but rows are empty, the table is hidden temporarily and the button is disabled; the show preference remains on
- if show table is off, the table must not appear automatically when rows become available
- if rows are empty, disabled or unavailable, the button stays disabled until rows are available again

Point rows are calculated using the current Step backend value:

- default Step is `1,0`
- Step display may be rounded/formatted, but row generation uses the backend Step value
- changing Step through a valid commit recalculates the table from backend graph data
- invalid Step input keeps the old Step backend value and does not recalculate the table

For wide zoom levels, the table should reduce row density automatically:

- when zoomed out, not every small step needs a visible table row
- the effective table step may automatically increase to `10`, `100`, `1000`, etc.
- when zooming back in, the effective table step may decrease below the user/default Step when the visible scale needs finer points
- this automatic effective table step controls table row generation, visible red point density and display density only
- it must not overwrite the user Step backend value unless the user explicitly edits Step
- the Step field may show the current effective table/display step for the active zoom level
- the user Step remains separate internally; editing the Step field commits a new user Step
- default user Step is `1,0`, but the effective table step may become larger while zoomed out or smaller while zoomed in
- Step spinner arrows should use scale-aware jumps such as `1 -> 0,75 -> 0,5` when stepping down and `1 -> 1,25 -> 1,5 -> 2` when stepping up, instead of a fixed `+1/-1`
- spinner stepping must commit a new user Step because it is an explicit user action
- Step values use mantissa times power-of-ten form: `mantissa x 10^n`
- example: `1,25 x 10^0 = 1,25`
- example: `1,25 x 10^-3 = 0,00125`
- automatic zoom step and spinner step should choose a mantissa from `0,5`, `0,75`, `1`, `1,25`, `1,5`, `2`, `2,5`, `5`, `7,5`, `10`, then multiply by `10^n`
- this keeps step behavior predictable across normal, nano and very large graph scales
- red points should be drawn only for the effective table/display step, so zoomed-out views do not show too many red points
- calculations for table rows and red point markers should use the effective table/display step where possible
- avoid calculating unnecessary intermediate table/marker points that will not be displayed
- this protects performance for large zoomed-out ranges

Example:

```text
backend x: 0.000000010
table x:   10 n
```

The table is display/help only:

- table cells may show SI prefixes
- table cells may show `lj` or smallest-scale labels where the renderer supports them
- table cells must not be used as backend source values
- copy/export must be an explicit choice: exact backend copy or display copy

Recommended behavior:

- visible table: display/SI values
- copy exact: backend values
- copy display: optional separate command later

SI and extreme display labels must have a clear meaning table in the technical design and later in user Help.

SI labels are case-sensitive. This prevents confusion between prefixes and units:

```text
m as unit   = meter, factor 10^0
m as prefix = milli = 10^-3
M as prefix = mega  = 10^6
k as prefix = kilo  = 10^3
h as prefix = hecto = 10^2
```

| Display label | Kind | Meaning | Value | Base unit |
| --- | --- | --- | --- | --- |
| m | base unit | meter | 10^0 | meter |
| h | SI prefix | hecto | 10^2 | base unit |
| k | SI prefix | kilo | 10^3 | base unit |
| M | SI prefix | mega | 10^6 | base unit |
| G | SI prefix | giga | 10^9 | base unit |
| m-prefix | SI prefix | milli | 10^-3 | base unit |
| u | SI prefix | micro | 10^-6 | base unit |
| n | SI prefix | nano | 10^-9 | base unit |
| p | SI prefix | pico | 10^-12 | base unit |
| f | SI prefix | femto | 10^-15 | base unit |
| a | SI prefix | atto | 10^-18 | base unit |
| z | SI prefix | zepto | 10^-21 | base unit |
| y | SI prefix | yocto | 10^-24 | base unit |
| r | SI prefix | ronto | 10^-27 | base unit |
| q | SI prefix | quecto | 10^-30 | base unit |
| lj | special length unit | lichtjaar | 9.4607304725808 x 10^15 | meter |
| lP | special length unit | Plancklengte | 1.616255 x 10^-35 | meter |

For graph display, avoid putting `m` directly behind special values such as `lj` and `lP`. Keep the factor and base unit separate so `1 lj = 9.4607304725808 x 10^15` stays visually clear.

Example scale values for Help and testing:

| Example | Approximate size | Display idea | Notes |
| --- | --- | --- | --- |
| typical atom diameter | 1 x 10^-10 to 5 x 10^-10 | 100 p to 500 p, or 0.1 n to 0.5 n | depends on element and bonding |
| atomic nucleus diameter | 4 x 10^-15 to 15 x 10^-15 | 4 f to 15 f | depends on nucleus mass |
| neutron/nucleon scale | about 1 x 10^-15 | about 1 f | not a hard sphere; use as scale example only |

Official SI prefix references:

- BIPM SI prefixes: https://www.bipm.org/en/measurement-units/si-prefixes
- NIST metric prefixes: https://www.nist.gov/weights-and-measures/prefixes
- OpenStax/LibreTexts atomic diameter example: https://chem.libretexts.org/Bookshelves/Organic_Chemistry/Organic_Chemistry_%28OpenStax%29/01%3A_Structure_and_Bonding/1.01%3A_Atomic_Structure_-_The_Nucleus
- Britannica femtometre/nucleus scale example: https://www.britannica.com/science/femtometre
- Neutron scale example: https://www.nuclear-power.com/nuclear-power/reactor-physics/atomic-nuclear-physics/fundamental-particles/neutron/properties-neutron/

BIPM/NIST are authoritative for SI prefix symbols. Educational screenshots or third-party tables may be useful for explanation, but they are not the source of truth for implementation.

## Clipboard Menu

Copying graph table data should offer an explicit choice, similar to spreadsheet applications.

Recommended clipboard menu:

- Copy exact values
- Copy display values
- Copy as CSV
- Copy as TSV

Meaning:

```text
Copy exact values
```

uses backend numeric values and is best for calculations.

```text
Copy display values
```

uses the same SI/rounded text the user sees in the table and is best for reports or screenshots.

The default copy action should be conservative:

- keyboard shortcut / quick copy: exact values
- menu copy display: visible SI values

This avoids silently replacing exact data with rounded display text.

## NumericUpDown Risk

WinForms `NumericUpDown` tightly couples `Text` and `Value`.

Risk:

```text
Value = 241.000000123
Text = 241,0
ValidateEditText parses Text
Value becomes 241.0
```

That is not allowed.

If range/step fields get rounded display text, the control must keep an internal exact value and must only parse text when the user is actively editing.

## Safe Implementation Scope

Safe display-only changes:

- `GraphPlotRenderer` label text
- visible point table cells
- pointer/status labels
- marker label text

Risky changes that require separate design and testing:

- custom range/step input control
- SI parsing in input fields
- changing `NumericUpDown.Text`
- changing `NumericUpDown.Value`
- range/view/marker synchronization

## Acceptance Tests

Before accepting graph display/input changes:

- Setting X min and X max keeps the same backend values after repaint.
- Blue marker lines stay on the backend values.
- Zoom and pan do not rewrite X/Y min/max unless that is explicitly intended.
- Step `1` may display as `1,0`, but backend stays `1`.
- User input `1,05` becomes backend `1.05`.
- Backend `1.000000123` must not become `1.0` because of display formatting.
- Small display values can show `n`, `u`, `m` without changing backend values.
- Extreme display values can show `lj` or `lP` where the renderer already supports them.
