# Enterprise prototype modules

Deze versie voegt echte prototype-onderdelen toe voor de enterprise-laag.

## Modules

```text
EnterpriseModel.cs
SqlPreviewGenerator.cs
ReportBuilder.cs
DocumentBatchEngine.cs
```

## Wat werkt nu?

### SQL-preview

Genereert review-SQL:

```csharp
SqlPreviewGenerator.GeneratePreviewSql(doc)
```

Voor `chg` maakt hij een `CASE WHEN ... LIKE ... THEN ...`-preview.

### SQL-update skeleton

```csharp
SqlPreviewGenerator.GenerateUpdateSql(doc)
```

Let op: dit voert niets uit. Het geeft alleen SQL-tekst.

### Rapportage

```csharp
ReportBuilder.BuildPreviewReport(doc, rows)
```

Maakt een previewrapport op basis van in-memory rows.

### Safety model

```csharp
SafetyOptions
NodRunContext
```

Bewaart of preview, backup en approval verplicht zijn.

### Document batch

```csharp
DocumentBatchEngine.ReplaceAll(...)
```

Werkt nu op in-memory tekst. Later kan dit gekoppeld worden aan echte bestanden.

## Wat is nog niet productie?

```text
echte databaseconnectie
echte backup/restore
echte rollback-transacties
PDF/Word/ODT-documentverwerking
rechtenbeheer
auditlog naar database
```

Dit is dus een echte werkende prototypebasis, niet volledig enterprise-productie.
