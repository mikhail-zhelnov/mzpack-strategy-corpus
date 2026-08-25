# export/ — exporting data and chart objects to CSV

MZpack can export strategy/indicator data to CSV for analysis/ML.
API namespace: `MZpack.NT8.Algo.DataExport`. General scheme:
create an export object → set `DataSet.Schema` → `Register(export)` in `State.DataLoaded`.

## Two kinds of export

### 1. IndicatorExport — indicator values (canonical: `Algo\Strategies\Data_Export\Data_Export.cs`, 53 KB)
The file is large — keep it as a reference, don't copy it. What it shows:
```csharp
var export = new IndicatorExport(this, footprintIndicator,
    ExportDataSource.Level1, Footprint_ExportTemporality, ExportGranularity.Bar,
    new ExportArgs {
        IsExportWhileCollecting = false, IsHeader = ..., IsTime = ...,
        SignedVolume = ..., Delimiter = ';', IsFile = true, FileName = ..., IsBatch = ...
    });
export.DataSet.Schema = getFootprintExportSchema(export);  // see below
Register(export);
```
The schema is a list of fields via `IndValue`:
```csharp
DataSchema v = new DataSchema(export.DataSet);
if (Footprint_POC)   v.Append(IndValue.POC);
if (Footprint_Delta) v.Append(IndValue.Delta);
// ... dozens of IndValue.*: Open/Close/High/Low, Volume, BuyVolume, Delta*, VAH/VAL/POC,
//     Imbalance/Absorption counts, COTHigh/Low, UnfinishedAuction*, Session*, etc.
```
Data_Export demonstrates exporting Footprint / VolumeProfile / BigTrade / MarketDepth —
each with its own `getXxxExportSchema(...)`. The default file path is:
`Documents\NinjaTrader 8\mzpack\strategy\<namespace>\<class>\data\<random>.csv`.

### 2. DrawingObjectsExport — chart objects (this file: `DrawingObjects_Export.cs`, copied)
Exports objects from the chart (indicator labels/markers) to CSV using a mapping schema:
```csharp
var export = new DrawingObjectsExport(this, new ExportArgs { ... });
// schema: either from xml, or built
schema.Append("BOB", ValueKind.Feature, new ChartObjectDescriptor {
    Script = "ninZaBossOrderBlock",
    Map = new List<MapItem> {
        new MapItem { Tool = DrawingTool.Text, Text = "▲", Value =  1.0 },
        new MapItem { Tool = DrawingTool.Text, Text = "▼", Value = -1.0 },
    }
});
export.DataSet.Schema = schema;     // or DataSchema.LoadFromXml(this, path)
Register(export);
```
Shows: `EnableBacktesting = true` for historical export, `GetBasePath(this)` for the schema path,
generating a sample schema when the file is missing, and a set of UI parameters for the Export group.

## Key DataExport types
`Export`, `IndicatorExport`, `DrawingObjectsExport`, `ExportArgs`, `DataSchema`, `DataSet`,
`IndValue`, `ValueKind`, `ChartObjectDescriptor`, `MapItem`, `DrawingTool`,
`ExportTemporality` (Historical/Realtime), `ExportGranularity` (Bar/Tick),
`ExportDataSource` (Level1/Level2), `CalculateExportValueDelegate`.
Wiring up is always: create → `DataSet.Schema = ...` → `Register(export)` in `State.DataLoaded`.
