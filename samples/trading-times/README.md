# TradingTimes — scheduled trading windows

Source (canonical): `Algo\Samples\Built-in\TradingTimes.cs` (official API sample, `#if APISAMPLE`).

Demonstrates restricting trading by time via `Strategy.TradingTimes`.

Key points:
- adding windows: `strategy.TradingTimes.Add(new TradingTime { Begin = ..., End = ... })`
  for each enabled interval;
- the time is entered as strings in the UI (`"08:30:00"`) and parsed by `DateTime.TryParse`
  (the `TryParseDateTime` helper; errors go through `NTMessageBox.Show`);
- `strategy.SessionBreak = !(Time1_Enable || Time2_Enable)` — disable the session reset
  when a schedule is set;
- the rest is the standard skeleton: `Entry[]` (Market, SL/TP in ticks) → `Pattern(Logic.And)` →
  `Signals.Root.AddChild(...)` → `Strategy.Initialize(pattern, entries, 3)`;
- a nested `UpDownBarSignal` as a simple trigger for demonstration.

A reference for when you need to trade only during specified intervals (the RTH open, news windows, etc.).
