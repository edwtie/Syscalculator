---

# Technical Appendix — Syscalculator 2.0 Alpha 1

## A. Project structure

```text
src/
├─ NodSystem.Core
│  ├─ NodDocument.cs
│  ├─ NodParser.cs
│  ├─ NodEngine.cs
│  ├─ NodExpressionEvaluator.cs
│  ├─ NodReverse.cs
│  ├─ CalculationTrace.cs
│  ├─ EquationEngine.cs
│  ├─ DataTransformEngine.cs
│  ├─ SqlPreviewGenerator.cs
│  ├─ ReportBuilder.cs
│  ├─ DocumentBatchEngine.cs
│  ├─ DataModel.cs
│  ├─ EquationModel.cs
│  └─ EnterpriseModel.cs
│
├─ NodSystem.Tests
│  └─ Program.cs
│
├─ NodSystem.Demo
│  └─ Program.cs
│
└─ Syscalculator.UI.WinForms
   ├─ MainForm.cs
   ├─ WizardExpressForm.cs
   ├─ NodEditorForm.cs
   ├─ CatalogManagerForm.cs
   ├─ TraceViewerForm.cs
   ├─ NodCatalog.cs
   ├─ NodUiMetadata.cs
   ├─ Program.cs
   ├─ freesyscal.cfg
   └─ Converters/
```

## B. Architectural split

### Syscalculator 1.72

In the old VB6 version, UI and engine logic were closely connected.

```text
Form1.frm
  ↓
Getans(...)
  ↓
MathExecute / ChgMath / transMath
  ↓
global arrays and variables
```

Examples of old global structures:

```text
Smath()
chgo() / chgn()
transo() / transn()
symbool1..4
vraag1 / vraag2
Appsnaam
```

### Syscalculator 2.0 Alpha 1

In Alpha 1, these concerns are separated.

```text
Syscalculator.UI.WinForms
  ↓
NodSystem.Core
  ↓
NodParser / NodEngine / sub-engines
```

The core library has no UI dependency.  
The UI project depends on the core library.

## C. NOD parsing flow

```text
.nod text
  ↓
NodParser.Parse(...)
  ↓
NodDocument
  ↓
NodEngine
```

`NodParser` recognizes:

```text
Name
format
mode
reverse
chg
trans
math
table
field
output
phoneformat
lookup
match
given
equation
solve
constraint
end
```

UI metadata such as `input1`, `input2`, `Symb1`–`Symb4` is also read by the UI helper:

```text
NodUiMetadata.cs
```

Reason:

```text
NodSystem.Core = conversion engine
Syscalculator.UI = display labels and form layout
```

## D. Legacy math vs NOD 2.0 math

One important technical fix in Alpha 1 is the distinction between legacy math and expression math.

### Legacy math

```nod
math ans * 1,8
math ans + 32
```

Internal model:

```csharp
MathStep10(char Operator, decimal Number)
```

This only accepts:

```text
ans operator decimal-number
```

### NOD 2.0 expression math

```nod
math ans * e^2
math sqrt((ans^2 + 25) / 3)
math log(ans,2)
math sind(ans)
```

Internal model:

```csharp
List<string> MathExpressions20
```

Technical rule:

```text
If the third part is not a decimal number, do not parse it as MathStep10.
Send it to NodExpressionEvaluator.
```

This prevents the old bug:

```text
Invalid decimal number: 'e^2'
```

## E. Expression evaluator

`NodExpressionEvaluator` is a small recursive-descent parser.

Supported concepts:

```text
numbers with comma or dot
ans / x
variables from equation context
e
pi / π
+ - * / ^
parentheses
absolute value with |ans|
functions
implicit multiplication
```

Examples:

```nod
math ans e^2
math 2(ans + 1)
math pi * ans^2
```

Supported functions include:

```text
sqrt / sqr
abs
pow
exp
ln
log
sin / cos / tan
asin / acos / atan
sind / cosd / tand
asind / acosd / atand
rad / deg
min / max
round / floor / ceil
```

## F. Reverse strategy

Alpha 1 supports three reverse mechanisms.

### 1. Legacy automatic reverse

For NOD 1.0 math:

```nod
math ans * 1,8
math ans + 32
```

Reverse becomes:

```text
ans - 32
ans / 1,8
```

Implementation:

```csharp
MathStep10.Reverse()
```

### 2. Explicit reverse

For NOD 2.0 formulas:

```nod
math (ans + 21) * 2
reverse (ans / 2) - 21
```

Implementation:

```csharp
doc.ReverseExpression
```

### 3. Auto-reverse for simple steps

`NodReverse.cs` can infer simple inverse expressions.

Examples:

```text
ans * e^2     -> ans / e^2
ans + 32      -> ans - 32
sind(ans)     -> asind(ans)
log(ans,2)    -> pow(2,ans)
ln(ans)       -> exp(ans)
rad(ans)      -> deg(ans)
```

## G. Calculation Trace

Calculation Trace records each math step during forward calculation.

Example:

```nod
math ans * e^2
math ans + 10
math sqrt(ans)
```

Trace model:

```csharp
CalculationTrace
CalculationTraceStep
NodTraceResult
```

Each step stores:

```text
Expression
InputValue
OutputValue
AutoReverseExpression
```

Example trace:

```text
3              -> ans * e^2  -> 22.167168...
22.167168...   -> ans + 10   -> 32.167168...
32.167168...   -> sqrt(ans)  -> 5.671610...
```

Reverse from trace:

```text
sqrt(ans)  -> ans^2
ans + 10   -> ans - 10
ans * e^2  -> ans / e^2
```

Implementation:

```csharp
NodEngine.ConvertForwardWithTrace(...)
NodEngine.ConvertReverseFromTrace(...)
```

Purpose:

```text
Avoid full algebra solving for normal calculator use.
Use stored steps instead.
```

## H. Equation engine

The equation engine is intentionally limited in Alpha 1.

Supported form:

```nod
mode equation
given m = 80
given v = 12
equation E = 0,5 * m * v^2
solve E
constraint E >= 0
end
```

Supported solving pattern:

```text
solve variable is alone on left side
or
solve variable is alone on right side
```

Example supported:

```text
E = 0,5 * m * v^2
```

Not yet supported:

```text
2 * x + 4 = 10
x^2 = 9
symbolic rearranging
multiple solutions
complex algebra
```

Alpha 1 is therefore a formula evaluator, not a full CAS.

## I. Data engine

`DataTransformEngine` can process an in-memory row.

Input:

```csharp
IDictionary<string,string>
```

NOD example:

```nod
mode data
table klanten

field telefoon
phoneformat country NL
phoneformat remove_text_prefix true
chg 03402,03060
output telefoon_nieuw
```

Output model:

```csharp
FieldTransformResult
```

Contains:

```text
FieldName
OutputField
Original
Normalized
Result
```

This makes preview/reporting possible without touching a real database.

## J. Phoneformat logic

Phoneformat currently supports basic normalization.

Options:

```text
country
remove_spaces
remove_dots
remove_slashes
remove_parentheses
remove_text_prefix
normalize_international
keep_separator
```

Example:

```text
tel. 03402-36647 -> 0340236647 -> 0306036647
+31 3402 36647   -> 0340236647 -> 0306036647
```

Current limitation:

```text
keep_separator exists in the model, but final formatting with separator is not fully implemented yet.
```

## K. SQL preview generator

`SqlPreviewGenerator` generates SQL text only.

It does not execute SQL.

Supported in Alpha 1:

```text
field preview
field update skeleton
chg via CASE WHEN
trans via CASE WHEN
legacy math via arithmetic expressions
lookup/JOIN preview skeleton
```

Example:

```sql
SELECT
  telefoon AS old_value,
  CASE WHEN telefoon LIKE '03402%' THEN '03060' || SUBSTR(telefoon, 6) ELSE telefoon END AS new_value
FROM klanten;
```

Technical limitation:

```text
SQL dialect is generic.
'||' and SUBSTR are SQLite/PostgreSQL-like.
SQL Server would need + and SUBSTRING.
```

Future design:

```text
SqlDialect
  - SQLite
  - PostgreSQL
  - SQL Server
  - MySQL
```

## L. Report builder

`ReportBuilder` creates preview reports from in-memory rows.

Input:

```csharp
NodDocument
IEnumerable<IDictionary<string,string>>
NodRunContext
```

Output:

```csharp
NodPreviewReport
```

Report includes:

```text
Run ID
RuleSetName
TableName
ScannedRows
ChangedFields
Warnings
Lines
```

Alpha 1 does not yet export PDF/Word/HTML.

## M. Safety model

Alpha 1 has a safety model, not a full enterprise safety system.

Classes:

```text
SafetyOptions
NodRunContext
NodPreviewReport
```

Safety flags:

```text
PreviewRequired
BackupRequired
ApprovalRequired
RollbackSupported
ApprovedBy
```

Current status:

```text
safety is visible in report
approval warning can be generated
no real database transaction yet
no real backup file yet
no real audit database yet
```

## N. Document batch engine

`DocumentBatchEngine` works on in-memory strings.

Input:

```csharp
IReadOnlyDictionary<string,string> documents
IReadOnlyList<DocumentTextRule> rules
```

Output:

```csharp
DocumentBatchResult
```

Tracks:

```text
DocumentsScanned
DocumentsChanged
Replacements
Lines
```

Current limitation:

```text
No direct .docx/.odt/.pdf processing yet.
No filesystem include/exclude yet.
No backup/restore files yet.
```

Future module:

```text
DocumentFileBatchEngine
DocxTextAdapter
OdtTextAdapter
PlainTextAdapter
```

## O. UI layer

### MainForm

MainForm maps to old VB6 `Form1.frm`.

Functions:

```text
load freesyscal.cfg
select converter
load .nod
show input1/input2
forward conversion
reverse conversion
trace storage
menu access
```

### WizardExpressForm

Maps to old `WizardExpress.frm`.

Functions:

```text
clipboard input
tab-separated cells
line-by-line conversion
forward/reverse
copy output to clipboard
```

### NodEditorForm

Maps to old `editor.frm` and `Zeditor.frm`.

Functions:

```text
open .nod
save .nod
validate .nod
test input/output
```

### CatalogManagerForm

Maps to old `Form3.frm` / `Form4.frm`.

Functions:

```text
load freesyscal.cfg
edit display name
edit nod path
set default flag
save catalog
```

### TraceViewerForm

New UI form.

Functions:

```text
show start value
show final value
show steps
show reverse expressions
```

## P. Configuration compatibility

Alpha 1 keeps the old catalog idea:

```text
freesyscal.cfg
```

Current format:

```text
Display name,NOD path,*
```

Example:

```text
Celsius naar Fahrenheit,Converters/celsius_fahrenheit.nod,*
Ans maal e kwadraat,Converters/e2.nod,
```

The `*` means default converter.

## Q. Known technical debt

```text
No .sln file yet
No installer yet
No dependency injection
No formal unit test framework
No CI/CD
No SQL dialect abstraction
No real database adapter
No real document file adapter
No old language-file migration yet
No old tray behavior fully restored
No old MenuXP styling migrated
```

## R. Recommended next technical steps

1. Add solution file:

```bash
dotnet new sln
dotnet sln add src/NodSystem.Core/NodSystem.Core.csproj
dotnet sln add src/NodSystem.Tests/NodSystem.Tests.csproj
dotnet sln add src/Syscalculator.UI.WinForms/Syscalculator.UI.WinForms.csproj
```

2. Add real test framework:

```text
xUnit or MSTest
```

3. Add SQL dialect abstraction:

```csharp
ISqlDialect
SqlServerDialect
PostgreSqlDialect
SQLiteDialect
```

4. Add converter catalog import from old `freesyscal.cfg`.

5. Add full old `.nod` compatibility test set.

6. Add UI polish:

```text
icons
status colors
keyboard shortcuts
tray support
about dialog
language files
```

7. Add document adapters:

```text
.txt
.csv
.docx
.odt
```

8. Add persistence:

```text
recent files
settings
default converter
last window position
```

## S. Technical conclusion

Syscalculator 2.0 Alpha 1 proves that the old architecture can be modernized.

The old VB6 model:

```text
Form + global arrays + OpenNOD + Getans
```

is now split into:

```text
UI
Parser
Model
Engine
Math evaluator
Reverse
Trace
Data
SQL preview
Report
Document batch
```

This makes the system:

```text
more testable
more maintainable
more extensible
safer for future enterprise use
```
