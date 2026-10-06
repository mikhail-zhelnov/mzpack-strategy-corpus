# Filter calibration — API 2.4.18

`MZpack.NT8.Algo.FilterCalibration` computes Volume and absolute Delta thresholds from prior sessions.
It does not choose signals, submit orders, or configure a consumer's filtering policy automatically.

Verified source: product commit `185d67621238dd0ea1f08c1a358f7c84da0600d4` (API 2.4.18, 2026-10-06),
`MZpack.NT8/Algo/FilterCalibration.cs`, `TradingTime.cs`, and
`Algo/Strategies/FootprintAction/FootprintAction.cs`. All types below are in `MZpack.NT8.Algo`.

## Input and output types

| Type | Public fields |
| --- | --- |
| `CalibrationBar` | `int BarIdx`, `DateTime Time`, `long Volume`, `long Delta`, `double DurationMs`, `DateTime SessionBegin`, `int SessionEndBarIdx` |
| `CalibrationBucket` | `int Index`, `int Bars`, `long MinVolume`, `long MinDelta`, `bool Thin`, `bool Dominated`, `double MaxSessionShare`, `double ExpectedShare`, `double DominanceZ` |
| `CalibrationSession` | `DateTime SessionBegin`, `int Bars`, `double MedianVolume`, `double Coverage`, `double PoolShare`, `double PoolDominanceRatio`, `string ClosingBar`, `bool Dropped`, `string Verdict` |

`CalibrationBar.Time` is the bar **close** time; `Delta` is signed and the class takes its absolute value.
Session identifiers and closing-bar indices must come from the actual footprint sessions, not calendar
dates guessed from the trading window. `DurationMs` is retained in the projected input/dump but is not a
threshold variable. `ClosingBar` in the output is `excluded`, `out of window`, or `not found`.

`IntradayBuckets` has `Off`, `Minutes60`, and `Minutes30`. Its grid is anchored to midnight of the supplied
bar clock, not the session open. `Off` uses one bucket. Use the same clock for input close times and
`TradingTime.Begin/End`; do not substitute exchange hours for chart times without verifying the conversion.
Trading windows include their endpoints and support overnight ranges. Empty/null windows include all bars;
coverage is then undefined and cannot reject a session.

## Constructing and rebuilding a baseline

```csharp
public FilterCalibration(int baselineSessions, double percentile,
    IntradayBuckets buckets, double sessionOutlierTolerance);

public void Build(IEnumerable<CalibrationBar> bars,
    IEnumerable<TradingTime> tradingTimes, DateTime currentSessionBegin,
    string headerInfo);
```

`percentile` is on a 0..100 scale. `sessionOutlierTolerance` is a fraction: 0.5 means 50%, not 0.5%.
The constructor stores configuration and does not build a sample. The consumer is responsible for valid
finite configuration values and enough candidate sessions.

Build on a session boundary before evaluating signals in that session. Rebuild if the windows or baseline
configuration change. `Build` allocates and sorts samples; do not run it on every tick. A changing percentile
can use the query overloads below without rebuilding.

**The caller must supply only bars closed strictly before `currentSessionBegin`.** Build excludes sessions
whose `SessionBegin >= currentSessionBegin`, but does not validate each bar's close timestamp. That session
check alone does not prevent malformed input or future-bar leakage. An old session's closing bar is excluded
by `BarIdx == SessionEndBarIdx`, not by its position in a time-sorted list.

This helper projects real footprint data using the product's snapshot pattern. It intentionally skips bars
without known session metadata instead of fabricating it:

```csharp
private static List<CalibrationBar> ProjectClosedBars(
    StrategyFootprintIndicator footprint, DateTime currentSessionBegin)
{
    var bars = footprint.FootprintBars;
    List<CalibrationBar> projected;
    lock (bars)
    {
        projected = new List<CalibrationBar>(bars.Count);
        foreach (var pair in bars)
        {
            IFootprintBar bar = pair.Value;
            if (bar == null || bar.Time >= currentSessionBegin)
                continue;
            projected.Add(new CalibrationBar
            {
                BarIdx = bar.BarIdx,
                Time = bar.Time,
                Volume = bar.Volume,
                Delta = bar.Delta,
                DurationMs = bar.DurationMs
            });
        }
    }

    var resolved = new List<CalibrationBar>(projected.Count);
    for (int i = 0; i < projected.Count; i++)
    {
        CalibrationBar item = projected[i];
        ISession session = footprint.GetSession(item.Time);
        if (session == null)
            continue;
        item.SessionBegin = session.BeginTime;
        item.SessionEndBarIdx = session.EndBarIdx;
        resolved.Add(item);
    }
    return resolved;
}
```

The helper needs `System`, `System.Collections.Generic`, `MZpack`, `MZpack.NT8.Algo`, and
`MZpack.NT8.Algo.Indicators`. Call it after the footprint/session data is available. For example, in a
consumer's session-boundary method, where `currentSessionBegin` is the known current session begin time:

```csharp
calibration = new FilterCalibration(10, 70, IntradayBuckets.Minutes60, 0.5);
calibration.Build(ProjectClosedBars(footprint, currentSessionBegin),
    Strategy.TradingTimes, currentSessionBegin, "My strategy");
Print(calibration.Diagnostics);
```

These are example configuration values. The latest ten candidate sessions are selected before rejection;
rejected candidates are not replaced by older sessions. Input enumeration order does not choose the
baseline. During historical replay, rebuild separately for each session using its own begin time; never
apply the newest session's baseline backwards across the replay. Before live use, confirm that warm-up,
bar clock, actual session metadata and historical tick/volume coverage are available.

## Readiness and rejection rules

Check `IsReady` before consuming thresholds. Readiness requires all of the following in this release:

- At least `baselineSessions` candidate sessions with in-window non-closing bars.
- No inconsistent candidate session groups (one begin time associated with different closing-bar indices).
- A nonzero median of the structurally surviving sessions' median volumes.
- At least `max(3, ceil(baselineSessions * 0.7))` surviving sessions.
- At least 200 pooled bars and a positive pooled Volume threshold.

`IsWarmingUp` specifically means too few candidate sessions. `NotReadyReason` lists all failed criteria;
not every not-ready result is warm-up. `SessionsAvailable` is the candidate count before rejection;
`SessionsUsed` and `BarsUsed` describe the retained pool. Output statistics can exist while `IsReady` is
false; they are diagnostic evidence, not usable thresholds.

Sessions are rejected in this order: inconsistent group, fewer than 20 usable bars, window coverage below
70%, excessive share of pooled bars, then median-Volume outlier. Window coverage is the span between earliest
and latest in-window closes divided by the total window duration. It is a span test, not proof that every
minute inside the span has data; with separated windows the value can exceed 1. Missing windows yield
`double.NaN` coverage and skip that check.

Pool dominance uses bar count, measured once over the structurally surviving candidates: a session is
rejected when its observed share exceeds twice its equal expected share (`PoolDominanceRatio > 2.0`).
Shares are not recomputed repeatedly after dropping sessions. Outlier rejection compares each surviving
session's median Volume with the median of those medians, and rejects absolute relative deviation strictly
greater than `sessionOutlierTolerance`. Existing structural verdicts keep their priority.

Buckets with fewer than 30 bars fall back to the common pool. A bucket also falls back when its largest
contributing session has both `DominanceZ > 4.0` and observed/expected share `>= 2.0`; expected share is that
session's bar share in the retained pool, not an equal-session assumption. Thin and Dominated may both be
true. A bucket absent from the baseline uses the pool too. These are fallback decisions, not session drops.

Volume and absolute Delta are separate empirical distributions. Thresholds use linear interpolation and
round upward to an integer; consumers compare with `>=`. Delta percentage is not calibrated by this class.

## Threshold queries and per-signal percentiles

```csharp
long GetMinVolume(DateTime barCloseTime);
long GetMinDelta(DateTime barCloseTime);
long GetMinVolume(DateTime barCloseTime, double percentile);
long GetMinDelta(DateTime barCloseTime, double percentile);
long GetPoolMinVolume(double percentile);
long GetPoolMinDelta(double percentile);
```

One-argument methods use the constructor percentile. Two-argument methods query the same retained sample at
another percentile, clamped to 0..100; no new baseline is built. Use each examined bar's own close time,
especially when a multi-bar condition crosses a bucket boundary. For an otherwise global threshold, the
pool query avoids selecting an arbitrary time of day.

```csharp
long minVolume = manualMinVolume;
long minDelta = manualMinDelta;
if (filtersAuto && calibration != null && calibration.IsReady)
{
    minVolume = calibration.GetMinVolume(bar.Time, signalPercentile);
    minDelta = calibration.GetMinDelta(bar.Time, signalPercentile);
}
bool passes = bar.Volume >= minVolume && Math.Abs(bar.Delta) >= minDelta;
```

This is a consumer policy example: `bar`, `filtersAuto`, `signalPercentile` and manual thresholds are
consumer-owned. A consumer can instead block trading during warm-up if its specification requires that.
Not-ready getters return **0** and set `NotReadyAccessDetected`; using them blindly removes filters instead
of safely choosing a fallback. `PoolMinVolume`/`PoolMinDelta` and the bucket/session collections do not
replace the readiness check.

Read-only status also includes `BucketsCount`, `ThinBucketsCount`, `DominatedBucketsCount`, `ElapsedMs`,
`Diagnostics`, `IReadOnlyList<CalibrationBucket> Buckets`, and `IReadOnlyList<CalibrationSession> Sessions`.
The collections describe nonempty buckets by index and candidate sessions by begin time.

## Ranks and selecting a recorded sample

```csharp
double GetVolumeRank(DateTime barCloseTime, long volume);
double GetDeltaRank(DateTime barCloseTime, long delta); // accepts signed delta
static double RankOf(long[] sorted, long value);
```

Queries use the exact distribution that supplied that bucket's threshold, including pool fallback. They
return `double.NaN` when not ready, and set `NotReadyAccessDetected`. `RankOf` expects ascending sorted data
and returns `NaN` for null/empty samples. Ties resolve to the highest matching position; a value at/above
the maximum gives 100 and one below the minimum gives 0.

For values at or above the sample minimum, integer filtering at percentile P agrees exactly with
`rank >= P`. The below-minimum result is a special case: rank 0 still fails the P=0 threshold, which is the
sample minimum. Unknown (`NaN`, written as `n/a`) must not be treated as the known rank zero.

A journal collected at a low percentile can only select a subset at a higher percentile; firings rejected
during collection cannot be recovered. Exact event selection also requires that the recorded event ranks
represent every percentile-dependent condition: AND takes the weakest condition, OR the strongest passing
alternative. A single event bar's rank is insufficient for a multi-bar condition. Absolute filters and
manual Delta-percentage filters must remain accounted for separately. The class provides bar rank queries,
not generic Boolean-condition accumulation or proof that any consumer's journal selection is exact.
See [signal probe](signal-probe.md) for its optional rank callbacks.

## Diagnostics and support dumps

`Diagnostics` reports the current baseline, session verdicts, bucket thresholds, coverage, pool shares and
dominance evidence. Treat missing or dropped input as an observed state; the class cannot establish a
provider outage or another cause merely from a short session.

`FilterCalibration` does not write files itself. FootprintAction's `Filters: dump baseline` writes paired
input `.bars.csv` and recorded-output `.expected.csv` files under the strategy's `calibration` folder.
The output has parameters/window, readiness result, session verdicts and bucket results; missing numerical
values use `n/a`. The public `DumpFormat` class supplies headers and row formatting, including
`BarsHeader`, `ParamsHeader`, `WindowHeader`, `ResultHeader`, `SessionsHeader`, `BucketsHeader`,
`BarRow`, `SessionRow`, `BucketRow`, `Fraction`, `TimeFormat` and `NotDefined`.

The support-dump format has no versioned backward compatibility. Validate its headers rather than silently
reading an older shape. A generic consumer must create its own writer if it needs these artifacts; do not
invent a `FilterCalibration.Export` or `SaveToCsv` method.
