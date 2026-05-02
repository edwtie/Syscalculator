# Old model build fix

Fixed build error:

```text
CS0029: Cannot implicitly convert type 'NodTraceResult' to 'NodResult'
```

Cause:

```csharp
var traceResult = NodEngine.ConvertForwardWithTrace(...);
result = traceResult; // wrong type
```

Fix:

```csharp
string outputText;
outputText = traceResult.Text;
_lastTrace = traceResult.Trace;
```

Also changed `NodEditorForm.cs` from:

```csharp
#nullable disable
```

to:

```csharp
#nullable enable
```

to remove CS8632 nullable annotation warnings.
