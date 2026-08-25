# StackedImbalanceSignal — stack of imbalances

Source (canonical): `MZpack.NT8\Algo\Strategies\FootprintAction\StackedImbalanceSignal.cs`. A snapshot for learning; edit the original.

Pattern: N or more imbalances in a row.
- LONG: N+ buy imbalances (Ask zones).
- SHORT: N+ sell imbalances (Bid zones).

What it demonstrates:
- working with zones: `bar.ImbalanceSRZones.Zones[(int)TradeSide.Bid|Ask]` (`.Count`, `.Hi`, `.Lo`, LINQ `OrderBy`);
- a POC filter (imbalance above/below the POC) and a reverse flag;
- an early exit when the direction is ambiguous (zones on both sides);
- `ResolveDirection`/`IsDetermined` without the `Signal.` prefix (inherited).
