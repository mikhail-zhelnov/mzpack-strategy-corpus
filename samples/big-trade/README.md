# BigTradeSignal — large trades in the bar's tail

Source (canonical): `MZpack.NT8\Algo\Strategies\GhostResistance\BigTradeSignal.cs`. A snapshot for learning; edit the original.

Pattern (on bar close):
- LONG: one or more large sell trades in the lower wick (the trade's POC below the body).
- SHORT: one or more large buy trades in the upper wick (the trade's POC above the body).

What it demonstrates:
- a different data source — `strategy.BigTradeIndicator.Trades` (`ITrade`: `.StartBarIdx`, `.Side`, `.POC`);
- filtering trades by bar and side via LINQ;
- a custom `EntryPrice`: a limit at the best POC of the large trade when `Position_EnterByLimitOrder`, otherwise `GetBestEntryPrice`;
- using `SignalDirection.Any` when the direction is ambiguous;
- `GetCurrentBarAgo(0)` to access the candle.
