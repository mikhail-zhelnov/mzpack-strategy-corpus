# extensions/ — custom Entry / Exit / Trail

IMPORTANT: SL/TP/trailing/exit in MZpack are NOT only declarative values (`StopLossTicks` and the like).
There are extensible base classes that you INHERIT from and override for arbitrary logic.
Source (canonical): `MZpack.NT8\Algo\Extensions`. These are compact reference snapshots.

## Base classes and overridable methods
- `Entry : EntryBase` — entry. Override:
  - `double GetStopLossValue()` — custom stop price;
  - `double GetEntryPrice(double entry)` — custom entry price (for limit orders).
  - Properties: `Quantity`, `EntryMethod` (Market/Limit/StopLimit), `SignalName`,
    `StopLossCalculationMode`, `StopLossPrice`, `Strategy`, `Position`.
- `ExitBase` — exit. Override:
  - `bool CheckExit(MarketDataEventArgs e, Entry entry, MarketPosition mp, out string reason)`.
  - Property `Calculate` (OnBarClose/OnEachTick).
- `TrailBase` — trailing. Override:
  - `bool IsBeginTrailing(MarketPosition, Entry)`;
  - `bool IsTrailingStep(MarketPosition, Entry)`;
  - `double GetTrailingPrice(MarketPosition, Entry)`.
  - Property `TrailingPrice`.

## References in this folder
| File | Base | What it does |
|------|------|-----------|
| `BarStopLossEntry.cs`    | Entry    | stop at the High/Low of a chosen previous bar + offset in ticks |
| `SignalStopLossEntry.cs` | Entry    | stop at the price computed inside the signal (`StopLossPrice`) + offset |
| `FiboRetracementEntry.cs`| Entry    | limit entry at a Fibonacci level between SwingHigh/SwingLow |
| `BarCloseTarget.cs`      | ExitBase | exit at the close of the next bar after entry |
| `BarHiLoTrail.cs`        | TrailBase| trail by the bar's High/Low (Low for Long, High for Short) + offset |

## How to wire it up
Instead of a plain `new Entry(Strategy){...}` you create your own subclass and pass it into `Entry[]`:
```csharp
entries[0] = new BarStopLossEntry(Strategy) {
    EntryMethod = EntryMethod.Limit, Quantity = 1, SignalName = ENTRY1,
    ProfitTargetTicks = 20, StopLossBarsAgo = 1, StopLossShiftTicks = 2
};
```
Exit/trail are assigned to an Entry (the `Trail`/exit mechanism) — for the exact trail binding see Entry.cs
(`MZpack.NT8\Algo\Extensions\Entries\Entry.cs`) and the product strategies.

Useful instrument helpers in these classes: `Strategy.MZpackStrategy.GetCurrentBid()/GetCurrentAsk()`,
`Instrument.MasterInstrument.RoundToTickSize(...)`, `Strategy.TicksToPrice(ticks)`,
`Strategy.MZpackStrategy.High[barsAgo]/Low[barsAgo]`, `BarsSinceEntryExecution(signalName)`.
