# CustomPlots — custom plots based on StrategyPlotIndicator

Source (canonical): `Algo\Samples\Built-in\CustomPlots.cs` (official API sample, `#if APISAMPLE`).

Shows how to draw your own lines/values and read mzFootprint data.

Key points:
- a custom plot indicator: `class MyPlots : StrategyPlotIndicator`, constructor
  `base(strategy, input, new MZpack.Series<double>[N])` — N plots;
- logic in `override BarUpdate()`: writing to `Values[i][barIdx]`; accessing bars via
  `__CurrentBar`, `__High[1]`, `__Low[1]`, `strategy.TickSize`;
- reading footprint data from the plot: `(strategy as CustomPlots).FootprintIndicator
  .FootprintBars.TryGetValue(barIdx, out IFootprintBar bar)` → `bar.COTHigh/COTLow`;
- a plot on a separate panel: `IsOnPanel = true`, `PanelHeight`, `IsUpperPanel`;
- styling: `Strokes[i] = new Stroke(Brushes.Red, 2)`, `Calculate = Calculate.OnBarClose`, `Visible`;
- wiring up: create in `CreateIndicators()` and add to `List<TickIndicator>` alongside
  `StrategyFootprintIndicator`;
- an extensive `StrategyFootprintIndicator` configuration example (VA/POC/COT/imbalances/absorptions/
  SR zones/unfinished auction/cluster zones) — handy as a reference for footprint settings.

Note: the `IndicatorName()` override is required; `EnableBacktesting = true` — so that plots are
computed on historical bars; `FootprintIndicator.Calculate = Calculate.OnBarClose`.
