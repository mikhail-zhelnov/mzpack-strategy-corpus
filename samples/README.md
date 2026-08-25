# samples/ — worked examples, one technique each

Verbatim snapshots from the MZpack product sources and the official API samples. Each one is a
real, readable example of a single technique, with a README next to it explaining what it
demonstrates, which API members it uses, and at which moment it calculates.

Which one to open for which pattern: `../docs/catalog.md`.

## Entry signals

| Pattern | Folder | File |
|---|---|---|
| delta / divergence | `delta-divergence/` | DeltaDivergenceSignal |
| imbalance / stacked | `stacked-imbalance/` | StackedImbalanceSignal |
| big trade | `big-trade/` | BigTradeSignal |
| absorption | `hammer-absorption/` | HammerWithAbsorption |
| levels | `approaching-level/` | ApproachingToSessionLevelSignal |

## Trade and risk management

| Topic | Folder | File |
|---|---|---|
| risk limits + complete mini-strategy | `risk/` | RiskManagement |
| custom Entry / Exit / Trail | `extensions/` | BarStopLossEntry, SignalStopLossEntry, FiboRetracementEntry, BarCloseTarget, BarHiLoTrail |

## Advanced techniques

| Topic | Folder | File |
|---|---|---|
| trading on a second data series | `multi-dataseries/` | MultiDataSeriesAdvancedStrategy |
| scheduled trading windows | `trading-times/` | TradingTimes |
| indicator built on a strategy | `indicator-strategy/` | BiggestTradeIndicator |
| custom plots (`StrategyPlotIndicator`) | `custom-plots/` | CustomPlots |
| exporting data and chart objects to CSV | `export/` | DrawingObjects_Export |

## These files do not compile as they stand

The snapshots still carry `#if STRAT`, `#if DATA` and `#if APISAMPLE` directives from the
sources they came from. Remove the directive and its `#endif` when you copy a sample into your
own project.

They are left unedited deliberately: an edited snapshot drifts from the original and quietly
becomes wrong, an unedited one does not. `samples/` is not part of any `.csproj`, so nothing here
is compiled and nothing conflicts.

## More examples on your own machine

Your MZpack installation carries roughly two dozen further API samples under
`Documents\NinjaTrader 8\...\API samples` — control panel, data access per indicator, exporting
indicator values, multi-series, advanced template. Good material when the corpus does not cover
what you need.
