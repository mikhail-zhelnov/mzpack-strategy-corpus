# AGENTS.md — how to write a trading strategy on the MZpack API (NinjaTrader 8)

A guide for the AI agent (and developer). Read it in full before generating code.
This release targets **MZpack Strategies API 2.4.17** only. Do not use members introduced by a later API
version. The mechanics below are confirmed against the `API-2.4.17` product source and against real production
strategies (`FootprintAction`, `GhostResistance`).
Reference examples are in `samples/`; the skeleton is in `templates/StrategyTemplate/`.

## 0. Context
An MZpack strategy is a NinjaTrader 8 add-on (.NET Framework 4.8, x64). It compiles to a DLL and is
placed in `Documents\NinjaTrader 8\bin\Custom`. The API (`MZpack.NT8.Pro.dll`) is a separately
installed product. References to the API/platform are defined in `Directory.Build.props`;
they are not duplicated in the `.csproj`. Namespace = project name; one project = one strategy = one DLL.

> Product strategies live INSIDE the API repository under conditional compilation
> (`#if STRAT`) and in the namespace `NinjaTrader.NinjaScript.Strategies.MZpackStrategies`.
> Your own strategy is a separate standalone project WITHOUT these flags,
> namespace = project name. The API mechanics are the same; only the wrapper differs.

## 0a. Tech spec — a living document (mandatory)
Every strategy has a `SPEC.md` next to its code (the source of truth for requirements). Fill it in
during intake and UPDATE it as you go: when logic changes, edit `SPEC.md` first, then the code, and append
a line to the changelog. `SPEC.md` always reflects the current behavior of the strategy.
Tech spec template: `templates/SPEC.md`.

## 1. Anatomy of a strategy: host + algo + signals

### 1.1. Host class `: MZpackStrategyBase`  (file `<Name>.cs`)
The NinjaScript strategy visible in NinjaTrader. Configuration and UI only — NOT trading logic.
- UI parameters: properties with `[Display(Name, GroupName, Order, Description)]`; parameters
  configurable from NinjaScript additionally get `[NinjaScriptProperty]` (+ `[Range]` when needed).
  Group order — a series of `[CategoryOrder("Group", n)]` above the class.
- In the constructor, bind the delegates:
  ```csharp
  OnCreateAlgoStrategy = new OnCreateAlgoStrategyDelegate(CreateAlgoStrategy);
  OnCreateIndicators   = new OnCreateIndicatorsDelegate(CreateIndicators);
  ```
- `CreateAlgoStrategy()` — create and CONFIGURE the algo `Strategy` (see §3).
- `CreateIndicators()` — return a `List<TickIndicator>` (see §4).
- `OnStateChange()` — call `base.OnStateChange()`; in `State.Configure` assemble the `Entry[]`
  and call `Strategy.Initialize(...)` (see §3). Configure the indicators here as well,
  after `GetIndicator(NAME) as <Type>`.
- `OnBeforeSignalProbePass(SignalProbe)` — optional, only when a signal probe is used (see §3.1).

The engine calls the `OnCreateIndicators` and `OnCreateAlgoStrategy` delegates in `State.Configure`.

### 1.2. Algo class `: MZpack.NT8.Algo.Strategy`  (nested or `<Name>Strategy.cs`)
Execution. Override ONLY the hooks that are genuinely virtual (see §5). In product
strategies this is usually a nested class `class <Name>AlgoStrategy : Strategy`, in which
`OnPositionUpdate` (auto-pause after a trade) and/or `OnPositionOpenFilter`
(one logical entry per bar) are overridden.

### 1.3. Signals `: Signal`  (folder `Signals/`)
Recognition of the entry pattern — the core of your logic. One signal = one condition. See §2.

## 2. How to write a Signal (the canonical pattern)
```csharp
public class MySignal : Signal
{
    // Data source, calculation moment, hasPrice. The only form of the constructor.
    public MySignal(MZpack.NT8.Algo.Strategy strategy)
        : base(strategy, MarketDataSource.Level1, SignalCalculate.OnBarClose, true) { }

    public override void OnCalculate(MarketDataEventArgs e, int barIdx, SignalDirection allowed)
    {
        var strategy = (MyStrategy)Strategy.MZpackStrategy;       // cast to the host class
        SignalDirection direction = SignalDirection.None;

        // Current (just-closed) footprint bar:
        if (!strategy.FootprintIndicator.FootprintBars.TryGetValue(barIdx, out IFootprintBar bar))
            return;

        // (opt.) shared host bar filters:
        // if (!strategy.CheckBarFilters(bar, ...)) return;

        // ... logic over bar.Delta / bar.POC / bar.ImbalanceSRZones / ICandle ...
        // candles: strategy.GetCandle(1) — closed, GetCandle(2) — previous; .IsBullish()/.IsBearish()

        direction = ResolveDirection(SignalDirection.Long, allowed);   // (or Short / Any)

        if (IsDetermined(direction))   // ResolveDirection/IsDetermined are inherited — the Signal. prefix is optional
        {
            Direction   = direction;
            Time        = e.Time;
            EntryPrice  = GetBestEntryPrice(direction);   // Bid for Long, Ask for Short; or a custom price
            ChartRange  = new ChartRange { MinBarIdx = barIdx, MaxBarIdx = barIdx, Low = EntryPrice, High = EntryPrice };
            Description = $" => Delta = {bar.Delta}";      // optional, for the log/legend
        }
    }
}
```
Rules: start with `None`; an early `return` on every unmet condition; finish via
`IsDetermined(direction)` filling in `Direction/Time/EntryPrice/ChartRange` (+`Description`).
The engine resets the signal between calculations on its own (the `isReset` flag in the constructor).

### Signal constructor parameters
`base(strategy, MarketDataSource source, SignalCalculate calculate, bool hasPrice)`:
- `source` — almost always `Level1` (there are also `Level2`, `Custom`).
- `calculate` — `OnBarClose` or `OnEachTick`.
- `hasPrice` — whether the signal price participates in the decision tree.

### Hold your indicator, and configure what it must calculate
Take the indicator through the constructor and keep it in a field. Reaching for it through the host
(`((MyStrategy)Strategy.MZpackStrategy).SomeIndicator`) makes the signal unmovable between strategies.

Part of the indicator data is calculated **only when the matching setting is on** — footprint absorptions,
imbalance S/R zones, bar value area, session value area, delta rate and others. A signal reading such data
without enabling its calculation reads nothing on every bar and is indistinguishable from a signal that never
fires.

MZpack Strategies API 2.4.17 has no `DeclareRequirements()` / `Require(...)` capability API. Configure the
indicator directly in the host, during `State.Configure`, before `Strategy.Initialize(...)`:

```csharp
var footprint = GetIndicator(FOOTPRINT) as StrategyFootprintIndicator;
if (footprint != null)
{
    footprint.ShowAbsorption = true;       // required before reading bar.Absorptions
    // footprint.ShowImbalanceSRZones = true; // required before reading imbalance S/R zones
}
```

Rules:
- enable the setting for what the signal **reads**, not an unrelated display preference;
- configure the exact `StrategyFootprintIndicator` instance used by the strategy;
- data that is always calculated (`bar.Delta`, `bar.Volume`, the per-level rows, `bar.POC`,
  `bar.MinDelta`/`MaxDelta`, `session.POCs`) needs no extra configuration.

## 3. Assembling the strategy: Entry[] + Pattern + signals tree + Initialize
This is the core. SL/TP/trailing are DECLARATIVE via the `Entry` object, not via override methods.

In `CreateAlgoStrategy()` — create and configure the strategy:
```csharp
var strategy = new MyStrategy(@"MyStrategy v1.0", this)
{
    OppositePatternAction = OppositePatternAction.None,
    LogLevel = LogLevel, LogTarget = LogTarget, LogTime = LogTime
};
strategy.TradingTimes.Add(new TradingTime { Begin = ..., End = ... });
strategy.SessionBreak = ...;
strategy.RiskManagement = new RiskManagement(strategy)
{
    Currency = Currency.UsDollar, EntryName = ENTRY1,
    DailyLossLimitEnable = ..., DailyLossLimit = ...,
    DailyProfitLimitEnable = ..., DailyProfitLimit = ...,
    DailyTradesLimitEnable = ..., DailyTradesLimit = ...
};
return strategy;
```

In `OnStateChange()` at `State.Configure` — build the entries and initialize:
```csharp
Entry[] entries = new Entry[count];
entries[0] = new Entry(Strategy)
{
    EntryMethod       = EntryMethod.Limit,   // or .Market
    Quantity          = qty1,
    SignalName        = ENTRY1,
    StopLossTicks     = sl1,
    ProfitTargetTicks = tp1,
    IsBreakEven         = isBE,
    BreakEvenAfterTicks = beAfter,
    BreakEvenShiftTicks = beShift,
    Trail = isTrail ? new Trail(trailAfter, trailDistance, trailStep) : null
};
Strategy.Initialize(CreateEntryPattern(), null, entries);   // exitPattern = null
```

`Initialize` is the last thing a strategy calls: it also builds the signal probes and turns on whatever the
signals declared they need from their indicators. Nothing has to be called after it.

To observe signals as well as trade them, pass optional signal FACTORIES — see §3.1:
```csharp
Strategy.Initialize(CreateEntryPattern(), null, entries, 0, probeEnabled ? new Func<Signal>[]
{
    () => new MySignal(Strategy, footprintIndicator) { Name = "My signal" },
} : null);
```

The signals tree inside the pattern:
```csharp
Pattern CreateEntryPattern()
{
    var pattern = new Pattern(Strategy, Logic.And, null, false);
    pattern.AllowedDirection = position_Direction;            // allowed directions

    var conj = new LogicalNode(Logic.Conjunction);            // tree node
    pattern.Signals.Root.AddChild(conj);                      // NOT AddSignal — specifically AddChild
    conj.AddChild(new Signals.MySignal(Strategy) { Name = "Long",  Allowed = SignalDirection.Long });
    conj.AddChild(new Signals.MySignal(Strategy) { Name = "Short", Allowed = SignalDirection.Short });
    return pattern;
}
```
For several signals with configurable logic: a `Logic.And` root + `new LogicalNode(Logic.Or)`
subnodes; on an OR node you can set `Signals.MinValidatedCount = N`.

### 3.1. Signal probe (optional) — measuring signals instead of guessing
Inside the tree a signal is not measurable on its own: an `Or` node stops at the first determined signal, the
allowed direction narrows as the walk proceeds, and from outside only the node's aggregate is visible. A signal
switched off for trading produces nothing at all, so "would this signal have helped" cannot be answered without
first letting it trade.

The probe is a second, independent evaluation path — observation and statistics only, never trading. Every
signal is evaluated with `SignalDirection.Any`, without early exit and without direction resolution between
signals. Pass optional factories to `Initialize`:

```csharp
Strategy.Initialize(CreateEntryPattern(), null, entries, 0, probeEnabled ? new Func<Signal>[]
{
    () => new Signals.MySignal(Strategy, footprintIndicator) { Name = "My signal" },
    () => new Signals.OtherSignal(Strategy, footprintIndicator) { Name = "Other signal" },
} : null);
```

- **Factories, not instances.** `Initialize` rebuilds the probe on every call, and the probe demands instances
  of its own: the ones in the tree are stateful, and evaluating them twice would corrupt the pattern. Passing a
  signal that already belongs to a tree throws.
- **List every signal worth observing, including the ones switched off for trading.** Whether a signal also
  trades is worked out from the pattern and recorded as `ProbeEvent.IsInTree` — that is exactly what makes an
  observed-only signal measurable.
- **Set `Name` in the factory** — it is what shows up in the report.
- Results: `Strategy.SignalProbes` (one probe per pattern), each with `Events` and a `Diagnostics` summary
  worth printing in `State.Terminated`.
- A signal that never fired is flagged in `Diagnostics`: the usual cause is a missing capability declaration,
  not a quiet market.
- If the probe would touch shared state before the trading path does, override
  `protected internal virtual void OnBeforeSignalProbePass(SignalProbe)` on the host and settle it there.

## 4. Indicators (CreateIndicators) and bar data
Create them by name-constant and add them to the list; do the detailed configuration later in
`Configure` via `GetIndicator(NAME) as <Type>` (you may use `SaveSettings = true`).
Available: `StrategyFootprintIndicator` (the main one), `StrategyVolumeProfileIndicator`,
`StrategyBigTradeIndicator`. The return value is a `List<TickIndicator>`.

`IFootprintBar` (access: `FootprintIndicator.FootprintBars.TryGetValue(barIdx, out bar)`):
`POC`, `Delta`, `DeltaPercentage`, `DeltaChange`, `DeltaRate`, `MinDelta`, `MaxDelta`,
`Imbalances`, `Absorptions`, `ImbalanceSRZones.Zones[(int)TradeSide.Bid|Ask]`,
`AbsorptionSRZones.Zones[...]`, `BuyVolumes`, `SellVolumes`, `Volume`, `Lo`, `Hi`,
`RangeTicks`, `UnfinishedAuctionHigh/Low`, `PartiallyVisible`.
`StrategyBigTradeIndicator.Trades` → `ITrade { StartBarIdx, Side, POC }`.
Full map — `docs/api-surface.md`.

## 5. Virtual hooks of the algo Strategy (override ONLY these)
Genuinely virtual in the `Strategy.cs` source:
- `OnValidateEntryPatternFilter(object e, MarketDataSource source) -> bool` (default true)
- `OnValidateExitPatternFilter(object e, MarketDataSource source) -> bool`
- `OnPositionOpenFilter(DateTime time) -> bool`
- `OnOrderFilled(Order order)`
- `OnOrderUpdate(Order order, OrderState orderState)`            // requires a base call
- `OnPositionUpdate(Position position, double avgPrice, int qty, MarketPosition mp)` // requires a base call
- `OnRender(ChartControl cc, ChartScale cs)`

Do NOT override stop/target/trail with methods — they do not exist in the current API; use `Entry.*Ticks`/`Trail`.

Stop/target/trail/exit have TWO models — use the one that fits:
1. Declarative (simple fixed values) — fields of the `Entry` object:
   `StopLossTicks`, `ProfitTargetTicks`, `IsBreakEven`, `Trail = new Trail(...)`.
2. Custom logic — inherit the base extension classes and override the methods:
   - `class MyEntry : Entry` → `GetStopLossValue()`, `GetEntryPrice(double entry)`;
   - `class MyExit : ExitBase` → `CheckExit(MarketDataEventArgs e, Entry entry, MarketPosition mp, out string reason)`;
   - `class MyTrail : TrailBase` → `IsBeginTrailing(...)`, `IsTrailingStep(...)`, `GetTrailingPrice(...)`.
   Ready-made references: `samples/extensions/` (BarStopLossEntry, SignalStopLossEntry,
   FiboRetracementEntry, BarCloseTarget, BarHiLoTrail). You put them directly into `Entry[]`
   in place of the base `Entry`. This is NOT a deprecated style — overriding `GetStopLossValue()` in old
   projects was exactly inheritance from `Entry`, not a nonexistent `Strategy` method.

## 5a. Exporting data to CSV (optional)
MZpack can export indicator data and chart objects to CSV (namespace
`MZpack.NT8.Algo.DataExport`). Wire it up in `State.DataLoaded`: create the export object →
set `DataSet.Schema` → `Register(export)`.
- Indicator values: `new IndicatorExport(this, indicator, ExportDataSource.Level1,
  temporality, granularity, new ExportArgs {...})`; schema — `new DataSchema(export.DataSet)`
  + `v.Append(IndValue.POC/Delta/Volume/...)`.
- Chart objects: `new DrawingObjectsExport(this, new ExportArgs {...})`; schema —
  `schema.Append(name, ValueKind.Feature, new ChartObjectDescriptor { Script=..., Map=[...] })`
  or `DataSchema.LoadFromXml(this, path)`. For a historical export: `EnableBacktesting = true`.
References and the full list of types — `samples/export/`.

## 6. Build and deploy
- Build: `msbuild <Name>.csproj`; if `msbuild` is not on PATH (no Developer console) — fall back to
  `dotnet msbuild <Name>.csproj`. Paths/references come from `Directory.Build.props`.
- **NinjaTrader must be CLOSED during the build.** While it runs it holds the assemblies in
  `bin\Custom`; the copy fails and the build still reports success.
- After Build, the `DeployToNinjaTrader` target copies the DLL to `…\NinjaTrader 8\bin\Custom`.
  Disable it with: `-p:DeployToNinjaTrader=false` (CI, or building on a machine without NT8).
- Do not launch NinjaTrader.exe from the build.

## 7. Conventions
- Product strategy: namespace = `NinjaTrader.NinjaScript.Strategies.MZpackStrategies`.
- Your own strategy: **namespace = project name**, signals in `<Project>.Signals`;
  host class and assembly = `<StrategyName>`.
- Base template: folder/namespace/assembly = project name — rename all three when you copy it.
- Files: `<Name>.cs` (host), algo class nested or `<Name>Strategy.cs`, `Signals/*Signal.cs`.
- Signals — the `Signal` suffix. Entry labels — short constants (`static readonly string ENTRY1="T1.1";`).

## 8. What NOT to do
- Do not redistribute `MZpack.NT8.Pro.dll` or the NinjaTrader assemblies — they come with your own installation.
- Do not write absolute/deep relative HintPath in the .csproj — only through props.
- Do not use `EntrySignals.Add(...)`/`AddSignal(...)` — they do not exist; the tree is built with `Signals.Root.AddChild`.
- Stop/target methods are overrides on the `Entry`/`ExitBase`/`TrailBase` extensions (see `samples/extensions/`), not on `Strategy`. Simple SL/TP — declaratively via `Entry` fields.
- Do not put trading logic in the host; do not put UI parameters in a signal.
