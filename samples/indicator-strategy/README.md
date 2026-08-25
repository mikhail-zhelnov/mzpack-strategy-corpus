# BiggestTradeIndicator — an indicator based on a strategy (rendering without trading)

Source (canonical): `Algo\Samples\Built-in\BiggestTradeIndicator.cs` (API sample).

A strategy `: MZpackStrategyBase` that does NOT trade and only computes indicators and renders —
a way to build a custom indicator on top of the MZpack engine.
It demonstrates:
- only `OnCreateIndicators` (without `OnCreateAlgoStrategy`) — `StrategyBigTradeIndicator`
  with `TradeFilterMin/Max`;
- custom rendering: `override OnRender(ChartControl, ChartScale)` + SharpDX
  `RenderTarget.DrawRectangle(...)`;
- `override OnRenderTargetChanged()` — binding `RenderTarget` to the `Stroke` brush;
- access to the indicator's data/view: `BigTradeIndicator.__ChartBars`,
  `((BigTradeBaseMVC)IndicatorMVC).TradesView`, `ITradeView`/`BigTradeViewItem`;
- drawing properties via `Stroke` and `[View]`.
A reference for when you need an "indicator" rather than trading logic.
