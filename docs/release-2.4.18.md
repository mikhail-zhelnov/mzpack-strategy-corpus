# MZpack Strategies API 2.4.18 — builder changes

Source baseline: API 2.4.18, changelog dated **2026-10-06**, product commit
`185d67621238dd0ea1f08c1a358f7c84da0600d4`. The member names, constructors
and behavior below were checked against `MZpack.NT8` and `MZpackBase` at that
commit. This corpus targets this release only.

The release adds streaming CSV export, five included built-in signals,
a DOM-pressure filter gate and an interactive pattern dashboard. See
[signal probes](signal-probe.md) for independent signal observation and outcomes,
[filter calibration](filter-calibration.md) for the Footprint Action baseline,
and [the strategy guide](../AGENTS.md) / [API surface](api-surface.md) for
per-instance indicator capability declarations.

## Included built-in signals

The public namespace is `MZpack.NT8.Algo.Signals`; strategy indicator wrappers
are in `MZpack.NT8.Algo.Indicators`. The following constructors take the algo
`MZpack.NT8.Algo.Strategy`, not the NinjaScript host:

| Type | Constructors for direct use | Calculation and settings |
|---|---|---|
| `FootprintImbalanceSignal` | `(Strategy, StrategyFootprintIndicator)`; `(Strategy, StrategyFootprintIndicator, SignalCalculate)` | Level1; default `OnEachTick`. Set `MinCount` (for example, 1). Buy/Ask imbalances give Long; Sell/Bid give Short. |
| `FootprintAbsoprtionSignal` | `(Strategy, StrategyFootprintIndicator)`; `(Strategy, StrategyFootprintIndicator, SignalCalculate)` | Level1; default `OnEachTick`. Set `MinCount` (for example, 1). Sell/Bid absorptions give Long; Buy/Ask give Short. |
| `BigTradeSignal` | `(Strategy, StrategyBigTradeIndicator)` | Level1, `OnEachTick`. A new filtered trade gives Long for Bid/Sell and Short for Ask/Buy; `IsInverted = true` reverses it. |
| `RelativeToProfileSignal` | `(Strategy, IVolumeProfileIndicator)` | Level1, `OnBarClose`. Set `RelativeToProfile` (`VA` or `VWAP`), `VWAPType`, `IsReversed`, `IsAnyWhenOutside` as needed. |
| `DOMImbalanceSignal` | `(Strategy, StrategyMarketDepthIndicator)` | Level2, `NotApplicable`. Set `Ratio`; offer/bid at least the ratio gives Short, bid/offer at least it gives Long. |

`FootprintAbsoprtionSignal` is the exact public spelling. Do not rename it to
`FootprintAbsorptionSignal`. The footprint count signals return no direction
when both sides have nonzero counts.

These five signals do **not** override `DeclareRequirements()`. Configure the
exact indicator instance directly in `State.Configure` before
`Strategy.Initialize(...)`:

```csharp
footprintIndicator.ShowImbalance = true;   // FootprintImbalanceSignal
footprintIndicator.ShowAbsorption = true;  // FootprintAbsoprtionSignal
```

For `RelativeToProfileSignal`, profile VAH/VAL need no gate. VWAP needs the
indicator's `VWAPMode` enabled; deviation bands need `DynamicStdDev1` or
`DynamicStdDev2` and the appropriate `Sigma1`/`Sigma2`. Also set the signal's
`VWAPType` explicitly: its default is `None`.

The profile signal uses the latest profile. It normally gives Short above VAH,
VWAP or the positive deviation band and Long below VAL, VWAP or the negative
band; `IsReversed` reverses that direction. `IsAnyWhenOutside` preserves the
incoming allowed direction outside VA or a deviation band. It does not change
the plain `Dynamic`/`Last` VWAP comparison.

`BigTradeSignal` reads the indicator's filtered `Trades` collection. The realtime
order book used by `DOMImbalanceSignal` is always maintained; enabling the
display flag `ShowRealtimeOrderBook` is not a calculation prerequisite.
`DOMImbalanceSignal.PercentageToRatio(100)` yields `2`; its inverse is
`RatioToPercentage(2)` yielding `100`.

Configure `IsReset` and `HasPrice` explicitly for the intended pattern.
The fourth parameter of the base `Signal` constructor is `isReset`.
The footprint count constructors set `HasPrice = true`; the other three direct
constructors leave it false. `RelativeToProfileSignal` defaults to resetting,
while the other four default to retaining state until reset. Set `HasPrice = true`
on `BigTradeSignal` to use the trade's POC as its entry price.

For example, add a bar-close imbalance signal to an existing pattern root:

```csharp
footprintIndicator.ShowImbalance = true;
pattern.Signals.Root.AddChild(new MZpack.NT8.Algo.Signals.FootprintImbalanceSignal(
    Strategy, footprintIndicator, SignalCalculate.OnBarClose)
{
    Name = "Footprint imbalance",
    MinCount = 1,
    IsReset = true
});
```

## DOM-pressure gate and CSV field

`StrategyBigTradeIndicator` inherits the public method
`bool DomPressureSignaturePassesFilter(ITrade trade)` from `mzBigTrade`.
It evaluates the trade's current DOM-pressure signature using:

- `DomPressureFilterEnable`;
- absolute pressure volume converted from internal units against
  `DomPressureFilterMin` / `DomPressureFilterMax`;
- `RefillMinHoldSec`, `RefillMinIntensity`, `RefillMaxIntensity`,
  and `RefillMinTradedSig`.

It returns false for a null trade, a disabled pressure filter or zero pressure.
A nonzero `trade.DomPressureVolume` alone does not establish that the signature
passes the current filters. Apply the gate to a trade supplied by this indicator:

```csharp
ITrade trade = bigTradeIndicator.LastTrade;
if (!bigTradeIndicator.DomPressureSignaturePassesFilter(trade))
    return;
```

The DataExport namespace is `MZpack.NT8.Algo.DataExport`.
`IndValue.DomPressurePassesFilter` exports this result as `1` or `0` through
`StrategyBigTradeIndicator`; `IndValue.DomPressureVolume` remains the pressure
volume field. Add the new field to a custom BigTrade schema with
`schema.Append(IndValue.DomPressurePassesFilter)`. The copied `Data_Export`
sample has a volume setting but no UI setting for the new gate field.

## Streaming export

Register exports in `State.DataLoaded` after assigning `DataSet.Schema`.
See [samples/export](../samples/export/README.md) and its canonical
[Data_Export.cs](../samples/export/Data_Export.cs).

| Setting | Behavior |
|---|---|
| `ExportArgs.IsExportWhileCollecting = true` | Append collected rows while that export's temporality is active. Default is false. |
| `ExportTemporality.Realtime` | Collect live rows. `Historical` collects during historical loading, even with streaming enabled. |
| `ExportArgs.FlushIntervalMs` | `0` (default) flushes every row. A positive interval batches flushes; the DOM sample uses 250 ms. |
| `ExportArgs.IsBatch = true` | Create a separate numbered file per run; the suffix is applied once. |
| `Export.IsStreamFailed` | True after a live write failure; collected rows are retained for the final dataset write. |

The built-in strategy has independent `Export real-time` switches in Footprint,
VolumeProfile, BigTrade and MarketDepth, all off by default. Footprint,
VolumeProfile and BigTrade also require `Temporality = Realtime` for live output;
they default to `Historical`. MarketDepth uses `Level2` / `Realtime` / `Update`
and writes at DOM-update frequency.

The persistent writer resolves the path/header and truncates the selected file
once when opened, then appends. It uses `FileShare.ReadWrite`. Historical streams
open at `State.Historical`, Realtime streams at `State.Transition`; termination
flushes and closes writers. With streaming off, the historical dataset is saved
at Transition and the realtime dataset at Terminated.

A live file-write failure is reported once and does not escape into the market
data handler. The export keeps collecting and attempts the final dataset write
when that temporality ends. An error routing data stops that export only; the
other exports continue. `DrawingObjectsExport` now shares the same streaming
path, including target directory, file name and header initialization.

## Pattern dashboard

The existing `DashboardTemplate` still enables the built-in dashboard in
`State.SetDefaults`; custom rendering is unnecessary.

| Host property | Purpose / default |
|---|---|
| `ShowPatternsDashboard` | Enable the dashboard; false by default. |
| `DashboardGridShowLegend` | Show the legend/tree; false by default. |
| `DashboardGridViewPosition` | `Top` or `Bottom`; Bottom by default. |
| `DashboardGridViewOffset` | Distance from the docked edge, 0 by default. Rendering uses the absolute value and derives the sign from docking. |
| `DashboardGridViewRowHeight` | Row height in pixels, default 24; UI range 10..100. |
| `DashboardTheme` | `Dark` (default) or `Light`. |
| `DashboardShowSignalHint` | Show signal hints; true by default. |
| `DashboardCollapsedNodes` | Hidden serialized string of structural paths separated by `;`, empty by default. |

A compact header provides panel collapse/expand, tree visibility, docking,
theme and signal-hint controls. Its Layout overlay adjusts offset and row
height. Movement via the arrows is clamped to the available chart height;
row-height buttons stay within 10..100. Collapsing the panel leaves its header
available so it can be expanded again.

Clicking a legend node with children collapses/expands its subtree in both the
legend and grid. The collapsed node retains its own aggregate direction cell.
`Pattern`, `Signals`, `Filters`, logical nodes and nodes with children can
collapse; `DashboardCollapsedNodes` preserves this state across strategy
reinitialization and template serialization. Logical nodes now have their own
grid cells alongside signals. A `None` direction is an empty cell; an uncalculated signal shows `-`.

Set a meaningful `Name` on each signal for the legend. Legend widths are measured
from text, low-height rows retain their text, and Range/indicator-template nodes
are omitted from the legend. Dashboard controls can be used directly alongside
the optional NinjaScript Control Panel properties.

The template's `Top` / `-30` offset is valid: it renders 30 px below the top.
Changing it to a positive 30 has the same rendered distance; button movement
stores the sign appropriate to the current dock.

## MarketDepth price aggregation

`IMarketDepthIndicator.TicksPerLevel` groups consecutive DOM price levels; 1
means no aggregation. Historical blocks carry a zone's total volume and are
keyed by the zone low for Bid and the zone high for Ask. When reading
`MarketDepthBlockDescriptor`, use `GetZone(out low, out high)` for the bounds;
`Price` is its key, not necessarily a single raw tick level. The bounds are
clipped at the current spread and depth window.

The realtime `RealtimeBids` / `RealtimeOffers` dictionaries keep per-tick
prices. Code reading historical `Blocks` must account for aggregation rather
than looking up each raw price as though every block were one tick wide.
Changing `TicksPerLevel` clears blocks built on the previous zone grid.

## Source evidence

In the product source at the baseline commit:

- included signal files: `MZpack.NT8/Algo/Signals/Built-in/` and compile entries
  in `MZpack.NT8/MZpack.NT8.csproj`;
- base constructor/defaults: `MZpack.NT8/Algo/Signal.cs`;
- gate: `MZpack.NT8/mzBigTrade/mzBigTrade.cs`;
- gate export: `MZpack.NT8/Algo/Indicators/StrategyBigTradeIndicator.cs`
  and `MZpack.NT8/Algo/DataExport/IndValue.cs`;
- streaming: `MZpack.NT8/Algo/DataExport/ExportArgs.cs`, `Export.cs`, `Exports.cs`;
- canonical export: `MZpack.NT8/Algo/Strategies/Data_Export/Data_Export.cs`;
- DOM aggregation: `MZpackBase/mzMarketDepth/IMarketDepthIndicator.cs`,
  `MarketDepthBaseMVC.cs` and `MarketDepthBlockDescriptor.cs`;
- dashboard properties/control/layout:
  `MZpack.NT8/Algo/MZpackStrategyBase.cs`,
  `MZpack.NT8/Algo/View/DashboardView.cs` and `DashboardControlsView.cs`.

The dashboard source uses a compact header and a Layout overlay. The changelog's
earlier description of a strip along the left edge is not the final layout at
the baseline commit; this guide follows the source.
