# MZpack.NT8.Algo — API surface (from source + real usage)

Assembled from the source code of `MZpack.NT8\Algo` (Signal.cs, Strategy.cs, MZpackStrategyBase.cs,
Pattern/Entry/RiskManagement and the product strategies). API version: `MZpackStrategyBase.Version = "2.4.18"`.
Source: product release commit `185d67621238dd0ea1f08c1a358f7c84da0600d4` (2026-10-06).
See `release-2.4.18.md` for the changes from 2.4.17.

## Base classes and hierarchy
- `MZpackStrategyBase : StrategyRenderBase` — the NinjaScript host. No abstract members;
  everything is wired up with delegates. Attribute `[CategoryOrder("MZpack", 0)]`.
- `MZpack.NT8.Algo.Strategy : ViewModelBase` — the algo execution class.
- `Signal : Node` — the base signal.
- `Pattern`, `LogicalNode`, `Node` — the decision tree.
- `Entry`, `Trail`, `RiskManagement`, `TradingTime`, `ChartRange` — configuration.
- `TickIndicator` — the base indicator.

## MZpackStrategyBase (host)
Delegates (declared at the namespace level) and their properties:
```csharp
public delegate Algo.Strategy       OnCreateAlgoStrategyDelegate();
public delegate List<TickIndicator> OnCreateIndicatorsDelegate();
public delegate void OnTickDelegate(MarketDataEventArgs e, int currentBarIdx);
public delegate void OnMarketDepthDelegate(MarketDepthEventArgs e, int currentBarIdx);

public OnCreateAlgoStrategyDelegate OnCreateAlgoStrategy { get; set; }
public OnCreateIndicatorsDelegate   OnCreateIndicators   { get; set; }
public OnTickDelegate OnEachTickHandler { get; set; }
public OnTickDelegate OnBarCloseHandler { get; set; }
public OnMarketDepthDelegate OnMarketDepthHandler { get; set; }
```
Key members: `Strategy Strategy { get; protected set; }`,
`ObservableCollection<TickIndicator> Indicators`, `static readonly string Version`,
`static readonly int ATTEMPTS = 3`, series indices `WorkingDataSeriesIdx`,
`TradingDataSeriesIdx`, `PatternValidatingDataSeriesIdx`,
`ICandle GetCandle(int ago)` / `GetCandle(int ago, int dataseries)`,
`TickIndicator GetIndicator(string)` / `GetIndicator<T>(string)`,
`AddTickDataSeries()`, `AddCustomDataSeries(...)`,
price helpers `RoundToTickSize`, `PriceAddTicks`, `PriceDiffTicks`,
`DefaultAlert(...)`, `static string GetBasePath(MZpackStrategyBase)`.
Lifecycle: `OnStateChange()` (override with a `base` call) — stages
`SetDefaults → Configure → DataLoaded → Historical → Terminated`.
UI hooks (opt.): `CreateControlPanelElements()`, `ControlPanel_Attach/DetachEventHandlers()`.
Probe hooks on the host: `protected internal virtual void OnConfigureSignalProbe(SignalProbe)` and
`OnBeforeSignalProbePass(SignalProbe)`. A standalone assembly must use `protected override` for both.
Dashboard properties: `ShowPatternsDashboard`, `DashboardGridShowLegend`, `DashboardGridViewPosition`,
`DashboardGridViewOffset`, `DashboardGridViewRowHeight`, `DashboardTheme`, `DashboardShowSignalHint` and
serialized hidden `DashboardCollapsedNodes`. See `../templates/README.md`.

## Strategy (algo) — for overriding in a subclass
```csharp
public virtual bool OnValidateEntryPatternFilter(object e, MarketDataSource source); // default true
public virtual bool OnValidateExitPatternFilter(object e, MarketDataSource source);
public virtual bool OnPositionOpenFilter(DateTime time);                              // default true
public virtual void OnOrderFilled(Order order);
public virtual void OnOrderUpdate(Order order, OrderState orderState);                // base call required
public virtual void OnPositionUpdate(NinjaTrader.Cbi.Position position, double averagePrice,
                                     int quantity, MarketPosition marketPosition);    // base call required
public virtual void OnRender(ChartControl chartControl, ChartScale chartScale);
```
Initialization (called by the host, NOT overridden):
```csharp
void Initialize(Pattern entryPattern, Pattern exitPattern = null,
                Func<Signal>[] entryProbeSignals = null, Func<Signal>[] exitProbeSignals = null);
void Initialize(Pattern entryPattern, Pattern exitPattern, Entry[] entries,
                Func<Signal>[] entryProbeSignals = null, Func<Signal>[] exitProbeSignals = null);
void Initialize(Pattern entryPattern, Pattern exitPattern, Entry[] entries, int attempts,
                Func<Signal>[] entryProbeSignals = null, Func<Signal>[] exitProbeSignals = null);
void Initialize(Pattern entryPattern, Entry[] entries, int attempts,
                Func<Signal>[] entryProbeSignals = null);
```
`Initialize` also builds the signal probes and turns on whatever the signals declared they need from their
indicators — a strategy calls nothing after it. The probe parameters are optional; omit them and no probe is
built. See "Signal probe" below.
Properties: `Name`, `MZpackStrategy`, `bool SessionBreak=true`, `bool IsOpeningPositionEnabled=true`,
`bool[] TradingDays`, `List<TradingTime> TradingTimes`, `RiskManagement RiskManagement`,
`Patterns Patterns`, `Positions Positions`, `Pattern Pattern` (=Patterns.Get(Entry)),
`Pattern ExitPattern` (=Patterns.Get(Exit)), `OppositePatternAction OppositePatternAction`,
`bool IsUnmanaged`, `LogLevel`, `LogTarget`, `bool LogTime`.
Utilities: `int PriceToTicks(double)`, `double TicksToPrice(int)`, `bool IsTrade(DateTime, out string)`, `Log(...)`.
> There are NO virtual stop/target/trailing methods in Strategy.cs — they are declarative via `Entry`.

## Signal (base) — for overriding and filling in
Override: `OnCalculate(MarketDataEventArgs e, int barIdx, SignalDirection allowed)`
(overloads for `MarketDepthEventArgs` and `TimeEventArgs`).
Result members to fill in: `SignalDirection Direction`, `DateTime Time`, `double EntryPrice`,
`ChartRange ChartRange`, `string Description`.
Config (in the constructor / at creation): `Strategy Strategy`, `MarketDataSource MarketDataSource`,
`SignalCalculate Calculate`, `bool IsReset`, `bool HasPrice`, `int DataSeriesIndex`,
`SignalPartiallyVisibleMode PartiallyVisibleMode`, `string Name`, `List<Position> Positions`.
Helpers: `ResolveDirection(dir, allowed)`, `IsDetermined(dir)`,
`static SignalDirection MarketPositionToSignalDirection(MarketPosition)`,
`protected double GetBestEntryPrice(SignalDirection)` (Bid for Long, Ask for Short),
`protected double GetEntryPrice(object args, SignalDirection)`,
`protected int GetCurrentBarAgo()` / `GetCurrentBarAgo(int)`,
`bool CheckSessionLimitByIndex(int)` / `CheckSessionLimitByBarsAgo(int, int)`,
`bool IsMarketEventSupported(MarketDataSource)`.
Constructor: `Signal(Strategy strategy, MarketDataSource source, SignalCalculate calculate, bool isReset)`.

### Declaring required calculations (API 2.4.18)
```csharp
public virtual void DeclareRequirements(); // override in a custom Signal
protected void Require(IFootprintIndicator indicator, FootprintCapabilities caps);
protected void Require(IVolumeProfileIndicator indicator, VolumeProfileCapabilities caps);
protected void Require(IBigTradeIndicator indicator, BigTradeCapabilities caps);
protected void Require(IMarketDepthIndicator indicator, MarketDepthCapabilities caps);
protected void Require(IVolumeDeltaIndicator indicator, VolumeDeltaCapabilities caps);
```
Interfaces are in `MZpack`; capability enums are in `MZpack.NT8.Algo`. Requirements are merged per concrete
`TickIndicator` instance. A nonzero requirement against null or an unrelated implementation throws.
The framework calls the declaration after resolving the signal's indicator references, then applies all
requirements from entry/exit signal trees, filter trees and probe signals inside `Strategy.Initialize`.
```csharp
public override void DeclareRequirements()
{
    Require(footprint, FootprintCapabilities.Absorptions | FootprintCapabilities.BarValueArea);
}
```

| Enum | Available flags |
|---|---|
| `FootprintCapabilities` | `None`, `Imbalances`, `ImbalanceSRZones`, `Absorptions`, `AbsorptionSRZones`, `BarValueArea`, `BarMultiplePOC`, `UnfinishedAuction`, `RatioNumbers`, `AbsoluteDeltaAverage`, `DeltaRate`, `DeltaDivergence`, `SessionValueArea` |
| `VolumeProfileCapabilities` | `None`, `ProfileVWAP`, `ProfileVWAPDeviation` |
| `BigTradeCapabilities` | `None`, `Icebergs`, `Aggression` |
| `MarketDepthCapabilities` | `None`, `OverallLiquidity`, `LiquidityMigration` |
| `VolumeDeltaCapabilities` | `None` only; also covers Delta Divergence |

The applier only enables calculation or raises a minimum: `BarMultiplePOC` ensures `ShowBarPOCCount >= 2`;
`AbsorptionSRZones` implies `Absorptions`; `ImbalanceSRZones` does not turn on imbalance markers.
`ProfileVWAPDeviation` implies VWAP and upgrades `None`/`Dynamic` to `DynamicStdDev1`. An explicitly chosen
`VWAPMode.Last` is kept and produces a diagnostic; set a dynamic standard deviation mode in the host if
that signal needs deviation. Detection thresholds, algorithm choices and display filters remain host settings.

Unconditional data needs no requirement: `bar.Delta`, `bar.Volume`, per-level rows, `bar.POC`,
`bar.MinDelta`/`MaxDelta`, footprint `session.POCs`, volume-profile POC/VAH/VAL and the realtime order book.
Built-in signals without their own declaration require explicit host settings; see `release-2.4.18.md`.

## Signal probe — observing signals outside the pattern tree
An optional second evaluation path, for observation and statistics only; it never takes part in trading.
The tree stops walking an `Or` node at the first determined signal and narrows the allowed direction, so from
outside only the node's aggregate is visible. The probe evaluates every signal independently with
`SignalDirection.Any`.
```csharp
Strategy.Initialize(entryPattern, null, entries, 0, probeEnabled ? new Func<Signal>[]
{
    () => new MySignal(Strategy, footprint) { Name = "My signal" },
    // list every signal worth observing, including the ones switched off for trading
} : null);
```
Factories, not instances: `Initialize` rebuilds the probe on every call, and the probe requires signal
instances of its own — the ones in the tree are stateful and evaluating them twice would corrupt the pattern.
Whether a signal also trades is worked out from the pattern and recorded as `ProbeEvent.IsInTree`, so listing a
disabled signal is how you find out what it WOULD have given.
Results: `Strategy.SignalProbes` (one per pattern); `Events` and `Outcomes` are index-aligned read-only lists.
`ProbeEvent` carries `Time` (bar close), `EventTime` (market event), `BarIdx`, `SignalName`, `IsInTree`,
`Direction` (+1/-1), `Price`, bar statistics, ranks and session/profile context. Missing ranks/distances are
`NaN`. `IsInTree` matches runtime signal type in that pattern's signal/filter trees, not its name or settings.
`ProbeOutcome` exposes first-touch arrays `FavTime`/`AdvTime` and `FavBar`/`AdvBar`, `Step`, `Levels`,
`FirstTickPrice`, `TicksAtHorizon`, `MFE`, nonpositive `MAE`, `BarsToMFE`, `IsOpen` and `Censored`.
`ProbeCensored`: `None`, `SessionEnd`, `EndOfData`.

Configure in `OnConfigureSignalProbe`: `HorizonBars` (default 40), `LadderTicks` (0 = derive), optional
`CanRecord`, `ResetRankScope`, `VolumeRankOf`, `DeltaRankOf` and `UndeclaredAlternativesOf` callbacks.
Queries: `Diagnostics`, `IsEnabled`, `IsGated`, `SkippedCount`, `FirstRecordedSession`, `TouchedCount`,
`ResolvedLadderTicks`, `ResolvedLadderStep`, `ResolvedLadderLevels`, `GetEffectiveCurrentBarIndex()`.
Call `CloseOpenOutcomes()` before `BuildReport(int stopLossTicks, int profitTargetTicks, string headerInfo)`
at termination. `Clear()` discards observations. `ProbeDumpFormat.EventsHeader(levels, step)` and
`EventRow(ProbeEvent, ProbeOutcome)` format CSV; `ParamsHeader`, `WindowHeader`, `StrategyHeader`,
`FiltersHeader` define metadata sections but do not write a file.
See `signal-probe.md` for factory identity, routing, outcome semantics and report limits.

## Entry (declarative entry and per-trade risk)
```csharp
new Entry(Strategy) {
    EntryMethod = EntryMethod.Limit | EntryMethod.Market,
    Quantity, SignalName,
    StopLossTicks, ProfitTargetTicks,
    IsBreakEven, BreakEvenAfterTicks, BreakEvenShiftTicks,
    Trail = new Trail(after, distance, step)   // or null
}
```

## Pattern / signals tree
```csharp
var pattern = new Pattern(Strategy, Logic.And, null, false);
pattern.AllowedDirection = SignalDirection.Long | Short | Any;
var node = new LogicalNode(Logic.Conjunction | Logic.And | Logic.Or);
pattern.Signals.Root.AddChild(node);
node.AddChild(new MySignal(Strategy) { Name = "...", Allowed = SignalDirection.Long });
// on an OR node: node.Signals.MinValidatedCount = N;
```

## RiskManagement
```csharp
new RiskManagement(strategy) {
    Currency = Currency.UsDollar, EntryName,
    DailyLossLimitEnable, DailyLossLimit,
    DailyProfitLimitEnable, DailyProfitLimit,
    DailyTradesLimitEnable, DailyTradesLimit
}
```

## Enumerations
`SignalDirection`: None | Long | Short | Any.
`MarketDataSource`: Level1 | Level2 | Custom.
`SignalCalculate`: OnBarClose | OnEachTick | NotApplicable.
`Logic`: And | Or | Conjunction.
`EntryMethod`: Limit | Market.
`OppositePatternAction`: None | …
`Currency`: UsDollar | …
`TradeSide`: Bid | Ask.
`LogLevel` / `LogTarget`.

## Indicators and data
- `StrategyFootprintIndicator` → `.FootprintBars.TryGetValue(barIdx, out IFootprintBar bar)`.
- `StrategyVolumeProfileIndicator` → `.Profiles`; `StrategyBigTradeIndicator` → `.Trades` (`ITrade`).
- `StrategyBigTradeIndicator.DomPressureSignaturePassesFilter(ITrade)` gates on the current DOM-pressure
  signature filters. Export the same result as `IndValue.DomPressurePassesFilter` (1/0).
- `StrategyMarketDepthIndicator` / `IMarketDepthIndicator.TicksPerLevel`: aggregation changes the keys and
  ranges of historical `Blocks`; `RealtimeBids` / `RealtimeOffers` retain per-tick prices. See `release-2.4.18.md`.
- `ICandle` (via `host.GetCandle(ago)`): `IsBullish()`, `IsBearish()`, `Low`, `High`, `LowerBody`, `UpperBody`.
- `IFootprintBar`: POC, Delta, DeltaPercentage, DeltaChange, DeltaRate, MinDelta, MaxDelta,
  Imbalances, Absorptions, ImbalanceSRZones, AbsorptionSRZones, BuyVolumes, SellVolumes,
  Volume, Lo, Hi, RangeTicks, UnfinishedAuctionHigh/Low, PartiallyVisible.

## Extensions: Entry / Exit / Trail (custom SL/TP/exit/trail logic)
Inherit and override (references — `samples/extensions/`):
- `Entry : EntryBase` — `double GetStopLossValue()`, `double GetEntryPrice(double entry)`.
  Properties: `Quantity`, `EntryMethod` (Market|Limit|StopLimit), `SignalName`,
  `StopLossCalculationMode`, `StopLossPrice`, `ProfitTargetTicks`, `Strategy`, `Position`.
- `ExitBase` — `bool CheckExit(MarketDataEventArgs e, Entry entry, MarketPosition mp, out string reason)`;
  property `Calculate`.
- `TrailBase` — `bool IsBeginTrailing(MarketPosition, Entry)`, `bool IsTrailingStep(MarketPosition, Entry)`,
  `double GetTrailingPrice(MarketPosition, Entry)`; property `TrailingPrice`.
Instrument helpers: `MZpackStrategy.GetCurrentBid()/GetCurrentAsk()`,
`Instrument.MasterInstrument.RoundToTickSize(p)`, `Strategy.TicksToPrice(ticks)`,
`MZpackStrategy.High[ago]/Low[ago]`, `MZpackStrategy.BarsSinceEntryExecution(signalName)`.

## DataExport (namespace MZpack.NT8.Algo.DataExport)
Wire it up in `State.DataLoaded`: create → `DataSet.Schema = ...` → `Register(export)`.
- `IndicatorExport(this, indicator, ExportDataSource, ExportTemporality, ExportGranularity, ExportArgs)`
  — indicator values; schema `new DataSchema(ds)` + `Append(IndValue.X)`.
- `DrawingObjectsExport(this, ExportArgs)` — chart objects; schema `Append(name, ValueKind.Feature,
  new ChartObjectDescriptor{ Script, Map=List<MapItem>{ Tool=DrawingTool.*, Text, Value } })`
  or `DataSchema.LoadFromXml(this, path)`.
Types: `Export`, `ExportArgs`, `DataSchema`, `DataSet`, `IndValue`, `ValueKind`,
`ChartObjectDescriptor`, `MapItem`, `DrawingTool`, `ExportTemporality` (Historical|Realtime),
`ExportGranularity` (Bar|Tick|Update), `ExportDataSource` (Level1|Level2|Custom), `CalculateExportValueDelegate`.
`ExportArgs.IsExportWhileCollecting` writes rows during collection; set `FlushIntervalMs` (0 = each row)
for flush batching. For live appending use `ExportTemporality.Realtime`. The writer permits concurrent
readers, initializes the file once per run and retains collected rows in `DataSet`.
`IsBatch` separates run files. Historical exports finish at `State.Transition`; realtime exports finish at
`State.Terminated`. See `../samples/export/README.md` for the two sample hosts.
Other: `EnableBacktesting=true` (historical export), `GetBasePath(this)` (base path for files).

## Released built-in signals
`MZpack.NT8.Algo.Signals` includes `FootprintImbalanceSignal`, `FootprintAbsoprtionSignal` (public spelling),
`BigTradeSignal`, `RelativeToProfileSignal` and `DOMImbalanceSignal`. These are API classes distinct from
similarly named product-strategy snapshots in `samples/`. Constructors and required settings:
`release-2.4.18.md`.

## Filter calibration
`FilterCalibration` and `CalibrationBar` / `CalibrationBucket` / `CalibrationSession` are in
`MZpack.NT8.Algo`. Constructor: `(int baselineSessions, double percentile, IntradayBuckets buckets,
double sessionOutlierTolerance)`. `IntradayBuckets`: `Off`, `Minutes60`, `Minutes30`.
`Build(IEnumerable<CalibrationBar>, IEnumerable<TradingTime>, DateTime currentSessionBegin, string headerInfo)`
uses completed prior sessions only. Query `IsReady`, `IsWarmingUp`, `NotReadyReason`, `GetMinVolume(time)` /
`GetMinDelta(time)` (also overloads with percentile), `GetVolumeRank(time, volume)` / `GetDeltaRank(time, delta)`.
Ranks return `NaN` when unavailable. Full input contract and readiness rules: `filter-calibration.md`.

> The source of truth is the matching installed `MZpack.NT8.Pro.dll` and the immutable product source ref
> in `skill-manifest.json`. For full signatures, see the corresponding .cs in `MZpack.NT8\Algo` at that ref.
