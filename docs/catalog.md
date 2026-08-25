# Catalog — which example to open

Start from the pattern you want to build, not from the file list.

## Entry signals

| Pattern | Open | What it shows |
|---|---|---|
| Delta divergence | `samples/delta-divergence/` | The minimal clean signal — `bar.Delta` plus `ICandle` via `GetCandle(1)` / `GetCandle(2)`, bull/bear branches. Start here if you have never written one. |
| Stacked imbalance | `samples/stacked-imbalance/` | `bar.ImbalanceSRZones.Zones[Bid/Ask]`, POC filter, reversal handling. |
| Absorption in a hammer | `samples/hammer-absorption/` | Absorption in the wick with a POC filter. Requires a capability declaration — see `pitfalls.md` §1. |
| Big trade | `samples/big-trade/` | `BigTradeIndicator.Trades` (`ITrade`), a custom `EntryPrice` (limit at POC), `SignalDirection.Any`. |
| Approaching a level | `samples/approaching-level/` | `StrategyVolumeProfileIndicator`, overnight and prior-session levels, approach logic. |

## Trade and risk management

| Topic | Open | What it shows |
|---|---|---|
| Risk limits, plus a complete miniature strategy | `samples/risk/` | `RiskManagement`: daily loss, profit and trade limits. Also the shortest end-to-end strategy in the corpus. |
| Custom entry, exit and trailing | `samples/extensions/` | `BarStopLossEntry`, `SignalStopLossEntry`, `FiboRetracementEntry`, `BarCloseTarget`, `BarHiLoTrail` — the pattern for subclassing `Entry`, `ExitBase` and `TrailBase`. |
| Scheduled trading windows | `samples/trading-times/` | `TradingTimes`, session break. |

## Beyond a single series

| Topic | Open | What it shows |
|---|---|---|
| Trading on a second data series | `samples/multi-dataseries/` | Secondary series wiring. |
| An indicator built on a strategy | `samples/indicator-strategy/` | Rendering from a strategy — marking the biggest trade on the chart. |
| Custom plots | `samples/custom-plots/` | `StrategyPlotIndicator`, own panel over the chart. |
| Exporting to CSV | `samples/export/` | `DrawingObjectsExport`, schemas, `ChartObjectDescriptor`. |

## Templates

| Template | When |
|---|---|
| `templates/StrategyTemplate` | Any new strategy. `Entry[]` + `Pattern` + signals tree + `Strategy.Initialize`. |
| `templates/DashboardTemplate` | You want the built-in Pattern Dashboard — a grid of the signals tree with a legend. No custom rendering needed. |
| `templates/ControlPanelTemplate` | You want buttons on the chart: trading on/off, close, break even. |

## Reading order for a first strategy

1. `AGENTS.md` sections 1–3 — anatomy, how to write a signal, how the tree is assembled.
2. `samples/delta-divergence/` — the smallest complete signal.
3. `templates/StrategyTemplate` — copy it, rename it, build it before changing anything.
4. `docs/pitfalls.md` §1 — the one that costs everybody an afternoon.
5. Then the sample matching the pattern you actually want.

## Whole strategies

`FootprintAction` and `GhostResistance` ship compiled with MZpack Strategies. The signals from
both are in `samples/` — those are the parts worth reading. For the shape of a large multi-signal
host, `templates/DashboardTemplate` shows the same structure in a size you can hold in your head.
