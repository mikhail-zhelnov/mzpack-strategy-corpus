# Signal probe — API 2.4.18

The probe observes signals independently of a pattern's trading decision. It creates no orders and does not
change the trading tree's direction. A firing is an observation, not a filled trade or a trading result.

Verified source: product commit `185d67621238dd0ea1f08c1a358f7c84da0600d4` (API 2.4.18, 2026-10-06),
`MZpack.NT8/Algo/Strategy.cs`, `SignalProbe.cs`, `Signal.cs`, `MZpackStrategyBase.cs`, and
`Algo/Strategies/FootprintAction/FootprintAction.cs`. The APIs below are in `MZpack.NT8.Algo`.

## Wiring a probe into a standalone strategy

After configuring the indicator and building the entries in the host's `State.Configure` block, pass fresh
signal factories to `Initialize`. This fragment assumes the host already has `footprint`, `entries`, and
`CreateEntryPattern()`:

```csharp
Strategy.Initialize(CreateEntryPattern(), null, entries, 0, new Func<Signal>[]
{
    () => new MySignal(Strategy, footprint) { Name = "My signal" },
    () => new OtherSignal(Strategy, footprint) { Name = "Other signal" }
});
```

Both signals here are examples supplied by the consumer. Include signals worth observing even when their
types are absent from the trading tree. A signal still needs `IsEnabled = true` to be evaluated.

`Initialize` accepts optional `entryProbeSignals` and `exitProbeSignals` arrays on its pattern overloads.
An array builds a probe only when its corresponding pattern exists and the array is nonempty. Null factories
and null results are skipped. Each call rebuilds `Strategy.SignalProbes`, then applies the union of indicator
requirements from the trading trees and the probes. Declare gated inputs in each signal's
`DeclareRequirements()`; see [API surface](api-surface.md).

Every factory must construct a separate signal instance. `SignalProbe.Add` rejects an instance whose `Tree`
or `Parent` is already set. Probe signals receive their indicators through their constructors; `Add` calls
`DeclareRequirements()` directly and does not call `ReferIndicators()` or `Signal.Initialize()`.

Configure a newly built probe through this host hook. For a standalone assembly, the override is
`protected`, although the API declares the base member `protected internal`:

```csharp
protected override void OnConfigureSignalProbe(SignalProbe probe)
{
    probe.HorizonBars = 40;
    probe.LadderTicks = 0; // derive the reach from the validating series
}
```

`protected override void OnBeforeSignalProbePass(SignalProbe probe)` is another optional host hook. It runs
before the probe evaluates any signals and can settle shared calibration or other state. Use
`probe.GetEffectiveCurrentBarIndex()` to obtain the first enabled probe signal's effective bar index; it
returns -1 when none is available. Do not assume it describes all signals if their series or calculation
moments differ.

Historical collection still requires the host's historical processing to be enabled (`EnableBacktesting`)
and suitable loaded data. Probe factories alone do not turn historical processing on. Tick Replay supplies
the tick sequence needed for historical first-touch outcomes; OHLC bars cannot reconstruct that sequence.

## Evaluation and identity

The automatic API 2.4.18 path feeds probes from `Strategy.OnMarketData` with `MarketDataSource.Level1`, after
the trading tree. It runs outside `Positions.HasFlat()` and `OnValidateEntryPatternFilter`, so observations
continue while a position is open. It does not reproduce those trading gates.

The probe checks each signal's `IsEnabled` and supported source, then uses `Signal.OnMarketEvent` with
`SignalDirection.Any`. The signal's calculation and data-series gates still apply. There is no OR-node
short-circuit or direction resolution between probe signals. Each pass forcibly resets probe signals,
including ones with `IsReset = false`; validated-signal alerts are suppressed during the pass and restored
afterwards. A thrown signal exception can interrupt the pass; the framework catches a failed probe pass so
it does not escape into the trading path.

The public `OnMarketEvent(object, MarketDataSource, bool)` can accept other sources, but automatic probe
routing does not feed Level2 or Custom in this release. Do not promise those observations merely because a
signal uses those sources. Avoid manually routing Level1 again and duplicating the framework's pass.

`ProbeEvent.IsInTree` means that a signal of the **exact same runtime type** was found in the corresponding
pattern's signals tree or filters tree when `Initialize` built the probe. It does not compare instances,
names, parameters, `Allowed`, or `IsEnabled`; two differently configured signals of the same type still
match. It is metadata, not an evaluation gate. For a direct `SignalProbe.Add(signal, isInTree)` call, the
caller supplies that value instead.

Names have a separate role: events use `signal.GetDisplayName()`, and the report merges signals sharing the
same display name into one row. Assign distinct names to configurations that must remain separate; a report
row's tree flag comes from its first roster entry.

## Recording gates and ranks

`CanRecord` is an optional `Func<bool>`, queried for each determined firing **after evaluation**. False
increments the total and per-signal skipped counters, then returns before creating an event or outcome.
It does not skip the signal calculation or prevent the other eligible signals from being evaluated.
Null records all determined firings. `IsGated` reports whether a predicate was supplied;
`SkippedCount` counts refused firings and `FirstRecordedSession` identifies the first recorded event with
session context, or is `default(DateTime)` if there is none.

For an auto-calibrated host, use a predicate reading current state, for example:

```csharp
probe.CanRecord = () => !filtersAuto || (calibration != null && calibration.IsReady);
```

Here `filtersAuto` and `calibration` are consumer-owned fields. Keeping the auto-off branch matters:
calibration need not become ready when auto calibration is disabled. A generic predicate may use another
condition, but the built-in diagnostic/report text describes skipped events as calibration warm-up. If
using another gate, provide your own explanation alongside that output.

`VolumeRank` and `DeltaRank` are read from optional callbacks at recording time. Without callbacks they are
`double.NaN`, exported as `n/a`. The probe does not compute calibration ranks automatically.

| Callback | Consumer responsibility |
| --- | --- |
| `ResetRankScope : System.Action` | Start a fresh accumulator before each eligible signal evaluation. |
| `VolumeRankOf`, `DeltaRankOf : Func<double>` | Return the completed firing's rank, not an unrelated current bar's rank. |
| `UndeclaredAlternativesOf : Func<bool>` | Mark a firing whose alternative branches were not represented correctly. |

For a multi-bar AND condition, the weakest rank binds; alternative OR branches need the strongest valid
branch, rather than a minimum over unrelated alternatives. Absolute manual filters do not move with a
calibration percentile. The rank accumulation helpers used by FootprintAction belong to that host, not to
the generic `Signal` class. See [filter calibration](filter-calibration.md) for rank query contracts.
`UndeclaredAlternativeSignals()` returns the flagged display names joined by `|`.

## Events and outcomes

`Events : IReadOnlyList<ProbeEvent>` and `Outcomes : IReadOnlyList<ProbeOutcome>` are index aligned. Events
follow processing order; firings within one market event follow factory order. The records contain:

| Event member | Meaning |
| --- | --- |
| `Time` | Footprint bar close time; `default(DateTime)` without a usable bar. |
| `EventTime` | Market event time (`signal.CalculatedTime`). For bar-close signals it is usually the first tick of the next bar. |
| `BarIdx`, `SignalName`, `IsInTree` | Effective signal bar index, display name, and type-based tree metadata. |
| `Direction`, `Price` | 1 for Long, -1 for Short; the signal's own `EntryPrice`. |
| `BarVolume`, `BarDelta`, `BarDeltaPercent` | Context from the footprint bar, not order-fill information. |
| `VolumeRank`, `DeltaRank` | Callback results for the firing; `NaN` when unknown. |
| `SessionBegin`, `BarInSession` | Session context; default time and -1 when absent. |
| `DistanceToPOC`, `DistanceToVAH`, `DistanceToVAL` | `(Price - session level)` in ticks, independent of signal direction; `NaN` when absent. |

The framework uses the host's first footprint indicator to describe probe events, even if a signal reads
another footprint. The probe adds a `SessionValueArea` requirement for that context indicator. Without bar
or session context, some numeric context fields retain their default zero values; inspect diagnostics
rather than treating every zero as a measured value.

An outcome has first-touch arrays `FavTime`, `FavBar`, `AdvTime`, `AdvBar`. Favorable/adverse follows the
signal direction. Array element `i` describes `(i + 1) * Step` ticks from the event price. Untouched levels
have `default(DateTime)` and bar -1. `FirstTickPrice` records the first processed tick after recording;
for a bar-close event it can be the same market event that triggered the signal. The ladder remains
anchored to `ProbeEvent.Price`.

`HorizonBars` defaults to 40; values <= 0 use one bar. `LadderTicks <= 0` derives ten bar ranges on a Range
validating series and 100 ticks otherwise. Reach is capped at 9999 ticks, step is derived to fit at most
100 levels per side, and resolved reach may be rounded down to a whole number of steps. Query
`ResolvedLadderTicks`, `ResolvedLadderStep`, and `ResolvedLadderLevels` after the first event. A wider reach
can lose resolution: off-grid stops cannot be reconstructed exactly.

The automatic path advances outcomes only on ticks of `PatternValidatingDataSeriesIdx`. Do not mix another
instrument's prices into a manually routed `OnTick(double, DateTime, int, bool)`. On a bar boundary, ended
observations close before the new tick advances their ladders. `TicksAtHorizon` stores the signed price
distance at closure; `MFE` is nonnegative, `MAE` nonpositive, and `BarsToMFE` is -1 until favorable movement
occurs. Excursion extrema can exceed the stored ladder's reach.

`Censored` is `None` when the full horizon completed, `SessionEnd` when session context changed first, or
`EndOfData` when the run ended. A probe without session context cannot enforce a session boundary.
`IsOpen` remains true until closure; `CloseOpenOutcomes()` closes remaining observations as `EndOfData`.

## Termination report and journal

Add this after the base lifecycle call in the host's `State.Terminated` block. The two tick arguments are
the consumer's configured stop and target for the first entry leg:

```csharp
if (State == State.Terminated && Strategy != null)
{
    for (int i = 0; i < Strategy.SignalProbes.Count; i++)
    {
        SignalProbe probe = Strategy.SignalProbes[i];
        probe.CloseOpenOutcomes();
        Print(probe.Diagnostics);
        Print(probe.BuildReport(stopLossTicks, profitTargetTicks, "My strategy"));
    }
}
```

The generic base does not close outcomes or print a report automatically. Call `CloseOpenOutcomes()` before
the final report. With both thresholds positive, `BuildReport` uses the nearest available ladder levels,
clamped to its reach, and prints any adjustment. First stop/target touch resolves the scenario; a timestamp
tie counts as the stop. A closed, uncensored event reaching neither uses `TicksAtHorizon`. Unresolved open
or censored events are excluded and counted by reason; a censored event whose bracket was touched is kept.

Rows are sorted by name and show counts, direction split, median MFE/MAE, and mean scenario outcome in ticks.
Every column uses the included events of that row. With either threshold nonpositive, expectation is `n/a`
and counts/extrema include all recorded events. `N<50` is a sample-size annotation
(`REPORT_MIN_EVENTS = 50`), not an assurance that a larger sample predicts future trading results.
The report does not model fills, commissions, slippage, multi-leg positions, or combinations of signals.

Journal creation is consumer code; `SignalProbe` has no generic `SaveToCsv` method. `ProbeDumpFormat` exposes
`ParamsHeader`, `WindowHeader`, `StrategyHeader`, `FiltersHeader`, `EventsHeader(levels, step)`, and
`EventRow(event, outcome)`. Touch names such as `Fav0100_Time` contain the level **in ticks**, not its index.
FootprintAction writes a metadata-bearing journal with parameters, settings, applied filters and warnings.
Reject older journals missing `unmarkedAlternatives` in `[params]` or `Percentile` in `[filters]`: they lack
the current rank semantics or per-signal threshold evidence. Do not silently interpret them as this format.

`Diagnostics` also reports missing context and outcome coverage. A nonempty event set with no touched
outcomes requires checking tick delivery; a zero count alone does not establish why a signal did not fire.
`Clear()` drops captured observations and counters while retaining the probe's signals.
