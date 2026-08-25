# RiskManagement — risk limits + complete minimal skeleton

Source (canonical): `Algo\Samples\Built-in\RiskManagement.cs` (official API sample, `#if APISAMPLE`).

Demonstrates the `RiskManagement` class and a complete minimal strategy skeleton:
- `strategy.RiskManagement = new RiskManagement(strategy) { Currency, EntryName,
  DailyLossLimitEnable/DailyLossLimit, DailyProfitLimitEnable/DailyProfitLimit,
  DailyTradesLimitEnable/DailyTradesLimit }`;
- assembly in `Configure`: `Entry[]` (Market, StopLossTicks/ProfitTargetTicks) →
  `new Pattern(Strategy, Logic.And, null, true)` → `pattern.Signals.Root.AddChild(signal)` →
  `Strategy.Initialize(pattern, entries, 3)`;
- a nested `UpDownBarSignal : Signal` (long on an up bar, short on a down bar);
- `lock (Sync)` around the `OnStateChange` handler;
- risk parameters as `[NinjaScriptProperty]`.
A good reference for a "complete small strategy from start to finish".
