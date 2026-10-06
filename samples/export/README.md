# export/ — exporting data and chart objects to CSV

MZpack can export strategy/indicator data to CSV for analysis/ML.
API namespace: `MZpack.NT8.Algo.DataExport`. General scheme:
create an export object → set `DataSet.Schema` → `Register(export)` in `State.DataLoaded`.

## Two kinds of export

### 1. IndicatorExport — indicator values (canonical: [Data_Export.cs](Data_Export.cs))
This API 2.4.18 reference exports Footprint, VolumeProfile, BigTrade and MarketDepth.
Select the relevant export/schema blocks when building your own strategy:
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

## Streaming CSV in API 2.4.18

In the built-in `Data_Export` strategy, enable the indicator group's `Export` and
`Export real-time` options. For Footprint, VolumeProfile and BigTrade also set
`Temporality = Realtime`; their default is `Historical`, which collects during
historical loading. The MarketDepth export is already `Realtime` with
`ExportDataSource.Level2` and `ExportGranularity.Update`. Each group's streaming
switch is independent and defaults to `false`.

For a custom strategy, the same API configuration in `State.DataLoaded` is:

```csharp
var export = new IndicatorExport(this, footprintIndicator,
    ExportDataSource.Level1, ExportTemporality.Realtime, ExportGranularity.Bar,
    new ExportArgs
    {
        IsExportWhileCollecting = true,
        FlushIntervalMs = 0,
        IsHeader = true,
        IsTime = true,
        IsFile = true,
        FileName = "footprint.csv",
        Shift = -1  // export the closed footprint bar
    });
var schema = new DataSchema(export.DataSet);
schema.Append(IndValue.Delta);
schema.Append(IndValue.Volume);
export.DataSet.Schema = schema;
Register(export);
```

- `FlushIntervalMs = 0` flushes every row. The canonical MarketDepth export uses
  `250` ms to reduce writes at DOM-update rates; rows become visible at a flush.
- The streaming writer opens during its temporality's initialization and resolves
  the path/header once. It truncates the selected file once, then appends rows.
  Set `IsBatch = true` for a separate numbered file per run.
- With streaming off, Historical exports are written at `State.Transition`,
  Realtime exports at `State.Terminated`. Streaming Historical exports still write
  during historical loading; the checkbox alone does not select live data.
- The writer uses `FileShare.ReadWrite`. A live file-write failure is reported once
  and sets `Export.IsStreamFailed`; rows continue collecting for a final dataset
  write at the end of the export's temporality. A routing error stops only the
  affected export, and the other registered exports continue.
- `DrawingObjectsExport` uses the same streaming path, including path/header setup.

For a custom BigTrade schema, `schema.Append(IndValue.DomPressurePassesFilter)`
exports `1` or `0` from the indicator's current DOM-pressure filter gate.
`DomPressureVolume` exports pressure volume (with `SignedVolume` applied); it is a different field. The canonical
`Data_Export` sample exposes the volume field but does not add a UI switch for this
new gate field. See [API 2.4.18 changes](../../docs/release-2.4.18.md).

## Key DataExport types
`Export`, `IndicatorExport`, `DrawingObjectsExport`, `ExportArgs`, `DataSchema`, `DataSet`,
`IndValue`, `ValueKind`, `ChartObjectDescriptor`, `MapItem`, `DrawingTool`,
`ExportTemporality` (Historical/Realtime), `ExportGranularity` (Bar/Tick/Update),
`ExportDataSource` (Level1/Level2/Custom), `CalculateExportValueDelegate`.
Level2 exports require `Update`; Level1 exports cannot use `Update`.
Wiring up is always: create → `DataSet.Schema = ...` → `Register(export)` in `State.DataLoaded`.
