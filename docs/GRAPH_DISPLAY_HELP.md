# Graph Display Technical Help Notes

These notes describe technical content that can later be translated into user-facing Help. This is not the final in-app Help text.

## Main Explanation

The graph uses two layers:

```text
backend value -> formatter -> display text
user input text -> parser -> backend value
```

Backend values are exact values used for calculation, line positions, zoom, pan and markers.

Display values are readable text shown in labels, table cells and pointer text.

When the formula changes in the node editor, the calculated line, point table, red points and locator/reference view must be recalculated before redraw.

## Input Tooltip

Range and step input fields should later show a short tooltip:

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

The tooltip is explanation only. It must not parse text or change graph values.

While the user is typing in a graph range or step field, the graph should not update live. Apply the new value only after Enter or after the field loses focus.

If the field loses focus with invalid text, show a warning and keep the old graph value. The warning must not change graph lines, ranges or zoom.

When graph lines are off, range and step fields are information only. They must be read-only or disabled, and keyboard input must not be accepted. The calculated graph line, normal X/Y grid, red Step points and point table stay separate. The checkbox hides only the blue range markers and green markers.

In the large graph window, right mouse drag may show a red rectangle for zoom selection. The rectangle keeps the same aspect ratio as the current graph plot area, recalculated after dialog resize/layout changes. Releasing the mouse zooms to that rectangle. This feature is not available in the mini preview graph because there is not enough space. It changes only the graph view; it must not parse or edit the range input fields.

At nano scale and smaller, the large graph may show a one-time locator cue that explains where the current zoom position is. This prevents the user from getting lost at tiny scales, where the graph can feel like being very small inside the view. While dragging/panning, a red point or small red rectangle may move with the view. This is visual only and must not change graph data or parse input fields.

The locator uses the same reference view as the Home button.

## SI And Extreme Label Table

SI labels are case-sensitive. The design must distinguish prefix and unit:

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

Avoid putting `m` directly behind special values such as `lj` and `lP`. Keep the factor and base unit separate so `1 lj = 9.4607304725808 x 10^15` stays visually clear.

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

BIPM/NIST are authoritative for implementation. Educational screenshots or third-party tables are explanatory only.

## Point Table

The point table is shown only when the calculated graph line is visible. If the line is hidden or unavailable, the table is empty and hidden. The table button is disabled while the table is empty.

Table visibility has its own show/hide preference. If show is on and rows become available, the table appears again. If show is off, the table stays hidden even when rows become available.

Point rows are calculated from the current Step value. The default Step is `1,0`.

When zoomed far out, the table and red point markers may automatically use fewer rows/points by using a larger effective table step, such as `10` or `100`. When zooming in, the effective step may become smaller than `1,0`, such as `0,1`, `0,01` or SI-scale values. This prevents too many red points, keeps small-scale views useful, avoids unnecessary calculations and must not overwrite the user's Step value.

The Step spinner arrows should use scale-aware jumps, for example `1 -> 0,75 -> 0,5` when stepping down and `1 -> 1,25 -> 1,5 -> 2` when stepping up. Spinner changes are explicit user actions and commit a new user Step.

Step values are based on `mantissa x 10^n`. For example, `1,25 x 10^0 = 1,25`, while `1,25 x 10^-3 = 0,00125`. Automatic zoom step and spinner step should use predictable mantissas such as `0,5`, `0,75`, `1`, `1,25`, `1,5`, `2`, `2,5`, `5`, `7,5` and `10`.

## Clipboard Menu

Graph table copy should later offer spreadsheet-like choices:

- Copy exact values
- Copy display values
- Copy as CSV
- Copy as TSV

Exact values are for calculation. Display values are for reports and screenshots.

## Future Help Content

When the technical design is stable, create a separate user-facing Help page. That page should be shorter and avoid implementation terms such as backend, parser and formatter.

```text
src/syscalculator/Resources/Help/Content/main/graph-display.html
```
