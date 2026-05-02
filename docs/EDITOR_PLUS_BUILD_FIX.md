# EditorPlus build fix

Fixed ambiguous overload errors:

```text
CS0121 Graphics.DrawLines(Point[] vs PointF[])
CS0121 Graphics.FillPolygon(Point[] vs PointF[])
```

Cause:

```csharp
[new Point(...), new Point(...)]
```

The compiler could not infer whether the collection expression should become `Point[]` or `PointF[]`.

Fix:

```csharp
new Point[] { new Point(...), new Point(...) }
```
