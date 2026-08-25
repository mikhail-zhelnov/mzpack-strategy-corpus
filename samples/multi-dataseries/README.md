# MultiDataSeriesAdvancedStrategy — trading on a second data series

Source (canonical): `Algo\Samples\Built-in\MultiDataSeriesAdvancedStrategy.cs` (API sample, `#if APISAMPLE`).

Demonstrates multi-instrument / multi-timeframe:
- `IsUnmanaged = true` — MANDATORY for a multi-dataseries strategy;
- `WorkingDataSeriesIdx = -1` — pass all series to the core;
- `AddDataSeries(name, BarsPeriodType, value)` — add a trading series;
- `TradingDataSeriesIdx = 1` — the index of the trading series;
- `Entry` with `StopLossCalculationMode`/`ProfitTargetCalculationMode = CalculationMode.Ticks`;
- the standard pattern assembly + `Strategy.Initialize(entryPattern, exitPattern, entries)`.
A reference for strategies that trade on a series other than the main chart series.
