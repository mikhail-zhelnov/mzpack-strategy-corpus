# MZpack.NT8.Algo — API surface (from source + real usage)

Assembled from the source code of `MZpack.NT8\Algo` (Signal.cs, Strategy.cs, MZpackStrategyBase.cs,
Pattern/Entry/RiskManagement and the product strategies). API version in the code: `MZpackStrategyBase.Version = "2.4.17"`.

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

Declaring what the indicator must calculate:
```csharp
public virtual void DeclareRequirements();                                   // override, call Require(...)
protected void Require(IFootprintIndicator indicator, FootprintCapabilities caps);
protected void Require(IVolumeProfileIndicator indicator, VolumeProfileCapabilities caps);
protected void Require(IBigTradeIndicator indicator, BigTradeCapabilities caps);
protected void Require(IMarketDepthIndicator indicator, MarketDepthCapabilities caps);
protected void Require(IVolumeDeltaIndicator indicator, VolumeDeltaCapabilities caps);
```
Some indicator data is calculated only when the matching setting is on — footprint absorptions, imbalance S/R
zones, bar value area, session value area, delta rate and so on. A signal that reads such data MUST declare it,
otherwise it reads nothing and looks like a signal that never fires. Declare what you READ, not the setting you
want; the strategy unions the declarations of all its signals and turns the settings ON, never off. Requirements
belong to an INSTANCE, not to a type — a strategy may own more than one footprint. Data that is always
calculated (`bar.Delta`, `bar.Volume`, the per-level rows, `bar.POC`, `bar.MinDelta`/`MaxDelta`, `session.POCs`)
needs no declaration.
```csharp
readonly StrategyFootprintIndicator footprint;

public MySignal(Strategy strategy, StrategyFootprintIndicator footprint)
    : base(strategy, MarketDataSource.Level1, SignalCalculate.OnBarClose, true) { this.footprint = footprint; }

public override void DeclareRequirements() => Require(footprint, FootprintCapabilities.Absorptions);
```
> Hold the indicator in a field injected through the constructor. Reaching for it through the host strategy
> (`((MyStrategy)Strategy.MZpackStrategy).SomeIndicator`) makes the signal unmovable and leaves its
> requirements unattributable to an instance.

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
Results: `Strategy.SignalProbes` (one per pattern), each with `IReadOnlyList<ProbeEvent> Events` and a
`Diagnostics` summary. Hook: `protected internal virtual void OnBeforeSignalProbePass(SignalProbe)` on
`MZpackStrategyBase` — override to settle shared state the probe would otherwise touch first.

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
- `StrategyVolumeProfileIndicator`, `StrategyBigTradeIndicator` → `.Trades` (`ITrade`).
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
`ExportGranularity` (Bar|Tick), `ExportDataSource` (Level1|Level2), `CalculateExportValueDelegate`.
Other: `EnableBacktesting=true` (historical export), `GetBasePath(this)` (base path for files).

> The source of truth is `MZpack.NT8.Pro.dll` and the source in `…\source\repos\mzpack`. For full
> signatures of the missing types, see the corresponding .cs in `MZpack.NT8\Algo`.
