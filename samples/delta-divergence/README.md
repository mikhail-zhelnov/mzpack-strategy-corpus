# DeltaDivergenceSignal — reversal signal based on delta divergence

Source (canonical): `MZpack.NT8\Algo\Strategies\FootprintAction\DeltaDivergenceSignal.cs`. This is a snapshot for learning; edit the original.

Pattern: trend reversal.
- LONG: price makes a new low, but the candle is bullish and delta is positive.
- SHORT: price makes a new high, but the candle is bearish and delta is negative.

What it demonstrates:
- a minimal, clean signal — the best "hello world";
- the calculation moment: `SignalCalculate.OnBarClose`;
- working with candles through the host: `GetCandle(1)` (closed), `GetCandle(2)` (previous), `IsBullish()/IsBearish()`, `Low/High`;
- injection of `StrategyFootprintIndicator` through the constructor; `IFootprintBar.Delta` is
  unconditional, so this signal needs no capability declaration;
- the host's shared filter `CheckBarFilters(...)` with filter mode and percentile in API 2.4.18;
  these properties and the `FootprintAction` cast must be adapted in a standalone host;
- completion: `ResolveDirection` → `IsDetermined` → filling in `Direction/Time/EntryPrice/ChartRange/Description`.
